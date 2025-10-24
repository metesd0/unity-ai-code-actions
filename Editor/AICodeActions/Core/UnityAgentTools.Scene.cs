using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICodeActions.Core
{
    /// <summary>
    /// Scene Management Operations: Scene info, save, project stats
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Get current scene information
        /// </summary>
        public static string GetSceneInfo()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            
            var info = new System.Text.StringBuilder();
            info.AppendLine($"# Scene: {scene.name}");
            info.AppendLine($"Path: {scene.path}");
            info.AppendLine($"Root GameObjects: {rootObjects.Length}");
            info.AppendLine();
            
            info.AppendLine("## Hierarchy:");
            foreach (var root in rootObjects)
            {
                AppendGameObjectTree(root, info, 0);
            }
            
            return info.ToString();
        }
        
        /// <summary>
        /// Save the current scene
        /// </summary>
        public static string SaveScene()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                
                if (string.IsNullOrEmpty(scene.path))
                {
                    return $"❌ Scene has never been saved. Use 'Save As' from Unity menu first to set a path.";
                }
                
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                
                if (saved)
                {
                    Debug.Log($"[SaveScene] Saved scene: {scene.name}");
                    return $"✅ Saved scene: {scene.name} ({scene.path})";
                }
                else
                {
                    return $"❌ Failed to save scene: {scene.name}";
                }
            }
            catch (Exception e)
            {
                return $"❌ Error saving scene: {e.Message}";
            }
        }
        
        /// <summary>
        /// Save scene with a new path
        /// </summary>
        public static string SaveSceneAs(string sceneName)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                string path = $"Assets/{sceneName}.unity";
                
                bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                
                if (saved)
                {
                    Debug.Log($"[SaveSceneAs] Saved scene as: {path}");
                    return $"✅ Saved scene as: {path}";
                }
                else
                {
                    return $"❌ Failed to save scene as: {path}";
                }
            }
            catch (Exception e)
            {
                return $"❌ Error saving scene: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get project statistics
        /// </summary>
        public static string GetProjectStats()
        {
            try
            {
                var stats = new System.Text.StringBuilder();
                stats.AppendLine("# Project Statistics");
                stats.AppendLine();
                
                // Count scripts
                var scripts = AssetDatabase.FindAssets("t:MonoScript");
                stats.AppendLine($"📄 Scripts: {scripts.Length}");
                
                // Count scenes
                var scenes = AssetDatabase.FindAssets("t:Scene");
                stats.AppendLine($"🎬 Scenes: {scenes.Length}");
                
                // Count prefabs
                var prefabs = AssetDatabase.FindAssets("t:Prefab");
                stats.AppendLine($"📦 Prefabs: {prefabs.Length}");
                
                // Count materials
                var materials = AssetDatabase.FindAssets("t:Material");
                stats.AppendLine($"🎨 Materials: {materials.Length}");
                
                // Count textures
                var textures = AssetDatabase.FindAssets("t:Texture");
                stats.AppendLine($"🖼️ Textures: {textures.Length}");
                
                // Current scene info
                var scene = SceneManager.GetActiveScene();
                stats.AppendLine();
                stats.AppendLine($"## Current Scene: {scene.name}");
                stats.AppendLine($"- GameObjects: {scene.rootCount}");
                stats.AppendLine($"- Path: {scene.path}");
                
                return stats.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting project stats: {e.Message}";
            }
        }
        
        // Helper method (defined here to avoid duplication from Core.cs)
        private static void AppendGameObjectTree(GameObject go, System.Text.StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}- {go.name}");
            
            // Add components
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null && !(comp is Transform))
                {
                    sb.AppendLine($"{indent}  [{comp.GetType().Name}]");
                }
            }
            
            // Recurse children
            for (int i = 0; i < go.transform.childCount; i++)
            {
                AppendGameObjectTree(go.transform.GetChild(i).gameObject, sb, depth + 1);
            }
        }
    }
}

