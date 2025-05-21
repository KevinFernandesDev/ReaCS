using ReaCS.Runtime.Internal;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ReaCS.Runtime
{
    public abstract class SystemBase<TSO> : MonoBehaviour
        where TSO : ObservableScriptableObject
    {
        private string _observedField;

        protected virtual void OnEnable()
        {
            StartCoroutine(DeferredSubscribe());
        }

        private IEnumerator DeferredSubscribe()
        {
            yield return new WaitForEndOfFrame(); // Wait for all SOs to register

            _observedField = ResolveObservedField();

            if (string.IsNullOrEmpty(_observedField))
            {
                Debug.LogWarning($"[ReaCS] {GetType().Name} is missing a valid [ReactTo] attribute.");
                yield break;
            }

            foreach (var so in ObservableRegistry.GetAll<TSO>())
            {
                if (IsTarget(so))
                {
                    so.OnChanged -= HandleChange; // Safety
                    so.OnChanged += HandleChange;
                }
            }
        }


        protected virtual void OnDisable()
        {
            if (string.IsNullOrEmpty(_observedField)) return;

            foreach (var so in ObservableRegistry.GetAll<TSO>())
            {
                if (IsTarget(so))
                    so.OnChanged -= HandleChange;
            }
        }

        private void TrySubscribe(ObservableScriptableObject obj)
        {
            if (obj is TSO so && IsTarget(so))
            {
                so.OnChanged -= HandleChange; // prevent double sub
                so.OnChanged += HandleChange;
            }
        }

        private void TryUnsubscribe(ObservableScriptableObject obj)
        {
            if (obj is TSO so && IsTarget(so))
                so.OnChanged -= HandleChange;
        }

        private void HandleChange(ObservableScriptableObject so, string fieldName)
        {
            if (fieldName == _observedField)
                OnFieldChanged((TSO)so);
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
