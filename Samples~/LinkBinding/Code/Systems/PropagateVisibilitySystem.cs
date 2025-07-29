using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using UnityEngine;

[ReactTo(nameof(MainObjectData.isVisible))]
public class PropagateVisibilitySystem : SystemBase<MainObjectData>
{
    protected override void OnFieldChanged(MainObjectData changedSO)
    {
        foreach (var link in Access.Query<LinkSORegistry>().FindLinksFrom<MainObjectData, ObjectVisibilityData>(changedSO))
        {
            Debug.Log(link.name);
            link.RightSO.Value.isVisible.Value = changedSO.isVisible.Value;
        }
    }
}
