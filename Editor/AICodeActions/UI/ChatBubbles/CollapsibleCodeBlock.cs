using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Renders collapsible code blocks with syntax highlighting
    /// Supports collapse/expand toggle and language detection
    /// </summary>
    public class CollapsibleCodeBlock
    {
        private Dictionary<string, bool> collapseStates = new Dictionary<string, bool>();
        private Dictionary<string, Vector2> scrollPositions = new Dictionary<string, Vector2>();
        private Dictionary<string, float> contentHeights = new Dictionary<string, float>();

        private const float MAX_COLLAPSED_HEIGHT = 150f;
        private const float MAX_EXPANDED_HEIGHT = 400f;
        private const float MIN_HEIGHT = 60f;

        /// <summary>
        /// Draw a collapsible code block
        /// </summary>
        public void Draw(string blockId, string code, string language = "csharp")
        {
            if (string.IsNullOrEmpty(code))
                return;

            string stateKey = $"{blockId}_{code.GetHashCode()}";

            // Get or initialize collapse state
            if (!collapseStates.TryGetValue(stateKey, out bool isCollapsed))
            {
                // Auto-collapse if code is long
                int lineCount = code.Split('\n').Length;
                isCollapsed = lineCount > 15;
                collapseStates[stateKey] = isCollapsed;
            }

            if (!scrollPositions.TryGetValue(stateKey, out Vector2 scrollPos))
            {
                scrollPos = Vector2.zero;
                scrollPositions[stateKey] = scrollPos;
            }

            // Calculate content height
            float lineHeight = 14f;
            int lines = code.Split('\n').Length;
            float naturalHeight = lines * lineHeight + ChatBubbleStyles.Dimensions.CodeBlockPadding * 2;

            if (!contentHeights.ContainsKey(stateKey))
            {
                contentHeights[stateKey] = naturalHeight;
            }

            float maxHeight = isCollapsed ? MAX_COLLAPSED_HEIGHT : MAX_EXPANDED_HEIGHT;
            float displayHeight = Mathf.Clamp(naturalHeight, MIN_HEIGHT, maxHeight);
            bool needsScroll = naturalHeight > displayHeight;

            // Draw container
            EditorGUILayout.BeginVertical();

            // Header bar
            DrawHeader(stateKey, language, isCollapsed, lines);

            // Code content
            DrawCodeContent(stateKey, code, language, displayHeight, needsScroll, ref scrollPos);

            scrollPositions[stateKey] = scrollPos;

            EditorGUILayout.EndVertical();
        }

        private void DrawHeader(string stateKey, string language, bool isCollapsed, int lineCount)
        {
            Rect headerRect = GUILayoutUtility.GetRect(
                GUIContent.none,
                ChatBubbleStyles.CodeHeaderStyle,
                GUILayout.Height(ChatBubbleStyles.Dimensions.CodeBlockHeaderHeight)
            );

            // Draw header background
            GUI.DrawTexture(headerRect, RoundedRectTexture.Create(
                (int)headerRect.width,
                (int)headerRect.height,
                ChatBubbleStyles.Dimensions.CodeBlockRadius,
                ChatBubbleStyles.Colors.CodeBlockHeader
            ));

            // Language icon and label
            string langIcon = GetLanguageIcon(language);
            string langLabel = GetLanguageLabel(language);

            Rect iconRect = new Rect(headerRect.x + 8, headerRect.y + 4, 20, 20);
            GUI.Label(iconRect, langIcon, EditorStyles.label);

            Rect labelRect = new Rect(headerRect.x + 28, headerRect.y + 4, 100, 20);
            GUI.Label(labelRect, langLabel, EditorStyles.miniLabel);

            // Line count
            Rect lineCountRect = new Rect(headerRect.x + 130, headerRect.y + 4, 60, 20);
            GUI.Label(lineCountRect, $"{lineCount} lines", EditorStyles.miniLabel);

            // Collapse button
            float buttonWidth = 70f;
            Rect collapseRect = new Rect(
                headerRect.xMax - buttonWidth - 8,
                headerRect.y + 3,
                buttonWidth,
                22
            );

            string buttonText = isCollapsed ? "▼ Expand" : "▲ Collapse";
            if (GUI.Button(collapseRect, buttonText, EditorStyles.miniButton))
            {
                collapseStates[stateKey] = !isCollapsed;
            }
        }

        private void DrawCodeContent(string stateKey, string code, string language, float height, bool needsScroll, ref Vector2 scrollPos)
        {
            // Apply syntax highlighting
            string highlightedCode = SyntaxHighlighter.ApplyHighlighting(code, language);

            // Create code style
            GUIStyle codeStyle = new GUIStyle(ChatBubbleStyles.CodeBlockStyle);
            codeStyle.wordWrap = false;

            // Draw background
            Rect bgRect = GUILayoutUtility.GetRect(GUIContent.none, codeStyle, GUILayout.Height(height));

            // Draw rounded background
            GUI.DrawTexture(bgRect, RoundedRectTexture.Create(
                (int)bgRect.width,
                (int)bgRect.height,
                ChatBubbleStyles.Dimensions.CodeBlockRadius,
                ChatBubbleStyles.Colors.CodeBlockBg
            ));

            // Content area with padding
            Rect contentRect = new Rect(
                bgRect.x + ChatBubbleStyles.Dimensions.CodeBlockPadding,
                bgRect.y + ChatBubbleStyles.Dimensions.CodeBlockPadding,
                bgRect.width - ChatBubbleStyles.Dimensions.CodeBlockPadding * 2,
                bgRect.height - ChatBubbleStyles.Dimensions.CodeBlockPadding * 2
            );

            if (needsScroll)
            {
                // Calculate content size
                GUIContent content = new GUIContent(highlightedCode);
                Vector2 contentSize = codeStyle.CalcSize(content);

                Rect viewRect = new Rect(0, 0, contentSize.x, contentSize.y);

                scrollPos = GUI.BeginScrollView(contentRect, scrollPos, viewRect, true, true);
                GUI.Label(new Rect(0, 0, contentSize.x, contentSize.y), highlightedCode, codeStyle);
                GUI.EndScrollView();
            }
            else
            {
                GUI.Label(contentRect, highlightedCode, codeStyle);
            }
        }

        private string GetLanguageIcon(string language)
        {
            return language?.ToLower() switch
            {
                "csharp" or "cs" or "c#" => "📄",
                "json" => "📋",
                "xml" => "📰",
                "javascript" or "js" => "📜",
                "python" or "py" => "🐍",
                "html" => "🌐",
                "css" => "🎨",
                "sql" => "🗃",
                "bash" or "shell" or "sh" => "💻",
                _ => "📝"
            };
        }

        private string GetLanguageLabel(string language)
        {
            return language?.ToLower() switch
            {
                "csharp" or "cs" or "c#" => "C#",
                "javascript" or "js" => "JavaScript",
                "python" or "py" => "Python",
                "typescript" or "ts" => "TypeScript",
                _ => language?.ToUpper() ?? "CODE"
            };
        }

        /// <summary>
        /// Set collapse state for a code block
        /// </summary>
        public void SetCollapsed(string blockId, bool collapsed)
        {
            collapseStates[blockId] = collapsed;
        }

        /// <summary>
        /// Toggle collapse state
        /// </summary>
        public void ToggleCollapsed(string blockId)
        {
            if (collapseStates.TryGetValue(blockId, out bool current))
            {
                collapseStates[blockId] = !current;
            }
        }

        /// <summary>
        /// Clear all collapse states
        /// </summary>
        public void ClearCollapseStates()
        {
            collapseStates.Clear();
            scrollPositions.Clear();
            contentHeights.Clear();
        }

        /// <summary>
        /// Collapse all code blocks
        /// </summary>
        public void CollapseAll()
        {
            var keys = new List<string>(collapseStates.Keys);
            foreach (var key in keys)
            {
                collapseStates[key] = true;
            }
        }

        /// <summary>
        /// Expand all code blocks
        /// </summary>
        public void ExpandAll()
        {
            var keys = new List<string>(collapseStates.Keys);
            foreach (var key in keys)
            {
                collapseStates[key] = false;
            }
        }
    }
}
