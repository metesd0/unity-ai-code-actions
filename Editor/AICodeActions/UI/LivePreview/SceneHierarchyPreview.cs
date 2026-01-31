using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Displays a preview of the scene hierarchy that will be created
    /// Shows GameObjects, Components, and their relationships
    /// </summary>
    public class SceneHierarchyPreview
    {
        private List<HierarchyNode> nodes = new List<HierarchyNode>();
        private Vector2 scrollPosition;
        private HashSet<string> expandedNodes = new HashSet<string>();

        /// <summary>
        /// Set hierarchy nodes to display
        /// </summary>
        public void SetNodes(List<HierarchyNode> newNodes)
        {
            nodes = newNodes ?? new List<HierarchyNode>();
        }

        /// <summary>
        /// Add a node to the hierarchy
        /// </summary>
        public void AddNode(HierarchyNode node)
        {
            nodes.Add(node);
        }

        /// <summary>
        /// Clear the hierarchy
        /// </summary>
        public void Clear()
        {
            nodes.Clear();
            expandedNodes.Clear();
            scrollPosition = Vector2.zero;
        }

        /// <summary>
        /// Draw the hierarchy preview
        /// </summary>
        public void Draw(Rect rect)
        {
            if (nodes.Count == 0)
            {
                DrawEmptyState(rect);
                return;
            }

            // Background
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.HierarchyBg);

            // Calculate content height
            float contentHeight = CalculateTotalHeight(nodes);
            Rect viewRect = new Rect(0, 0, rect.width - 20, contentHeight);

            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, viewRect);

            float y = 0;
            DrawNodes(nodes, ref y, rect.width - 20);

            GUI.EndScrollView();
        }

        private void DrawNodes(List<HierarchyNode> nodeList, ref float y, float width)
        {
            foreach (var node in nodeList)
            {
                DrawNode(node, ref y, width);

                // Draw children if expanded
                string nodeKey = GetNodeKey(node);
                if (node.children.Count > 0 && expandedNodes.Contains(nodeKey))
                {
                    DrawNodes(node.children, ref y, width);
                }
            }
        }

        private void DrawNode(HierarchyNode node, ref float y, float width)
        {
            float itemHeight = PreviewStyles.Dimensions.HierarchyItemHeight;
            float indent = node.depth * PreviewStyles.Dimensions.HierarchyIndent;

            Rect itemRect = new Rect(indent, y, width - indent, itemHeight);

            // Hover highlight
            bool isHovered = itemRect.Contains(Event.current.mousePosition);
            if (isHovered)
            {
                EditorGUI.DrawRect(itemRect, new Color(1f, 1f, 1f, 0.05f));
            }

            // Draw indent lines
            DrawIndentLines(node.depth, y, itemHeight);

            // Expand/collapse arrow for nodes with children
            if (node.children.Count > 0)
            {
                string nodeKey = GetNodeKey(node);
                bool isExpanded = expandedNodes.Contains(nodeKey);

                Rect arrowRect = new Rect(indent, y, 16, itemHeight);
                string arrow = isExpanded ? "▼" : "▶";

                if (GUI.Button(arrowRect, arrow, EditorStyles.miniLabel))
                {
                    if (isExpanded)
                        expandedNodes.Remove(nodeKey);
                    else
                        expandedNodes.Add(nodeKey);
                }
            }

            // Icon
            float iconX = indent + (node.children.Count > 0 ? 16 : 4);
            Rect iconRect = new Rect(iconX, y, 20, itemHeight);
            GUI.Label(iconRect, node.icon, EditorStyles.label);

            // Name
            Rect nameRect = new Rect(iconX + 20, y, width - iconX - 24, itemHeight);
            GUIStyle nameStyle = new GUIStyle(PreviewStyles.HierarchyItemStyle);

            if (node.isNew)
            {
                nameStyle.normal.textColor = PreviewStyles.Colors.HierarchyItemNew;
                GUI.Label(nameRect, $"{node.name} (new)", nameStyle);
            }
            else
            {
                GUI.Label(nameRect, node.name, nameStyle);
            }

            y += itemHeight;

            // Draw components if expanded
            if (node.components.Count > 0)
            {
                string nodeKey = GetNodeKey(node);
                if (expandedNodes.Contains(nodeKey) || node.children.Count == 0)
                {
                    foreach (var comp in node.components)
                    {
                        DrawComponent(comp, node.depth + 1, ref y, width);
                    }
                }
            }
        }

        private void DrawComponent(string componentName, int depth, ref float y, float width)
        {
            float itemHeight = PreviewStyles.Dimensions.HierarchyItemHeight - 4;
            float indent = depth * PreviewStyles.Dimensions.HierarchyIndent + 8;

            Rect itemRect = new Rect(indent, y, width - indent, itemHeight);

            // Component style (smaller, gray)
            GUIStyle compStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10
            };
            compStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);

            GUI.Label(itemRect, $"🧩 {componentName}", compStyle);

            y += itemHeight;
        }

        private void DrawIndentLines(int depth, float y, float height)
        {
            for (int i = 0; i < depth; i++)
            {
                float x = i * PreviewStyles.Dimensions.HierarchyIndent + 8;
                Rect lineRect = new Rect(x, y, 1, height);
                EditorGUI.DrawRect(lineRect, PreviewStyles.Colors.HierarchyIndent);
            }
        }

        private float CalculateTotalHeight(List<HierarchyNode> nodeList)
        {
            float height = 0;

            foreach (var node in nodeList)
            {
                height += PreviewStyles.Dimensions.HierarchyItemHeight;

                string nodeKey = GetNodeKey(node);
                bool showComponents = expandedNodes.Contains(nodeKey) || node.children.Count == 0;

                if (showComponents && node.components.Count > 0)
                {
                    height += node.components.Count * (PreviewStyles.Dimensions.HierarchyItemHeight - 4);
                }

                if (node.children.Count > 0 && expandedNodes.Contains(nodeKey))
                {
                    height += CalculateTotalHeight(node.children);
                }
            }

            return height;
        }

        private string GetNodeKey(HierarchyNode node)
        {
            return $"{node.depth}_{node.name}";
        }

        private void DrawEmptyState(Rect rect)
        {
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.HierarchyBg);

            GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 12
            };

            Rect labelRect = new Rect(
                rect.x,
                rect.y + rect.height / 2 - 20,
                rect.width,
                40
            );

            GUI.Label(labelRect, "No hierarchy changes\nGameObjects will appear here", style);
        }

        /// <summary>
        /// Expand all nodes
        /// </summary>
        public void ExpandAll()
        {
            ExpandNodes(nodes);
        }

        private void ExpandNodes(List<HierarchyNode> nodeList)
        {
            foreach (var node in nodeList)
            {
                expandedNodes.Add(GetNodeKey(node));
                if (node.children.Count > 0)
                {
                    ExpandNodes(node.children);
                }
            }
        }

        /// <summary>
        /// Collapse all nodes
        /// </summary>
        public void CollapseAll()
        {
            expandedNodes.Clear();
        }
    }
}
