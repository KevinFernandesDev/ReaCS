#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using ReaCS.Runtime.Core;
using static ReaCS.Runtime.Access;
using ReaCS.Runtime.Registries;

namespace ReaCS.Editor
{
    [CustomEditor(typeof(ObservableScriptableObject), true)]
    public class ObservableSOEditor : UnityEditor.Editor
    {
        private ObservableScriptableObject targetSO;
        private List<FieldInfo> observableFields;

        protected virtual void OnEnable()
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
                if (fieldValue == null)
                {
                    EditorGUILayout.LabelField(field.Name, "Field is null");
                    continue;
                }

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
                object newValue = DrawField(label, currentValue, field);
                if (!Equals(newValue, currentValue))
                {
                    valueProp.SetValue(fieldValue, newValue);
                }

                bool newShouldPersist = GUILayout.Toggle(shouldPersist, shouldPersist ? "🔒" : "🔄", GUILayout.Width(30));
                if (newShouldPersist != shouldPersist)
                {
                    persistField.SetValue(fieldValue, newShouldPersist); 
                    EditorUtility.SetDirty(targetSO);
                    AssetDatabase.SaveAssets(); // optional but helpful
                }

                EditorGUILayout.EndHorizontal();
            }

            // Draw the rest of the SO fields below Observables
            var excluded = new List<string> { "m_Script" };
            foreach (var field in observableFields)
                excluded.Add(field.Name);

            DrawPropertiesExcluding(serializedObject, excluded.ToArray());

            foreach (var field in observableFields)
            {
                var fieldValue = field.GetValue(targetSO);
                if (fieldValue == null) continue;

                var syncMethod = fieldValue.GetType().GetMethod("EditorSyncFromInspector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                syncMethod?.Invoke(fieldValue, null);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(12);
            DrawLinkInfo(targetSO); // 👇 now at the bottom
        }

        private object DrawField(string label, object value, FieldInfo backingField = null)
        {
            if (value is float floatVal)
            {
                var range = backingField?.GetCustomAttribute<ObservableRangeAttribute>();
                return range != null
                    ? EditorGUILayout.Slider(label, floatVal, range.min, range.max)
                    : EditorGUILayout.FloatField(label, floatVal);
            }

            if (value is int intVal)
                return EditorGUILayout.IntField(label, intVal);

            if (value is bool boolVal)
                return EditorGUILayout.Toggle(label, boolVal);

            if (value is string strVal)
                return EditorGUILayout.TextField(label, strVal);

            if (value is Enum enumVal)
                return EditorGUILayout.EnumPopup(label, enumVal);

            if (value is Vector2 vec2Val)
                return EditorGUILayout.Vector2Field(label, vec2Val);

            if (value is Vector3 vec3Val)
                return EditorGUILayout.Vector3Field(label, vec3Val);

            if (value is Vector4 vec4Val)
                return EditorGUILayout.Vector4Field(label, vec4Val);

            if (value is Quaternion quatVal)
            {
                Vector4 raw = new(quatVal.x, quatVal.y, quatVal.z, quatVal.w);
                Vector4 newVal = EditorGUILayout.Vector4Field(label + " (xyzw)", raw);
                return new Quaternion(newVal.x, newVal.y, newVal.z, newVal.w);
            }

            if (value is Color colorVal)
                return EditorGUILayout.ColorField(label, colorVal);

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
