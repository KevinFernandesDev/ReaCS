#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ReaCS.Runtime;
using ReaCS.Runtime.Core;
using UnityEngine.UIElements;

namespace ReaCS.Editor
{
    public class LinkTreeGraphWindow : EditorWindow
    {
        private LinkTreeGraphView graphView;

        [MenuItem("ReaCS/Link Tree Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<LinkTreeGraphWindow>("Link Tree Graph");
            window.Show();
        }

        public static void ShowForRoot(ObservableScriptableObject root)
        {
            var window = GetWindow<LinkTreeGraphWindow>("Link Tree Graph");
            window.Initialize(root);
        }

        private void Initialize(ObservableScriptableObject root)
        {
            rootVisualElement.Clear();
            graphView = new LinkTreeGraphView(root);
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }
    }
}
#endif
