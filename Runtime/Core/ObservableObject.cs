using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Services;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Runtime.Core
{
    public enum UpdateMode
    {
        Default,        // ✅ Event-driven
        HighFrequency   // ✅ Polled every frame
    }

    public abstract class ObservableObject : ScriptableObject, IPoolable, ICoreRegistrable, IRegistrable
    {
        public EntityId entityId;

        [Header("ReaCS Runtime Settings")]
        public UpdateMode updateMode = UpdateMode.Default;

        public event Action<ObservableObject, string> OnChanged;

        private static readonly Dictionary<Type, List<CachedFieldInfo>> _fieldCache = new();
        private List<CachedFieldInfo> _observedFields;
        private Dictionary<string, object> _cachedValues = new();

        private bool _isDirty = false;
        private string lastChangedField;

#if UNITY_EDITOR
        private static readonly Dictionary<ObservableObject, Dictionary<string, object>> _defaultValueCache
            = new();
#endif
        // Pool setup for pool type inference via interface reference
        private IPool _pool;
        public void SetPool(IPool pool) => _pool = pool;

        // ───────────────────────────────
        // Unity Lifecycle
        // ───────────────────────────────
        protected virtual void OnEnable()
        {
            InitializeFields(); // ✅ FIX: must be first to avoid HighFrequency NREs

            Register();

#if UNITY_EDITOR
            EditorApplication.delayCall += () =>
            {
                if (this) LoadStateFromJson();
            };
#else
            LoadStateFromJson();
#endif
        }

        protected virtual void OnDisable()
        {
            Unregister();

#if !UNITY_EDITOR
            SaveStateToJson();
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            ProcessAllFields();
        }
#endif

        public virtual void Initialize()
        {
            Register();
        }

        public virtual void Release()
        {
            Unregister(); 
            _pool?.Release(this); // Handles returning to pool
        }

        public void Register()
        {
            RegisterBase();
            RegisterSelf();
        }
        public void Unregister()
        {
            UnregisterBase();
            UnregisterSelf();
        }

        protected void RegisterBase()
        {
            ObservableRuntimeWatcher.Register(this);
            ObservableRegistry.Register(this);
            Query<IndexRegistry>().Register(this);
        }
        protected void UnregisterBase()
        {
            ObservableRuntimeWatcher.Unregister(this);
            ObservableRegistry.Unregister(this);
            Query<IndexRegistry>().Unregister(this);
        }

       // used in all subclasses to override specific registrations to particular registries and or pools.
        public abstract void RegisterSelf();
        public abstract void UnregisterSelf();


        // ───────────────────────────────
        // Initialization
        // ───────────────────────────────
        private void InitializeFields()
        {
            Type type = GetType();

            if (!_fieldCache.TryGetValue(type, out var cachedFields))
            {
                cachedFields = new();
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!Attribute.IsDefined(field, typeof(ObservableAttribute)) &&
                        !Attribute.IsDefined(field, typeof(ObservableSavedAttribute)))
                        continue;

                    var valueProp = field.FieldType.GetProperty("Value");
                    var persistField = field.FieldType.GetField("ShouldPersist", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    cachedFields.Add(new CachedFieldInfo
                    {
                        Field = field,
                        ValueProperty = valueProp,
                        ShouldPersistField = persistField
                    });
                }

                _fieldCache[type] = cachedFields;
            }

            _observedFields = cachedFields;

            foreach (var cached in _observedFields)
            {
                var field = cached.Field;
                var value = field.GetValue(this);

                if (value == null)
                {
                    value = Activator.CreateInstance(field.FieldType);
                    field.SetValue(this, value);
                }

                var initMethod = field.FieldType.GetMethod("Init");
                initMethod?.Invoke(value, new object[] { this, field.Name });

                var currentVal = cached.ValueProperty?.GetValue(value);
                _cachedValues[field.Name] = currentVal;
            }
        }

        // ───────────────────────────────
        // Dirty Tracking
        // ───────────────────────────────
        public void MarkDirty(string fieldName)
        {
            lastChangedField = fieldName;
            _isDirty = true;

            if (updateMode == UpdateMode.Default)
                ObservableRuntimeWatcher.NotifyDirty(this, fieldName);
        }

        internal void ProcessFieldChange(string fieldName)
        {
            if (_observedFields == null || _cachedValues == null) return;

            foreach (var cached in _observedFields)
            {
                if (cached.Field.Name != fieldName) continue;

                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(cached.Field.Name, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[cached.Field.Name] = newValue;
                    OnChanged?.Invoke(this, cached.Field.Name);
                }
                break;
            }
            _isDirty = false;
        }

        internal void ProcessAllFields()
        {
            if (_observedFields == null || _cachedValues == null) return;

            foreach (var cached in _observedFields)
            {
                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(cached.Field.Name, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[cached.Field.Name] = newValue;
                    OnChanged?.Invoke(this, cached.Field.Name);
                }
            }
            _isDirty = false;
        }

        /// <summary>
        /// Lightweight deterministic hash for HighFrequency update mode.
        /// </summary>
        public float ComputeFastHash()
        {
            if (_observedFields == null) return 0f;

            unchecked
            {
                int hash = 17;
                for (int i = 0; i < _observedFields.Count; i++)
                {
                    var cached = _observedFields[i];
                    var obs = cached.Field.GetValue(this);
                    var val = cached.ValueProperty?.GetValue(obs);
                    hash = hash * 31 + (val?.GetHashCode() ?? 0);
                }
                return hash;
            }
        }

        // ✅ HighFrequency support (now null-safe)
        internal IEnumerable<string> GetObservedFieldNames()
        {
            if (_observedFields == null) yield break;

            for (int i = 0; i < _observedFields.Count; i++)
                yield return _observedFields[i].Field.Name;
        }

        internal object GetObservedFieldValue(string fieldName)
        {
            if (_observedFields == null) return null;

            for (int i = 0; i < _observedFields.Count; i++)
            {
                if (_observedFields[i].Field.Name == fieldName)
                {
                    var obs = _observedFields[i].Field.GetValue(this);
                    return _observedFields[i].ValueProperty?.GetValue(obs);
                }
            }
            return null;
        }

        // ───────────────────────────────
        // PlayMode Reset (Non-persistent Fields)
        // ───────────────────────────────
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void SetupEditorResetHook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                {
                    CacheDefaultValues();
                }
                else if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    RestoreDefaultValues();
                }
            };
        }

        private static void CacheDefaultValues()
        {
            _defaultValueCache.Clear();
            foreach (var so in Resources.FindObjectsOfTypeAll<ObservableObject>())
            {
                if (!_defaultValueCache.ContainsKey(so))
                    _defaultValueCache[so] = new Dictionary<string, object>();

                foreach (var cached in so._observedFields ?? new List<CachedFieldInfo>())
                {
                    var obs = cached.Field.GetValue(so);
                    var currentVal = cached.ValueProperty?.GetValue(obs);
                    _defaultValueCache[so][cached.Field.Name] = currentVal;
                }
            }
        }

        private static void RestoreDefaultValues()
        {
            foreach (var so in _defaultValueCache.Keys)
            {
                if (so == null) continue;

                foreach (var cached in so._observedFields ?? new List<CachedFieldInfo>())
                {
                    if (cached.ShouldPersistField == null) continue;

                    bool shouldPersist = (bool)(cached.ShouldPersistField
                        .GetValue(cached.Field.GetValue(so)) ?? false);

                    if (!shouldPersist &&
                        _defaultValueCache[so].TryGetValue(cached.Field.Name, out var defaultVal))
                    {
                        var targetObs = cached.Field.GetValue(so);
                        ObservablePlayModeGuard.Suppress = true;
                        cached.ValueProperty?.SetValue(targetObs, defaultVal);
                        ObservablePlayModeGuard.Suppress = false;
                    }
                }
            }

            _defaultValueCache.Clear();
        }
#endif

        // ───────────────────────────────
        // Persistence
        // ───────────────────────────────
        private string GetSavePath()
        {
#if UNITY_EDITOR
            return Path.Combine("Temp", name + "_snapshot.json");
#else
            return Path.Combine(Application.persistentDataPath, name + "_state.json");
#endif
        }

        public void SaveStateToJson()
        {
            var json = JsonUtility.ToJson(this);
            File.WriteAllText(GetSavePath(), json);
        }

        public void LoadStateFromJson()
        {
            string path = GetSavePath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var clone = CreateInstance(GetType()) as ObservableObject;
            JsonUtility.FromJsonOverwrite(json, clone);

            foreach (var cached in _observedFields)
            {
                var field = cached.Field;

                var sourceObs = field.GetValue(clone);
                var targetObs = field.GetValue(this);

                if (sourceObs == null || targetObs == null) continue;

                bool shouldPersist = cached.ShouldPersistField != null &&
                                     (bool)(cached.ShouldPersistField.GetValue(targetObs) ?? false);

                if (shouldPersist)
                {
                    var value = cached.ValueProperty?.GetValue(sourceObs);
                    cached.ValueProperty?.SetValue(targetObs, value);
                }
            }

            ObservableRuntimeWatcher.Unregister(clone);
            ObservableRegistry.Unregister(clone);

#if UNITY_EDITOR
            DestroyImmediate(clone);
#else
            Destroy(clone);
#endif
        }

        // ───────────────────────────────
        // Internal Types
        // ───────────────────────────────
        private class CachedFieldInfo
        {
            public FieldInfo Field;
            public PropertyInfo ValueProperty;
            public FieldInfo ShouldPersistField;
        }
    }
}
