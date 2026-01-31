using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Real-time code preview with syntax highlighting
    /// Updates as streaming content arrives
    /// </summary>
    public class LiveCodePreview
    {
        private string currentCode = "";
        private string highlightedCode = "";
        private string[] lines;
        private Vector2 scrollPosition;
        private float lineHeight = 16f;
        private bool needsRefresh = false;

        // Streaming state
        private int lastCharCount = 0;
        private double lastUpdateTime;
        private bool isStreaming = false;

        /// <summary>
        /// Set the code to preview
        /// </summary>
        public void SetCode(string code)
        {
            if (code == currentCode)
                return;

            currentCode = code ?? "";

            // Check if this is a streaming update
            isStreaming = currentCode.Length > lastCharCount;
            lastCharCount = currentCode.Length;
            lastUpdateTime = EditorApplication.timeSinceStartup;

            RefreshHighlighting();
        }

        /// <summary>
        /// Append streaming content
        /// </summary>
        public void AppendCode(string chunk)
        {
            if (string.IsNullOrEmpty(chunk))
                return;

            currentCode += chunk;
            isStreaming = true;
            lastCharCount = currentCode.Length;
            lastUpdateTime = EditorApplication.timeSinceStartup;

            // Debounce highlighting refresh
            needsRefresh = true;
        }

        /// <summary>
        /// Clear the preview
        /// </summary>
        public void Clear()
        {
            currentCode = "";
            highlightedCode = "";
            lines = null;
            lastCharCount = 0;
            isStreaming = false;
            scrollPosition = Vector2.zero;
        }

        private void RefreshHighlighting()
        {
            if (string.IsNullOrEmpty(currentCode))
            {
                highlightedCode = "";
                lines = new string[0];
                return;
            }

            // Apply syntax highlighting
            highlightedCode = SyntaxHighlighter.ApplyHighlighting(currentCode, "csharp");
            lines = highlightedCode.Split('\n');
            needsRefresh = false;
        }

        /// <summary>
        /// Draw the code preview
        /// </summary>
        public void Draw(Rect rect)
        {
            if (needsRefresh)
            {
                RefreshHighlighting();
            }

            if (lines == null || lines.Length == 0)
            {
                DrawEmptyState(rect);
                return;
            }

            // Calculate content size
            float contentHeight = lines.Length * lineHeight;
            float contentWidth = CalculateMaxWidth();

            Rect viewRect = new Rect(0, 0, Mathf.Max(contentWidth, rect.width - 20), contentHeight);

            // Auto-scroll when streaming
            if (isStreaming)
            {
                scrollPosition.y = Mathf.Max(0, contentHeight - rect.height + 20);
            }

            // Begin scroll view
            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, viewRect);

            // Draw line numbers and code
            float lineNumberWidth = PreviewStyles.Dimensions.LineNumberWidth;

            for (int i = 0; i < lines.Length; i++)
            {
                float y = i * lineHeight;

                // Line number
                Rect lineNumRect = new Rect(0, y, lineNumberWidth, lineHeight);
                GUI.Label(lineNumRect, (i + 1).ToString(), PreviewStyles.LineNumberStyle);

                // Code line
                Rect codeRect = new Rect(lineNumberWidth, y, viewRect.width - lineNumberWidth, lineHeight);

                // Highlight current streaming line
                if (isStreaming && i == lines.Length - 1)
                {
                    EditorGUI.DrawRect(codeRect, PreviewStyles.Colors.CodeHighlight);
                }

                GUI.Label(codeRect, lines[i], PreviewStyles.CodeStyle);
            }

            // Draw streaming cursor
            if (isStreaming)
            {
                DrawStreamingCursor(lines.Length - 1, lines[lines.Length - 1].Length);
            }

            GUI.EndScrollView();

            // Reset streaming state after delay
            if (isStreaming && EditorApplication.timeSinceStartup - lastUpdateTime > 0.5)
            {
                isStreaming = false;
            }
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

            GUI.Label(labelRect, "No code to preview\nCode will appear here as AI generates it", style);
        }

        private void DrawStreamingCursor(int line, int column)
        {
            // Blinking cursor effect
            float blinkRate = 2f;
            bool showCursor = (EditorApplication.timeSinceStartup * blinkRate) % 1 < 0.5f;

            if (!showCursor)
                return;

            float lineNumberWidth = PreviewStyles.Dimensions.LineNumberWidth;
            float charWidth = 7f; // Approximate monospace character width

            float x = lineNumberWidth + column * charWidth;
            float y = line * lineHeight;

            Rect cursorRect = new Rect(x, y + 2, 2, lineHeight - 4);
            EditorGUI.DrawRect(cursorRect, new Color(1f, 1f, 1f, 0.8f));
        }

        private float CalculateMaxWidth()
        {
            if (lines == null || lines.Length == 0)
                return 200f;

            float maxChars = 0;
            foreach (var line in lines)
            {
                // Strip rich text tags for accurate width calculation
                string plainLine = System.Text.RegularExpressions.Regex.Replace(line, "<.*?>", "");
                maxChars = Mathf.Max(maxChars, plainLine.Length);
            }

            float charWidth = 7f;
            return PreviewStyles.Dimensions.LineNumberWidth + maxChars * charWidth + 40;
        }

        /// <summary>
        /// Get the current code
        /// </summary>
        public string GetCode()
        {
            return currentCode;
        }

        /// <summary>
        /// Get line count
        /// </summary>
        public int GetLineCount()
        {
            return lines?.Length ?? 0;
        }
    }
}
