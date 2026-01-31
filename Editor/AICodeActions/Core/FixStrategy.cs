using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Automated fix strategies for common errors
    /// </summary>
    public static class FixStrategy
    {
        public class FixResult
        {
            public bool success;
            public string action;
            public string details;
            public string[] changesApplied;
            
            public FixResult()
            {
                changesApplied = new string[0];
            }
            
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine(success ? "✅ FIX APPLIED" : "❌ FIX FAILED");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Action: {action}");
                sb.AppendLine($"Details: {details}");
                
                if (changesApplied.Length > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Changes Applied:");
                    foreach (var change in changesApplied)
                    {
                        sb.AppendLine($"  • {change}");
                    }
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Apply automatic fix based on error analysis
        /// </summary>
        public static FixResult ApplyFix(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> operationContext)
        {
            return analysis.category switch
            {
                ErrorAnalyzer.ErrorCategory.CompilationError => FixCompilationError(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.MissingReference => FixMissingReference(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.GameObjectNotFound => FixGameObjectNotFound(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.ComponentNotFound => FixComponentNotFound(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.ScriptNotFound => FixScriptNotFound(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.NullReference => FixNullReference(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.TypeMismatch => FixTypeMismatch(analysis, operationContext),
                ErrorAnalyzer.ErrorCategory.InvalidParameter => FixInvalidParameter(analysis, operationContext),
                _ => new FixResult
                {
                    success = false,
                    action = "No automatic fix available",
                    details = $"Manual intervention required for {analysis.category}"
                }
            };
        }
        
        /// <summary>
        /// Fix compilation errors
        /// </summary>
        private static FixResult FixCompilationError(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Fix compilation error" };
            var changes = new List<string>();
            
            try
            {
                // Get script name from context
                if (!context.ContainsKey("script_name") && !analysis.context.ContainsKey("file"))
                {
                    result.details = "Cannot determine which script has the error";
                    return result;
                }
                
                string scriptName = context.ContainsKey("script_name") 
                    ? context["script_name"] 
                    : Path.GetFileNameWithoutExtension(analysis.context["file"]);
                
                // Find script
                var scriptPath = GetScriptPath(scriptName);
                if (string.IsNullOrEmpty(scriptPath))
                {
                    result.details = $"Script '{scriptName}' not found";
                    return result;
                }
                
                string content = File.ReadAllText(scriptPath);
                bool modified = false;
                
                // Fix: Missing using statements
                if (analysis.rootCause.Contains("using statement"))
                {
                    var commonUsings = new[] 
                    { 
                        "using UnityEngine;",
                        "using System.Collections;",
                        "using System.Collections.Generic;"
                    };
                    
                    foreach (var usingStatement in commonUsings)
                    {
                        if (!content.Contains(usingStatement))
                        {
                            content = usingStatement + "\n" + content;
                            changes.Add($"Added {usingStatement}");
                            modified = true;
                        }
                    }
                }
                
                // Fix: Missing semicolons (simple heuristic)
                if (analysis.rootCause.Contains("semicolon") && analysis.context.ContainsKey("line"))
                {
                    int lineNum = int.Parse(analysis.context["line"]);
                    var lines = content.Split('\n');
                    
                    if (lineNum > 0 && lineNum <= lines.Length)
                    {
                        string line = lines[lineNum - 1].TrimEnd();
                        
                        // Add semicolon if missing and line looks like statement
                        if (!line.EndsWith(";") && !line.EndsWith("{") && !line.EndsWith("}") && 
                            !line.TrimStart().StartsWith("//") && !string.IsNullOrWhiteSpace(line))
                        {
                            lines[lineNum - 1] = line + ";";
                            content = string.Join("\n", lines);
                            changes.Add($"Added semicolon at line {lineNum}");
                            modified = true;
                        }
                    }
                }
                
                // Save if modified
                if (modified)
                {
                    // Validate with Roslyn
                    if (RoslynScriptAnalyzer.TryValidate(content, out var report))
                    {
                        File.WriteAllText(scriptPath, content);
                        AssetDatabase.Refresh();
                        
                        result.success = true;
                        result.details = $"Applied {changes.Count} fix(es) to {scriptName}.cs";
                        result.changesApplied = changes.ToArray();
                    }
                    else
                    {
                        result.details = $"Fix validation failed: {report}";
                    }
                }
                else
                {
                    result.details = "No automatic fix available for this compilation error";
                }
                
                return result;
            }
            catch (Exception e)
            {
                result.details = $"Error applying fix: {e.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Fix missing reference errors
        /// </summary>
        private static FixResult FixMissingReference(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            // Delegate to specific handlers
            if (analysis.category == ErrorAnalyzer.ErrorCategory.GameObjectNotFound)
                return FixGameObjectNotFound(analysis, context);
            
            if (analysis.category == ErrorAnalyzer.ErrorCategory.ComponentNotFound)
                return FixComponentNotFound(analysis, context);
            
            return new FixResult
            {
                success = false,
                action = "Fix missing reference",
                details = "Reference type not recognized"
            };
        }
        
        /// <summary>
        /// Fix GameObject not found by creating it
        /// </summary>
        private static FixResult FixGameObjectNotFound(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Create missing GameObject" };
            
            try
            {
                string goName = analysis.context.ContainsKey("gameobject") 
                    ? analysis.context["gameobject"] 
                    : context.ContainsKey("gameobject_name") 
                        ? context["gameobject_name"] 
                        : "NewGameObject";
                
                // Check if already exists
                var existing = GameObject.Find(goName);
                if (existing != null)
                {
                    result.success = true;
                    result.details = $"GameObject '{goName}' already exists";
                    return result;
                }
                
                // Create it
                var go = new GameObject(goName);
                Undo.RegisterCreatedObjectUndo(go, $"Create {goName}");
                
                result.success = true;
                result.details = $"Created GameObject '{goName}'";
                result.changesApplied = new[] { $"Created GameObject: {goName}" };
                
                return result;
            }
            catch (Exception e)
            {
                result.details = $"Error creating GameObject: {e.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Fix component not found by adding it
        /// </summary>
        private static FixResult FixComponentNotFound(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Add missing component" };
            
            try
            {
                if (!context.ContainsKey("gameobject_name") || !context.ContainsKey("component_type"))
                {
                    result.details = "Missing GameObject or component type information";
                    return result;
                }
                
                string goName = context["gameobject_name"];
                string componentType = context["component_type"];
                
                var go = GameObject.Find(goName);
                if (go == null)
                {
                    result.details = $"GameObject '{goName}' not found";
                    return result;
                }
                
                // Try to add component
                var component = UnityAgentTools.AddComponent(goName, componentType);
                
                if (component.Contains("✅"))
                {
                    result.success = true;
                    result.details = $"Added {componentType} to {goName}";
                    result.changesApplied = new[] { $"Added component: {componentType}" };
                }
                else
                {
                    result.details = component;
                }
                
                return result;
            }
            catch (Exception e)
            {
                result.details = $"Error adding component: {e.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Fix script not found
        /// </summary>
        private static FixResult FixScriptNotFound(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Handle missing script" };
            
            // We can't auto-generate complex scripts, but we can suggest
            result.success = false;
            result.details = "Script must be created manually or regenerated";
            result.changesApplied = new[]
            {
                "Suggestion: Use create_and_attach_script tool",
                "Or manually create the script file"
            };
            
            return result;
        }
        
        /// <summary>
        /// Fix null reference by adding null checks
        /// </summary>
        private static FixResult FixNullReference(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Add null check" };
            
            // This would require code analysis and modification
            // For now, provide guidance
            result.success = false;
            result.details = "Null reference requires code review";
            result.changesApplied = new[]
            {
                "Add null check before access",
                "Initialize object before use",
                "Use safe navigation operator: obj?.Method()"
            };
            
            return result;
        }
        
        /// <summary>
        /// Fix type mismatch
        /// </summary>
        private static FixResult FixTypeMismatch(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Fix type mismatch" };
            
            result.success = false;
            result.details = "Type mismatch requires manual correction";
            result.changesApplied = new[]
            {
                "Check parameter types",
                "Add type conversion",
                "Use correct method signature"
            };
            
            return result;
        }
        
        /// <summary>
        /// Fix invalid parameter
        /// </summary>
        private static FixResult FixInvalidParameter(ErrorAnalyzer.ErrorAnalysis analysis, Dictionary<string, string> context)
        {
            var result = new FixResult { action = "Fix invalid parameter" };
            
            // Try to validate and correct parameters
            result.success = false;
            result.details = "Parameter validation required";
            result.changesApplied = new[]
            {
                "Check parameter format",
                "Validate parameter range",
                "Use correct parameter type"
            };
            
            return result;
        }
        
        /// <summary>
        /// Get script path by name
        /// </summary>
        private static string GetScriptPath(string scriptName)
        {
            var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
            
            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                
                if (script != null && script.name == scriptName)
                {
                    return path;
                }
            }
            
            return null;
        }
    }
}

