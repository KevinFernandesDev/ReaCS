#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using ReaCS.Runtime.Core;

namespace ReaCS.Editor
{

    public static class ObservableObjectUtility
    {
        [MenuItem("Assets/ReaCS/Clear Persisted State", true)]
        private static bool ValidateClearPersistedState()
        {
            return Selection.activeObject is ObservableObject;
        }

        [MenuItem("Assets/ReaCS/Clear Persisted State")]
        private static void ClearPersistedState()
        {
            if (Selection.activeObject is ObservableObject obj)
            {
                string path = Path.Combine(Application.persistentDataPath, obj.name + "_state.json");
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log($"Cleared saved state for: {obj.name}");
                }
                else
                {
                    Debug.Log($"No saved state found for: {obj.name}");
                }
            }
        }
    }
#endif

}
