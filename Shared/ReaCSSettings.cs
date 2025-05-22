namespace ReaCS.Shared
{
    public static class ReaCSSettings
    {
        private const string DebounceKey = "ReaCS_DefaultDebounceDelay";
        private const string DebugLogsKey = "ReaCS_DebugLogs";

        public static float DefaultDebounceDelay =>
            UnityEditor.EditorPrefs.GetFloat(DebounceKey, 0.05f);

        public static void SetDebounceDelay(float value)
        {
            UnityEditor.EditorPrefs.SetFloat(DebounceKey, value);
        }

        public static bool ReaCSLogs =>
            UnityEditor.EditorPrefs.GetBool(DebugLogsKey, false);

        public static void SetReaCSLogs(bool value)
        {
            UnityEditor.EditorPrefs.SetBool(DebugLogsKey, value);
        }

        public static bool EnableVisualGraphEditModeReactions = false;
    }
}
