using UnityEngine;

namespace ReaCS.Editor
{
    using UnityEditor;
    using UnityEngine;
    using ReaCS.Runtime.Core;
    using System;

    public static class SOAutoLinkUtility
    {
        [MenuItem("Assets/ReaCS/Create Link To...", true)]
        public static bool ValidateLinkTarget()
        {
            return Selection.activeObject is ObservableObject;
        }

        [MenuItem("Assets/ReaCS/Create Link To...")]
        public static void CreateLinkToAnotherSO()
        {
            var sourceSO = Selection.activeObject as ObservableObject;
            if (sourceSO == null) return;

            string path = EditorUtility.OpenFilePanel("Select Target SO", "Assets", "asset");
            if (string.IsNullOrEmpty(path)) return;

            string assetPath = "Assets" + path.Replace(Application.dataPath, "");
            var targetSO = AssetDatabase.LoadAssetAtPath<ObservableObject>(assetPath);
            if (targetSO == null)
            {
                EditorUtility.DisplayDialog("Invalid Target", "That file is not a valid ObservableScriptableObject.", "OK");
                return;
            }

            CreateLinkAsset(sourceSO, targetSO);
        }

        private static void CreateLinkAsset(ObservableObject left, ObservableObject right)
        {
            var leftType = left.GetType();
            var rightType = right.GetType();

            var linkType = typeof(Link<,>).MakeGenericType(leftType, rightType);
            var link = ScriptableObject.CreateInstance(linkType);

            // Use reflection to set Left/Right
            var leftField = linkType.GetField("Left");
            var rightField = linkType.GetField("Right");

            var observableLeft = Activator.CreateInstance(typeof(ObservableSO<>).MakeGenericType(leftType));
            var observableRight = Activator.CreateInstance(typeof(ObservableSO<>).MakeGenericType(rightType));

            var valueProp = observableLeft.GetType().GetProperty("Value");
            valueProp.SetValue(observableLeft, left);

            valueProp = observableRight.GetType().GetProperty("Value");
            valueProp.SetValue(observableRight, right);

            leftField.SetValue(link, observableLeft);
            rightField.SetValue(link, observableRight);

            string leftName = left.name.Replace(" ", "_");
            string rightName = right.name.Replace(" ", "_");
            string folder = "Assets/GeneratedLinks";

            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets", "GeneratedLinks");

            string path = $"{folder}/Link_{leftName}_To_{rightName}.asset";
            AssetDatabase.CreateAsset(link, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(link);
            Debug.Log($"✅ Created LinkSO between {left.name} ➜ {right.name}");
        }
    }
}

