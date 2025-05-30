#if UNITY_EDITOR
using ReaCS.Runtime.Core;
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

            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ReaCSGraphViewWindowHelper.ResetOpenGraphView();
            }
        }
    }

#endif
}