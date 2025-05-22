#if UNITY_EDITOR
using ReaCS.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    [InitializeOnLoad]
    public static class ObservableStateDispatcher
    {
        static ObservableStateDispatcher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var allSOs = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>();

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                foreach (var so in allSOs)
                    so.SaveStateToJson();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                foreach (var so in allSOs)
                    so.LoadStateFromJson();
            }
        }
    }
#endif
}