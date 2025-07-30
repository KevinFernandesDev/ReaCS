using ReaCS.Runtime.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ReaCS.Runtime
{
    public static class Access
    {
        public static T Query<T>() where T : IReaCSQuery, new() => QueryCache<T>.instance;
        public static T Use<T>() where T : IReaCSService, new() => UseCache<T>.instance;

        private static readonly Dictionary<Type, object> queryInstances = new();
        private static readonly Dictionary<Type, object> serviceInstances = new();

        private static class QueryCache<T> where T : IReaCSQuery, new()
        {
            public static T instance;

            static QueryCache()
            {
                instance = new T();
                queryInstances[typeof(T)] = instance;
            }
        }

        private static class UseCache<T> where T : IReaCSService, new()
        {
            public static T instance;

            static UseCache()
            {
                instance = new T();
                serviceInstances[typeof(T)] = instance;
            }
        }

        public static void ClearAll()
        {
            foreach (var inst in queryInstances.Values)
                if (inst is IDisposable d) d.Dispose();

            foreach (var inst in serviceInstances.Values)
                if (inst is IDisposable d) d.Dispose();

            queryInstances.Clear();
            serviceInstances.Clear();

#if UNITY_EDITOR
            Debug.Log("[Access] Cleared all cached services and queries.");
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeReload()
        {
            ClearAll();
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void SetupEditorReset()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    ClearAll();
                }
            };
        }
#endif
    }
}
