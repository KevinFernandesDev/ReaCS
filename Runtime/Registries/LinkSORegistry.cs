using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReaCS.Runtime.Registries
{
    public class LinkSORegistry : IReaCSQuery
    {
        private readonly Dictionary<Type, List<ScriptableObject>> _linkMap = new();

        public void Register(ScriptableObject link)
        {
            var baseType = link.GetType().BaseType;
            if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != typeof(LinkSO<,>))
            {
                Debug.LogWarning($"[LinkSORegistry] Tried to register invalid link: {link.name}");
                return;
            }

            if (!_linkMap.TryGetValue(baseType, out var list))
                _linkMap[baseType] = list = new();
            list.Add(link);
        }

        public void Unregister(ScriptableObject link)
        {
            var type = link.GetType();
            if (_linkMap.TryGetValue(type, out var list))
                list.Remove(link);
        }

        public IEnumerable<ScriptableObject> GetAllLinksOfType(Type linkType)
            => _linkMap.TryGetValue(linkType, out var list) ? list : Enumerable.Empty<ScriptableObject>();

        public IEnumerable<LinkSO<TLeft, TRight>> GetLinks<TLeft, TRight>()
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
            => _linkMap.TryGetValue(typeof(LinkSO<TLeft, TRight>), out var list)
                ? list.Cast<LinkSO<TLeft, TRight>>()
                : Enumerable.Empty<LinkSO<TLeft, TRight>>();
        public IEnumerable<LinkSO> GetAllLinksInvolving(ObservableScriptableObject oso)
        {
            foreach (var list in _linkMap.Values)
            {
                foreach (var link in list)
                {
                    if (link is LinkSO casted &&
                        (casted.Left == oso || casted.Right == oso))
                    {
                        yield return casted;
                    }
                }
            }
        }

        public IEnumerable<LinkSO<TLeft, TRight>> FindLinksFrom<TLeft, TRight>(TLeft left)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            return GetLinks<TLeft, TRight>().Where(link => link.LeftSO.Value == left);
        }

        public IEnumerable<LinkSO<TLeft, TRight>> FindLinksTo<TLeft, TRight>(TRight right)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            return GetLinks<TLeft, TRight>().Where(link => link.RightSO.Value == right);
        }

        public int CountLinksFor(ObservableScriptableObject oso)
        {
            int count = 0;
            foreach (var list in _linkMap.Values)
            {
                foreach (var link in list)
                {
                    if (link is LinkSO casted &&
                        (casted.Left == oso || casted.Right == oso))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }

}
