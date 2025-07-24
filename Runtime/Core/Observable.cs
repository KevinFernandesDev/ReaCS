using ReaCS.Runtime.Internal;
using System;
using Unity.Collections;
using Unity.Properties;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class Observable<T> : IInitializableObservable
    {
        [SerializeField] public bool ShouldPersist = false;

        [NonSerialized] private ObservableScriptableObject owner;
        [NonSerialized] private string fieldName;
        [SerializeField] public T _value;

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

        [CreateProperty]
        public T Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    var oldValue = _value;
                    _value = value;

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

        public void SyncFromBinding()
        {
            Value = _value; // Triggers OnChanged, MarkDirty, history, etc.
        }


        private void LogToBurstHistory(T oldVal, T newVal)
        {
            ReaCSBurstHistory.Init();

            var so = new FixedString64Bytes(owner != null ? owner.name ?? "UnnamedSO" : "null");
            var field = new FixedString64Bytes(fieldName ?? "UnknownField");
            var sys = new FixedString64Bytes(SystemContext.ActiveSystemName ?? "UnknownSystem");

            switch (oldVal)
            {
                case float fOld when newVal is float fNew:
                    ReaCSBurstHistory.LogFloat(so, field, fOld, fNew, sys); break;

                case int iOld when newVal is int iNew:
                    ReaCSBurstHistory.LogInt(so, field, iOld, iNew, sys); break;

                case bool bOld when newVal is bool bNew:
                    ReaCSBurstHistory.LogBool(so, field, bOld, bNew, sys); break;

                case Vector2 v2Old when newVal is Vector2 v2New:
                    ReaCSBurstHistory.LogVector2(so, field, v2Old, v2New, sys); break;

                case Vector3 v3Old when newVal is Vector3 v3New:
                    ReaCSBurstHistory.LogVector3(so, field, v3Old, v3New, sys); break;

                case Vector4 v4Old when newVal is Vector4 v4New:
                    ReaCSBurstHistory.LogVector4(so, field, v4Old, v4New, sys); break;

                case Quaternion qOld when newVal is Quaternion qNew:
                    ReaCSBurstHistory.LogQuaternion(so, field, qOld, qNew, sys); break;

                case Color cOld when newVal is Color cNew:
                    ReaCSBurstHistory.LogColor(so, field, cOld, cNew, sys); break;

                case Enum eOld when newVal is Enum eNew:
                    ReaCSBurstHistory.LogEnum(so, field, eOld.ToString(), eNew.ToString(), sys); break;

                case string sOld when newVal is string sNew:
                    ReaCSBurstHistory.LogString(so, field, sOld ?? "null", sNew ?? "null", sys); break;

                default:
#if UNITY_EDITOR
                    if (_enableDebug)
                        Debug.LogWarning($"[Observable<T>] Unhandled history type: {typeof(T).Name} in {field}");
#endif
                    break;
            }
        }

    }
}