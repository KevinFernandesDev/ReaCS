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

        protected virtual void OnEnable()
        {
            ReaCSDebug.Log($"[ReaCS] Enabling {GetType().Name}...");
#if UNITY_EDITOR
            if (!Application.isPlaying && !ReaCSSettings.EnableVisualGraphEditModeReactions)
            {
                ReaCSDebug.Log($"[ReaCS] Skipping {GetType().Name} — not in playmode and visual graph reactions disabled.");
                return;
            }
#endif
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

        protected virtual void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && !ReaCSSettings.EnableVisualGraphEditModeReactions)
            {
                ReaCSDebug.Log($"[ReaCS] Disabling {GetType().Name} — not in playmode and visual graph reactions disabled.");
                return;
            }
#endif
            ObservableRegistry.OnRegistered -= HandleNewSO;
            ObservableRegistry.OnUnregistered -= HandleRemovedSO;

            UnsubscribeAll();
            _subscribed = false;
            ReaCSDebug.Log($"[ReaCS] {GetType().Name} unsubscribed from all.");
        }

        private void HandleNewSO(ObservableScriptableObject so)
        {
            if (so is TSO typed && IsTarget(typed))
            {
                typed.OnChanged -= HandleChange;
                typed.OnChanged += HandleChange;
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} late-subscribed to {typed.name}.{_observedField}");
            }
        }

        private void HandleRemovedSO(ObservableScriptableObject so)
        {
            if (so is TSO typed && IsTarget(typed))
            {
                typed.OnChanged -= HandleChange;
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} unsubscribed from {typed.name}.{_observedField} (removed)");
            }
        }

        private void SubscribeAll()
        {
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
