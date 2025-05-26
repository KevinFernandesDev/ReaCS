using System.Collections.Generic;
using UnityEngine;
using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Core;

namespace ReaCS.Runtime.Services
{
    /// <summary>
    /// Assigns and tracks a unique shared entityId for each object hierarchy containing an Entity MonoBehaviour.
    /// </summary>
    public class SharedEntityIdService : IReaCSService
    {
        private static int _nextEntityId = 1;
        private readonly Dictionary<Transform, int> _cache = new();

        public int GetOrAssignEntityId(Transform context)
        {
            var root = FindEntityRoot(context);
            if (_cache.TryGetValue(root, out var id))
                return id;

            id = _nextEntityId++;
            _cache[root] = id;
            return id;
        }

        private Transform FindEntityRoot(Transform context)
        {
            var current = context;
            while (current != null)
            {
                if (current.GetComponent<Entity>() != null)
                    return current;
                current = current.parent;
            }
            return context.root;
        }

        public void Reset()
        {
            _cache.Clear();
            _nextEntityId = 1;
        }
    }
}
