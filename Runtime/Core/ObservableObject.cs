using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using ReaCS.Runtime.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using static ReaCS.Runtime.Access;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Runtime.Core
{
    public abstract class ObservableObject : ScriptableObject, IPoolable, ICoreRegistrable, IRegistrable
    {
        public EntityId entityId;
        public event Action<ObservableObject, string> OnChanged;

        [SerializeField, HideInInspector] private int snapshotVersion = 1;
        public int SnapshotVersion => snapshotVersion;

        private static readonly Dictionary<Type, List<CachedFieldInfo>> _fieldCache = new();
        private List<CachedFieldInfo> _observedFields;
        private Dictionary<string, object> _cachedValues = new();
        private IPool _pool;

        public void SetPool(IPool pool) => _pool = pool;

        private static string _pendingName;
        private static bool _hasPendingName;

        /// <summary>
        /// Used internally to assign a name to a ScriptableObject right after instantiation.
        /// Wrap this in a `using` block for safety.
        /// </summary>
        public readonly struct NameInjectionScope : IDisposable
        {
            public NameInjectionScope(string name)
            {
                _pendingName = name;
                _hasPendingName = true;
            }

            public void Dispose()
            {
                _pendingName = null;
                _hasPendingName = false;
            }
        }

#if UNITY_EDITOR
        private static readonly Dictionary<ObservableObject, Dictionary<string, object>> _defaultValueCache = new();
#endif

        public virtual void OnEnable()
        {
            if (_hasPendingName && !string.IsNullOrEmpty(_pendingName))
            {
                this.name = _pendingName;
                _pendingName = null;
                _hasPendingName = false;
            }
            InitializeFields();
            Register();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                LoadStateFromJson();
#else
            LoadStateFromJson();
#endif
        }

        protected virtual void OnDisable()
        {
            Unregister();

#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
                SaveStateToJson();
#else
            SaveStateToJson();
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnValidate() => ProcessAllFields();
#endif

        public virtual void Initialize() => Register();

        public virtual void Release()
        {
            Unregister();
            _pool?.Release(this);
        }

        public void Register()
        {
            ObservableRegistry.Register(this);
            Query<IndexRegistry>().Register(this);
            RegisterSelf();
        }

        public void Unregister()
        {
            ObservableRegistry.Unregister(this);
            Query<IndexRegistry>().Unregister(this);
            UnregisterSelf();
        }

        public abstract void RegisterSelf();
        public abstract void UnregisterSelf();

        private void InitializeFields()
        {
            Type type = GetType();

            if (!_fieldCache.TryGetValue(type, out var cachedFields))
            {
                cachedFields = new();
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!Attribute.IsDefined(field, typeof(ObservableAttribute))) continue;

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

        internal void ProcessFieldChange(string fieldName)
        {
            if (_observedFields == null || _cachedValues == null) return;

            foreach (var cached in _observedFields)
            {
                if (cached.Field.Name != fieldName) continue;

                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(fieldName, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[fieldName] = newValue;
                    OnChanged?.Invoke(this, fieldName);
                }
                break;
            }
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
        }

        internal IEnumerable<string> GetObservedFieldNames()
        {
            if (_observedFields == null) yield break;
            foreach (var cached in _observedFields)
                yield return cached.Field.Name;
        }

        internal object GetObservedFieldValue(string fieldName)
        {
            if (_observedFields == null) return null;
            foreach (var cached in _observedFields)
                if (cached.Field.Name == fieldName)
                    return cached.ValueProperty?.GetValue(cached.Field.GetValue(this));
            return null;
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void SetupEditorResetHook()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode) CacheDefaultValues();
                else if (state == PlayModeStateChange.ExitingPlayMode) RestoreDefaultValues();
            };
        }

        private static void CacheDefaultValues()
        {
            _defaultValueCache.Clear();
            foreach (var so in Resources.FindObjectsOfTypeAll<ObservableObject>())
            {
                if (!_defaultValueCache.ContainsKey(so))
                    _defaultValueCache[so] = new();

                foreach (var cached in so._observedFields ?? new List<CachedFieldInfo>())
                {
                    var obs = cached.Field.GetValue(so);
                    var val = cached.ValueProperty?.GetValue(obs);
                    _defaultValueCache[so][cached.Field.Name] = val;
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

                    bool shouldPersist = (bool)(cached.ShouldPersistField.GetValue(cached.Field.GetValue(so)) ?? false);

                    if (!shouldPersist && _defaultValueCache[so].TryGetValue(cached.Field.Name, out var val))
                    {
                        var obs = cached.Field.GetValue(so);
                        ObservablePlayModeGuard.Suppress = true;
                        cached.ValueProperty?.SetValue(obs, val);
                        ObservablePlayModeGuard.Suppress = false;
                    }
                }
            }
            _defaultValueCache.Clear();
        }
#endif

        private string GetSavePath()
        {
#if UNITY_EDITOR
            return EditorApplication.isPlaying
                ? Path.Combine(Application.persistentDataPath, name + "_state.json")
                : Path.Combine("Temp", name + "_snapshot.json");
#else
            return Path.Combine(Application.persistentDataPath, name + "_state.json");
#endif
        }

        public void SaveStateToJson(bool log = true)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    Debug.LogError($"[ReaCS] {GetType().Name} has no name — cannot save. Use the factory or set .name before saving.");
                    return;
                }

                var path = GetSavePath();
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonUtility.ToJson(this);
                File.WriteAllText(path, json);

                PlayerPrefs.SetInt(name + "_snapshot_version", snapshotVersion);
                PlayerPrefs.Save();

#if UNITY_EDITOR
                if (log) Debug.Log($"[ReaCS] Saved '{name}' to:\n{path}");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReaCS] Failed to save '{name}': {ex}");
            }
        }

        private static bool _isLoadingJson = false;

        public void LoadStateFromJson(bool log = true)
        {
            if (_isLoadingJson) return;
            _isLoadingJson = true;

            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    if (log) Debug.LogWarning($"[ReaCS] {GetType().Name} has no name on load — skipping.");
                    return;
                }

                string path = GetSavePath();
                if (!File.Exists(path))
                {
#if UNITY_EDITOR
                    if (log) Debug.Log($"[ReaCS] No save found for '{name}' at {path}");
#endif
                    return;
                }

                string versionKey = name + "_snapshot_version";
                int savedVersion = PlayerPrefs.GetInt(versionKey, -1);
                if (savedVersion != snapshotVersion)
                {
                    if (log) Debug.Log($"[ReaCS] Snapshot invalidated for {name} (v{savedVersion} → v{snapshotVersion}). Deleting.");
                    File.Delete(path);
                    PlayerPrefs.SetInt(versionKey, snapshotVersion);
                    PlayerPrefs.Save();
                    return;
                }

                var json = File.ReadAllText(path);
                var clone = ScriptableObject.CreateInstance(GetType()) as ObservableObject;
                if (clone == null) return;

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

                ObservableRegistry.Unregister(clone);
#if UNITY_EDITOR
                DestroyImmediate(clone);
#else
        Destroy(clone);
#endif

#if UNITY_EDITOR
                if (log) Debug.Log($"[ReaCS] Loaded '{name}' from:\n{path}");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReaCS] Failed to load '{name}': {ex}");
            }
            finally
            {
                _isLoadingJson = false;
            }
        }


#if UNITY_EDITOR
        public void BumpSnapshotVersion()
        {
            snapshotVersion++;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
#endif

        private class CachedFieldInfo
        {
            public FieldInfo Field;
            public PropertyInfo ValueProperty;
            public FieldInfo ShouldPersistField;
        }
    }
}
