using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using System.Linq;
using UnityEngine;
using EntityId = ReaCS.Runtime.Core.EntityId;

public sealed class EntityService : IReaCSService
{
    private int _nextId = 1;

    /// <summary>
    /// Creates a new EntityId and registers it with the given transform.
    /// </summary>
    public EntityId CreateEntityFor(Transform t)
    {
        var entity = t.GetComponent<Entity>();
        if (!entity)
            entity = t.gameObject.AddComponent<Entity>();

        var id = new EntityId(_nextId++);
        entity.entityId = id;

        Access.Query<EntityRegistry>().RegisterEntity(id, t);

        return id;
    }

    public void RegisterSO(EntityId entityId, ObservableObject so)
        => Access.Query<EntityRegistry>().RegisterSO(entityId, so);

    public void UnregisterSO(EntityId entityId, ObservableObject so)
        => Access.Query<EntityRegistry>().UnregisterSO(entityId, so);

    public void Release(EntityId id)
    {
        var registry = Access.Query<EntityRegistry>();

        foreach (var obj in registry.GetSOs(id).ToList())
            obj.Release();

        registry.UnregisterEntity(id);
    }

    public System.Collections.Generic.IEnumerable<ObservableObject> GetSOs(EntityId id)
        => Access.Query<EntityRegistry>().GetSOs(id);

    public Transform GetTransform(EntityId id)
        => Access.Query<EntityRegistry>().GetTransform(id);

    public bool Exists(EntityId id)
        => Access.Query<EntityRegistry>().Exists(id);
}
