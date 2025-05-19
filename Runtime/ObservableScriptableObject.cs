using ReaCS.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime
{
    public abstract class ObservableScriptableObject : ScriptableObject
    {
        public event Action<ObservableScriptableObject, string> OnChanged;

        private Dictionary<string, object> _cachedValues;
        private List<FieldInfo> _observedFields;
        private bool _isDirty = false;

        protected virtual void OnEnable()
        {
            ObservableRuntimeWatcher.Register(this);
            CacheInitialValues();
        }

        protected virtual void OnDisable()
        {
            ObservableRuntimeWatcher.Unregister(this);
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
                if (Attribute.IsDefined(field, typeof(ObservableAttribute)))
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

            // Delay caching until all fields are fully set up
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
            ReaCSDebug.Log($"[{name}] CheckForChanges called (isDirty = {_isDirty})");

            if (!_isDirty)
            {
                ReaCSDebug.Log($"[{name}] Not dirty — skipping");
                return;
            }

            foreach (var field in _observedFields)
            {
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

                    _cachedValues[field.Name] = newValue;
                    OnChanged?.Invoke(this, field.Name);
                    break;
                }
            }

            _isDirty = false;
        }


        public void MarkDirty()
        {
            ReaCSDebug.Log($"[{name}] Marked dirty.");

            _isDirty = true;

            ReaCSDebug.Log($"[{name}] Marked dirty (isDirty now = {_isDirty})");
        }
    }
}