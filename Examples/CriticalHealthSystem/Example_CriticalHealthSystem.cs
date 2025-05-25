using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Examples
{
    [ReactTo(nameof(Example_HealthSO.isCritical))]
    public class CriticalHealthSystem : SystemBase<Example_HealthSO>
    {
        protected override void OnFieldChanged(Example_HealthSO changedSO)
        {
            if (changedSO.isCritical.Value)
                Debug.Log($"CRITICAL: {changedSO.name} needs healing!");
        }
    }
}  