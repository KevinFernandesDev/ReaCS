#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    public class ReaCSSettingsWindow : EditorWindow
    {
        [MenuItem("Window/ReaCS/Settings")]
        public static void ShowWindow()
        {
            GetWindow<ReaCSSettingsWindow>("ReaCS Settings");
        }

        void OnGUI()
        {
            GUILayout.Label("ReaCS Options", EditorStyles.boldLabel);

            // Logs toggle (directly updates EditorPrefs via ReaCSDebug)
            bool logsEnabled = EditorPrefs.GetBool("ReaCS_DebugLogs", false);
            bool newLogsEnabled = EditorGUILayout.Toggle("Enable Debug Logs", logsEnabled);
            if (newLogsEnabled != logsEnabled)
            {
                EditorPrefs.SetBool("ReaCS_DebugLogs", newLogsEnabled);
            }

            GUILayout.Space(10);
            GUILayout.Label("Debounce Settings", EditorStyles.boldLabel);

            float currentDebounce = ReaCSSettings.DefaultDebounceDelay;
            float newDebounce = EditorGUILayout.Slider(
                new GUIContent("Default Debounce Delay (s)", "Delay before SO changes trigger systems."),
                currentDebounce,
                0.001f,
                1f
            );

            if (!Mathf.Approximately(currentDebounce, newDebounce))
            {
                ReaCSSettings.SetDebounceDelay(newDebounce);
            }
        }
    }
}
#endif
