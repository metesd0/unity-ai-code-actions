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
    /// Advanced Project Inspection: Search, Dependencies, Asset Management
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Advanced asset search with filters
        /// </summary>
        public static string SearchAssets(string searchQuery, string assetType = "", string folder = "Assets")
        {
            try
            {
                // Build search filter
                string filter = searchQuery;
                if (!string.IsNullOrEmpty(assetType))
                {
                    filter += $" t:{assetType}";
                }
                
                var guids = AssetDatabase.FindAssets(filter, new[] { folder });
                
                if (guids.Length == 0)
                    return $"❌ No assets found matching '{searchQuery}' (type: {assetType ?? "any"}) in {folder}";
                
                var results = new StringBuilder();
                results.AppendLine($"🔍 Found {guids.Length} asset(s) matching '{searchQuery}':");
                results.AppendLine();
                
                var assetsByType = new Dictionary<string, List<string>>();
                
                foreach (var guid in guids.Take(100)) // Limit to 100 for performance
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    
                    if (asset != null)
                    {
                        string type = asset.GetType().Name;
                        if (!assetsByType.ContainsKey(type))
                            assetsByType[type] = new List<string>();
                        
                        assetsByType[type].Add(path);
                    }
                }
                
                // Group by type
                foreach (var typeGroup in assetsByType.OrderBy(x => x.Key))
                {
                    results.AppendLine($"## {typeGroup.Key} ({typeGroup.Value.Count}):");
                    foreach (var path in typeGroup.Value.Take(20))
                    {
                        results.AppendLine($"  - {path}");
                    }
                    if (typeGroup.Value.Count > 20)
                        results.AppendLine($"  ... and {typeGroup.Value.Count - 20} more");
                    results.AppendLine();
                }
                
                if (guids.Length > 100)
                    results.AppendLine($"... showing first 100 of {guids.Length} results");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error searching assets: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find all references to an asset in the project
        /// </summary>
        public static string FindAssetReferences(string assetPath)
        {
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                    return $"❌ Asset not found: {assetPath}";
                
                var results = new StringBuilder();
                results.AppendLine($"🔍 Finding references to: {assetPath}");
                results.AppendLine();
                
                // Get all assets in project
                var allAssetPaths = AssetDatabase.GetAllAssetPaths()
                    .Where(p => p.StartsWith("Assets/"))
                    .ToArray();
                
                var references = new List<string>();
                
                foreach (var path in allAssetPaths)
                {
                    // Get dependencies of this asset
                    var dependencies = AssetDatabase.GetDependencies(path, false);
                    
                    if (dependencies.Contains(assetPath))
                    {
                        references.Add(path);
                    }
                }
                
                if (references.Count == 0)
                {
                    results.AppendLine("ℹ️ No references found. Asset is not used by any other assets.");
                }
                else
                {
                    results.AppendLine($"✅ Found {references.Count} reference(s):");
                    results.AppendLine();
                    
                    foreach (var refPath in references.Take(50))
                    {
                        var refAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(refPath);
                        string type = refAsset != null ? refAsset.GetType().Name : "Unknown";
                        results.AppendLine($"  - [{type}] {refPath}");
                    }
                    
                    if (references.Count > 50)
                        results.AppendLine($"  ... and {references.Count - 50} more");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding references: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze asset dependencies
        /// </summary>
        public static string AnalyzeAssetDependencies(string assetPath, bool recursive = false)
        {
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                    return $"❌ Asset not found: {assetPath}";
                
                var results = new StringBuilder();
                results.AppendLine($"📊 Dependency analysis for: {assetPath}");
                results.AppendLine();
                
                // Get dependencies
                var dependencies = AssetDatabase.GetDependencies(assetPath, recursive);
                
                // Exclude self
                dependencies = dependencies.Where(d => d != assetPath).ToArray();
                
                if (dependencies.Length == 0)
                {
                    results.AppendLine("ℹ️ No dependencies found. Asset is self-contained.");
                    return results.ToString();
                }
                
                results.AppendLine($"✅ Found {dependencies.Length} dependenc{(recursive ? "ies (recursive)" : "ies (direct)")}:");
                results.AppendLine();
                
                // Group by type
                var depsByType = new Dictionary<string, List<string>>();
                
                foreach (var depPath in dependencies)
                {
                    var depAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(depPath);
                    string type = depAsset != null ? depAsset.GetType().Name : "Unknown";
                    
                    if (!depsByType.ContainsKey(type))
                        depsByType[type] = new List<string>();
                    
                    depsByType[type].Add(depPath);
                }
                
                foreach (var typeGroup in depsByType.OrderByDescending(x => x.Value.Count))
                {
                    results.AppendLine($"## {typeGroup.Key} ({typeGroup.Value.Count}):");
                    foreach (var path in typeGroup.Value.Take(10))
                    {
                        results.AppendLine($"  - {path}");
                    }
                    if (typeGroup.Value.Count > 10)
                        results.AppendLine($"  ... and {typeGroup.Value.Count - 10} more");
                    results.AppendLine();
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error analyzing dependencies: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get detailed project structure
        /// </summary>
        public static string GetProjectStructure(string rootFolder = "Assets", int maxDepth = 3)
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine($"📁 Project Structure: {rootFolder}");
                results.AppendLine();
                
                void TraverseDirectory(string path, int depth, string prefix = "")
                {
                    if (depth > maxDepth) return;
                    
                    if (!Directory.Exists(path)) return;
                    
                    // Get directories
                    var directories = Directory.GetDirectories(path)
                        .Where(d => !d.Contains("/.") && !d.Contains("\\.")) // Skip hidden
                        .OrderBy(d => d)
                        .ToArray();
                    
                    // Get files
                    var files = Directory.GetFiles(path)
                        .Where(f => !f.EndsWith(".meta"))
                        .OrderBy(f => f)
                        .ToArray();
                    
                    // Show directories
                    for (int i = 0; i < directories.Length; i++)
                    {
                        bool isLast = i == directories.Length - 1 && files.Length == 0;
                        string dirName = Path.GetFileName(directories[i]);
                        string connector = isLast ? "└──" : "├──";
                        results.AppendLine($"{prefix}{connector} 📁 {dirName}/");
                        
                        string newPrefix = prefix + (isLast ? "    " : "│   ");
                        TraverseDirectory(directories[i], depth + 1, newPrefix);
                    }
                    
                    // Show files (limited)
                    int fileLimit = 10;
                    for (int i = 0; i < Math.Min(files.Length, fileLimit); i++)
                    {
                        bool isLast = i == files.Length - 1;
                        string fileName = Path.GetFileName(files[i]);
                        string connector = isLast ? "└──" : "├──";
                        
                        // File icon based on extension
                        string icon = Path.GetExtension(fileName) switch
                        {
                            ".cs" => "📄",
                            ".prefab" => "🎮",
                            ".unity" => "🗺️",
                            ".mat" => "🎨",
                            ".png" or ".jpg" => "🖼️",
                            ".mp3" or ".wav" => "🔊",
                            _ => "📎"
                        };
                        
                        results.AppendLine($"{prefix}{connector} {icon} {fileName}");
                    }
                    
                    if (files.Length > fileLimit)
                        results.AppendLine($"{prefix}└── ... and {files.Length - fileLimit} more files");
                }
                
                TraverseDirectory(rootFolder, 0);
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting project structure: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find unused assets in project
        /// </summary>
        public static string FindUnusedAssets(string folder = "Assets")
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine($"🔍 Finding unused assets in: {folder}");
                results.AppendLine("⏳ This may take a while for large projects...");
                results.AppendLine();
                
                // Get all assets
                var allAssets = AssetDatabase.FindAssets("", new[] { folder })
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !path.EndsWith(".cs")) // Exclude scripts
                    .ToList();
                
                // Get all scenes in build settings
                var scenesInBuild = EditorBuildSettings.scenes
                    .Select(s => s.path)
                    .ToArray();
                
                // Collect all dependencies from scenes
                var usedAssets = new HashSet<string>();
                
                foreach (var scenePath in scenesInBuild)
                {
                    if (string.IsNullOrEmpty(scenePath)) continue;
                    
                    var dependencies = AssetDatabase.GetDependencies(scenePath, true);
                    foreach (var dep in dependencies)
                    {
                        usedAssets.Add(dep);
                    }
                }
                
                // Find unused
                var unusedAssets = allAssets
                    .Where(asset => !usedAssets.Contains(asset))
                    .ToList();
                
                if (unusedAssets.Count == 0)
                {
                    results.AppendLine("✅ All assets are used! No unused assets found.");
                    return results.ToString();
                }
                
                results.AppendLine($"⚠️ Found {unusedAssets.Count} potentially unused asset(s):");
                results.AppendLine("(Not referenced by any scene in build settings)");
                results.AppendLine();
                
                // Group by type
                var byType = new Dictionary<string, List<string>>();
                foreach (var asset in unusedAssets)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset);
                    string type = obj != null ? obj.GetType().Name : "Unknown";
                    
                    if (!byType.ContainsKey(type))
                        byType[type] = new List<string>();
                    
                    byType[type].Add(asset);
                }
                
                foreach (var typeGroup in byType.OrderByDescending(x => x.Value.Count))
                {
                    results.AppendLine($"## {typeGroup.Key} ({typeGroup.Value.Count}):");
                    foreach (var path in typeGroup.Value.Take(20))
                    {
                        results.AppendLine($"  - {path}");
                    }
                    if (typeGroup.Value.Count > 20)
                        results.AppendLine($"  ... and {typeGroup.Value.Count - 20} more");
                    results.AppendLine();
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding unused assets: {e.Message}";
            }
        }
        
        /// <summary>
        /// Import asset from file system
        /// </summary>
        public static string ImportAsset(string sourcePath, string targetPath)
        {
            try
            {
                if (!File.Exists(sourcePath))
                    return $"❌ Source file not found: {sourcePath}";
                
                // Ensure target path is in Assets folder
                if (!targetPath.StartsWith("Assets/"))
                    targetPath = "Assets/" + targetPath;
                
                // Create directory if needed
                string directory = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // Copy file
                File.Copy(sourcePath, targetPath, true);
                
                // Import to Unity
                AssetDatabase.ImportAsset(targetPath);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ImportAsset] Imported {sourcePath} to {targetPath}");
                
                return $"✅ Imported asset to: {targetPath}";
            }
            catch (Exception e)
            {
                return $"❌ Error importing asset: {e.Message}";
            }
        }
        
        /// <summary>
        /// Organize assets into folders by type
        /// </summary>
        public static string OrganizeAssets(string sourceFolder, string targetRootFolder)
        {
            try
            {
                if (!AssetDatabase.IsValidFolder(sourceFolder))
                    return $"❌ Invalid source folder: {sourceFolder}";
                
                var results = new StringBuilder();
                results.AppendLine($"📁 Organizing assets from: {sourceFolder}");
                results.AppendLine();
                
                var assetGuids = AssetDatabase.FindAssets("", new[] { sourceFolder });
                
                var movedCount = 0;
                var byType = new Dictionary<string, int>();
                
                foreach (var guid in assetGuids)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    
                    if (asset == null) continue;
                    
                    string typeName = asset.GetType().Name;
                    string targetFolder = $"{targetRootFolder}/{typeName}s";
                    
                    // Create folder if needed
                    if (!AssetDatabase.IsValidFolder(targetFolder))
                    {
                        string parentFolder = Path.GetDirectoryName(targetFolder).Replace("\\", "/");
                        string folderName = Path.GetFileName(targetFolder);
                        AssetDatabase.CreateFolder(parentFolder, folderName);
                    }
                    
                    // Move asset
                    string fileName = Path.GetFileName(assetPath);
                    string newPath = $"{targetFolder}/{fileName}";
                    
                    if (assetPath != newPath)
                    {
                        string error = AssetDatabase.MoveAsset(assetPath, newPath);
                        if (string.IsNullOrEmpty(error))
                        {
                            movedCount++;
                            if (!byType.ContainsKey(typeName))
                                byType[typeName] = 0;
                            byType[typeName]++;
                        }
                    }
                }
                
                if (movedCount == 0)
                {
                    results.AppendLine("ℹ️ No assets to organize.");
                    return results.ToString();
                }
                
                results.AppendLine($"✅ Organized {movedCount} asset(s):");
                foreach (var type in byType.OrderByDescending(x => x.Value))
                {
                    results.AppendLine($"  - {type.Key}: {type.Value} asset(s)");
                }
                
                AssetDatabase.Refresh();
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error organizing assets: {e.Message}";
            }
        }
    }
}

