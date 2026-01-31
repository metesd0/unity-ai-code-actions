using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using AICodeActions.Providers;

namespace AICodeActions.Core
{
    /// <summary>
    /// Decomposes complex user requests into actionable sub-tasks
    /// </summary>
    public class TaskDecomposer
    {
        private IModelProvider modelProvider;
        
        public TaskDecomposer(IModelProvider provider)
        {
            modelProvider = provider;
        }
        
        /// <summary>
        /// Analyzes user goal and creates a detailed task plan
        /// </summary>
        public async Task<TaskPlan> DecomposeTask(string userGoal)
        {
            Debug.Log($"[TaskDecomposer] Analyzing goal: {userGoal}");
            
            var decompositionPrompt = BuildDecompositionPrompt(userGoal);
            
            try
            {
                var response = await modelProvider.GenerateAsync(decompositionPrompt);
                var plan = ParseDecompositionResponse(response, userGoal);
                
                Debug.Log($"[TaskDecomposer] Created plan with {plan.TotalTasksCount} sub-tasks");
                return plan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TaskDecomposer] Failed to decompose task: {ex.Message}");
                // Fallback: Create simple single-step plan
                return CreateFallbackPlan(userGoal);
            }
        }
        
        /// <summary>
        /// Checks if user request is complex enough to need decomposition
        /// </summary>
        public bool ShouldDecompose(string userMessage)
        {
            // Simple heuristics
            var keywords = new[] {
                "fps", "controller", "system", "complete", "full", "entire",
                "menu", "inventory", "dialogue", "ai", "enemy", "game",
                "multi", "several", "bunch of", "tüm", "komple", "sistem"
            };
            
            var lowerMessage = userMessage.ToLower();
            
            // Check for complexity indicators
            var hasComplexKeyword = Array.Exists(keywords, k => lowerMessage.Contains(k));
            var hasMultipleRequests = lowerMessage.Split(new[] { "and", "ve", "," }, StringSplitOptions.None).Length > 2;
            var isLongRequest = userMessage.Length > 100;
            
            return hasComplexKeyword || hasMultipleRequests || isLongRequest;
        }
        
        private string BuildDecompositionPrompt(string userGoal)
        {
            return $@"You are a Unity task planning expert. Break down the following user goal into 5-10 concrete, actionable sub-tasks.

USER GOAL: ""{userGoal}""

For each sub-task:
1. Provide a clear description
2. List required Unity tools (e.g., create_script, get_scene_info, attach_script, etc.)
3. Suggest key parameters if applicable

IMPORTANT:
- Keep sub-tasks atomic (one clear objective each)
- Order them logically (dependencies first)
- Include compilation/error checking steps
- Be specific about GameObject names and script names

Respond in this EXACT JSON format:
{{
  ""subTasks"": [
    {{
      ""description"": ""Check scene for existing player GameObject"",
      ""requiredTools"": [""get_scene_info""],
      ""suggestedParameters"": {{}}
    }},
    {{
      ""description"": ""Create PlayerMovement script with WASD controls"",
      ""requiredTools"": [""create_script""],
      ""suggestedParameters"": {{
        ""script_name"": ""PlayerMovement"",
        ""folder_path"": ""Assets/Scripts""
      }}
    }},
    {{
      ""description"": ""Check for compilation errors"",
      ""requiredTools"": [""get_compilation_errors""],
      ""suggestedParameters"": {{}}
    }}
  ]
}}

Only respond with valid JSON, nothing else.";
        }
        
        private TaskPlan ParseDecompositionResponse(string response, string mainGoal)
        {
            var plan = new TaskPlan
            {
                MainGoal = mainGoal
            };
            
            try
            {
                // Extract JSON from response
                var jsonMatch = Regex.Match(response, @"\{[\s\S]*\}", RegexOptions.Multiline);
                if (!jsonMatch.Success)
                {
                    throw new Exception("No JSON found in response");
                }
                
                var jsonStr = jsonMatch.Value;
                
                // Parse JSON manually (Unity's JsonUtility doesn't support lists at root level)
                var subTasksMatch = Regex.Matches(jsonStr, @"\{[^{}]*""description""[^{}]*\}", RegexOptions.Multiline);
                
                foreach (Match match in subTasksMatch)
                {
                    var taskJson = match.Value;
                    var subTask = ParseSubTask(taskJson);
                    if (subTask != null)
                    {
                        plan.SubTasks.Add(subTask);
                    }
                }
                
                // If no tasks parsed, create a simple one
                if (plan.SubTasks.Count == 0)
                {
                    plan.SubTasks.Add(new SubTask("Complete the task", "get_scene_info"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TaskDecomposer] Failed to parse JSON: {ex.Message}. Using fallback.");
                plan.SubTasks.Add(new SubTask("Complete the task", "get_scene_info"));
            }
            
            return plan;
        }
        
        private SubTask ParseSubTask(string json)
        {
            try
            {
                var subTask = new SubTask();
                
                // Extract description
                var descMatch = Regex.Match(json, @"""description""\s*:\s*""([^""]+)""");
                if (descMatch.Success)
                {
                    subTask.Description = descMatch.Groups[1].Value;
                }
                
                // Extract required tools
                var toolsMatch = Regex.Match(json, @"""requiredTools""\s*:\s*\[(.*?)\]", RegexOptions.Singleline);
                if (toolsMatch.Success)
                {
                    var toolsStr = toolsMatch.Groups[1].Value;
                    var tools = Regex.Matches(toolsStr, @"""([^""]+)""");
                    foreach (Match tool in tools)
                    {
                        subTask.RequiredTools.Add(tool.Groups[1].Value);
                    }
                }
                
                // Extract suggested parameters
                var paramsMatch = Regex.Match(json, @"""suggestedParameters""\s*:\s*\{([^}]*)\}", RegexOptions.Singleline);
                if (paramsMatch.Success)
                {
                    var paramsStr = paramsMatch.Groups[1].Value;
                    var paramPairs = Regex.Matches(paramsStr, @"""([^""]+)""\s*:\s*""([^""]+)""");
                    foreach (Match pair in paramPairs)
                    {
                        subTask.AddParameter(pair.Groups[1].Value, pair.Groups[2].Value);
                    }
                }
                
                return subTask;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TaskDecomposer] Failed to parse sub-task: {ex.Message}");
                return null;
            }
        }
        
        private TaskPlan CreateFallbackPlan(string userGoal)
        {
            Debug.Log("[TaskDecomposer] Creating fallback plan");
            
            var plan = new TaskPlan
            {
                MainGoal = userGoal
            };
            
            // Simple 3-step fallback
            plan.SubTasks.Add(new SubTask("Analyze scene", "get_scene_info"));
            plan.SubTasks.Add(new SubTask("Execute user request", "create_script", "create_gameobject"));
            plan.SubTasks.Add(new SubTask("Verify result", "get_compilation_errors"));
            
            return plan;
        }
        
        /// <summary>
        /// Quick check: Does this look like a simple single-step task?
        /// </summary>
        public bool IsSimpleTask(string userMessage)
        {
            var simplePatterns = new[]
            {
                @"^(what|ne|nedir|explain|açıkla)",
                @"^(show|göster|list|listele)",
                @"^(get|al|bul|find)",
                @"^(delete|sil|remove|kaldır)\s+\w+$"
            };
            
            return Array.Exists(simplePatterns, pattern => 
                Regex.IsMatch(userMessage.ToLower().Trim(), pattern));
        }
    }
}

