using UnityEngine;

namespace ReaCS.Runtime.Internal
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public static class ReaCSDebug
    {
        public static bool EnableLogs => ReaCSSettings.ReaCSLogs;

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
