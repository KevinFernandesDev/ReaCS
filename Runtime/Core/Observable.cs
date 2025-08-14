using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Properties;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    [Serializable]
    public class Observable<T> : ObservableBase, IInitializableObservable
    {
        [SerializeField] public bool ShouldPersist = false;

        [NonSerialized] private ObservableObject owner;
        [NonSerialized] private string fieldName;

        [SerializeField] private T _value;

        private int _lastHash;
        private int _fastHashValue;

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

        public void Init(ObservableObject owner, string fieldName)
        {
            this.owner = owner;
            this.fieldName = fieldName;
            _fastHashValue = ComputeValueHash(_value);
            _lastHash = _fastHashValue;

            // Register self with per-field runtime watcher
            if (Application.isPlaying)
                ObservableRuntimeWatcher.Register(this);
        }

        [CreateProperty]
        public T Value
        {
            get => _value;
            set
            {
#if UNITY_EDITOR
                if (ObservablePlayModeGuard.Suppress) { _value = value; return; }
#endif
                if (!AreEqual(_value, value))
                {
                    var old = _value;
                    _value = value;

                    _fastHashValue = ComputeValueHash(value);
                    _lastHash = _fastHashValue;

                    if (_logHistory && Application.isPlaying)
                        LogToBurstHistory(old, value);

                    _onChanged?.Invoke(value);

#if UNITY_EDITOR
                    ObservableRegistry.OnEditorFieldChanged?.Invoke(owner?.name ?? "null", fieldName);
#endif
                }
            }
        }

        internal static bool AreEqual<T>(T oldValue, T newValue)
        {
            if (typeof(T).IsGenericType)
            {
                var typeDef = typeof(T).GetGenericTypeDefinition();

                if (typeDef == typeof(List<>) || typeDef == typeof(HashSet<>))
                {
                    var oldEnum = ((System.Collections.IEnumerable)oldValue)?.Cast<object>().ToList();
                    var newEnum = ((System.Collections.IEnumerable)newValue)?.Cast<object>().ToList();
                    return oldEnum?.SequenceEqual(newEnum) ?? newEnum == null;
                }
            }

            return Equals(oldValue, newValue);
        }

        // ───────────────────────────────
        // ObservableBase Implementation
        // ───────────────────────────────
        public override int FastHashValue => _fastHashValue;

        public override void NotifyChanged()
        {
            _onChanged?.Invoke(_value);
#if UNITY_EDITOR
            ObservableRegistry.OnEditorFieldChanged?.Invoke(owner?.name ?? "null", fieldName);
#endif
        }

        // ───────────────────────────────
        // Helpers
        // ───────────────────────────────
        public void SyncFromBinding() => Value = _value;

        private static int ComputeValueHash(T val)
        {
            if (val is UnityEngine.Object unityObj)
                return unityObj ? unityObj.GetInstanceID() : 0;

            return val?.GetHashCode() ?? 0;
        }

        private void LogToBurstHistory(T oldVal, T newVal)
        {
            if (owner == null) return;
            ReaCSBurstHistory.Init();

            var so = new FixedString64Bytes(owner.name ?? "UnnamedSO");
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