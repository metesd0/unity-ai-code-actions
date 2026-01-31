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
    /// Analyze project context and patterns
    /// Learn project structure, common patterns, architecture
    /// </summary>
    public class ProjectContextAnalyzer
    {
        private LongTermMemoryManager memory;
        
        public ProjectContextAnalyzer(LongTermMemoryManager memory)
        {
            this.memory = memory;
        }
        
        /// <summary>
        /// Analyze entire project and store insights
        /// </summary>
        public string AnalyzeProject()
        {
            try
            {
                var insights = new List<string>();
                
                // Analyze scripts
                var scriptInsights = AnalyzeScripts();
                insights.AddRange(scriptInsights);
                
                // Analyze scenes
                var sceneInsights = AnalyzeScenes();
                insights.AddRange(sceneInsights);
                
                // Analyze folder structure
                var structureInsights = AnalyzeFolderStructure();
                insights.AddRange(structureInsights);
                
                // Store insights
                foreach (var insight in insights)
                {
                    memory.Store(MemoryType.ProjectContext, insight, importance: 0.7f);
                }
                
                var result = new StringBuilder();
                result.AppendLine("🔍 PROJECT ANALYSIS COMPLETE");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine($"Found {insights.Count} insights:");
                result.AppendLine();
                
                foreach (var insight in insights)
                {
                    result.AppendLine($"  • {insight}");
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Analysis error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze scripts in project
        /// </summary>
        private List<string> AnalyzeScripts()
        {
            var insights = new List<string>();
            
            try
            {
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                var scripts = scriptGuids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path.StartsWith("Assets/") && path.EndsWith(".cs"))
                    .ToList();
                
                insights.Add($"Project has {scripts.Count} C# scripts");
                
                // Analyze namespace usage
                var namespaces = new HashSet<string>();
                var baseClasses = new Dictionary<string, int>();
                
                foreach (var scriptPath in scripts.Take(100)) // Limit for performance
                {
                    try
                    {
                        string content = File.ReadAllText(scriptPath);
                        
                        // Detect namespace
                        var nsMatch = System.Text.RegularExpressions.Regex.Match(content, @"namespace\s+([^\s{]+)");
                        if (nsMatch.Success)
                        {
                            namespaces.Add(nsMatch.Groups[1].Value);
                        }
                        
                        // Detect base classes
                        var classMatches = System.Text.RegularExpressions.Regex.Matches(content, @":\s*(MonoBehaviour|ScriptableObject|Editor|EditorWindow)");
                        foreach (System.Text.RegularExpressions.Match match in classMatches)
                        {
                            string baseClass = match.Groups[1].Value;
                            if (!baseClasses.ContainsKey(baseClass))
                                baseClasses[baseClass] = 0;
                            baseClasses[baseClass]++;
                        }
                    }
                    catch { }
                }
                
                // Insights
                if (namespaces.Count > 0)
                {
                    insights.Add($"Uses {namespaces.Count} namespace(s): {string.Join(", ", namespaces.Take(3))}");
                }
                
                foreach (var kvp in baseClasses.OrderByDescending(x => x.Value))
                {
                    insights.Add($"Has {kvp.Value} {kvp.Key} scripts");
                }
                
                // Detect patterns
                if (baseClasses.ContainsKey("ScriptableObject"))
                {
                    insights.Add("Project uses ScriptableObjects pattern");
                }
                
                if (scripts.Any(s => s.Contains("/Editor/")))
                {
                    insights.Add("Has custom editor scripts");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectAnalysis] Script analysis error: {e.Message}");
            }
            
            return insights;
        }
        
        /// <summary>
        /// Analyze scenes
        /// </summary>
        private List<string> AnalyzeScenes()
        {
            var insights = new List<string>();
            
            try
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                insights.Add($"Project has {sceneGuids.Length} scene(s)");
                
                // Build settings scenes
                var buildScenes = UnityEditor.EditorBuildSettings.scenes;
                if (buildScenes.Length > 0)
                {
                    insights.Add($"{buildScenes.Length} scene(s) in build settings");
                }
                
                // Active scene
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    insights.Add($"Current scene: {activeScene.name}");
                    
                    int gameObjectCount = activeScene.rootCount;
                    insights.Add($"Active scene has {gameObjectCount} root GameObject(s)");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectAnalysis] Scene analysis error: {e.Message}");
            }
            
            return insights;
        }
        
        /// <summary>
        /// Analyze folder structure
        /// </summary>
        private List<string> AnalyzeFolderStructure()
        {
            var insights = new List<string>();
            
            try
            {
                string[] commonFolders = { "Scripts", "Prefabs", "Materials", "Textures", "Scenes", "Editor", "Resources" };
                var existingFolders = new List<string>();
                
                foreach (var folder in commonFolders)
                {
                    if (Directory.Exists(Path.Combine("Assets", folder)))
                    {
                        existingFolders.Add(folder);
                    }
                }
                
                if (existingFolders.Count > 0)
                {
                    insights.Add($"Uses standard folder structure: {string.Join(", ", existingFolders)}");
                }
                
                // Detect organization pattern
                if (existingFolders.Contains("Scripts") && existingFolders.Contains("Prefabs"))
                {
                    insights.Add("Project follows asset-type organization");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectAnalysis] Structure analysis error: {e.Message}");
            }
            
            return insights;
        }
        
        /// <summary>
        /// Detect project type (FPS, RPG, Platformer, etc.)
        /// </summary>
        public string DetectProjectType()
        {
            try
            {
                var keywords = new Dictionary<string, List<string>>
                {
                    { "FPS", new List<string> { "first person", "fps", "gun", "weapon", "shoot" } },
                    { "RPG", new List<string> { "rpg", "inventory", "quest", "stats", "level up" } },
                    { "Platformer", new List<string> { "platform", "jump", "2d", "sidescroll" } },
                    { "Strategy", new List<string> { "strategy", "rts", "unit", "build", "resource" } },
                    { "Puzzle", new List<string> { "puzzle", "match", "solve" } },
                };
                
                var scores = new Dictionary<string, int>();
                
                // Search scripts for keywords
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                
                foreach (var guid in scriptGuids.Take(50)) // Limit for performance
                {
                    try
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!path.StartsWith("Assets/")) continue;
                        
                        string content = File.ReadAllText(path).ToLower();
                        
                        foreach (var genre in keywords)
                        {
                            if (!scores.ContainsKey(genre.Key))
                                scores[genre.Key] = 0;
                            
                            foreach (var keyword in genre.Value)
                            {
                                if (content.Contains(keyword))
                                {
                                    scores[genre.Key]++;
                                }
                            }
                        }
                    }
                    catch { }
                }
                
                if (scores.Count > 0)
                {
                    var topGenre = scores.OrderByDescending(kvp => kvp.Value).First();
                    if (topGenre.Value > 0)
                    {
                        memory.Store(MemoryType.ProjectContext,
                            $"Project type detected: {topGenre.Key}",
                            importance: 0.9f);
                        
                        return topGenre.Key;
                    }
                }
                
                return "General";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        /// <summary>
        /// Get project summary
        /// </summary>
        public string GetProjectSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📁 PROJECT CONTEXT");
            sb.AppendLine("═══════════════════════════════════");
            
            var contextMemories = memory.Recall(MemoryType.ProjectContext, limit: 20);
            
            if (contextMemories.Count > 0)
            {
                foreach (var mem in contextMemories)
                {
                    sb.AppendLine($"  • {mem.content}");
                }
            }
            else
            {
                sb.AppendLine("No project context learned yet.");
                sb.AppendLine("Use 'analyze_project' to analyze.");
            }
            
            return sb.ToString();
        }
    }
}

