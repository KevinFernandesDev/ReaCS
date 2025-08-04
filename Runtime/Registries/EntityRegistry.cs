using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Registries
{
    /// <summary>
    /// Flat registry for entities and associated ScriptableObjects.
    /// Maps EntityId to Transform and tracks attached SOs.
    /// </summary>
    public sealed class EntityRegistry : IReaCSQuery, IDisposable
    {
        private readonly HashSet<EntityId> _allEntities = new();
        private readonly Dictionary<EntityId, Transform> _entityTransforms = new();
        private readonly Dictionary<EntityId, List<ObservableObject>> _entityObjects = new();

        public void RegisterEntity(EntityId id, Transform transform)
        {
            _allEntities.Add(id);
            if (transform != null)
                _entityTransforms[id] = transform;
            if (!_entityObjects.ContainsKey(id))
                _entityObjects[id] = new List<ObservableObject>();
        }

        public void UnregisterEntity(EntityId id)
        {
            _allEntities.Remove(id);
            _entityTransforms.Remove(id);
            _entityObjects.Remove(id);
        }

        public bool Exists(EntityId id) => _allEntities.Contains(id);

        public Transform GetTransform(EntityId id)
            => _entityTransforms.TryGetValue(id, out var t) ? t : null;

        public IEnumerable<EntityId> QueryAll() => _allEntities;

        public void RegisterSO(EntityId entityId, ObservableObject so)
        {
            if (!_entityObjects.TryGetValue(entityId, out var list))
                _entityObjects[entityId] = list = new List<ObservableObject>();
            list.Add(so);
        }

        public void UnregisterSO(EntityId entityId, ObservableObject so)
        {
            if (_entityObjects.TryGetValue(entityId, out var list))
            {
                list.Remove(so);
                if (list.Count == 0)
                    _entityObjects.Remove(entityId);
            }
        }

        public IEnumerable<ObservableObject> GetSOs(EntityId entityId)
            => _entityObjects.TryGetValue(entityId, out var list) ? list : System.Linq.Enumerable.Empty<ObservableObject>();

        public void Dispose()
        {
            Debug.Log("[EntityRegistry] Disposing and clearing link map.");
            _allEntities.Clear();
            _entityTransforms.Clear();
            _entityObjects.Clear();
        }
    }
}