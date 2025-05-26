#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using ReaCS.Runtime.Core;
using static ReaCS.Runtime.ReaCS;
using ReaCS.Runtime.Registries;

namespace ReaCS.Editor
{
    [CustomEditor(typeof(ObservableScriptableObject), true)]
    public class ObservableSOEditor : UnityEditor.Editor
    {
        private ObservableScriptableObject targetSO;
        private List<FieldInfo> observableFields;

        private void OnEnable()
        {
            targetSO = (ObservableScriptableObject)target;
            observableFields = new List<FieldInfo>();

            var fields = targetSO.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (Attribute.IsDefined(field, typeof(ObservableAttribute)))
                {
                    observableFields.Add(field);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Observables", EditorStyles.boldLabel);

            foreach (var field in observableFields)
            {
                object fieldValue = field.GetValue(targetSO);
                if (fieldValue == null) continue;

                var fieldType = field.FieldType;
                if (!fieldType.IsGenericType || fieldType.GetGenericTypeDefinition() != typeof(Observable<>))
                {
                    EditorGUILayout.LabelField(field.Name, "Unsupported type");
                    continue;
                }

                var valueProp = fieldType.GetProperty("Value");
                var persistField = fieldType.GetField("ShouldPersist", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (valueProp == null || persistField == null)
                {
                    EditorGUILayout.LabelField(field.Name, "Missing Value or ShouldPersist");
                    continue;
                }

                object currentValue = valueProp.GetValue(fieldValue);
                bool shouldPersist = (bool)persistField.GetValue(fieldValue);

                EditorGUILayout.BeginHorizontal();

                string label = ObjectNames.NicifyVariableName(field.Name);
                object newValue = DrawField(label, currentValue);
                if (!Equals(newValue, currentValue))
                {
                    valueProp.SetValue(fieldValue, newValue);
                }

                bool newShouldPersist = GUILayout.Toggle(shouldPersist, shouldPersist ? "🔒" : "🔄", GUILayout.Width(30));
                if (newShouldPersist != shouldPersist)
                {
                    persistField.SetValue(fieldValue, newShouldPersist);
                }

                EditorGUILayout.EndHorizontal();
            }

            // Draw the rest of the SO fields below Observables
            var excluded = new List<string> { "m_Script" };
            foreach (var field in observableFields)
                excluded.Add(field.Name);

            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            DrawLinkInfo(targetSO); // 👇 now at the bottom
        }

        private object DrawField(string label, object value)
        {
            if (value is int intVal)
                return EditorGUILayout.IntField(label, intVal);
            if (value is float floatVal)
                return EditorGUILayout.FloatField(label, floatVal);
            if (value is bool boolVal)
                return EditorGUILayout.Toggle(label, boolVal);
            if (value is string strVal)
                return EditorGUILayout.TextField(label, strVal);
            if (value is Enum enumVal)
                return EditorGUILayout.EnumPopup(label, enumVal);

            EditorGUILayout.LabelField(label, "Unsupported type");
            return value;
        }

        private void DrawLinkInfo(ObservableScriptableObject oso)
        {
            var linkCount = Query<LinkSORegistry>().CountLinksFor(oso);

            EditorGUILayout.LabelField("Link Tree", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"🔗 {linkCount} links found", EditorStyles.helpBox);
        }
    }
}
#endif
