using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICodeActions.Core
{
    /// <summary>
    /// Advanced Scene Management: Multi-scene, Templates, Build Settings, Comparison
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Load scene additively (multi-scene editing)
        /// </summary>
        public static string LoadSceneAdditive(string sceneName)
        {
            try
            {
                // Find scene
                var sceneGuids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
                if (sceneGuids.Length == 0)
                    return $"❌ Scene '{sceneName}' not found";
                
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
                
                // Load additively
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                
                Debug.Log($"[LoadSceneAdditive] Loaded {sceneName} additively");
                
                var results = new StringBuilder();
                results.AppendLine($"✅ Loaded scene '{sceneName}' additively");
                results.AppendLine();
                results.AppendLine($"**Scene Path:** {scenePath}");
                results.AppendLine($"**Root GameObjects:** {scene.rootCount}");
                results.AppendLine($"**Is Loaded:** {scene.isLoaded}");
                results.AppendLine();
                results.AppendLine($"💡 **Tip:** Use unload_scene to unload this scene");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error loading scene: {e.Message}";
            }
        }
        
        /// <summary>
        /// Unload an additively loaded scene
        /// </summary>
        public static string UnloadScene(string sceneName)
        {
            try
            {
                // Find loaded scene
                Scene? targetScene = null;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == sceneName || scene.path.Contains(sceneName))
                    {
                        targetScene = scene;
                        break;
                    }
                }
                
                if (!targetScene.HasValue)
                    return $"❌ Scene '{sceneName}' is not loaded";
                
                if (!EditorSceneManager.CloseScene(targetScene.Value, true))
                    return $"❌ Failed to unload scene '{sceneName}'";
                
                Debug.Log($"[UnloadScene] Unloaded {sceneName}");
                
                return $"✅ Unloaded scene '{sceneName}'";
            }
            catch (Exception e)
            {
                return $"❌ Error unloading scene: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get list of currently loaded scenes
        /// </summary>
        public static string GetLoadedScenes()
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine($"📋 Currently Loaded Scenes: {SceneManager.sceneCount}");
                results.AppendLine();
                
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    string activeMarker = SceneManager.GetActiveScene() == scene ? "⭐" : "  ";
                    
                    results.AppendLine($"{activeMarker} **{scene.name}**");
                    results.AppendLine($"   Path: {scene.path}");
                    results.AppendLine($"   GameObjects: {scene.rootCount}");
                    results.AppendLine($"   Modified: {scene.isDirty}");
                    results.AppendLine();
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting loaded scenes: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set active scene (for multi-scene editing)
        /// </summary>
        public static string SetActiveScene(string sceneName)
        {
            try
            {
                // Find loaded scene
                Scene? targetScene = null;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == sceneName || scene.path.Contains(sceneName))
                    {
                        targetScene = scene;
                        break;
                    }
                }
                
                if (!targetScene.HasValue)
                    return $"❌ Scene '{sceneName}' is not loaded";
                
                SceneManager.SetActiveScene(targetScene.Value);
                
                Debug.Log($"[SetActiveScene] Set {sceneName} as active scene");
                
                return $"✅ Set '{sceneName}' as active scene\n💡 New GameObjects will be created in this scene";
            }
            catch (Exception e)
            {
                return $"❌ Error setting active scene: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create scene from template
        /// </summary>
        public static string CreateSceneFromTemplate(string sceneName, string templateType)
        {
            try
            {
                string scenePath = $"Assets/{sceneName}.unity";
                
                // Create new scene
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                
                // Apply template
                switch (templateType.ToLower())
                {
                    case "basic":
                        // Basic scene: Camera + Light
                        CreateCamera("Main Camera", 60f);
                        CreateLight("Directional Light", "directional", "white", 1f);
                        break;
                    
                    case "3d":
                        // 3D scene: Camera + Light + Ground
                        CreateCamera("Main Camera", 60f);
                        SetPosition("Main Camera", 0, 1, -10);
                        CreateLight("Directional Light", "directional", "white", 1f);
                        SetRotation("Directional Light", 50, -30, 0);
                        CreatePrimitive("Plane", "Ground", 0, 0, 0);
                        SetScale("Ground", 10, 1, 10);
                        break;
                    
                    case "2d":
                        // 2D scene: Camera (orthographic) + Light
                        CreateCamera("Main Camera", 60f);
                        var cam = GameObject.Find("Main Camera").GetComponent<Camera>();
                        cam.orthographic = true;
                        cam.orthographicSize = 5;
                        CreateLight("Directional Light", "directional", "white", 1f);
                        break;
                    
                    case "ui":
                        // UI scene: Canvas + EventSystem
                        CreateCamera("Main Camera", 60f);
                        var canvas = new GameObject("Canvas");
                        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                        
                        var eventSystem = new GameObject("EventSystem");
                        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                        break;
                    
                    default:
                        return $"❌ Unknown template type: {templateType}\nAvailable: basic, 3d, 2d, ui";
                }
                
                // Save scene
                EditorSceneManager.SaveScene(scene, scenePath);
                AssetDatabase.Refresh();
                
                Debug.Log($"[CreateSceneFromTemplate] Created {sceneName} from {templateType} template");
                
                return $"✅ Created scene '{sceneName}' from '{templateType}' template\n📍 Path: {scenePath}";
            }
            catch (Exception e)
            {
                return $"❌ Error creating scene from template: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get build settings information
        /// </summary>
        public static string GetBuildSettings()
        {
            try
            {
                var results = new StringBuilder();
                results.AppendLine("🎮 BUILD SETTINGS");
                results.AppendLine("═══════════════════");
                results.AppendLine();
                
                results.AppendLine($"**Active Build Target:** {EditorUserBuildSettings.activeBuildTarget}");
                results.AppendLine($"**Development Build:** {EditorUserBuildSettings.development}");
                results.AppendLine();
                
                var scenes = EditorBuildSettings.scenes;
                results.AppendLine($"**Scenes in Build:** {scenes.Length}");
                results.AppendLine();
                
                if (scenes.Length == 0)
                {
                    results.AppendLine("⚠️ No scenes added to build!");
                }
                else
                {
                    for (int i = 0; i < scenes.Length; i++)
                    {
                        var scene = scenes[i];
                        string enabledIcon = scene.enabled ? "✅" : "⬜";
                        results.AppendLine($"{i}. {enabledIcon} {scene.path}");
                    }
                }
                
                results.AppendLine();
                results.AppendLine("💡 **Tip:** Use add_scene_to_build to add scenes");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting build settings: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add scene to build settings
        /// </summary>
        public static string AddSceneToBuild(string sceneName, bool enabled = true)
        {
            try
            {
                // Find scene
                var sceneGuids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
                if (sceneGuids.Length == 0)
                    return $"❌ Scene '{sceneName}' not found";
                
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[0]);
                
                // Check if already in build
                var scenes = EditorBuildSettings.scenes.ToList();
                var existing = scenes.FirstOrDefault(s => s.path == scenePath);
                
                if (existing != null)
                {
                    existing.enabled = enabled;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return $"ℹ️ Scene '{sceneName}' already in build settings\n✅ Updated enabled state to: {enabled}";
                }
                
                // Add to build
                scenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
                EditorBuildSettings.scenes = scenes.ToArray();
                
                Debug.Log($"[AddSceneToBuild] Added {sceneName} to build settings");
                
                return $"✅ Added '{sceneName}' to build settings\n📍 Path: {scenePath}\n🎯 Position: {scenes.Count - 1}";
            }
            catch (Exception e)
            {
                return $"❌ Error adding scene to build: {e.Message}";
            }
        }
        
        /// <summary>
        /// Remove scene from build settings
        /// </summary>
        public static string RemoveSceneFromBuild(string sceneName)
        {
            try
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                var toRemove = scenes.FirstOrDefault(s => s.path.Contains(sceneName));
                
                if (toRemove == null)
                    return $"❌ Scene '{sceneName}' not found in build settings";
                
                scenes.Remove(toRemove);
                EditorBuildSettings.scenes = scenes.ToArray();
                
                Debug.Log($"[RemoveSceneFromBuild] Removed {sceneName} from build settings");
                
                return $"✅ Removed '{sceneName}' from build settings";
            }
            catch (Exception e)
            {
                return $"❌ Error removing scene from build: {e.Message}";
            }
        }
        
        /// <summary>
        /// Compare two scenes
        /// </summary>
        public static string CompareScenes(string sceneName1, string sceneName2)
        {
            try
            {
                // Find scenes
                var guid1 = AssetDatabase.FindAssets($"{sceneName1} t:Scene").FirstOrDefault();
                var guid2 = AssetDatabase.FindAssets($"{sceneName2} t:Scene").FirstOrDefault();
                
                if (string.IsNullOrEmpty(guid1))
                    return $"❌ Scene '{sceneName1}' not found";
                if (string.IsNullOrEmpty(guid2))
                    return $"❌ Scene '{sceneName2}' not found";
                
                string path1 = AssetDatabase.GUIDToAssetPath(guid1);
                string path2 = AssetDatabase.GUIDToAssetPath(guid2);
                
                // Load scenes (without changing current scene)
                var originalScene = EditorSceneManager.GetActiveScene();
                
                var scene1 = EditorSceneManager.OpenScene(path1, OpenSceneMode.Additive);
                var scene2 = EditorSceneManager.OpenScene(path2, OpenSceneMode.Additive);
                
                var results = new StringBuilder();
                results.AppendLine($"🔀 Scene Comparison");
                results.AppendLine("═══════════════════");
                results.AppendLine();
                
                results.AppendLine($"**Scene 1:** {sceneName1}");
                results.AppendLine($"  - Root Objects: {scene1.rootCount}");
                results.AppendLine($"  - Path: {path1}");
                results.AppendLine();
                
                results.AppendLine($"**Scene 2:** {sceneName2}");
                results.AppendLine($"  - Root Objects: {scene2.rootCount}");
                results.AppendLine($"  - Path: {path2}");
                results.AppendLine();
                
                // Get root objects
                var roots1 = scene1.GetRootGameObjects();
                var roots2 = scene2.GetRootGameObjects();
                
                var names1 = roots1.Select(go => go.name).ToHashSet();
                var names2 = roots2.Select(go => go.name).ToHashSet();
                
                // Objects only in scene 1
                var only1 = names1.Except(names2).ToList();
                if (only1.Any())
                {
                    results.AppendLine($"**Only in {sceneName1}:** ({only1.Count})");
                    foreach (var name in only1.Take(20))
                        results.AppendLine($"  - {name}");
                    if (only1.Count > 20)
                        results.AppendLine($"  ... and {only1.Count - 20} more");
                    results.AppendLine();
                }
                
                // Objects only in scene 2
                var only2 = names2.Except(names1).ToList();
                if (only2.Any())
                {
                    results.AppendLine($"**Only in {sceneName2}:** ({only2.Count})");
                    foreach (var name in only2.Take(20))
                        results.AppendLine($"  - {name}");
                    if (only2.Count > 20)
                        results.AppendLine($"  ... and {only2.Count - 20} more");
                    results.AppendLine();
                }
                
                // Common objects
                var common = names1.Intersect(names2).ToList();
                if (common.Any())
                {
                    results.AppendLine($"**Common Objects:** ({common.Count})");
                    foreach (var name in common.Take(20))
                        results.AppendLine($"  - {name}");
                    if (common.Count > 20)
                        results.AppendLine($"  ... and {common.Count - 20} more");
                }
                
                // Cleanup - close the loaded scenes
                EditorSceneManager.CloseScene(scene1, false);
                EditorSceneManager.CloseScene(scene2, false);
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error comparing scenes: {e.Message}";
            }
        }
        
        /// <summary>
        /// Merge two scenes
        /// </summary>
        public static string MergeScenes(string sourceScene, string targetScene)
        {
            try
            {
                // Find scenes
                var sourceGuid = AssetDatabase.FindAssets($"{sourceScene} t:Scene").FirstOrDefault();
                var targetGuid = AssetDatabase.FindAssets($"{targetScene} t:Scene").FirstOrDefault();
                
                if (string.IsNullOrEmpty(sourceGuid))
                    return $"❌ Source scene '{sourceScene}' not found";
                if (string.IsNullOrEmpty(targetGuid))
                    return $"❌ Target scene '{targetScene}' not found";
                
                string sourcePath = AssetDatabase.GUIDToAssetPath(sourceGuid);
                string targetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                
                // Load scenes
                var target = EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);
                var source = EditorSceneManager.OpenScene(sourcePath, OpenSceneMode.Additive);
                
                // Move all root objects from source to target
                var sourceRoots = source.GetRootGameObjects();
                int movedCount = 0;
                
                foreach (var go in sourceRoots)
                {
                    SceneManager.MoveGameObjectToScene(go, target);
                    movedCount++;
                }
                
                // Close source scene without saving
                EditorSceneManager.CloseScene(source, false);
                
                // Save target scene
                EditorSceneManager.SaveScene(target);
                
                Debug.Log($"[MergeScenes] Merged {sourceScene} into {targetScene}");
                
                return $"✅ Merged '{sourceScene}' into '{targetScene}'\n📦 Moved {movedCount} GameObject(s)\n💾 Target scene saved";
            }
            catch (Exception e)
            {
                return $"❌ Error merging scenes: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get scene statistics
        /// </summary>
        public static string GetSceneStats(string sceneName = "")
        {
            try
            {
                Scene scene;
                
                if (string.IsNullOrEmpty(sceneName))
                {
                    scene = SceneManager.GetActiveScene();
                    sceneName = scene.name;
                }
                else
                {
                    // Find and load scene
                    var guid = AssetDatabase.FindAssets($"{sceneName} t:Scene").FirstOrDefault();
                    if (string.IsNullOrEmpty(guid))
                        return $"❌ Scene '{sceneName}' not found";
                    
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
                
                var results = new StringBuilder();
                results.AppendLine($"📊 SCENE STATISTICS: {sceneName}");
                results.AppendLine("═══════════════════════════════");
                results.AppendLine();
                
                // Get all objects
                var allObjects = scene.GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.gameObject)
                    .ToList();
                
                results.AppendLine($"**Total GameObjects:** {allObjects.Count}");
                results.AppendLine($"**Root Objects:** {scene.rootCount}");
                results.AppendLine($"**Active Objects:** {allObjects.Count(go => go.activeInHierarchy)}");
                results.AppendLine($"**Inactive Objects:** {allObjects.Count(go => !go.activeInHierarchy)}");
                results.AppendLine();
                
                // Component statistics
                var componentCounts = new Dictionary<string, int>();
                foreach (var go in allObjects)
                {
                    var components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp == null) continue;
                        string typeName = comp.GetType().Name;
                        if (!componentCounts.ContainsKey(typeName))
                            componentCounts[typeName] = 0;
                        componentCounts[typeName]++;
                    }
                }
                
                results.AppendLine($"**Component Types:** {componentCounts.Count}");
                results.AppendLine();
                results.AppendLine("## Top Components:");
                foreach (var comp in componentCounts.OrderByDescending(c => c.Value).Take(10))
                {
                    results.AppendLine($"  - {comp.Key}: {comp.Value}");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting scene stats: {e.Message}";
            }
        }
    }
}

