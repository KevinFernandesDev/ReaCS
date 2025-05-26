using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;

namespace ReaCS.Runtime.Services
{
    /// <summary>
    /// Tracks ComponentDataBinding<T> instances for fast SO ➝ MonoBehaviour lookup at runtime.
    /// </summary>
    /// <typeparam name="TSO">The ObservableScriptableObject type associated with the binding.</typeparam>
    public class ComponentDataBindingService<TSO> : IReaCSService where TSO : ObservableScriptableObject, new()
    {
        private readonly Dictionary<TSO, ComponentDataBinding<TSO>> _map = new();
        private readonly Dictionary<int, List<ComponentDataBinding<TSO>>> _byEntity = new();

        public void Register(TSO so, ComponentDataBinding<TSO> binding)
        {
            _map[so] = binding;

            if (so is IHasEntityId withId)
            {
                int id = withId.entityId.Value;
                if (!_byEntity.TryGetValue(id, out var list))
                    _byEntity[id] = list = new();
                list.Add(binding);
            }
        }

        public void Unregister(TSO so)
        {
            if (_map.TryGetValue(so, out var binding))
            {
                if (so is IHasEntityId withId)
                {
                    int id = withId.entityId.Value;
                    if (_byEntity.TryGetValue(id, out var list))
                        list.Remove(binding);
                }

                _map.Remove(so);
            }
        }

        public bool TryGetBinding(TSO so, out ComponentDataBinding<TSO> binding)
        {
            return _map.TryGetValue(so, out binding);
        }

        [Obsolete("Use ComponentDataBindingLookup<TSO> in runtime systems! This is for tools & inspectors")]
        public ComponentDataBinding<TSO> GetBinding(TSO so)
        {
            _map.TryGetValue(so, out var result);
            return result;
        }

        public IEnumerable<ComponentDataBinding<TSO>> GetAllBindings()
        {
            return _map.Values;
        }

        public List<ComponentDataBinding<TSO>> GetAllForEntity(int entityId)
        {
            if (_byEntity.TryGetValue(entityId, out var list))
                return list;
            return ListPool<ComponentDataBinding<TSO>>.Empty;
        }

        public List<ComponentDataBinding<TSO>> GetAllForEntity(EntityId entityId)
            => GetAllForEntity(entityId.Value);
    }

    /// <summary>
    /// Shared empty list for safe non-alloc fallback.
    /// </summary>
    internal static class ListPool<T>
    {
        public static readonly List<T> Empty = new();
    }


}
