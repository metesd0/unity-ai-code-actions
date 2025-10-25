using System;
using System.Collections.Generic;
using System.Text;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace AICodeActions.Core
{
    /// <summary>
    /// Extensions for AgentToolSystem to support grouped execution
    /// </summary>
    public static class AgentToolSystemExtensions
    {
        /// <summary>
        /// Process tool calls IN GROUPS for sequential execution with live progress
        /// </summary>
        public static List<string> ProcessToolCallsInGroups(this AgentToolSystem toolSystem, string response, Action<string> groupResultCallback)
        {
            var groupResults = new List<string>();
            
            // Parse all tools from response
            var parsedTools = ToolGrouper.ParseTools(response);
            
            if (parsedTools.Count == 0)
            {
                return groupResults;
            }
            
            // Group tools by logical units
            var toolGroups = ToolGrouper.GroupTools(parsedTools);
            
            // Execute each group sequentially
            int groupIndex = 0;
            foreach (var group in toolGroups)
            {
                groupIndex++;
                var groupResult = new StringBuilder();
                groupResult.AppendLine($"🤖 **AI Agent** (Group {groupIndex}/{toolGroups.Count}: {group.GroupName})");
                groupResult.AppendLine();
                
                // Execute all tools in this group
                int toolIndex = 0;
                var toolSummaries = new List<string>();
                
                foreach (var tool in group.Tools)
                {
                    toolIndex++;
                    
                    // Show tool starting
                    var paramSummary = GetParameterSummary(tool.Parameters, tool.ToolName);
                    var summary = $"⏳ **{toolIndex}.** {tool.ToolName}: {paramSummary}";
                    groupResult.AppendLine(summary);
                    toolSummaries.Add(summary);
                }
                
                groupResult.AppendLine();
                
                // Notify UI about this group's tools (before execution)
                groupResultCallback?.Invoke(groupResult.ToString());
                
                // Now execute them
                var detailedLog = new StringBuilder();
                var stopwatch = Stopwatch.StartNew();
                
                toolIndex = 0;
                foreach (var tool in group.Tools)
                {
                    toolIndex++;
                    
                    var toolStopwatch = Stopwatch.StartNew();
                    
                    // Execute tool using reflection to access private method
                    string toolResult = ExecuteToolViaReflection(toolSystem, tool.ToolName, tool.Parameters);
                    
                    toolStopwatch.Stop();
                    var elapsed = toolStopwatch.Elapsed.TotalSeconds;
                    
                    // Get icon
                    string icon = toolResult.Contains("✅") ? "✅" : 
                                  toolResult.Contains("❌") ? "❌" : 
                                  toolResult.Contains("⚠️") ? "⚠️" : "✅";
                    
                    string compactResult = GetCompactResult(toolResult);
                    detailedLog.AppendLine($"**{toolIndex}.** `{tool.ToolName}` {icon} {compactResult} ({elapsed:F3}s)");
                }
                
                stopwatch.Stop();
                
                // Finalize group result
                groupResult.AppendLine($"✅ **Completed {group.Tools.Count} tool(s)** in {stopwatch.Elapsed.TotalSeconds:F2}s");
                groupResult.AppendLine();
                groupResult.AppendLine("<details><summary>📊 Show Detailed Execution Log</summary>");
                groupResult.AppendLine();
                groupResult.AppendLine(detailedLog.ToString());
                groupResult.AppendLine("</details>");
                groupResult.AppendLine();
                
                // Store this group's result
                groupResults.Add(groupResult.ToString());
                
                // Show final result for this group
                groupResultCallback?.Invoke(groupResult.ToString());
            }
            
            return groupResults;
        }
        
        private static string ExecuteToolViaReflection(AgentToolSystem toolSystem, string toolName, Dictionary<string, string> parameters)
        {
            try
            {
                // Use reflection to call ExecuteToolWithValidation
                var method = typeof(AgentToolSystem).GetMethod("ExecuteToolWithValidation", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (method != null)
                {
                    return (string)method.Invoke(toolSystem, new object[] { toolName, parameters });
                }
                else
                {
                    return $"❌ Could not find ExecuteToolWithValidation method";
                }
            }
            catch (Exception ex)
            {
                return $"❌ Error executing {toolName}: {ex.Message}";
            }
        }
        
        private static string GetParameterSummary(Dictionary<string, string> parameters, string toolName)
        {
            if (parameters.Count == 0) return "()";
            
            // Special formatting for common tools
            if (toolName == "set_scale" || toolName == "set_position" || toolName == "set_rotation")
            {
                if (parameters.ContainsKey("gameobject_name"))
                {
                    string obj = parameters["gameobject_name"];
                    if (parameters.ContainsKey("x") && parameters.ContainsKey("y") && parameters.ContainsKey("z"))
                    {
                        return $"{obj} → ({parameters["x"]}, {parameters["y"]}, {parameters["z"]})";
                    }
                }
            }
            
            if (toolName == "create_material")
            {
                if (parameters.ContainsKey("name") && parameters.ContainsKey("color"))
                {
                    return $"{parameters["name"]}, {parameters["color"]}";
                }
            }
            
            if (toolName == "assign_material")
            {
                if (parameters.ContainsKey("gameobject_name") && parameters.ContainsKey("material_name"))
                {
                    return $"{parameters["gameobject_name"]}, {parameters["material_name"]}";
                }
            }
            
            // Default: show first 2-3 key parameters
            var summary = new List<string>();
            int count = 0;
            foreach (var kvp in parameters)
            {
                if (count >= 2) break;
                summary.Add($"{kvp.Key}: {kvp.Value}");
                count++;
            }
            
            return string.Join(", ", summary);
        }
        
        private static string GetCompactResult(string fullResult)
        {
            // Extract key info from full result
            if (fullResult.Contains("Created"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(fullResult, @"Created (\w+) '([^']+)'");
                if (match.Success)
                {
                    return $"Created {match.Groups[1].Value} '{match.Groups[2].Value}'";
                }
            }
            
            if (fullResult.Contains("Set "))
            {
                var match = System.Text.RegularExpressions.Regex.Match(fullResult, @"Set ([^\s]+)");
                if (match.Success)
                {
                    return fullResult.Split('\n')[0]; // First line usually has the summary
                }
            }
            
            if (fullResult.Contains("Found"))
            {
                return fullResult.Split('\n')[0];
            }
            
            // Default: first line or first 80 chars
            var lines = fullResult.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 0)
            {
                string firstLine = lines[0];
                return firstLine.Length > 80 ? firstLine.Substring(0, 77) + "..." : firstLine;
            }
            
            return fullResult.Length > 80 ? fullResult.Substring(0, 77) + "..." : fullResult;
        }
    }
}

