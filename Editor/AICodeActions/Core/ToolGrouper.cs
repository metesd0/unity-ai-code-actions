using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AICodeActions.Core
{
    /// <summary>
    /// Represents a parsed tool call from AI response
    /// </summary>
    public class ParsedToolCall
    {
        public string ToolName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        
        public ParsedToolCall()
        {
            Parameters = new Dictionary<string, string>();
        }
        
        // Get the target object name (for grouping logic)
        public string GetTargetObjectName()
        {
            // Check common parameter names for target object
            if (Parameters.ContainsKey("name")) return Parameters["name"];
            if (Parameters.ContainsKey("gameobject_name")) return Parameters["gameobject_name"];
            if (Parameters.ContainsKey("material_name")) return Parameters["material_name"];
            return null;
        }
        
        // Check if this is a query tool (needs to wait for result)
        public bool IsQueryTool()
        {
            return ToolName == "find_gameobjects" || 
                   ToolName == "get_scene_info" || 
                   ToolName == "get_component_property" ||
                   ToolName == "get_gameobject_info";
        }
    }
    
    /// <summary>
    /// Represents a group of related tools to execute together
    /// </summary>
    public class ToolGroup
    {
        public List<ParsedToolCall> Tools { get; set; }
        public string GroupName { get; set; }
        public string TargetObject { get; set; }
        
        public ToolGroup()
        {
            Tools = new List<ParsedToolCall>();
        }
    }
    
    /// <summary>
    /// Parses and groups tools from AI response for sequential execution
    /// </summary>
    public class ToolGrouper
    {
        /// <summary>
        /// Parse all tools from AI response
        /// </summary>
        public static List<ParsedToolCall> ParseTools(string response)
        {
            var tools = new List<ParsedToolCall>();
            int startIndex = 0;
            
            while (true)
            {
                int toolStart = response.IndexOf("[TOOL:", startIndex);
                if (toolStart == -1) break;
                
                int toolNameEnd = response.IndexOf("]", toolStart);
                if (toolNameEnd == -1) break;
                
                string toolName = response.Substring(toolStart + 6, toolNameEnd - toolStart - 6).Trim();
                
                int toolEnd = response.IndexOf("[/TOOL]", toolNameEnd);
                if (toolEnd == -1) break;
                
                // Extract parameters
                string paramSection = response.Substring(toolNameEnd + 1, toolEnd - toolNameEnd - 1);
                var parameters = ParseParameters(paramSection);
                
                var tool = new ParsedToolCall
                {
                    ToolName = toolName,
                    Parameters = parameters,
                    StartIndex = toolStart,
                    EndIndex = toolEnd + 7
                };
                
                tools.Add(tool);
                startIndex = toolEnd + 7;
            }
            
            return tools;
        }
        
        /// <summary>
        /// Group tools into logical execution groups
        /// </summary>
        public static List<ToolGroup> GroupTools(List<ParsedToolCall> tools)
        {
            var groups = new List<ToolGroup>();
            
            if (tools.Count == 0) return groups;
            
            var currentGroup = new ToolGroup();
            string currentTarget = null;
            
            foreach (var tool in tools)
            {
                // Rule 1: Query tools always get their own group
                if (tool.IsQueryTool())
                {
                    // Finish current group if it has tools
                    if (currentGroup.Tools.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = new ToolGroup();
                        currentTarget = null;
                    }
                    
                    // Add query tool alone
                    var queryGroup = new ToolGroup
                    {
                        GroupName = $"Query: {tool.ToolName}",
                        TargetObject = "Query"
                    };
                    queryGroup.Tools.Add(tool);
                    groups.Add(queryGroup);
                    continue;
                }
                
                // Rule 2: Group by target object
                string toolTarget = tool.GetTargetObjectName();
                
                // If this is a new target or group is getting too large (max 5 tools)
                if (currentTarget != null && 
                    (toolTarget != currentTarget || currentGroup.Tools.Count >= 5))
                {
                    // Finish current group
                    groups.Add(currentGroup);
                    currentGroup = new ToolGroup();
                    currentTarget = null;
                }
                
                // Add tool to current group
                currentGroup.Tools.Add(tool);
                
                // Update target tracking
                if (toolTarget != null)
                {
                    if (currentTarget == null)
                    {
                        currentTarget = toolTarget;
                        currentGroup.TargetObject = toolTarget;
                        currentGroup.GroupName = $"Object: {toolTarget}";
                    }
                }
                else
                {
                    // Tool without specific target (like create_material)
                    if (currentTarget == null)
                    {
                        currentGroup.GroupName = $"Operations: {tool.ToolName}";
                    }
                }
            }
            
            // Add remaining group
            if (currentGroup.Tools.Count > 0)
            {
                groups.Add(currentGroup);
            }
            
            return groups;
        }
        
        /// <summary>
        /// Parse parameters from tool parameter section
        /// </summary>
        private static Dictionary<string, string> ParseParameters(string paramSection)
        {
            var parameters = new Dictionary<string, string>();
            var lines = paramSection.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = line.Substring(0, colonIndex).Trim();
                    string value = line.Substring(colonIndex + 1).Trim();
                    
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        parameters[key] = value;
                    }
                }
            }
            
            return parameters;
        }
    }
}

