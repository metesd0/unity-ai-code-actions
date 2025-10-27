using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch; // Thread-safe timing

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages tool calls for AI Agent
    /// </summary>
    public class AgentToolSystem
    {
        public delegate string ToolFunction(Dictionary<string, string> parameters);
        
        private Dictionary<string, ToolInfo> availableTools = new Dictionary<string, ToolInfo>();
        private ToolResultInterceptor interceptor;
        
        // Context tracking for better AI understanding
        public string LastCreatedScript { get; private set; } = "";
        public string LastCreatedGameObject { get; private set; } = "";
        public string LastModifiedGameObject { get; private set; } = "";
        public List<string> RecentScripts { get; private set; } = new List<string>();
        public List<string> RecentGameObjects { get; private set; } = new List<string>();
        
        public AgentToolSystem()
        {
            RegisterDefaultTools();
            interceptor = new ToolResultInterceptor(this);
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
                    p.ContainsKey("x") ? float.Parse(p["x"], CultureInfo.InvariantCulture) : 0,
                    p.ContainsKey("y") ? float.Parse(p["y"], CultureInfo.InvariantCulture) : 0,
                    p.ContainsKey("z") ? float.Parse(p["z"], CultureInfo.InvariantCulture) : 0));
            
            RegisterTool("set_position",
                "Set GameObject position in world space",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetPosition(p["gameobject_name"], float.Parse(p["x"], CultureInfo.InvariantCulture), float.Parse(p["y"], CultureInfo.InvariantCulture), float.Parse(p["z"], CultureInfo.InvariantCulture)));
            
            RegisterTool("set_rotation",
                "Set GameObject rotation using Euler angles (degrees)",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetRotation(p["gameobject_name"], float.Parse(p["x"], CultureInfo.InvariantCulture), float.Parse(p["y"], CultureInfo.InvariantCulture), float.Parse(p["z"], CultureInfo.InvariantCulture)));
            
            RegisterTool("set_scale",
                "Set GameObject scale",
                new string[] { "gameobject_name", "x", "y", "z" },
                (p) => UnityAgentTools.SetScale(p["gameobject_name"], float.Parse(p["x"], CultureInfo.InvariantCulture), float.Parse(p["y"], CultureInfo.InvariantCulture), float.Parse(p["z"], CultureInfo.InvariantCulture)));
            
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
            
            RegisterTool("create_script",
                "✨ RECOMMENDED: Create a NEW C# script file (does NOT attach). Better than create_and_attach_script because: 1) Auto-checks compilation 2) Clear error messages 3) Can attach to multiple GameObjects",
                new string[] { "script_name", "script_content", "folder_path?" },
                (p) => UnityAgentTools.CreateScript(
                    p["script_name"], 
                    p["script_content"], 
                    p.ContainsKey("folder_path") ? p["folder_path"] : "Assets"));
            
            RegisterTool("create_and_attach_script",
                "[DEPRECATED] Create and attach script in ONE step. ⚠️ Use create_script + attach_script instead for better error handling!",
                new string[] { "gameobject_name", "script_name", "script_content" },
                (p) => UnityAgentTools.CreateAndAttachScript(p["gameobject_name"], p["script_name"], p["script_content"]));
            
                // Advanced Script Manipulation
            RegisterTool("modify_script",
                "Modify an existing script by appending code (for simple modifications)",
                new string[] { "script_name", "modifications" },
                (p) => UnityAgentTools.ModifyScript(p["script_name"], p["modifications"]));
            
            RegisterTool("add_method_to_script",
                "Add a new method to an existing script (inserted before last closing brace)",
                new string[] { "script_name", "method_code" },
                (p) => UnityAgentTools.AddMethodToScript(p["script_name"], p["method_code"]));
            
            RegisterTool("add_field_to_script",
                "Add a new field/property to an existing script (inserted after class opening brace)",
                new string[] { "script_name", "field_code" },
                (p) => UnityAgentTools.AddFieldToScript(p["script_name"], p["field_code"]));
            
            RegisterTool("delete_script",
                "Delete a script file from the project",
                new string[] { "script_name" },
                (p) => UnityAgentTools.DeleteScript(p["script_name"]));
            
            RegisterTool("find_in_script",
                "Search for text in a script and show all occurrences with line numbers",
                new string[] { "script_name", "search_text" },
                (p) => UnityAgentTools.FindInScript(p["script_name"], p["search_text"]));
            
            RegisterTool("replace_in_script",
                "Find and replace all occurrences of text in a script",
                new string[] { "script_name", "find_text", "replace_text" },
                (p) => UnityAgentTools.ReplaceInScript(p["script_name"], p["find_text"], p["replace_text"]));
            
            RegisterTool("validate_script",
                "Validate script for basic syntax errors (brace matching, class name, etc.)",
                new string[] { "script_name" },
                (p) => UnityAgentTools.ValidateScript(p["script_name"]));
            
            RegisterTool("create_from_template",
                "Create a script from a template (singleton, statemachine, objectpool, scriptableobject)",
                new string[] { "script_name", "template_type", "gameobject_name" },
                (p) => UnityAgentTools.CreateFromTemplate(
                    p["script_name"],
                    p["template_type"],
                    p.ContainsKey("gameobject_name") ? p["gameobject_name"] : null));
            
            RegisterTool("add_comments_to_script",
                "Add header comments/documentation to a script",
                new string[] { "script_name", "comments" },
                (p) => UnityAgentTools.AddCommentsToScript(p["script_name"], p["comments"]));
            
            RegisterTool("create_multiple_scripts",
                "Create multiple scripts at once (comma/semicolon/newline separated names)",
                new string[] { "script_names", "base_namespace" },
                (p) => UnityAgentTools.CreateMultipleScripts(
                    p["script_names"],
                    p.ContainsKey("base_namespace") ? p["base_namespace"] : null));
            
            RegisterTool("add_namespace_to_script",
                "Add a namespace wrapper to an existing script",
                new string[] { "script_name", "namespace_name" },
                (p) => UnityAgentTools.AddNamespaceToScript(p["script_name"], p["namespace_name"]));
            
            // File reading tools
            RegisterTool("read_script",
                "Read the content of a C# script file by name (e.g., 'PlayerController' or 'PlayerController.cs')",
                new string[] { "script_name" },
                (p) => UnityAgentTools.ReadScript(p["script_name"]));
            
            RegisterTool("read_file",
                "Read any file content from Assets folder (supports .txt, .json, .xml, .md, etc.)",
                new string[] { "file_path" },
                (p) => UnityAgentTools.ReadFile(p["file_path"]));
            
            // Console reading tools
            RegisterTool("read_console",
                "Read recent Unity Console messages (errors, warnings, logs). CRITICAL: Use this after script creation to check for compilation errors!",
                new string[] { "count", "filter_type" },
                (p) => UnityAgentTools.ReadConsole(
                    p.ContainsKey("count") ? int.Parse(p["count"]) : 10,
                    p.ContainsKey("filter_type") ? p["filter_type"] : "all"));
            
            RegisterTool("get_compilation_errors",
                "Check if scripts compiled successfully and get compilation errors if any. Use this immediately after creating/modifying scripts!",
                new string[] { },
                (p) => UnityAgentTools.GetCompilationErrors());
            
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
                    p.ContainsKey("intensity") ? float.Parse(p["intensity"], CultureInfo.InvariantCulture) : 1.0f));
            
            RegisterTool("create_camera",
                "Create a camera GameObject with optional field of view",
                new string[] { "name", "field_of_view" },
                (p) => UnityAgentTools.CreateCamera(
                    p["name"],
                    p.ContainsKey("field_of_view") ? float.Parse(p["field_of_view"], CultureInfo.InvariantCulture) : 60f));
            
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
            
            // ===== NEW ADVANCED COMPONENT OPERATIONS =====
            
            RegisterTool("remove_component",
                "Remove a component from a GameObject",
                new string[] { "gameobject_name", "component_type" },
                (p) => UnityAgentTools.RemoveComponent(p["gameobject_name"], p["component_type"]));
            
            RegisterTool("enable_component",
                "Enable or disable a component (set enabled property)",
                new string[] { "gameobject_name", "component_type", "enabled" },
                (p) => UnityAgentTools.EnableComponent(p["gameobject_name"], p["component_type"], p["enabled"].ToLower() == "true"));
            
            RegisterTool("copy_component",
                "Copy a component from one GameObject to another",
                new string[] { "source_gameobject", "target_gameobject", "component_type" },
                (p) => UnityAgentTools.CopyComponent(p["source_gameobject"], p["target_gameobject"], p["component_type"]));
            
            RegisterTool("reset_component",
                "Reset a component to its default values",
                new string[] { "gameobject_name", "component_type" },
                (p) => UnityAgentTools.ResetComponent(p["gameobject_name"], p["component_type"]));
            
            RegisterTool("set_multiple_properties",
                "Set multiple component properties at once (batch operation). Format: 'property1:value1,property2:value2'",
                new string[] { "gameobject_name", "component_type", "properties" },
                (p) => UnityAgentTools.SetMultipleProperties(p["gameobject_name"], p["component_type"], p["properties"]));
            
            // ===== NEW ADVANCED SCRIPT OPERATIONS =====
            
            RegisterTool("modify_method",
                "Modify an existing method's body in a script",
                new string[] { "script_name", "method_name", "new_method_body" },
                (p) => UnityAgentTools.ModifyMethod(p["script_name"], p["method_name"], p["new_method_body"]));
            
            RegisterTool("delete_method",
                "Delete a method from a script",
                new string[] { "script_name", "method_name" },
                (p) => UnityAgentTools.DeleteMethod(p["script_name"], p["method_name"]));
            
            RegisterTool("rename_method",
                "Rename a method in a script (including all calls)",
                new string[] { "script_name", "old_method_name", "new_method_name" },
                (p) => UnityAgentTools.RenameMethod(p["script_name"], p["old_method_name"], p["new_method_name"]));
            
            RegisterTool("create_property",
                "Create a property with get/set in a script",
                new string[] { "script_name", "property_type", "property_name", "get_body", "set_body" },
                (p) => UnityAgentTools.CreateProperty(
                    p["script_name"],
                    p["property_type"],
                    p["property_name"],
                    p.ContainsKey("get_body") ? p["get_body"] : null,
                    p.ContainsKey("set_body") ? p["set_body"] : null));
            
            RegisterTool("add_interface",
                "Add interface implementation to a class",
                new string[] { "script_name", "interface_name" },
                (p) => UnityAgentTools.AddInterface(p["script_name"], p["interface_name"]));
            
            RegisterTool("add_using_statement",
                "Add a using statement to a script",
                new string[] { "script_name", "namespace" },
                (p) => UnityAgentTools.AddUsingStatement(p["script_name"], p["namespace"]));
            
            RegisterTool("remove_unused_using",
                "Remove unused using statements from a script",
                new string[] { "script_name" },
                (p) => UnityAgentTools.RemoveUnusedUsing(p["script_name"]));
            
            RegisterTool("format_code",
                "Format code (indentation and spacing)",
                new string[] { "script_name" },
                (p) => UnityAgentTools.FormatCode(p["script_name"]));
            
            // ===== ADVANCED PROJECT INSPECTION =====
            
            RegisterTool("search_assets",
                "Advanced asset search with filters (type, folder)",
                new string[] { "search_query", "asset_type", "folder" },
                (p) => UnityAgentTools.SearchAssets(
                    p["search_query"],
                    p.ContainsKey("asset_type") ? p["asset_type"] : "",
                    p.ContainsKey("folder") ? p["folder"] : "Assets"));
            
            RegisterTool("find_asset_references",
                "Find all assets that reference a specific asset",
                new string[] { "asset_path" },
                (p) => UnityAgentTools.FindAssetReferences(p["asset_path"]));
            
            RegisterTool("analyze_asset_dependencies",
                "Analyze what an asset depends on",
                new string[] { "asset_path", "recursive" },
                (p) => UnityAgentTools.AnalyzeAssetDependencies(
                    p["asset_path"],
                    p.ContainsKey("recursive") && p["recursive"].ToLower() == "true"));
            
            RegisterTool("get_project_structure",
                "Get detailed project folder structure",
                new string[] { "root_folder", "max_depth" },
                (p) => UnityAgentTools.GetProjectStructure(
                    p.ContainsKey("root_folder") ? p["root_folder"] : "Assets",
                    p.ContainsKey("max_depth") ? int.Parse(p["max_depth"]) : 3));
            
            RegisterTool("find_unused_assets",
                "Find assets not referenced by any scene in build",
                new string[] { "folder" },
                (p) => UnityAgentTools.FindUnusedAssets(
                    p.ContainsKey("folder") ? p["folder"] : "Assets"));
            
            RegisterTool("import_asset",
                "Import asset from file system to Unity project",
                new string[] { "source_path", "target_path" },
                (p) => UnityAgentTools.ImportAsset(p["source_path"], p["target_path"]));
            
            RegisterTool("organize_assets",
                "Organize assets into folders by type",
                new string[] { "source_folder", "target_root_folder" },
                (p) => UnityAgentTools.OrganizeAssets(p["source_folder"], p["target_root_folder"]));
            
            // ===== CODE ANALYSIS =====
            
            RegisterTool("calculate_complexity",
                "Calculate cyclomatic complexity of a script",
                new string[] { "script_name" },
                (p) => UnityAgentTools.CalculateComplexity(p["script_name"]));
            
            RegisterTool("detect_code_smells",
                "Detect code smells and quality issues in a script",
                new string[] { "script_name" },
                (p) => UnityAgentTools.DetectCodeSmells(p["script_name"]));
            
            RegisterTool("analyze_script_dependencies",
                "Analyze script dependencies and using statements",
                new string[] { "script_name" },
                (p) => UnityAgentTools.AnalyzeScriptDependencies(p["script_name"]));
            
            RegisterTool("generate_quality_report",
                "Generate code quality report for entire project",
                new string[] { },
                (p) => UnityAgentTools.GenerateQualityReport());
            
            // ===== ADVANCED SCENE MANAGEMENT =====
            
            RegisterTool("load_scene_additive",
                "Load scene additively (multi-scene editing)",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.LoadSceneAdditive(p["scene_name"]));
            
            RegisterTool("unload_scene",
                "Unload an additively loaded scene",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.UnloadScene(p["scene_name"]));
            
            RegisterTool("get_loaded_scenes",
                "Get list of currently loaded scenes",
                new string[] { },
                (p) => UnityAgentTools.GetLoadedScenes());
            
            RegisterTool("set_active_scene",
                "Set active scene (for multi-scene editing)",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.SetActiveScene(p["scene_name"]));
            
            RegisterTool("create_scene_from_template",
                "Create scene from template (basic, 3d, 2d, ui)",
                new string[] { "scene_name", "template_type" },
                (p) => UnityAgentTools.CreateSceneFromTemplate(p["scene_name"], p["template_type"]));
            
            RegisterTool("get_build_settings",
                "Get build settings and scenes in build",
                new string[] { },
                (p) => UnityAgentTools.GetBuildSettings());
            
            RegisterTool("add_scene_to_build",
                "Add scene to build settings",
                new string[] { "scene_name", "enabled" },
                (p) => UnityAgentTools.AddSceneToBuild(
                    p["scene_name"],
                    !p.ContainsKey("enabled") || p["enabled"].ToLower() == "true"));
            
            RegisterTool("remove_scene_from_build",
                "Remove scene from build settings",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.RemoveSceneFromBuild(p["scene_name"]));
            
            RegisterTool("compare_scenes",
                "Compare two scenes and show differences",
                new string[] { "scene_name1", "scene_name2" },
                (p) => UnityAgentTools.CompareScenes(p["scene_name1"], p["scene_name2"]));
            
            RegisterTool("merge_scenes",
                "Merge one scene into another",
                new string[] { "source_scene", "target_scene" },
                (p) => UnityAgentTools.MergeScenes(p["source_scene"], p["target_scene"]));
            
            RegisterTool("get_scene_stats",
                "Get detailed statistics about a scene",
                new string[] { "scene_name" },
                (p) => UnityAgentTools.GetSceneStats(
                    p.ContainsKey("scene_name") ? p["scene_name"] : ""));
            
            // ===== RAG + SEMANTIC SEARCH =====
            
            RegisterTool("index_project",
                "Index entire project for semantic search (must do once before semantic search)",
                new string[] { "force_reindex" },
                (p) => UnityAgentTools.IndexProject(
                    p.ContainsKey("force_reindex") ? p["force_reindex"] : "false"));
            
            RegisterTool("semantic_search",
                "Search code by meaning, not just keywords (requires indexed project)",
                new string[] { "query", "top_k" },
                (p) => UnityAgentTools.SearchSemantic(
                    p["query"],
                    p.ContainsKey("top_k") ? p["top_k"] : "5"));
            
            RegisterTool("find_similar_code",
                "Find code similar to given snippet",
                new string[] { "code_snippet", "top_k" },
                (p) => UnityAgentTools.FindSimilar(
                    p["code_snippet"],
                    p.ContainsKey("top_k") ? p["top_k"] : "5"));
            
            RegisterTool("get_rag_stats",
                "Get RAG/Vector Database statistics",
                new string[] { },
                (p) => UnityAgentTools.GetRAGStats());
            
            // ===== ROSLYN SEMANTIC ANALYSIS =====
            
            RegisterTool("get_call_graph",
                "Analyze who calls a method (call graph)",
                new string[] { "script_name", "method_name" },
                (p) => UnityAgentTools.GetMethodCallGraph(p["script_name"], p["method_name"]));
            
            RegisterTool("find_symbol_usages",
                "Find all usages of a variable/field/property",
                new string[] { "script_name", "symbol_name" },
                (p) => UnityAgentTools.FindSymbolUsages(p["script_name"], p["symbol_name"]));
            
            RegisterTool("analyze_data_flow",
                "Analyze how data flows through code (data flow analysis)",
                new string[] { "script_name", "variable_name" },
                (p) => UnityAgentTools.AnalyzeVariableDataFlow(p["script_name"], p["variable_name"]));
            
            RegisterTool("get_all_symbols",
                "Get all methods/fields/properties in a script (symbol table)",
                new string[] { "script_name" },
                (p) => UnityAgentTools.GetScriptSymbols(p["script_name"]));
            
            // ===== REACT (REASONING + ACTING) =====
            
            RegisterTool("plan_with_react",
                "Plan task execution using ReAct (shows steps without executing)",
                new string[] { "task" },
                (p) => UnityAgentTools.PlanWithReAct(p["task"]));
            
            RegisterTool("estimate_task_complexity",
                "Estimate how complex a task is (1-10 scale)",
                new string[] { "task" },
                (p) => UnityAgentTools.EstimateTaskComplexity(p["task"]));
            
            // ===== SELF-CORRECTION =====
            
            RegisterTool("analyze_error",
                "Analyze error message and get root cause + fixes",
                new string[] { "error_message" },
                (p) => UnityAgentTools.AnalyzeError(p["error_message"]));
            
            RegisterTool("suggest_fixes",
                "Get suggested fixes for an error",
                new string[] { "error_message" },
                (p) => UnityAgentTools.SuggestFixes(p["error_message"]));
            
            RegisterTool("get_self_correction_stats",
                "Get self-correction engine statistics",
                new string[] { },
                (p) => UnityAgentTools.GetSelfCorrectionStats());
            
            // ===== LONG-TERM MEMORY =====
            
            RegisterTool("store_memory",
                "Store information in long-term memory",
                new string[] { "type", "content", "importance" },
                (p) => UnityAgentTools.StoreMemory(
                    p["type"],
                    p["content"],
                    p.ContainsKey("importance") ? p["importance"] : "0.5"));
            
            RegisterTool("recall_memories",
                "Recall memories by type (Episodic, Semantic, UserPreference, ProjectContext, Success, Failure, Insight, Tool)",
                new string[] { "type", "limit" },
                (p) => UnityAgentTools.RecallMemories(
                    p["type"],
                    p.ContainsKey("limit") ? p["limit"] : "10"));
            
            RegisterTool("search_memories",
                "Search memories by content",
                new string[] { "query", "limit" },
                (p) => UnityAgentTools.SearchMemories(
                    p["query"],
                    p.ContainsKey("limit") ? p["limit"] : "10"));
            
            RegisterTool("get_memory_stats",
                "Get memory system statistics",
                new string[] { },
                (p) => UnityAgentTools.GetMemoryStats());
            
            RegisterTool("get_important_memories",
                "Get most important memories",
                new string[] { "limit" },
                (p) => UnityAgentTools.GetImportantMemories(
                    p.ContainsKey("limit") ? p["limit"] : "10"));
            
            RegisterTool("consolidate_memories",
                "Consolidate duplicate/similar memories",
                new string[] { },
                (p) => UnityAgentTools.ConsolidateMemories());
            
            RegisterTool("analyze_project",
                "Analyze project structure and patterns",
                new string[] { },
                (p) => UnityAgentTools.AnalyzeProject());
            
            RegisterTool("get_project_context",
                "Get learned project context",
                new string[] { },
                (p) => UnityAgentTools.GetProjectContext());
            
            RegisterTool("get_coding_style",
                "Get learned user coding style preferences",
                new string[] { },
                (p) => UnityAgentTools.GetCodingStyle());
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
            return ProcessToolCallsWithProgress(response, null);
        }
        
        /// <summary>
        /// Process tool calls with progress callback for live UI updates
        /// NEW: Compact format + detailed view support + guard rails
        /// </summary>
        public string ProcessToolCallsWithProgress(string response, Action<string> progressCallback, string detailLevel = "Normal")
        {
            var result = new StringBuilder();
            var detailedLog = new StringBuilder(); // For expandable details
            
            // Check if response contains tool calls
            bool hasToolCalls = response.Contains("[TOOL:");
            
            if (!hasToolCalls)
            {
                // No tool calls, just return the response
                return response;
            }
            
            // Only show full AI response in Detailed mode
            if (detailLevel == "Detailed")
            {
                detailedLog.AppendLine("## 📋 Full AI Response:");
                detailedLog.AppendLine(response);
                detailedLog.AppendLine();
            }
            
            // Find all tool calls in format [TOOL:name]...[/TOOL]
            int startIndex = 0;
            int toolCount = 0;
            var toolExecutionTimes = new System.Collections.Generic.List<double>();
            
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
                
                // Live progress update - ULTRA SHORT: just icon + name
                progressCallback?.Invoke($"⚡ {toolName}()");
                
                // Execute tool and measure time (thread-safe)
                var stopwatch = Stopwatch.StartNew();
                string toolResult = ExecuteToolWithValidation(toolName, parameters);
                stopwatch.Stop();
                var elapsed = stopwatch.Elapsed.TotalSeconds;
                toolExecutionTimes.Add(elapsed);
                
                // 🤖 INTERCEPT RESULT: Auto-analyze and execute follow-up actions
                var paramsDict = parameters.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
                var interceptionResult = interceptor.InterceptResult(toolName, toolResult, paramsDict);
                
                // Execute auto-actions (e.g., auto-check compilation after script creation)
                foreach (var autoAction in interceptionResult.AutoExecuteActions)
                {
                    if (autoAction.Priority == ToolActionPriority.Critical)
                    {
                        toolCount++;
                        progressCallback?.Invoke($"🤖 {autoAction.ToolName}()");
                        
                        var autoStopwatch = Stopwatch.StartNew();
                        var autoParams = autoAction.Parameters.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                        string autoResult = ExecuteToolWithValidation(autoAction.ToolName, autoParams);
                        autoStopwatch.Stop();
                        var autoElapsed = autoStopwatch.Elapsed.TotalSeconds;
                        toolExecutionTimes.Add(autoElapsed);
                        
                        // Append auto-check result to detailed log
                        detailedLog.AppendLine($"**{toolCount}.** `{autoAction.ToolName}` 🤖 {autoAction.Reason} ({autoElapsed:F3}s)");
                        detailedLog.AppendLine(autoResult);
                        detailedLog.AppendLine();
                    }
                }
                
                // Use enriched result instead of raw result
                toolResult = interceptor.FormatEnrichedResult(interceptionResult);
                
                // Track context from tool execution
                UpdateContext(toolName, parameters, toolResult);
                
                // Update compact view with result icon
                string icon = toolResult.Contains("✅") ? "✅" : 
                              toolResult.Contains("❌") ? "❌" : 
                              toolResult.Contains("⚠️") ? "⚠️" : "✅";
                
                string compactResult = GetCompactResult(toolResult);
                var paramSummary = GetParameterSummary(parameters, toolName);
                
                // Progress callback - ULTRA SHORT completion message
                progressCallback?.Invoke($"{icon} {toolName}()");
                
                // DETAILED LOG: Format based on detail level (for final report)
                if (detailLevel == "Compact")
                {
                    // COMPACT: Just show tool name + compact result
                    detailedLog.AppendLine($"**{toolCount}.** `{toolName}` {icon} {compactResult} ({elapsed:F3}s)");
                }
                else if (detailLevel == "Normal")
                {
                    // NORMAL: Show params summary + compact result
                    detailedLog.AppendLine($"### 🔧 Tool {toolCount}: {toolName}");
                    detailedLog.AppendLine($"**Parameters:** {paramSummary}");
                    detailedLog.AppendLine($"**Execution Time:** {elapsed:F3}s");
                    detailedLog.AppendLine($"**Result:** {compactResult}");
                    detailedLog.AppendLine();
                }
                else // Detailed
                {
                    // DETAILED: Full everything
                    detailedLog.AppendLine($"### 🔧 Tool {toolCount}: {toolName}");
                    detailedLog.AppendLine($"**Parameters:**");
                    foreach (var param in parameters)
                    {
                        string valuePreview = param.Value.Length > 150 
                            ? param.Value.Substring(0, 150) + "..." 
                            : param.Value;
                        detailedLog.AppendLine($"- `{param.Key}`: {valuePreview}");
                    }
                    detailedLog.AppendLine();
                    detailedLog.AppendLine($"**Execution Time:** {elapsed:F3}s");
                    detailedLog.AppendLine($"**Result:**");
                    detailedLog.AppendLine(toolResult);
                    detailedLog.AppendLine();
                    detailedLog.AppendLine("---");
                    detailedLog.AppendLine();
                }
                
                startIndex = toolEnd + 7;
            }
            
            // Final summary - COMPACT or with details
            double totalTime = toolExecutionTimes.Count > 0 ? toolExecutionTimes.Sum() : 0;
            
            if (detailLevel == "Compact")
            {
                // COMPACT: Show count + brief tool log for ReAct loop
                result.AppendLine();
                result.AppendLine($"✅ Completed {toolCount} tool(s) in {totalTime:F1}s");
                result.AppendLine();
                result.AppendLine(detailedLog.ToString()); // Include brief tool results
                
                return result.ToString();
            }
            else
            {
                // Show detailed log in collapsible section
                result.AppendLine();
                result.AppendLine($"✅ **Completed {toolCount} tool(s)** in {totalTime:F2}s");
                result.AppendLine();
                result.AppendLine($"<details><summary>📊 Show Detailed Execution Log</summary>");
                result.AppendLine();
                result.AppendLine(detailedLog.ToString());
                result.AppendLine("</details>");
                
                return result.ToString();
            }
        }
        
        /// <summary>
        /// Get compact parameter summary for display
        /// </summary>
        private string GetParameterSummary(Dictionary<string, string> parameters, string toolName)
        {
            if (parameters.Count == 0) return "()";
            
            // Special handling for common tools
            if (toolName == "set_position" || toolName == "set_rotation" || toolName == "set_scale")
            {
                string obj = parameters.ContainsKey("gameobject_name") ? parameters["gameobject_name"] : "?";
                string x = parameters.ContainsKey("x") ? parameters["x"] : "0";
                string y = parameters.ContainsKey("y") ? parameters["y"] : "0";
                string z = parameters.ContainsKey("z") ? parameters["z"] : "0";
                return $"{obj} → ({x}, {y}, {z})";
            }
            else if (toolName == "create_gameobject" || toolName == "create_primitive")
            {
                string name = parameters.ContainsKey("name") ? parameters["name"] : 
                              parameters.ContainsKey("primitive_type") ? parameters["primitive_type"] : "?";
                return name;
            }
            else if (toolName == "create_and_attach_script")
            {
                string obj = parameters.ContainsKey("gameobject_name") ? parameters["gameobject_name"] : "?";
                string script = parameters.ContainsKey("script_name") ? parameters["script_name"] : "?";
                return $"{obj} → {script}";
            }
            else if (toolName == "add_component" || toolName == "set_component_property")
            {
                string obj = parameters.ContainsKey("gameobject_name") ? parameters["gameobject_name"] : "?";
                string comp = parameters.ContainsKey("component_type") ? parameters["component_type"] : "?";
                return $"{obj}.{comp}";
            }
            
            // Default: first 2 parameters
            int count = 0;
            var summary = new StringBuilder();
            foreach (var kvp in parameters)
            {
                if (count >= 2) break;
                if (kvp.Key == "script_content") continue; // Skip large content
                
                string value = kvp.Value.Length > 20 ? kvp.Value.Substring(0, 20) + "..." : kvp.Value;
                if (count > 0) summary.Append(", ");
                summary.Append($"{value}");
                count++;
            }
            
            return summary.ToString();
        }
        
        /// <summary>
        /// Get compact result summary
        /// </summary>
        private string GetCompactResult(string fullResult)
        {
            // Special compact summaries for verbose tools
            if (fullResult.Contains("# Scene:"))
            {
                // Extract just the scene name and object count
                var match = System.Text.RegularExpressions.Regex.Match(fullResult, @"# Scene: (\w+).*Root GameObjects: (\d+)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (match.Success)
                    return $"Scene '{match.Groups[1].Value}' ({match.Groups[2].Value} objects)";
            }
            
            if (fullResult.Contains("# Project Statistics"))
            {
                // Extract key stats
                var scripts = System.Text.RegularExpressions.Regex.Match(fullResult, @"Scripts: (\d+)");
                var scenes = System.Text.RegularExpressions.Regex.Match(fullResult, @"Scenes: (\d+)");
                if (scripts.Success && scenes.Success)
                    return $"{scripts.Groups[1].Value} scripts, {scenes.Groups[1].Value} scenes";
            }
            
            if (fullResult.Contains("# GameObject:"))
            {
                // Extract object name and component count
                var match = System.Text.RegularExpressions.Regex.Match(fullResult, @"# GameObject: (\w+).*Components \((\d+)\)", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (match.Success)
                    return $"'{match.Groups[1].Value}' ({match.Groups[2].Value} components)";
            }
            
            // CRITICAL: Extract GameObject names from find_gameobjects results
            if (fullResult.Contains("🔍 Found") && fullResult.Contains("GameObject(s)"))
            {
                var lines = fullResult.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                var objectNames = new System.Collections.Generic.List<string>();
                
                foreach (var line in lines)
                {
                    // Match lines like "- **ObjectName**"
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"-\s*\*\*(\w+)\*\*");
                    if (match.Success)
                    {
                        objectNames.Add(match.Groups[1].Value);
                    }
                }
                
                if (objectNames.Count > 0)
                {
                    string names = objectNames.Count <= 5 ? 
                        string.Join(", ", objectNames) : 
                        string.Join(", ", objectNames.Take(5)) + $" (+{objectNames.Count - 5} more)";
                    return $"🔍 Found {objectNames.Count}: {names}";
                }
            }
            
            // Default: Extract first meaningful line
            var lines2 = fullResult.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines2.Length > 0)
            {
                string firstLine = lines2[0].Trim();
                if (firstLine.Length > 60)
                    return firstLine.Substring(0, 60) + "...";
                return firstLine;
            }
            return "Done";
        }
        
        /// <summary>
        /// Execute tool with validation guard rails
        /// </summary>
        private string ExecuteToolWithValidation(string toolName, Dictionary<string, string> parameters)
        {
            // GUARD RAIL: Validate numeric parameters before execution
            if (toolName == "set_position")
            {
                if (parameters.ContainsKey("y"))
                {
                    float y = float.Parse(parameters["y"], CultureInfo.InvariantCulture);
                    
                    // Check for suspicious values (likely typos)
                    if (y > 100 || y < -100)
                    {
                        Debug.LogWarning($"[Guard Rail] Suspicious Y position: {y}. Did you mean {y/10}?");
                        return $"⚠️ Validation Warning: Y position {y} seems unusual. Typical range: [-10, 10]. Please verify.";
                    }
                }
            }
            
            if (toolName == "set_scale")
            {
                float x = parameters.ContainsKey("x") ? float.Parse(parameters["x"], CultureInfo.InvariantCulture) : 1;
                float y = parameters.ContainsKey("y") ? float.Parse(parameters["y"], CultureInfo.InvariantCulture) : 1;
                float z = parameters.ContainsKey("z") ? float.Parse(parameters["z"], CultureInfo.InvariantCulture) : 1;
                
                if (x > 100 || y > 100 || z > 100 || x < 0.01f || y < 0.01f || z < 0.01f)
                {
                    Debug.LogWarning($"[Guard Rail] Suspicious scale: ({x}, {y}, {z})");
                    return $"⚠️ Validation Warning: Scale ({x}, {y}, {z}) seems unusual. Typical range: [0.1, 10]";
                }
            }
            
            // Execute the tool normally
            string result = ExecuteTool(toolName, parameters);
            
            // POST-VALIDATION: Verify critical operations
            if (toolName == "set_position" && result.Contains("✅"))
            {
                // Verify the GameObject is actually where we set it
                string goName = parameters.ContainsKey("gameobject_name") ? parameters["gameobject_name"] : null;
                if (!string.IsNullOrEmpty(goName))
                {
                    var verifyResult = UnityAgentTools.GetGameObjectInfo(goName);
                    if (verifyResult.Contains("Position:"))
                    {
                        Debug.Log($"[Validation] Verified position for {goName}");
                    }
                }
            }
            
            return result;
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

