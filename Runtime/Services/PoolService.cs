using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using System.Collections.Generic;
using UnityEngine;

namespace ReaCS.Runtime.Services
{
    public class PoolService<T> : IReaCSService where T : ObservableScriptableObject
    {
        private readonly Stack<T> _pool = new();

        public T Get()
        {
            var instance = _pool.Count > 0
                ? _pool.Pop()
                : ScriptableObject.CreateInstance<T>();

            // Auto-register with IndexRegistry
            Query<IndexRegistry>().Register(instance);
            return instance;
        }

        public void Release(T instance)
        {
            Query<IndexRegistry>().Unregister(instance);

            if (instance is ILinkResettable resettable)
                resettable.ClearLink();

            _pool.Push(instance);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public int Count => _pool.Count;

        // Optional helper for links
        public TLink Get<TLink, TLeft, TRight>(TLeft left, TRight right)
            where TLink : LinkSO<TLeft, TRight>, T
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            var link = Get() as TLink;
            link.LeftSO.Value = left;
            link.RightSO.Value = right;
            return link;
        }
    }
}
