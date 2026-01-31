using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Code Analysis: Code Smells, Complexity, Dependencies, Quality Metrics
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Calculate cyclomatic complexity of a script
        /// </summary>
        public static string CalculateComplexity(string scriptName)
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
                results.AppendLine($"📊 Complexity Analysis: {scriptName}.cs");
                results.AppendLine();
                
                // Extract methods
                var methodPattern = @"(public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:void|bool|int|float|string|[\w<>]+)\s+(\w+)\s*\([^\)]*\)\s*\{";
                var methods = Regex.Matches(content, methodPattern);
                
                var methodComplexities = new List<(string name, int complexity)>();
                int totalComplexity = 0;
                
                foreach (Match method in methods)
                {
                    string methodName = method.Groups[2].Value;
                    int methodStart = method.Index;
                    
                    // Find method end
                    int braceCount = 1;
                    int methodBodyStart = content.IndexOf('{', methodStart) + 1;
                    int methodEnd = methodBodyStart;
                    
                    for (int i = methodBodyStart; i < content.Length && braceCount > 0; i++)
                    {
                        if (content[i] == '{') braceCount++;
                        if (content[i] == '}') braceCount--;
                        methodEnd = i;
                    }
                    
                    if (braceCount == 0)
                    {
                        string methodBody = content.Substring(methodBodyStart, methodEnd - methodBodyStart);
                        
                        // Calculate complexity
                        int complexity = 1; // Base complexity
                        
                        // Decision points
                        complexity += Regex.Matches(methodBody, @"\bif\b").Count;
                        complexity += Regex.Matches(methodBody, @"\belse\s+if\b").Count;
                        complexity += Regex.Matches(methodBody, @"\bwhile\b").Count;
                        complexity += Regex.Matches(methodBody, @"\bfor\b").Count;
                        complexity += Regex.Matches(methodBody, @"\bforeach\b").Count;
                        complexity += Regex.Matches(methodBody, @"\bcase\b").Count;
                        complexity += Regex.Matches(methodBody, @"\bcatch\b").Count;
                        complexity += Regex.Matches(methodBody, @"\b&&\b").Count;
                        complexity += Regex.Matches(methodBody, @"\b\|\|\b").Count;
                        complexity += Regex.Matches(methodBody, @"\?.*:").Count; // Ternary
                        
                        methodComplexities.Add((methodName, complexity));
                        totalComplexity += complexity;
                    }
                }
                
                if (methodComplexities.Count == 0)
                {
                    results.AppendLine("ℹ️ No methods found in script.");
                    return results.ToString();
                }
                
                results.AppendLine($"**Methods analyzed:** {methodComplexities.Count}");
                results.AppendLine($"**Total complexity:** {totalComplexity}");
                results.AppendLine($"**Average complexity:** {totalComplexity / (float)methodComplexities.Count:F1}");
                results.AppendLine();
                
                results.AppendLine("## Methods by Complexity:");
                results.AppendLine();
                
                foreach (var (name, complexity) in methodComplexities.OrderByDescending(m => m.complexity))
                {
                    string rating = complexity <= 5 ? "✅ Low" :
                                   complexity <= 10 ? "⚠️ Medium" :
                                   complexity <= 20 ? "🔶 High" : "🔴 Very High";
                    
                    results.AppendLine($"  - **{name}**: {complexity} ({rating})");
                }
                
                results.AppendLine();
                results.AppendLine("💡 **Recommendations:**");
                results.AppendLine("  - Complexity 1-5: Simple, good");
                results.AppendLine("  - Complexity 6-10: Moderate, acceptable");
                results.AppendLine("  - Complexity 11-20: Complex, consider refactoring");
                results.AppendLine("  - Complexity 21+: Very complex, should refactor");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error calculating complexity: {e.Message}";
            }
        }
        
        /// <summary>
        /// Detect code smells in a script
        /// </summary>
        public static string DetectCodeSmells(string scriptName)
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
                results.AppendLine($"🔍 Code Smell Detection: {scriptName}.cs");
                results.AppendLine();
                
                var smells = new List<string>();
                
                // 1. Long methods (>50 lines)
                var methodPattern = @"(public|private|protected|internal)\s+(?:static\s+)?(?:async\s+)?(?:void|bool|int|float|string|[\w<>]+)\s+(\w+)\s*\([^\)]*\)\s*\{";
                var methods = Regex.Matches(content, methodPattern);
                
                foreach (Match method in methods)
                {
                    string methodName = method.Groups[2].Value;
                    int methodStart = method.Index;
                    
                    int braceCount = 1;
                    int methodBodyStart = content.IndexOf('{', methodStart) + 1;
                    int methodEnd = methodBodyStart;
                    
                    for (int i = methodBodyStart; i < content.Length && braceCount > 0; i++)
                    {
                        if (content[i] == '{') braceCount++;
                        if (content[i] == '}') braceCount--;
                        methodEnd = i;
                    }
                    
                    if (braceCount == 0)
                    {
                        string methodBody = content.Substring(methodBodyStart, methodEnd - methodBodyStart);
                        int lineCount = methodBody.Split('\n').Length;
                        
                        if (lineCount > 50)
                        {
                            smells.Add($"⚠️ **Long Method**: `{methodName}` ({lineCount} lines) - Consider breaking into smaller methods");
                        }
                    }
                }
                
                // 2. Magic numbers
                var magicNumbers = Regex.Matches(content, @"(?<![a-zA-Z0-9_])(\d+(?:\.\d+)?f?)(?![a-zA-Z0-9_])");
                var significantNumbers = magicNumbers.Cast<Match>()
                    .Select(m => m.Value)
                    .Where(n => n != "0" && n != "1" && n != "2" && !n.StartsWith("0.") && n != "false" && n != "true")
                    .Distinct()
                    .Take(10);
                
                if (significantNumbers.Any())
                {
                    smells.Add($"🔢 **Magic Numbers**: Found {significantNumbers.Count()} magic numbers - Consider using named constants");
                }
                
                // 3. Deep nesting (>3 levels)
                var lines = content.Split('\n');
                int maxNesting = 0;
                int currentNesting = 0;
                
                foreach (var line in lines)
                {
                    currentNesting += line.Count(c => c == '{');
                    currentNesting -= line.Count(c => c == '}');
                    maxNesting = Math.Max(maxNesting, currentNesting);
                }
                
                if (maxNesting > 4)
                {
                    smells.Add($"📊 **Deep Nesting**: Max nesting level {maxNesting} - Consider extracting methods");
                }
                
                // 4. Commented code
                var commentedCodeLines = lines.Where(l => 
                    l.Trim().StartsWith("//") && 
                    (l.Contains("(") || l.Contains("{") || l.Contains("="))
                ).Count();
                
                if (commentedCodeLines > 3)
                {
                    smells.Add($"💬 **Commented Code**: ~{commentedCodeLines} lines of commented code - Remove or uncomment");
                }
                
                // 5. Long parameter lists (>5 params)
                var longParamMethods = Regex.Matches(content, @"\w+\s*\([^)]{100,}\)")
                    .Cast<Match>()
                    .Where(m => m.Value.Split(',').Length > 5);
                
                if (longParamMethods.Any())
                {
                    smells.Add($"📝 **Long Parameter Lists**: {longParamMethods.Count()} method(s) with >5 parameters - Consider parameter objects");
                }
                
                // 6. Duplicate code (simple heuristic)
                var codeLines = lines.Where(l => !string.IsNullOrWhiteSpace(l) && !l.Trim().StartsWith("//")).ToList();
                var duplicates = codeLines.GroupBy(l => l.Trim())
                    .Where(g => g.Count() > 3 && g.Key.Length > 20)
                    .ToList();
                
                if (duplicates.Any())
                {
                    smells.Add($"📋 **Duplicate Code**: Found {duplicates.Count} potentially duplicated line(s) - Consider extracting");
                }
                
                // 7. Missing error handling
                var methodsWithTryCatch = Regex.Matches(content, @"try\s*\{").Count;
                var totalMethods = methods.Count;
                
                if (totalMethods > 3 && methodsWithTryCatch == 0)
                {
                    smells.Add($"⚠️ **No Error Handling**: {totalMethods} methods without try-catch - Consider adding error handling");
                }
                
                // 8. Public fields (should be properties)
                var publicFields = Regex.Matches(content, @"public\s+(?!class|interface|enum|struct|static\s+readonly)(\w+)\s+(\w+)\s*;");
                if (publicFields.Count > 0)
                {
                    smells.Add($"🔓 **Public Fields**: {publicFields.Count} public field(s) - Consider using properties or [SerializeField] private");
                }
                
                // Results
                if (smells.Count == 0)
                {
                    results.AppendLine("✅ **No major code smells detected!**");
                    results.AppendLine();
                    results.AppendLine("Your code looks clean! Keep up the good work.");
                }
                else
                {
                    results.AppendLine($"⚠️ **Found {smells.Count} code smell(s):**");
                    results.AppendLine();
                    
                    foreach (var smell in smells)
                    {
                        results.AppendLine(smell);
                    }
                    
                    results.AppendLine();
                    results.AppendLine("💡 **Tip:** Addressing these smells will improve code maintainability!");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error detecting code smells: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze script dependencies and suggest optimizations
        /// </summary>
        public static string AnalyzeScriptDependencies(string scriptName)
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
                results.AppendLine($"🔗 Dependency Analysis: {scriptName}.cs");
                results.AppendLine();
                
                // Extract using statements
                var usingStatements = Regex.Matches(content, @"using\s+([\w\.]+);")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .ToList();
                
                results.AppendLine($"**Using Statements:** {usingStatements.Count}");
                results.AppendLine();
                
                // Categorize namespaces
                var unity = usingStatements.Where(u => u.StartsWith("UnityEngine")).ToList();
                var system = usingStatements.Where(u => u.StartsWith("System")).ToList();
                var editor = usingStatements.Where(u => u.StartsWith("UnityEditor")).ToList();
                var custom = usingStatements.Except(unity).Except(system).Except(editor).ToList();
                
                if (unity.Any())
                {
                    results.AppendLine($"**Unity ({unity.Count}):**");
                    foreach (var ns in unity)
                        results.AppendLine($"  - {ns}");
                    results.AppendLine();
                }
                
                if (system.Any())
                {
                    results.AppendLine($"**System ({system.Count}):**");
                    foreach (var ns in system)
                        results.AppendLine($"  - {ns}");
                    results.AppendLine();
                }
                
                if (editor.Any())
                {
                    results.AppendLine($"**UnityEditor ({editor.Count}):**");
                    foreach (var ns in editor)
                        results.AppendLine($"  - {ns}");
                    results.AppendLine();
                    results.AppendLine("⚠️ **Warning:** UnityEditor dependencies in runtime script!");
                    results.AppendLine();
                }
                
                if (custom.Any())
                {
                    results.AppendLine($"**Custom/Third-party ({custom.Count}):**");
                    foreach (var ns in custom)
                        results.AppendLine($"  - {ns}");
                    results.AppendLine();
                }
                
                // Analyze class dependencies
                var classReferences = Regex.Matches(content, @"\b([A-Z]\w+)\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(c => !c.Equals(scriptName))
                    .Distinct()
                    .OrderBy(c => c)
                    .Take(20)
                    .ToList();
                
                if (classReferences.Any())
                {
                    results.AppendLine($"**Referenced Types (top 20):**");
                    foreach (var type in classReferences)
                        results.AppendLine($"  - {type}");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error analyzing dependencies: {e.Message}";
            }
        }
        
        /// <summary>
        /// Generate code quality report for entire project
        /// </summary>
        public static string GenerateQualityReport()
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine("📊 PROJECT CODE QUALITY REPORT");
                results.AppendLine("═══════════════════════════════");
                results.AppendLine();
                
                // Find all scripts
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                var scripts = scriptGuids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path.StartsWith("Assets/") && !path.Contains("/Editor/"))
                    .ToList();
                
                results.AppendLine($"**Total Scripts:** {scripts.Count}");
                results.AppendLine();
                
                int totalLines = 0;
                int totalMethods = 0;
                int scriptsWithSmells = 0;
                var largestScripts = new List<(string name, int lines)>();
                
                foreach (var scriptPath in scripts)
                {
                    try
                    {
                        string content = File.ReadAllText(scriptPath);
                        int lines = content.Split('\n').Length;
                        totalLines += lines;
                        
                        var methods = Regex.Matches(content, @"(public|private|protected)\s+(?:static\s+)?(?:void|bool|int|float|string|[\w<>]+)\s+\w+\s*\(");
                        totalMethods += methods.Count;
                        
                        largestScripts.Add((Path.GetFileNameWithoutExtension(scriptPath), lines));
                        
                        // Quick smell check
                        if (lines > 500 || methods.Count > 20)
                            scriptsWithSmells++;
                    }
                    catch { }
                }
                
                results.AppendLine($"**Total Lines of Code:** {totalLines:N0}");
                results.AppendLine($"**Total Methods:** {totalMethods:N0}");
                results.AppendLine($"**Average Lines per Script:** {totalLines / Math.Max(1, scripts.Count)}");
                results.AppendLine($"**Average Methods per Script:** {totalMethods / Math.Max(1, scripts.Count)}");
                results.AppendLine();
                
                results.AppendLine("## 📏 Largest Scripts:");
                foreach (var (name, lines) in largestScripts.OrderByDescending(s => s.lines).Take(10))
                {
                    string status = lines > 500 ? "🔴" : lines > 300 ? "🔶" : "✅";
                    results.AppendLine($"  {status} **{name}**: {lines} lines");
                }
                results.AppendLine();
                
                results.AppendLine($"## ⚠️ Quality Metrics:");
                results.AppendLine($"  - Scripts with potential issues: {scriptsWithSmells} ({scriptsWithSmells * 100 / Math.Max(1, scripts.Count)}%)");
                results.AppendLine();
                
                // Overall grade
                int grade = 100;
                if (scriptsWithSmells > scripts.Count / 4) grade -= 20;
                if (totalLines / Math.Max(1, scripts.Count) > 300) grade -= 15;
                if (totalMethods / Math.Max(1, scripts.Count) > 15) grade -= 15;
                
                string gradeText = grade >= 90 ? "A+ 🏆" :
                                  grade >= 80 ? "A ✅" :
                                  grade >= 70 ? "B 👍" :
                                  grade >= 60 ? "C 😐" : "D ⚠️";
                
                results.AppendLine($"## 🎯 Overall Code Quality: **{gradeText}**");
                results.AppendLine();
                results.AppendLine("💡 **Recommendations:**");
                results.AppendLine("  - Keep scripts under 300 lines");
                results.AppendLine("  - Keep methods under 50 lines");
                results.AppendLine("  - Maximum 15 methods per class");
                results.AppendLine("  - Use Extract Method refactoring for large methods");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error generating quality report: {e.Message}";
            }
        }
    }
}

