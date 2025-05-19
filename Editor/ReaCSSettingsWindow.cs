#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    public class ReaCSSettingsWindow : EditorWindow
    {
        [MenuItem("Window/ReaCS/Debug Settings")]
        public static void ShowWindow()
        {
            GetWindow<ReaCSSettingsWindow>("ReaCS Debug Settings");
        }

        void OnGUI()
        {
            GUILayout.Label("ReaCS Debug Options", EditorStyles.boldLabel);

            bool current = EditorPrefs.GetBool("ReaCS_DebugLogs", false);
            bool updated = EditorGUILayout.Toggle("Enable Debug Logs", current);

            if (updated != current)
                EditorPrefs.SetBool("ReaCS_DebugLogs", updated);
        }
    }
}
#endif