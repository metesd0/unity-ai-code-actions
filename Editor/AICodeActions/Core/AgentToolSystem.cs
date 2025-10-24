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
        
        // Context tracking for better AI understanding
        public string LastCreatedScript { get; private set; } = "";
        public string LastCreatedGameObject { get; private set; } = "";
        public string LastModifiedGameObject { get; private set; } = "";
        public List<string> RecentScripts { get; private set; } = new List<string>();
        public List<string> RecentGameObjects { get; private set; } = new List<string>();
        
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
            
            RegisterTool("create_primitive",
                "Create a primitive GameObject (Cube, Sphere, Capsule, Cylinder, Plane, Quad) at a specific position",
                new string[] { "primitive_type", "name", "x", "y", "z" },
                (p) => UnityAgentTools.CreatePrimitive(
                    p["primitive_type"],
                    p.ContainsKey("name") ? p["name"] : null,
                    p.ContainsKey("x") ? float.Parse(p["x"]) : 0,
                    p.ContainsKey("y") ? float.Parse(p["y"]) : 0,
                    p.ContainsKey("z") ? float.Parse(p["z"]) : 0));
            
            RegisterTool("set_position",
                "Set GameObject position in world space",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetPosition(p["gameobject_name"], float.Parse(p["x"]), float.Parse(p["y"]), float.Parse(p["z"])));
            
            RegisterTool("set_rotation",
                "Set GameObject rotation using Euler angles (degrees)",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetRotation(p["gameobject_name"], float.Parse(p["x"]), float.Parse(p["y"]), float.Parse(p["z"])));
            
            RegisterTool("set_scale",
                "Set GameObject scale",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetScale(p["gameobject_name"], float.Parse(p["x"]), float.Parse(p["y"]), float.Parse(p["z"])));
            
            RegisterTool("delete_gameobject",
                "Delete a GameObject from the scene (supports undo)",
                new string[] { "gameobject_name" },
                (p) => UnityAgentTools.DeleteGameObject(p["gameobject_name"]));
            
            RegisterTool("set_parent",
                "Set the parent of a GameObject (hierarchy organization). Use 'null' or empty string to move to root.",
                new string[] { "child_name", "parent_name" },
                (p) => UnityAgentTools.SetParent(p["child_name"], p.ContainsKey("parent_name") ? p["parent_name"] : null));
            
            RegisterTool("set_active",
                "Enable or disable a GameObject (active/inactive state)",
                new string[] { "gameobject_name", "active" },
                (p) => UnityAgentTools.SetActive(p["gameobject_name"], p["active"].ToLower() == "true"));
            
            RegisterTool("rename_gameobject",
                "Rename a GameObject",
                new string[] { "old_name", "new_name" },
                (p) => UnityAgentTools.RenameGameObject(p["old_name"], p["new_name"]));
            
            RegisterTool("duplicate_gameobject",
                "Duplicate a GameObject with all its components and children",
                new string[] { "name", "new_name" },
                (p) => UnityAgentTools.DuplicateGameObject(p["name"], p.ContainsKey("new_name") ? p["new_name"] : null));
            
            RegisterTool("set_tag",
                "Set the tag of a GameObject (e.g., 'Player', 'Enemy', 'Untagged')",
                new string[] { "gameobject_name", "tag" },
                (p) => UnityAgentTools.SetTag(p["gameobject_name"], p["tag"]));
            
            RegisterTool("set_layer",
                "Set the layer of a GameObject (e.g., 'Default', 'UI', 'Water')",
                new string[] { "gameobject_name", "layer_name" },
                (p) => UnityAgentTools.SetLayer(p["gameobject_name"], p["layer_name"]));
            
            RegisterTool("add_component",
                "Add a built-in Unity component to a GameObject (e.g., Rigidbody, CharacterController, BoxCollider, AudioSource, Light).",
                new string[] { "gameobject_name", "component_type" },
                (p) => UnityAgentTools.AddComponent(p["gameobject_name"], p["component_type"]));
            
            RegisterTool("set_component_property",
                "Set a property/field value on a component (e.g., set Camera reference, float values, colors). Supports: Transform/GameObject references, float, int, bool, string, Vector3, Color.",
                new string[] { "gameobject_name", "component_type", "property_name", "value" },
                (p) => UnityAgentTools.SetComponentProperty(p["gameobject_name"], p["component_type"], p["property_name"], p["value"]));
            
            RegisterTool("attach_script",
                "Attach an EXISTING compiled C# script to a GameObject. The script must already exist in the project. Use this when you want to add a script that's already created.",
                new string[] { "gameobject_name", "script_name" },
                (p) => UnityAgentTools.AttachScript(p["gameobject_name"], p["script_name"]));
            
            RegisterTool("create_and_attach_script",
                "Create a NEW C# script with code and attach it to a GameObject. Use this when you need to write custom behavior/logic from scratch.",
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
            
            // Material & Visual
            RegisterTool("create_material",
                "Create a new material asset with optional color (Standard shader)",
                new string[] { "name", "color" },
                (p) => UnityAgentTools.CreateMaterial(p["name"], p.ContainsKey("color") ? p["color"] : null));
            
            RegisterTool("assign_material",
                "Assign a material to a GameObject's renderer",
                new string[] { "gameobject_name", "material_name" },
                (p) => UnityAgentTools.AssignMaterial(p["gameobject_name"], p["material_name"]));
            
            RegisterTool("create_light",
                "Create a light GameObject (types: directional, point, spot, area)",
                new string[] { "name", "light_type", "color", "intensity" },
                (p) => UnityAgentTools.CreateLight(
                    p["name"],
                    p["light_type"],
                    p.ContainsKey("color") ? p["color"] : "white",
                    p.ContainsKey("intensity") ? float.Parse(p["intensity"]) : 1.0f));
            
            RegisterTool("create_camera",
                "Create a camera GameObject with optional field of view",
                new string[] { "name", "field_of_view" },
                (p) => UnityAgentTools.CreateCamera(
                    p["name"],
                    p.ContainsKey("field_of_view") ? float.Parse(p["field_of_view"]) : 60f));
            
            // Scene Management
            RegisterTool("save_scene",
                "Save the current scene to its existing path",
                new string[] { },
                (p) => UnityAgentTools.SaveScene());
            
            RegisterTool("save_scene_as",
                "Save the current scene with a new name/path in Assets folder",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.SaveSceneAs(p["scene_name"]));
            
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
        /// Update context based on tool execution
        /// </summary>
        private void UpdateContext(string toolName, Dictionary<string, string> parameters, string result)
        {
            try
            {
                // Track created scripts
                if (toolName == "create_and_attach_script" && result.Contains("✅"))
                {
                    if (parameters.ContainsKey("script_name") || parameters.ContainsKey("scriptName"))
                    {
                        string scriptName = parameters.ContainsKey("script_name") 
                            ? parameters["script_name"] 
                            : parameters["scriptName"];
                        
                        LastCreatedScript = scriptName;
                        if (!RecentScripts.Contains(scriptName))
                        {
                            RecentScripts.Add(scriptName);
                            if (RecentScripts.Count > 10) RecentScripts.RemoveAt(0); // Keep last 10
                        }
                        Debug.Log($"[Context] Last created script: {scriptName}");
                    }
                }
                
                // Track created GameObjects
                if ((toolName == "create_gameobject" || toolName == "create_primitive") && result.Contains("✅"))
                {
                    string goName = parameters.ContainsKey("name") ? parameters["name"] : null;
                    if (goName == null && toolName == "create_primitive")
                    {
                        goName = parameters.ContainsKey("primitive_type") ? parameters["primitive_type"] : null;
                    }
                    
                    if (!string.IsNullOrEmpty(goName))
                    {
                        LastCreatedGameObject = goName;
                        if (!RecentGameObjects.Contains(goName))
                        {
                            RecentGameObjects.Add(goName);
                            if (RecentGameObjects.Count > 20) RecentGameObjects.RemoveAt(0); // Keep last 20
                        }
                        Debug.Log($"[Context] Last created GameObject: {goName}");
                    }
                }
                
                // Track modified GameObjects
                if ((toolName == "set_position" || toolName == "set_rotation" || toolName == "set_scale" || 
                     toolName == "add_component" || toolName == "attach_script" || toolName == "set_component_property") && result.Contains("✅"))
                {
                    string goName = parameters.ContainsKey("gameobject_name") ? parameters["gameobject_name"] 
                        : parameters.ContainsKey("gameObjectName") ? parameters["gameObjectName"] : null;
                    
                    if (!string.IsNullOrEmpty(goName))
                    {
                        LastModifiedGameObject = goName;
                        Debug.Log($"[Context] Last modified GameObject: {goName}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Context] Error updating context: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get context summary for AI
        /// </summary>
        public string GetContextSummary()
        {
            var summary = new StringBuilder();
            
            if (!string.IsNullOrEmpty(LastCreatedScript))
            {
                summary.AppendLine($"📝 Last created script: {LastCreatedScript}");
            }
            
            if (!string.IsNullOrEmpty(LastCreatedGameObject))
            {
                summary.AppendLine($"🎮 Last created GameObject: {LastCreatedGameObject}");
            }
            
            if (!string.IsNullOrEmpty(LastModifiedGameObject))
            {
                summary.AppendLine($"🔧 Last modified GameObject: {LastModifiedGameObject}");
            }
            
            if (RecentScripts.Count > 0)
            {
                summary.AppendLine($"📚 Recent scripts: {string.Join(", ", RecentScripts)}");
            }
            
            if (RecentGameObjects.Count > 0)
            {
                summary.AppendLine($"🎯 Recent GameObjects: {string.Join(", ", RecentGameObjects)}");
            }
            
            return summary.ToString();
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
                
                // Track context from tool execution
                UpdateContext(toolName, parameters, toolResult);
                
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
                        potentialKey == "script_content" || potentialKey == "component_type" ||
                        potentialKey == "search_term" || potentialKey == "by_tag" ||
                        potentialKey == "property_name" || potentialKey == "value" ||
                        potentialKey == "x" || potentialKey == "y" || potentialKey == "z" ||
                        potentialKey == "primitive_type" || potentialKey == "file_path" ||
                        potentialKey == "filter" || char.IsLower(potentialKey[0])))
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

