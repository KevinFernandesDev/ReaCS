#if UNITY_EDITOR
using UnityEditor;

namespace ReaCS.Editor
{
    public static partial class ReaCSSettings
    {
        private const string DebugLogsKey = "ReaCS_DebugLogs";

        public static bool ReaCSLogs => EditorPrefs.GetBool(DebugLogsKey, false);

        public static float DefaultDebounceDelay =>
            EditorPrefs.GetFloat("ReaCS_DefaultDebounceDelay", 0.05f);

        public static void SetDebounceDelay(float value) =>
            EditorPrefs.SetFloat("ReaCS_DefaultDebounceDelay", value);

        public static void SetReaCSLogs(bool value) =>
            EditorPrefs.SetBool(DebugLogsKey, value);

        public static bool EnableVisualGraphEditModeReactions = false;
    }
}
#endif