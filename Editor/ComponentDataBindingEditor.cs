#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ReaCS.Runtime.Core;

namespace ReaCS.Editor
{
    [CustomEditor(typeof(ComponentDataBinding), true)]
    public class ComponentDataBindingEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var staticDataProp = serializedObject.FindProperty("staticData");
            var sourceDataProp = serializedObject.FindProperty("sourceDataSO");

            bool hasStatic = staticDataProp != null && staticDataProp.objectReferenceValue != null;

            // 🧠 Clear mode indicator
            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                hasStatic
                    ? "🔒 Static Mode: This component uses a persistent asset from the project."
                    : "⚡ Runtime Mode: This component instantiates pooled data at runtime.",
                MessageType.Info
            );

            EditorGUILayout.Space(6);
            if (staticDataProp != null)
                EditorGUILayout.PropertyField(staticDataProp, new GUIContent("Static Data (Persistent)"));

            if (!hasStatic && sourceDataProp != null)
                EditorGUILayout.PropertyField(sourceDataProp, new GUIContent("Runtime Source (Cloned)"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif