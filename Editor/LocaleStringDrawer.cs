using ReaCS.Runtime.Core;
using ReaCS.Runtime.Internal;
using UnityEditor;
using UnityEngine;

namespace ReaCS.Editor
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Observable<LocaleString>))]
    public class LocaleStringDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var tableProp = property.FindPropertyRelative("table");
            var entryProp = property.FindPropertyRelative("entry");

            float halfWidth = position.width / 2f;
            var tableRect = new Rect(position.x, position.y, halfWidth - 2, position.height);
            var entryRect = new Rect(position.x + halfWidth + 2, position.y, halfWidth - 2, position.height);

            EditorGUI.PropertyField(tableRect, tableProp, GUIContent.none);
            EditorGUI.PropertyField(entryRect, entryProp, GUIContent.none);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
#endif
}
