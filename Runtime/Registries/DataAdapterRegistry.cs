using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Registries
{
    public class DataAdapterRegistry<TData> : IReaCSQuery
        where TData : Data, new()
    {
        private readonly Dictionary<TData, List<DataAdapterBase<TData>>> _map = new();
        private readonly Dictionary<EntityId, List<DataAdapterBase<TData>>> _byEntityId = new();
        private readonly Dictionary<Type, List<DataAdapterBase<TData>>> _byComponentType = new();

        public void Register(TData so, DataAdapterBase<TData> binding)
        {
            // By SO
            if (!_map.TryGetValue(so, out var list))
                _map[so] = list = new();
            list.Add(binding);

            // By EntityId (uses struct key, never int)
            EntityId eid = so.entityId;
            if (!_byEntityId.TryGetValue(eid, out var byEntityList))
                _byEntityId[eid] = byEntityList = new();
            byEntityList.Add(binding);

            // By TUC
            var bindingType = binding.GetType();
            while (bindingType != null && bindingType != typeof(DataAdapterBase<TData>))
            {
                if (bindingType.IsGenericType &&
                    bindingType.GetGenericTypeDefinition() == typeof(DataAdapter<,>))
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

        public void Unregister(TData so)
        {
            // By SO
            if (_map.TryGetValue(so, out var list))
            {
                list.RemoveAll(b => b == null || b.data == so);
                if (list.Count == 0)
                    _map.Remove(so);
            }

            // By EntityId
            EntityId eid = so.entityId;
            if (_byEntityId.TryGetValue(eid, out var byEntityList))
            {
                byEntityList.RemoveAll(b => b == null || b.data == so);
                if (byEntityList.Count == 0)
                    _byEntityId.Remove(eid);
            }

            // By TUC
            foreach (var typedList in _byComponentType.Values)
                typedList.RemoveAll(b => b == null || b.data == so);
        }

        public bool TryGetBinding(TData so, out DataAdapterBase<TData> binding)
        {
            if (_map.TryGetValue(so, out var list) && list.Count > 0)
            {
                binding = list[0];
                return true;
            }
            binding = null;
            return false;
        }

        public bool TryGetUnityComponent<TUC>(TData so, out TUC component)
            where TUC : Component
        {
            component = null;
            if (_map.TryGetValue(so, out var list))
            {
                foreach (var binding in list)
                {
                    if (binding is DataAdapter<TData, TUC> typed)
                    {
                        component = typed.component;
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<TUC> GetAllUnityComponents<TUC>(TData so)
            where TUC : Component
        {
            if (_map.TryGetValue(so, out var list))
            {
                foreach (var binding in list)
                {
                    if (binding is DataAdapter<TData, TUC> typed)
                        yield return typed.component;
                }
            }
        }

        [Obsolete("Use ComponentDataBindingLookup<TSO> in runtime systems! This is for tools & inspectors")]
        public DataAdapterBase<TData> GetBinding(TData so)
        {
            if (_map.TryGetValue(so, out var list) && list.Count > 0)
                return list[0];
            return null;
        }

        public IEnumerable<DataAdapterBase<TData>> GetAllDataAdapters()
        {
            foreach (var list in _map.Values)
                foreach (var b in list)
                    yield return b;
        }

        // The main method you want:
        public List<DataAdapterBase<TData>> GetAllForEntity(EntityId entityId)
        {
            if (_byEntityId.TryGetValue(entityId, out var list))
                return list;
            return ListPool<DataAdapterBase<TData>>.Empty;
        }

        public IEnumerable<DataAdapter<TData, TUC>> GetTypedBindings<TUC>() where TUC : Component
        {
            if (_byComponentType.TryGetValue(typeof(TUC), out var list))
            {
                foreach (var b in list)
                {
                    if (b is DataAdapter<TData, TUC> typed)
                        yield return typed;
                }
            }
        }

        public IEnumerable<TUC> GetTypedUnityComponents<TUC>() where TUC : Component
        {
            foreach (var b in GetTypedBindings<TUC>())
                yield return b.component;
        }

        public IEnumerable<DataAdapter<TData, TUC>> GetTypedBindingsFor<TUC>(TData target) where TUC : Component
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
