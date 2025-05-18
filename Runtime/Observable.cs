using System;
using UnityEngine;

[Serializable]
public class Observable<T>
{
    [SerializeField] private T value;
    [NonSerialized] private ObservableScriptableObject owner;
    [NonSerialized] private string fieldName;

    public event Action<T> OnChanged;

    public void Init(ObservableScriptableObject owner, string fieldName)
    {
        this.owner = owner;
        this.fieldName = fieldName;
    }

    public T Value
    {
        get => value;
        set
        {
            if (!Equals(this.value, value))
            {
                this.value = value;
                OnChanged?.Invoke(value);
                owner?.MarkDirty();
            }
        }
    }
}