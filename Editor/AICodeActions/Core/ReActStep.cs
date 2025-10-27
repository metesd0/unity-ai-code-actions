using System;
using System.Collections.Generic;
using System.Text;

namespace AICodeActions.Core
{
    /// <summary>
    /// Single step in ReAct (Reasoning + Acting) loop
    /// Thought → Action → Observation → Reflection
    /// </summary>
    [Serializable]
    public class ReActStep
    {
        public string thought;          // AI's reasoning
        public string action;            // Tool to execute
        public Dictionary<string, string> actionParams; // Tool parameters
        public string observation;       // Result of action
        public string reflection;        // Evaluation of result
        public bool isSuccessful;        // Did it work?
        public bool shouldContinue;      // Continue loop?
        public DateTime timestamp;
        
        public ReActStep()
        {
            actionParams = new Dictionary<string, string>();
            timestamp = DateTime.Now;
            shouldContinue = true;
        }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"💭 **Thought**: {thought}");
            sb.AppendLine($"⚡ **Action**: {action}");
            if (actionParams.Count > 0)
            {
                sb.AppendLine($"   Parameters: {string.Join(", ", actionParams.Keys)}");
            }
            sb.AppendLine($"👁️ **Observation**: {observation}");
            if (!string.IsNullOrEmpty(reflection))
            {
                sb.AppendLine($"🔍 **Reflection**: {reflection}");
            }
            sb.AppendLine($"✅ Status: {(isSuccessful ? "Success" : "Failed")}");
            return sb.ToString();
        }
        
        /// <summary>
        /// Create a compact summary for UI
        /// </summary>
        public string ToCompactString()
        {
            string statusIcon = isSuccessful ? "✅" : "❌";
            return $"{statusIcon} {action}: {reflection ?? observation}";
        }
    }
    
    /// <summary>
    /// Complete ReAct trajectory (sequence of steps)
    /// </summary>
    [Serializable]
    public class ReActTrajectory
    {
        public string taskDescription;
        public List<ReActStep> steps;
        public string finalResult;
        public bool isComplete;
        public int maxSteps;
        public DateTime startTime;
        public DateTime endTime;
        
        public ReActTrajectory(string task, int maxSteps = 10)
        {
            this.taskDescription = task;
            this.maxSteps = maxSteps;
            this.steps = new List<ReActStep>();
            this.isComplete = false;
            this.startTime = DateTime.Now;
        }
        
        public void AddStep(ReActStep step)
        {
            steps.Add(step);
            
            // Check if we should stop
            if (!step.shouldContinue || steps.Count >= maxSteps)
            {
                isComplete = true;
                endTime = DateTime.Now;
            }
        }
        
        public string GetSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🎯 **Task**: {taskDescription}");
            sb.AppendLine($"📊 **Steps**: {steps.Count}/{maxSteps}");
            sb.AppendLine($"⏱️ **Duration**: {(endTime - startTime).TotalSeconds:F2}s");
            sb.AppendLine($"✅ **Status**: {(isComplete ? "Complete" : "In Progress")}");
            sb.AppendLine();
            
            for (int i = 0; i < steps.Count; i++)
            {
                sb.AppendLine($"### Step {i + 1}:");
                sb.AppendLine(steps[i].ToString());
                sb.AppendLine();
            }
            
            if (!string.IsNullOrEmpty(finalResult))
            {
                sb.AppendLine("## 🎉 Final Result:");
                sb.AppendLine(finalResult);
            }
            
            return sb.ToString();
        }
        
        public string GetCompactSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🎯 {taskDescription}");
            sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            
            for (int i = 0; i < steps.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {steps[i].ToCompactString()}");
            }
            
            sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"⏱️ {(endTime - startTime).TotalSeconds:F1}s | {steps.Count} steps");
            
            return sb.ToString();
        }
    }
}

