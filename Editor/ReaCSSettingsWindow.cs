#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

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

            // Debug logs toggle
            bool logsEnabled = EditorPrefs.GetBool("ReaCS_DebugLogs", false);
            bool newLogsEnabled = EditorGUILayout.Toggle("Enable Debug Logs", logsEnabled);
            if (newLogsEnabled != logsEnabled)
                EditorPrefs.SetBool("ReaCS_DebugLogs", newLogsEnabled);

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
                ReaCSSettings.SetDebounceDelay(newDebounce);

            GUILayout.Space(12);
            GUILayout.Label("Persistence", EditorStyles.boldLabel);

            // Persistent Data Path (runtime saves)
            string pdp = Application.persistentDataPath;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(pdp, GUILayout.Height(16));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
                EditorGUIUtility.systemCopyBuffer = pdp;
            if (GUILayout.Button("Reveal", GUILayout.Width(70)))
                RevealInFinderSafe(pdp);
            EditorGUILayout.EndHorizontal();

            // Editor snapshot info (Temp) when NOT playing
            if (!EditorApplication.isPlaying)
            {
                string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp");
                EditorGUILayout.HelpBox(
                    "In Edit Mode, snapshots are written to the project Temp/ folder. " +
                    "In Play Mode or in builds, data is written to the Persistent Data Path above.",
                    MessageType.Info
                );

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel(tempPath, GUILayout.Height(16));
                if (GUILayout.Button("Copy", GUILayout.Width(60)))
                    EditorGUIUtility.systemCopyBuffer = tempPath;
                if (GUILayout.Button("Reveal Temp", GUILayout.Width(90)))
                    RevealInFinderSafe(tempPath);
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void RevealInFinderSafe(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                EditorUtility.RevealInFinder(path);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ReaCS] Failed to reveal path: {path}\n{ex}");
            }
        }
    }
}
#endif
