using ReaCS.Runtime.Core;
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
    [CustomPropertyDrawer(typeof(ObservableSO<>))]
    public class ObservableSOPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("value");
            if (valueProp != null)
            {
                EditorGUI.PropertyField(position, valueProp, label);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Missing 'Value' field");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("Value");
            return valueProp != null
                ? EditorGUI.GetPropertyHeight(valueProp, label, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}