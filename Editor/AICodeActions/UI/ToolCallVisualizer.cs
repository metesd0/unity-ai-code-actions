using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Visual representation of tool calls in chat
    /// Collapsible, with icons and status indicators
    /// </summary>
    public class ToolCallVisualizer
    {
        public class ToolCall
        {
            public string toolName;
            public Dictionary<string, string> parameters;
            public string result;
            public ToolStatus status;
            public float executionTime;
            public bool isExpanded;
            public DateTime timestamp;
            
            public ToolCall(string toolName)
            {
                this.toolName = toolName;
                this.parameters = new Dictionary<string, string>();
                this.status = ToolStatus.Pending;
                this.isExpanded = false;
                this.timestamp = DateTime.Now;
            }
        }
        
        public enum ToolStatus
        {
            Pending,
            Running,
            Success,
            Failed,
            Cancelled
        }
        
        private GUIStyle toolBoxStyle;
        private GUIStyle toolHeaderStyle;
        private GUIStyle toolContentStyle;
        private GUIStyle parameterStyle;
        private bool stylesInitialized = false;
        
        /// <summary>
        /// Draw tool call in GUI
        /// </summary>
        public void DrawToolCall(ToolCall toolCall)
        {
            InitializeStyles();
            
            EditorGUILayout.BeginVertical(toolBoxStyle);
            
            // Header with icon, name, status
            DrawToolHeader(toolCall);
            
            // Expandable content
            if (toolCall.isExpanded)
            {
                DrawToolContent(toolCall);
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }
        
        /// <summary>
        /// Draw tool header (clickable to expand)
        /// </summary>
        private void DrawToolHeader(ToolCall toolCall)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Status icon
            string statusIcon = GetStatusIcon(toolCall.status);
            Color statusColor = GetStatusColor(toolCall.status);
            
            var oldColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            GUILayout.Label(statusIcon, GUILayout.Width(20));
            GUI.contentColor = oldColor;
            
            // Expand/collapse arrow
            string arrow = toolCall.isExpanded ? "▼" : "▶";
            if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(15)))
            {
                toolCall.isExpanded = !toolCall.isExpanded;
            }
            
            // Tool name
            GUILayout.Label($"<b>{GetToolDisplayName(toolCall.toolName)}</b>", toolHeaderStyle);
            
            GUILayout.FlexibleSpace();
            
            // Execution time
            if (toolCall.executionTime > 0)
            {
                GUILayout.Label($"{toolCall.executionTime:F2}s", EditorStyles.miniLabel);
            }
            
            // Status badge
            GUILayout.Label(GetStatusText(toolCall.status), EditorStyles.miniLabel);
            
            EditorGUILayout.EndHorizontal();
        }
        
        /// <summary>
        /// Draw tool content (parameters and result)
        /// </summary>
        private void DrawToolContent(ToolCall toolCall)
        {
            EditorGUI.indentLevel++;
            
            // Parameters
            if (toolCall.parameters.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("<b>Parameters:</b>", parameterStyle);
                
                foreach (var param in toolCall.parameters)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"  • <color=#9CDCFE>{param.Key}</color>:", parameterStyle, GUILayout.Width(120));
                    
                    string value = param.Value;
                    if (value.Length > 50)
                        value = value.Substring(0, 50) + "...";
                    
                    GUILayout.Label(value, parameterStyle);
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            // Result
            if (!string.IsNullOrEmpty(toolCall.result))
            {
                GUILayout.Space(4);
                GUILayout.Label("<b>Result:</b>", parameterStyle);
                
                // Show compact result
                string compactResult = GetCompactResult(toolCall.result);
                GUILayout.Label(compactResult, toolContentStyle);
                
                // Copy button
                if (GUILayout.Button("Copy Full Result", GUILayout.Width(120)))
                {
                    GUIUtility.systemCopyBuffer = toolCall.result;
                    Debug.Log("Result copied to clipboard");
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// Get status icon
        /// </summary>
        private string GetStatusIcon(ToolStatus status)
        {
            return status switch
            {
                ToolStatus.Pending => "⏳",
                ToolStatus.Running => "⚡",
                ToolStatus.Success => "✅",
                ToolStatus.Failed => "❌",
                ToolStatus.Cancelled => "🚫",
                _ => "❓"
            };
        }
        
        /// <summary>
        /// Get status color
        /// </summary>
        private Color GetStatusColor(ToolStatus status)
        {
            return status switch
            {
                ToolStatus.Pending => new Color(1f, 0.8f, 0f), // Yellow
                ToolStatus.Running => new Color(0.3f, 0.7f, 1f), // Blue
                ToolStatus.Success => new Color(0.3f, 1f, 0.3f), // Green
                ToolStatus.Failed => new Color(1f, 0.3f, 0.3f), // Red
                ToolStatus.Cancelled => new Color(0.7f, 0.7f, 0.7f), // Gray
                _ => Color.white
            };
        }
        
        /// <summary>
        /// Get status text
        /// </summary>
        private string GetStatusText(ToolStatus status)
        {
            return status switch
            {
                ToolStatus.Pending => "Pending",
                ToolStatus.Running => "Running...",
                ToolStatus.Success => "Success",
                ToolStatus.Failed => "Failed",
                ToolStatus.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get tool display name (prettier version)
        /// </summary>
        private string GetToolDisplayName(string toolName)
        {
            // Convert snake_case to Title Case
            var words = toolName.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }
        
        /// <summary>
        /// Get compact result (first few lines)
        /// </summary>
        private string GetCompactResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return "";
            
            var lines = result.Split('\n');
            int maxLines = 3;
            
            if (lines.Length <= maxLines)
                return result;
            
            var compact = string.Join("\n", lines, 0, maxLines);
            return compact + $"\n... ({lines.Length - maxLines} more lines)";
        }
        
        /// <summary>
        /// Initialize GUI styles
        /// </summary>
        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;
            
            // Tool box style
            toolBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(4, 4, 2, 2)
            };
            
            // Tool header style
            toolHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            
            // Tool content style
            toolContentStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 10,
                wordWrap = true
            };
            
            // Parameter style
            parameterStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 10
            };
            
            stylesInitialized = true;
        }
        
        /// <summary>
        /// Parse tool call from text
        /// </summary>
        public static ToolCall ParseToolCall(string text)
        {
            // Parse format: [TOOL:name] ... [/TOOL]
            var match = System.Text.RegularExpressions.Regex.Match(
                text, 
                @"\[TOOL:(\w+)\](.*?)\[/TOOL\]", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (!match.Success)
                return null;
            
            string toolName = match.Groups[1].Value;
            string paramSection = match.Groups[2].Value;
            
            var toolCall = new ToolCall(toolName);
            
            // Parse parameters
            var lines = paramSection.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                int colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = line.Substring(0, colonIndex).Trim();
                    string value = line.Substring(colonIndex + 1).Trim();
                    toolCall.parameters[key] = value;
                }
            }
            
            return toolCall;
        }
        
        /// <summary>
        /// Create tool call progress display
        /// </summary>
        public static string CreateProgressDisplay(List<ToolCall> toolCalls)
        {
            int total = toolCalls.Count;
            int completed = toolCalls.FindAll(t => t.status == ToolStatus.Success || t.status == ToolStatus.Failed).Count;
            
            return $"Tool Progress: {completed}/{total} completed";
        }
    }
}

