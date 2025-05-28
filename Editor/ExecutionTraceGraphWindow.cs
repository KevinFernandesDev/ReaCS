// ExecutionTraceGraphWindow.cs
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ReaCS.Editor
{
    public class ExecutionTraceGraphWindow : EditorWindow
    {
        private ExecutionTraceGraphView graphView;

        [MenuItem("ReaCS/Runtime Execution Trace Graph")]
        public static void ShowWindow()
        {
            var window = GetWindow<ExecutionTraceGraphWindow>();
            window.titleContent = new GUIContent("Runtime Execution Trace");
            window.Show();
        }

        private void OnEnable()
        {
            graphView = new ExecutionTraceGraphView();
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        private void OnDisable()
        {
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
                graphView.Cleanup();
            }
        }
    }
}
