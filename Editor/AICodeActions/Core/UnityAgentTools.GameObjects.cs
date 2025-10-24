using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// GameObject Operations: Create, Find, Transform, Hierarchy, Primitives
    /// </summary>
    public static partial class UnityAgentTools
    {
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
                info.AppendLine();
                info.AppendLine($"- **Active**: {go.activeInHierarchy}");
                info.AppendLine($"- **Tag**: {go.tag}");
                info.AppendLine($"- **Layer**: {LayerMask.LayerToName(go.layer)}");
                info.AppendLine($"- **Position**: {go.transform.position}");
                info.AppendLine($"- **Rotation**: {go.transform.rotation.eulerAngles}");
                info.AppendLine($"- **Scale**: {go.transform.localScale}");
                
                if (go.transform.parent != null)
                {
                    info.AppendLine($"- **Parent**: {go.transform.parent.name}");
                }
                
                info.AppendLine();
                info.AppendLine($"## Components ({go.GetComponents<Component>().Length}):");
                
                foreach (var component in go.GetComponents<Component>())
                {
                    if (component == null) continue;
                    
                    info.AppendLine($"- {component.GetType().Name}");
                    
                    if (component is MonoBehaviour mb)
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
        /// Find GameObjects by name or tag
        /// </summary>
        public static string FindGameObjects(string searchTerm, bool byTag = false)
        {
            try
            {
                GameObject[] found;
                
                if (byTag)
                {
                    found = GameObject.FindGameObjectsWithTag(searchTerm);
                }
                else
                {
                    var allObjects = GameObject.FindObjectsOfType<GameObject>();
                    found = allObjects.Where(go => 
                        go.name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0
                    ).ToArray();
                }
                
                if (found.Length == 0)
                {
                    var allGOs = GameObject.FindObjectsOfType<GameObject>();
                    var names = string.Join(", ", allGOs.Select(g => g.name).Distinct().Take(20));
                    return byTag 
                        ? $"❌ No GameObjects found with tag '{searchTerm}'"
                        : $"❌ No GameObjects found matching '{searchTerm}'. Available: {names}";
                }
                
                var result = new System.Text.StringBuilder();
                result.AppendLine($"🔍 Found {found.Length} GameObject(s):");
                result.AppendLine();
                
                foreach (var go in found.Take(20))
                {
                    result.AppendLine($"- **{go.name}**");
                    result.AppendLine($"  Position: {go.transform.position}");
                    result.AppendLine($"  Components: {go.GetComponents<Component>().Length}");
                }
                
                if (found.Length > 20)
                {
                    result.AppendLine($"... and {found.Length - 20} more");
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding GameObjects: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create an empty GameObject
        /// </summary>
        public static string CreateGameObject(string name, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                var go = new GameObject(name);
                go.transform.position = new Vector3(x, y, z);
                
                Undo.RegisterCreatedObjectUndo(go, "Create GameObject");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreateGameObject] Created '{name}' at ({x}, {y}, {z})");
                
                return $"✅ Created GameObject '{name}' at position ({x}, {y}, {z})";
            }
            catch (Exception e)
            {
                return $"❌ Error creating GameObject: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create a primitive GameObject
        /// </summary>
        public static string CreatePrimitive(string primitiveType, string name = null, float x = 0, float y = 0, float z = 0)
        {
            try
            {
                PrimitiveType type;
                if (!Enum.TryParse(primitiveType, true, out type))
                {
                    return $"❌ Invalid primitive type '{primitiveType}'. Valid types: Cube, Sphere, Capsule, Cylinder, Plane, Quad";
                }
                
                var go = GameObject.CreatePrimitive(type);
                go.name = string.IsNullOrEmpty(name) ? primitiveType : name;
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
                
                Undo.RecordObject(go, "Set Tag");
                go.tag = tag;
                
                Debug.Log($"[SetTag] Set {gameObjectName} tag to '{tag}'");
                
                return $"✅ Set {gameObjectName} tag to '{tag}'";
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
                    return $"❌ Layer '{layerName}' does not exist";
                
                Undo.RecordObject(go, "Set Layer");
                go.layer = layer;
                
                Debug.Log($"[SetLayer] Set {gameObjectName} layer to '{layerName}'");
                
                return $"✅ Set {gameObjectName} layer to '{layerName}'";
            }
            catch (Exception e)
            {
                return $"❌ Error setting layer: {e.Message}";
            }
        }
    }
}

