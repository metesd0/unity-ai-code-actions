using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// File Operations: Reading scripts, files, and listing assets
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Read a C# script file content
        /// </summary>
        public static string ReadScript(string scriptName)
        {
            try
            {
                // Remove .cs extension if present
                if (scriptName.EndsWith(".cs"))
                    scriptName = scriptName.Substring(0, scriptName.Length - 3);
                
                // Find the script using AssetDatabase
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found in project";
                
                // Get the first match
                string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                
                if (script == null)
                    return $"❌ Script '{scriptName}' could not be loaded";
                
                string content = script.text;
                
                return $"📄 **{scriptName}.cs**\n```csharp\n{content}\n```";
            }
            catch (Exception e)
            {
                return $"❌ Error reading script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Read any file content from Assets
        /// </summary>
        public static string ReadFile(string filePath)
        {
            try
            {
                // Ensure path starts with Assets/
                if (!filePath.StartsWith("Assets/"))
                    filePath = "Assets/" + filePath;
                
                if (!System.IO.File.Exists(filePath))
                    return $"❌ File not found: {filePath}";
                
                string content = System.IO.File.ReadAllText(filePath);
                string extension = System.IO.Path.GetExtension(filePath).ToLower();
                
                string langTag = extension switch
                {
                    ".cs" => "csharp",
                    ".json" => "json",
                    ".xml" => "xml",
                    ".txt" => "text",
                    ".md" => "markdown",
                    _ => "text"
                };
                
                return $"📄 **{System.IO.Path.GetFileName(filePath)}**\n```{langTag}\n{content}\n```";
            }
            catch (Exception e)
            {
                return $"❌ Error reading file: {e.Message}";
            }
        }
        
        /// <summary>
        /// List all C# scripts in the project
        /// </summary>
        public static string ListScripts(string filter = "")
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                var scripts = new System.Collections.Generic.List<string>();
                
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    
                    if (script != null)
                    {
                        if (string.IsNullOrEmpty(filter) || 
                            script.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            scripts.Add($"{script.name}.cs ({path})");
                        }
                    }
                }
                
                if (scripts.Count == 0)
                {
                    return string.IsNullOrEmpty(filter) 
                        ? "❌ No scripts found in project" 
                        : $"❌ No scripts found matching '{filter}'";
                }
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"📝 Found {scripts.Count} script(s):");
                result.AppendLine();
                
                foreach (var script in scripts.Take(50))
                {
                    result.AppendLine($"- {script}");
                }
                
                if (scripts.Count > 50)
                {
                    result.AppendLine($"... and {scripts.Count - 50} more");
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error listing scripts: {e.Message}";
            }
        }
    }
}

