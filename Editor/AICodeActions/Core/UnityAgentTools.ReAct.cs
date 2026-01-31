using System;
using System.Text;
using AICodeActions.Providers;

namespace AICodeActions.Core
{
    /// <summary>
    /// ReAct (Reasoning + Acting) tool wrappers
    /// </summary>
    public static partial class UnityAgentTools
    {
        private static ReActAgent reactAgent;
        
        /// <summary>
        /// Initialize ReAct agent
        /// </summary>
        private static void InitializeReActAgent(AgentToolSystem toolSystem, IModelProvider aiProvider)
        {
            if (reactAgent == null)
            {
                reactAgent = new ReActAgent(toolSystem, aiProvider);
            }
        }
        
        /// <summary>
        /// Execute task using ReAct loop
        /// </summary>
        public static string ExecuteWithReAct(string task, string maxSteps = "10")
        {
            try
            {
                // This will be called from AgentToolSystem, so we need to get the instance
                // For now, return a message that ReAct needs to be initialized
                
                if (reactAgent == null)
                {
                    return "⚠️ ReAct agent not initialized. Use execute_react_task from chat window.";
                }
                
                int steps = 10;
                if (int.TryParse(maxSteps, out int parsed))
                {
                    steps = parsed;
                }
                
                var trajectory = reactAgent.ExecuteTask(task, steps);
                
                return trajectory.GetSummary();
            }
            catch (Exception e)
            {
                return $"❌ ReAct execution error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get ReAct planning for a task (without executing)
        /// </summary>
        public static string PlanWithReAct(string task)
        {
            try
            {
                var plan = ReActPlanner.PlanTask(task);
                int complexity = ReActPlanner.EstimateComplexity(task);
                int recommendedSteps = ReActPlanner.GetRecommendedMaxSteps(task);
                
                var result = new StringBuilder();
                result.AppendLine($"📋 REACT EXECUTION PLAN");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine();
                result.AppendLine($"**Task**: {task}");
                result.AppendLine($"**Strategy**: {plan.strategy}");
                result.AppendLine($"**Complexity**: {complexity}/10");
                result.AppendLine($"**Recommended Steps**: {recommendedSteps}");
                result.AppendLine();
                result.AppendLine("## Execution Steps:");
                
                for (int i = 0; i < plan.steps.Count; i++)
                {
                    result.AppendLine($"{i + 1}. {plan.steps[i]}");
                }
                
                result.AppendLine();
                result.AppendLine("💡 Use `execute_with_react` to run this plan!");
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Planning error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Estimate task complexity
        /// </summary>
        public static string EstimateTaskComplexity(string task)
        {
            try
            {
                int complexity = ReActPlanner.EstimateComplexity(task);
                int recommendedSteps = ReActPlanner.GetRecommendedMaxSteps(task);
                
                var result = new StringBuilder();
                result.AppendLine($"📊 TASK COMPLEXITY ANALYSIS");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine();
                result.AppendLine($"**Task**: {task}");
                result.AppendLine($"**Complexity Score**: {complexity}/10");
                result.AppendLine();
                
                string rating = complexity switch
                {
                    <= 3 => "🟢 Simple - Should be quick",
                    <= 6 => "🟡 Moderate - May take several steps",
                    <= 8 => "🟠 Complex - Requires careful execution",
                    _ => "🔴 Very Complex - Will need multiple iterations"
                };
                
                result.AppendLine($"**Difficulty**: {rating}");
                result.AppendLine($"**Recommended Max Steps**: {recommendedSteps}");
                result.AppendLine($"**Estimated Time**: {EstimateTime(complexity)}");
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Estimate time based on complexity
        /// </summary>
        private static string EstimateTime(int complexity)
        {
            return complexity switch
            {
                <= 3 => "< 30 seconds",
                <= 6 => "30s - 2 minutes",
                <= 8 => "2 - 5 minutes",
                _ => "5+ minutes"
            };
        }
        
        /// <summary>
        /// Set ReAct agent (called from window)
        /// </summary>
        public static void SetReActAgent(ReActAgent agent)
        {
            reactAgent = agent;
        }
    }
}

