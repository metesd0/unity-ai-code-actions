using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.LivePreview
{
    /// <summary>
    /// Displays a live diff view showing changes between original and new code
    /// Highlights additions, removals, and modifications
    /// </summary>
    public class LiveDiffPreview
    {
        private string originalCode = "";
        private string newCode = "";
        private List<DiffLine> diffLines = new List<DiffLine>();
        private Vector2 scrollPosition;
        private float lineHeight = 16f;

        public enum DiffType
        {
            Unchanged,
            Added,
            Removed,
            Modified
        }

        public class DiffLine
        {
            public string content;
            public DiffType type;
            public int originalLineNum;
            public int newLineNum;

            public DiffLine(string content, DiffType type, int originalNum = -1, int newNum = -1)
            {
                this.content = content;
                this.type = type;
                this.originalLineNum = originalNum;
                this.newLineNum = newNum;
            }
        }

        /// <summary>
        /// Set content for diff comparison
        /// </summary>
        public void SetContent(string original, string newContent)
        {
            originalCode = original ?? "";
            newCode = newContent ?? "";
            ComputeDiff();
        }

        /// <summary>
        /// Clear the diff view
        /// </summary>
        public void Clear()
        {
            originalCode = "";
            newCode = "";
            diffLines.Clear();
            scrollPosition = Vector2.zero;
        }

        /// <summary>
        /// Compute diff between original and new content
        /// </summary>
        private void ComputeDiff()
        {
            diffLines.Clear();

            if (string.IsNullOrEmpty(originalCode) && string.IsNullOrEmpty(newCode))
                return;

            // If no original, all lines are additions
            if (string.IsNullOrEmpty(originalCode))
            {
                var newLines = newCode.Split('\n');
                for (int i = 0; i < newLines.Length; i++)
                {
                    diffLines.Add(new DiffLine(newLines[i], DiffType.Added, -1, i + 1));
                }
                return;
            }

            // If no new content, all lines are removals
            if (string.IsNullOrEmpty(newCode))
            {
                var origLines = originalCode.Split('\n');
                for (int i = 0; i < origLines.Length; i++)
                {
                    diffLines.Add(new DiffLine(origLines[i], DiffType.Removed, i + 1, -1));
                }
                return;
            }

            // Simple line-by-line diff (LCS-based diff would be more accurate)
            var originalLines = originalCode.Split('\n');
            var newLines = newCode.Split('\n');

            ComputeSimpleDiff(originalLines, newLines);
        }

        private void ComputeSimpleDiff(string[] originalLines, string[] newLines)
        {
            int origIdx = 0;
            int newIdx = 0;

            while (origIdx < originalLines.Length || newIdx < newLines.Length)
            {
                if (origIdx >= originalLines.Length)
                {
                    // Remaining new lines are additions
                    diffLines.Add(new DiffLine(newLines[newIdx], DiffType.Added, -1, newIdx + 1));
                    newIdx++;
                }
                else if (newIdx >= newLines.Length)
                {
                    // Remaining original lines are removals
                    diffLines.Add(new DiffLine(originalLines[origIdx], DiffType.Removed, origIdx + 1, -1));
                    origIdx++;
                }
                else if (originalLines[origIdx] == newLines[newIdx])
                {
                    // Lines match - unchanged
                    diffLines.Add(new DiffLine(originalLines[origIdx], DiffType.Unchanged, origIdx + 1, newIdx + 1));
                    origIdx++;
                    newIdx++;
                }
                else
                {
                    // Lines differ - check if it's a modification or add/remove
                    int lookAheadNew = FindLineInRange(originalLines[origIdx], newLines, newIdx, Mathf.Min(newIdx + 5, newLines.Length));
                    int lookAheadOrig = FindLineInRange(newLines[newIdx], originalLines, origIdx, Mathf.Min(origIdx + 5, originalLines.Length));

                    if (lookAheadNew >= 0 && (lookAheadOrig < 0 || lookAheadNew - newIdx <= lookAheadOrig - origIdx))
                    {
                        // Original line found ahead in new - lines before are additions
                        while (newIdx < lookAheadNew)
                        {
                            diffLines.Add(new DiffLine(newLines[newIdx], DiffType.Added, -1, newIdx + 1));
                            newIdx++;
                        }
                    }
                    else if (lookAheadOrig >= 0)
                    {
                        // New line found ahead in original - lines before are removals
                        while (origIdx < lookAheadOrig)
                        {
                            diffLines.Add(new DiffLine(originalLines[origIdx], DiffType.Removed, origIdx + 1, -1));
                            origIdx++;
                        }
                    }
                    else
                    {
                        // Treat as modification (remove + add)
                        diffLines.Add(new DiffLine(originalLines[origIdx], DiffType.Removed, origIdx + 1, -1));
                        diffLines.Add(new DiffLine(newLines[newIdx], DiffType.Added, -1, newIdx + 1));
                        origIdx++;
                        newIdx++;
                    }
                }
            }
        }

        private int FindLineInRange(string line, string[] lines, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (lines[i] == line)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Draw the diff view
        /// </summary>
        public void Draw(Rect rect)
        {
            if (diffLines.Count == 0)
            {
                DrawEmptyState(rect);
                return;
            }

            // Calculate content size
            float contentHeight = diffLines.Count * lineHeight;
            float contentWidth = CalculateMaxWidth();

            Rect viewRect = new Rect(0, 0, Mathf.Max(contentWidth, rect.width - 20), contentHeight);

            scrollPosition = GUI.BeginScrollView(rect, scrollPosition, viewRect);

            float lineNumWidth = 30f;
            float gutterWidth = lineNumWidth * 2 + 8;

            for (int i = 0; i < diffLines.Count; i++)
            {
                DrawDiffLine(diffLines[i], i, gutterWidth, viewRect.width);
            }

            GUI.EndScrollView();

            // Draw stats
            DrawStats(rect);
        }

        private void DrawDiffLine(DiffLine line, int index, float gutterWidth, float width)
        {
            float y = index * lineHeight;
            float lineNumWidth = 30f;

            // Line background based on type
            Color bgColor = line.type switch
            {
                DiffType.Added => PreviewStyles.Colors.DiffAdded,
                DiffType.Removed => PreviewStyles.Colors.DiffRemoved,
                DiffType.Modified => PreviewStyles.Colors.DiffModified,
                _ => Color.clear
            };

            if (bgColor.a > 0)
            {
                Rect bgRect = new Rect(0, y, width, lineHeight);
                EditorGUI.DrawRect(bgRect, bgColor);
            }

            // Original line number
            if (line.originalLineNum > 0)
            {
                Rect origNumRect = new Rect(0, y, lineNumWidth, lineHeight);
                GUI.Label(origNumRect, line.originalLineNum.ToString(), PreviewStyles.LineNumberStyle);
            }

            // New line number
            if (line.newLineNum > 0)
            {
                Rect newNumRect = new Rect(lineNumWidth + 4, y, lineNumWidth, lineHeight);
                GUI.Label(newNumRect, line.newLineNum.ToString(), PreviewStyles.LineNumberStyle);
            }

            // Change indicator
            string indicator = line.type switch
            {
                DiffType.Added => "+",
                DiffType.Removed => "-",
                DiffType.Modified => "~",
                _ => " "
            };

            Color indicatorColor = line.type switch
            {
                DiffType.Added => new Color(0.4f, 0.8f, 0.4f),
                DiffType.Removed => new Color(0.8f, 0.4f, 0.4f),
                DiffType.Modified => new Color(0.8f, 0.7f, 0.4f),
                _ => Color.gray
            };

            Rect indicatorRect = new Rect(gutterWidth - 12, y, 12, lineHeight);
            GUIStyle indicatorStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            indicatorStyle.normal.textColor = indicatorColor;
            GUI.Label(indicatorRect, indicator, indicatorStyle);

            // Code content
            Rect codeRect = new Rect(gutterWidth, y, width - gutterWidth, lineHeight);
            string highlightedContent = SyntaxHighlighter.ApplyHighlighting(line.content, "csharp");
            GUI.Label(codeRect, highlightedContent, PreviewStyles.CodeStyle);
        }

        private void DrawStats(Rect rect)
        {
            int added = 0, removed = 0, unchanged = 0;

            foreach (var line in diffLines)
            {
                switch (line.type)
                {
                    case DiffType.Added: added++; break;
                    case DiffType.Removed: removed++; break;
                    case DiffType.Unchanged: unchanged++; break;
                }
            }

            // Stats bar at bottom
            Rect statsRect = new Rect(rect.x, rect.yMax - 20, rect.width, 20);
            EditorGUI.DrawRect(statsRect, new Color(0, 0, 0, 0.3f));

            GUIStyle statsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            string stats = $"<color=#88CC88>+{added}</color>  <color=#CC8888>-{removed}</color>  <color=#888888>~{unchanged}</color>";
            statsStyle.richText = true;
            GUI.Label(statsRect, stats, statsStyle);
        }

        private float CalculateMaxWidth()
        {
            float maxChars = 0;
            foreach (var line in diffLines)
            {
                maxChars = Mathf.Max(maxChars, line.content?.Length ?? 0);
            }

            float charWidth = 7f;
            float gutterWidth = 68f;
            return gutterWidth + maxChars * charWidth + 40;
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

            GUI.Label(labelRect, "No changes to display\nDiff will appear when comparing code", style);
        }

        /// <summary>
        /// Get diff statistics
        /// </summary>
        public (int added, int removed, int unchanged) GetStats()
        {
            int added = 0, removed = 0, unchanged = 0;

            foreach (var line in diffLines)
            {
                switch (line.type)
                {
                    case DiffType.Added: added++; break;
                    case DiffType.Removed: removed++; break;
                    case DiffType.Unchanged: unchanged++; break;
                }
            }

            return (added, removed, unchanged);
        }
    }
}
