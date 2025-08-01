using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Services;
using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Registries;

namespace ReaCS.Runtime.Registries
{
    public class UnifiedObservableRegistry : IReaCSQuery
    {
        private readonly IndexRegistry _index = Query<IndexRegistry>();

        public IEnumerable<T> GetAllRuntime<T>() where T : ObservableObject
            => _index.GetAll<T>();

        public IReadOnlyList<T> GetAllEditorTracked<T>() where T : ObservableObject
            => ObservableRegistry.GetAll<T>();

        public IEnumerable<T> GetAll<T>() where T : ObservableObject
        {
#if UNITY_EDITOR
            return ObservableRegistry.GetAll<T>();
#else
            return _index.GetAll<T>();
#endif
        }

        /*public IEnumerable<T> GetByEntity<T>(int entityId) where T : ObservableObject, new()
        {
            var bindings = Query<DataAdapterRegistry<T>>().GetAllForEntity(entityId);
            for (int i = 0; i < bindings.Count; i++)
                yield return bindings[i].data;
        }

        public IEnumerable<TBinding> GetBindingsForEntity<T, TBinding>(Core.EntityId id)
            where T : ObservableObject, new()
            where TBinding : DataAdapterBase<T>
        {
            var bindings = Query<DataAdapterRegistry<T>>().GetAllForEntity(id);
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i] is TBinding match)
                    yield return match;
            }
        }*/

        public NativeArray<TField> BuildNativeLookup<TSO, TField>(Func<TSO, TField> selector, Allocator allocator)
            where TSO : ObservableObject
            where TField : struct
        {
#if UNITY_EDITOR
            throw new InvalidOperationException("BuildNativeLookup<T>() is not supported in editor mode. Use ReaCSIndexRegistry directly.");
#else
            return _index.ToNativeArrayOf<TSO, TField>(selector, allocator);
#endif
        }
    }
}
