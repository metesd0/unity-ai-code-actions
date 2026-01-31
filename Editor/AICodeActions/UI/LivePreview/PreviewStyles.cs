using UnityEngine;
using UnityEditor;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Style definitions for Live Preview Panel
    /// Provides consistent theming for all preview components
    /// </summary>
    public static class PreviewStyles
    {
        // Colors
        public static class Colors
        {
            // Panel
            public static readonly Color PanelBg = new Color(0.15f, 0.15f, 0.17f, 1f);
            public static readonly Color PanelBorder = new Color(0.25f, 0.25f, 0.28f, 1f);
            public static readonly Color HeaderBg = new Color(0.12f, 0.12f, 0.14f, 1f);

            // Tabs
            public static readonly Color TabNormal = new Color(0.18f, 0.18f, 0.2f, 1f);
            public static readonly Color TabHover = new Color(0.22f, 0.22f, 0.25f, 1f);
            public static readonly Color TabActive = new Color(0.25f, 0.45f, 0.7f, 1f);
            public static readonly Color TabText = new Color(0.75f, 0.75f, 0.75f, 1f);
            public static readonly Color TabTextActive = new Color(1f, 1f, 1f, 1f);

            // Code preview
            public static readonly Color CodeBg = new Color(0.1f, 0.1f, 0.12f, 1f);
            public static readonly Color CodeLineNumber = new Color(0.4f, 0.4f, 0.4f, 1f);
            public static readonly Color CodeHighlight = new Color(0.3f, 0.5f, 0.7f, 0.3f);

            // Hierarchy
            public static readonly Color HierarchyBg = new Color(0.13f, 0.13f, 0.15f, 1f);
            public static readonly Color HierarchyItem = new Color(0.85f, 0.85f, 0.85f, 1f);
            public static readonly Color HierarchyItemNew = new Color(0.5f, 0.85f, 0.5f, 1f);
            public static readonly Color HierarchyIndent = new Color(0.3f, 0.3f, 0.33f, 1f);

            // Diff
            public static readonly Color DiffAdded = new Color(0.2f, 0.4f, 0.2f, 0.5f);
            public static readonly Color DiffRemoved = new Color(0.4f, 0.2f, 0.2f, 0.5f);
            public static readonly Color DiffModified = new Color(0.4f, 0.35f, 0.2f, 0.5f);

            // Apply button
            public static readonly Color ApplyButton = new Color(0.2f, 0.6f, 0.3f, 1f);
            public static readonly Color ApplyButtonHover = new Color(0.25f, 0.7f, 0.35f, 1f);

            // Splitter
            public static readonly Color Splitter = new Color(0.2f, 0.2f, 0.22f, 1f);
            public static readonly Color SplitterHover = new Color(0.35f, 0.55f, 0.8f, 1f);
        }

        // Dimensions
        public static class Dimensions
        {
            public const float HeaderHeight = 32f;
            public const float TabHeight = 28f;
            public const float TabMinWidth = 80f;
            public const float TabPadding = 12f;

            public const float SplitterWidth = 6f;
            public const float MinPanelWidth = 200f;

            public const float LineNumberWidth = 40f;
            public const float CodePadding = 10f;

            public const float HierarchyIndent = 16f;
            public const float HierarchyItemHeight = 22f;

            public const float ApplyButtonHeight = 32f;
            public const float FooterHeight = 44f;
        }

        // Cached styles
        private static GUIStyle _headerStyle;
        private static GUIStyle _tabStyle;
        private static GUIStyle _tabActiveStyle;
        private static GUIStyle _codeStyle;
        private static GUIStyle _lineNumberStyle;
        private static GUIStyle _hierarchyItemStyle;
        private static GUIStyle _applyButtonStyle;

        public static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                {
                    _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(10, 10, 0, 0)
                    };
                    _headerStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
                }
                return _headerStyle;
            }
        }

        public static GUIStyle TabStyle
        {
            get
            {
                if (_tabStyle == null)
                {
                    _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset((int)Dimensions.TabPadding, (int)Dimensions.TabPadding, 4, 4),
                        fixedHeight = Dimensions.TabHeight
                    };
                    _tabStyle.normal.textColor = Colors.TabText;
                }
                return _tabStyle;
            }
        }

        public static GUIStyle TabActiveStyle
        {
            get
            {
                if (_tabActiveStyle == null)
                {
                    _tabActiveStyle = new GUIStyle(TabStyle);
                    _tabActiveStyle.normal.textColor = Colors.TabTextActive;
                    _tabActiveStyle.fontStyle = FontStyle.Bold;
                }
                return _tabActiveStyle;
            }
        }

        public static GUIStyle CodeStyle
        {
            get
            {
                if (_codeStyle == null)
                {
                    _codeStyle = new GUIStyle(EditorStyles.label)
                    {
                        font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                        fontSize = 11,
                        richText = true,
                        wordWrap = false,
                        padding = new RectOffset(0, 0, 2, 2)
                    };
                    _codeStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                }
                return _codeStyle;
            }
        }

        public static GUIStyle LineNumberStyle
        {
            get
            {
                if (_lineNumberStyle == null)
                {
                    _lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 10,
                        alignment = TextAnchor.MiddleRight,
                        padding = new RectOffset(0, 8, 0, 0)
                    };
                    _lineNumberStyle.normal.textColor = Colors.CodeLineNumber;
                }
                return _lineNumberStyle;
            }
        }

        public static GUIStyle HierarchyItemStyle
        {
            get
            {
                if (_hierarchyItemStyle == null)
                {
                    _hierarchyItemStyle = new GUIStyle(EditorStyles.label)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(4, 4, 2, 2)
                    };
                    _hierarchyItemStyle.normal.textColor = Colors.HierarchyItem;
                }
                return _hierarchyItemStyle;
            }
        }

        public static GUIStyle ApplyButtonStyle
        {
            get
            {
                if (_applyButtonStyle == null)
                {
                    _applyButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        fixedHeight = Dimensions.ApplyButtonHeight,
                        padding = new RectOffset(16, 16, 6, 6)
                    };
                    _applyButtonStyle.normal.textColor = Color.white;
                }
                return _applyButtonStyle;
            }
        }

        /// <summary>
        /// Clear cached styles
        /// </summary>
        public static void ClearStyles()
        {
            _headerStyle = null;
            _tabStyle = null;
            _tabActiveStyle = null;
            _codeStyle = null;
            _lineNumberStyle = null;
            _hierarchyItemStyle = null;
            _applyButtonStyle = null;
        }

        /// <summary>
        /// Create a solid color texture
        /// </summary>
        public static Texture2D CreateTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
