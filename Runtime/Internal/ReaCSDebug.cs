using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ReaCSDebug
    {
#if UNITY_EDITOR
        public static bool EnableLogs =>
            UnityEditor.EditorPrefs.GetBool("ReaCS_DebugLogs", false);
#else
        public static bool EnableLogs => false;
#endif

        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (EnableLogs)
                Debug.Log(message);
#endif
        }

        public static void LogWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (EnableLogs)
                Debug.LogWarning(message);
#endif
        }

        public static void LogError(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (EnableLogs)
                Debug.LogError(message);
#endif
        }
    }
}
