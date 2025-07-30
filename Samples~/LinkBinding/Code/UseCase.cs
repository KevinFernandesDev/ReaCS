using ReaCS.Runtime;
using ReaCS.Runtime.Services;
using UnityEngine;

public class LinkBindingUseCase : MonoBehaviour
{
    MainObjectData pooledSO;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pooledSO = Access.Use<PoolService<MainObjectData>>().Get();
        pooledSO.name = "New Main Object";

        // BindAll<TSOParent, TSOChild (pooled, per object), Unity component to find, The binding MonoBehaviour to add, the LinkSO type for the relationship>
        Access.Use<LinkBindingService>().BindAll<MainObjectData, ObjectVisibilityData, MeshRenderer, MeshVisibilityComponentBinding, ObjectVisibilityLinkData>(gameObject, pooledSO);
    }

    private void OnDestroy()
    {
        Access.Use<PoolService<MainObjectData>>().Release(pooledSO);
    }

    public void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            pooledSO.isVisible.Value = !pooledSO.isVisible.Value;
        }
    }
}