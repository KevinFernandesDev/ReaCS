#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using ReaCS.Runtime.Internal;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;

namespace ReaCS.Editor
{
    public enum NodeType { SO, Field, System }

    public class StaticDependencyGraphView : GraphView
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
        private readonly Dictionary<Node, Color> originalNodeColors = new();
        private Dictionary<string, string> fieldLastChangedBySO = new(); 
        private Dictionary<string, Label> fieldValueLastSO = new();
        private string focusedSOName = null;
        private string currentFocusedSOName = null;
        private bool _isAnimatingView = false;
        private string currentFilter = "";

        // Refactoring for optimization
        private ObservableScriptableObject[] cachedSOs = Array.Empty<ObservableScriptableObject>();
        private readonly Dictionary<Node, List<FlowingEdge>> edgesFromNode = new();
        private Dictionary<string, string> soNameToType = new(); // SO name ➝ type name cache
        private Dictionary<(Type, string), FieldInfo> fieldInfoCache = new(); // (Type, FieldName) ➝ FieldInfo cache                                                                              
        private readonly Stack<Node> nodePool = new(); // Pools to reuse node elements
        private readonly Stack<FlowingEdge> edgePool = new(); // Pools to reuse edges elements
        private readonly Dictionary<(Type, string), FieldInfo> fieldDictInfoCache = new();
        private Dictionary<Type, PropertyInfo> valuePropCache = new(); // Value property cache

        float CenteredStartY(float centerY, int count, float spacing) => centerY - (count * spacing) / 2f;

        public StaticDependencyGraphView()
        {
            TryLoadStyleSheet("ReaCSGraphViewStyles");

            ObservableRegistry.OnEditorFieldChanged += (soName, fieldName) =>
            {
                string fullFieldId = $"{soName}.{fieldName}";
                MarkChanged(fullFieldId);
            };

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

            var editorSystemWarningLabel = new Label("When in EditMode 'Systems' won't trigger their behaviour but you can still see every 'System' in the project reacting to an 'Observable' field that changed for debug purposes.\n" +
                "When in PlayMode only the 'Systems' that are in the current opened scenes will be visible and they will react and trigger their behavior normally to an 'Observable' changing.")
            {
                style = {
                    fontSize = 10,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    color = new Color(1f, 1f, 1f, 0.5f),
                    position = Position.Relative
                }
            };
            editorSystemWarningLabel.style.unityTextAlign = TextAnchor.UpperCenter;
            editorSystemWarningLabel.style.alignSelf = Align.Center;
            editorSystemWarningLabel.style.marginTop = 8;
            editorSystemWarningLabel.style.marginBottom = 4;
            Add(editorSystemWarningLabel);
        }

        private void TryLoadStyleSheet(string resourceName)
        {
            // Try loading from Resources (for dev/in-project context)
            var styleSheet = Resources.Load<StyleSheet>(resourceName);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
                return;
            }

            // Fallback for Package-based .uss
#if UNITY_EDITOR
            string packageRelativePath = "Packages/com.kevinfernandesdev.reacs/Editor/Styles/ReaCSGraphViewStyles.uss";
            styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(packageRelativePath);
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
                return;
            }
#endif

            Debug.LogWarning($"[ReaCS] Could not find style sheet '{resourceName}' in Resources or '{packageRelativePath}' in Package.");
        }

        private Node GetPooledNode()
        {
            return nodePool.Count > 0 ? nodePool.Pop() : new Node();
        }

        private FlowingEdge GetPooledEdge()
        {
            return edgePool.Count > 0 ? edgePool.Pop() : new FlowingEdge();
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

        public void SetFocusedSO(string soName)
        {
            currentFocusedSOName = soName;
            UpdateFieldLabels();
        }

        private void UpdatePulse()
        {
            double now = EditorApplication.timeSinceStartup;

            AnimateEdgeTrails(now);
            AnimateNodePulses(now);
        }

        private void AnimateEdgeTrails(double now)
        {
            foreach (var kvp in activePulseEdges.ToList())
            {
                var edge = kvp.Key;
                double start = kvp.Value;
                double elapsed = now - start;

                if (edge == null)
                {
                    activePulseEdges.Remove(kvp.Key);
                    continue;
                }

                // Stop showing pulse trail after 6 seconds
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
        }

        private void AnimateNodePulses(double now)
        {
            foreach (var id in changedTimestamps.Keys.ToList())
            {
                float elapsed = (float)(now - changedTimestamps[id]);

                if (nodeMap.TryGetValue(id, out var node))
                {
                    if (elapsed > 4f)
                    {
                        if (originalNodeColors.TryGetValue(node, out var baseColor))
                            SetNodeColor(node, baseColor);
                        else
                            SetNodeColor(node, Color.clear);

                        changedTimestamps.Remove(id);
                    }
                    else
                    {
                        float pulse = Mathf.Abs(Mathf.Sin(elapsed * 5f));
                        SetNodeColor(node, Color.Lerp(Color.yellow, Color.red, pulse));
                    }
                }
            }
        }

        public void Populate(bool isInitialLoad = false, bool triggerPulse = false)
        {
            PopulateInternal(null, isInitialLoad, triggerPulse);
        }

        public void Populate(HashSet<string> visibleNodes, bool triggerPulse = false)
        {
            PopulateInternal(visibleNodes, false, triggerPulse);
        }

        private void PopulateInternal(HashSet<string> visibleNodes, bool isInitialLoad, bool triggerPulse)
        {
            originalNodeColors.Clear();
            var previousActiveEdges = new Dictionary<FlowingEdge, double>(activePulseEdges);
            var previousChanged = new Dictionary<string, float>(changedTimestamps);

            // Pooling nodes for performance
            foreach (var node in nodeMap.Values)
                nodePool.Push(node);

            // Pooling edges for performance
            foreach (var edge in allFlowingEdges)
                edgePool.Push(edge);

            graphElements.ToList().ForEach(RemoveElement); 

            nodeMap.Clear();
            changedTimestamps.Clear();
            activePulseEdges.Clear();
            allFlowingEdges.Clear();
            fieldToSO.Clear();
            systemToSO.Clear();

            cachedSOs = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>();
            soNameToType = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                .GroupBy(s => s.name)
                .ToDictionary(g => g.Key, g => g.First().GetType().Name); fieldInfoCache.Clear();

            if (visibleNodes != null)
                CorePopulate(visibleNodes);
            else
                CorePopulate();

            foreach (var kvp in fieldToSO.ToList())
            {
                string fieldId = kvp.Key;
                string soName = kvp.Value;
                string fieldName = fieldId.Split('.').Last();

                var soObj = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == soName);

                if (soObj == null) continue;

                string groupedFieldId = $"group:{soObj.GetType().Name}.{fieldName}";

                if (!fieldLastChangedBySO.ContainsKey(fieldId))
                    fieldLastChangedBySO[fieldId] = soName;

                if (!fieldLastChangedBySO.ContainsKey(groupedFieldId))
                    fieldLastChangedBySO[groupedFieldId] = soName;

                lastFieldValues[fieldId] = TryGetFieldValue(soObj, fieldName);
                lastFieldValues[groupedFieldId] = TryGetFieldValue(soObj, fieldName);

                if (triggerPulse)
                {
                    MarkChanged(fieldId);
                }
            }

            RestorePreviousPulseState(previousChanged, previousActiveEdges);

            schedule.Execute(UpdatePulse).Every(16);
            UpdateFieldLabels();
            schedule.Execute(() => AnimateFrameAllNodes()).ExecuteLater(100);
        }

        private void RestorePreviousPulseState(Dictionary<string, float> previousChanged, Dictionary<FlowingEdge, double> previousEdges)
        {
            foreach (var pair in previousChanged)
                if (nodeMap.ContainsKey(pair.Key)) changedTimestamps[pair.Key] = pair.Value;

            foreach (var pair in previousEdges)
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
        }

        public void MarkChanged(string fieldId)
        {
            ReaCSDebug.Log($"[GraphView] MarkChanged called for {fieldId}");

            string soName = currentFocusedSOName;
            if (string.IsNullOrEmpty(soName))
            {
                if (fieldToSO.TryGetValue(fieldId, out var mappedSO))
                    soName = mappedSO;
                else if (fieldId.Contains("."))
                    soName = fieldId.Split('.').First();
            }

            if (string.IsNullOrEmpty(soName))
                return;

            string fieldName = fieldId.Split('.').Last();
            string groupedFieldId = $"group:{GetSOType(soName)}.{fieldName}";

            // Update last changer metadata// Update SO ownership and last changer metadata
            fieldToSO[fieldId] = soName;
            fieldToSO[groupedFieldId] = soName;

            fieldLastChangedBySO[fieldId] = soName;
            fieldLastChangedBySO[groupedFieldId] = soName;

            var soObj = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                      .FirstOrDefault(s => s.name == soName);
            if (soObj != null)
            {
                lastFieldValues[fieldId] = TryGetFieldValue(soObj, fieldName);
                lastFieldValues[groupedFieldId] = TryGetFieldValue(soObj, fieldName);
            }

            // Pulse all edges/nodes starting from both field IDs (solo and group)
            PulseConnectedEdges(fieldId);
            PulseConnectedEdges(groupedFieldId);

            UpdatePulse();
            UpdateFieldLabels();
        }

        private void PulseConnectedEdges(string primaryFieldId)
        {
            double now = EditorApplication.timeSinceStartup;

            if (!fieldToSO.TryGetValue(primaryFieldId, out var soName))
                return;

            string fieldName = primaryFieldId.Split('.').Last();
            string groupType = GetSOType(soName);
            string groupFieldId = $"group:{groupType}.{fieldName}";
            string proxyId = $"group:{groupType}:proxy";

            // Determine the correct fieldId to pulse
            string fieldIdToUse = nodeMap.ContainsKey(primaryFieldId) ? primaryFieldId :
                                  nodeMap.ContainsKey(groupFieldId) ? groupFieldId : null;

            if (string.IsNullOrEmpty(fieldIdToUse)) return;

            // Start from SO node, not proxy
            if (!nodeMap.TryGetValue(soName, out var startNode))
                return;

            var visited = new HashSet<Node>();
            var toVisit = new Queue<Node>();
            toVisit.Enqueue(startNode);

            while (toVisit.Count > 0)
            {
                var current = toVisit.Dequeue();
                if (!visited.Add(current)) continue;

                string currentId = nodeToId.GetValueOrDefault(current);
                if (!string.IsNullOrEmpty(currentId))
                    changedTimestamps[currentId] = (float)now;

                foreach (var edge in allFlowingEdges)
                {
                    bool isFrom = edge.output?.node == current;
                    if (!isFrom) continue;

                    var next = edge.input?.node;
                    if (next == null || visited.Contains(next)) continue;

                    string fromId = nodeToId.GetValueOrDefault(current);
                    string toId = nodeToId.GetValueOrDefault(next);

                    // ✅ Block unrelated fanout at SO node
                    if (fromId == soName && toId != proxyId && toId != primaryFieldId)
                        continue;

                    // ✅ Block unrelated fanout at proxy node
                    if (fromId == proxyId && toId != groupFieldId)
                        continue;

                    // ✅ Block invalid fanout from field
                    if ((fromId == groupFieldId || fromId == primaryFieldId) &&
                        !(toId?.Contains("System") == true || toId?.Contains("sysProxy") == true))
                        continue;

                    activePulseEdges[edge] = now;
                    toVisit.Enqueue(next);
                }
            }
        }

        private string GetSOType(string soName)
        {
            return soNameToType.TryGetValue(soName, out var type) ? type : "UnknownSOType";
        }

        private object TryGetFieldValue(ObservableScriptableObject so, string fieldName)
        {
            if (so == null || string.IsNullOrEmpty(fieldName))
                return null;

            var fieldKey = (so.GetType(), fieldName);

            if (!fieldDictInfoCache.TryGetValue(fieldKey, out var field))
            {
                field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) return null;
                fieldDictInfoCache[fieldKey] = field;
            }

            var observable = field.GetValue(so);
            if (observable == null) return null;

            var obsType = observable.GetType();
            if (!valuePropCache.TryGetValue(obsType, out var valueProp))
            {
                valueProp = obsType.GetProperty("Value");
                valuePropCache[obsType] = valueProp;
            }

            return valueProp?.GetValue(observable);
        }





        private void CorePopulate(HashSet<string> filter = null)
        {
            var allSOs = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                      .Where(so => so != null)
                      .ToArray();
            var allSystemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOfRawGeneric(typeof(Reactor<>)) && !t.IsAbstract)
                .ToList();

            var groupedByType = allSOs
                .GroupBy(so => so.GetType())
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var sysType in allSystemTypes)
            {
                var attr = sysType.GetCustomAttribute<ReactToAttribute>();
                if (attr == null) continue;
                var baseSOType = sysType.BaseType?.GetGenericArguments()[0];
                if (baseSOType == null) continue;
                if (!groupedByType.ContainsKey(baseSOType))
                {
                    groupedByType[baseSOType] = new List<ObservableScriptableObject>();
                }
            }

            float xGroup = 0f;
            float xProxy = xGroup + 420f;
            float xField = xProxy + 300f;
            float xSystem = xField + 400f;
            float xSystemGroup = xSystem + 260f;
            float y = 0f;
            float verticalSpacing = 140f;

            if (filter != null)
            {
                var allowedTypes = new HashSet<Type>(
                    Resources.FindObjectsOfTypeAll<ObservableScriptableObject>()
                        .Where(so =>
                            filter.Contains(so.name) ||
                            GetObservedFields(so).Any(f => filter.Contains($"{so.name}.{f.Name}")))
                        .Select(so => so.GetType())
                );

                groupedByType = groupedByType
                    .Where(kvp => allowedTypes.Contains(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }

            var systemsPerField = new Dictionary<FieldReactionKey, List<Type>>();
            foreach (var sysType in allSystemTypes)
            {
                var attr = sysType.GetCustomAttribute<ReactToAttribute>();
                if (attr == null) continue;

                var baseSOType = sysType.BaseType?.GetGenericArguments()[0];
                if (baseSOType == null) continue;

                var key = new FieldReactionKey(attr.FieldName, baseSOType);
                if (!systemsPerField.TryGetValue(key, out var fieldList))
                    systemsPerField[key] = fieldList = new List<Type>();

                fieldList.Add(sysType);
            }

            foreach (var kvp in groupedByType.ToList())
            {
                var soType = kvp.Key;
                var allTypeSOs = kvp.Value;
                var fieldNames = allTypeSOs
                    .SelectMany(so => GetObservedFields(so))
                    .Select(f => f.Name)
                    .Distinct()
                    .OrderBy(n => n)
                    .ToList();

                string groupTypeName = soType.Name;
                string groupKey = $"group:{groupTypeName}";
                string proxyId = $"{groupKey}:proxy";

                float groupHeight = allTypeSOs.Count * verticalSpacing;
                float fieldBlockHeight = fieldNames.Count * verticalSpacing;
                float maxHeight = Mathf.Max(groupHeight, fieldBlockHeight);

                float fieldStartY = y + (maxHeight - fieldBlockHeight) / 2f;
                float proxyY = y + (maxHeight - verticalSpacing) / 2f;

                bool shouldGroup = allTypeSOs.Count > 1;
                VisualElement soContainer = null;

                if (shouldGroup)
                {
                    var groupBox = new Group();
                    groupBox.SetPosition(new Rect(xGroup - 100f, y, 260, groupHeight + 40));
                    var titleLabel = new Label($"🧩 {groupTypeName} ({allTypeSOs.Count})");
                    titleLabel.AddToClassList("reaCS-group-title");
                    groupBox.Insert(0, titleLabel);
                    groupBox.AddToClassList("reaCS-group-box");
                    AddElement(groupBox);
                    soContainer = groupBox;
                }

                var proxyNode = CreateNode($"📦 {groupTypeName} (Group)", NodeType.SO, new Vector2(xProxy, proxyY), proxyId);
                nodeMap[proxyId] = proxyNode;
                AddElement(proxyNode);

                float localY = y;
                foreach (var so in allTypeSOs)
                {
                    var soNode = CreateNode($"🧩 {so.name}", NodeType.SO, new Vector2(xGroup + 20, localY), so.name);
                    nodeMap[so.name] = soNode;
                    AddElement(soNode);
                    if (shouldGroup) ((Group)soContainer).AddElement(soNode);

                    var traceEdge = Connect(soNode.outputContainer[0] as Port, proxyNode.inputContainer[0] as Port);
                    traceEdge.pickingMode = PickingMode.Ignore;
                    traceEdge.style.opacity = 0.3f;
                    traceEdge.style.borderBottomWidth = 0.5f;

                    localY += verticalSpacing;
                }

                for (int i = 0; i < fieldNames.Count; i++)
                {
                    string fieldName = fieldNames[i];
                    string fieldId = $"{groupKey}.{fieldName}";
                    if (filter != null && !filter.Contains(fieldId)) continue;

                    var fieldSystems = systemsPerField
                        .Where(kvp => kvp.Key.FieldName == fieldName && soType.IsAssignableFrom(kvp.Key.SOType))
                        .SelectMany(kvp => kvp.Value)
                        .Distinct()
                        .ToList();

                    bool hasGroup = fieldSystems.Count > 1;
                    bool hasSingleSystem = fieldSystems.Count == 1;

                    float groupSysHeight = hasGroup ? fieldSystems.Count * verticalSpacing + 40f : 0f;
                    float singleHeight = hasSingleSystem ? verticalSpacing : 0f;
                    float fieldHeight = verticalSpacing;
                    float blockHeight = Mathf.Max(groupSysHeight, singleHeight, fieldHeight);
                    float blockPadding = 80f;
                    float totalBlockHeight = blockHeight + blockPadding;
                    float blockCenterY = fieldStartY + totalBlockHeight / 2f;

                    var fieldNode = CreateNode($"🔸 {fieldName}", NodeType.Field, new Vector2(xField, blockCenterY), fieldId);
                    nodeMap[fieldId] = fieldNode;
                    AddElement(fieldNode);

                    if (!fieldToSO.ContainsKey(fieldId))
                        fieldToSO[fieldId] = $"<group:{groupTypeName}>";

                    if (!string.IsNullOrEmpty(currentFocusedSOName))
                    {
                        string focusedFieldId = $"{currentFocusedSOName}.{fieldName}";
                        if (!fieldToSO.ContainsKey(focusedFieldId))
                            fieldToSO[focusedFieldId] = currentFocusedSOName;
                    }

                    Connect(proxyNode.outputContainer[0] as Port, fieldNode.inputContainer[0] as Port);

                    if (hasSingleSystem)
                    {
                        var sysType = fieldSystems[0];
                        string sysId = sysType.Name;

                        if (!nodeMap.TryGetValue(sysId, out var sysNode))
                        {
                            var systemNode = CreateNode($"🧪 {sysType.Name}", NodeType.System, new Vector2(xSystemGroup, blockCenterY), sysType.FullName);
                            nodeMap[sysId] = systemNode;
                            AddElement(systemNode);
                            systemToSO[sysId] = allTypeSOs.FirstOrDefault()?.name ?? groupKey;
                        }

                        Connect(fieldNode.outputContainer[0] as Port, nodeMap[sysId].inputContainer[0] as Port);
                    }
                    else if (hasGroup)
                    {
                        string sysGroupId = $"{groupKey}.{fieldName}.sysProxy";

                        var proxySystemNode = CreateNode($"📦 Systems ({fieldName})", NodeType.System, new Vector2(xSystem, blockCenterY), sysGroupId);
                        nodeMap[sysGroupId] = proxySystemNode;
                        AddElement(proxySystemNode);
                        Connect(fieldNode.outputContainer[0] as Port, proxySystemNode.inputContainer[0] as Port);

                        if (fieldSystems.Count > 1)
                        {
                            var sysGroupBox = new Group();
                            sysGroupBox.SetPosition(new Rect(xSystemGroup, blockCenterY - groupSysHeight / 2f, 260, groupSysHeight + 40));
                            var label = new Label($"🧪 {fieldName} Systems ({fieldSystems.Count})");
                            label.AddToClassList("reaCS-group-title");
                            sysGroupBox.Insert(0, label);
                            sysGroupBox.AddToClassList("reaCS-group-box");
                            AddElement(sysGroupBox);

                            float sysLocalY = blockCenterY - (fieldSystems.Count * verticalSpacing) / 2f;
                            foreach (var sysType in fieldSystems)
                            {
                                string sysId = sysType.Name;
                                if (!nodeMap.TryGetValue(sysId, out var sysNode))
                                {
                                    sysNode = CreateNode($"🧪 {sysType.Name}", NodeType.System, new Vector2(xSystemGroup + 20, sysLocalY), sysType.FullName);
                                    nodeMap[sysId] = sysNode;
                                    AddElement(sysNode);
                                    systemToSO[sysId] = allTypeSOs.FirstOrDefault()?.name ?? groupKey;
                                }

                                sysGroupBox.AddElement(sysNode);
                                var traceEdge = Connect(proxySystemNode.outputContainer[0] as Port, sysNode.inputContainer[0] as Port);
                                traceEdge.pickingMode = PickingMode.Ignore;
                                traceEdge.style.opacity = 0.3f;

                                sysLocalY += verticalSpacing;
                            }

                            CenterSystemGroupToProxy(sysGroupId);
                        }
                        else
                        {
                            var sysType = fieldSystems[0];
                            string sysId = sysType.Name;
                            if (!nodeMap.TryGetValue(sysId, out var sysNode))
                            {
                                var systemNode = CreateNode($"🧪 {sysType.Name}", NodeType.System, new Vector2(xSystemGroup, blockCenterY), sysType.FullName);
                                nodeMap[sysId] = systemNode;
                                AddElement(systemNode);
                                systemToSO[sysId] = allTypeSOs.FirstOrDefault()?.name ?? groupKey;
                            }
                            Connect(proxySystemNode.outputContainer[0] as Port, nodeMap[sysId].inputContainer[0] as Port);
                        }
                    }

                    fieldStartY += totalBlockHeight;
                }

                y += maxHeight + 140;
            }

            UpdateFieldLabels();
        }



        private void CenterSOGroupToProxy(string proxyId, string groupKey, Group groupBox, List<ObservableScriptableObject> allTypeSOs)
        {
            schedule.Execute(() =>
            {
                schedule.Execute(() =>
                {
                    if (!nodeMap.TryGetValue(proxyId, out var proxyNode)) return;

                    // Step 1: Recenter proxy to average Y of connected fields
                    var connectedFieldYs = nodeMap
                        .Where(kvp => kvp.Key.StartsWith($"{groupKey}.") && kvp.Value != null)
                        .Where(kvp => allFlowingEdges.Any(edge =>
                            edge.output?.node == proxyNode && edge.input?.node == kvp.Value))
                        .Select(kvp => kvp.Value.GetPosition().center.y)
                        .ToList();

                    if (connectedFieldYs.Count > 0)
                    {
                        float avgY = connectedFieldYs.Average();
                        var proxyPos = proxyNode.GetPosition();
                        proxyNode.SetPosition(new Rect(new Vector2(proxyPos.x, avgY - proxyPos.height / 2f), proxyPos.size));
                    }

                    // Step 2: Delay again to get updated proxy position, shift SO nodes to match
                    schedule.Execute(() =>
                    {
                        var proxyY = proxyNode.GetPosition().center.y;

                        var soNodes = allTypeSOs
                            .Where(so => nodeMap.ContainsKey(so.name))
                            .Select(so => nodeMap[so.name])
                            .ToList();

                        if (soNodes.Count > 0)
                        {
                            float groupCenterY = soNodes.Select(n => n.GetPosition().center.y).Average();
                            float deltaY = proxyY - groupCenterY;

                            foreach (var soNode in soNodes)
                            {
                                var rect = soNode.GetPosition();
                                soNode.SetPosition(new Rect(new Vector2(rect.x, rect.y + deltaY), rect.size));
                            }
                        }
                    }).ExecuteLater(20);

                }).ExecuteLater(20);
            }).ExecuteLater(10);
        }

        private void CenterSystemGroupToProxy(string proxySystemId)
        {
            schedule.Execute(() =>
            {
                schedule.Execute(() =>
                {
                    if (!nodeMap.TryGetValue(proxySystemId, out var proxyNode)) return;

                    // Step 1: Recenter system group contents based on updated proxy Y
                    schedule.Execute(() =>
                    {
                        float proxyY = proxyNode.GetPosition().center.y;

                        // Find all systems connected from this proxy
                        var systemNodes = allFlowingEdges
                            .Where(edge => edge.output?.node == proxyNode)
                            .Select(edge => edge.input?.node)
                            .Where(n => n != null && nodeMap.ContainsValue(n))
                            .ToList();

                        if (systemNodes.Count == 0) return;

                        float groupCenterY = systemNodes.Select(n => n.GetPosition().center.y).Average();
                        float deltaY = proxyY - groupCenterY;

                        foreach (var sysNode in systemNodes)
                        {
                            var rect = sysNode.GetPosition();
                            sysNode.SetPosition(new Rect(new Vector2(rect.x, rect.y + deltaY), rect.size));
                        }

                    }).ExecuteLater(20);

                }).ExecuteLater(20);
            }).ExecuteLater(10);
        }

        private List<FieldInfo> GetObservedFields(ObservableScriptableObject so)
        {
            return so.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => Attribute.IsDefined(f, typeof(ObservableAttribute)) || Attribute.IsDefined(f, typeof(ObservableSavedAttribute)))
                .ToList();
        }

        private void SetNodeColor(Node node, Color color)
        {
            //node.style.backgroundColor = color;
            node.mainContainer.style.backgroundColor = color;
            node.titleContainer.style.backgroundColor = color;

            // ✅ Only store original color if it hasn't been stored yet
            if (!originalNodeColors.ContainsKey(node))
                originalNodeColors[node] = color;
        }

        private Node CreateNode(string title, NodeType type, Vector2 position, string nodeId)
        {
            // Get Pooled nodes for performance
            var node = GetPooledNode();

            // Cleanup reused nodes from pool
            node.inputContainer.Clear();
            node.outputContainer.Clear();
            node.extensionContainer.Clear();

            var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            input.portName = "In";
            node.inputContainer.Add(input);

            var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            output.portName = "Out";
            node.outputContainer.Add(output);

            node.userData = type;

            string displayTitle = title;
            string soName = null;
            string fieldName = null;

            if (type == NodeType.SO)
            {
                SetNodeColor(node, Color.teal);
            }
            else if (type == NodeType.System)
            {
                SetNodeColor(node, new Color(0.22f, 0.15f, 0.60f));
            }
            else if (type == NodeType.Field)
            {
                SetNodeColor(node, new Color(0.17f, 0.17f, 0.17f));

                var split = nodeId.Split('.');
                if (split.Length >= 2)
                {
                    // Support for group:Type.fieldName or normal SO.fieldName
                    fieldName = split[^1];
                    if (fieldToSO.TryGetValue(nodeId, out var resolvedSO))
                        soName = resolvedSO;
                }

                if (!string.IsNullOrEmpty(soName) && !string.IsNullOrEmpty(fieldName))
                {
                    var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == soName);

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
                var icon = new Label("") { style = { marginRight = 4, fontSize = 14 } };
                var valueLabel = new Label("null") { style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold } };

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

                if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(soName))
                {
                    var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == soName);
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

                var lastSONameLabel = new Label("")
                {
                    style = {
                fontSize = 9,
                color = Color.gray,
                marginTop = 2,
                unityTextAlign = TextAnchor.MiddleRight
            }
                };
                fieldInfoContainer.Add(lastSONameLabel);
                fieldValueLastSO[nodeId] = lastSONameLabel;

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

        private string ResolveLastChangedSO(string fieldId)
        {
            if (fieldLastChangedBySO.TryGetValue(fieldId, out var so)) return so;

            // If grouped field, resolve from known last-changed SO for specific instances
            if (fieldId.StartsWith("group:") && fieldId.Contains("."))
            {
                string typeName = fieldId.Split(':')[1].Split('.')[0];
                string fieldName = fieldId.Split('.').Last();

                var candidates = fieldLastChangedBySO
                    .Where(kvp => kvp.Key.EndsWith($".{fieldName}"))
                    .Where(kvp =>
                    {
                        var soObj = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == kvp.Value);
                        return soObj != null && soObj.GetType().Name == typeName;
                    });

                var latest = candidates.OrderByDescending(kvp => EditorApplication.timeSinceStartup).FirstOrDefault();
                return latest.Value;
            }

            return null;
        }

        private void UpdateFieldLabels()
        {
            foreach (var kvp in fieldValueLabels)
            {
                string fieldId = kvp.Key;
                Label label = kvp.Value;

                string fieldName = fieldId.Split('.').Last();
                string resolvedFieldId = fieldId;
                string soName = null;

                if (!string.IsNullOrEmpty(currentFocusedSOName))
                {
                    // 🟡 Focus mode: use focused SO and re-map fieldId if it's a grouped node
                    soName = currentFocusedSOName;

                    if (fieldId.StartsWith("group:"))
                    {
                        // Convert from group:Type.fieldName → soName.fieldName
                        resolvedFieldId = $"{soName}.{fieldName}";
                    }
                }
                else
                {
                    // 🔵 Project mode: use last-changed SO
                    soName = ResolveLastChangedSO(fieldId);
                }

                if (string.IsNullOrEmpty(fieldName) || string.IsNullOrEmpty(soName))
                    continue;

                var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == soName);
                if (so == null) continue;

                var field = so.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null) continue;

                var observable = field.GetValue(so);
                if (observable == null) continue;

                var valueProp = observable.GetType().GetProperty("Value");
                if (valueProp == null) continue;

                var value = valueProp.GetValue(observable);
                var valueStr = value?.ToString() ?? "null";

                // 🔁 Force refresh internal tracking so grouped fields show focused SO value
                lastFieldValues[resolvedFieldId] = value;
                fieldLastChangedBySO[resolvedFieldId] = soName;
                fieldLastChangedBySO[fieldId] = soName; // ensure both group and raw are synced
                ReaCSDebug.Log($"[UpdateFieldLabels] {fieldId} (resolved: {resolvedFieldId}) → Value from SO: {soName} = {valueStr}");

                label.text = valueStr ?? "--"; ;

                // 🟢 Update icon
                if (fieldValueIcons.TryGetValue(fieldId, out var iconElem) ||
                    fieldValueIcons.TryGetValue(resolvedFieldId, out iconElem))
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

                // 🟣 Update SO label
                if (fieldValueLastSO.TryGetValue(fieldId, out var lastLabel) ||
                    fieldValueLastSO.TryGetValue(resolvedFieldId, out lastLabel))
                {
                    if (!string.IsNullOrEmpty(currentFocusedSOName))
                        lastLabel.text = $"Focused: {currentFocusedSOName}";
                    else if (fieldLastChangedBySO.TryGetValue(fieldId, out var lastSo))
                        lastLabel.text = $"Last: {lastSo}";
                    else
                        lastLabel.text = "";

                    lastLabel.style.display = DisplayStyle.Flex;
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

        private FlowingEdge Connect(Port from, Port to, Action<FlowingEdge> customize = null)
        {
            var edge = GetPooledEdge();
            edge.output = from;
            edge.input = to;

            from.Connect(edge);
            to.Connect(edge);
            AddElement(edge);

            allFlowingEdges.Add(edge);

            var node = from.node;
            if (node != null)
            {
                if (!edgesFromNode.TryGetValue(node, out var list))
                    edgesFromNode[node] = list = new List<FlowingEdge>();
                list.Add(edge);
            }

            customize?.Invoke(edge);
            return edge;
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

        #region Zoom & Pan Focus
        public void AnimateFrameAllNodes(long delayMs = 100, float padding = 80f)
        {
            schedule.Execute(() =>
            {
                FrameAllNodes(padding);
            }).ExecuteLater(delayMs);
        }

        public void ScrollToNode(string nodeId)
        {
            if (nodeMap.TryGetValue(nodeId, out var node))
            {
                ClearSelection();
                AddToSelection(node);

                // This line triggers Unity's FrameSelected shortcut behavior
                ZoomToNode(nodeId);
            }
        }

        public void ZoomToNode(string nodeId, float padding = 60f)
        {
            if (!nodeMap.TryGetValue(nodeId, out var node))
                return;

            var rect = node.GetPosition();

            // Expand a little padding around the node
            rect.xMin -= padding;
            rect.yMin -= padding;
            rect.xMax += padding;
            rect.yMax += padding;

            ZoomToRect(rect);
        }

        private void AnimateZoomAndPan(Vector2 targetOffset, Vector3 targetScale, float duration = 2f)
        {
            if (_isAnimatingView) return;

            _isAnimatingView = true;

            Vector2 startOffset = contentViewContainer.resolvedStyle.translate;
            Vector3 startScale = contentViewContainer.resolvedStyle.scale.value;

            double startTime = EditorApplication.timeSinceStartup;

            void Step()
            {
                double now = EditorApplication.timeSinceStartup;
                float elapsed = (float)(now - startTime);

                // Ease the animation with some bounce
                float rawT = Mathf.Clamp01((float)(EditorApplication.timeSinceStartup - startTime) / duration);
                float easedT = EaseOutBack(rawT);

                easedT = Mathf.SmoothStep(0, 1, easedT);

                Vector2 newOffset = Vector2.LerpUnclamped(startOffset, targetOffset, EaseOutBack(easedT));
                Vector3 newScale = Vector3.LerpUnclamped(startScale, targetScale, EaseOutBack(easedT));

                UpdateViewTransform(newOffset, newScale);

                if (easedT >= 1f)
                {
                    EditorApplication.update -= Step;
                    _isAnimatingView = false;
                }
            }

            EditorApplication.update += Step;
        }

        public void FrameAllNodes(float padding = 80f)
        {
            var nodeRects = graphElements
                .OfType<Node>()
                .Select(n => n.GetPosition())
                .ToList();

            if (nodeRects.Count == 0) return;

            var totalBounds = EncapsulateAll(nodeRects);

            // Add padding
            totalBounds.xMin -= padding;
            totalBounds.yMin -= padding;
            totalBounds.xMax += padding;
            totalBounds.yMax += padding;

            ZoomToRect(totalBounds);
        }

        private static Rect EncapsulateAll(IEnumerable<Rect> rects)
        {
            var enumerator = rects.GetEnumerator();
            if (!enumerator.MoveNext()) return Rect.zero;

            var total = enumerator.Current;
            while (enumerator.MoveNext())
            {
                total = RectUtils.Encapsulate(total, enumerator.Current);
            }

            return total;
        }

        public static class RectUtils
        {
            public static Rect Encapsulate(Rect a, Rect b)
            {
                float xMin = Mathf.Min(a.xMin, b.xMin);
                float yMin = Mathf.Min(a.yMin, b.yMin);
                float xMax = Mathf.Max(a.xMax, b.xMax);
                float yMax = Mathf.Max(a.yMax, b.yMax);
                return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            }
        }

        private void ZoomToRect(Rect rect)
        {
            var viewSize = layout.size;

            float zoomX = viewSize.x / rect.width;
            float zoomY = viewSize.y / rect.height;
            float targetZoom = Mathf.Min(zoomX, zoomY);
            targetZoom = Mathf.Clamp(targetZoom, minScale, maxScale);

            Vector2 center = rect.center;
            Vector2 targetOffset = viewSize / 2f - center * targetZoom;

            AnimateZoomAndPan(targetOffset, new Vector3(targetZoom, targetZoom, Mathf.PI));
        }

        public static float EaseOutBack(float t, float overshoot = 1.70158f)
        {
            t -= 1;
            return t * t * ((overshoot + 1) * t + overshoot) + 1;
        }
        #endregion

        public HashSet<string> BuildFilterSetForSO(ObservableScriptableObject so)
        {
            var visible = new HashSet<string>();
            var soType = so.GetType();
            string groupKey = $"group:{soType.Name}";

            visible.Add(so.name); // The SO node
            visible.Add($"{groupKey}:proxy"); // SO group proxy

            var fields = GetObservedFields(so);

            foreach (var field in fields)
            {
                string fieldId = $"{so.name}.{field.Name}";
                string groupedFieldId = $"{groupKey}.{field.Name}";
                string groupedSysProxy = $"{groupKey}.{field.Name}.sysProxy";

                visible.Add(fieldId);
                visible.Add(groupedFieldId);

                var matchingSystems = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .Where(t =>
                    {
                        var attr = t.GetCustomAttribute<ReactToAttribute>();
                        if (attr == null) return false;

                        var baseType = t.BaseType;
                        if (baseType == null || !baseType.IsGenericType) return false;

                        var args = baseType.GetGenericArguments();
                        if (args.Length == 0) return false;

                        return attr.FieldName == field.Name && args[0].IsAssignableFrom(soType);
                    })
                    .ToList();

                foreach (var sysType in matchingSystems)
                {
                    visible.Add(sysType.FullName); // Correct ID used in nodeMap
                }

                if (matchingSystems.Count > 1)
                {
                    visible.Add(groupedSysProxy); // System proxy node
                }
            }

            return visible;
        }

        public HashSet<string> BuildFilterSetForSystem(Type systemType)
        {
            var visible = new HashSet<string>();

            // System node ID
            visible.Add(systemType.Name);

            var attr = systemType.GetCustomAttribute<ReactToAttribute>();
            if (attr == null) return visible;

            string fieldName = attr.FieldName;

            // Detect the target SO type
            var soType = systemType.BaseType?.GetGenericArguments()[0];
            if (soType == null) return visible;

            foreach (var so in Resources.FindObjectsOfTypeAll<ObservableScriptableObject>())
            {
                if (!soType.IsAssignableFrom(so.GetType())) continue;

                string soId = so.name;
                string soGroupKey = $"group:{so.GetType().Name}";
                string fieldId = $"{soGroupKey}.{fieldName}";
                string sysProxyId = $"{soGroupKey}.{fieldName}.sysProxy";

                // Core nodes
                visible.Add(soId);
                visible.Add(soGroupKey + ":proxy"); // SO group proxy
                visible.Add(fieldId);

                // Add system group proxy only if used
                if (nodeMap.ContainsKey(sysProxyId))
                    visible.Add(sysProxyId);
            }

            return visible;
        }

        private void PingNodeAsset(Node node)
        {
            if (!nodeToId.TryGetValue(node, out string nodeId))
                return;

            if (node.userData is not NodeType type)
                return;

            // 🚫 Skip proxy nodes
            if (nodeId.Contains(".sysProxy") || nodeId.Contains(":proxy"))
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

                        var so = Resources.FindObjectsOfTypeAll<ObservableScriptableObject>().FirstOrDefault(s => s.name == soName);

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

    struct FieldReactionKey
    {
        public string FieldName;
        public Type SOType;

        public FieldReactionKey(string fieldName, Type soType)
        {
            FieldName = fieldName;
            SOType = soType;
        }

        public override int GetHashCode() => FieldName.GetHashCode() ^ SOType.GetHashCode();
        public override bool Equals(object obj) =>
            obj is FieldReactionKey other &&
            other.FieldName == FieldName &&
            other.SOType == SOType;
    }
}
#endif