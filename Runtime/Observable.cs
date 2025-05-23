using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Internal.Debugging;
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
        [SerializeField] public bool ShouldPersist = false;

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
                ReaCSDebug.Log($"[Observable] Attempting to set {fieldName} to {value} (was {this.value})");

                if (!Equals(this.value, value))
                {
                    var oldValue = this.value;
                    ReaCSDebug.Log($"[Observable] Value changed from {this.value} to {value}");

                    this.value = value;

                    if (Application.isPlaying)
                    {
                        ReaCSHistory.Log(
                            owner,
                            fieldName,
                            oldValue,
                            value,
                            SystemContext.ActiveSystemName ?? "Editor Change"
                        );
                    }

                    OnChanged?.Invoke(value);
                    owner?.MarkDirty(fieldName);

#if UNITY_EDITOR
                    ObservableRegistry.OnEditorFieldChanged?.Invoke(owner?.name ?? "null", fieldName);
#endif
                }
            }
        }
    }
}