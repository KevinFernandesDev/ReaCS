using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using static ReaCS.Runtime.ReaCS;
using UnityEditor;
using System;
using ReaCS.Runtime.Core;
using ReaCS.Runtime.Registries;
using System.Linq;

namespace ReaCS.Editor
{
    public class LinkTreeGraphView : GraphView
    {
        private readonly List<Edge> edges = new();
        private readonly Dictionary<ObservableScriptableObject, Node> nodeMap = new();
        private readonly HashSet<(ObservableScriptableObject, ObservableScriptableObject)> drawnLinks = new();
        private ObservableScriptableObject rootNodeSO;

        public LinkTreeGraphView(ObservableScriptableObject root)
        {
            var helpLabel = new Label("⤷ Shortcuts:  [P] Ping Selected")
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

            style.flexGrow = 1;
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
                var width = layout.width;
                var height = layout.height;
                minimap.SetPosition(new Rect(width - 210, height - 160, 200, 150));
            });

            RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.P && selection.FirstOrDefault() is Node selected)
                {
                    if (selected.userData is ObservableScriptableObject oso)
                        EditorGUIUtility.PingObject(oso);
                }
            });

            rootNodeSO = root;
            var rootNode = CreateNode(root, Vector2.zero, true);
            AddElement(rootNode);
            nodeMap[root] = rootNode;

            TraverseAll(root, rootNode); // Delay centering until layout has resolved
            schedule.Execute(() =>
            {
                FrameGraphToCenter();
            }).ExecuteLater(100);
        }

        private void TraverseAll(ObservableScriptableObject root, Node rootNode)
        {
            var queue = new Queue<(ObservableScriptableObject, Node, Vector2)>();
            queue.Enqueue((root, rootNode, rootNode.GetPosition().position));

            while (queue.Count > 0)
            {
                var (currentSO, currentNode, currentPos) = queue.Dequeue();
                var links = Query<LinkSORegistry>().GetAllLinksInvolving(currentSO);
                float yOffset = -((links.Count() - 1) * 100f) / 2f;

                foreach (var link in links)
                {
                    bool isForward = link.Left == currentSO;
                    var other = isForward ? link.Right : link.Left;

                    if (drawnLinks.Contains((currentSO, other)) || drawnLinks.Contains((other, currentSO)))
                        continue;

                    if (other == rootNodeSO)
                        continue;

                    Vector2 otherPos = currentPos + new Vector2(isForward ? 300 : -300, yOffset);
                    yOffset += 200;

                    var otherNode = CreateNode(other, otherPos, false);
                    AddElement(otherNode);

                    var edge = isForward
                        ? Connect(currentNode.outputContainer[0] as Port, otherNode.inputContainer[0] as Port)
                        : Connect(otherNode.outputContainer[0] as Port, currentNode.inputContainer[0] as Port);

                    edges.Add(edge);
                    drawnLinks.Add((currentSO, other));

                    queue.Enqueue((other, otherNode, otherPos));
                }
            }
        }

        private Node CreateNode(ObservableScriptableObject oso, Vector2 position, bool isRoot)
        {
            var node = new Node
            {
                title = $"\U0001F9E9 {oso.name}",
                userData = oso
            };

            node.capabilities |= Capabilities.Movable | Capabilities.Selectable;
            node.SetPosition(new Rect(position, new Vector2(200, 100)));

            var input = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            input.portName = "In";
            node.inputContainer.Add(input);

            var output = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            output.portName = "Out";
            node.outputContainer.Add(output);

            var label = new Label(oso.GetType().Name)
            {
                style = {
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Italic,
                    marginTop = 4,
                    color = new Color(1f, 1f, 1f, 0.6f)
                }
            };
            node.mainContainer.Add(label);

            if (isRoot)
            {
                node.mainContainer.style.backgroundColor = new Color(0.1f, 0.6f, 0.7f);
                node.titleContainer.style.backgroundColor = new Color(0.1f, 0.4f, 0.5f);
            }

            node.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    EditorGUIUtility.PingObject(oso);
            });

            return node;
        }

        private void FrameGraphToCenter()
        {
            var nodeRects = nodeMap.Values.Select(n => n.GetPosition()).ToList();
            if (nodeRects.Count == 0) return;

            float xMin = nodeRects.Min(r => r.xMin);
            float xMax = nodeRects.Max(r => r.xMax);
            float yMin = nodeRects.Min(r => r.yMin);
            float yMax = nodeRects.Max(r => r.yMax);

            var contentBounds = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
            var graphCenter = contentBounds.center;
            var viewCenter = new Vector2(layout.width, layout.height) / 2f;

            // Offset = how much we shift the graph so that its center aligns with view center
            Vector3 offset = (Vector3)(viewCenter - graphCenter);
            this.UpdateViewTransform(offset, Vector3.one);
        }




        private void ZoomToRect(Rect rect)
        {
            var viewSize = layout.size;
            if (viewSize == Vector2.zero) return;

            float zoomX = viewSize.x / rect.width;
            float zoomY = viewSize.y / rect.height;
            float targetZoom = Mathf.Min(zoomX, zoomY);
            targetZoom = Mathf.Clamp(targetZoom, 0.1f, 2.0f); // Adjust as needed

            Vector2 center = rect.center;
            Vector2 targetOffset = viewSize / 2f - center * targetZoom;

            UpdateViewTransform(targetOffset, new Vector3(targetZoom, targetZoom, 1));
        }


        private Edge Connect(Port from, Port to)
        {
            var edge = from.ConnectTo(to);
            edge.style.opacity = 0.7f;
            AddElement(edge);
            return edge;
        }
    }
}
