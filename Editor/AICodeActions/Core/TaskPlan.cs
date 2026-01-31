using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Represents a complex task broken down into sub-tasks
    /// </summary>
    [Serializable]
    public class TaskPlan
    {
        public string MainGoal;
        public List<SubTask> SubTasks = new List<SubTask>();
        public int CurrentStep = 0;
        public DateTime StartTime;
        public DateTime? CompletedTime;
        
        public bool IsComplete => CurrentStep >= SubTasks.Count;
        public float Progress => SubTasks.Count > 0 ? (float)CurrentStep / SubTasks.Count : 0f;
        public int CompletedTasksCount => SubTasks.Count(t => t.IsComplete);
        public int TotalTasksCount => SubTasks.Count;
        
        public SubTask CurrentTask => CurrentStep < SubTasks.Count ? SubTasks[CurrentStep] : null;
        public SubTask NextTask => CurrentStep + 1 < SubTasks.Count ? SubTasks[CurrentStep + 1] : null;
        
        public TaskPlan()
        {
            StartTime = DateTime.Now;
        }
        
        public void MarkCurrentStepComplete(string result = "")
        {
            if (CurrentStep < SubTasks.Count)
            {
                SubTasks[CurrentStep].IsComplete = true;
                SubTasks[CurrentStep].Result = result;
                SubTasks[CurrentStep].CompletedTime = DateTime.Now;
                CurrentStep++;
                
                if (IsComplete)
                {
                    CompletedTime = DateTime.Now;
                }
            }
        }
        
        public void MarkStepFailed(string error)
        {
            if (CurrentStep < SubTasks.Count)
            {
                SubTasks[CurrentStep].IsFailed = true;
                SubTasks[CurrentStep].Result = error;
            }
        }
        
        public string GetProgressSummary()
        {
            return $"{CompletedTasksCount}/{TotalTasksCount} tasks completed ({Progress:P0})";
        }
        
        public string GetDetailedStatus()
        {
            var status = $"🎯 Goal: {MainGoal}\n";
            status += $"📊 Progress: {GetProgressSummary()}\n\n";
            
            for (int i = 0; i < SubTasks.Count; i++)
            {
                var task = SubTasks[i];
                var icon = task.IsComplete ? "✅" : 
                          task.IsFailed ? "❌" : 
                          (i == CurrentStep ? "🔄" : "⏳");
                
                status += $"{icon} {i + 1}. {task.Description}\n";
                
                if (!string.IsNullOrEmpty(task.Result))
                {
                    status += $"   └─ {task.Result}\n";
                }
            }
            
            return status;
        }
    }
    
    /// <summary>
    /// Represents a single sub-task within a task plan
    /// </summary>
    [Serializable]
    public class SubTask
    {
        public string Description;
        public List<string> RequiredTools = new List<string>();
        public Dictionary<string, string> SuggestedParameters = new Dictionary<string, string>();
        public bool IsComplete = false;
        public bool IsFailed = false;
        public string Result = "";
        public DateTime? CompletedTime;
        
        public SubTask() { }
        
        public SubTask(string description, params string[] tools)
        {
            Description = description;
            RequiredTools.AddRange(tools);
        }
        
        public void AddParameter(string key, string value)
        {
            SuggestedParameters[key] = value;
        }
        
        public string GetParametersHint()
        {
            if (SuggestedParameters.Count == 0) return "";
            
            var hint = "Suggested parameters: ";
            hint += string.Join(", ", SuggestedParameters.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            return hint;
        }
    }
}

