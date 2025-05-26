#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ReaCS.Runtime.Core;
using System;

namespace ReaCS.Editor
{
    [InitializeOnLoad]
    public static class LinkTreeAutoOpener
    {
        static LinkTreeAutoOpener()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void OnSelectionChanged()
        {
            if (Selection.activeObject is ObservableScriptableObject oso)
            {
                EditorApplication.delayCall += () =>
                {
                    if (Selection.activeObject == oso) // Still selected
                    {
                        LinkTreeGraphWindow.ShowForRoot(oso);
                    }
                };
            }
        }
    }
}
#endif
