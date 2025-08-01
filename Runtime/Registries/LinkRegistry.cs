using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReaCS.Runtime.Registries
{
    public class LinkRegistry : Registry<LinkBase>, IDisposable
    {
        // Maps generic link type (Link<TLeft, TRight>) to all instances
        private readonly Dictionary<Type, List<LinkBase>> _linkMap = new();

        public override void Register(LinkBase link)
        {
            //base.Register(link);

            // Find the generic Link<TLeft, TRight> base
            var linkType = link.GetType().BaseType;
            if (linkType == null || !linkType.IsGenericType || linkType.GetGenericTypeDefinition() != typeof(Link<,>))
            {
                Debug.LogWarning($"[LinkRegistry] Tried to register invalid link: {link.name}");
                return;
            }

            if (!_linkMap.TryGetValue(linkType, out var list))
                _linkMap[linkType] = list = new();

            if (!list.Contains(link))
                list.Add(link);
        }

        public override void Unregister(LinkBase link)
        {
            base.Unregister(link);

            var linkType = link.GetType().BaseType;
            if (linkType != null && _linkMap.TryGetValue(linkType, out var list))
                list.Remove(link);
        }

        /// <summary>
        /// Gets all links of a concrete Link<TLeft, TRight> type.
        /// </summary>
        public IEnumerable<Link<TLeft, TRight>> GetLinks<TLeft, TRight>()
            where TLeft : ObservableObject
            where TRight : ObservableObject
            => _linkMap.TryGetValue(typeof(Link<TLeft, TRight>), out var list)
                ? list.Cast<Link<TLeft, TRight>>()
                : Enumerable.Empty<Link<TLeft, TRight>>();

        /// <summary>
        /// Gets all links where either endpoint matches the given ObservableObject.
        /// </summary>
        public IEnumerable<LinkBase> GetAllLinksInvolving(ObservableObject oso)
        {
            foreach (var list in _linkMap.Values)
            {
                foreach (var link in list)
                {
                    if (link.Left == oso || link.Right == oso)
                        yield return link;
                }
            }
        }

        /// <summary>
        /// Gets all links of a specific runtime type (non-generic, slower, editor-only).
        /// </summary>
        public IEnumerable<LinkBase> GetAllLinksOfType(Type linkType)
        {
            if (linkType == null) yield break;

            foreach (var list in _linkMap.Values)
            {
                foreach (var link in list)
                {
                    if (link.GetType() == linkType)
                        yield return link;
                }
            }
        }

        /// <summary>
        /// Finds all links from a specific left endpoint.
        /// </summary>
        public IEnumerable<Link<TLeft, TRight>> FindLinksFrom<TLeft, TRight>(TLeft left)
            where TLeft : ObservableObject
            where TRight : ObservableObject
            => GetLinks<TLeft, TRight>().Where(link => link.LeftSO.Value == left);

        /// <summary>
        /// Finds all links to a specific right endpoint.
        /// </summary>
        public IEnumerable<Link<TLeft, TRight>> FindLinksTo<TLeft, TRight>(TRight right)
            where TLeft : ObservableObject
            where TRight : ObservableObject
            => GetLinks<TLeft, TRight>().Where(link => link.RightSO.Value == right);

        /// <summary>
        /// Counts links for a given ObservableObject.
        /// </summary>
        public int CountLinksFor(ObservableObject oso)
            => GetAllLinksInvolving(oso).Count();

        public void Dispose()
        {
            Debug.Log("[LinkRegistry] Disposing and clearing link map.");
            _linkMap.Clear();
            Clear();
        }
    }
}
