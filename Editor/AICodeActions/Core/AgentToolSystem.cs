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
                "Add a built-in Unity component to a GameObject (e.g., Rigidbody, CharacterController, BoxCollider). For EXISTING compiled scripts, use this. For NEW scripts, use create_and_attach_script.",
                new string[] { "gameobject_name", "component_type" },
                (p) => UnityAgentTools.AddComponent(p["gameobject_name"], p["component_type"]));
            
            RegisterTool("create_and_attach_script",
                "Create a NEW C# script with code and attach it to a GameObject. Use this when you need to write custom behavior/logic. The script will be compiled and automatically attached.",
                new string[] { "gameobject_name", "script_name", "script_content" },
                (p) => UnityAgentTools.CreateAndAttachScript(p["gameobject_name"], p["script_name"], p["script_content"]));
            
            // File reading tools
            RegisterTool("read_script",
                "Read the content of a C# script file by name (e.g., 'PlayerController' or 'PlayerController.cs')",
                new string[] { "script_name" },
                (p) => UnityAgentTools.ReadScript(p["script_name"]));
            
            RegisterTool("read_file",
                "Read any file content from Assets folder (supports .txt, .json, .xml, .md, etc.)",
                new string[] { "file_path" },
                (p) => UnityAgentTools.ReadFile(p["file_path"]));
            
            RegisterTool("get_gameobject_info",
                "Get detailed information about a GameObject including all components and their scripts",
                new string[] { "gameobject_name" },
                (p) => UnityAgentTools.GetGameObjectInfo(p["gameobject_name"]));
            
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
            sb.AppendLine("scriptContent: FULL C# CODE HERE (if creating script)");
            sb.AppendLine("[/TOOL]");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("⚠️ IMPORTANT: When using create_and_attach_script, you MUST provide the COMPLETE C# script code in scriptContent parameter!");
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
                
                // Special note for script creation
                if (tool.name == "create_and_attach_script")
                {
                    sb.AppendLine();
                    sb.AppendLine("📝 **Example:**");
                    sb.AppendLine("```");
                    sb.AppendLine("[TOOL:create_and_attach_script]");
                    sb.AppendLine("gameobject_name: Player");
                    sb.AppendLine("script_name: PlayerController");
                    sb.AppendLine("script_content:");
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("");
                    sb.AppendLine("public class PlayerController : MonoBehaviour");
                    sb.AppendLine("{");
                    sb.AppendLine("    void Update()");
                    sb.AppendLine("    {");
                    sb.AppendLine("        // Your code here");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                    sb.AppendLine("[/TOOL]");
                    sb.AppendLine("```");
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
                
                // Normalize parameter keys to support both snake_case and camelCase
                var normalizedParams = new Dictionary<string, string>();
                foreach (var kvp in parameters)
                {
                    // Add original key
                    normalizedParams[kvp.Key] = kvp.Value;
                    
                    // Also add converted version
                    if (kvp.Key.Contains("_"))
                    {
                        // Convert snake_case to camelCase
                        string camelCase = ToCamelCase(kvp.Key);
                        if (!normalizedParams.ContainsKey(camelCase))
                        {
                            normalizedParams[camelCase] = kvp.Value;
                        }
                    }
                    else
                    {
                        // Convert camelCase to snake_case
                        string snakeCase = ToSnakeCase(kvp.Key);
                        if (!normalizedParams.ContainsKey(snakeCase))
                        {
                            normalizedParams[snakeCase] = kvp.Value;
                        }
                    }
                }
                
                var result = availableTools[toolName].function(normalizedParams);
                Debug.Log($"[Agent] Tool result: {result.Substring(0, Math.Min(100, result.Length))}...");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Agent] Tool execution failed: {e}");
                return $"❌ Tool execution failed: {e.Message}";
            }
        }
        
        private string ToCamelCase(string snakeCase)
        {
            var parts = snakeCase.Split('_');
            if (parts.Length == 0) return snakeCase;
            
            var result = parts[0].ToLower();
            for (int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    result += char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            return result;
        }
        
        private string ToSnakeCase(string camelCase)
        {
            var result = new StringBuilder();
            for (int i = 0; i < camelCase.Length; i++)
            {
                if (i > 0 && char.IsUpper(camelCase[i]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(camelCase[i]));
            }
            return result.ToString();
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
            
            string currentKey = null;
            var currentValue = new StringBuilder();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();
                
                // Skip empty lines at the start
                if (string.IsNullOrEmpty(trimmed) && currentKey == null) continue;
                
                // Check if this is a new parameter (key: value format)
                int colonIndex = trimmed.IndexOf(':');
                
                // New parameter detection: has colon AND not inside code block
                bool isNewParam = colonIndex > 0 && colonIndex < 50; // Keys are usually short
                
                if (isNewParam && currentKey == null)
                {
                    // First parameter
                    currentKey = trimmed.Substring(0, colonIndex).Trim();
                    string restOfLine = trimmed.Substring(colonIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(restOfLine))
                    {
                        currentValue.Append(restOfLine);
                    }
                }
                else if (isNewParam && !string.IsNullOrEmpty(currentKey))
                {
                    // Check if this looks like a new parameter (not code like "void Update():")
                    string potentialKey = trimmed.Substring(0, colonIndex).Trim();
                    bool looksLikeCode = potentialKey.Contains(" ") || potentialKey.Contains("(") || potentialKey.Contains(")");
                    
                    if (!looksLikeCode && (potentialKey == "gameObjectName" || potentialKey == "scriptName" || 
                        potentialKey == "scriptContent" || potentialKey == "componentType" || 
                        potentialKey == "name" || potentialKey == "parent" ||
                        potentialKey == "gameobject_name" || potentialKey == "script_name" ||
                        potentialKey == "script_content" || potentialKey == "component_type"))
                    {
                        // Save previous parameter
                        parameters[currentKey] = currentValue.ToString().Trim();
                        currentValue.Clear();
                        
                        // Start new parameter
                        currentKey = potentialKey;
                        string restOfLine = trimmed.Substring(colonIndex + 1).Trim();
                        if (!string.IsNullOrEmpty(restOfLine))
                        {
                            currentValue.Append(restOfLine);
                        }
                    }
                    else
                    {
                        // Part of multi-line value (code)
                        if (currentValue.Length > 0) currentValue.AppendLine();
                        currentValue.Append(line); // Use original line to preserve indentation
                    }
                }
                else if (currentKey != null)
                {
                    // Continue multi-line value
                    if (currentValue.Length > 0) currentValue.AppendLine();
                    currentValue.Append(line); // Preserve original indentation
                }
            }
            
            // Save last parameter
            if (currentKey != null)
            {
                parameters[currentKey] = currentValue.ToString().Trim();
            }
            
            // Debug log
            Debug.Log($"[AgentToolSystem] Parsed {parameters.Count} parameters:");
            foreach (var kvp in parameters)
            {
                string preview = kvp.Value.Length > 100 ? kvp.Value.Substring(0, 100) + "..." : kvp.Value;
                Debug.Log($"  - {kvp.Key}: {preview}");
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

