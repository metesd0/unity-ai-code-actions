using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Learn user preferences from interactions
    /// Coding style, naming conventions, project patterns
    /// </summary>
    public class UserPreferenceLearning
    {
        private LongTermMemoryManager memory;
        
        // Tracked patterns
        private Dictionary<string, int> namingPatterns = new Dictionary<string, int>();
        private Dictionary<string, int> codeStylePatterns = new Dictionary<string, int>();
        private Dictionary<string, int> componentUsage = new Dictionary<string, int>();
        
        public UserPreferenceLearning(LongTermMemoryManager memory)
        {
            this.memory = memory;
            LoadPatterns();
        }
        
        /// <summary>
        /// Learn from script creation
        /// </summary>
        public void LearnFromScript(string scriptName, string scriptContent)
        {
            try
            {
                // Naming convention
                DetectNamingConvention(scriptName);
                
                // Brace style
                DetectBraceStyle(scriptContent);
                
                // Indentation
                DetectIndentationStyle(scriptContent);
                
                // Common patterns
                DetectCommonPatterns(scriptContent);
                
                SavePatterns();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Learning] Error learning from script: {e.Message}");
            }
        }
        
        /// <summary>
        /// Learn from GameObject creation
        /// </summary>
        public void LearnFromGameObject(string gameObjectName, string[] components)
        {
            try
            {
                // Naming pattern
                DetectNamingConvention(gameObjectName);
                
                // Component usage
                foreach (var component in components)
                {
                    if (!componentUsage.ContainsKey(component))
                        componentUsage[component] = 0;
                    
                    componentUsage[component]++;
                }
                
                SavePatterns();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Learning] Error learning from GameObject: {e.Message}");
            }
        }
        
        /// <summary>
        /// Learn from user feedback (accept/reject)
        /// </summary>
        public void LearnFromFeedback(string operation, bool accepted, string reason = null)
        {
            try
            {
                if (accepted)
                {
                    memory.Store(MemoryType.Success, 
                        $"User accepted: {operation}", 
                        importance: 0.7f,
                        metadata: new Dictionary<string, string>
                        {
                            { "operation", operation },
                            { "outcome", "accepted" }
                        });
                }
                else
                {
                    memory.Store(MemoryType.Failure, 
                        $"User rejected: {operation}" + (reason != null ? $" - {reason}" : ""), 
                        importance: 0.8f,
                        metadata: new Dictionary<string, string>
                        {
                            { "operation", operation },
                            { "outcome", "rejected" },
                            { "reason", reason ?? "unknown" }
                        });
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Learning] Error learning from feedback: {e.Message}");
            }
        }
        
        /// <summary>
        /// Detect naming convention (PascalCase, camelCase, snake_case)
        /// </summary>
        private void DetectNamingConvention(string name)
        {
            if (IsPascalCase(name))
            {
                IncrementPattern("naming_pascalcase");
            }
            else if (IsCamelCase(name))
            {
                IncrementPattern("naming_camelcase");
            }
            else if (IsSnakeCase(name))
            {
                IncrementPattern("naming_snakecase");
            }
        }
        
        /// <summary>
        /// Detect brace style (same line vs new line)
        /// </summary>
        private void DetectBraceStyle(string code)
        {
            // Count braces on same line vs new line
            int sameLine = Regex.Matches(code, @"\)\s*\{").Count;
            int newLine = Regex.Matches(code, @"\)\s*\n\s*\{").Count;
            
            if (sameLine > newLine)
            {
                IncrementPattern("brace_sameline");
            }
            else if (newLine > sameLine)
            {
                IncrementPattern("brace_newline");
            }
        }
        
        /// <summary>
        /// Detect indentation style (tabs vs spaces, size)
        /// </summary>
        private void DetectIndentationStyle(string code)
        {
            var lines = code.Split('\n');
            int tabCount = 0;
            int spaceCount = 0;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("\t"))
                    tabCount++;
                else if (line.StartsWith("    "))
                    spaceCount++;
            }
            
            if (tabCount > spaceCount)
            {
                IncrementPattern("indent_tabs");
            }
            else if (spaceCount > tabCount)
            {
                IncrementPattern("indent_spaces");
            }
        }
        
        /// <summary>
        /// Detect common code patterns
        /// </summary>
        private void DetectCommonPatterns(string code)
        {
            // Verbose logging
            if (code.Contains("Debug.Log"))
            {
                IncrementPattern("uses_debug_log");
            }
            
            // Null checks
            if (code.Contains("!= null") || code.Contains("== null"))
            {
                IncrementPattern("uses_null_checks");
            }
            
            // Properties vs fields
            if (Regex.IsMatch(code, @"\{\s*get;\s*set;\s*\}"))
            {
                IncrementPattern("uses_properties");
            }
            
            // XML comments
            if (code.Contains("///"))
            {
                IncrementPattern("uses_xml_comments");
            }
            
            // Serialization
            if (code.Contains("[SerializeField]"))
            {
                IncrementPattern("uses_serializefield");
            }
        }
        
        /// <summary>
        /// Get preferred naming convention
        /// </summary>
        public string GetPreferredNaming()
        {
            int pascal = namingPatterns.ContainsKey("naming_pascalcase") ? namingPatterns["naming_pascalcase"] : 0;
            int camel = namingPatterns.ContainsKey("naming_camelcase") ? namingPatterns["naming_camelcase"] : 0;
            int snake = namingPatterns.ContainsKey("naming_snakecase") ? namingPatterns["naming_snakecase"] : 0;
            
            if (pascal > camel && pascal > snake)
                return "PascalCase";
            else if (camel > snake)
                return "camelCase";
            else if (snake > 0)
                return "snake_case";
            else
                return "PascalCase"; // Default
        }
        
        /// <summary>
        /// Get preferred brace style
        /// </summary>
        public string GetPreferredBraceStyle()
        {
            int sameLine = codeStylePatterns.ContainsKey("brace_sameline") ? codeStylePatterns["brace_sameline"] : 0;
            int newLine = codeStylePatterns.ContainsKey("brace_newline") ? codeStylePatterns["brace_newline"] : 0;
            
            return sameLine > newLine ? "same_line" : "new_line";
        }
        
        /// <summary>
        /// Get coding style summary
        /// </summary>
        public string GetCodingStyleSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("👤 USER CODING STYLE PROFILE");
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine($"Naming: {GetPreferredNaming()}");
            sb.AppendLine($"Braces: {GetPreferredBraceStyle()}");
            sb.AppendLine($"Indentation: {GetPreferredIndentation()}");
            sb.AppendLine();
            
            sb.AppendLine("Common Patterns:");
            var patterns = codeStylePatterns
                .OrderByDescending(kvp => kvp.Value)
                .Take(5);
            
            foreach (var pattern in patterns)
            {
                string desc = GetPatternDescription(pattern.Key);
                sb.AppendLine($"  • {desc} (used {pattern.Value}x)");
            }
            
            if (componentUsage.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Frequently Used Components:");
                var topComponents = componentUsage
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(5);
                
                foreach (var comp in topComponents)
                {
                    sb.AppendLine($"  • {comp.Key} ({comp.Value}x)");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Store learned preference as memory
        /// </summary>
        public void StorePreference(string key, string value, string description)
        {
            memory.Store(MemoryType.UserPreference,
                description,
                importance: 0.8f,
                metadata: new Dictionary<string, string>
                {
                    { "key", key },
                    { "value", value }
                });
        }
        
        /// <summary>
        /// Get all learned preferences
        /// </summary>
        public List<Memory> GetAllPreferences()
        {
            return memory.Recall(MemoryType.UserPreference, limit: 50);
        }
        
        // Helper methods
        
        private bool IsPascalCase(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   char.IsUpper(name[0]) && 
                   !name.Contains("_");
        }
        
        private bool IsCamelCase(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   char.IsLower(name[0]) && 
                   !name.Contains("_") &&
                   name.Any(char.IsUpper);
        }
        
        private bool IsSnakeCase(string name)
        {
            return !string.IsNullOrEmpty(name) && 
                   name.Contains("_") && 
                   name == name.ToLower();
        }
        
        private void IncrementPattern(string pattern)
        {
            if (!codeStylePatterns.ContainsKey(pattern))
                codeStylePatterns[pattern] = 0;
            
            codeStylePatterns[pattern]++;
        }
        
        private string GetPreferredIndentation()
        {
            int tabs = codeStylePatterns.ContainsKey("indent_tabs") ? codeStylePatterns["indent_tabs"] : 0;
            int spaces = codeStylePatterns.ContainsKey("indent_spaces") ? codeStylePatterns["indent_spaces"] : 0;
            
            return tabs > spaces ? "Tabs" : "4 Spaces";
        }
        
        private string GetPatternDescription(string pattern)
        {
            return pattern switch
            {
                "uses_debug_log" => "Uses Debug.Log",
                "uses_null_checks" => "Uses null checks",
                "uses_properties" => "Uses properties",
                "uses_xml_comments" => "Uses XML comments",
                "uses_serializefield" => "Uses [SerializeField]",
                "brace_sameline" => "Braces on same line",
                "brace_newline" => "Braces on new line",
                "indent_tabs" => "Tab indentation",
                "indent_spaces" => "Space indentation",
                _ => pattern
            };
        }
        
        private void SavePatterns()
        {
            // Save patterns to memory
            if (codeStylePatterns.Count > 0)
            {
                var summary = GetCodingStyleSummary();
                memory.Store(MemoryType.Insight,
                    $"Coding style profile updated: {GetPreferredNaming()}, {GetPreferredBraceStyle()}",
                    importance: 0.6f);
            }
        }
        
        private void LoadPatterns()
        {
            // Could load patterns from memory if needed
            // For now, patterns are rebuilt from observations
        }
    }
}

