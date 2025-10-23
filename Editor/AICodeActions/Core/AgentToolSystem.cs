using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages tool calls for AI Agent
    /// </summary>
    public class AgentToolSystem
    {
        public delegate string ToolFunction(Dictionary<string, string> parameters);
        
        private Dictionary<string, ToolInfo> availableTools = new Dictionary<string, ToolInfo>();
        
        public AgentToolSystem()
        {
            RegisterDefaultTools();
        }
        
        private void RegisterDefaultTools()
        {
            // Scene inspection
            RegisterTool("get_scene_info", 
                "Get current Unity scene hierarchy and GameObjects",
                new string[] { },
                (p) => UnityAgentTools.GetSceneInfo());
            
            RegisterTool("get_gameobject_info",
                "Get detailed information about a specific GameObject",
                new string[] { "name" },
                (p) => UnityAgentTools.GetGameObjectInfo(p["name"]));
            
            RegisterTool("find_gameobjects",
                "Find GameObjects by name or tag",
                new string[] { "search_term", "by_tag" },
                (p) => UnityAgentTools.FindGameObjects(p["search_term"], p.ContainsKey("by_tag") && p["by_tag"] == "true"));
            
            // GameObject manipulation
            RegisterTool("create_gameobject",
                "Create a new GameObject in the scene",
                new string[] { "name", "parent" },
                (p) => UnityAgentTools.CreateGameObject(p["name"], p.ContainsKey("parent") ? p["parent"] : null));
            
            RegisterTool("add_component",
                "Add a component to a GameObject",
                new string[] { "gameobject_name", "component_type" },
                (p) => UnityAgentTools.AddComponent(p["gameobject_name"], p["component_type"]));
            
            RegisterTool("create_and_attach_script",
                "Create a new C# script and attach it to a GameObject",
                new string[] { "gameobject_name", "script_name", "script_content" },
                (p) => UnityAgentTools.CreateAndAttachScript(p["gameobject_name"], p["script_name"], p["script_content"]));
            
            // Project inspection
            RegisterTool("list_scripts",
                "List all C# scripts in the project",
                new string[] { "filter" },
                (p) => UnityAgentTools.ListScripts(p.ContainsKey("filter") ? p["filter"] : ""));
            
            RegisterTool("get_project_stats",
                "Get project statistics (assets, scenes, etc.)",
                new string[] { },
                (p) => UnityAgentTools.GetProjectStats());
        }
        
        public void RegisterTool(string name, string description, string[] parameters, ToolFunction function)
        {
            availableTools[name] = new ToolInfo
            {
                name = name,
                description = description,
                parameters = parameters,
                function = function
            };
        }
        
        public string GetToolsDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Available Unity Tools");
            sb.AppendLine();
            sb.AppendLine("You can use these tools to interact with Unity. To use a tool, format your response like:");
            sb.AppendLine("```");
            sb.AppendLine("[TOOL:tool_name]");
            sb.AppendLine("param1: value1");
            sb.AppendLine("param2: value2");
            sb.AppendLine("[/TOOL]");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Tools:");
            
            foreach (var tool in availableTools.Values)
            {
                sb.AppendLine();
                sb.AppendLine($"### {tool.name}");
                sb.AppendLine($"**Description:** {tool.description}");
                
                if (tool.parameters.Length > 0)
                {
                    sb.AppendLine($"**Parameters:** {string.Join(", ", tool.parameters)}");
                }
                else
                {
                    sb.AppendLine("**Parameters:** None");
                }
            }
            
            return sb.ToString();
        }
        
        public string ExecuteTool(string toolName, Dictionary<string, string> parameters)
        {
            if (!availableTools.ContainsKey(toolName))
            {
                return $"❌ Unknown tool: {toolName}";
            }
            
            try
            {
                Debug.Log($"[Agent] Executing tool: {toolName}");
                var result = availableTools[toolName].function(parameters);
                Debug.Log($"[Agent] Tool result: {result.Substring(0, Math.Min(100, result.Length))}...");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Agent] Tool execution failed: {e}");
                return $"❌ Tool execution failed: {e.Message}";
            }
        }
        
        /// <summary>
        /// Parse and execute tool calls from AI response
        /// </summary>
        public string ProcessToolCalls(string response)
        {
            var result = new StringBuilder();
            
            // Check if response contains tool calls
            bool hasToolCalls = response.Contains("[TOOL:");
            
            if (!hasToolCalls)
            {
                // No tool calls, just return the response
                return response;
            }
            
            result.AppendLine("## AI Response:");
            result.AppendLine(response);
            result.AppendLine();
            result.AppendLine("---");
            result.AppendLine("## Tool Execution Results:");
            result.AppendLine();
            
            // Find all tool calls in format [TOOL:name]...[/TOOL]
            int startIndex = 0;
            int toolCount = 0;
            
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
                
                toolCount++;
                
                // Execute tool with visual feedback
                result.AppendLine($"### 🔧 Tool {toolCount}: {toolName}");
                result.AppendLine($"**Parameters:**");
                foreach (var param in parameters)
                {
                    string valuePreview = param.Value.Length > 100 
                        ? param.Value.Substring(0, 100) + "..." 
                        : param.Value;
                    result.AppendLine($"- {param.Key}: {valuePreview}");
                }
                result.AppendLine();
                
                result.AppendLine("**Result:**");
                string toolResult = ExecuteTool(toolName, parameters);
                result.AppendLine(toolResult);
                result.AppendLine();
                result.AppendLine("---");
                result.AppendLine();
                
                startIndex = toolEnd + 7;
            }
            
            if (toolCount > 0)
            {
                result.AppendLine($"✅ Executed {toolCount} tool(s) successfully!");
            }
            
            return result.ToString();
        }
        
        private Dictionary<string, string> ParseParameters(string paramSection)
        {
            var parameters = new Dictionary<string, string>();
            var lines = paramSection.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                
                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = trimmed.Substring(0, colonIndex).Trim();
                    string value = trimmed.Substring(colonIndex + 1).Trim();
                    parameters[key] = value;
                }
            }
            
            return parameters;
        }
        
        public class ToolInfo
        {
            public string name;
            public string description;
            public string[] parameters;
            public ToolFunction function;
        }
    }
}

