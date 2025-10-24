using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICodeActions.Core
{
    /// <summary>
    /// Unity Agent Tools - Main Entry Point
    /// 
    /// This class has been organized into multiple partial class files for better maintainability:
    /// 
    /// 📁 UnityAgentTools.Core.cs
    ///    - Helper methods (GetGameObjectPath, AppendGameObjectTree)
    /// 
    /// 📁 UnityAgentTools.FileOps.cs  (~140 lines)
    ///    - ReadScript: Read C# script content
    ///    - ReadFile: Read any file from Assets
    ///    - ListScripts: List all scripts in project
    /// 
    /// 📁 UnityAgentTools.GameObjects.cs  (~450 lines)
    ///    - GetGameObjectInfo: Get detailed GameObject info
    ///    - FindGameObjects: Search GameObjects by name/tag
    ///    - CreateGameObject: Create empty GameObject
    ///    - CreatePrimitive: Create primitive shapes
    ///    - SetPosition, SetRotation, SetScale: Transform operations
    ///    - DeleteGameObject: Remove GameObject
    ///    - SetParent, SetActive, RenameGameObject, DuplicateGameObject
    ///    - SetTag, SetLayer: GameObject properties
    /// 
    /// 📁 UnityAgentTools.Components.cs  (~450 lines)
    ///    - AddComponent: Add built-in or custom components
    ///    - AttachScript: Attach existing compiled script
    ///    - CreateAndAttachScript: Create and attach new script
    ///    - SetComponentProperty: Set property values on components
    /// 
    /// 📁 UnityAgentTools.Scripts.cs  (~750 lines)
    ///    - ModifyScript: Add code to existing script
    ///    - AddMethodToScript: Insert new method
    ///    - AddFieldToScript: Insert new field/property
    ///    - DeleteScript: Remove script file
    ///    - FindInScript: Search text in script
    ///    - ReplaceInScript: Find and replace text
    ///    - ValidateScript: Basic syntax validation
    ///    - CreateFromTemplate: Generate from templates (Singleton, StateMachine, ObjectPool, ScriptableObject)
    ///    - AddCommentsToScript: Add header documentation
    ///    - CreateMultipleScripts: Batch script creation
    ///    - AddNamespaceToScript: Wrap script in namespace
    /// 
    /// 📁 UnityAgentTools.Visual.cs  (~200 lines)
    ///    - CreateMaterial: Create new material asset
    ///    - AssignMaterial: Assign material to renderer
    ///    - CreateLight: Create light GameObject (Directional, Point, Spot, Area)
    ///    - CreateCamera: Create camera GameObject
    /// 
    /// 📁 UnityAgentTools.Scene.cs  (~150 lines)
    ///    - GetSceneInfo: Get current scene hierarchy
    ///    - SaveScene: Save current scene
    ///    - SaveSceneAs: Save scene with new name
    ///    - GetProjectStats: Get project statistics
    /// 
    /// Total: ~2150 lines split into 7 focused files
    /// </summary>
    public static partial class UnityAgentTools
    {
        // All method implementations are in the partial class files listed above.
        // This main file serves as documentation and entry point.
    }
}
