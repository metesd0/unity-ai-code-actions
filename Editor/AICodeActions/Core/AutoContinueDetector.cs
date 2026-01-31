using System;
using System.Linq;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Detects when AI should automatically continue to next step without user intervention
    /// </summary>
    public class AutoContinueDetector
    {
        private static readonly string[] CompletionKeywords = new[]
        {
            "tamamlandı", "bitti", "hazır", "completed", "done", "ready", "finished",
            "başarıyla", "successfully", "all set", "tamam", "ok"
        };
        
        private static readonly string[] ErrorKeywords = new[]
        {
            "error", "hata", "failed", "başarısız", "couldn't", "cannot", "unable"
        };
        
        private DateTime lastAutoContinue = DateTime.MinValue;
        private const float AUTO_CONTINUE_COOLDOWN = 2f; // seconds
        
        /// <summary>
        /// Determines if AI should automatically continue to next step
        /// </summary>
        public bool ShouldAutoContinue(
            TaskPlan currentPlan,
            string lastAIResponse,
            string lastToolResult)
        {
            // Cooldown check to prevent infinite loops
            if ((DateTime.Now - lastAutoContinue).TotalSeconds < AUTO_CONTINUE_COOLDOWN)
            {
                return false;
            }
            
            // No active plan = no auto-continue
            if (currentPlan == null || currentPlan.IsComplete)
            {
                return false;
            }
            
            // Check 1: Last tool had error? Don't auto-continue
            if (HasError(lastToolResult))
            {
                Debug.Log("[AutoContinue] Error detected, waiting for manual intervention");
                return false;
            }
            
            // Check 2: AI thinks it's done, but plan says otherwise
            bool aiThinksDone = ContainsCompletionKeyword(lastAIResponse);
            bool planIncomplete = !currentPlan.IsComplete;
            
            if (aiThinksDone && planIncomplete)
            {
                Debug.Log($"[AutoContinue] AI thinks done but {currentPlan.TotalTasksCount - currentPlan.CurrentStep} steps remain");
                lastAutoContinue = DateTime.Now;
                return true;
            }
            
            // Check 3: Current step has more required tools
            var currentTask = currentPlan.CurrentTask;
            if (currentTask != null && currentTask.RequiredTools.Count > 1)
            {
                // If we just used one tool, there might be more
                Debug.Log($"[AutoContinue] Current task has {currentTask.RequiredTools.Count} required tools");
                lastAutoContinue = DateTime.Now;
                return true;
            }
            
            // Check 4: Last tool was successful and there are pending steps
            bool toolSuccessful = !string.IsNullOrEmpty(lastToolResult) && !HasError(lastToolResult);
            bool hasPendingSteps = currentPlan.CurrentStep < currentPlan.TotalTasksCount;
            
            if (toolSuccessful && hasPendingSteps)
            {
                Debug.Log($"[AutoContinue] Tool successful, {currentPlan.TotalTasksCount - currentPlan.CurrentStep} steps pending");
                lastAutoContinue = DateTime.Now;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Generates continuation prompt to push AI to next step
        /// </summary>
        public string GetContinuationPrompt(TaskPlan plan)
        {
            if (plan == null || plan.IsComplete)
            {
                return "";
            }
            
            var currentTask = plan.CurrentTask;
            var nextTask = plan.NextTask;
            
            var prompt = $@"
[🤖 Auto-Continue]

📊 Progress: {plan.CompletedTasksCount}/{plan.TotalTasksCount} steps completed

✅ Completed:
";
            
            // Show last 2 completed tasks
            for (int i = Math.Max(0, plan.CurrentStep - 2); i < plan.CurrentStep; i++)
            {
                var task = plan.SubTasks[i];
                prompt += $"  {i + 1}. {task.Description}\n";
            }
            
            prompt += $"\n🔄 CURRENT STEP ({plan.CurrentStep + 1}/{plan.TotalTasksCount}):\n";
            prompt += $"  Task: {currentTask.Description}\n";
            prompt += $"  Required tools: {string.Join(", ", currentTask.RequiredTools)}\n";
            
            if (currentTask.SuggestedParameters.Count > 0)
            {
                prompt += $"  Suggested parameters:\n";
                foreach (var kvp in currentTask.SuggestedParameters)
                {
                    prompt += $"    • {kvp.Key} = \"{kvp.Value}\"\n";
                }
            }
            
            if (nextTask != null)
            {
                prompt += $"\n⏳ Next step: {nextTask.Description}\n";
            }
            
            prompt += "\nProceed with the CURRENT STEP now. Use the required tools.";
            
            return prompt;
        }
        
        /// <summary>
        /// Generates a reminder prompt when AI seems stuck
        /// </summary>
        public string GetStuckReminderPrompt(TaskPlan plan, int messagesSinceLastTool)
        {
            if (plan == null) return "";
            
            var currentTask = plan.CurrentTask;
            
            return $@"
⚠️ REMINDER: You haven't used any tools in the last {messagesSinceLastTool} messages.

Current task: {currentTask.Description}
Required tools: {string.Join(", ", currentTask.RequiredTools)}

Please use one of the required tools to proceed.";
        }
        
        /// <summary>
        /// Checks if response contains completion indicators
        /// </summary>
        private bool ContainsCompletionKeyword(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            
            var lowerText = text.ToLower();
            return CompletionKeywords.Any(keyword => lowerText.Contains(keyword));
        }
        
        /// <summary>
        /// Checks if result contains error indicators
        /// </summary>
        private bool HasError(string result)
        {
            if (string.IsNullOrEmpty(result)) return false;
            
            var lowerResult = result.ToLower();
            return ErrorKeywords.Any(keyword => lowerResult.Contains(keyword));
        }
        
        /// <summary>
        /// Estimates how many auto-continues are likely needed
        /// </summary>
        public int EstimateRemainingAutoContinues(TaskPlan plan)
        {
            if (plan == null || plan.IsComplete) return 0;
            
            return plan.TotalTasksCount - plan.CurrentStep;
        }
        
        /// <summary>
        /// Resets cooldown (useful for testing or manual triggers)
        /// </summary>
        public void ResetCooldown()
        {
            lastAutoContinue = DateTime.MinValue;
        }
    }
}

