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
using ReaCS.Runtime.Core;

namespace ReaCS.Editor
{
    public class StaticDependencyGraphWindow : EditorWindow
    {
        private StaticDependencyGraphView graphView;
        private Label statusLabel;
        private Button resetButton;
        private Toggle lockToggle;
        private bool isLocked = false;
        private ScrollView _historyDrawer;
        private TextField _historySearchField;
        private VisualElement _drawerContainer;
        private string _searchQuery = string.Empty;

        [MenuItem("ReaCS/Static Dependency Graph")]
        public static void Open()
        {
            var wnd = GetWindow<StaticDependencyGraphWindow>();
            wnd.titleContent = new GUIContent("Static Dependency Graph");
        }

        public void OnEnable()
        {
            ReaCSSettings.EnableVisualGraphEditModeReactions = true;
            ObservableEditorBridge.OnEditorFieldChanged += MarkFieldChangedFromRuntime;
            Selection.selectionChanged += OnSelectionChanged;
            ReaCSBurstHistory.OnEditorLogUpdated += RefreshHistoryView;

            rootVisualElement.Clear();

            var layout = new VisualElement { 
                style = { 
                    flexDirection = FlexDirection.Column, 
                    flexGrow = 1 
                } 
            };
            layout.pickingMode = PickingMode.Position;
            layout.focusable = true;

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
                    if (Selection.activeObject is ObservableObject so)
                    {
                        FilterToSO(so);
                    }
                    else
                    {
                        AnimateStatus("🧠 Showing: Entire Project");
                        graphView?.Populate(isInitialLoad: true, triggerPulse: true);
                    }
                }
            });

            resetButton = new Button(() =>
            {
                isLocked = false;
                lockToggle.SetValueWithoutNotify(false);

                if (Selection.activeObject is ObservableObject so)
                {
                    FilterToSO(so);
                }
                else
                {
                    AnimateStatus("🧠 Showing: Entire Project");
                    graphView?.Populate(isInitialLoad: true, triggerPulse: true);
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

            var contentRow = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
            graphView.style.flexGrow = 1;
            contentRow.Add(graphView);
            ConstructHistoryDrawer();
            contentRow.Add(_drawerContainer);

            layout.Add(contentRow);

            graphView?.Populate(isInitialLoad: true, triggerPulse: true);


            // Schedule FrameAllNodes after layout finishes
            EditorApplication.delayCall += () =>
            {
                graphView.schedule.Execute(() =>
                {
                    graphView.FrameAllNodes();
                }).ExecuteLater(100); // allow final layout pass
            };

            if (Application.isPlaying)
            {
                ReaCSBurstHistory.Init(); // ensure _entries is created and _backup copied in
                RefreshHistoryView();     // immediately fill the scroll view

                // Automatically reset view when entering play mode
                HandleReset();
            }

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

        private void ConstructHistoryDrawer()
        {
            _drawerContainer = new VisualElement
            {
                pickingMode = PickingMode.Position,
                focusable = true
            };
            _drawerContainer.style.minWidth = 320;
            _drawerContainer.style.flexShrink = 0;
            _drawerContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f);
            _drawerContainer.style.flexDirection = FlexDirection.Column;
            _drawerContainer.style.paddingTop = 4;
            _drawerContainer.style.paddingLeft = 6;
            _drawerContainer.style.paddingRight = 6; 
            _drawerContainer.usageHints = UsageHints.DynamicTransform;

            var drawerLabel = new Label("🕓 Runtime History")
            {
                style = {
            unityFontStyleAndWeight = FontStyle.Bold,
            fontSize = 13,
            marginBottom = 4
        }
            };

            _historySearchField = new TextField("Filter:")
            {
                style = {
            marginBottom = 4
        }
            };
            _historySearchField.RegisterValueChangedCallback(evt =>
            {
                _searchQuery = evt.newValue;
                RefreshHistoryView();
            });

            _historyDrawer = new ScrollView(ScrollViewMode.Vertical)
            {
                pickingMode = PickingMode.Position,
                focusable = true
            };
            _historyDrawer.style.flexGrow = 1;

            _drawerContainer.Add(drawerLabel);
            _drawerContainer.Add(_historySearchField);

            var clearButton = new Button(() =>
            {
                ReaCSBurstHistory.Clear();
                RefreshHistoryView();
            })
            {
                text = "🗑 Clear History",
                tooltip = "Clear all runtime field change logs"
            };
            clearButton.style.marginBottom = 4;
            _drawerContainer.Add(clearButton);

            var isMac = Application.platform == RuntimePlatform.OSXEditor;
            var keyTip = isMac ? "⌘" : "Ctrl";
            var shortcutHelp = new Label($"⤷ Shortcuts: [Click] Pulse Nodes     [{keyTip}+Click] Open System Script")
            {
                style = {
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new Color(1f, 1f, 1f, 0.45f),
                    position = Position.Relative,
                    marginTop = 4,
                    marginBottom = 2
                }
            };

            shortcutHelp.style.alignSelf = Align.Center;
            _drawerContainer.Add(shortcutHelp);

            _drawerContainer.Add(_historyDrawer); 

        }

        private void RefreshHistoryView()
        {
            _historyDrawer.Clear();

            var entries = ReaCSBurstHistory.ToArray();
            if (entries == null || entries.Length == 0) return;

            foreach (var entry in entries)
            {
                string soName = entry.soName.ToString();
                string fieldName = entry.fieldName.ToString();
                string systemName = entry.systemName.ToString();
                string fieldId = $"{soName}.{fieldName}";

                if (!string.IsNullOrWhiteSpace(_searchQuery))
                {
                    string q = _searchQuery.ToLowerInvariant();
                    if (!soName.ToLowerInvariant().Contains(q) &&
                        !fieldName.ToLowerInvariant().Contains(q) &&
                        !systemName.ToLowerInvariant().Contains(q))
                        continue;
                }

                var container = new VisualElement
                {
                    pickingMode = PickingMode.Position,
                    focusable = true,
                    style =
            {
                backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                marginBottom = 4,
                paddingTop = 4,
                paddingBottom = 4,
                paddingLeft = 6,
                paddingRight = 6,
                borderBottomWidth = 1,
                borderBottomColor = new Color(0.05f, 0.05f, 0.05f),
                borderLeftWidth = 2,
                borderLeftColor = Color.green,
                unityFontStyleAndWeight = FontStyle.Normal,
                cursor = new StyleCursor((StyleKeyword)MouseCursor.Link),
                whiteSpace = WhiteSpace.Normal,
                minHeight = 50,
                flexGrow = 0,
                flexDirection = FlexDirection.Column
            }
                };

                container.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                    graphView?.ScrollToNode(fieldId);
                });

                container.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    container.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
                });

                var fieldLine = new Label($"🔹 {soName}.{fieldName}")
                {
                    style = {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 12,
                marginBottom = 2
            }
                };
                container.Add(fieldLine);

                var valueLine = new Label($"→ {entry.debugOld:F2} → {entry.debugNew:F2}")
                {
                    style = {
                fontSize = 11,
                color = new Color(0.8f, 0.85f, 0.95f),
                marginBottom = 2
            }
                };
                container.Add(valueLine);

                var systemLine = new Label($"👤 {systemName}  •  Frame {entry.frame}")
                {
                    style = {
                fontSize = 10,
                color = new Color(0.6f, 0.6f, 0.6f)
            }
                };
                container.Add(systemLine);

                container.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if ((evt.ctrlKey || evt.commandKey) && !string.IsNullOrWhiteSpace(systemName) && systemName != "Unknown")
                    {
                        var type = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == systemName);

                        if (type != null)
                        {
#if UNITY_EDITOR
                            var guids = AssetDatabase.FindAssets("t:MonoScript");
                            foreach (var guid in guids)
                            {
                                var path = AssetDatabase.GUIDToAssetPath(guid);
                                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                                if (script != null && script.GetClass() == type)
                                {
                                    AssetDatabase.OpenAsset(script);
                                    Debug.Log($"[ReaCS] ✍️ Opened script for system: {systemName}");
                                    break;
                                }
                            }
#endif
                        }

                        evt.StopPropagation();
                        return;
                    }

                    graphView?.ScrollToNode(fieldId);
#if UNITY_EDITOR
                    ObservableEditorBridge.OnEditorFieldChanged?.Invoke(soName, fieldName);
#endif
                });

                _historyDrawer.Add(container);
            }
        }



        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                RefreshHistoryView();
            }
        }

        private void MarkFieldChangedFromRuntime(string soName, string fieldName)
        {
            graphView?.MarkChanged($"{soName}.{fieldName}");
        }

        public void HandleReset()
        {
            isLocked = false;
            lockToggle.SetValueWithoutNotify(false);

            if (Selection.activeObject is ObservableObject so)
            {
                FilterToSO(so);
            }
            else
            {
                AnimateStatus("🧠 Showing: Entire Project");
                graphView?.Populate(isInitialLoad: true, triggerPulse: true);
                graphView.schedule.Execute(() => graphView.AnimateFrameAllNodes()).ExecuteLater(100);
            }
        }

        private void ConstructGraphView()
        {
            graphView = new StaticDependencyGraphView
            {
                name = "ReaCS Graph View"
            };
        }

        private void OnSelectionChanged()
        {
            if (isLocked) return;

            if (Selection.activeObject is ObservableObject so)
            {
                AnimateStatus($"🔬 Focused on: {so.name}.asset");
                graphView.SetFocusedSO(so.name);
                graphView?.Populate(graphView.BuildFilterSetForSO(so));
                graphView.schedule.Execute(() => graphView.AnimateFrameAllNodes()).ExecuteLater(100);
                return;
            }

            if (Selection.activeObject is MonoScript script)
            {
                var type = script.GetClass();
                if (type != null &&
                    type.BaseType?.IsGenericType == true &&
                    type.BaseType.GetGenericTypeDefinition() == typeof(Reactor<>))
                {
                    AnimateStatus($"⚙️ Focused on: {type.Name}");
                    graphView?.Populate(graphView.BuildFilterSetForSystem(type));
                    graphView.schedule.Execute(() => graphView.AnimateFrameAllNodes()).ExecuteLater(100);
                    return;
                }
            }

            // fallback to full graph
            AnimateStatus("🧠 Showing: Entire Project");
            graphView?.Populate(isInitialLoad: false, triggerPulse: false);

            graphView.schedule.Execute(() => graphView.AnimateFrameAllNodes()).ExecuteLater(100);
        }

        private void FilterToSO(ObservableObject so)
        {
            AnimateStatus($"🔬 Focused on: {so.name}.asset");

            var allSystemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOfRawGeneric(typeof(Reactor<>)) && !t.IsAbstract)
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
            ObservableEditorBridge.OnEditorFieldChanged -= MarkFieldChangedFromRuntime;
            Selection.selectionChanged -= OnSelectionChanged;
            ReaCSBurstHistory.OnEditorLogUpdated -= RefreshHistoryView;
            rootVisualElement.Clear();
            ReaCSSettings.EnableVisualGraphEditModeReactions = false;
        }
    }
}
