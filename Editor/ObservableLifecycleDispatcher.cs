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
                Debug.Log("[ObservableStateDispatcher] Saving state before play.");

                foreach (var so in allSOs)
                    so.SaveStateToJson();
            }

            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                ReaCSGraphViewWindowHelper.ResetOpenGraphView();
            }
        }
    }

#endif
}