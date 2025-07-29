using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Services;
using UnityEngine;

[ReactTo(nameof(ObjectVisibilityData.isVisible))]
public class ApplyVisibilitySystem : SystemBase<ObjectVisibilityData>
{
    protected override void OnFieldChanged(ObjectVisibilityData changedSO)
    {
        foreach (var binding in Access.Query<ComponentDataBindingRegistry<ObjectVisibilityData>>().GetTypedBindingsFor<MeshRenderer>(changedSO))
        {
            if (binding.uc != null)
                binding.uc.enabled = changedSO.isVisible.Value;
        }
    }
}
