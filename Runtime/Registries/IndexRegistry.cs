using Unity.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using ReaCS.Runtime.Core;
using UnityEngine;

namespace ReaCS.Runtime.Registries
{
    public class IndexRegistry : IReaCSQuery
    {
        private readonly Dictionary<Type, List<ObservableObject>> activeByType = new();
        private readonly List<ScriptableObject> allLinks = new();

        public void Register(ObservableObject so)
        {
            var type = so.GetType();
            if (!activeByType.TryGetValue(type, out var list))
                activeByType[type] = list = new();
            list.Add(so);

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Link<,>))
                allLinks.Add(so);
        }

        public void Unregister(ObservableObject so)
        {
            if (so == null) return;

            var type = so.GetType();

            // Remove from typed registry
            if (activeByType.TryGetValue(type, out var list))
                list.Remove(so);

            // If it's a link itself, remove directly from the allLinks list
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Link<,>))
            {
                allLinks.Remove(so);
                return;
            }

            // Otherwise, remove any links that reference this SO (Left or Right)
            allLinks.RemoveAll(link =>
            {
                var linkType = link.GetType();
                var leftField = linkType.GetField("Left");
                var rightField = linkType.GetField("Right");

                var left = leftField?.GetValue(link) as IObservableReference;
                var right = rightField?.GetValue(link) as IObservableReference;

                return left?.Value == so || right?.Value == so;
            });
        }

        public IEnumerable<T> GetAll<T>() where T : ObservableObject
        {
            if (activeByType.TryGetValue(typeof(T), out var list))
                return list.Cast<T>();
            return Enumerable.Empty<T>();
        }

        public NativeArray<TField> ToNativeArrayOf<TSO, TField>(Func<TSO, TField> selector, Allocator allocator)
            where TSO : ObservableObject
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
            where TSO : ObservableObject
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
            where TLeft : ObservableObject
            where TRight : ObservableObject
        {
            return allLinks.OfType<Link<TLeft, TRight>>()
                .Where(link => (TLeft)link.Left == left)
                .Select(link => (TRight)link.Right);
        }

        public IEnumerable<TLeft> GetLinkedOwner<TLeft, TRight>(TRight right)
            where TLeft : ObservableObject
            where TRight : ObservableObject
        {
            return allLinks.OfType<Link<TLeft, TRight>>()
                .Where(link => (TRight)link.Right == right)
                .Select(link => (TLeft)link.Left);
        }
    }
}