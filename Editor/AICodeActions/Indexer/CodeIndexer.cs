using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using AICodeActions.Core;

namespace AICodeActions.Indexer
{
    /// <summary>
    /// Indexes C# scripts, scenes, and prefabs in the project
    /// Simplified AST parsing using regex (Roslyn integration can be added later)
    /// </summary>
    public class CodeIndexer
    {
        private ProjectContext context;
        private Dictionary<string, DateTime> fileCache = new Dictionary<string, DateTime>();

        public CodeIndexer()
        {
            context = new ProjectContext();
        }

        public ProjectContext GetContext() => context;

        /// <summary>
        /// Performs full project indexing
        /// </summary>
        public void IndexProject()
        {
            context = new ProjectContext();
            IndexScripts();
            IndexScenes();
            IndexPrefabs();
            Debug.Log($"[AI Code Actions] Indexed {context.scripts.Count} scripts, {context.scenes.Count} scenes, {context.prefabs.Count} prefabs");
        }

        /// <summary>
        /// Incremental update - only re-index changed files
        /// </summary>
        public void IncrementalUpdate()
        {
            var scriptsToUpdate = new List<string>();
            var allScripts = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (var scriptPath in allScripts)
            {
                var lastWrite = File.GetLastWriteTime(scriptPath);
                if (!fileCache.ContainsKey(scriptPath) || fileCache[scriptPath] < lastWrite)
                {
                    scriptsToUpdate.Add(scriptPath);
                    fileCache[scriptPath] = lastWrite;
                }
            }

            if (scriptsToUpdate.Count > 0)
            {
                Debug.Log($"[AI Code Actions] Updating {scriptsToUpdate.Count} changed scripts");
                foreach (var path in scriptsToUpdate)
                {
                    UpdateScript(path);
                }
            }
        }

        private void IndexScripts()
        {
            var scriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
            
            foreach (var scriptPath in scriptPaths)
            {
                // Skip generated and package files
                if (scriptPath.Contains("\\Temp\\") || scriptPath.Contains("\\Library\\"))
                    continue;

                try
                {
                    var scriptInfo = ParseScript(scriptPath);
                    context.scripts.Add(scriptInfo);
                    fileCache[scriptPath] = File.GetLastWriteTime(scriptPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse {scriptPath}: {e.Message}");
                }
            }
        }

        private void UpdateScript(string scriptPath)
        {
            var relativePath = GetRelativePath(scriptPath);
            var existing = context.scripts.FirstOrDefault(s => s.path == relativePath);
            
            if (existing != null)
                context.scripts.Remove(existing);

            var scriptInfo = ParseScript(scriptPath);
            context.scripts.Add(scriptInfo);
        }

        private ScriptInfo ParseScript(string fullPath)
        {
            var content = File.ReadAllText(fullPath);
            var relativePath = GetRelativePath(fullPath);
            var fileName = Path.GetFileNameWithoutExtension(fullPath);

            var info = new ScriptInfo
            {
                name = fileName,
                path = relativePath,
                content = content
            };

            // Simple regex-based parsing (can be replaced with Roslyn later)
            
            // Extract classes
            var classMatches = Regex.Matches(content, @"(?:public|private|internal|protected)?\s*(?:abstract|sealed)?\s*(?:partial)?\s*class\s+(\w+)");
            foreach (Match match in classMatches)
            {
                info.classes.Add(match.Groups[1].Value);
            }

            // Extract methods
            var methodMatches = Regex.Matches(content, @"(?:public|private|internal|protected)\s+(?:static\s+)?(?:async\s+)?(?:void|bool|int|float|string|[\w<>]+)\s+(\w+)\s*\(");
            foreach (Match match in methodMatches)
            {
                info.methods.Add(match.Groups[1].Value);
            }

            // Extract using statements (dependencies)
            var usingMatches = Regex.Matches(content, @"using\s+([\w\.]+);");
            foreach (Match match in usingMatches)
            {
                info.dependencies.Add(match.Groups[1].Value);
            }

            return info;
        }

        private void IndexScenes()
        {
            var scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToList();

            foreach (var scenePath in scenePaths)
            {
                var sceneInfo = new SceneInfo
                {
                    name = Path.GetFileNameWithoutExtension(scenePath),
                    path = scenePath
                };
                
                // Scene analysis can be expanded
                context.scenes.Add(sceneInfo);
            }
        }

        private void IndexPrefabs()
        {
            var prefabPaths = AssetDatabase.FindAssets("t:Prefab")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToList();

            foreach (var prefabPath in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null) continue;

                var prefabInfo = new PrefabInfo
                {
                    name = prefab.name,
                    path = prefabPath
                };

                // Get all components
                var components = prefab.GetComponentsInChildren<Component>(true);
                prefabInfo.components = components
                    .Where(c => c != null)
                    .Select(c => c.GetType().Name)
                    .Distinct()
                    .ToList();

                context.prefabs.Add(prefabInfo);
            }
        }

        private string GetRelativePath(string fullPath)
        {
            return "Assets" + fullPath.Replace(Application.dataPath, "").Replace("\\", "/");
        }

        /// <summary>
        /// Get context for specific files (for targeted operations)
        /// </summary>
        public ProjectContext GetContextForFiles(List<string> filePaths)
        {
            var targetContext = new ProjectContext();
            
            foreach (var path in filePaths)
            {
                var script = context.scripts.FirstOrDefault(s => s.path == path || s.name == path);
                if (script != null)
                {
                    targetContext.scripts.Add(script);
                }
            }

            return targetContext;
        }
    }
}

