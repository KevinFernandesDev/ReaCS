using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime
{
    public abstract class ObservableScriptableObject : ScriptableObject
    {
        public event Action<ObservableScriptableObject, string> OnChanged;

        private static readonly Dictionary<Type, List<CachedFieldInfo>> _fieldCache = new();

        private List<CachedFieldInfo> _observedFields;
        private Dictionary<string, object> _cachedValues = new();

        private bool _isDirty = false;
        private string lastChangedField;

        protected virtual void OnEnable()
        {
            ObservableRegistry.Register(this);
            ObservableRuntimeWatcher.Register(this);

            InitializeFields();

#if !UNITY_EDITOR
            LoadStateFromJson();
#endif
        }

        protected virtual void OnDisable()
        {
            ObservableRegistry.Unregister(this);
            ObservableRuntimeWatcher.Unregister(this);

#if !UNITY_EDITOR
            SaveStateToJson();
#endif
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            CheckForChanges();
        }
#endif

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

        public void MarkDirty(string fieldName)
        {
            lastChangedField = fieldName;
            _isDirty = true;
            CheckForChanges();
        }

        internal void CheckForChanges()
        {
            if (_observedFields == null || _cachedValues == null)
            {
                ReaCSDebug.LogWarning($"[ReaCS] Skipping CheckForChanges on {name} — not fully initialized.");
                return;
            }

            if (!_isDirty) return;

            foreach (var cached in _observedFields)
            {
                if (cached.Field.Name != lastChangedField) continue;

                var obs = cached.Field.GetValue(this);
                var newValue = cached.ValueProperty?.GetValue(obs);

                if (!_cachedValues.TryGetValue(cached.Field.Name, out var oldValue) || !Equals(oldValue, newValue))
                {
                    _cachedValues[cached.Field.Name] = newValue;
                    OnChanged?.Invoke(this, cached.Field.Name);
                    break;
                }
            }

            _isDirty = false;
        }

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
            var clone = CreateInstance(GetType()) as ObservableScriptableObject;
            JsonUtility.FromJsonOverwrite(json, clone);

            foreach (var cached in _observedFields)
            {
                var field = cached.Field;

                var sourceObs = field.GetValue(clone);
                var targetObs = field.GetValue(this);

                if (sourceObs == null || targetObs == null) continue;

                bool shouldPersist = cached.ShouldPersistField != null && (bool)(cached.ShouldPersistField.GetValue(targetObs) ?? false);

#if UNITY_EDITOR
                if (shouldPersist) continue;
#else
                if (!shouldPersist) continue;
#endif

                var value = cached.ValueProperty?.GetValue(sourceObs);
                cached.ValueProperty?.SetValue(targetObs, value);
            }

            ObservableRuntimeWatcher.Unregister(clone);
            ObservableRegistry.Unregister(clone);

#if UNITY_EDITOR
            DestroyImmediate(clone);
#else
            Destroy(clone);
#endif
        }

        private class CachedFieldInfo
        {
            public FieldInfo Field;
            public PropertyInfo ValueProperty;
            public FieldInfo ShouldPersistField;
        }
    }
}
