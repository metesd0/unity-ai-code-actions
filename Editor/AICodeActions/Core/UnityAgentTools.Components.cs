using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Component Operations: Add, Attach, Configure components and scripts
    /// </summary>
    public static partial class UnityAgentTools
    {
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
        
        /// <summary>
        /// Remove a component from a GameObject
        /// </summary>
        public static string RemoveComponent(string gameObjectName, string componentType)
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
                
                // Prevent removing Transform
                if (component is Transform)
                    return $"❌ Cannot remove Transform component - it's required!";
                
                // Record undo and destroy
                Undo.DestroyObjectImmediate(component);
                
                Debug.Log($"[RemoveComponent] Removed {componentType} from {gameObjectName}");
                Selection.activeGameObject = go;
                
                return $"✅ Removed {componentType} from {gameObjectName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[RemoveComponent] Error: {e}");
                return $"❌ Error removing component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Enable or disable a component
        /// </summary>
        public static string EnableComponent(string gameObjectName, string componentType, bool enabled)
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
                
                // Check if component is a Behaviour (has enabled property)
                if (component is Behaviour behaviour)
                {
                    Undo.RecordObject(behaviour, $"{(enabled ? "Enable" : "Disable")} {componentType}");
                    behaviour.enabled = enabled;
                    EditorUtility.SetDirty(behaviour);
                }
                else if (component is Renderer renderer)
                {
                    Undo.RecordObject(renderer, $"{(enabled ? "Enable" : "Disable")} {componentType}");
                    renderer.enabled = enabled;
                    EditorUtility.SetDirty(renderer);
                }
                else if (component is Collider collider)
                {
                    Undo.RecordObject(collider, $"{(enabled ? "Enable" : "Disable")} {componentType}");
                    collider.enabled = enabled;
                    EditorUtility.SetDirty(collider);
                }
                else
                {
                    return $"❌ Component '{componentType}' does not have an 'enabled' property";
                }
                
                Debug.Log($"[EnableComponent] {(enabled ? "Enabled" : "Disabled")} {componentType} on {gameObjectName}");
                Selection.activeGameObject = go;
                
                return $"✅ {(enabled ? "Enabled" : "Disabled")} {componentType} on {gameObjectName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[EnableComponent] Error: {e}");
                return $"❌ Error {(enabled ? "enabling" : "disabling")} component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Copy a component from one GameObject to another
        /// </summary>
        public static string CopyComponent(string sourceGameObjectName, string targetGameObjectName, string componentType)
        {
            try
            {
                var sourceGo = GameObject.Find(sourceGameObjectName);
                if (sourceGo == null)
                    return $"❌ Source GameObject '{sourceGameObjectName}' not found";
                
                var targetGo = GameObject.Find(targetGameObjectName);
                if (targetGo == null)
                    return $"❌ Target GameObject '{targetGameObjectName}' not found";
                
                // Find source component
                Component sourceComponent = null;
                var components = sourceGo.GetComponents<Component>();
                
                foreach (var comp in components)
                {
                    if (comp != null && comp.GetType().Name == componentType)
                    {
                        sourceComponent = comp;
                        break;
                    }
                }
                
                if (sourceComponent == null)
                    return $"❌ Component '{componentType}' not found on {sourceGameObjectName}";
                
                // Check if target already has this component
                if (targetGo.GetComponent(sourceComponent.GetType()) != null)
                {
                    return $"ℹ️ Component '{componentType}' already exists on {targetGameObjectName}. Use remove_component first if you want to replace it.";
                }
                
                // Copy component using Unity's component copy functionality
                UnityEditorInternal.ComponentUtility.CopyComponent(sourceComponent);
                bool success = UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetGo);
                
                if (!success)
                    return $"❌ Failed to copy component '{componentType}'";
                
                Debug.Log($"[CopyComponent] Copied {componentType} from {sourceGameObjectName} to {targetGameObjectName}");
                Selection.activeGameObject = targetGo;
                
                return $"✅ Copied {componentType} from {sourceGameObjectName} to {targetGameObjectName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[CopyComponent] Error: {e}");
                return $"❌ Error copying component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Reset a component to default values
        /// </summary>
        public static string ResetComponent(string gameObjectName, string componentType)
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
                
                // Use Unity's built-in reset functionality
                Undo.RecordObject(component, $"Reset {componentType}");
                
                // Get default values by creating a temporary GameObject
                var tempGo = new GameObject("TempResetObject");
                var defaultComponent = tempGo.AddComponent(component.GetType());
                
                // Copy serialized properties from default to actual component
                var serializedDefault = new SerializedObject(defaultComponent);
                var serializedActual = new SerializedObject(component);
                
                var prop = serializedDefault.GetIterator();
                while (prop.NextVisible(true))
                {
                    // Skip script reference
                    if (prop.name == "m_Script") continue;
                    
                    var actualProp = serializedActual.FindProperty(prop.name);
                    if (actualProp != null)
                    {
                        serializedActual.CopyFromSerializedProperty(prop);
                    }
                }
                
                serializedActual.ApplyModifiedProperties();
                
                // Cleanup
                UnityEngine.Object.DestroyImmediate(tempGo);
                
                EditorUtility.SetDirty(component);
                Debug.Log($"[ResetComponent] Reset {componentType} on {gameObjectName}");
                Selection.activeGameObject = go;
                
                return $"✅ Reset {componentType} on {gameObjectName} to default values";
            }
            catch (Exception e)
            {
                Debug.LogError($"[ResetComponent] Error: {e}");
                return $"❌ Error resetting component: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set multiple properties on a component at once (batch operation)
        /// </summary>
        public static string SetMultipleProperties(string gameObjectName, string componentType, string propertiesJson)
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
                
                Undo.RecordObject(component, $"Set Multiple Properties on {componentType}");
                
                // Parse JSON properties (simple key:value format)
                // Expected format: "property1:value1,property2:value2,property3:value3"
                var properties = propertiesJson.Split(',');
                var results = new System.Text.StringBuilder();
                results.AppendLine($"📋 Setting {properties.Length} properties on {componentType}:");
                
                int successCount = 0;
                int failCount = 0;
                
                foreach (var propPair in properties)
                {
                    var parts = propPair.Split(':');
                    if (parts.Length != 2)
                    {
                        results.AppendLine($"  ⚠️ Invalid format: {propPair}");
                        failCount++;
                        continue;
                    }
                    
                    string propertyName = parts[0].Trim();
                    string value = parts[1].Trim();
                    
                    // Reuse SetComponentProperty logic for each property
                    var type = component.GetType();
                    var field = type.GetField(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var property = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (field == null && property == null)
                    {
                        results.AppendLine($"  ❌ {propertyName}: Property not found");
                        failCount++;
                        continue;
                    }
                    
                    try
                    {
                        var fieldOrPropertyType = field != null ? field.FieldType : property.PropertyType;
                        object convertedValue = ConvertValueToType(value, fieldOrPropertyType);
                        
                        if (field != null)
                            field.SetValue(component, convertedValue);
                        else
                            property.SetValue(component, convertedValue);
                        
                        results.AppendLine($"  ✅ {propertyName} = {value}");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        results.AppendLine($"  ❌ {propertyName}: {ex.Message}");
                        failCount++;
                    }
                }
                
                EditorUtility.SetDirty(component);
                Selection.activeGameObject = go;
                
                results.AppendLine();
                results.AppendLine($"📊 Results: ✅ {successCount} success, ❌ {failCount} failed");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SetMultipleProperties] Error: {e}");
                return $"❌ Error setting multiple properties: {e.Message}";
            }
        }
        
        /// <summary>
        /// Helper: Convert string value to target type
        /// </summary>
        private static object ConvertValueToType(string value, Type targetType)
        {
            if (targetType == typeof(float))
                return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            if (targetType == typeof(int))
                return int.Parse(value);
            if (targetType == typeof(bool))
                return bool.Parse(value);
            if (targetType == typeof(string))
                return value;
            if (targetType == typeof(Vector3))
            {
                string cleanValue = value.Replace("(", "").Replace(")", "").Trim();
                var parts = cleanValue.Split(',');
                return new Vector3(
                    float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture));
            }
            if (targetType == typeof(Color))
            {
                if (value.StartsWith("#"))
                {
                    ColorUtility.TryParseHtmlString(value, out Color color);
                    return color;
                }
                return value.ToLower() switch
                {
                    "red" => Color.red,
                    "green" => Color.green,
                    "blue" => Color.blue,
                    "white" => Color.white,
                    "black" => Color.black,
                    "yellow" => Color.yellow,
                    "cyan" => Color.cyan,
                    "magenta" => Color.magenta,
                    _ => Color.gray
                };
            }
            if (targetType == typeof(Transform) || targetType == typeof(GameObject))
            {
                var targetGo = GameObject.Find(value);
                return targetType == typeof(Transform) ? (object)targetGo?.transform : (object)targetGo;
            }
            
            throw new NotSupportedException($"Type {targetType.Name} is not supported");
        }
    }
}

