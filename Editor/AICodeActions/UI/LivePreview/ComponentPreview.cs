using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Displays component property cards in the preview panel
    /// Shows component fields and their values
    /// </summary>
    public class ComponentPreview
    {
        private List<ComponentData> components = new List<ComponentData>();
        private Vector2 scrollPosition;
        private HashSet<string> expandedComponents = new HashSet<string>();

        public class ComponentData
        {
            public string name;
            public string typeName;
            public bool isNew;
            public List<PropertyData> properties;

            public ComponentData(string name, string typeName = null)
            {
                this.name = name;
                this.typeName = typeName ?? name;
                this.isNew = true;
                this.properties = new List<PropertyData>();
            }
        }

        public class PropertyData
        {
            public string name;
            public string value;
            public string type;
            public bool isModified;

            public PropertyData(string name, string value, string type = "")
            {
                this.name = name;
                this.value = value;
                this.type = type;
                this.isModified = false;
            }
        }

        /// <summary>
        /// Set components to display
        /// </summary>
        public void SetComponents(List<ComponentData> newComponents)
        {
            components = newComponents ?? new List<ComponentData>();
        }

        /// <summary>
        /// Add a component
        /// </summary>
        public void AddComponent(ComponentData component)
        {
            components.Add(component);
        }

        /// <summary>
        /// Clear all components
        /// </summary>
        public void Clear()
        {
            components.Clear();
            expandedComponents.Clear();
            scrollPosition = Vector2.zero;
        }

        /// <summary>
        /// Draw component preview
        /// </summary>
        public void Draw(Rect rect)
        {
            if (components.Count == 0)
            {
                DrawEmptyState(rect);
                return;
            }

            float contentHeight = CalculateTotalHeight();
            Rect viewRect = new Rect(0, 0, rect.width - 20, contentHeight);

            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, viewRect);

            float y = 0;
            foreach (var comp in components)
            {
                DrawComponentCard(comp, ref y, rect.width - 20);
                y += 8; // Spacing between cards
            }

            GUI.EndScrollView();
        }

        private void DrawComponentCard(ComponentData comp, ref float y, float width)
        {
            float headerHeight = 28f;
            float propertyHeight = 20f;

            string compKey = comp.name;
            bool isExpanded = expandedComponents.Contains(compKey);

            // Card background
            float cardHeight = headerHeight;
            if (isExpanded && comp.properties.Count > 0)
            {
                cardHeight += comp.properties.Count * propertyHeight + 8;
            }

            Rect cardRect = new Rect(0, y, width, cardHeight);

            // Draw rounded background
            Texture2D cardBg = ChatBubbles.RoundedRectTexture.Create(
                (int)width,
                (int)cardHeight,
                6f,
                PreviewStyles.Colors.PanelBg
            );
            GUI.DrawTexture(cardRect, cardBg);

            // Header
            Rect headerRect = new Rect(0, y, width, headerHeight);
            DrawComponentHeader(comp, headerRect, compKey, isExpanded);

            y += headerHeight;

            // Properties
            if (isExpanded && comp.properties.Count > 0)
            {
                y += 4;
                foreach (var prop in comp.properties)
                {
                    DrawProperty(prop, ref y, width);
                }
                y += 4;
            }
        }

        private void DrawComponentHeader(ComponentData comp, Rect rect, string compKey, bool isExpanded)
        {
            // Header background
            Texture2D headerBg = ChatBubbles.RoundedRectTexture.Create(
                (int)rect.width,
                (int)rect.height,
                6f,
                PreviewStyles.Colors.HeaderBg
            );
            GUI.DrawTexture(rect, headerBg);

            // Expand arrow
            if (comp.properties.Count > 0)
            {
                Rect arrowRect = new Rect(rect.x + 8, rect.y, 20, rect.height);
                string arrow = isExpanded ? "▼" : "▶";

                GUIStyle arrowStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter
                };

                if (GUI.Button(arrowRect, arrow, arrowStyle))
                {
                    if (isExpanded)
                        expandedComponents.Remove(compKey);
                    else
                        expandedComponents.Add(compKey);
                }
            }

            // Icon
            float iconX = rect.x + (comp.properties.Count > 0 ? 28 : 8);
            Rect iconRect = new Rect(iconX, rect.y, 24, rect.height);
            GUI.Label(iconRect, "🧩", EditorStyles.label);

            // Component name
            Rect nameRect = new Rect(iconX + 24, rect.y, rect.width - iconX - 80, rect.height);
            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };

            if (comp.isNew)
            {
                nameStyle.normal.textColor = PreviewStyles.Colors.HierarchyItemNew;
            }

            GUI.Label(nameRect, comp.name, nameStyle);

            // New badge
            if (comp.isNew)
            {
                Rect badgeRect = new Rect(rect.xMax - 50, rect.y + 5, 40, 18);
                DrawBadge(badgeRect, "NEW", new Color(0.3f, 0.7f, 0.4f));
            }
        }

        private void DrawProperty(PropertyData prop, ref float y, float width)
        {
            float propertyHeight = 20f;
            float labelWidth = width * 0.4f;

            Rect propRect = new Rect(24, y, width - 32, propertyHeight);

            // Highlight if modified
            if (prop.isModified)
            {
                EditorGUI.DrawRect(propRect, PreviewStyles.Colors.DiffModified);
            }

            // Property name
            Rect nameRect = new Rect(propRect.x, y, labelWidth, propertyHeight);
            GUIStyle nameStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            nameStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);
            GUI.Label(nameRect, prop.name, nameStyle);

            // Property value
            Rect valueRect = new Rect(propRect.x + labelWidth, y, width - labelWidth - 32, propertyHeight);
            GUIStyle valueStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
            valueStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(valueRect, prop.value, valueStyle);

            y += propertyHeight;
        }

        private void DrawBadge(Rect rect, string text, Color color)
        {
            Texture2D badgeBg = ChatBubbles.RoundedRectTexture.Create(
                (int)rect.width,
                (int)rect.height,
                4f,
                color
            );
            GUI.DrawTexture(rect, badgeBg);

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                fontStyle = FontStyle.Bold
            };
            badgeStyle.normal.textColor = Color.white;
            GUI.Label(rect, text, badgeStyle);
        }

        private float CalculateTotalHeight()
        {
            float height = 0;

            foreach (var comp in components)
            {
                height += 28; // Header

                if (expandedComponents.Contains(comp.name) && comp.properties.Count > 0)
                {
                    height += comp.properties.Count * 20 + 8;
                }

                height += 8; // Spacing
            }

            return height;
        }

        private void DrawEmptyState(Rect rect)
        {
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

            GUI.Label(labelRect, "No components to preview", style);
        }
    }
}
