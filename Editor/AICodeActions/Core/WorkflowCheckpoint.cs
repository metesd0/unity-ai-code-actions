using System;
using System.Collections.Generic;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Represents a saved state of the workflow for rollback capability
    /// </summary>
    [Serializable]
    public class WorkflowCheckpoint
    {
        public int StepIndex;
        public DateTime Timestamp;
        public string Description;
        
        // Saved state
        public TaskPlan PlanSnapshot;
        public Dictionary<string, object> ExecutionState;
        public List<string> CompletedSteps;
        
        public WorkflowCheckpoint()
        {
            Timestamp = DateTime.Now;
            ExecutionState = new Dictionary<string, object>();
            CompletedSteps = new List<string>();
        }
        
        public WorkflowCheckpoint(int stepIndex, TaskPlan plan, string description = "")
        {
            StepIndex = stepIndex;
            Timestamp = DateTime.Now;
            Description = description;
            
            // Deep copy plan
            PlanSnapshot = DeepCopyPlan(plan);
            
            ExecutionState = new Dictionary<string, object>();
            CompletedSteps = new List<string>();
            
            // Record completed steps
            for (int i = 0; i < plan.CurrentStep; i++)
            {
                if (i < plan.SubTasks.Count && plan.SubTasks[i].IsComplete)
                {
                    CompletedSteps.Add(plan.SubTasks[i].Description);
                }
            }
        }
        
        /// <summary>
        /// Create a deep copy of task plan (to avoid reference issues)
        /// </summary>
        private TaskPlan DeepCopyPlan(TaskPlan original)
        {
            var copy = new TaskPlan
            {
                MainGoal = original.MainGoal,
                CurrentStep = original.CurrentStep,
                StartTime = original.StartTime,
                CompletedTime = original.CompletedTime
            };
            
            // Copy sub-tasks
            foreach (var task in original.SubTasks)
            {
                var taskCopy = new SubTask
                {
                    Description = task.Description,
                    IsComplete = task.IsComplete,
                    IsFailed = task.IsFailed,
                    Result = task.Result,
                    CompletedTime = task.CompletedTime
                };
                
                taskCopy.RequiredTools.AddRange(task.RequiredTools);
                
                foreach (var param in task.SuggestedParameters)
                {
                    taskCopy.SuggestedParameters[param.Key] = param.Value;
                }
                
                copy.SubTasks.Add(taskCopy);
            }
            
            return copy;
        }
        
        /// <summary>
        /// Store custom execution state
        /// </summary>
        public void StoreState(string key, object value)
        {
            ExecutionState[key] = value;
        }
        
        /// <summary>
        /// Retrieve stored execution state
        /// </summary>
        public T GetState<T>(string key, T defaultValue = default)
        {
            if (ExecutionState.ContainsKey(key))
            {
                return (T)ExecutionState[key];
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Get summary of this checkpoint
        /// </summary>
        public string GetSummary()
        {
            return $"Checkpoint at step {StepIndex}: {Description} ({CompletedSteps.Count} steps completed)";
        }
        
        /// <summary>
        /// Calculate age of this checkpoint
        /// </summary>
        public TimeSpan GetAge()
        {
            return DateTime.Now - Timestamp;
        }
    }
}

