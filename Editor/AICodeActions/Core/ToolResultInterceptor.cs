using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AICodeActions.Core
{
    /// <summary>
    /// Intercepts tool execution results and automatically analyzes them for errors,
    /// suggesting follow-up actions to the AI agent.
    /// This eliminates the need for verbose system prompts.
    /// </summary>
    public class ToolResultInterceptor
    {
        private readonly AgentToolSystem toolSystem;
        
        // Pattern-based rules for automatic follow-ups
        private readonly Dictionary<string, Func<string, InterceptionResult>> rules;
        
        public ToolResultInterceptor(AgentToolSystem toolSystem)
        {
            this.toolSystem = toolSystem;
            rules = new Dictionary<string, Func<string, InterceptionResult>>
            {
                // Script creation/modification rules
                { "create_and_attach_script", AnalyzeScriptCreation },
                { "create_script", AnalyzeScriptCreation },
                { "modify_script", AnalyzeScriptModification },
                { "attach_script", AnalyzeScriptAttachment },
                
                // GameObject operation rules
                { "find_gameobjects", AnalyzeFindGameObjects },
                { "get_gameobject_info", AnalyzeGameObjectInfo },
                { "set_component_property", AnalyzeComponentProperty },
                { "add_component", AnalyzeAddComponent },
            };
        }
        
        /// <summary>
        /// Intercept and enrich a tool result with automatic observations and suggestions.
        /// </summary>
        public InterceptionResult InterceptResult(string toolName, string toolResult, Dictionary<string, object> parameters)
        {
            var result = new InterceptionResult
            {
                OriginalResult = toolResult,
                ToolName = toolName,
                Parameters = parameters
            };
            
            // Apply rule-based analysis
            if (rules.ContainsKey(toolName))
            {
                var ruleResult = rules[toolName](toolResult);
                result.Observations.AddRange(ruleResult.Observations);
                result.SuggestedActions.AddRange(ruleResult.SuggestedActions);
                result.AutoExecuteActions.AddRange(ruleResult.AutoExecuteActions);
            }
            
            // Generic error detection
            DetectGenericErrors(toolResult, result);
            
            return result;
        }
        
        #region Rule Implementations
        
        private InterceptionResult AnalyzeScriptCreation(string result)
        {
            var interception = new InterceptionResult();
            
            // Always check compilation after script creation
            if (result.Contains("created") || result.Contains("Created"))
            {
                interception.AutoExecuteActions.Add(new ToolAction
                {
                    ToolName = "get_compilation_errors",
                    Reason = "🔍 Auto-checking compilation after script creation",
                    Priority = ToolActionPriority.Critical
                });
                
                interception.Observations.Add("⚠️ Script created - compilation check is MANDATORY before proceeding");
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeScriptModification(string result)
        {
            var interception = new InterceptionResult();
            
            // Always verify compilation after modification
            if (result.Contains("modified") || result.Contains("updated") || result.Contains("Changed"))
            {
                interception.AutoExecuteActions.Add(new ToolAction
                {
                    ToolName = "get_compilation_errors",
                    Reason = "🔍 Auto-checking compilation after script modification",
                    Priority = ToolActionPriority.Critical
                });
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeScriptAttachment(string result)
        {
            var interception = new InterceptionResult();
            
            // Script attachment failed - check console immediately
            if (result.Contains("not found") || result.Contains("class not found") || 
                result.Contains("failed") || result.Contains("❌"))
            {
                interception.AutoExecuteActions.Add(new ToolAction
                {
                    ToolName = "read_console",
                    Parameters = new Dictionary<string, object> 
                    { 
                        { "count", 20 },
                        { "filterType", "error" }
                    },
                    Reason = "🚨 Script attachment failed - checking console for compilation errors",
                    Priority = ToolActionPriority.Critical
                });
                
                interception.Observations.Add("❌ Script attachment failed - likely compilation errors");
                interception.SuggestedActions.Add("Check console output above and fix compilation errors");
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeFindGameObjects(string result)
        {
            var interception = new InterceptionResult();
            
            // No GameObjects found
            if (result.Contains("No GameObjects found") || result.Contains("not found"))
            {
                interception.Observations.Add("ℹ️ No GameObjects found - scene might be empty or search term incorrect");
                interception.SuggestedActions.Add("Try get_scene_info to see all objects in scene");
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeGameObjectInfo(string result)
        {
            var interception = new InterceptionResult();
            
            // GameObject not found
            if (result.Contains("not found") || result.Contains("❌"))
            {
                interception.Observations.Add("⚠️ GameObject not found in scene");
                interception.SuggestedActions.Add("Use find_gameobjects or get_scene_info to discover actual GameObject names");
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeComponentProperty(string result)
        {
            var interception = new InterceptionResult();
            
            // Component not found
            if (result.Contains("Component") && result.Contains("not found"))
            {
                interception.Observations.Add("⚠️ Component not found - it may not be attached yet");
                interception.SuggestedActions.Add("Use get_gameobject_info to see which components are attached");
            }
            
            return interception;
        }
        
        private InterceptionResult AnalyzeAddComponent(string result)
        {
            var interception = new InterceptionResult();
            
            // Component added successfully - might need configuration
            if (result.Contains("Added") && result.Contains("✅"))
            {
                interception.Observations.Add("✅ Component added - you may need to configure its properties");
            }
            
            return interception;
        }
        
        #endregion
        
        #region Generic Error Detection
        
        private void DetectGenericErrors(string result, InterceptionResult interception)
        {
            // Null reference patterns
            if (Regex.IsMatch(result, @"(NullReferenceException|null reference|object is null)", RegexOptions.IgnoreCase))
            {
                interception.Observations.Add("🐛 Null reference detected - check if objects/components are properly assigned");
            }
            
            // Compilation error patterns
            if (Regex.IsMatch(result, @"(CS\d{4}|compilation error|syntax error)", RegexOptions.IgnoreCase))
            {
                interception.Observations.Add("❌ Compilation error detected - must be fixed before proceeding");
                if (!interception.AutoExecuteActions.Exists(a => a.ToolName == "read_console"))
                {
                    interception.AutoExecuteActions.Add(new ToolAction
                    {
                        ToolName = "read_console",
                        Parameters = new Dictionary<string, object> { { "filterType", "error" } },
                        Reason = "🔍 Reading console for compilation error details",
                        Priority = ToolActionPriority.High
                    });
                }
            }
            
            // Missing dependency patterns
            if (Regex.IsMatch(result, @"(missing|requires|dependency|not installed)", RegexOptions.IgnoreCase))
            {
                interception.Observations.Add("📦 Missing dependency or requirement detected");
            }
            
            // Permission/access errors
            if (Regex.IsMatch(result, @"(permission|access denied|unauthorized)", RegexOptions.IgnoreCase))
            {
                interception.Observations.Add("🔒 Permission or access issue detected");
            }
        }
        
        #endregion
        
        /// <summary>
        /// Format the interception result as enriched output for the AI.
        /// </summary>
        public string FormatEnrichedResult(InterceptionResult interception)
        {
            var sb = new StringBuilder();
            
            // Original result
            sb.AppendLine(interception.OriginalResult);
            
            // Auto-executed actions (will be shown inline)
            if (interception.AutoExecuteActions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("🤖 **Auto-Checks Performed:**");
                // These will be executed and their results appended
            }
            
            // Observations
            if (interception.Observations.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("🔍 **Automatic Analysis:**");
                foreach (var obs in interception.Observations)
                {
                    sb.AppendLine($"   {obs}");
                }
            }
            
            // Suggested actions
            if (interception.SuggestedActions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("💡 **Suggested Next Steps:**");
                foreach (var action in interception.SuggestedActions)
                {
                    sb.AppendLine($"   • {action}");
                }
            }
            
            return sb.ToString();
        }
    }
    
    #region Data Structures
    
    public class InterceptionResult
    {
        public string OriginalResult { get; set; }
        public string ToolName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public List<string> Observations { get; set; } = new List<string>();
        public List<string> SuggestedActions { get; set; } = new List<string>();
        public List<ToolAction> AutoExecuteActions { get; set; } = new List<ToolAction>();
    }
    
    public class ToolAction
    {
        public string ToolName { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string Reason { get; set; }
        public ToolActionPriority Priority { get; set; }
    }
    
    public enum ToolActionPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    #endregion
}

