#if UNITY_EDITOR
using ReaCS.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    [CustomPropertyDrawer(typeof(Observable<>))]
    public class ObservableDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            if (valueProp == null)
            {
                EditorGUI.LabelField(position, label.text, "Unsupported type");
                return;
            }

            EditorGUI.PropertyField(position, valueProp, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            return EditorGUI.GetPropertyHeight(valueProp, label, true);
        }
    }
#endif
}