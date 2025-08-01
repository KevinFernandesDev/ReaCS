using UnityEngine;
using System.Collections.Generic;
using ReaCS.Runtime.Core;
using EntityId = ReaCS.Runtime.Core.EntityId;

namespace ReaCS.Runtime.Services
{
    /// <summary>
    /// Pools instances of strongly-typed Link objects for runtime efficiency.
    /// Registers relationship to entities only via EntityRegistry (handled by EntityService).
    /// Usage: Access.Use<LinkPool<MyLinkType>>().Get(leftObj, rightObj, optionalEntityId);
    /// </summary>
    public class LinkPool<TLink> : IReaCSService, IPool where TLink : LinkBase, IPoolable, ILinkConnector
    {
        private readonly Stack<TLink> _pool = new();

        /// <summary>
        /// Gets a link instance, connects it, and (optionally) associates it with an EntityId for cleanup.
        /// right and left must be the right type, otherwise an ArgumentException will be thrown.
        /// </summary>
        public TLink Get(ObservableObject left, ObservableObject right, EntityId? entityId = null)
        {
            if (left == null || right == null)
                throw new System.ArgumentNullException("Left and right cannot be null!");

            // Optional: runtime type check
            var linkType = typeof(TLink);
            var genericArgs = linkType.BaseType.GetGenericArguments();
            var leftType = genericArgs[0];
            var rightType = genericArgs[1];

            if (!leftType.IsInstanceOfType(left))
                throw new System.ArgumentException($"Left must be of type {leftType.Name}, got {left.GetType().Name}");
            if (!rightType.IsInstanceOfType(right))
                throw new System.ArgumentException($"Right must be of type {rightType.Name}, got {right.GetType().Name}");

            var link = _pool.Count > 0
                ? _pool.Pop()
                : ScriptableObject.CreateInstance<TLink>();

            link.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            link.SetPool(this);                // Inject this pool for auto-release
            link.Connect(left, right);         // Assign endpoints
            link.Initialize();                 // Registry setup, etc.

            if (entityId.HasValue)
                Access.Use<EntityService>().RegisterSO(entityId.Value, link);

            return link;
        }

        /// <summary>
        /// Called by the object itself on Release().
        /// </summary>
        public void Release(IPoolable obj)
        {
            if (obj is TLink link)
            {
                if (link is ILinkResettable resettable)
                    resettable.ClearLink();
                _pool.Push(link);
            }
        }

        /// <summary>
        /// For manual pool release (optional).
        /// </summary>
        public void Release(TLink link)
        {
            link.Release(); // Calls Release(IPoolable) above
        }

        public void Clear() => _pool.Clear();
        public int Count => _pool.Count;
    }
}
