using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace ReaCS.Runtime.Services
{
    public class PoolService<T> : IReaCSService where T : ObservableObject
    {
        private readonly Stack<T> _pool = new();

        public T Get()
        {
            var instance = _pool.Count > 0
                ? _pool.Pop()
                : ScriptableObject.CreateInstance<T>();

            instance.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

            // Auto-register with IndexRegistry
            Query<IndexRegistry>().Register(instance);

            Use<SceneContextService>().Register(SceneManager.GetActiveScene().name, instance);
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
        public TLink GetLink<TLink, TLeft, TRight>(TLeft left, TRight right)
            where TLink : Link<TLeft, TRight>, T
            where TLeft : ObservableObject
            where TRight : ObservableObject
        {
            var link = Get() as TLink;
            link.SetLinks(left, right);

            return link;
        }
    }
}
