using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ReaCS.Runtime.Services
{
    public class LinkedSOService : IReaCSService
    {
        private readonly Dictionary<Type, List<ScriptableObject>> _linkMap = new();

        public void Register(ScriptableObject link)
        {
            var type = link.GetType();
            if (!_linkMap.TryGetValue(type, out var list))
                _linkMap[type] = list = new();
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

        public IEnumerable<LinkedSO<TLeft, TRight>> GetLinks<TLeft, TRight>()
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
            => _linkMap.TryGetValue(typeof(LinkedSO<TLeft, TRight>), out var list)
                ? list.Cast<LinkedSO<TLeft, TRight>>()
                : Enumerable.Empty<LinkedSO<TLeft, TRight>>();
    }

}
