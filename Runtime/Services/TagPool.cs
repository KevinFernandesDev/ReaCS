using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Core;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    /// <summary>
    /// Pools Tag instances for runtime efficiency.
    /// Tracks relationship to entities only via EntityRegistry (handled by EntityService).
    /// Usage: Access.Use<TagPool<MyTagType>>().Get(optionalEntityId);
    /// </summary>
    public class TagPool<T> : IReaCSService, IPool where T : Tag, IPoolable
    {
        private readonly Stack<T> _pool = new();

        public T Get(EntityId? entityId = null)
        {
            var tag = _pool.Count > 0
                ? _pool.Pop()
                : ScriptableObject.CreateInstance<T>();

            tag.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            tag.SetPool(this);
            tag.Initialize();

            // Register relationship (actual mapping is tracked by EntityRegistry via EntityService)
            if (entityId.HasValue)
                Access.Use<EntityService>().RegisterSO(entityId.Value, tag);

            return tag;
        }

        public void Release(IPoolable obj)
        {
            if (obj is T t)
                _pool.Push(t);
        }

        public void Release(T tag)
        {
            tag.Release(); // Calls Release(IPoolable)
        }

        public void Clear() => _pool.Clear();
        public int Count => _pool.Count;
    }
}
