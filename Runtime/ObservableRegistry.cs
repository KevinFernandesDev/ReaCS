using System;
using System.Collections.Generic;

namespace ReaCS.Runtime
{
    public static class ObservableRegistry
    {
        private static readonly Dictionary<Type, List<ObservableScriptableObject>> _instances = new();
        public static event Action<ObservableScriptableObject> OnRegistered;
        public static event Action<ObservableScriptableObject> OnUnregistered;

        

        

        public static void Register(ObservableScriptableObject so)
        {
            var type = so.GetType();
            if (!_instances.TryGetValue(type, out var list))
            {
                list = new List<ObservableScriptableObject>();
                _instances[type] = list;
            }

            if (!list.Contains(so))
            {
                list.Add(so);
                OnRegistered?.Invoke(so); // 🔥 Notify
            }
        }

        public static void Unregister(ObservableScriptableObject so)
        {
            var type = so.GetType();
            if (_instances.TryGetValue(type, out var list))
            {
                if (list.Remove(so))
                    OnUnregistered?.Invoke(so); // 🔥 Notify

                if (list.Count == 0)
                    _instances.Remove(type);
            }
        }

        public static IReadOnlyList<T> GetAll<T>() where T : ObservableScriptableObject
        {
            if (_instances.TryGetValue(typeof(T), out var list))
                return list.ConvertAll(x => (T)x);
            return Array.Empty<T>();
        }

#if UNITY_EDITOR
        public static void ClearAll()
        {
            _instances.Clear();
        }
#endif
    }
}
