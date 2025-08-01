using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Core;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    /// <summary>
    /// Pools Data instances for runtime efficiency.
    /// Tracks relationship to entities only via EntityRegistry (handled by EntityService).
    /// Usage: Access.Use<DataPool<MyDataType>>().Get(optionalEntityId);
    /// </summary>
    public class DataPool<T> : IReaCSService, IPool where T : Data, IPoolable
    {
        private readonly Stack<T> _pool = new();

        public T Get(EntityId? entityId = null)
        {
            var data = _pool.Count > 0
                ? _pool.Pop()
                : ScriptableObject.CreateInstance<T>();

            data.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            data.SetPool(this);
            data.Initialize();

            // Only register relationship—actual mapping/tracking is done by EntityRegistry via EntityService
            if (entityId.HasValue)
                Access.Use<EntityService>().RegisterSO(entityId.Value, data);

            return data;
        }

        public void Release(IPoolable obj)
        {
            if (obj is T t)
                _pool.Push(t);
        }

        // for direct releases if ever needed
        public void Release(T data)
        {
            data.Release(); // Calls Release(IPoolable)
        }

        public void Clear() => _pool.Clear();
        public int Count => _pool.Count;
    }
}
