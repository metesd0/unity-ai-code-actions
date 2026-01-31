using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AICodeActions.Core
{
    /// <summary>
    /// Core utilities and helper methods for Unity Agent Tools
    /// This partial class contains shared helper methods used across all tool categories
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Get full hierarchical path of a GameObject
        /// </summary>
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
        
        // NOTE: AppendGameObjectTree is defined in UnityAgentTools.Scene.cs
        // to avoid duplication since it's primarily used there
    }
}

