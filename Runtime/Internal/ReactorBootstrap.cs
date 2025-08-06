using ReaCS.Runtime.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReaCS.Runtime.Internal
{
    public static class ReactorBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void ReconnectAllReactors()
        {
            foreach (var mono in Object.FindObjectsOfType<MonoBehaviour>())
            {
                var type = mono.GetType();
                while (type != null)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Reactor<>))
                    {
                        var startMethod = type.GetMethod("Start", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        startMethod?.Invoke(mono, null);
                        break;
                    }
                    type = type.BaseType;
                }
            }
        }
    }
}
