using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICodeActions.Core
{
    /// <summary>
    /// Tools that AI Agent can use to interact with Unity
    /// </summary>
    public static class UnityAgentTools
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
        
        /// <summary>
        /// Create a new GameObject in the scene
        /// </summary>
        public static string CreateGameObject(string name, string parent = null)
        {
            try
            {
                GameObject go = new GameObject(name);
                
                if (!string.IsNullOrEmpty(parent))
                {
                    var parentObj = GameObject.Find(parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }
                
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                Selection.activeGameObject = go;
                
                return $"✅ Created GameObject: {name} (ID: {go.GetInstanceID()})";
            }
            catch (Exception e)
            {
                return $"❌ Error creating GameObject: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add a component to a GameObject
        /// </summary>
        public static string AddComponent(string gameObjectName, string componentType)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                // Try to find the type
                Type type = Type.GetType($"UnityEngine.{componentType}, UnityEngine");
                if (type == null)
                    type = Type.GetType(componentType);
                
                if (type == null)
                    return $"❌ Component type '{componentType}' not found";
                
                var component = Undo.AddComponent(go, type);
                return $"✅ Added {componentType} to {gameObjectName}";
            }
            catch (Exception e)
            {
                return $"❌ Error adding component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create and attach a script to a GameObject
        /// </summary>
        public static string CreateAndAttachScript(string gameObjectName, string scriptName, string scriptContent)
        {
            try
            {
                Debug.Log($"[CreateScript] Creating {scriptName}.cs for {gameObjectName}");
                Debug.Log($"[CreateScript] Content length: {scriptContent?.Length ?? 0}");
                Debug.Log($"[CreateScript] Content preview: {scriptContent?.Substring(0, Math.Min(200, scriptContent?.Length ?? 0))}...");
                
                // Validate content
                if (string.IsNullOrEmpty(scriptContent))
                {
                    return $"❌ Script content is empty!";
                }
                
                // Clean up script content (remove markdown if present)
                scriptContent = scriptContent.Trim();
                if (scriptContent.StartsWith("```csharp") || scriptContent.StartsWith("```c#"))
                {
                    int firstNewline = scriptContent.IndexOf('\n');
                    if (firstNewline > 0)
                        scriptContent = scriptContent.Substring(firstNewline + 1);
                }
                if (scriptContent.EndsWith("```"))
                {
                    int lastBacktick = scriptContent.LastIndexOf("```");
                    scriptContent = scriptContent.Substring(0, lastBacktick);
                }
                scriptContent = scriptContent.Trim();
                
                // Create the script file
                string path = $"Assets/{scriptName}.cs";
                System.IO.File.WriteAllText(path, scriptContent);
                Debug.Log($"[CreateScript] File written to: {path}");
                
                AssetDatabase.Refresh();
                
                // Wait for compilation using EditorApplication.update
                double startTime = EditorApplication.timeSinceStartup;
                int maxWaitTime = 15; // Maximum 15 seconds
                int checkInterval = 0;
                
                void CheckAndAttach()
                {
                    checkInterval++;
                    double elapsed = EditorApplication.timeSinceStartup - startTime;
                    
                    // Only check every 10 frames (roughly every 0.3 seconds at 60fps)
                    if (checkInterval % 10 != 0)
                        return;
                    
                    // Timeout after max wait time
                    if (elapsed > maxWaitTime)
                    {
                        EditorApplication.update -= CheckAndAttach;
                        Debug.LogError($"❌ Timeout: Could not attach {scriptName} after {maxWaitTime}s. Script may have compilation errors.");
                        Debug.LogError($"💡 Check Console for compilation errors or manually attach the script to {gameObjectName}");
                        return;
                    }
                    
                    // Try to find GameObject
                    var go = GameObject.Find(gameObjectName);
                    if (go == null)
                    {
                        // GameObject might not exist yet or wrong name
                        if (elapsed > 2) // Only log after 2 seconds
                        {
                            Debug.LogWarning($"[{elapsed:F1}s] GameObject '{gameObjectName}' not found. Waiting...");
                        }
                        return;
                    }
                    
                    // Try to load script
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script == null)
                    {
                        Debug.LogWarning($"[{elapsed:F1}s] Script asset not loaded yet. Waiting...");
                        return;
                    }
                    
                    // Try to get compiled class
                    var scriptClass = script.GetClass();
                    if (scriptClass != null)
                    {
                        // Success! Remove update callback
                        EditorApplication.update -= CheckAndAttach;
                        
                        // Check if component already exists
                        if (go.GetComponent(scriptClass) == null)
                        {
                            Undo.AddComponent(go, scriptClass);
                            Debug.Log($"✅ Successfully attached {scriptName} to {gameObjectName} (after {elapsed:F1}s)");
                            
                            // Select the GameObject to show it worked
                            Selection.activeGameObject = go;
                        }
                        else
                        {
                            Debug.Log($"ℹ️ {scriptName} already attached to {gameObjectName}");
                        }
                    }
                    else
                    {
                        // Still compiling
                        if (checkInterval % 30 == 0) // Log every 30 checks
                        {
                            Debug.Log($"[{elapsed:F1}s] Waiting for {scriptName} to compile...");
                        }
                    }
                }
                
                // Register update callback
                EditorApplication.update += CheckAndAttach;
                
                return $"✅ Created script {scriptName}.cs ({scriptContent.Length} chars)\n⏳ Attaching to {gameObjectName} after compilation...";
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateScript] Error: {e}");
                return $"❌ Error creating script: {e.Message}\n{e.StackTrace}";
            }
        }
        
        /// <summary>
        /// Find GameObjects by name or tag
        /// </summary>
        public static string FindGameObjects(string searchTerm, bool byTag = false)
        {
            try
            {
                GameObject[] objects;
                
                if (byTag)
                {
                    objects = GameObject.FindGameObjectsWithTag(searchTerm);
                }
                else
                {
                    objects = Resources.FindObjectsOfTypeAll<GameObject>()
                        .Where(go => go.name.Contains(searchTerm) && go.scene.isLoaded)
                        .ToArray();
                }
                
                if (objects.Length == 0)
                    return $"No GameObjects found matching '{searchTerm}'";
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"Found {objects.Length} GameObject(s):");
                
                foreach (var go in objects.Take(20)) // Limit to 20
                {
                    result.AppendLine($"- {go.name} (Path: {GetGameObjectPath(go)})");
                    
                    var components = go.GetComponents<Component>();
                    foreach (var comp in components)
                    {
                        if (comp != null && !(comp is Transform))
                        {
                            result.AppendLine($"    [{comp.GetType().Name}]");
                        }
                    }
                }
                
                if (objects.Length > 20)
                    result.AppendLine($"... and {objects.Length - 20} more");
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding GameObjects: {e.Message}";
            }
        }
        
        private static string GetGameObjectPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        /// <summary>
        /// Get information about a specific GameObject
        /// </summary>
        public static string GetGameObjectInfo(string name)
        {
            try
            {
                var go = GameObject.Find(name);
                if (go == null)
                    return $"❌ GameObject '{name}' not found";
                
                var info = new System.Text.StringBuilder();
                info.AppendLine($"# GameObject: {go.name}");
                info.AppendLine($"Active: {go.activeSelf}");
                info.AppendLine($"Tag: {go.tag}");
                info.AppendLine($"Layer: {LayerMask.LayerToName(go.layer)}");
                info.AppendLine($"Position: {go.transform.position}");
                info.AppendLine($"Rotation: {go.transform.rotation.eulerAngles}");
                info.AppendLine($"Scale: {go.transform.localScale}");
                info.AppendLine();
                
                info.AppendLine("## Components:");
                var components = go.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        info.AppendLine($"- {comp.GetType().Name}");
                        
                        // Get serialized properties
                        var so = new SerializedObject(comp);
                        var prop = so.GetIterator();
                        prop.NextVisible(true);
                        
                        int count = 0;
                        while (prop.NextVisible(false) && count < 10) // Limit to 10 properties
                        {
                            info.AppendLine($"    {prop.name}: {prop.propertyType}");
                            count++;
                        }
                    }
                }
                
                if (go.transform.childCount > 0)
                {
                    info.AppendLine();
                    info.AppendLine($"## Children ({go.transform.childCount}):");
                    for (int i = 0; i < Math.Min(go.transform.childCount, 10); i++)
                    {
                        info.AppendLine($"- {go.transform.GetChild(i).name}");
                    }
                }
                
                return info.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting GameObject info: {e.Message}";
            }
        }
        
        /// <summary>
        /// List all scripts in the project
        /// </summary>
        public static string ListScripts(string filter = "")
        {
            try
            {
                var scripts = AssetDatabase.FindAssets("t:MonoScript")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path.EndsWith(".cs"))
                    .Where(path => string.IsNullOrEmpty(filter) || path.Contains(filter))
                    .Take(50)
                    .ToList();
                
                if (scripts.Count == 0)
                    return "No scripts found";
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"Found {scripts.Count} script(s):");
                
                foreach (var script in scripts)
                {
                    result.AppendLine($"- {script}");
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error listing scripts: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get project statistics
        /// </summary>
        public static string GetProjectStats()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("# Project Statistics");
            info.AppendLine();
            
            // Scenes
            var scenes = EditorBuildSettings.scenes;
            info.AppendLine($"Build Scenes: {scenes.Length}");
            info.AppendLine($"Current Scene: {SceneManager.GetActiveScene().name}");
            info.AppendLine();
            
            // Assets
            var scripts = AssetDatabase.FindAssets("t:MonoScript").Length;
            var prefabs = AssetDatabase.FindAssets("t:Prefab").Length;
            var materials = AssetDatabase.FindAssets("t:Material").Length;
            var textures = AssetDatabase.FindAssets("t:Texture").Length;
            
            info.AppendLine($"Scripts: {scripts}");
            info.AppendLine($"Prefabs: {prefabs}");
            info.AppendLine($"Materials: {materials}");
            info.AppendLine($"Textures: {textures}");
            info.AppendLine();
            
            // Current scene objects
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();
            int totalObjects = 0;
            foreach (var root in rootObjects)
            {
                totalObjects += root.GetComponentsInChildren<Transform>(true).Length;
            }
            
            info.AppendLine($"GameObjects in Scene: {totalObjects}");
            info.AppendLine($"Root GameObjects: {rootObjects.Length}");
            
            return info.ToString();
        }
    }
}

