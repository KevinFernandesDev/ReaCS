#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ReaCS.Runtime;
using ReaCS.Runtime.Internal;

namespace ReaCS.Editor
{
    public enum NodeType { SO, Field, System }

    public class ReaCSGraphView : GraphView
    {
        private readonly Vector2 defaultNodeSize = new(200, 80);
        private Dictionary<string, Node> nodeMap = new();
        private Dictionary<string, float> changedTimestamps = new();
        private Dictionary<FlowingEdge, double> activePulseEdges = new();
        private List<FlowingEdge> allFlowingEdges = new();
        private Dictionary<string, string> fieldToSO = new();
        private Dictionary<string, string> systemToSO = new();
        private Dictionary<string, Label> fieldValueLabels = new();
        private Dictionary<string, object> lastFieldValues = new();
        private Dictionary<string, VisualElement> fieldValueIcons = new();
        private Dictionary<string, VisualElement> fieldValueContainers = new();
        private Dictionary<Node, string> nodeToId = new();
        private Dictionary<string, List<string>> fieldToFieldGraph = new();
        private string currentFilter = "";

        public ReaCSGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            var minimap = new MiniMap { anchored = true };
            Add(minimap);

            RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (parent == null) return;
                var width = layout.width;
                var height = layout.height;
                minimap.SetPosition(new Rect(width - 210, height - 160, 200, 150));
            });

            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.P)
                {
                    var selected = selection.FirstOrDefault() as Node;
                    if (selected != null)
                    {
                        PingNodeAsset(selected);
                        evt.StopPropagation();
                    }
                }
            });


            // === Shortcut Help Banner ===
            var helpLabel = new Label("⤷ Shortcuts: [F] Frame     [R] Reset     [L] Lock     [P] Ping Selected")
            {
                style = {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new Color(1f, 1f, 1f, 0.5f),
                    position = Position.Relative
                }
            };

            helpLabel.style.alignSelf = Align.Center;
            helpLabel.style.marginTop = 8;
            helpLabel.style.marginBottom = 4;

            Add(helpLabel);
        }

        public void Filter(string filter)
        {
            currentFilter = string.IsNullOrEmpty(filter) ? "" : filter.ToLower();

            if (string.IsNullOrEmpty(currentFilter))
            {
                Populate();
                return;
            }

            var matchedIds = new HashSet<string>();

            foreach (var fieldId in fieldToSO.Keys)
            {
                if (fieldId.ToLower().Contains(currentFilter))
                {
                    matchedIds.Add(fieldId);
                    matchedIds.Add(fieldToSO[fieldId]);
                }
            }

            foreach (var systemId in systemToSO.Keys)
            {
                if (systemId.ToLower().Contains(currentFilter))
                {
                    matchedIds.Add(systemId);
                    matchedIds.Add(systemToSO[systemId]);
                }
            }

            foreach (var soId in nodeMap.Keys)
            {
                if (soId.ToLower().Contains(currentFilter))
                {
                    matchedIds.Add(soId);
                }
            }

            foreach (var edge in allFlowingEdges)
            {
                var fromId = edge.output?.node?.title;
                var toId = edge.input?.node?.title;

                if (fromId != null && matchedIds.Contains(toId)) matchedIds.Add(fromId);
                if (toId != null && matchedIds.Contains(fromId)) matchedIds.Add(toId);
            }

            Populate(matchedIds);
        }

        public void MarkChanged(string fieldId)
        {
            ReaCSDebug.Log($"[GraphView] MarkChanged called for {fieldId}");

            if (nodeMap.TryGetValue(fieldId, out var fieldNode))
            {
                changedTimestamps[fieldId] = (float)EditorApplication.timeSinceStartup;
            }

            schedule.Execute(UpdatePulse).Every(16);
            UpdatePulse();

            foreach (var edge in allFlowingEdges)
            {
                if (!nodeToId.TryGetValue(edge.output?.node, out var outputId)) continue;
                if (!nodeToId.TryGetValue(edge.input?.node, out var inputId)) continue;

                // ✅ Pulse edges coming *from* the changed field
                if (outputId == fieldId)
                {
                    activePulseEdges[edge] = EditorApplication.timeSinceStartup;
                    changedTimestamps[inputId] = (float)EditorApplication.timeSinceStartup;
                }

                // ✅ Pulse edges going *to* the changed field (e.g. SO ➝ field)
                if (inputId == fieldId)
                {
                    activePulseEdges[edge] = EditorApplication.timeSinceStartup;
                    changedTimestamps[outputId] = (float)EditorApplication.timeSinceStartup;
                }
            }
        }


        private void UpdatePulse()
        {
            double now = EditorApplication.timeSinceStartup;
            foreach (var kvp in activePulseEdges.ToList())
            {
                var edge = kvp.Key;
                double start = kvp.Value;
                double elapsed = now - start;

                // Only hide non-loop edges
                if (elapsed > 6.0 && !edge.IsLoopPulse)
                {
                    edge.HideTrail();
                    activePulseEdges.Remove(edge);
                    continue;
                }

                if (!edge.IsLoopPulse)
                {
                    float t = (float)(elapsed / 3.0);
                    edge.UpdateTrail(t);
                }
            }

            double timeNow = EditorApplication.timeSinceStartup;
            foreach (var id in changedTimestamps.Keys.ToList())
            {
                float elapsed = (float)(timeNow - changedTimestamps[id]);
                if (nodeMap.TryGetValue(id, out var node))
                {
                    if (elapsed > 4f)
                    {
                        node.style.backgroundColor = Color.clear;
                        changedTimestamps.Remove(id);
                    }
                    else
                    {
                        float pulse = Mathf.Abs(Mathf.Sin(elapsed * 5f));
                        node.style.backgroundColor = Color.Lerp(Color.yellow, Color.red, pulse);
                    }
                }
            }
            /*
            foreach (var kvp in fieldValueLabels)
            {
                string fieldId = kvp.Key;
                Label label = kvp.Value;

                var split = fieldId.Split('.');
                if (split.Length != 2) continue;

                string soName = split[0];
                string fieldName = split[1];

                var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                    .FirstOrDefault(s => s.name == soName);
                if (so == null) continue;

                var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) continue;

                var observable = field.GetValue(so);
                if (observable == null) continue;

                var valueProp = observable.GetType().GetProperty("Value");
                if (valueProp == null) continue;

                var value = valueProp.GetValue(observable);
                var valueStr = value?.ToString() ?? "null";

                // Update text only if changed
                if (!lastFieldValues.TryGetValue(fieldId, out var oldValue) || !Equals(oldValue, value))
                {
                    label.text = valueStr;
                    lastFieldValues[fieldId] = value;
                }

                // Emoji logic
                if (fieldValueIcons.TryGetValue(fieldId, out var iconElem))
                {
                    if (value == null)
                    {
                        iconElem.visible = true;
                        (iconElem as Label).text = "⚠️";
                        iconElem.tooltip = "Value is null";
                    }
                    else if (value is bool b)
                    {
                        iconElem.visible = true;
                        (iconElem as Label).text = b ? "✅" : "❌";
                        iconElem.tooltip = b ? "True" : "False";
                    }
                    else
                    {
                        iconElem.visible = false;
                    }
                }
            }
            */
            UpdateFieldLabels();

        }

        public void Populate()
        {
            var previousActiveEdges = new Dictionary<FlowingEdge, double>(activePulseEdges);
            var previousChanged = new Dictionary<string, float>(changedTimestamps);

            graphElements.ToList().ForEach(RemoveElement);
            nodeMap.Clear();
            changedTimestamps.Clear();
            activePulseEdges.Clear();
            allFlowingEdges.Clear();
            fieldToSO.Clear();
            systemToSO.Clear();

            InternalPopulate();

            foreach (var pair in previousChanged)
                if (nodeMap.ContainsKey(pair.Key)) changedTimestamps[pair.Key] = pair.Value;

            foreach (var pair in previousActiveEdges)
            {
                var oldEdge = pair.Key;
                string fromTitle = oldEdge.output?.node?.title;
                string toTitle = oldEdge.input?.node?.title;

                var newEdge = allFlowingEdges.FirstOrDefault(e =>
                    e.output?.node?.title == fromTitle &&
                    e.input?.node?.title == toTitle);

                if (newEdge != null)
                    activePulseEdges[newEdge] = pair.Value;
            }

            schedule.Execute(UpdatePulse).Every(16);
            UpdateFieldLabels();
        }

        public void Populate(HashSet<string> visibleNodes)
        {
            var previousActiveEdges = new Dictionary<FlowingEdge, double>(activePulseEdges);
            var previousChanged = new Dictionary<string, float>(changedTimestamps);

            graphElements.ToList().ForEach(RemoveElement);
            nodeMap.Clear();
            changedTimestamps.Clear();
            activePulseEdges.Clear();
            allFlowingEdges.Clear();
            fieldToSO.Clear();
            systemToSO.Clear();

            InternalPopulate(visibleNodes);

            foreach (var pair in previousChanged)
                if (nodeMap.ContainsKey(pair.Key)) changedTimestamps[pair.Key] = pair.Value;

            foreach (var pair in previousActiveEdges)
            {
                var oldEdge = pair.Key;
                string fromTitle = oldEdge.output?.node?.title;
                string toTitle = oldEdge.input?.node?.title;

                var newEdge = allFlowingEdges.FirstOrDefault(e =>
                    e.output?.node?.title == fromTitle &&
                    e.input?.node?.title == toTitle);

                if (newEdge != null)
                    activePulseEdges[newEdge] = pair.Value;
            }

            schedule.Execute(UpdatePulse).Every(16);
            UpdateFieldLabels();
        }

        private void InternalPopulate(HashSet<string> filter = null)
        {
            var allSOs = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>();
            var allSystemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOfRawGeneric(typeof(SystemBase<>)) && !t.IsAbstract)
                .ToList();

            float xSO = 0f, xField = 300f, xSystem = 600f;
            float y = 0f, verticalSpacing = 160f;

            foreach (var so in allSOs)
            {
                string soId = so.name;
                if (filter != null && !filter.Contains(soId)) continue;

                var soNode = CreateNode($"🧩 {so.name}", NodeType.SO, new Vector2(xSO, y), so.name);
                nodeMap[soId] = soNode;
                AddElement(soNode);

                var fields = so.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(f =>
                        Attribute.IsDefined(f, typeof(ObservableAttribute)) ||
                        Attribute.IsDefined(f, typeof(ObservableSavedAttribute)));

                int fieldIndex = 0;
                foreach (var field in fields)
                {
                    string fieldName = field.Name;
                    string fieldId = $"{so.name}.{fieldName}";
                    if (filter != null && !filter.Contains(fieldId)) continue;

                    float fieldY = y + fieldIndex * verticalSpacing;
                                        

                    var fieldNode = CreateNode($"🔸 {fieldName}", NodeType.Field, new Vector2(xField, fieldY), $"{so.name}.{fieldName}");
                                        
                    nodeMap[fieldId] = fieldNode;
                    AddElement(fieldNode);
                    fieldToSO[fieldId] = soId;

                    Connect(soNode.outputContainer[0] as Port, fieldNode.inputContainer[0] as Port);

                    fieldNode.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button == 1) MarkChanged(fieldId);
                    });

                    int systemIndex = 0;
                    ReaCSDebug.Log($"[ReaCS] Found {allSystemTypes.Count} system types.");
                    foreach (var sysType in allSystemTypes)
                    {
                        ReaCSDebug.Log($"[ReaCS] System: {sysType.FullName}");
                        var attrs = sysType.GetCustomAttributes(typeof(ReactToAttribute), true);
                        foreach (var attr in attrs.Cast<ReactToAttribute>())
                        {
                            ReaCSDebug.Log($" └ Reacts to: {attr.FieldName}");
                            if (attr.FieldName != fieldName)
                            {
                                ReaCSDebug.Log($"   ❌ Skipped (doesn't match field: {fieldName})");
                                continue;
                            }

                            var baseType = sysType.BaseType;
                            if (baseType is { IsGenericType: true })
                            {
                                var soType = baseType.GetGenericArguments()[0];
                                ReaCSDebug.Log($"   ➤ Checks if {soType.Name} is assignable from {so.GetType().Name}");

                                if (!soType.IsAssignableFrom(so.GetType()))
                                {
                                    ReaCSDebug.Log($"   ❌ Skipped (type mismatch)");
                                    continue;
                                }

                                ReaCSDebug.Log($"   ✅ Matched — will create system node!");

                                string sysId = sysType.Name;
                                if (filter != null && !filter.Contains(sysId)) continue;

                                if (!nodeMap.TryGetValue(sysId, out var sysNode))
                                {
                                    float systemY = y + systemIndex * verticalSpacing;
                                    sysNode = CreateNode($"▶ {sysType.Name}", NodeType.System, new Vector2(xSystem, systemY), sysType.FullName);
                                    systemIndex++;
                                    nodeMap[sysId] = sysNode;
                                    AddElement(sysNode);
                                    systemToSO[sysId] = soId;
                                }

                                Connect(fieldNode.outputContainer[0] as Port, sysNode.inputContainer[0] as Port);
                            }
                        }

                        string sourceFieldId = $"{so.name}.{fieldName}";

                        foreach (var otherField in so.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            if (!Attribute.IsDefined(otherField, typeof(ObservableAttribute)) &&
                                !Attribute.IsDefined(otherField, typeof(ObservableSavedAttribute))) continue;

                            string targetFieldId = $"{so.name}.{otherField.Name}";
                            if (targetFieldId == sourceFieldId) continue;

                            if (!fieldToFieldGraph.TryGetValue(sourceFieldId, out var list))
                            {
                                list = new List<string>();
                                fieldToFieldGraph[sourceFieldId] = list;
                            }

                            list.Add(targetFieldId);
                        }
                    }

                    fieldIndex++;
                }

                // Final Y adjustment per SO
                y += Math.Max(fields.Count(), 1) * verticalSpacing;
            }
            UpdateFieldLabels();
        }

        private Node CreateNode(string title, NodeType type, Vector2 position, string nodeId)
        {
            var node = new Node();

            var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "In";
            node.inputContainer.Add(input);

            var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            output.portName = "Out";
            node.outputContainer.Add(output);

            node.userData = type;

            // Default title, may override for fields
            string displayTitle = title;

            if (type == NodeType.SO)
            {
                node.style.backgroundColor = Color.teal;
            }
            else if (type == NodeType.System)
            {
                node.style.backgroundColor = new Color(0.42f, 0.35f, 0.80f);
            }
            else if (type == NodeType.Field)
            {
                var split = nodeId.Split('.');
                if (split.Length == 2)
                {
                    string soName = split[0];
                    string fieldName = split[1];

                    var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                        .FirstOrDefault(s => s.name == soName);

                    if (so != null)
                    {
                        var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            var observable = field.GetValue(so);
                            if (observable != null)
                            {
                                var persistFlag = field.FieldType.GetField("ShouldPersist", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                                if (persistFlag != null && persistFlag.FieldType == typeof(bool))
                                {
                                    bool shouldPersist = (bool)persistFlag.GetValue(observable);
                                    displayTitle = (shouldPersist ? "🔒" : "🔁") + $" {fieldName}";
                                }
                            }
                        }
                    }
                }
            }

            node.title = displayTitle;

            if (type == NodeType.Field)
            {
                var fieldInfoContainer = new VisualElement
                {
                    style = {
                flexDirection = FlexDirection.Column,
                marginTop = 4,
                marginBottom = 4,
                paddingLeft = 6,
                paddingRight = 6
            }
                };

                var valueRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };

                var icon = new Label(""); // ✅ / ❌ / ⚠️
                icon.style.marginRight = 4;
                icon.style.fontSize = 14;

                var valueLabel = new Label("null");
                valueLabel.style.fontSize = 12;
                valueLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                valueRow.Add(icon);
                valueRow.Add(valueLabel);

                var typeRow = new Label
                {
                    style = {
                fontSize = 9,
                color = Color.gray,
                marginTop = 2
            }
                };

                var split = nodeId.Split('.');
                if (split.Length == 2)
                {
                    string soName = split[0];
                    string fieldName = split[1];

                    var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                        .FirstOrDefault(s => s.name == soName);

                    if (so != null)
                    {
                        var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (field != null)
                        {
                            string fType = field.FieldType.IsGenericType
                                ? $"Observable<{field.FieldType.GetGenericArguments()[0].Name}>"
                                : field.FieldType.Name;

                            typeRow.text = $"[{fType}]";
                        }
                    }
                }

                fieldInfoContainer.Add(typeRow);
                fieldInfoContainer.Add(CreateDottedDivider());
                fieldInfoContainer.Add(valueRow);

                node.extensionContainer.Add(fieldInfoContainer);
                node.RefreshExpandedState();

                fieldValueLabels[nodeId] = valueLabel;
                fieldValueIcons[nodeId] = icon;
                fieldValueContainers[nodeId] = valueRow;
            }

            node.capabilities |= Capabilities.Movable | Capabilities.Selectable;
            node.style.paddingTop = 6;
            node.style.paddingBottom = 6;
            node.RefreshExpandedState();
            node.RefreshPorts();
            node.SetPosition(new Rect(position, defaultNodeSize));

            nodeToId[node] = nodeId;

            return node;
        }


        private void UpdateFieldLabels()
        {
            foreach (var kvp in fieldValueLabels)
            {
                string fieldId = kvp.Key;
                Label label = kvp.Value;

                var split = fieldId.Split('.');
                if (split.Length != 2) continue;

                string soName = split[0];
                string fieldName = split[1];

                var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                    .FirstOrDefault(s => s.name == soName);
                if (so == null) continue;

                var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) continue;

                var observable = field.GetValue(so);
                if (observable == null) continue;

                var valueProp = observable.GetType().GetProperty("Value");
                if (valueProp == null) continue;

                var value = valueProp.GetValue(observable);
                var valueStr = value?.ToString() ?? "null";

                label.text = valueStr;

                // Emoji logic
                if (fieldValueIcons.TryGetValue(fieldId, out var iconElem))
                {
                    if (value == null)
                    {
                        iconElem.visible = true;
                        (iconElem as Label).text = "⚠️";
                        iconElem.tooltip = "Value is null";
                    }
                    else if (value is bool b)
                    {
                        iconElem.visible = true;
                        (iconElem as Label).text = b ? "✅" : "❌";
                        iconElem.tooltip = b ? "True" : "False";
                    }
                    else
                    {
                        iconElem.visible = false;
                    }
                }
            }
        }

        private Texture2D MakeDottedLineTexture()
        {
            var tex = new Texture2D(4, 1);
            tex.filterMode = FilterMode.Point;

            var pixels = new Color[4];
            pixels[0] = Color.gray;
            pixels[1] = new Color(0, 0, 0, 0); // transparent
            pixels[2] = Color.gray;
            pixels[3] = new Color(0, 0, 0, 0); // transparent

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void Connect(Port from, Port to)
        {
            var edge = new FlowingEdge();
            AddElement(edge);

            edge.output = from;
            edge.input = to;

            from.Connect(edge);
            to.Connect(edge);

            allFlowingEdges.Add(edge);
        }

        public void ClearAllHighlights()
        {
            changedTimestamps.Clear();

            foreach (var node in nodeMap.Values)
            {
                if (node.userData is NodeType.Field or NodeType.System)
                    node.style.backgroundColor = Color.clear;
            }
        }

        public void FocusSystemGraph(Type systemType)
        {
            if (systemType == null) return;

            var baseType = systemType.BaseType;
            if (baseType is not { IsGenericType: true }) return;

            var soType = baseType.GetGenericArguments()[0];
            var reactAttributes = systemType.GetCustomAttributes(typeof(ReactToAttribute), true)
                                            .Cast<ReactToAttribute>()
                                            .ToList();

            var visibleIds = new HashSet<string>
            {
                systemType.Name // System node
            };

            foreach (var attr in reactAttributes)
            {
                foreach (var so in Resources.FindObjectsOfTypeAll<ObservableScriptableObject>())
                {
                    if (!soType.IsAssignableFrom(so.GetType())) continue;

                    string soId = so.name;
                    string fieldId = $"{so.name}.{attr.FieldName}";

                    visibleIds.Add(fieldId);
                    visibleIds.Add(soId);
                }
            }

            Populate(visibleIds);
            ScrollToNode(systemType.Name);
        }

        public void ScrollToNode(string nodeId)
        {
            if (nodeMap.TryGetValue(nodeId, out var node))
            {
                ClearSelection();
                AddToSelection(node);

                // This line triggers Unity's FrameSelected shortcut behavior
                FrameSelection();
            }
        }

        private void PingNodeAsset(Node node)
        {
            if (!nodeToId.TryGetValue(node, out string nodeId))
            {
                Debug.LogWarning("Unknown node ID.");
                return;
            }

            if (node.userData is not NodeType type)
                return;

            switch (type)
            {
                case NodeType.SO:
                case NodeType.Field:
                    {
                        string soName = type == NodeType.SO
                            ? nodeId
                            : fieldToSO.TryGetValue(nodeId, out var owner) ? owner : null;

                        if (string.IsNullOrEmpty(soName)) return;

                        var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                            .FirstOrDefault(s => s.name == soName);

                        if (so != null)
                            EditorGUIUtility.PingObject(so);

                        break;
                    }

                case NodeType.System:
                    {
                        string systemName = nodeId;

                        var typeObj = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == systemName);

                        if (typeObj != null)
                        {
                            string path = AssetDatabase.FindAssets($"{typeObj.Name} t:Script")
                                .Select(AssetDatabase.GUIDToAssetPath)
                                .FirstOrDefault();

                            if (!string.IsNullOrEmpty(path))
                            {
                                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                                if (script != null)
                                    EditorGUIUtility.PingObject(script);
                            }
                        }

                        break;
                    }
            }
        }

        private VisualElement CreateDottedDivider()
        {
            int width = 32;
            var tex = new Texture2D(width, 1);
            tex.filterMode = FilterMode.Point;

            var pixels = new Color[width];
            for (int i = 0; i < width; i++)
            {
                // Every other pixel is gray, others are transparent
                pixels[i] = (i % 2 == 0) ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0, 0, 0, 0);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            var divider = new VisualElement
            {
                style =
        {
            height = 1,
            marginTop = 4,
            marginBottom = 4,
            backgroundImage = tex,
            unityBackgroundScaleMode = ScaleMode.StretchToFill,
            backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat)
        }
            };

            return divider;
        }

    }
}
#endif