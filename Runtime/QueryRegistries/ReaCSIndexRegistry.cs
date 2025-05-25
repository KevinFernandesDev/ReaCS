using Unity.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Runtime.Registries
{
    public class ReaCSIndexRegistry : IReaCSQuery
    {
        private readonly Dictionary<Type, List<ObservableScriptableObject>> activeByType = new();
        private readonly List<ScriptableObject> allLinks = new();

        public void Register(ObservableScriptableObject so)
        {
            var type = so.GetType();
            if (!activeByType.TryGetValue(type, out var list))
                activeByType[type] = list = new();
            list.Add(so);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LinkedSO<,>))
                allLinks.Add(so);
        }

        public void Unregister(ObservableScriptableObject so)
        {
            var type = so.GetType();
            if (activeByType.TryGetValue(type, out var list))
                list.Remove(so);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LinkedSO<,>))
                allLinks.Remove(so);
        }

        public IEnumerable<T> GetAll<T>() where T : ObservableScriptableObject
        {
            if (activeByType.TryGetValue(typeof(T), out var list))
                return list.Cast<T>();
            return Enumerable.Empty<T>();
        }

        public NativeArray<TField> ToNativeArrayOf<TSO, TField>(Func<TSO, TField> selector, Allocator allocator)
            where TSO : ObservableScriptableObject
            where TField : struct
        {
            if (!activeByType.TryGetValue(typeof(TSO), out var raw))
                return new NativeArray<TField>(0, allocator);

            var result = new NativeArray<TField>(raw.Count, allocator, NativeArrayOptions.UninitializedMemory);
            int count = 0;
            foreach (var entry in raw)
            {
                if (entry is TSO so)
                    result[count++] = selector(so);
            }
            return result;
        }

        public void ApplyNativeArrayTo<TSO, TField>(NativeArray<TField> native, Action<TSO, TField> apply)
            where TSO : ObservableScriptableObject
            where TField : struct
        {
            if (!activeByType.TryGetValue(typeof(TSO), out var list)) return;
            int count = Math.Min(native.Length, list.Count);
            for (int i = 0; i < count; i++)
            {
                if (list[i] is TSO so)
                    apply(so, native[i]);
            }
        }

        public IEnumerable<TRight> GetLinkedTarget<TLeft, TRight>(TLeft left)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            return allLinks.OfType<LinkedSO<TLeft, TRight>>()
                .Where(link => link.Left.Value == left)
                .Select(link => link.Right.Value);
        }

        public IEnumerable<TLeft> GetLinkedOwner<TLeft, TRight>(TRight right)
            where TLeft : ObservableScriptableObject
            where TRight : ObservableScriptableObject
        {
            return allLinks.OfType<LinkedSO<TLeft, TRight>>()
                .Where(link => link.Right.Value == right)
                .Select(link => link.Left.Value);
        }
    }
}