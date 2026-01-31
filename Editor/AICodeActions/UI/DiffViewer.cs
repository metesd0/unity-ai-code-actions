using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Side-by-side diff viewer for code changes
    /// Shows additions, deletions, and modifications
    /// </summary>
    public class DiffViewer
    {
        public class DiffResult
        {
            public List<DiffLine> lines;
            public int additions;
            public int deletions;
            public int modifications;
            
            public DiffResult()
            {
                lines = new List<DiffLine>();
            }
            
            public string GetSummary()
            {
                return $"+{additions} -{deletions} ~{modifications}";
            }
        }
        
        public class DiffLine
        {
            public int lineNumber;
            public string content;
            public DiffType type;
            public string oldContent; // For modifications
            
            public DiffLine(int lineNumber, string content, DiffType type)
            {
                this.lineNumber = lineNumber;
                this.content = content;
                this.type = type;
            }
        }
        
        public enum DiffType
        {
            Unchanged,
            Added,
            Deleted,
            Modified
        }
        
        public enum ViewMode
        {
            SideBySide,
            Unified,
            Split
        }
        
        private Vector2 scrollPos;
        private ViewMode currentMode = ViewMode.Unified;
        private GUIStyle lineStyle;
        private GUIStyle addedStyle;
        private GUIStyle deletedStyle;
        private GUIStyle modifiedStyle;
        private GUIStyle lineNumberStyle;
        private bool stylesInitialized = false;
        
        /// <summary>
        /// Compute diff between two texts
        /// </summary>
        public DiffResult ComputeDiff(string oldText, string newText)
        {
            var result = new DiffResult();
            
            var oldLines = (oldText ?? "").Split('\n');
            var newLines = (newText ?? "").Split('\n');
            
            // Simple line-by-line diff (can be improved with Myers algorithm)
            var lcs = LongestCommonSubsequence(oldLines, newLines);
            
            int oldIndex = 0;
            int newIndex = 0;
            int lineNumber = 1;
            
            while (oldIndex < oldLines.Length || newIndex < newLines.Length)
            {
                if (oldIndex < oldLines.Length && newIndex < newLines.Length &&
                    oldLines[oldIndex] == newLines[newIndex])
                {
                    // Unchanged line
                    result.lines.Add(new DiffLine(lineNumber, newLines[newIndex], DiffType.Unchanged));
                    oldIndex++;
                    newIndex++;
                    lineNumber++;
                }
                else if (oldIndex < oldLines.Length && 
                        (newIndex >= newLines.Length || !lcs.Contains(oldLines[oldIndex])))
                {
                    // Deleted line
                    result.lines.Add(new DiffLine(lineNumber, oldLines[oldIndex], DiffType.Deleted));
                    result.deletions++;
                    oldIndex++;
                }
                else if (newIndex < newLines.Length)
                {
                    // Added line
                    result.lines.Add(new DiffLine(lineNumber, newLines[newIndex], DiffType.Added));
                    result.additions++;
                    newIndex++;
                    lineNumber++;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Draw diff viewer
        /// </summary>
        public void DrawDiff(DiffResult diff, string title = "Code Changes")
        {
            InitializeStyles();
            
            if (diff == null || diff.lines.Count == 0)
            {
                EditorGUILayout.HelpBox("No changes to display", MessageType.Info);
                return;
            }
            
            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"<b>{title}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            GUILayout.FlexibleSpace();
            GUILayout.Label(diff.GetSummary(), EditorStyles.miniLabel);
            
            // View mode selector
            currentMode = (ViewMode)EditorGUILayout.EnumPopup(currentMode, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
            
            // Diff content
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            switch (currentMode)
            {
                case ViewMode.Unified:
                    DrawUnifiedDiff(diff);
                    break;
                case ViewMode.SideBySide:
                    DrawSideBySideDiff(diff);
                    break;
                case ViewMode.Split:
                    DrawSplitDiff(diff);
                    break;
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        /// <summary>
        /// Draw unified diff view (GitHub style)
        /// </summary>
        private void DrawUnifiedDiff(DiffResult diff)
        {
            foreach (var line in diff.lines)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Line number
                string lineNum = line.type != DiffType.Deleted ? line.lineNumber.ToString() : "-";
                GUILayout.Label(lineNum, lineNumberStyle, GUILayout.Width(40));
                
                // Line prefix
                string prefix = line.type switch
                {
                    DiffType.Added => "+ ",
                    DiffType.Deleted => "- ",
                    DiffType.Modified => "~ ",
                    _ => "  "
                };
                
                // Line content with styling
                GUIStyle style = GetLineStyle(line.type);
                GUILayout.Label(prefix + line.content, style);
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        /// <summary>
        /// Draw side-by-side diff view
        /// </summary>
        private void DrawSideBySideDiff(DiffResult diff)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Left side (old/deleted)
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 10));
            GUILayout.Label("<b>Before</b>", EditorStyles.boldLabel);
            
            foreach (var line in diff.lines.Where(l => l.type == DiffType.Deleted || l.type == DiffType.Unchanged))
            {
                GUIStyle style = line.type == DiffType.Deleted ? deletedStyle : lineStyle;
                GUILayout.Label($"{line.lineNumber}: {line.content}", style);
            }
            
            EditorGUILayout.EndVertical();
            
            // Divider
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            GUILayout.Box("", GUILayout.ExpandHeight(true), GUILayout.Width(2));
            EditorGUILayout.EndVertical();
            
            // Right side (new/added)
            EditorGUILayout.BeginVertical();
            GUILayout.Label("<b>After</b>", EditorStyles.boldLabel);
            
            foreach (var line in diff.lines.Where(l => l.type == DiffType.Added || l.type == DiffType.Unchanged))
            {
                GUIStyle style = line.type == DiffType.Added ? addedStyle : lineStyle;
                GUILayout.Label($"{line.lineNumber}: {line.content}", style);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draw split diff view (compact)
        /// </summary>
        private void DrawSplitDiff(DiffResult diff)
        {
            // Only show changed lines
            var changedLines = diff.lines.Where(l => l.type != DiffType.Unchanged).ToList();
            
            if (changedLines.Count == 0)
            {
                EditorGUILayout.HelpBox("No changes", MessageType.Info);
                return;
            }
            
            foreach (var line in changedLines)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Line number
                GUILayout.Label(line.lineNumber.ToString(), lineNumberStyle, GUILayout.Width(40));
                
                // Change indicator
                string indicator = line.type switch
                {
                    DiffType.Added => "[+]",
                    DiffType.Deleted => "[-]",
                    DiffType.Modified => "[~]",
                    _ => "   "
                };
                
                Color color = GetDiffColor(line.type);
                var oldColor = GUI.contentColor;
                GUI.contentColor = color;
                GUILayout.Label(indicator, GUILayout.Width(30));
                GUI.contentColor = oldColor;
                
                // Content
                GUILayout.Label(line.content, GetLineStyle(line.type));
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        /// <summary>
        /// Get style for line type
        /// </summary>
        private GUIStyle GetLineStyle(DiffType type)
        {
            return type switch
            {
                DiffType.Added => addedStyle,
                DiffType.Deleted => deletedStyle,
                DiffType.Modified => modifiedStyle,
                _ => lineStyle
            };
        }
        
        /// <summary>
        /// Get color for diff type
        /// </summary>
        private Color GetDiffColor(DiffType type)
        {
            return type switch
            {
                DiffType.Added => new Color(0.3f, 1f, 0.3f),
                DiffType.Deleted => new Color(1f, 0.3f, 0.3f),
                DiffType.Modified => new Color(1f, 0.8f, 0.3f),
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Initialize GUI styles
        /// </summary>
        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;
            
            lineStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                wordWrap = false
            };
            
            addedStyle = new GUIStyle(lineStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.2f, 0.6f, 0.2f, 0.3f)) }
            };
            
            deletedStyle = new GUIStyle(lineStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.6f, 0.2f, 0.2f, 0.3f)) }
            };
            
            modifiedStyle = new GUIStyle(lineStyle)
            {
                normal = { background = MakeTex(2, 2, new Color(0.6f, 0.5f, 0.2f, 0.3f)) }
            };
            
            lineNumberStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = Color.gray }
            };
            
            stylesInitialized = true;
        }
        
        /// <summary>
        /// Make texture for background
        /// </summary>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        /// <summary>
        /// Longest Common Subsequence (for diff algorithm)
        /// </summary>
        private HashSet<string> LongestCommonSubsequence(string[] arr1, string[] arr2)
        {
            int m = arr1.Length;
            int n = arr2.Length;
            
            int[,] dp = new int[m + 1, n + 1];
            
            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    if (arr1[i - 1] == arr2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                    }
                }
            }
            
            // Backtrack to find LCS
            var lcs = new HashSet<string>();
            int x = m, y = n;
            
            while (x > 0 && y > 0)
            {
                if (arr1[x - 1] == arr2[y - 1])
                {
                    lcs.Add(arr1[x - 1]);
                    x--;
                    y--;
                }
                else if (dp[x - 1, y] > dp[x, y - 1])
                {
                    x--;
                }
                else
                {
                    y--;
                }
            }
            
            return lcs;
        }
        
        // For side-by-side positioning
        private static Rect position = new Rect(0, 0, 800, 600);
        
        public static void SetPosition(Rect rect)
        {
            position = rect;
        }
    }
}

