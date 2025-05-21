using ReaCS.Runtime.Internal;
using System;
using UnityEngine;

namespace ReaCS.Runtime
{
    public interface IInitializableObservable
    {
        void Init(ObservableScriptableObject owner, string fieldName);
    }

    [Serializable]
    public class Observable<T> : IInitializableObservable
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
                    ReaCSDebug.Log($"[Observable] Value changed from {this.value} to {value}");

                    this.value = value;
                    OnChanged?.Invoke(value);
                    owner?.MarkDirty();

#if UNITY_EDITOR
                    // Invoke field changed for Editor Graph View Live Debugging
                    ObservableEditorBridge.OnEditorFieldChanged?.Invoke(owner?.name ?? "null", fieldName);
#endif
                }
            }
        }
    }
}