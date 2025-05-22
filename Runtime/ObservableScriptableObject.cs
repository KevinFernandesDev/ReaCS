using ReaCS.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using ReaCS.Shared;

namespace ReaCS.Runtime
{
    public abstract class ObservableScriptableObject : ScriptableObject
    {
        public event Action<ObservableScriptableObject, string> OnChanged;

        private Dictionary<string, object> _cachedValues;
        private List<FieldInfo> _observedFields;
        private bool _isDirty = false;
        private string lastChangedField;

        protected virtual void OnEnable()
        {
            ObservableRegistry.Register(this);
            ObservableRuntimeWatcher.Register(this);

            CacheInitialValues();

            var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (!Attribute.IsDefined(field, typeof(ObservableAttribute)) &&
                    !Attribute.IsDefined(field, typeof(ObservableSavedAttribute))) continue;

                var value = field.GetValue(this);
                if (value is IInitializableObservable initObs)
                {
                    initObs.Init(this, field.Name);
                }
            }

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

        internal void CacheInitialValues()
        {
            _observedFields = new List<FieldInfo>();
            _cachedValues = new Dictionary<string, object>();

            foreach (var field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (Attribute.IsDefined(field, typeof(ObservableAttribute)) ||
                    Attribute.IsDefined(field, typeof(ObservableSavedAttribute)))
                {
                    var fieldValue = field.GetValue(this);

                    if (fieldValue == null)
                    {
                        var observableType = field.FieldType;
                        fieldValue = Activator.CreateInstance(observableType);
                        field.SetValue(this, fieldValue);
                    }

                    var method = field.FieldType.GetMethod("Init");
                    method?.Invoke(fieldValue, new object[] { this, field.Name });

                    _observedFields.Add(field);
                }
            }

            foreach (var field in _observedFields)
            {
                var observable = field.GetValue(this);
                var valueProp = field.FieldType.GetProperty("Value");
                var value = valueProp?.GetValue(observable);
                _cachedValues[field.Name] = value;
            }
        }

        internal void CheckForChanges()
        {

            if (_observedFields == null || _cachedValues == null)
            {
                ReaCSDebug.LogWarning($"[ReaCS] Skipping CheckForChanges on {name} — not fully initialized.");
                return;
            }

            ReaCSDebug.Log($"[{name}] CheckForChanges called (isDirty = {_isDirty})");

            if (!_isDirty)
            {
                ReaCSDebug.Log($"[{name}] Not dirty — skipping");
                return;
            }

            foreach (var field in _observedFields)
            {
                if (field.Name != lastChangedField) continue;

                var observable = field.GetValue(this);
                var valueProp = field.FieldType.GetProperty("Value");
                var newValue = valueProp?.GetValue(observable);

                if (_cachedValues.TryGetValue(field.Name, out var oldValue))
                {
                    ReaCSDebug.Log($"[{name}] Comparing {field.Name}: old={oldValue}, new={newValue}");
                }
                else
                {
                    ReaCSDebug.Log($"[{name}] No previous value for {field.Name}. Treating as changed.");
                }

                if (!_cachedValues.TryGetValue(field.Name, out var cachedValue) || !Equals(cachedValue, newValue))
                {
                    ReaCSDebug.Log($"[{name}] Field changed: {field.Name}");
                    ReaCSDebug.Log($"[{name}] Checking field {field.Name}: cached={cachedValue}, current={newValue}");

                    _cachedValues[field.Name] = newValue;
                    OnChanged?.Invoke(this, field.Name);
                    break;
                }
            }

            _isDirty = false;
        }
        public void MarkDirty(string fieldName)
        {
            lastChangedField = fieldName;
            _isDirty = true;
            CheckForChanges();
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

        /// <summary>
        /// Loads values for Observable fields from a saved JSON snapshot.
        /// 
        /// In runtime builds:
        ///     - Fields marked ShouldPersist = true will be loaded from disk.
        ///     - Fields with ShouldPersist = false will retain their inspector values.
        ///
        /// In the Unity Editor:
        ///     - Fields marked ShouldPersist = true will keep their inspector values (Serialized fields just work).
        ///     - Fields with ShouldPersist = false will be overwritten by the saved runtime values (preview mode).
        /// </summary>
        public void LoadStateFromJson()
        {
            string path = GetSavePath();
            if (!File.Exists(path)) return;

            var json = File.ReadAllText(path);
            var clone = CreateInstance(GetType()) as ObservableScriptableObject;
            JsonUtility.FromJsonOverwrite(json, clone);

            foreach (var field in _observedFields)
            {
                var targetObs = field.GetValue(this);
                var sourceObs = field.GetValue(clone);

                if (targetObs == null || sourceObs == null)
                    continue;

                var shouldPersistField = field.FieldType.GetField("ShouldPersist", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var valueProp = field.FieldType.GetProperty("Value");

                if (shouldPersistField == null || valueProp == null)
                    continue;

                bool shouldPersist = (bool)(shouldPersistField.GetValue(targetObs) ?? false);

#if UNITY_EDITOR
                // In Editor: skip loading if marked to persist inspector value
                if (shouldPersist)
                    continue;
#else
        // At runtime: skip loading if NOT marked to persist
        if (!shouldPersist)
            continue;
#endif

                var value = valueProp.GetValue(sourceObs);
                valueProp.SetValue(targetObs, value);
            }

            ObservableRuntimeWatcher.Unregister(clone);
            ObservableRegistry.Unregister(clone);

#if UNITY_EDITOR
            DestroyImmediate(clone);
#else
    Destroy(clone);
#endif
        }
    }
}
