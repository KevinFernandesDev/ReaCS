using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;

namespace ReaCS.Runtime.Registries
{
    /// <summary>
    /// ObservableRegistry is useful for live editor tooling and system subscription (like in SystemBase<TSO>).
    /// It's mostly used for having an index of edit-time SO population.
    /// It handles system subscriptions to know when a field changed, and is heavily used by the graph window. 
    /// </summary>
    public static class ObservableRegistry
    {
        private static readonly Dictionary<Type, List<ObservableObject>> _instances = new();
        public static event Action<ObservableObject> OnRegistered;
        public static event Action<ObservableObject> OnUnregistered;

#if UNITY_EDITOR
        public static Action<string, string> OnEditorFieldChanged = delegate { };

#endif

        public static void Register(ObservableObject so)
        {
/*#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#else
            if (!Application.isPlaying) return;
#endif*/
            var type = so.GetType();
            if (!_instances.TryGetValue(type, out var list))
            {
                list = new List<ObservableObject>();
                _instances[type] = list;
            }

            if (!list.Contains(so))
            {
                list.Add(so);
                OnRegistered?.Invoke(so);
            }
        }

        public static void Unregister(ObservableObject so)
        {
/*#if UNITY_EDITOR
            if (!Application.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#else
            if (!Application.isPlaying) return;
#endif*/
            var type = so.GetType();
            if (_instances.TryGetValue(type, out var list))
            {
                if (list.Remove(so))
                    OnUnregistered?.Invoke(so);

                if (list.Count == 0)
                    _instances.Remove(type);
            }
        }

        public static IReadOnlyList<T> GetAll<T>() where T : ObservableObject
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
