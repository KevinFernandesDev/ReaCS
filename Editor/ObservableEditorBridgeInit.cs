using ReaCS.Runtime.Internal;
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    /// <summary>
    /// This is made to be able to not use Domain Reload and get faster workflow
    /// This manually reinitialized the editor bridges
    /// </summary>
    [InitializeOnLoad]
    public static class ObservableEditorBridgeInit
    {
        static ObservableEditorBridgeInit()
        {
            ObservableEditorBridge.OnEditorFieldChanged = null; // Clear if stale
        }
    }

}
