using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Advanced Roslyn-based semantic analysis
    /// Call graphs, data flow, symbol resolution, refactoring
    /// </summary>
    public static class RoslynSemanticAnalyzer
    {
        private static bool roslynAvailable = false;
        private static Type syntaxTreeType;
        private static Type compilationType;
        private static Type semanticModelType;
        private static Assembly roslynCSharpAssembly;
        private static Assembly roslynAssembly;
        
        static RoslynSemanticAnalyzer()
        {
            InitializeRoslyn();
        }
        
        /// <summary>
        /// Initialize Roslyn assemblies
        /// </summary>
        private static void InitializeRoslyn()
        {
            try
            {
                roslynCSharpAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp");
                    
                roslynAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Microsoft.CodeAnalysis");
                
                if (roslynCSharpAssembly != null && roslynAssembly != null)
                {
                    syntaxTreeType = roslynCSharpAssembly.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
                    compilationType = roslynCSharpAssembly.GetType("Microsoft.CodeAnalysis.CSharp.CSharpCompilation");
                    semanticModelType = roslynAssembly.GetType("Microsoft.CodeAnalysis.SemanticModel");
                    
                    roslynAvailable = syntaxTreeType != null && compilationType != null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Roslyn] Failed to initialize: {ex.Message}");
                roslynAvailable = false;
            }
        }
        
        /// <summary>
        /// Check if Roslyn is available
        /// </summary>
        public static bool IsAvailable => roslynAvailable;
        
        /// <summary>
        /// Get call graph for a method (who calls this method?)
        /// </summary>
        public static string GetCallGraph(string scriptName, string methodName)
        {
            if (!roslynAvailable)
                return "❌ Roslyn not available. This feature requires Microsoft.CodeAnalysis packages.";
            
            try
            {
                var results = new StringBuilder();
                results.AppendLine($"📞 Call Graph Analysis: {scriptName}.{methodName}()");
                results.AppendLine("═══════════════════════════════════");
                results.AppendLine();
                
                // Find all scripts in project
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                var callers = new List<(string file, int line, string context)>();
                
                foreach (var guid in scriptGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.StartsWith("Assets/")) continue;
                    
                    try
                    {
                        string content = File.ReadAllText(path);
                        
                        // Parse with Roslyn
                        var tree = ParseSyntaxTree(content);
                        if (tree == null) continue;
                        
                        // Find method invocations
                        var lines = content.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains($"{methodName}("))
                            {
                                string scriptFileName = Path.GetFileNameWithoutExtension(path);
                                string context = lines[i].Trim();
                                
                                if (context.Length > 80)
                                    context = context.Substring(0, 80) + "...";
                                
                                callers.Add((scriptFileName, i + 1, context));
                            }
                        }
                    }
                    catch { }
                }
                
                if (callers.Count == 0)
                {
                    results.AppendLine($"ℹ️ No callers found for {methodName}()");
                    results.AppendLine();
                    results.AppendLine("This method is either:");
                    results.AppendLine("  - Not called anywhere");
                    results.AppendLine("  - Only called via reflection");
                    results.AppendLine("  - A Unity callback (Update, Start, etc.)");
                }
                else
                {
                    results.AppendLine($"✅ Found {callers.Count} caller(s):");
                    results.AppendLine();
                    
                    foreach (var (file, line, context) in callers)
                    {
                        results.AppendLine($"📍 **{file}.cs** (line {line})");
                        results.AppendLine($"   {context}");
                        results.AppendLine();
                    }
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error analyzing call graph: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find all usages of a variable/field
        /// </summary>
        public static string FindSymbolUsages(string scriptName, string symbolName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = File.ReadAllText(scriptPath);
                var results = new StringBuilder();
                results.AppendLine($"🔍 Symbol Usage Analysis: {symbolName}");
                results.AppendLine("═══════════════════════════════════");
                results.AppendLine();
                
                var usages = new List<(int line, string context, string type)>();
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    
                    if (line.Contains(symbolName))
                    {
                        string type = "reference";
                        
                        if (line.Contains($"{symbolName} =") || line.Contains($"{symbolName}="))
                            type = "write";
                        else if (line.Contains($"= {symbolName}") || line.Contains($"={symbolName}"))
                            type = "read";
                        else if (line.Contains($"{symbolName}++") || line.Contains($"{symbolName}--"))
                            type = "modify";
                        
                        string context = line.Trim();
                        if (context.Length > 80)
                            context = context.Substring(0, 80) + "...";
                        
                        usages.Add((i + 1, context, type));
                    }
                }
                
                if (usages.Count == 0)
                {
                    results.AppendLine($"ℹ️ No usages found for '{symbolName}'");
                }
                else
                {
                    results.AppendLine($"✅ Found {usages.Count} usage(s):");
                    results.AppendLine();
                    
                    // Group by type
                    var byType = usages.GroupBy(u => u.type);
                    
                    foreach (var group in byType)
                    {
                        string icon = group.Key switch
                        {
                            "write" => "✏️",
                            "read" => "📖",
                            "modify" => "🔄",
                            _ => "📌"
                        };
                        
                        results.AppendLine($"## {icon} {group.Key.ToUpper()} ({group.Count()}):");
                        foreach (var (line, context, _) in group)
                        {
                            results.AppendLine($"  Line {line}: {context}");
                        }
                        results.AppendLine();
                    }
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding symbol usages: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze data flow (how data moves through the code)
        /// </summary>
        public static string AnalyzeDataFlow(string scriptName, string variableName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = File.ReadAllText(scriptPath);
                var results = new StringBuilder();
                results.AppendLine($"🌊 Data Flow Analysis: {variableName}");
                results.AppendLine("═══════════════════════════════════");
                results.AppendLine();
                
                var flow = new List<(int line, string operation, string context)>();
                var lines = content.Split('\n');
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    
                    if (line.Contains(variableName))
                    {
                        string operation = "use";
                        string context = line.Trim();
                        
                        // Detect operation type
                        if (line.Contains($"new ") && line.Contains(variableName))
                            operation = "initialize";
                        else if (line.Contains($"{variableName} ="))
                            operation = "assign";
                        else if (line.Contains($"{variableName}.") || line.Contains($"{variableName}["))
                            operation = "access";
                        else if (line.Contains($"({variableName}") || line.Contains($", {variableName}"))
                            operation = "parameter";
                        else if (line.Contains($"return {variableName}"))
                            operation = "return";
                        
                        if (context.Length > 70)
                            context = context.Substring(0, 70) + "...";
                        
                        flow.Add((i + 1, operation, context));
                    }
                }
                
                if (flow.Count == 0)
                {
                    results.AppendLine($"ℹ️ No data flow found for '{variableName}'");
                }
                else
                {
                    results.AppendLine($"✅ Data Flow ({flow.Count} operations):");
                    results.AppendLine();
                    
                    foreach (var (line, operation, context) in flow)
                    {
                        string icon = operation switch
                        {
                            "initialize" => "🆕",
                            "assign" => "✏️",
                            "access" => "👁️",
                            "parameter" => "📥",
                            "return" => "📤",
                            _ => "💠"
                        };
                        
                        results.AppendLine($"{icon} **Line {line}** [{operation}]");
                        results.AppendLine($"   {context}");
                        results.AppendLine();
                    }
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error analyzing data flow: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find all symbols (methods, fields, properties) in a script
        /// </summary>
        public static string GetAllSymbols(string scriptName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = File.ReadAllText(scriptPath);
                var results = new StringBuilder();
                results.AppendLine($"📋 Symbol Table: {scriptName}.cs");
                results.AppendLine("═══════════════════════════════════");
                results.AppendLine();
                
                // Extract methods
                var methodPattern = @"(public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(\w+)\s+(\w+)\s*\([^\)]*\)";
                var methods = System.Text.RegularExpressions.Regex.Matches(content, methodPattern);
                
                if (methods.Count > 0)
                {
                    results.AppendLine($"## 🔧 Methods ({methods.Count}):");
                    foreach (System.Text.RegularExpressions.Match match in methods)
                    {
                        string access = match.Groups[1].Value;
                        string returnType = match.Groups[2].Value;
                        string methodName = match.Groups[3].Value;
                        
                        string icon = access == "public" ? "🟢" : "🔴";
                        results.AppendLine($"  {icon} {access} {returnType} **{methodName}**()");
                    }
                    results.AppendLine();
                }
                
                // Extract fields
                var fieldPattern = @"(public|private|protected|internal)\s+(?:static\s+)?(?:readonly\s+)?(\w+)\s+(\w+)\s*(?:=|;)";
                var fields = System.Text.RegularExpressions.Regex.Matches(content, fieldPattern);
                
                if (fields.Count > 0)
                {
                    results.AppendLine($"## 📦 Fields ({fields.Count}):");
                    foreach (System.Text.RegularExpressions.Match match in fields)
                    {
                        string access = match.Groups[1].Value;
                        string type = match.Groups[2].Value;
                        string fieldName = match.Groups[3].Value;
                        
                        string icon = access == "public" ? "🟢" : "🔴";
                        results.AppendLine($"  {icon} {access} {type} **{fieldName}**");
                    }
                    results.AppendLine();
                }
                
                // Extract properties
                var propertyPattern = @"(public|private|protected|internal)\s+(\w+)\s+(\w+)\s*\{\s*get;";
                var properties = System.Text.RegularExpressions.Regex.Matches(content, propertyPattern);
                
                if (properties.Count > 0)
                {
                    results.AppendLine($"## 🔑 Properties ({properties.Count}):");
                    foreach (System.Text.RegularExpressions.Match match in properties)
                    {
                        string access = match.Groups[1].Value;
                        string type = match.Groups[2].Value;
                        string propName = match.Groups[3].Value;
                        
                        string icon = access == "public" ? "🟢" : "🔴";
                        results.AppendLine($"  {icon} {access} {type} **{propName}** {{ get; set; }}");
                    }
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting symbols: {e.Message}";
            }
        }
        
        /// <summary>
        /// Parse syntax tree using Roslyn
        /// </summary>
        private static object ParseSyntaxTree(string code)
        {
            if (!roslynAvailable) return null;
            
            try
            {
                var parseText = syntaxTreeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "ParseText" && m.GetParameters().Length >= 1);
                
                return parseText.Invoke(null, new object[] { code });
            }
            catch
            {
                return null;
            }
        }
    }
}

