using UnityEngine;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using static ReaCS.Runtime.Access;
using System;

namespace ReaCS.Runtime.Services
{
    public class LinkFactory : IReaCSService
    {
        /// <summary>
        /// Creates a pooled, registered LinkSO between two ObservableScriptableObjects.
        /// </summary>
        public LinkSO<TLeft, TRight> Get<TLeft, TRight>(TLeft left, TRight right, string name = null)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            if (left == null)
                throw new ArgumentNullException(nameof(left));
            if (right == null)
                throw new ArgumentNullException(nameof(right));

            var pool = Use<PoolService<LinkSO<TLeft, TRight>>>();
            var link = pool.GetLink<LinkSO<TLeft, TRight>, TLeft, TRight>(left, right);
            link.name = name ?? $"{left.name}_to_{right.name}";
            return link;
        }

        public void Release<TLink>(TLink link) where TLink : ObservableScriptableObject
        {
            Query<LinkSORegistry>().Unregister(link);
        }
    }
}
