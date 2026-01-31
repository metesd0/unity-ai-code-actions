using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Main coordinator for the Live Preview Panel
    /// Manages tabs, content switching, and Apply functionality
    /// </summary>
    public class LivePreviewPanel
    {
        public enum PreviewTab
        {
            Code,
            Hierarchy,
            Diff
        }

        // State
        private PreviewTab currentTab = PreviewTab.Code;
        private Vector2 scrollPosition;
        private bool isVisible = true;

        // Child components
        private LiveCodePreview codePreview;
        private SceneHierarchyPreview hierarchyPreview;
        private LiveDiffPreview diffPreview;

        // Data
        private string currentCode = "";
        private string originalCode = "";
        private List<HierarchyNode> hierarchyNodes = new List<HierarchyNode>();

        // Events
        public Action<string> OnApplyCode;
        public Action OnClose;

        public bool IsVisible
        {
            get => isVisible;
            set => isVisible = value;
        }

        public LivePreviewPanel()
        {
            codePreview = new LiveCodePreview();
            hierarchyPreview = new SceneHierarchyPreview();
            diffPreview = new LiveDiffPreview();
        }

        /// <summary>
        /// Update preview with new streaming content
        /// </summary>
        public void UpdateContent(string code, string original = null)
        {
            currentCode = code ?? "";
            if (original != null)
            {
                originalCode = original;
            }

            codePreview.SetCode(currentCode);
            diffPreview.SetContent(originalCode, currentCode);
        }

        /// <summary>
        /// Update hierarchy preview
        /// </summary>
        public void UpdateHierarchy(List<HierarchyNode> nodes)
        {
            hierarchyNodes = nodes ?? new List<HierarchyNode>();
            hierarchyPreview.SetNodes(hierarchyNodes);
        }

        /// <summary>
        /// Clear all preview content
        /// </summary>
        public void Clear()
        {
            currentCode = "";
            originalCode = "";
            hierarchyNodes.Clear();

            codePreview.Clear();
            hierarchyPreview.Clear();
            diffPreview.Clear();
        }

        /// <summary>
        /// Draw the preview panel
        /// </summary>
        public void Draw(Rect rect)
        {
            if (!isVisible)
                return;

            // Background
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.PanelBg);

            // Border
            DrawBorder(rect);

            // Layout
            float headerHeight = PreviewStyles.Dimensions.HeaderHeight;
            float tabHeight = PreviewStyles.Dimensions.TabHeight;
            float footerHeight = PreviewStyles.Dimensions.FooterHeight;

            Rect headerRect = new Rect(rect.x, rect.y, rect.width, headerHeight);
            Rect tabRect = new Rect(rect.x, rect.y + headerHeight, rect.width, tabHeight);
            Rect contentRect = new Rect(
                rect.x,
                rect.y + headerHeight + tabHeight,
                rect.width,
                rect.height - headerHeight - tabHeight - footerHeight
            );
            Rect footerRect = new Rect(
                rect.x,
                rect.yMax - footerHeight,
                rect.width,
                footerHeight
            );

            DrawHeader(headerRect);
            DrawTabs(tabRect);
            DrawContent(contentRect);
            DrawFooter(footerRect);
        }

        private void DrawBorder(Rect rect)
        {
            // Left border (splitter side)
            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, 1, rect.height),
                PreviewStyles.Colors.PanelBorder
            );
        }

        private void DrawHeader(Rect rect)
        {
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.HeaderBg);

            // Title
            Rect titleRect = new Rect(rect.x + 10, rect.y, rect.width - 40, rect.height);
            GUI.Label(titleRect, "👁 Live Preview", PreviewStyles.HeaderStyle);

            // Close button
            Rect closeRect = new Rect(rect.xMax - 30, rect.y + 6, 20, 20);
            if (GUI.Button(closeRect, "✕", EditorStyles.miniButton))
            {
                OnClose?.Invoke();
            }
        }

        private void DrawTabs(Rect rect)
        {
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.TabNormal);

            float x = rect.x + 4;
            float tabWidth = (rect.width - 8) / 3f;

            DrawTab(new Rect(x, rect.y, tabWidth, rect.height), "📝 Code", PreviewTab.Code);
            x += tabWidth;
            DrawTab(new Rect(x, rect.y, tabWidth, rect.height), "📁 Hierarchy", PreviewTab.Hierarchy);
            x += tabWidth;
            DrawTab(new Rect(x, rect.y, tabWidth, rect.height), "📊 Diff", PreviewTab.Diff);
        }

        private void DrawTab(Rect rect, string label, PreviewTab tab)
        {
            bool isActive = currentTab == tab;
            bool isHovered = rect.Contains(Event.current.mousePosition);

            // Background
            Color bgColor = isActive ? PreviewStyles.Colors.TabActive :
                           (isHovered ? PreviewStyles.Colors.TabHover : PreviewStyles.Colors.TabNormal);
            EditorGUI.DrawRect(rect, bgColor);

            // Label
            GUIStyle style = isActive ? PreviewStyles.TabActiveStyle : PreviewStyles.TabStyle;
            GUI.Label(rect, label, style);

            // Click handler
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                currentTab = tab;
                Event.current.Use();
            }
        }

        private void DrawContent(Rect rect)
        {
            // Content background
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.CodeBg);

            // Padding
            Rect contentRect = new Rect(
                rect.x + 4,
                rect.y + 4,
                rect.width - 8,
                rect.height - 8
            );

            switch (currentTab)
            {
                case PreviewTab.Code:
                    codePreview.Draw(contentRect);
                    break;

                case PreviewTab.Hierarchy:
                    hierarchyPreview.Draw(contentRect);
                    break;

                case PreviewTab.Diff:
                    diffPreview.Draw(contentRect);
                    break;
            }
        }

        private void DrawFooter(Rect rect)
        {
            EditorGUI.DrawRect(rect, PreviewStyles.Colors.HeaderBg);

            // Apply button
            float buttonWidth = 150f;
            Rect buttonRect = new Rect(
                rect.x + (rect.width - buttonWidth) / 2,
                rect.y + (rect.height - PreviewStyles.Dimensions.ApplyButtonHeight) / 2,
                buttonWidth,
                PreviewStyles.Dimensions.ApplyButtonHeight
            );

            bool hasContent = !string.IsNullOrEmpty(currentCode);
            GUI.enabled = hasContent;

            // Button background
            bool isHovered = buttonRect.Contains(Event.current.mousePosition);
            Color buttonColor = isHovered ? PreviewStyles.Colors.ApplyButtonHover : PreviewStyles.Colors.ApplyButton;

            Texture2D buttonBg = ChatBubbles.RoundedRectTexture.Create(
                (int)buttonWidth,
                (int)PreviewStyles.Dimensions.ApplyButtonHeight,
                6f,
                buttonColor
            );
            GUI.DrawTexture(buttonRect, buttonBg);

            // Button label
            GUIStyle buttonStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.textColor = Color.white;

            if (GUI.Button(buttonRect, "✨ Apply to Scene", buttonStyle))
            {
                OnApplyCode?.Invoke(currentCode);
            }

            GUI.enabled = true;
        }

        /// <summary>
        /// Set the active tab
        /// </summary>
        public void SetTab(PreviewTab tab)
        {
            currentTab = tab;
        }

        /// <summary>
        /// Check if preview has content
        /// </summary>
        public bool HasContent()
        {
            return !string.IsNullOrEmpty(currentCode) || hierarchyNodes.Count > 0;
        }
    }

    /// <summary>
    /// Represents a node in the hierarchy preview
    /// </summary>
    public class HierarchyNode
    {
        public string name;
        public string icon;
        public int depth;
        public bool isNew;
        public List<string> components;
        public List<HierarchyNode> children;

        public HierarchyNode(string name, int depth = 0, bool isNew = true)
        {
            this.name = name;
            this.depth = depth;
            this.isNew = isNew;
            this.icon = "🎮";
            this.components = new List<string>();
            this.children = new List<HierarchyNode>();
        }
    }
}
