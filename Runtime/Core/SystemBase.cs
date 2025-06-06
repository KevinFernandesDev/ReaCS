using ReaCS.Runtime.Internal;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime.Core
{
    public abstract class SystemBase<TSO> : MonoBehaviour
        where TSO : ObservableScriptableObject
    {
        private string _observedField;
        private bool _subscribed = false;

        // Utility: active scene check for runtime filtering
        private bool IsInActiveScene()
        {
            return this != null && gameObject != null && gameObject.scene.IsValid() && gameObject.activeInHierarchy;
        }


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ReconnectSystems()
        {
            foreach (var system in FindObjectsByType<SystemBase<TSO>>(FindObjectsSortMode.InstanceID))
            {
                system.Start();
            }
        }

#if UNITY_EDITOR
        protected virtual void OnEnable()
        {
            //Debug.Log($"[ReaCS][Editor] {GetType().Name} OnEnable triggered. isPlaying: {Application.isPlaying}");

            // Always run this in Editor for Graph support

            _observedField = ResolveObservedField();
            //Debug.Log($"[ReaCS] {GetType().Name} registering to observe field: {_observedField}");
            if (string.IsNullOrEmpty(_observedField))
            {
                //Debug.LogWarning($"[ReaCS] {GetType().Name} is missing a valid [ReactTo] attribute.");
            }

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

            StartCoroutine(DeferredSubscribe());
        }
        private IEnumerator DeferredSubscribe()
        {
            yield return null; // wait one frame
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
            if (this == null || !IsInActiveScene()) return;

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
            Debug.Log($"[SystemBase] {GetType().Name} handling change for {fieldName}");
            if (!Application.isPlaying) return;

            if (fieldName == _observedField)
            {
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} triggered by {so.name}.{fieldName}");
                ReaCSDebug.Log($"[SystemBase] Matching field! Pushing system context: {GetType().Name}");
                SystemContext.WithSystem(GetType().Name, () => OnFieldChanged((TSO)so));
            }
        }

        protected abstract void OnFieldChanged(TSO changedSO);
        protected virtual bool IsTarget(TSO so) => true;

        private string ResolveObservedField()
        {
            var attr = GetType().GetCustomAttribute<ReactToAttribute>();
            if (attr == null)
            {
                ReaCSDebug.LogWarning($"[ReaCS] {GetType().Name} is missing [ReactTo] attribute.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(attr.FieldName))
            {
                ReaCSDebug.LogWarning($"[ReaCS] {GetType().Name} has an empty field name in [ReactTo] attribute.");
            }
            else
            {
                ReaCSDebug.Log($"[ReaCS] {GetType().Name} is observing field: {attr.FieldName}");
            }

            return attr.FieldName;
        }

    }
}