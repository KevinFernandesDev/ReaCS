#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using ReaCS.Runtime.Core;
using System.Linq;

namespace ReaCS.Editor
{

    public static class ObservableObjectContextMenu
    {
        [MenuItem("Assets/ReaCS/🧹 Invalidate Saved Snapshot(s)", true)]
        private static bool ValidateSelection()
        {
            return Selection.objects.Length > 0 &&
                   Selection.objects.All(obj => obj is ObservableObject);
        }

        [MenuItem("Assets/ReaCS/🧹 Invalidate Saved Snapshot(s)")]
        private static void InvalidateSnapshots()
        {
            foreach (var obj in Selection.objects)
            {
                if (obj is ObservableObject so)
                {
                    so.BumpSnapshotVersion();
                    Debug.Log($"[ReaCS] Snapshot invalidated for {so.name}");
                }
            }
        }
    }
#endif

}
