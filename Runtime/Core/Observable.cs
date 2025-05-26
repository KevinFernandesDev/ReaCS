using ReaCS.Runtime.Internal;
using System;
using Unity.Collections;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class Observable<T> : IInitializableObservable
    {
        [SerializeField] public bool ShouldPersist = false;
        [SerializeField] private T value;

        [NonSerialized] private ObservableScriptableObject owner;
        [NonSerialized] private string fieldName;

        private static readonly bool _enableDebug =
#if UNITY_EDITOR
        true;
#else
        false;
#endif

        private static readonly bool _logHistory = true;

        private Action<T> _onChanged;
        public event Action<T> OnChanged
        {
            add => _onChanged += value;
            remove => _onChanged -= value;
        }

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
                    var oldValue = this.value;
                    this.value = value;

                    if (_logHistory && Application.isPlaying)
                        LogToBurstHistory(oldValue, value);

                    _onChanged?.Invoke(value);
                    owner?.MarkDirty(fieldName);

#if UNITY_EDITOR
                    ObservableRegistry.OnEditorFieldChanged?.Invoke(owner?.name ?? "null", fieldName);
#endif
                }
            }
        }


        private void LogToBurstHistory(T oldVal, T newVal)
        {
            ReaCSBurstHistory.Init(); // Ensures burst history is ready even after domain reload
            var so = new FixedString64Bytes(owner?.name ?? "null");
            var field = new FixedString64Bytes(fieldName);
            var sys = new FixedString64Bytes(SystemContext.ActiveSystemName ?? "Unknown");

            switch (oldVal)
            {
                case float fOld when newVal is float fNew:
                    ReaCSBurstHistory.LogFloat(so, field, fOld, fNew, sys);
                    return;
                case int iOld when newVal is int iNew:
                    ReaCSBurstHistory.LogInt(so, field, iOld, iNew, sys);
                    return;
                case bool bOld when newVal is bool bNew:
                    ReaCSBurstHistory.LogBool(so, field, bOld, bNew, sys);
                    return;
                case Vector2 v2Old when newVal is Vector2 v2New:
                    ReaCSBurstHistory.LogVector2(so, field, v2Old, v2New, sys);
                    return;
                case Vector3 v3Old when newVal is Vector3 v3New:
                    ReaCSBurstHistory.LogVector3(so, field, v3Old, v3New, sys);
                    return;
            }
        }
    }
}
