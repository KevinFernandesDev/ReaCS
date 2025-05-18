using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

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
                _observedFields.Add(field);
                _cachedValues[field.Name] = field.GetValue(this);

                var fieldValue = field.GetValue(this);
                var method = field.FieldType.GetMethod("Init");
                method?.Invoke(fieldValue, new object[] { this, field.Name });
            }
        }
    }

    internal void CheckForChanges()
    {
        if (!_isDirty) return;

        foreach (var field in _observedFields)
        {
            var newValue = field.GetValue(this);
            if (!_cachedValues.TryGetValue(field.Name, out var oldValue) || !Equals(oldValue, newValue))
            {
                _cachedValues[field.Name] = newValue;
                OnChanged?.Invoke(this, field.Name);
                break;
            }
        }

        _isDirty = false;
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }
}