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

            var dataSourceProp = serializedObject.FindProperty("dataSource");
            var useAsTemplateProp = serializedObject.FindProperty("useAsTemplate");

            bool hasDataSource = dataSourceProp != null && dataSourceProp.objectReferenceValue != null;
            bool useTemplate = hasDataSource && useAsTemplateProp != null && useAsTemplateProp.boolValue;

            EditorGUILayout.Space(6);

            // 🧠 Mode explanation box
            if (!hasDataSource)
            {
                EditorGUILayout.HelpBox(
                    "📄 Blank Runtime Mode: No source asset assigned.\nA fresh pooled instance will be created at runtime.",
                    MessageType.Warning
                );
            }
            else if (useTemplate)
            {
                EditorGUILayout.HelpBox(
                    "🧬 Template Mode: Will clone from the source asset using pooling.\nEach instance gets its own data copy.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "🔒 Static Mode: Uses the source asset directly.\nData is shared across all bindings referencing this asset.",
                    MessageType.Info
                );
            }

            EditorGUILayout.Space(6);
            if (dataSourceProp != null)
                EditorGUILayout.PropertyField(dataSourceProp, new GUIContent("Data Source (ScriptableObject)"));

            if (hasDataSource && useAsTemplateProp != null)
                EditorGUILayout.PropertyField(useAsTemplateProp, new GUIContent("Use As Template"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
