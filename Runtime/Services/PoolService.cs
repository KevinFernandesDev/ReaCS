using ReaCS.Runtime.Core;
using System.Collections.Generic;
using UnityEngine;

namespace ReaCS.Runtime.Services
{
    public class PoolService<T> : IReaCSService where T : ObservableScriptableObject
    {
        private readonly Stack<T> _pool = new();

        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : ScriptableObject.CreateInstance<T>();
        }

        public void Release(T instance)
        {
            _pool.Push(instance);
        }

        public void Clear()
        {
            _pool.Clear();
        }

        public int Count => _pool.Count;
    }
}