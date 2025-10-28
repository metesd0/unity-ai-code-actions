using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AICodeActions.Providers;

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages error recovery through checkpoints and rollback
    /// </summary>
    public class SelfHealingManager
    {
        private Stack<WorkflowCheckpoint> checkpoints = new Stack<WorkflowCheckpoint>();
        private IModelProvider healingModel; // Cheap model for healing decisions
        
        private const int MAX_CHECKPOINTS = 20;
        private const int MAX_RETRIES_PER_STEP = 3;
        
        private Dictionary<int, int> stepRetryCount = new Dictionary<int, int>();
        
        public int CheckpointCount => checkpoints.Count;
        public bool CanRollback => checkpoints.Count > 0;
        
        public SelfHealingManager(IModelProvider provider)
        {
            healingModel = provider;
        }
        
        /// <summary>
        /// Save current workflow state as checkpoint
        /// </summary>
        public void SaveCheckpoint(TaskPlan plan, int stepIndex, string description = "")
        {
            var checkpoint = new WorkflowCheckpoint(stepIndex, plan, description);
            checkpoints.Push(checkpoint);
            
            // Limit checkpoint history
            if (checkpoints.Count > MAX_CHECKPOINTS)
            {
                var temp = checkpoints.ToList();
                temp.RemoveAt(0); // Remove oldest
                checkpoints = new Stack<WorkflowCheckpoint>(temp.AsEnumerable().Reverse());
            }
            
            Debug.Log($"[SelfHealing] Checkpoint saved: {checkpoint.GetSummary()}");
        }
        
        /// <summary>
        /// Rollback to last checkpoint
        /// </summary>
        public WorkflowCheckpoint RollbackToLastCheckpoint()
        {
            if (!CanRollback)
            {
                Debug.LogWarning("[SelfHealing] No checkpoints available for rollback");
                return null;
            }
            
            var checkpoint = checkpoints.Pop();
            Debug.Log($"[SelfHealing] Rolled back to: {checkpoint.GetSummary()}");
            
            return checkpoint;
        }
        
        /// <summary>
        /// Rollback to specific step index
        /// </summary>
        public WorkflowCheckpoint RollbackToStep(int targetStepIndex)
        {
            WorkflowCheckpoint target = null;
            
            while (checkpoints.Count > 0)
            {
                var checkpoint = checkpoints.Pop();
                if (checkpoint.StepIndex <= targetStepIndex)
                {
                    target = checkpoint;
                    break;
                }
            }
            
            if (target != null)
            {
                Debug.Log($"[SelfHealing] Rolled back to step {target.StepIndex}");
            }
            
            return target;
        }
        
        /// <summary>
        /// Decide how to heal from an error using AI
        /// </summary>
        public async Task<HealingDecision> DecideHowToHeal(
            TaskPlan plan,
            string errorMessage,
            string toolUsed,
            int currentStepIndex)
        {
            // Check retry count
            if (!stepRetryCount.ContainsKey(currentStepIndex))
            {
                stepRetryCount[currentStepIndex] = 0;
            }
            
            stepRetryCount[currentStepIndex]++;
            int retries = stepRetryCount[currentStepIndex];
            
            // If max retries exceeded, force different strategy
            if (retries >= MAX_RETRIES_PER_STEP)
            {
                Debug.LogWarning($"[SelfHealing] Step {currentStepIndex} failed {retries} times. Forcing alternative strategy.");
                
                return new HealingDecision
                {
                    Strategy = HealingStrategy.Skip,
                    Reason = $"Max retries ({MAX_RETRIES_PER_STEP}) exceeded for this step"
                };
            }
            
            // Ask AI for healing strategy
            var healingPrompt = $@"
A workflow step has failed. Analyze the error and recommend a recovery strategy.

STEP: {plan.SubTasks[currentStepIndex].Description}
TOOL USED: {toolUsed}
ERROR: {errorMessage}
RETRY COUNT: {retries}/{MAX_RETRIES_PER_STEP}

Available strategies:
1. RETRY - Try the same step again (maybe temporary issue)
2. SKIP - Skip this step and continue (if non-critical)
3. ROLLBACK - Go back to previous step (if dependency issue)
4. REPLAN - Revise entire plan (if fundamental issue)

Respond with ONLY ONE WORD: RETRY, SKIP, ROLLBACK, or REPLAN
";
            
            try
            {
                var response = await healingModel.SendMessageAsync(healingPrompt);
                var strategyStr = response.Trim().ToUpper();
                
                HealingStrategy strategy;
                if (strategyStr.Contains("RETRY"))
                    strategy = HealingStrategy.Retry;
                else if (strategyStr.Contains("SKIP"))
                    strategy = HealingStrategy.Skip;
                else if (strategyStr.Contains("ROLLBACK"))
                    strategy = HealingStrategy.Rollback;
                else if (strategyStr.Contains("REPLAN"))
                    strategy = HealingStrategy.Replan;
                else
                    strategy = HealingStrategy.Retry; // Default fallback
                
                Debug.Log($"[SelfHealing] AI recommends: {strategy}");
                
                return new HealingDecision
                {
                    Strategy = strategy,
                    Reason = $"AI analysis of error: {errorMessage.Substring(0, Math.Min(50, errorMessage.Length))}..."
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SelfHealing] Failed to get AI healing decision: {ex.Message}");
                
                // Fallback: Simple heuristic
                if (retries < 2)
                    return new HealingDecision { Strategy = HealingStrategy.Retry, Reason = "Fallback: retry" };
                else
                    return new HealingDecision { Strategy = HealingStrategy.Skip, Reason = "Fallback: skip" };
            }
        }
        
        /// <summary>
        /// Reset retry counter for a step (when it succeeds)
        /// </summary>
        public void ResetRetryCount(int stepIndex)
        {
            if (stepRetryCount.ContainsKey(stepIndex))
            {
                stepRetryCount[stepIndex] = 0;
            }
        }
        
        /// <summary>
        /// Clear all checkpoints
        /// </summary>
        public void ClearCheckpoints()
        {
            checkpoints.Clear();
            stepRetryCount.Clear();
            Debug.Log("[SelfHealing] All checkpoints cleared");
        }
        
        /// <summary>
        /// Get checkpoint history summary
        /// </summary>
        public string GetCheckpointHistory()
        {
            if (checkpoints.Count == 0)
            {
                return "No checkpoints saved";
            }
            
            var summary = $"Checkpoint History ({checkpoints.Count}):\n";
            var reversed = checkpoints.Reverse().ToList();
            
            for (int i = 0; i < reversed.Count; i++)
            {
                var cp = reversed[i];
                summary += $"  {i + 1}. {cp.GetSummary()} (age: {cp.GetAge().TotalSeconds:F1}s)\n";
            }
            
            return summary;
        }
    }
    
    /// <summary>
    /// Represents a healing decision
    /// </summary>
    public class HealingDecision
    {
        public HealingStrategy Strategy;
        public string Reason;
    }
    
    /// <summary>
    /// Available healing strategies
    /// </summary>
    public enum HealingStrategy
    {
        Retry,      // Try same step again
        Skip,       // Skip this step
        Rollback,   // Go back to previous step
        Replan      // Revise entire plan
    }
}

