using ReaCS.Runtime;
using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Registries;
using UnityEditor;

namespace ReaCS.Editor
{
    /// <summary>
    /// This is made to be able to not use Domain Reload and get faster workflow
    /// It clears up all static classes, events, etc to maintain correct behavior
    /// </summary>
    [InitializeOnLoad]
    public static class ReaCSEditorStaticReset
    {
        static ReaCSEditorStaticReset()
        {
            AssemblyReloadEvents.beforeAssemblyReload += CleanupStatics;
        }

        private static void CleanupStatics()
        {
            // Reset static singletons/events that won't get reloaded
            ReaCSBurstHistory.Clear();
            ObservableRegistry.ClearAll();

#if UNITY_EDITOR
            ReaCSBurstHistory.OnEditorLogUpdated = null;
            ObservableEditorBridge.OnEditorFieldChanged = null;
#endif
        }
    }
}