// ExecutionTraceGraphView.cs
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using ReaCS.Runtime.Internal;
using UnityEditor;
using System.Linq;
using ReaCS.Runtime.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReaCS.Editor
{
    public enum TypeOfEntry { System, Field }
    public class ExecutionTraceGraphView : GraphView
    {
        private int xSpacing = 200;
        private int xSpacingPerCharacter = 10;
        private int ySpacing = 200;
        private Port lastPortOut;
        private VisualElement hoverCard;
        private Label hoverCardLabel;
        Dictionary<int, float> depthMaxRightX = new(); // max X so far per depth

        //private readonly Dictionary<string, string> codePreviewCache = new();
        private Dictionary<string, VisualElement> codePreviewCache = new();
        private readonly HashSet<string> knownParams = new();

        public ExecutionTraceGraphView()
        {
            style.flexGrow = 1;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            InitializeHoverCard();

            ReaCSBurstHistory.OnEditorLogUpdated += Rebuild;
            Rebuild();
        }

        public void Cleanup()
        {
            ReaCSBurstHistory.OnEditorLogUpdated -= Rebuild;
        }

        private void Rebuild()
        {
            DeleteElements(graphElements);

            var entries = ReaCSBurstHistory.ToArray();
            int frameThreshold = 30;
            int lastFrame = -1;

            if (entries == null || entries.Length == 0) return;

            int ySpacing = 180;

            var systemNodes = new Dictionary<string, Node>();
            var fieldNodes = new Dictionary<string, Node>();
            var nodeDepths = new Dictionary<string, int>();
            var depthYOffsets = new Dictionary<int, int>();
            var nodeY = new Dictionary<string, float>(); // <--- this is the key!

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (lastFrame != -1 && Mathf.Abs(entry.frame - lastFrame) > frameThreshold)
                {
                    DeleteElements(graphElements);
                    systemNodes.Clear();
                    fieldNodes.Clear();
                    nodeDepths.Clear();
                    depthYOffsets.Clear();
                    depthMaxRightX.Clear();
                    depthYOffsets[0] = 0;
                    depthYOffsets[1] = 0;
                }
                lastFrame = entry.frame;

                string systemName = string.IsNullOrEmpty(entry.systemName.ToString()) ? "Unknown" : entry.systemName.ToString();
                string fieldKey = $"{entry.soName}.{entry.fieldName}";
                bool wasChanged = entry.debugOld.ToString() != entry.debugNew.ToString();

                // 🛠 Force depth of first system ➝ field pair
                if (i == 0 || systemNodes.Count == 0)
                {
                    nodeDepths[systemName] = 0;
                    nodeDepths[fieldKey] = 1;

                    // Always create system node at depth 0 and seed depthMaxRightX[0]
                    int sDepth = nodeDepths[systemName];
                    float sx = 0f;
                    int syIndex = depthYOffsets.TryGetValue(sDepth, out var sct) ? sct : 0;
                    float sy = syIndex * ySpacing;
                    var systemNode = CreateSystemNode(systemName, sx, sy);
                    AddElement(systemNode);
                    systemNodes[systemName] = systemNode;
                    depthYOffsets[sDepth] = syIndex + 1;
                    // Estimate width
                    float padding = 40f;
                    float charWidth = 7.5f;
                    float estimatedWidth = Mathf.Clamp(systemName.Length * charWidth + padding, 160f, 400f);
                    depthMaxRightX[0] = estimatedWidth;
                }

                // 🔁 1. Resolve previous depth
                int fromDepth = -1;
                if (wasChanged)
                {
                    if (nodeDepths.TryGetValue(systemName, out var sysDepth))
                        fromDepth = sysDepth;
                }
                else
                {
                    if (nodeDepths.TryGetValue(fieldKey, out var fldDepth))
                        fromDepth = fldDepth;
                }

                // 🧠 2. Assign depth for the new node
                int toDepth = (fromDepth == -1 ? 0 : fromDepth + 1);

                void SetMin(string k, int d)
                {
                    if (!nodeDepths.TryGetValue(k, out var old) || d < old)
                        nodeDepths[k] = d;
                }

                if (wasChanged)
                {
                    SetMin(systemName, fromDepth == -1 ? 0 : fromDepth);
                    SetMin(fieldKey, toDepth);
                }
                else
                {
                    SetMin(fieldKey, fromDepth == -1 ? 0 : fromDepth);
                    SetMin(systemName, toDepth);
                }

                // FIELD node (depth 1+)
                if (!fieldNodes.TryGetValue(fieldKey, out var fieldNode))
                {
                    int fDepth = nodeDepths[fieldKey];
                    float fx = (fDepth == 0) ? 0f : (depthMaxRightX.TryGetValue(fDepth - 1, out var prevRight) ? prevRight + 60f : 60f);
                    int fyIndex = depthYOffsets.TryGetValue(fDepth, out var fct) ? fct : 0;
                    float fy = fyIndex * ySpacing;

                    fieldNode = CreateFieldNode(entry, fx, fy);
                    AddElement(fieldNode);
                    fieldNodes[fieldKey] = fieldNode;
                    depthYOffsets[fDepth] = fyIndex + 1;

                    // Estimate width for next column
                    float padding = 40f;
                    float charWidth = 7.5f;
                    float estimatedWidth = Mathf.Clamp(fieldKey.Length * charWidth + padding, 160f, 400f);
                    float newRight = fx + estimatedWidth;
                    if (!depthMaxRightX.ContainsKey(fDepth) || newRight > depthMaxRightX[fDepth])
                        depthMaxRightX[fDepth] = newRight;
                }

                // ⚙️ SYSTEM node (depth 0+)
                if (!systemNodes.ContainsKey(systemName) && nodeDepths[systemName] != 0)
                {
                    int sDepth = nodeDepths[systemName];
                    float sx = depthMaxRightX.TryGetValue(sDepth - 1, out var prevRight) ? prevRight + 60f : 60f;
                    int syIndex = depthYOffsets.TryGetValue(sDepth, out var sct) ? sct : 0;
                    float sy = syIndex * ySpacing;

                    var systemNode = CreateSystemNode(systemName, sx, sy);
                    AddElement(systemNode);
                    systemNodes[systemName] = systemNode;
                    depthYOffsets[sDepth] = syIndex + 1;

                    // Estimate width
                    float padding = 40f;
                    float charWidth = 7.5f;
                    float estimatedWidth = Mathf.Clamp(systemName.Length * charWidth + padding, 160f, 400f);
                    float newRight = sx + estimatedWidth;
                    if (!depthMaxRightX.ContainsKey(sDepth) || newRight > depthMaxRightX[sDepth])
                        depthMaxRightX[sDepth] = newRight;
                }

                // (connection/port logic unchanged)
                var sysIn = CreatePort(entry, systemNodes[systemName], Direction.Input, "In", "Out");
                var sysOut = CreatePort(entry, systemNodes[systemName], Direction.Output, "In", "Out");
                var fieldIn = CreatePort(entry, fieldNodes[fieldKey], Direction.Input, entry.debugOld.ToString(), "Out");
                var fieldOut = CreatePort(entry, fieldNodes[fieldKey], Direction.Output, "In", entry.debugNew.ToString());

                fieldOut.Q<Label>().style.color = Color.white;
                fieldNodes[fieldKey].titleContainer.style.color = Color.white;
                fieldNodes[fieldKey].titleContainer.style.unityFontStyleAndWeight = FontStyle.Bold;

                if (ColorUtility.TryParseHtmlString("#374785", out var fieldNodeColor))
                    fieldNodes[fieldKey].mainContainer.style.backgroundColor = fieldNodeColor;
                if (ColorUtility.TryParseHtmlString("#F76C6C", out var outPortColor))
                    fieldNodes[fieldKey].outputContainer.style.backgroundColor = outPortColor;
                fieldNodes[fieldKey].outputContainer.style.unityFontStyleAndWeight = FontStyle.Bold;

                if (wasChanged)
                {
                    systemNodes[systemName].outputContainer.Add(sysOut);
                    fieldNodes[fieldKey].inputContainer.Add(fieldIn);
                    AddElement(sysOut.ConnectTo(fieldIn));
                }
                else
                {
                    fieldNodes[fieldKey].outputContainer.Add(fieldOut);
                    systemNodes[systemName].inputContainer.Add(sysIn);
                    AddElement(fieldOut.ConnectTo(sysIn));
                }
            }
        }

        private float GetX(string label, int depth, Dictionary<int, float> depthMaxRightX, bool isSystemNode)
        {
            float padding = 40f;
            float charWidth = 7.5f;
            float estimatedWidth = Mathf.Clamp(label.Length * charWidth + padding, 160f, 400f);

            // Only system nodes at depth 0 ever set depthMaxRightX[0]
            if (depth == 0)
            {
                if (isSystemNode)
                    depthMaxRightX[0] = estimatedWidth;
                return 0f;
            }

            // All other nodes align to the rightmost of the previous depth
            float baseX = depthMaxRightX.TryGetValue(depth - 1, out var prevRight) ? prevRight : 0f;
            float currentX = baseX + 60f; // add spacing between columns

            // Store rightmost X at this depth
            float newRight = currentX + estimatedWidth;
            if (!depthMaxRightX.ContainsKey(depth) || newRight > depthMaxRightX[depth])
                depthMaxRightX[depth] = newRight;

            return currentX;
        }

        /// <summary>
        /// This took me way too much time to figure out.
        /// For spacing to be equal everywhere, you need to know how many characters are in a title.
        /// That's it x_x
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public int GetResolvedSpacingPerCharaceter(string title)
        {
            return (title.Length * xSpacingPerCharacter) + xSpacing;
        }

        private Node CreateSystemNode(string systemName, float x, float y)
        {
            var node = new Node { title = "⚙️ " + systemName };
            node.SetPosition(new Rect(x, y, 250, 100));

            /*var foldout = new Foldout { text = "Code Preview" };
            if(TryGetSystemCodePreview(systemName, out var preview))
            {
                foldout.Add(new Label(preview));
                node.mainContainer.Add(foldout);
            }*/

            if (!string.IsNullOrEmpty(systemName))
            {
                var hoverIcon = new Label("🔍");
                hoverIcon.style.marginLeft = 4;
                hoverIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
                hoverIcon.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    var localMouse = evt.mousePosition;
                    ShowHoverCard(systemName, localMouse);
                });
                hoverIcon.RegisterCallback<MouseLeaveEvent>(_ => HideHoverCard());
                node.titleContainer.Add(hoverIcon);
            }

            return node;
        }

        private void InitializeHoverCard()
        {
            hoverCard = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f),
                    color = Color.white,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    unityFontStyleAndWeight = FontStyle.Normal,
                    fontSize = 12,
                    unityTextAlign = TextAnchor.UpperLeft,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    display = DisplayStyle.None
                }
            };

            hoverCardLabel = new Label();
            hoverCard.Add(hoverCardLabel);
            Add(hoverCard);
        }

        private void ShowHoverCard(string systemName, Vector2 position)
        {
            if (!codePreviewCache.TryGetValue(systemName, out var preview))
            {
                if (!TryBuildSystemCodePreview(systemName, out preview))
                    return;

                codePreviewCache[systemName] = preview;
            }

            hoverCard.Clear();
            hoverCard.Add(preview);
            hoverCard.style.left = position.x + 15;
            hoverCard.style.top = position.y + 15;
            hoverCard.style.display = DisplayStyle.Flex;
        }

        private void HideHoverCard()
        {
            hoverCard.style.display = DisplayStyle.None;
        }

        private static readonly Color KeywordColor = new Color(86 / 255f, 156 / 255f, 214 / 255f);   // Blue
        private static readonly Color TypeColor = new Color(78 / 255f, 201 / 255f, 176 / 255f);      // Teal
        private static readonly Color StringColor = new Color(206 / 255f, 145 / 255f, 120 / 255f);   // Light brown
        private static readonly Color MethodColor = new Color(220 / 255f, 220 / 255f, 170 / 255f);   // Pale yellow
        private static readonly Color InterpolationColor = new Color(255 / 255f, 214 / 255f, 153 / 255f); // Peach
        private static readonly Color PropertyColor = new Color(156 / 255f, 220 / 255f, 254 / 255f); // Cyan
        private static readonly Color DefaultColor = Color.white;
        private static readonly Color NumberColor = new Color(181 / 255f, 206 / 255f, 168 / 255f);

        private static readonly string[] Keywords = {
            "public", "private", "protected", "class", "void", "override", "return", "if", "else", "new", "var", "using", "namespace"
        };

        private static readonly string[] Methods = { "Log", "nameof" };

        private VisualElement CreateSyntaxHighlightedLabel(string line)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            // This pattern **matches every possible thing**, in order of priority
            var pattern = string.Join("|", new[]
            {
                @"(?<whitespace>\s+)",
                @"(?<keyword>\b(public|private|protected|class|void|override|return|if|else|new|var|using|namespace|nameof)\b)",
                @"(?<knownTypeOrAttr>\b(ReactTo|SystemBase|ReaCSDebug)\b)",
                @"(?<typeOrAttr>(?<=nameof\()\s*\b[A-Z][a-zA-Z0-9_]*\b(?=\s*\.)?)",
                @"(?<methodCall>\b\w+\b(?=\s*\())",                            // OnFieldChanged
                @"(?<method>\b(Log|nameof)\b)",                                // standalone
                @"(?<paramType>\b[A-Z][a-zA-Z0-9_]*\b)\s+(?<paramName>\b[a-zA-Z_][a-zA-Z0-9_]*\b)(?=\s*[),])",
                @"(?<string>\$?""[^""\\]*(?:\\.[^""\\]*)*"")",
                @"(?<interpolation>\$\{)|(?<brace>[{}])",
                @"(?<number>\b\d+\b)",
                @"(?<dot>\.)",
                @"(?<type>\b[A-Z][a-zA-Z0-9_]*\b)",
                @"(?<property>\b[a-z_][a-zA-Z0-9_]*\b)",
                @"(?<symbol>[();,+\-*/=<>!&|[\]])",
                @"(?<fallback>.)"
            });


            var matches = Regex.Matches(line, pattern);
            int currentIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > currentIndex)
                {
                    string inBetween = line.Substring(currentIndex, match.Index - currentIndex);
                    row.Add(CreateTokenLabel(inBetween, DefaultColor));
                }

                string token = match.Value;
                Color color = DefaultColor;

                if (match.Groups["keyword"].Success)
                    color = KeywordColor;
                else if (match.Groups["method"].Success || match.Groups["methodCall"].Success)
                    color = MethodColor;
                else if (match.Groups["typeOrAttr"].Success || match.Groups["knownTypeOrAttr"].Success)
                    color = TypeColor;
                else if (match.Groups["interpolation"].Success || match.Groups["brace"].Success)
                    color = InterpolationColor;
                else if (match.Groups["string"].Success && token.StartsWith("$\""))
                {
                    foreach (var el in ParseInterpolatedString(token))
                        row.Add(el);

                    currentIndex = match.Index + match.Length;
                    continue;
                }
                else if (match.Groups["type"].Success && !IsFalsePositiveType(token))
                    color = TypeColor;
                else if (match.Groups["property"].Success)
                    color = DefaultColor;
                else if (match.Groups["number"].Success)
                    color = NumberColor;
                else if (match.Groups["dot"].Success || match.Groups["symbol"].Success)
                    color = DefaultColor;
                else if (match.Groups["whitespace"].Success)
                    color = DefaultColor;
                else if (match.Groups["fallback"].Success)
                    color = DefaultColor;

                row.Add(CreateTokenLabel(token, color));
                currentIndex = match.Index + match.Length;
            }

            if (currentIndex < line.Length)
            {
                string rest = line.Substring(currentIndex);
                row.Add(CreateTokenLabel(rest, DefaultColor));
            }

            return row;
        }

        private bool IsFalsePositiveType(string token)
        {
            return token == "Value" || token == "OnFieldChanged"; // extend as needed
        }

        private IEnumerable<VisualElement> ParseInterpolatedString(string interpolated)
        {
            var elements = new List<VisualElement>();

            // Add the leading $"
            elements.Add(CreateTokenLabel("$\"", StringColor));

            // Remove the leading $" and trailing "
            string content = interpolated.Substring(2, interpolated.Length - 3);

            int current = 0;
            while (current < content.Length)
            {
                int openBrace = content.IndexOf('{', current);
                if (openBrace == -1)
                {
                    elements.Add(CreateTokenLabel(content.Substring(current), StringColor));
                    break;
                }

                int closeBrace = content.IndexOf('}', openBrace);
                if (closeBrace == -1)
                {
                    elements.Add(CreateTokenLabel(content.Substring(current), StringColor));
                    break;
                }

                if (openBrace > current)
                {
                    elements.Add(CreateTokenLabel(content.Substring(current, openBrace - current), StringColor));
                }

                elements.Add(CreateTokenLabel("{", InterpolationColor));

                string innerExpr = content.Substring(openBrace + 1, closeBrace - openBrace - 1);
                var exprRow = CreateSyntaxHighlightedLabel(innerExpr);
                foreach (var child in exprRow.Children())
                    elements.Add(child);

                elements.Add(CreateTokenLabel("}", InterpolationColor));

                current = closeBrace + 1;
            }

            // Add trailing quote
            elements.Add(CreateTokenLabel("\"", StringColor));

            return elements;
        }


        private Label CreateTokenLabel(string text, Color color)
        {
            return new Label(text)
            {
                style = {
                    color = color,
                    unityFontStyleAndWeight = FontStyle.Normal,
                    unityTextAlign = TextAnchor.UpperLeft,
                    marginLeft = 0,
                    marginRight = 0,
                    paddingLeft = 0,
                    paddingRight = 0,
                    whiteSpace = WhiteSpace.Pre
                }
            };
        }


        private bool TryBuildSystemCodePreview(string systemName, out VisualElement previewElement)
        {
            previewElement = null;

            if (string.IsNullOrEmpty(systemName)) return false;

            string cleanName = systemName.Replace("⚙️", "").Trim();

            foreach (var script in Resources.FindObjectsOfTypeAll<MonoScript>())
            {
                if (script == null) continue;

                var type = script.GetClass();
                if (type == null) continue;

                if (type.Name == cleanName || type.FullName?.EndsWith("." + cleanName) == true)
                {
                    var code = script.text;
                    if (string.IsNullOrEmpty(code)) return false;

                    var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Where(line => !line.TrimStart().StartsWith("using "))
                                    .ToArray();

                    var root = new VisualElement
                    {
                        style = {
                    flexDirection = FlexDirection.Column,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                }
                    };

                    int count = 0;
                    foreach (var line in lines)
                    {
                        root.Add(CreateSyntaxHighlightedLabel(line));
                        if (++count >= 10) break;
                    }

                    previewElement = root;
                    return true;
                }
            }

            return false;
        }




        private Node CreateFieldNode(BurstableHistoryEntry entry, float x, float y)
        {
            var node = new Node { title = entry.soName.ToString() };
            node.SetPosition(new Rect(x, y, 250, 120));

            var container = new VisualElement
            {
                style = { flexDirection = FlexDirection.Column,
                    alignItems = Align.Center, unityFontStyleAndWeight = FontStyle.BoldAndItalic, paddingTop = 6, paddingBottom = 6  }
            };
            Color color;
            ColorUtility.TryParseHtmlString("#374785", out color);
            container.style.backgroundColor = color;
            container.style.color = Color.white;
            container.Add(new Label(entry.fieldName.ToString()));
            node.mainContainer.Add(container);
            return node;
        }

        private Port CreatePort(BurstableHistoryEntry entry, Node node, Direction direction, string inLabel, string outLabel)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, direction, Port.Capacity.Multi, typeof(bool));
            port.portName = direction == Direction.Input ? inLabel : outLabel;
            return port;
        }
    }
}
