using ReaCS.Runtime.Internal;
using ReaCS.Shared;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime
{
    public abstract class SystemBase<TSO> : MonoBehaviour
        where TSO : ObservableScriptableObject
    {
        private string _observedField;
        private bool _subscribed = false;

        // Utility: active scene check for runtime filtering
        private bool IsInActiveScene()
        {
            return gameObject.scene.IsValid() && gameObject.activeInHierarchy;
        }

#if UNITY_EDITOR
        protected virtual void OnEnable()
        {
            if (Application.isPlaying) return; // Ignore during playmode
            _observedField = ResolveObservedField();

            ObservableRegistry.OnRegistered += HandleNewSO;
            ObservableRegistry.OnUnregistered += HandleRemovedSO;
        }

        protected virtual void OnDisable()
        {
            if (Application.isPlaying) return;
            ObservableRegistry.OnRegistered -= HandleNewSO;
            ObservableRegistry.OnUnregistered -= HandleRemovedSO;
        }
#endif

        // Runtime init — Start is guaranteed to run after all OnEnables
        protected virtual void Start()
        {
            if (!Application.isPlaying) return;
            if (!IsInActiveScene()) return;

            _observedField = ResolveObservedField();
            if (string.IsNullOrEmpty(_observedField))
            {
                ReaCSDebug.LogWarning($"[ReaCS] {GetType().Name} is missing a valid [ReactTo] attribute.");
                return;
            }

            ObservableRegistry.OnRegistered += HandleNewSO;
            ObservableRegistry.OnUnregistered += HandleRemovedSO;

            SubscribeAll();
        }

        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;

            ObservableRegistry.OnRegistered -= HandleNewSO;
            ObservableRegistry.OnUnregistered -= HandleRemovedSO;

            UnsubscribeAll();
            _subscribed = false;
            ReaCSDebug.Log($"[ReaCS] {GetType().Name} unsubscribed from all.");
        }

        private void HandleNewSO(ObservableScriptableObject so)
        {
            if (!Application.isPlaying) return;
            if (!IsInActiveScene()) return;

            if (so is TSO typed && IsTarget(typed))
            {
                typed.OnChanged -= HandleChange;
                typed.OnChanged += HandleChange;
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} late-subscribed to {typed.name}.{_observedField}");
            }
        }

        private void HandleRemovedSO(ObservableScriptableObject so)
        {
            if (!Application.isPlaying) return;
            if (!IsInActiveScene()) return;

            if (so is TSO typed && IsTarget(typed))
            {
                typed.OnChanged -= HandleChange;
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} unsubscribed from {typed.name}.{_observedField} (removed)");
            }
        }

        private void SubscribeAll()
        {
            if (!Application.isPlaying) return;
            if (!IsInActiveScene()) return;

            foreach (var so in ObservableRegistry.GetAll<TSO>())
            {
                if (IsTarget(so))
                {
                    so.OnChanged -= HandleChange;
                    so.OnChanged += HandleChange;
                    ReaCSDebug.Log($"[ReaCS] {GetType().Name} subscribed to {so.name}.{_observedField}");
                }
            }

            _subscribed = true;
        }

        private void UnsubscribeAll()
        {
            if (!Application.isPlaying) return;

            foreach (var so in ObservableRegistry.GetAll<TSO>())
            {
                if (IsTarget(so))
                {
                    so.OnChanged -= HandleChange;
                    ReaCSDebug.Log($"[ReaCS] {GetType().Name} unsubscribed from {so.name}.{_observedField}");
                }
            }
        }

        private void HandleChange(ObservableScriptableObject so, string fieldName)
        {
            if (!Application.isPlaying) return;

            if (fieldName == _observedField)
            {
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} triggered by {so.name}.{fieldName}");
                OnFieldChanged((TSO)so);
            }
        }

        protected abstract void OnFieldChanged(TSO changedSO);
        protected virtual bool IsTarget(TSO so) => true;

        private string ResolveObservedField()
        {
            var attr = GetType().GetCustomAttribute<ReactToAttribute>();
            return attr?.FieldName;
        }
    }
}
