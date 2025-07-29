using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ReaCS.Runtime.Services
{
    public class ComponentDataBindingRegistry<TSO> : IReaCSQuery
        where TSO : ObservableScriptableObject, new()
    {
        private readonly Dictionary<TSO, List<ComponentDataBinding<TSO>>> _map = new();
        private readonly Dictionary<int, List<ComponentDataBinding<TSO>>> _byEntity = new();
        private readonly Dictionary<Type, List<ComponentDataBinding<TSO>>> _byComponentType = new();

        public void Register(TSO so, ComponentDataBinding<TSO> binding)
        {
            // By SO
            if (!_map.TryGetValue(so, out var list))
                _map[so] = list = new();
            list.Add(binding);

            // By EntityId
            if (so is IHasEntityId withId)
            {
                int id = withId.entityId.Value;
                if (!_byEntity.TryGetValue(id, out var byEntityList))
                    _byEntity[id] = byEntityList = new();
                byEntityList.Add(binding);
            }

            // By TUC
            var bindingType = binding.GetType();
            while (bindingType != null && bindingType != typeof(ComponentDataBinding<TSO>))
            {
                if (bindingType.IsGenericType &&
                    bindingType.GetGenericTypeDefinition() == typeof(ComponentDataBinding<,>))
                {
                    var tucType = bindingType.GetGenericArguments()[1];
                    if (!_byComponentType.TryGetValue(tucType, out var typedList))
                        _byComponentType[tucType] = typedList = new();
                    typedList.Add(binding);
                    break;
                }

                bindingType = bindingType.BaseType;
            }
        }

        public void Unregister(TSO so)
        {
            if (_map.TryGetValue(so, out var list))
            {
                list.RemoveAll(b => b == null || b.data == so);
                if (list.Count == 0)
                    _map.Remove(so);
            }

            if (so is IHasEntityId withId)
            {
                int id = withId.entityId.Value;
                if (_byEntity.TryGetValue(id, out var byEntityList))
                {
                    byEntityList.RemoveAll(b => b == null || b.data == so);
                    if (byEntityList.Count == 0)
                        _byEntity.Remove(id);
                }
            }

            foreach (var typedList in _byComponentType.Values)
                typedList.RemoveAll(b => b == null || b.data == so);
        }

        public bool TryGetBinding(TSO so, out ComponentDataBinding<TSO> binding)
        {
            if (_map.TryGetValue(so, out var list) && list.Count > 0)
            {
                binding = list[0];
                return true;
            }

            binding = null;
            return false;
        }

        public bool TryGetUnityComponent<TUC>(TSO so, out TUC component)
            where TUC : Component
        {
            component = null;
            if (_map.TryGetValue(so, out var list))
            {
                foreach (var binding in list)
                {
                    if (binding is ComponentDataBinding<TSO, TUC> typed)
                    {
                        component = typed.uc;
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<TUC> GetAllUnityComponents<TUC>(TSO so)
            where TUC : Component
        {
            if (_map.TryGetValue(so, out var list))
            {
                foreach (var binding in list)
                {
                    if (binding is ComponentDataBinding<TSO, TUC> typed)
                        yield return typed.uc;
                }
            }
        }

        [Obsolete("Use ComponentDataBindingLookup<TSO> in runtime systems! This is for tools & inspectors")]
        public ComponentDataBinding<TSO> GetBinding(TSO so)
        {
            if (_map.TryGetValue(so, out var list) && list.Count > 0)
                return list[0];
            return null;
        }

        public IEnumerable<ComponentDataBinding<TSO>> GetAllBindings()
        {
            foreach (var list in _map.Values)
            {
                foreach (var b in list)
                    yield return b;
            }
        }

        public List<ComponentDataBinding<TSO>> GetAllForEntity(int entityId)
        {
            if (_byEntity.TryGetValue(entityId, out var list))
                return list;
            return ListPool<ComponentDataBinding<TSO>>.Empty;
        }

        public List<ComponentDataBinding<TSO>> GetAllForEntity(ReaCSEntityId entityId)
            => GetAllForEntity(entityId.Value);

        public IEnumerable<ComponentDataBinding<TSO, TUC>> GetTypedBindings<TUC>() where TUC : Component
        {
            if (_byComponentType.TryGetValue(typeof(TUC), out var list))
            {
                foreach (var b in list)
                {
                    if (b is ComponentDataBinding<TSO, TUC> typed)
                        yield return typed;
                }
            }
        }

        public IEnumerable<TUC> GetTypedUnityComponents<TUC>() where TUC : Component
        {
            foreach (var b in GetTypedBindings<TUC>())
                yield return b.uc;
        }

        public IEnumerable<ComponentDataBinding<TSO, TUC>> GetTypedBindingsFor<TUC>(TSO target) where TUC : Component
        {
            foreach (var so in GetTypedBindings<TUC>())
            {
                if (so.data == target)
                    yield return so;
            }
        }
    }

    internal static class ListPool<T>
    {
        public static readonly List<T> Empty = new();
    }
}
