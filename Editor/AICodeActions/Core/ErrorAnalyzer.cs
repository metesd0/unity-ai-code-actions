using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Analyzes errors and determines root cause
    /// </summary>
    public static class ErrorAnalyzer
    {
        public enum ErrorCategory
        {
            CompilationError,
            RuntimeError,
            MissingReference,
            TypeMismatch,
            NullReference,
            ComponentNotFound,
            GameObjectNotFound,
            ScriptNotFound,
            InvalidParameter,
            PermissionDenied,
            ResourceNotFound,
            SyntaxError,
            LogicError,
            Unknown
        }
        
        public class ErrorAnalysis
        {
            public ErrorCategory category;
            public string originalError;
            public string rootCause;
            public string[] possibleFixes;
            public int confidence; // 1-10
            public Dictionary<string, string> context;
            
            public ErrorAnalysis()
            {
                context = new Dictionary<string, string>();
                possibleFixes = new string[0];
            }
            
            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"🔍 ERROR ANALYSIS");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Category: {category}");
                sb.AppendLine($"Confidence: {confidence}/10");
                sb.AppendLine();
                sb.AppendLine($"Root Cause: {rootCause}");
                sb.AppendLine();
                
                if (possibleFixes.Length > 0)
                {
                    sb.AppendLine("Possible Fixes:");
                    for (int i = 0; i < possibleFixes.Length; i++)
                    {
                        sb.AppendLine($"  {i + 1}. {possibleFixes[i]}");
                    }
                }
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Analyze error message and determine category
        /// </summary>
        public static ErrorAnalysis Analyze(string errorMessage)
        {
            var analysis = new ErrorAnalysis
            {
                originalError = errorMessage
            };
            
            string errorLower = errorMessage.ToLower();
            
            // === COMPILATION ERRORS ===
            if (ContainsAny(errorLower, "cs0", "error cs", "compilation failed", "syntax error"))
            {
                analysis.category = ErrorCategory.CompilationError;
                analysis = AnalyzeCompilationError(errorMessage, analysis);
            }
            // === MISSING REFERENCES ===
            else if (ContainsAny(errorLower, "not found", "does not exist", "cannot find"))
            {
                if (ContainsAny(errorLower, "gameobject", "game object"))
                {
                    analysis.category = ErrorCategory.GameObjectNotFound;
                    analysis.rootCause = "GameObject does not exist in scene";
                    analysis.possibleFixes = new[]
                    {
                        "Create the GameObject first",
                        "Check GameObject name (case-sensitive)",
                        "Ensure GameObject is in active scene"
                    };
                    analysis.confidence = 9;
                }
                else if (ContainsAny(errorLower, "component", "monobehaviour"))
                {
                    analysis.category = ErrorCategory.ComponentNotFound;
                    analysis.rootCause = "Component not attached to GameObject";
                    analysis.possibleFixes = new[]
                    {
                        "Add component to GameObject first",
                        "Check component type name",
                        "Verify component is enabled"
                    };
                    analysis.confidence = 9;
                }
                else if (ContainsAny(errorLower, "script", "type", "class"))
                {
                    analysis.category = ErrorCategory.ScriptNotFound;
                    analysis.rootCause = "Script file does not exist";
                    analysis.possibleFixes = new[]
                    {
                        "Create the script first",
                        "Check script name spelling",
                        "Refresh AssetDatabase"
                    };
                    analysis.confidence = 8;
                }
                else
                {
                    analysis.category = ErrorCategory.ResourceNotFound;
                    analysis.rootCause = "Resource or asset not found";
                    analysis.possibleFixes = new[]
                    {
                        "Verify resource path",
                        "Check if asset exists",
                        "Refresh Unity project"
                    };
                    analysis.confidence = 7;
                }
            }
            // === NULL REFERENCE ===
            else if (ContainsAny(errorLower, "null", "nullreferenceexception"))
            {
                analysis.category = ErrorCategory.NullReference;
                analysis.rootCause = "Attempting to access null object";
                analysis.possibleFixes = new[]
                {
                    "Check if object exists before using",
                    "Initialize object before access",
                    "Add null check: if (obj != null)"
                };
                analysis.confidence = 8;
            }
            // === TYPE MISMATCH ===
            else if (ContainsAny(errorLower, "type", "cannot convert", "invalid cast"))
            {
                analysis.category = ErrorCategory.TypeMismatch;
                analysis.rootCause = "Incompatible types or invalid cast";
                analysis.possibleFixes = new[]
                {
                    "Use correct type",
                    "Add type conversion",
                    "Check parameter types"
                };
                analysis.confidence = 7;
            }
            // === INVALID PARAMETERS ===
            else if (ContainsAny(errorLower, "invalid", "parameter", "argument"))
            {
                analysis.category = ErrorCategory.InvalidParameter;
                analysis.rootCause = "Invalid parameter value or format";
                analysis.possibleFixes = new[]
                {
                    "Check parameter format",
                    "Validate input values",
                    "Use correct parameter type"
                };
                analysis.confidence = 7;
            }
            // === RUNTIME ERRORS ===
            else if (ContainsAny(errorLower, "exception", "error", "failed"))
            {
                analysis.category = ErrorCategory.RuntimeError;
                analysis.rootCause = "Runtime exception occurred";
                analysis.possibleFixes = new[]
                {
                    "Check operation prerequisites",
                    "Add error handling",
                    "Validate state before operation"
                };
                analysis.confidence = 5;
            }
            else
            {
                analysis.category = ErrorCategory.Unknown;
                analysis.rootCause = "Unknown error type";
                analysis.possibleFixes = new[]
                {
                    "Read full error message",
                    "Check Unity Console",
                    "Review recent changes"
                };
                analysis.confidence = 3;
            }
            
            // Extract context from error message
            ExtractContext(errorMessage, analysis);
            
            return analysis;
        }
        
        /// <summary>
        /// Analyze compilation error specifics
        /// </summary>
        private static ErrorAnalysis AnalyzeCompilationError(string error, ErrorAnalysis analysis)
        {
            string errorLower = error.ToLower();
            
            // Missing using statement
            if (ContainsAny(errorLower, "does not exist in the current context", "could not be found"))
            {
                analysis.rootCause = "Missing using statement or namespace";
                analysis.possibleFixes = new[]
                {
                    "Add missing using statement (e.g., 'using UnityEngine;')",
                    "Check namespace spelling",
                    "Ensure assembly references are correct"
                };
                analysis.confidence = 9;
            }
            // Missing semicolon
            else if (ContainsAny(errorLower, "expected ;", "; expected"))
            {
                analysis.rootCause = "Missing semicolon";
                analysis.possibleFixes = new[]
                {
                    "Add semicolon at end of statement",
                    "Check for syntax errors in line"
                };
                analysis.confidence = 10;
            }
            // Brace mismatch
            else if (ContainsAny(errorLower, "expected }", "} expected", "{ expected"))
            {
                analysis.rootCause = "Mismatched braces";
                analysis.possibleFixes = new[]
                {
                    "Add missing closing brace",
                    "Remove extra brace",
                    "Check code block structure"
                };
                analysis.confidence = 9;
            }
            // Access modifier
            else if (ContainsAny(errorLower, "inaccessible", "protection level"))
            {
                analysis.rootCause = "Accessing private or protected member";
                analysis.possibleFixes = new[]
                {
                    "Change member to public",
                    "Use proper accessor method",
                    "Check access modifiers"
                };
                analysis.confidence = 8;
            }
            // Method not found
            else if (ContainsAny(errorLower, "does not contain a definition"))
            {
                analysis.rootCause = "Method or property not found";
                analysis.possibleFixes = new[]
                {
                    "Check method name spelling",
                    "Ensure method exists in class",
                    "Add missing method implementation"
                };
                analysis.confidence = 8;
            }
            else
            {
                analysis.rootCause = "Syntax or compilation error";
                analysis.possibleFixes = new[]
                {
                    "Check code syntax",
                    "Review error message details",
                    "Use RoslynScriptAnalyzer for validation"
                };
                analysis.confidence = 6;
            }
            
            return analysis;
        }
        
        /// <summary>
        /// Extract context information from error message
        /// </summary>
        private static void ExtractContext(string error, ErrorAnalysis analysis)
        {
            // Extract line number
            var lineMatch = Regex.Match(error, @"line (\d+)");
            if (lineMatch.Success)
            {
                analysis.context["line"] = lineMatch.Groups[1].Value;
            }
            
            // Extract script/file name
            var fileMatch = Regex.Match(error, @"'([^']+\.cs)'");
            if (fileMatch.Success)
            {
                analysis.context["file"] = fileMatch.Groups[1].Value;
            }
            
            // Extract GameObject name
            var goMatch = Regex.Match(error, @"GameObject '([^']+)'");
            if (goMatch.Success)
            {
                analysis.context["gameobject"] = goMatch.Groups[1].Value;
            }
            
            // Extract component name
            var compMatch = Regex.Match(error, @"component '([^']+)'", RegexOptions.IgnoreCase);
            if (compMatch.Success)
            {
                analysis.context["component"] = compMatch.Groups[1].Value;
            }
        }
        
        /// <summary>
        /// Check if string contains any of the keywords
        /// </summary>
        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k));
        }
        
        /// <summary>
        /// Get confidence level description
        /// </summary>
        public static string GetConfidenceDescription(int confidence)
        {
            return confidence switch
            {
                >= 9 => "🟢 Very High - Fix is almost certain",
                >= 7 => "🟡 High - Fix is likely correct",
                >= 5 => "🟠 Medium - Fix may work",
                >= 3 => "🔴 Low - Fix is speculative",
                _ => "⚫ Very Low - Manual investigation needed"
            };
        }
        
        /// <summary>
        /// Suggest fix priority based on error category
        /// </summary>
        public static int GetFixPriority(ErrorCategory category)
        {
            return category switch
            {
                ErrorCategory.CompilationError => 10,
                ErrorCategory.SyntaxError => 10,
                ErrorCategory.MissingReference => 9,
                ErrorCategory.ComponentNotFound => 9,
                ErrorCategory.GameObjectNotFound => 9,
                ErrorCategory.NullReference => 8,
                ErrorCategory.TypeMismatch => 7,
                ErrorCategory.InvalidParameter => 7,
                ErrorCategory.RuntimeError => 6,
                ErrorCategory.ScriptNotFound => 8,
                _ => 5
            };
        }
    }
}

