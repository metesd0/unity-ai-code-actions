using System;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Visual & Material Operations: Materials, Lights, Cameras
    /// </summary>
    public static partial class UnityAgentTools
    {
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
                // Unity auto-imports - no need to block UI with Refresh()
                
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
                    case "directional":
                        light.type = LightType.Directional;
                        break;
                    case "point":
                        light.type = LightType.Point;
                        break;
                    case "spot":
                        light.type = LightType.Spot;
                        break;
                    case "area":
                        light.type = LightType.Rectangle;
                        break;
                    default:
                        light.type = LightType.Point;
                        break;
                }
                
                // Set color
                switch (color.ToLower())
                {
                    case "red": light.color = Color.red; break;
                    case "green": light.color = Color.green; break;
                    case "blue": light.color = Color.blue; break;
                    case "white": light.color = Color.white; break;
                    case "yellow": light.color = Color.yellow; break;
                    case "cyan": light.color = Color.cyan; break;
                    case "magenta": light.color = Color.magenta; break;
                    default: light.color = Color.white; break;
                }
                
                light.intensity = intensity;
                
                Undo.RegisterCreatedObjectUndo(go, "Create Light");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreateLight] Created {lightType} light '{name}'");
                
                return $"✅ Created {lightType} light '{name}'";
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
                
                Undo.RegisterCreatedObjectUndo(go, "Create Camera");
                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);
                
                Debug.Log($"[CreateCamera] Created camera '{name}' with FOV {fieldOfView}");
                
                return $"✅ Created camera '{name}' with FOV {fieldOfView}°";
            }
            catch (Exception e)
            {
                return $"❌ Error creating camera: {e.Message}";
            }
        }
    }
}

