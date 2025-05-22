using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ReaCS.Runtime;
using ReaCS.Runtime.Internal;
using ReaCS.Shared;

namespace ReaCS.Editor
{
    public class ReaCSGraphViewWindow : EditorWindow
    {
        private ReaCSGraphView graphView;
        private Label statusLabel;
        private Button resetButton;
        private Toggle lockToggle;
        private bool isLocked = false;

        [MenuItem("Window/ReaCS/Node Graph Visualizer")]
        public static void Open()
        {
            var wnd = GetWindow<ReaCSGraphViewWindow>();
            wnd.titleContent = new GUIContent("ReaCS Node Graph");
        }

        public void OnEnable()
        {
            ReaCSSettings.EnableVisualGraphEditModeReactions = true;
            ObservableEditorBridge.OnEditorFieldChanged += MarkFieldChangedFromRuntime;
            Selection.selectionChanged += OnSelectionChanged;
            rootVisualElement.Clear();

            var layout = new VisualElement { style = { flexDirection = FlexDirection.Column, flexGrow = 1 } };
            rootVisualElement.Add(layout);

            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

            statusLabel = new Label("🧠 Showing: Entire Project")
            {
                style = {
                    unityFontStyleAndWeight = FontStyle.Italic,
                    marginBottom = 4,
                    marginTop = 2,
                    marginLeft = 6,
                    fontSize = 12,
                    color = new Color(0.85f, 0.85f, 0.85f)
                }
            };

            lockToggle = new Toggle("Lock View") { value = false };
            lockToggle.tooltip = "Prevent automatic focus when selecting an SO (Shortcut: L)";
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                isLocked = evt.newValue;

                if (!isLocked)
                {
                    if (Selection.activeObject is ObservableScriptableObject so)
                    {
                        FilterToSO(so);
                    }
                    else
                    {
                        AnimateStatus("🧠 Showing: Entire Project");
                        graphView.Populate();
                    }
                }
            });

            resetButton = new Button(() =>
            {
                isLocked = false;
                lockToggle.SetValueWithoutNotify(false);

                if (Selection.activeObject is ObservableScriptableObject so)
                {
                    FilterToSO(so);
                }
                else
                {
                    AnimateStatus("🧠 Showing: Entire Project");
                    graphView.Populate();
                }
            })
            { text = "Reset View" };
            resetButton.tooltip = "Clear filters and show full graph (Shortcut: R)";

            toolbar.Add(statusLabel);
            toolbar.Add(lockToggle);
            toolbar.Add(resetButton);

            layout.Add(toolbar);

            ConstructGraphView();

            graphView.style.flexGrow = 1;
            layout.Add(graphView);           

            graphView.Populate();

            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.L)
                {
                    lockToggle.value = !lockToggle.value;
                    evt.StopPropagation();
                }
                else if (evt.keyCode == KeyCode.R)
                {
                    resetButton = new Button(HandleReset) { text = "Reset View" };
                    evt.StopPropagation();
                }
            });
        }

        private void MarkFieldChangedFromRuntime(string soName, string fieldName)
        {
            graphView?.MarkChanged($"{soName}.{fieldName}");
        }

        private void HandleReset()
        {
            isLocked = false;
            lockToggle.SetValueWithoutNotify(false);

            if (Selection.activeObject is ObservableScriptableObject so)
            {
                FilterToSO(so);
            }
            else
            {
                AnimateStatus("🧠 Showing: Entire Project");
                graphView.Populate();
            }
        }

        private void ConstructGraphView()
        {
            graphView = new ReaCSGraphView
            {
                name = "ReaCS Graph View"
            };
        }

        private void OnSelectionChanged()
        {
            if (isLocked) return;

            if (Selection.activeObject is ObservableScriptableObject selectedSO)
            {
                FilterToSO(selectedSO);
            }
            else
            {
                AnimateStatus("🧠 Showing: Entire Project");
                graphView?.Populate();
            }

            if (Selection.activeObject is MonoScript script)
            {
                var type = script.GetClass();
                if (type != null && type.BaseType?.IsGenericType == true &&
                    type.BaseType.GetGenericTypeDefinition() == typeof(SystemBase<>))
                {
                    graphView?.FocusSystemGraph(type);
                    return;
                }
            }
        }

        private void FilterToSO(ObservableScriptableObject so)
        {
            AnimateStatus($"🔬 Focused on: {so.name}.asset");

            var allSystemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOfRawGeneric(typeof(SystemBase<>)) && !t.IsAbstract)
                .ToList();

            var visibleNodes = new HashSet<string> { so.name };

            var fields = so.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f =>
                    Attribute.IsDefined(f, typeof(ObservableAttribute)) ||
                    Attribute.IsDefined(f, typeof(ObservableSavedAttribute)));

            foreach (var field in fields)
            {
                string fieldName = field.Name;
                string fieldId = $"{so.name}.{fieldName}";
                visibleNodes.Add(fieldId);

                foreach (var sysType in allSystemTypes)
                {
                    var attrs = sysType.GetCustomAttributes(typeof(ReactToAttribute), true);
                    foreach (var attr in attrs.Cast<ReactToAttribute>())
                    {
                        if (attr.FieldName != fieldName) continue;
                        var baseType = sysType.BaseType;
                        if (baseType is { IsGenericType: true })
                        {
                            var soType = baseType.GetGenericArguments()[0];
                            if (!soType.IsAssignableFrom(so.GetType())) continue;

                            string sysId = sysType.Name;
                            visibleNodes.Add(sysId);
                        }
                    }
                }
            }

            graphView.Populate(visibleNodes);
            graphView.ScrollToNode(so.name);
        }

        private void AnimateStatus(string text)
        {
            statusLabel.style.opacity = 0f;
            statusLabel.text = text;
            statusLabel.experimental.animation.Start(new StyleValues { opacity = 1f }, 300);
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            rootVisualElement.Clear();
            ReaCSSettings.EnableVisualGraphEditModeReactions = false;
        }
    }
}
