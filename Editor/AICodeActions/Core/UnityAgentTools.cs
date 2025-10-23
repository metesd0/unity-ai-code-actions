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
        /// Read a C# script file content
        /// </summary>
        public static string ReadScript(string scriptName)
        {
            try
            {
                // Find the script by name
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found in project";
                
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    
                    if (script != null && (script.name == scriptName || script.name + ".cs" == scriptName))
                    {
                        string content = System.IO.File.ReadAllText(path);
                        return $"📄 File: {script.name}.cs\n📍 Path: {path}\n\n```csharp\n{content}\n```";
                    }
                }
                
                return $"❌ Script '{scriptName}' not found";
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReadScript] Error: {e}");
                return $"❌ Error reading script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Read any file content from Assets
        /// </summary>
        public static string ReadFile(string filePath)
        {
            try
            {
                // If just filename, search for it
                if (!filePath.Contains("/") && !filePath.Contains("\\"))
                {
                    var guids = AssetDatabase.FindAssets(System.IO.Path.GetFileNameWithoutExtension(filePath));
                    if (guids.Length > 0)
                    {
                        filePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    }
                }
                
                // Ensure it's in Assets folder
                if (!filePath.StartsWith("Assets/"))
                    filePath = "Assets/" + filePath;
                
                if (!System.IO.File.Exists(filePath))
                    return $"❌ File not found: {filePath}";
                
                string content = System.IO.File.ReadAllText(filePath);
                string extension = System.IO.Path.GetExtension(filePath);
                string fileName = System.IO.Path.GetFileName(filePath);
                
                return $"📄 File: {fileName}\n📍 Path: {filePath}\n\n```{extension.TrimStart('.')}\n{content}\n```";
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReadFile] Error: {e}");
                return $"❌ Error reading file: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get detailed GameObject information
        /// </summary>
        public static string GetGameObjectInfo(string gameObjectName)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found in scene";
                
                var info = new System.Text.StringBuilder();
                info.AppendLine($"# GameObject: {go.name}");
                info.AppendLine($"**Active:** {go.activeSelf}");
                info.AppendLine($"**Tag:** {go.tag}");
                info.AppendLine($"**Layer:** {LayerMask.LayerToName(go.layer)}");
                info.AppendLine($"**Position:** {go.transform.position}");
                info.AppendLine($"**Rotation:** {go.transform.rotation.eulerAngles}");
                info.AppendLine($"**Scale:** {go.transform.localScale}");
                info.AppendLine();
                
                var components = go.GetComponents<Component>();
                info.AppendLine($"## Components ({components.Length}):");
                
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    
                    info.AppendLine($"- **{comp.GetType().Name}**");
                    
                    // If it's a MonoBehaviour, show the script
                    if (comp is MonoBehaviour mb)
                    {
                        var mbScript = MonoScript.FromMonoBehaviour(mb);
                        if (mbScript != null)
                        {
                            string mbPath = AssetDatabase.GetAssetPath(mbScript);
                            info.AppendLine($"  📄 Script: {mbScript.name}.cs");
                            info.AppendLine($"  📍 Path: {mbPath}");
                        }
                    }
                }
                
                // Show children
                if (go.transform.childCount > 0)
                {
                    info.AppendLine();
                    info.AppendLine($"## Children ({go.transform.childCount}):");
                    for (int i = 0; i < go.transform.childCount; i++)
                    {
                        var child = go.transform.GetChild(i);
                        info.AppendLine($"- {child.name}");
                    }
                }
                
                return info.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetGameObjectInfo] Error: {e}");
                return $"❌ Error getting GameObject info: {e.Message}";
            }
        }
        
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
        /// Add a component to a GameObject (built-in Unity components or compiled custom scripts)
        /// </summary>
        public static string AddComponent(string gameObjectName, string componentType)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Type type = null;
                
                // 1. Try built-in Unity components (UnityEngine namespace)
                type = Type.GetType($"UnityEngine.{componentType}, UnityEngine");
                
                // 2. Try UnityEngine.UI namespace (for UI components)
                if (type == null)
                    type = Type.GetType($"UnityEngine.UI.{componentType}, UnityEngine.UI");
                
                // 3. Try custom scripts in current assembly
                if (type == null)
                    type = Type.GetType(componentType);
                
                // 4. Search all assemblies for custom scripts
                if (type == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(componentType);
                        if (type != null)
                            break;
                    }
                }
                
                // 5. Try to find MonoScript asset (for user scripts)
                if (type == null)
                {
                    var scriptGuids = AssetDatabase.FindAssets($"{componentType} t:MonoScript");
                    foreach (var guid in scriptGuids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        if (script != null && script.name == componentType)
                        {
                            type = script.GetClass();
                            if (type != null)
                                break;
                        }
                    }
                }
                
                if (type == null)
                {
                    return $"❌ Component type '{componentType}' not found\n" +
                           $"💡 Tips:\n" +
                           $"   - For built-in: Use exact name (e.g., 'Rigidbody', 'CharacterController')\n" +
                           $"   - For custom scripts: Make sure the script is compiled and class name matches\n" +
                           $"   - To create AND attach a new script, use 'create_and_attach_script' tool instead";
                }
                
                // Check if component already exists
                if (go.GetComponent(type) != null)
                {
                    return $"ℹ️ Component '{componentType}' already exists on {gameObjectName}";
                }
                
                var component = Undo.AddComponent(go, type);
                Debug.Log($"[AddComponent] Successfully added {componentType} to {gameObjectName}");
                
                // Select the GameObject to show the result
                Selection.activeGameObject = go;
                
                return $"✅ Added {componentType} to {gameObjectName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddComponent] Error: {e}");
                return $"❌ Error adding component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Attach an existing script to a GameObject
        /// </summary>
        public static string AttachScript(string gameObjectName, string scriptName)
        {
            try
            {
                // Find the GameObject
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Debug.Log($"[AttachScript] Attaching {scriptName} to {gameObjectName}");
                
                // Remove .cs extension if present
                if (scriptName.EndsWith(".cs"))
                    scriptName = scriptName.Substring(0, scriptName.Length - 3);
                
                // Find the script
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found in project. Use 'create_and_attach_script' to create a new script.";
                
                MonoScript targetScript = null;
                string scriptPath = null;
                
                foreach (var guid in scriptGuids)
                {
                    scriptPath = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    
                    if (script != null && script.name == scriptName)
                    {
                        targetScript = script;
                        break;
                    }
                }
                
                if (targetScript == null)
                    return $"❌ Script '{scriptName}' not found";
                
                // Get the script's class
                var scriptClass = targetScript.GetClass();
                
                if (scriptClass == null)
                    return $"❌ Script '{scriptName}' class not found. Make sure the script is compiled and the class name matches the file name.";
                
                // Check if already attached
                if (go.GetComponent(scriptClass) != null)
                    return $"ℹ️ Script '{scriptName}' is already attached to {gameObjectName}";
                
                // Attach the script
                Undo.AddComponent(go, scriptClass);
                
                // Select the GameObject
                Selection.activeGameObject = go;
                
                Debug.Log($"✅ Successfully attached {scriptName} to {gameObjectName}");
                
                return $"✅ Attached {scriptName}.cs to {gameObjectName}\n📍 Script path: {scriptPath}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[AttachScript] Error: {e}");
                return $"❌ Error attaching script: {e.Message}";
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

