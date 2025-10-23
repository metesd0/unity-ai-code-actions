using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Holds contextual information about the Unity project for LLM
    /// </summary>
    [Serializable]
    public class ProjectContext
    {
        public List<ScriptInfo> scripts = new List<ScriptInfo>();
        public List<SceneInfo> scenes = new List<SceneInfo>();
        public List<PrefabInfo> prefabs = new List<PrefabInfo>();
        public Dictionary<string, string> customContext = new Dictionary<string, string>();

        public int TotalTokenEstimate => EstimateTokens();

        private int EstimateTokens()
        {
            // Rough estimate: 1 token ≈ 4 characters
            int total = 0;
            foreach (var script in scripts)
                total += script.content.Length / 4;
            return total;
        }

        public string BuildContextString(int maxTokens = 4000)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Unity Project Context");
            sb.AppendLine();

            // Add scripts with token budgeting
            int tokensUsed = 0;
            int tokenBudgetPerScript = maxTokens / Math.Max(1, scripts.Count);

            foreach (var script in scripts.OrderByDescending(s => s.relevanceScore))
            {
                int scriptTokens = script.content.Length / 4;
                if (tokensUsed + scriptTokens > maxTokens)
                {
                    // Add summary only
                    sb.AppendLine($"## {script.name}");
                    sb.AppendLine($"Path: {script.path}");
                    sb.AppendLine($"Classes: {string.Join(", ", script.classes)}");
                    sb.AppendLine();
                    tokensUsed += 50;
                }
                else
                {
                    sb.AppendLine($"## {script.name}");
                    sb.AppendLine($"```csharp");
                    sb.AppendLine(script.content);
                    sb.AppendLine("```");
                    sb.AppendLine();
                    tokensUsed += scriptTokens;
                }
            }

            return sb.ToString();
        }
    }

    [Serializable]
    public class ScriptInfo
    {
        public string name;
        public string path;
        public string content;
        public List<string> classes = new List<string>();
        public List<string> methods = new List<string>();
        public List<string> dependencies = new List<string>();
        public float relevanceScore = 1.0f;
    }

    [Serializable]
    public class SceneInfo
    {
        public string name;
        public string path;
        public List<string> gameObjects = new List<string>();
        public List<string> components = new List<string>();
    }

    [Serializable]
    public class PrefabInfo
    {
        public string name;
        public string path;
        public List<string> components = new List<string>();
    }
}

