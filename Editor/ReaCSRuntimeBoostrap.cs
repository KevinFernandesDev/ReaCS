#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;

namespace ReaCS.Editor
{
    public static class ReaCSRuntimeBootstrap
    {
        [InitializeOnEnterPlayMode]
        public static void RehydrateAllSOs()
        {
            var all = Resources.FindObjectsOfTypeAll<ObservableObject>();

            foreach (var so in all)
            {
                if (EditorUtility.IsPersistent(so)) continue; // only runtime-bound assets
                ObservableRegistry.Register(so);
                ObservableRuntimeWatcher.Register(so);
            }

            ReaCSDebug.Log($"[ReaCS] ♻️ Rehydrated {all.Length} ObservableScriptableObjects for runtime play mode.");
        }
    }
#endif
}