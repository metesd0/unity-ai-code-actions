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
        
        // ==================== GAMEOBJECT MANIPULATION ====================
        
        /// <summary>
        /// Set GameObject position
        /// </summary>
        public static string SetPosition(string gameObjectName, float x, float y, float z)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Undo.RecordObject(go.transform, "Set Position");
                go.transform.position = new Vector3(x, y, z);
                
                Debug.Log($"[SetPosition] {gameObjectName} moved to ({x}, {y}, {z})");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                return $"✅ Set {gameObjectName} position to ({x}, {y}, {z})";
            }
            catch (Exception e)
            {
                return $"❌ Error setting position: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set GameObject rotation (Euler angles)
        /// </summary>
        public static string SetRotation(string gameObjectName, float x, float y, float z)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Undo.RecordObject(go.transform, "Set Rotation");
                go.transform.rotation = Quaternion.Euler(x, y, z);
                
                Debug.Log($"[SetRotation] {gameObjectName} rotated to ({x}, {y}, {z})");
                Selection.activeGameObject = go;
                
                return $"✅ Set {gameObjectName} rotation to ({x}°, {y}°, {z}°)";
            }
            catch (Exception e)
            {
                return $"❌ Error setting rotation: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set GameObject scale
        /// </summary>
        public static string SetScale(string gameObjectName, float x, float y, float z)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Undo.RecordObject(go.transform, "Set Scale");
                go.transform.localScale = new Vector3(x, y, z);
                
                Debug.Log($"[SetScale] {gameObjectName} scaled to ({x}, {y}, {z})");
                Selection.activeGameObject = go;
                
                return $"✅ Set {gameObjectName} scale to ({x}, {y}, {z})";
            }
            catch (Exception e)
            {
                return $"❌ Error setting scale: {e.Message}";
            }
        }
        
        /// <summary>
        /// Delete a GameObject
        /// </summary>
        public static string DeleteGameObject(string gameObjectName)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                string info = $"{gameObjectName} (with {go.GetComponents<Component>().Length} components)";
                Undo.DestroyObjectImmediate(go);
                
                Debug.Log($"[DeleteGameObject] Deleted {info}");
                
                return $"✅ Deleted {info}";
            }
            catch (Exception e)
            {
                return $"❌ Error deleting GameObject: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set parent of a GameObject
        /// </summary>
        public static string SetParent(string childName, string parentName)
        {
            try
            {
                var child = GameObject.Find(childName);
                if (child == null)
                    return $"❌ GameObject '{childName}' not found";
                
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentName) && parentName.ToLower() != "null" && parentName.ToLower() != "none")
                {
                    parent = GameObject.Find(parentName);
                    if (parent == null)
                        return $"❌ Parent GameObject '{parentName}' not found";
                }
                
                Undo.SetTransformParent(child.transform, parent?.transform, "Set Parent");
                
                string result = parent == null 
                    ? $"✅ Moved {childName} to root (no parent)"
                    : $"✅ Set {childName} parent to {parentName}";
                
                Debug.Log($"[SetParent] {result}");
                Selection.activeGameObject = child;
                
                return result;
            }
            catch (Exception e)
            {
                return $"❌ Error setting parent: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set GameObject active state
        /// </summary>
        public static string SetActive(string gameObjectName, bool active)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                Undo.RecordObject(go, "Set Active");
                go.SetActive(active);
                
                Debug.Log($"[SetActive] {gameObjectName} set to {(active ? "active" : "inactive")}");
                
                return $"✅ {gameObjectName} is now {(active ? "active" : "inactive")}";
            }
            catch (Exception e)
            {
                return $"❌ Error setting active: {e.Message}";
            }
        }
        
        /// <summary>
        /// Rename a GameObject
        /// </summary>
        public static string RenameGameObject(string oldName, string newName)
        {
            try
            {
                var go = GameObject.Find(oldName);
                if (go == null)
                    return $"❌ GameObject '{oldName}' not found";
                
                Undo.RecordObject(go, "Rename GameObject");
                go.name = newName;
                
                Debug.Log($"[RenameGameObject] Renamed '{oldName}' to '{newName}'");
                Selection.activeGameObject = go;
                
                return $"✅ Renamed '{oldName}' to '{newName}'";
            }
            catch (Exception e)
            {
                return $"❌ Error renaming GameObject: {e.Message}";
            }
        }
        
        /// <summary>
        /// Duplicate a GameObject
        /// </summary>
        public static string DuplicateGameObject(string name, string newName = null)
        {
            try
            {
                var original = GameObject.Find(name);
                if (original == null)
                    return $"❌ GameObject '{name}' not found";
                
                var duplicate = GameObject.Instantiate(original);
                duplicate.name = string.IsNullOrEmpty(newName) ? name + " (Clone)" : newName;
                
                Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate GameObject");
                Selection.activeGameObject = duplicate;
                EditorGUIUtility.PingObject(duplicate);
                
                Debug.Log($"[DuplicateGameObject] Duplicated '{name}' as '{duplicate.name}'");
                
                return $"✅ Duplicated '{name}' as '{duplicate.name}'";
            }
            catch (Exception e)
            {
                return $"❌ Error duplicating GameObject: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set GameObject tag
        /// </summary>
        public static string SetTag(string gameObjectName, string tag)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                // Check if tag exists
                try
                {
                    go.tag = tag;
                }
                catch (UnityException)
                {
                    return $"❌ Tag '{tag}' does not exist. Create it in Tags & Layers first.";
                }
                
                Undo.RecordObject(go, "Set Tag");
                
                Debug.Log($"[SetTag] {gameObjectName} tagged as '{tag}'");
                Selection.activeGameObject = go;
                
                return $"✅ {gameObjectName} tagged as '{tag}'";
            }
            catch (Exception e)
            {
                return $"❌ Error setting tag: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set GameObject layer
        /// </summary>
        public static string SetLayer(string gameObjectName, string layerName)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                int layer = LayerMask.NameToLayer(layerName);
                if (layer == -1)
                    return $"❌ Layer '{layerName}' does not exist. Valid layers: Default, TransparentFX, Ignore Raycast, Water, UI";
                
                Undo.RecordObject(go, "Set Layer");
                go.layer = layer;
                
                Debug.Log($"[SetLayer] {gameObjectName} set to layer '{layerName}'");
                Selection.activeGameObject = go;
                
                return $"✅ {gameObjectName} set to layer '{layerName}'";
            }
            catch (Exception e)
            {
                return $"❌ Error setting layer: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create a primitive GameObject (Cube, Sphere, Capsule, Cylinder, Plane, Quad)
        /// </summary>
        public static string CreatePrimitive(string primitiveType, string name = null, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                PrimitiveType type;
                if (!System.Enum.TryParse(primitiveType, true, out type))
                {
                    return $"❌ Invalid primitive type '{primitiveType}'. Valid types: Cube, Sphere, Capsule, Cylinder, Plane, Quad";
                }
                
                var go = GameObject.CreatePrimitive(type);
                
                if (!string.IsNullOrEmpty(name))
                    go.name = name;
                else
                    go.name = primitiveType;
                
                go.transform.position = new Vector3(x, y, z);
                
                Undo.RegisterCreatedObjectUndo(go, "Create Primitive");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreatePrimitive] Created {primitiveType} '{go.name}' at ({x}, {y}, {z})");
                
                return $"✅ Created {primitiveType} '{go.name}' at ({x}, {y}, {z})";
            }
            catch (Exception e)
            {
                return $"❌ Error creating primitive: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set a property value on a component
        /// </summary>
        public static string SetComponentProperty(string gameObjectName, string componentType, string propertyName, string value)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                // Find component
                Component component = null;
                var components = go.GetComponents<Component>();
                
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name == componentType)
                    {
                        component = comp;
                        break;
                    }
                }
                
                if (component == null)
                    return $"❌ Component '{componentType}' not found on {gameObjectName}";
                
                // Find property using reflection
                var type = component.GetType();
                var field = type.GetField(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (field == null && property == null)
                    return $"❌ Property '{propertyName}' not found on {componentType}";
                
                var fieldOrPropertyType = field != null ? field.FieldType : property.PropertyType;
                
                // Record undo
                Undo.RecordObject(component, $"Set {propertyName}");
                
                // Convert and set value based on type
                object convertedValue = null;
                
                if (fieldOrPropertyType == typeof(Transform) || fieldOrPropertyType == typeof(GameObject))
                {
                    // GameObject or Transform reference
                    var targetGo = GameObject.Find(value);
                    if (targetGo == null)
                        return $"❌ Referenced GameObject '{value}' not found";
                    
                    convertedValue = fieldOrPropertyType == typeof(Transform) ? (object)targetGo.transform : (object)targetGo;
                }
                else if (fieldOrPropertyType == typeof(float))
                {
                    convertedValue = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (fieldOrPropertyType == typeof(int))
                {
                    convertedValue = int.Parse(value);
                }
                else if (fieldOrPropertyType == typeof(bool))
                {
                    convertedValue = bool.Parse(value);
                }
                else if (fieldOrPropertyType == typeof(string))
                {
                    convertedValue = value;
                }
                else if (fieldOrPropertyType == typeof(Vector3))
                {
                    // Parse Vector3 from format: "x,y,z" or "(x,y,z)"
                    string cleanValue = value.Replace("(", "").Replace(")", "").Trim();
                    var parts = cleanValue.Split(',');
                    if (parts.Length == 3)
                    {
                        convertedValue = new Vector3(
                            float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture)
                        );
                    }
                    else
                    {
                        return $"❌ Invalid Vector3 format. Use: 'x,y,z' or '(x,y,z)'";
                    }
                }
                else if (fieldOrPropertyType == typeof(Color))
                {
                    // Parse Color from name or hex
                    if (value.StartsWith("#"))
                    {
                        ColorUtility.TryParseHtmlString(value, out Color color);
                        convertedValue = color;
                    }
                    else
                    {
                        // Try to parse color name
                        switch (value.ToLower())
                        {
                            case "red": convertedValue = Color.red; break;
                            case "green": convertedValue = Color.green; break;
                            case "blue": convertedValue = Color.blue; break;
                            case "white": convertedValue = Color.white; break;
                            case "black": convertedValue = Color.black; break;
                            case "yellow": convertedValue = Color.yellow; break;
                            case "cyan": convertedValue = Color.cyan; break;
                            case "magenta": convertedValue = Color.magenta; break;
                            case "gray": case "grey": convertedValue = Color.gray; break;
                            default: return $"❌ Unknown color '{value}'. Use: red, green, blue, white, black, yellow, cyan, magenta, gray, or #RRGGBB";
                        }
                    }
                }
                else
                {
                    return $"❌ Unsupported property type: {fieldOrPropertyType.Name}. Supported: Transform, GameObject, float, int, bool, string, Vector3, Color";
                }
                
                // Set the value
                if (field != null)
                {
                    field.SetValue(component, convertedValue);
                }
                else
                {
                    property.SetValue(component, convertedValue);
                }
                
                EditorUtility.SetDirty(component);
                Selection.activeGameObject = go;
                
                Debug.Log($"[SetComponentProperty] Set {componentType}.{propertyName} = {value} on {gameObjectName}");
                
                return $"✅ Set {componentType}.{propertyName} = {value} on {gameObjectName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[SetComponentProperty] Error: {e}");
                return $"❌ Error setting property: {e.Message}";
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
                
                // Wait for compilation using delayCall (more reliable than update)
                double startTime = EditorApplication.timeSinceStartup;
                int maxWaitTime = 20; // Maximum 20 seconds
                int attemptCount = 0;
                
                void CheckAndAttach()
                {
                    attemptCount++;
                    double elapsed = EditorApplication.timeSinceStartup - startTime;
                    
                    Debug.Log($"[Attach] Attempt #{attemptCount} for {scriptName} (elapsed: {elapsed:F1}s)");
                    
                    // Timeout after max wait time
                    if (elapsed > maxWaitTime)
                    {
                        Debug.LogError($"❌ Timeout: Could not attach {scriptName} after {maxWaitTime}s");
                        Debug.LogError($"💡 Possible issues:\n" +
                            $"   - Script has compilation errors (check Console)\n" +
                            $"   - Class name doesn't match file name '{scriptName}'\n" +
                            $"   - Script is not inheriting from MonoBehaviour\n" +
                            $"💡 Try: Drag {scriptName}.cs to '{gameObjectName}' in Inspector");
                        return;
                    }
                    
                    // Try to find GameObject
                    var go = GameObject.Find(gameObjectName);
                    if (go == null)
                    {
                        Debug.LogWarning($"[Attach] GameObject '{gameObjectName}' not found, retrying...");
                        EditorApplication.delayCall += CheckAndAttach;
                        return;
                    }
                    
                    // Force refresh
                    AssetDatabase.Refresh();
                    
                    // Try to load script
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script == null)
                    {
                        Debug.LogWarning($"[Attach] Script asset '{path}' not loaded yet, retrying...");
                        EditorApplication.delayCall += CheckAndAttach;
                        return;
                    }
                    
                    // Try to get compiled class
                    var scriptClass = script.GetClass();
                    if (scriptClass != null)
                    {
                        // Success! Attach it
                        if (go.GetComponent(scriptClass) == null)
                        {
                            Undo.AddComponent(go, scriptClass);
                            Debug.Log($"✅✅✅ ATTACHED {scriptName} to {gameObjectName}! (took {elapsed:F1}s, {attemptCount} attempts)");
                            
                            // Select GameObject and ping it
                            Selection.activeGameObject = go;
                            EditorGUIUtility.PingObject(go);
                        }
                        else
                        {
                            Debug.Log($"ℹ️ {scriptName} already attached to {gameObjectName}");
                        }
                    }
                    else
                    {
                        // Not compiled yet, retry
                        Debug.LogWarning($"[Attach] {scriptName} not compiled yet, retrying...");
                        EditorApplication.delayCall += CheckAndAttach;
                    }
                }
                
                // Start checking with delayCall
                EditorApplication.delayCall += CheckAndAttach;
                
                return $"✅ Created script {scriptName}.cs ({scriptContent.Length} chars)\n⏳ Attaching to {gameObjectName} after compilation...";
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateScript] Error: {e}");
                return $"❌ Error creating script: {e.Message}\n{e.StackTrace}";
            }
        }
        
        // ==================== ADVANCED SCRIPT MANIPULATION ====================
        
        /// <summary>
        /// Modify an existing script by adding/replacing code
        /// </summary>
        public static string ModifyScript(string scriptName, string modifications)
        {
            try
            {
                // Find the script file
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
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                string newContent = currentContent + "\n" + modifications;
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ModifyScript] Modified {scriptName}.cs");
                
                return $"✅ Modified {scriptName}.cs\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error modifying script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add a method to an existing script
        /// </summary>
        public static string AddMethodToScript(string scriptName, string methodCode)
        {
            try
            {
                // Find the script file
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
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                
                // Find the last closing brace of the class
                int lastBrace = currentContent.LastIndexOf('}');
                if (lastBrace == -1)
                    return $"❌ Invalid script structure - no closing brace found";
                
                // Clean method code
                methodCode = methodCode.Trim();
                if (methodCode.StartsWith("```csharp") || methodCode.StartsWith("```c#"))
                {
                    int firstNewline = methodCode.IndexOf('\n');
                    if (firstNewline > 0)
                        methodCode = methodCode.Substring(firstNewline + 1);
                }
                if (methodCode.EndsWith("```"))
                {
                    int lastBacktick = methodCode.LastIndexOf("```");
                    methodCode = methodCode.Substring(0, lastBacktick);
                }
                methodCode = methodCode.Trim();
                
                // Insert method before the last closing brace
                string indent = "    "; // 4 spaces
                string formattedMethod = "\n" + indent + methodCode.Replace("\n", "\n" + indent) + "\n";
                string newContent = currentContent.Insert(lastBrace, formattedMethod);
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddMethodToScript] Added method to {scriptName}.cs");
                
                return $"✅ Added method to {scriptName}.cs\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error adding method: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add a field/property to an existing script
        /// </summary>
        public static string AddFieldToScript(string scriptName, string fieldCode)
        {
            try
            {
                // Find the script file
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
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                
                // Find the class declaration
                int classIndex = currentContent.IndexOf("class " + scriptName);
                if (classIndex == -1)
                    return $"❌ Class '{scriptName}' not found in script";
                
                // Find the opening brace after class declaration
                int openBrace = currentContent.IndexOf('{', classIndex);
                if (openBrace == -1)
                    return $"❌ Invalid script structure";
                
                // Clean field code
                fieldCode = fieldCode.Trim();
                
                // Insert field after the opening brace
                string indent = "    ";
                string formattedField = "\n" + indent + fieldCode + "\n";
                string newContent = currentContent.Insert(openBrace + 1, formattedField);
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddFieldToScript] Added field to {scriptName}.cs");
                
                return $"✅ Added field to {scriptName}.cs\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error adding field: {e.Message}";
            }
        }
        
        /// <summary>
        /// Delete a script file
        /// </summary>
        public static string DeleteScript(string scriptName)
        {
            try
            {
                // Remove .cs extension if present
                if (scriptName.EndsWith(".cs"))
                    scriptName = scriptName.Substring(0, scriptName.Length - 3);
                
                // Find the script file
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
                
                // Delete the script
                bool deleted = AssetDatabase.DeleteAsset(scriptPath);
                AssetDatabase.Refresh();
                
                if (deleted)
                {
                    Debug.Log($"[DeleteScript] Deleted {scriptName}.cs");
                    return $"✅ Deleted {scriptName}.cs";
                }
                else
                {
                    return $"❌ Failed to delete {scriptName}.cs";
                }
            }
            catch (Exception e)
            {
                return $"❌ Error deleting script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find text in a script
        /// </summary>
        public static string FindInScript(string scriptName, string searchText)
        {
            try
            {
                // Find the script file
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
                
                // Read content
                string content = System.IO.File.ReadAllText(scriptPath);
                string[] lines = content.Split('\n');
                
                var results = new System.Text.StringBuilder();
                results.AppendLine($"🔍 Search results for '{searchText}' in {scriptName}.cs:");
                results.AppendLine();
                
                int foundCount = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundCount++;
                        results.AppendLine($"Line {i + 1}: {lines[i].Trim()}");
                    }
                }
                
                if (foundCount == 0)
                {
                    return $"❌ '{searchText}' not found in {scriptName}.cs";
                }
                
                results.AppendLine();
                results.AppendLine($"✅ Found {foundCount} occurrence(s)");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error searching script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Replace text in a script
        /// </summary>
        public static string ReplaceInScript(string scriptName, string findText, string replaceText)
        {
            try
            {
                // Find the script file
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
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                
                // Count occurrences
                int count = 0;
                int index = 0;
                while ((index = currentContent.IndexOf(findText, index)) != -1)
                {
                    count++;
                    index += findText.Length;
                }
                
                if (count == 0)
                    return $"❌ '{findText}' not found in {scriptName}.cs";
                
                // Replace all occurrences
                string newContent = currentContent.Replace(findText, replaceText);
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ReplaceInScript] Replaced {count} occurrence(s) in {scriptName}.cs");
                
                return $"✅ Replaced {count} occurrence(s) of '{findText}' with '{replaceText}' in {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error replacing in script: {e.Message}";
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
                    // Use case-insensitive search
                    objects = Resources.FindObjectsOfTypeAll<GameObject>()
                        .Where(go => go.scene.isLoaded && 
                                     go.name.IndexOf(searchTerm, System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                                     !go.hideFlags.HasFlag(HideFlags.HideInHierarchy))
                        .ToArray();
                }
                
                if (objects.Length == 0)
                {
                    // Provide helpful suggestions
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    var allObjects = scene.GetRootGameObjects();
                    
                    var result = new System.Text.StringBuilder();
                    result.AppendLine($"❌ No GameObjects found matching '{searchTerm}'");
                    result.AppendLine();
                    result.AppendLine($"💡 Current scene: {scene.name}");
                    result.AppendLine($"💡 Total root objects: {allObjects.Length}");
                    
                    if (allObjects.Length > 0)
                    {
                        result.AppendLine();
                        result.AppendLine("Available GameObjects:");
                        foreach (var go in allObjects.Take(10))
                        {
                            result.AppendLine($"  - {go.name}");
                            // Show children too
                            for (int i = 0; i < go.transform.childCount && i < 3; i++)
                            {
                                result.AppendLine($"    - {go.transform.GetChild(i).name}");
                            }
                        }
                    }
                    else
                    {
                        result.AppendLine();
                        result.AppendLine("⚠️ Scene is empty! Create some GameObjects first.");
                    }
                    
                    return result.ToString();
                }
                
                var foundResult = new System.Text.StringBuilder();
                foundResult.AppendLine($"✅ Found {objects.Length} GameObject(s) matching '{searchTerm}':");
                foundResult.AppendLine();
                
                foreach (var go in objects.Take(20)) // Limit to 20
                {
                    foundResult.AppendLine($"📦 **{go.name}**");
                    foundResult.AppendLine($"   Path: {GetGameObjectPath(go)}");
                    foundResult.AppendLine($"   Active: {go.activeInHierarchy}");
                    
                    var components = go.GetComponents<Component>();
                    if (components.Length > 1) // More than just Transform
                    {
                        foundResult.AppendLine($"   Components:");
                        foreach (var comp in components)
                        {
                            if (comp != null && !(comp is Transform))
                            {
                                foundResult.AppendLine($"     • {comp.GetType().Name}");
                            }
                        }
                    }
                    foundResult.AppendLine();
                }
                
                if (objects.Length > 20)
                    foundResult.AppendLine($"... and {objects.Length - 20} more");
                
                return foundResult.ToString();
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
        
        // ==================== MATERIAL & VISUAL ====================
        
        /// <summary>
        /// Create a new material
        /// </summary>
        public static string CreateMaterial(string name, string color = null)
        {
            try
            {
                string path = $"Assets/{name}.mat";
                
                // Check if material already exists
                if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                    return $"❌ Material '{name}' already exists at {path}";
                
                var material = new Material(Shader.Find("Standard"));
                
                // Set color if provided
                if (!string.IsNullOrEmpty(color))
                {
                    Color col;
                    if (color.StartsWith("#"))
                    {
                        ColorUtility.TryParseHtmlString(color, out col);
                        material.color = col;
                    }
                    else
                    {
                        switch (color.ToLower())
                        {
                            case "red": material.color = Color.red; break;
                            case "green": material.color = Color.green; break;
                            case "blue": material.color = Color.blue; break;
                            case "white": material.color = Color.white; break;
                            case "black": material.color = Color.black; break;
                            case "yellow": material.color = Color.yellow; break;
                            case "cyan": material.color = Color.cyan; break;
                            case "magenta": material.color = Color.magenta; break;
                            case "gray": case "grey": material.color = Color.gray; break;
                            default: material.color = Color.white; break;
                        }
                    }
                }
                
                AssetDatabase.CreateAsset(material, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Debug.Log($"[CreateMaterial] Created material '{name}' at {path}");
                
                return $"✅ Created material '{name}' at {path}";
            }
            catch (Exception e)
            {
                return $"❌ Error creating material: {e.Message}";
            }
        }
        
        /// <summary>
        /// Assign a material to a GameObject's renderer
        /// </summary>
        public static string AssignMaterial(string gameObjectName, string materialName)
        {
            try
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                    return $"❌ GameObject '{gameObjectName}' not found";
                
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null)
                    return $"❌ GameObject '{gameObjectName}' has no Renderer component";
                
                // Find material
                var materialGuids = AssetDatabase.FindAssets($"{materialName} t:Material");
                if (materialGuids.Length == 0)
                    return $"❌ Material '{materialName}' not found. Create it first with create_material.";
                
                Material mat = null;
                foreach (var guid in materialGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var loadedMat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (loadedMat != null && loadedMat.name == materialName)
                    {
                        mat = loadedMat;
                        break;
                    }
                }
                
                if (mat == null)
                    return $"❌ Material '{materialName}' not found";
                
                Undo.RecordObject(renderer, "Assign Material");
                renderer.material = mat;
                
                Debug.Log($"[AssignMaterial] Assigned '{materialName}' to {gameObjectName}");
                Selection.activeGameObject = go;
                
                return $"✅ Assigned material '{materialName}' to {gameObjectName}";
            }
            catch (Exception e)
            {
                return $"❌ Error assigning material: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create a light GameObject
        /// </summary>
        public static string CreateLight(string name, string lightType, string color = "white", float intensity = 1.0f)
        {
            try
            {
                var go = new GameObject(name);
                var light = go.AddComponent<Light>();
                
                // Set light type
                switch (lightType.ToLower())
                {
                    case "directional": light.type = LightType.Directional; break;
                    case "point": light.type = LightType.Point; break;
                    case "spot": light.type = LightType.Spot; break;
                    case "area": light.type = LightType.Rectangle; break;
                    default: light.type = LightType.Point; break;
                }
                
                // Set color
                Color col = Color.white;
                if (color.StartsWith("#"))
                {
                    ColorUtility.TryParseHtmlString(color, out col);
                }
                else
                {
                    switch (color.ToLower())
                    {
                        case "red": col = Color.red; break;
                        case "green": col = Color.green; break;
                        case "blue": col = Color.blue; break;
                        case "yellow": col = Color.yellow; break;
                        case "cyan": col = Color.cyan; break;
                        case "magenta": col = Color.magenta; break;
                        default: col = Color.white; break;
                    }
                }
                light.color = col;
                light.intensity = intensity;
                
                Undo.RegisterCreatedObjectUndo(go, "Create Light");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreateLight] Created {lightType} light '{name}'");
                
                return $"✅ Created {lightType} light '{name}' (color: {color}, intensity: {intensity})";
            }
            catch (Exception e)
            {
                return $"❌ Error creating light: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create a camera GameObject
        /// </summary>
        public static string CreateCamera(string name, float fieldOfView = 60f)
        {
            try
            {
                var go = new GameObject(name);
                var camera = go.AddComponent<Camera>();
                camera.fieldOfView = fieldOfView;
                
                // Add audio listener if main camera
                if (name.ToLower().Contains("main"))
                {
                    go.AddComponent<AudioListener>();
                }
                
                Undo.RegisterCreatedObjectUndo(go, "Create Camera");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreateCamera] Created camera '{name}' (FOV: {fieldOfView})");
                
                return $"✅ Created camera '{name}' (FOV: {fieldOfView}°)";
            }
            catch (Exception e)
            {
                return $"❌ Error creating camera: {e.Message}";
            }
        }
        
        // ==================== SCENE MANAGEMENT ====================
        
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
        
        // ====================ADVANCED SCRIPT ANALYSIS & TEMPLATES ====================
        
        /// <summary>
        /// Validate script for syntax errors (basic check)
        /// </summary>
        public static string ValidateScript(string scriptName)
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
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                var results = new System.Text.StringBuilder();
                results.AppendLine($"🔍 Validation results for {scriptName}.cs:");
                results.AppendLine();
                
                int issueCount = 0;
                
                // Basic syntax checks
                int openBraces = 0, closeBraces = 0;
                foreach (char c in content)
                {
                    if (c == '{') openBraces++;
                    if (c == '}') closeBraces++;
                }
                
                if (openBraces != closeBraces)
                {
                    issueCount++;
                    results.AppendLine($"⚠️ Brace mismatch: {openBraces} open, {closeBraces} close");
                }
                
                // Check for common issues
                if (!content.Contains("using UnityEngine"))
                {
                    issueCount++;
                    results.AppendLine("⚠️ Missing 'using UnityEngine;'");
                }
                
                if (!content.Contains($"class {scriptName}"))
                {
                    issueCount++;
                    results.AppendLine($"⚠️ Class name doesn't match file name '{scriptName}'");
                }
                
                if (issueCount == 0)
                {
                    results.AppendLine("✅ No obvious syntax issues found");
                    results.AppendLine("💡 For detailed analysis, check Unity Console after compilation");
                }
                else
                {
                    results.AppendLine();
                    results.AppendLine($"❌ Found {issueCount} potential issue(s)");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error validating script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create script from template
        /// </summary>
        public static string CreateFromTemplate(string scriptName, string templateType, string gameObjectName = null)
        {
            try
            {
                string templateContent = "";
                
                switch (templateType.ToLower())
                {
                    case "singleton":
                        templateContent = $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    private static {scriptName} _instance;
    public static {scriptName} Instance
    {{
        get
        {{
            if (_instance == null)
            {{
                _instance = FindObjectOfType<{scriptName}>();
                if (_instance == null)
                {{
                    GameObject go = new GameObject(""{scriptName}"");
                    _instance = go.AddComponent<{scriptName}>();
                }}
            }}
            return _instance;
        }}
    }}

    void Awake()
    {{
        if (_instance != null && _instance != this)
        {{
            Destroy(gameObject);
            return;
        }}
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }}
}}";
                        break;
                        
                    case "statemachine":
                        templateContent = $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    public enum State {{ Idle, Moving, Attacking, Dead }}
    
    private State currentState = State.Idle;
    
    void Update()
    {{
        switch (currentState)
        {{
            case State.Idle:
                HandleIdleState();
                break;
            case State.Moving:
                HandleMovingState();
                break;
            case State.Attacking:
                HandleAttackingState();
                break;
            case State.Dead:
                HandleDeadState();
                break;
        }}
    }}
    
    public void ChangeState(State newState)
    {{
        currentState = newState;
        Debug.Log(""State changed to: "" + newState);
    }}
    
    void HandleIdleState() {{ }}
    void HandleMovingState() {{ }}
    void HandleAttackingState() {{ }}
    void HandleDeadState() {{ }}
}}";
                        break;
                        
                    case "objectpool":
                        templateContent = $@"using System.Collections.Generic;
using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start()
    {{
        for (int i = 0; i < poolSize; i++)
        {{
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }}
    }}
    
    public GameObject GetFromPool()
    {{
        if (pool.Count > 0)
        {{
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }}
        else
        {{
            GameObject obj = Instantiate(prefab);
            return obj;
        }}
    }}
    
    public void ReturnToPool(GameObject obj)
    {{
        obj.SetActive(false);
        pool.Enqueue(obj);
    }}
}}";
                        break;
                        
                    case "scriptableobject":
                        templateContent = $@"using UnityEngine;

[CreateAssetMenu(fileName = ""{scriptName}"", menuName = ""ScriptableObjects/{scriptName}"", order = 1)]
public class {scriptName} : ScriptableObject
{{
    public string displayName;
    public string description;
    
    // Add your custom properties here
}}";
                        break;
                        
                    default:
                        return $"❌ Unknown template type '{templateType}'. Available: singleton, statemachine, objectpool, scriptableobject";
                }
                
                // Create the script
                string path = $"Assets/{scriptName}.cs";
                System.IO.File.WriteAllText(path, templateContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[CreateFromTemplate] Created {scriptName}.cs from {templateType} template");
                
                // Attach to GameObject if specified
                if (!string.IsNullOrEmpty(gameObjectName) && templateType.ToLower() != "scriptableobject")
                {
                    return CreateAndAttachScript(gameObjectName, scriptName, templateContent);
                }
                
                return $"✅ Created {scriptName}.cs from {templateType} template at {path}";
            }
            catch (Exception e)
            {
                return $"❌ Error creating from template: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add comments/documentation to script
        /// </summary>
        public static string AddCommentsToScript(string scriptName, string comments)
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
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Add header comment
                string headerComment = $@"/*
 * {scriptName}
 * {comments}
 * Generated: {System.DateTime.Now.ToString(""yyyy-MM-dd HH:mm"")}
 */

";
                string newContent = headerComment + content;
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddCommentsToScript] Added comments to {scriptName}.cs");
                
                return $"✅ Added comments to {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error adding comments: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create multiple scripts at once
        /// </summary>
        public static string CreateMultipleScripts(string scriptNames, string baseNamespace = null)
        {
            try
            {
                var names = scriptNames.Split(new[] { ',', ';', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                var results = new System.Text.StringBuilder();
                results.AppendLine($"📝 Creating {names.Length} scripts:");
                results.AppendLine();
                
                int successCount = 0;
                foreach (var name in names)
                {
                    string cleanName = name.Trim();
                    if (string.IsNullOrEmpty(cleanName)) continue;
                    
                    string namespaceDecl = string.IsNullOrEmpty(baseNamespace) ? "" : $"namespace {baseNamespace}\n{{\n";
                    string namespaceEnd = string.IsNullOrEmpty(baseNamespace) ? "" : "\n}";
                    string indent = string.IsNullOrEmpty(baseNamespace) ? "" : "    ";
                    
                    string content = $@"using UnityEngine;

{namespaceDecl}{indent}public class {cleanName} : MonoBehaviour
{indent}{{
{indent}    void Start()
{indent}    {{
{indent}        
{indent}    }}

{indent}    void Update()
{indent}    {{
{indent}        
{indent}    }}
{indent}}}{namespaceEnd}";
                    
                    string path = $"Assets/{cleanName}.cs";
                    System.IO.File.WriteAllText(path, content);
                    results.AppendLine($"✅ {cleanName}.cs");
                    successCount++;
                }
                
                AssetDatabase.Refresh();
                
                results.AppendLine();
                results.AppendLine($"✅ Created {successCount} script(s) successfully!");
                
                Debug.Log($"[CreateMultipleScripts] Created {successCount} scripts");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error creating multiple scripts: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add namespace to script
        /// </summary>
        public static string AddNamespaceToScript(string scriptName, string namespaceName)
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
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Check if namespace already exists
                if (content.Contains($"namespace {namespaceName}"))
                    return $"ℹ️ Namespace '{namespaceName}' already exists in {scriptName}.cs";
                
                // Find where to insert namespace
                var lines = content.Split('\n');
                int lastUsingIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("using "))
                        lastUsingIndex = i;
                }
                
                // Build new content with namespace
                var newLines = new System.Collections.Generic.List<string>();
                for (int i = 0; i <= lastUsingIndex; i++)
                {
                    newLines.Add(lines[i]);
                }
                
                newLines.Add("");
                newLines.Add($"namespace {namespaceName}");
                newLines.Add("{");
                
                for (int i = lastUsingIndex + 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                        newLines.Add("    " + lines[i]);
                    else
                        newLines.Add(lines[i]);
                }
                
                newLines.Add("}");
                
                string newContent = string.Join("\n", newLines);
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddNamespaceToScript] Added namespace '{namespaceName}' to {scriptName}.cs");
                
                return $"✅ Added namespace '{namespaceName}' to {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error adding namespace: {e.Message}";
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

