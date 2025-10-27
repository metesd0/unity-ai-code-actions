using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using AICodeActions.Providers;

namespace AICodeActions.Core
{
    /// <summary>
    /// ReAct Agent: Reasoning + Acting in a loop
    /// Implements: Think → Act → Observe → Reflect → Repeat
    /// </summary>
    public class ReActAgent
    {
        private AgentToolSystem toolSystem;
        private IModelProvider aiProvider;
        private SelfCorrectionEngine selfCorrection;
        private bool useSelfCorrection = true;
        
        public ReActAgent(AgentToolSystem toolSystem, IModelProvider aiProvider, bool enableSelfCorrection = true)
        {
            this.toolSystem = toolSystem;
            this.aiProvider = aiProvider;
            this.useSelfCorrection = enableSelfCorrection;
            
            if (enableSelfCorrection)
            {
                this.selfCorrection = new SelfCorrectionEngine(toolSystem, maxRetries: 2);
            }
        }
        
        /// <summary>
        /// Execute task using ReAct loop
        /// </summary>
        public ReActTrajectory ExecuteTask(string task, int maxSteps = 0, Action<string> progressCallback = null)
        {
            try
            {
                // Plan the task
                var plan = ReActPlanner.PlanTask(task);
                
                if (maxSteps == 0)
                {
                    maxSteps = ReActPlanner.GetRecommendedMaxSteps(task);
                }
                
                var trajectory = new ReActTrajectory(task, maxSteps);
                
                progressCallback?.Invoke($"🎯 Task: {task}\n📋 Strategy: {plan.strategy}\n⏳ Starting ReAct loop...\n");
                
                // Main ReAct loop
                for (int stepNum = 0; stepNum < maxSteps; stepNum++)
                {
                    progressCallback?.Invoke($"\n━━━ Step {stepNum + 1}/{maxSteps} ━━━\n");
                    
                    // Determine current objective
                    string currentObjective = stepNum < plan.steps.Count 
                        ? plan.steps[stepNum] 
                        : "Complete remaining work";
                    
                    // Execute one ReAct step
                    var step = ExecuteReActStep(trajectory, currentObjective, progressCallback);
                    trajectory.AddStep(step);
                    
                    // Check if we're done
                    if (!step.shouldContinue)
                    {
                        progressCallback?.Invoke("\n✅ Task completed!\n");
                        break;
                    }
                    
                    // Check if step failed critically
                    if (!step.isSuccessful && step.reflection.Contains("cannot continue"))
                    {
                        progressCallback?.Invoke("\n❌ Critical error, stopping.\n");
                        break;
                    }
                }
                
                trajectory.endTime = DateTime.Now;
                trajectory.isComplete = true;
                
                // Generate final result
                trajectory.finalResult = GenerateFinalResult(trajectory);
                
                progressCallback?.Invoke($"\n{trajectory.GetCompactSummary()}\n");
                
                return trajectory;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReActAgent] Error executing task: {e}");
                
                var errorTrajectory = new ReActTrajectory(task, 1);
                errorTrajectory.finalResult = $"❌ Error: {e.Message}";
                errorTrajectory.isComplete = true;
                errorTrajectory.endTime = DateTime.Now;
                
                return errorTrajectory;
            }
        }
        
        /// <summary>
        /// Execute single ReAct step: Thought → Action → Observation → Reflection
        /// </summary>
        private ReActStep ExecuteReActStep(ReActTrajectory trajectory, string currentObjective, Action<string> progressCallback)
        {
            var step = new ReActStep();
            
            try
            {
                // === 1. THOUGHT (Reasoning) ===
                progressCallback?.Invoke("💭 Thinking...\n");
                
                step.thought = GenerateThought(trajectory, currentObjective);
                progressCallback?.Invoke($"💭 {step.thought}\n\n");
                
                // === 2. ACTION (Tool execution) ===
                progressCallback?.Invoke("⚡ Acting...\n");
                
                var (action, parameters) = ParseAction(step.thought);
                step.action = action;
                step.actionParams = parameters;
                
                if (string.IsNullOrEmpty(action))
                {
                    // No tool, just reasoning
                    step.observation = "No action required at this step.";
                    step.isSuccessful = true;
                    step.shouldContinue = !step.thought.Contains("DONE") && !step.thought.Contains("COMPLETE");
                }
                else
                {
                    progressCallback?.Invoke($"⚡ Tool: {action}\n");
                    
                    // Execute tool with self-correction if enabled
                    if (useSelfCorrection && selfCorrection != null)
                    {
                        step.observation = selfCorrection.ExecuteWithCorrection(action, parameters, 
                            (msg) => progressCallback?.Invoke($"   {msg}"));
                    }
                    else
                    {
                        step.observation = toolSystem.ExecuteTool(action, parameters);
                    }
                    
                    step.isSuccessful = step.observation.Contains("✅");
                    
                    progressCallback?.Invoke($"👁️ {GetCompactObservation(step.observation)}\n\n");
                }
                
                // === 3. REFLECTION (Evaluate) ===
                progressCallback?.Invoke("🔍 Reflecting...\n");
                
                step.reflection = GenerateReflection(step, trajectory, currentObjective);
                progressCallback?.Invoke($"🔍 {step.reflection}\n");
                
                // Determine if should continue
                if (step.reflection.Contains("task complete") || 
                    step.reflection.Contains("all done") ||
                    step.reflection.Contains("successfully completed"))
                {
                    step.shouldContinue = false;
                }
                
                return step;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ReActStep] Error: {e}");
                
                step.observation = $"❌ Error: {e.Message}";
                step.reflection = "Error occurred, cannot continue.";
                step.isSuccessful = false;
                step.shouldContinue = false;
                
                return step;
            }
        }
        
        /// <summary>
        /// Generate thought using AI
        /// </summary>
        private string GenerateThought(ReActTrajectory trajectory, string currentObjective)
        {
            // Build context
            var context = new StringBuilder();
            context.AppendLine("You are a Unity development assistant using ReAct (Reasoning + Acting).");
            context.AppendLine();
            context.AppendLine($"MAIN TASK: {trajectory.taskDescription}");
            context.AppendLine($"CURRENT OBJECTIVE: {currentObjective}");
            context.AppendLine();
            
            if (trajectory.steps.Count > 0)
            {
                context.AppendLine("PREVIOUS STEPS:");
                for (int i = Math.Max(0, trajectory.steps.Count - 2); i < trajectory.steps.Count; i++)
                {
                    var prevStep = trajectory.steps[i];
                    context.AppendLine($"  Step {i + 1}: {prevStep.action} → {(prevStep.isSuccessful ? "SUCCESS" : "FAILED")}");
                    if (!string.IsNullOrEmpty(prevStep.reflection))
                    {
                        context.AppendLine($"    Reflection: {prevStep.reflection}");
                    }
                }
                context.AppendLine();
            }
            
            context.AppendLine("THINK STEP BY STEP:");
            context.AppendLine("1. What have we accomplished?");
            context.AppendLine("2. What's the next logical action?");
            context.AppendLine("3. Should I use a tool or just reason?");
            context.AppendLine();
            context.AppendLine("If you need to use a tool, format like this:");
            context.AppendLine("[TOOL:tool_name]");
            context.AppendLine("param1: value1");
            context.AppendLine("[/TOOL]");
            context.AppendLine();
            context.AppendLine("If task is complete, say 'TASK COMPLETE' in your response.");
            
            // Simple thought generation (can be replaced with AI call)
            // For now, use heuristic-based reasoning
            return GenerateHeuristicThought(trajectory, currentObjective);
        }
        
        /// <summary>
        /// Generate heuristic thought (rule-based, no AI needed)
        /// </summary>
        private string GenerateHeuristicThought(ReActTrajectory trajectory, string objective)
        {
            var thought = new StringBuilder();
            
            // Analyze objective
            string objLower = objective.ToLower();
            
            if (objLower.Contains("create gameobject") || objLower.Contains("create object"))
            {
                thought.AppendLine("Need to create a GameObject.");
                thought.AppendLine("I'll use create_gameobject tool.");
                thought.AppendLine("[TOOL:create_gameobject]");
                thought.AppendLine($"name: {ExtractEntityName(objective)}");
                thought.AppendLine("[/TOOL]");
            }
            else if (objLower.Contains("add component") || objLower.Contains("attach component"))
            {
                thought.AppendLine("Need to add a component to a GameObject.");
                thought.AppendLine("I'll use add_component tool.");
            }
            else if (objLower.Contains("script") || objLower.Contains("code"))
            {
                thought.AppendLine("Need to create a script.");
                thought.AppendLine("I'll use create_and_attach_script tool.");
            }
            else if (objLower.Contains("test") || objLower.Contains("verify") || objLower.Contains("validate"))
            {
                thought.AppendLine("Need to verify the work is complete.");
                thought.AppendLine("Let me check the scene state.");
                thought.AppendLine("[TOOL:get_scene_info]");
                thought.AppendLine("[/TOOL]");
            }
            else
            {
                // Generic reasoning
                thought.AppendLine($"Working on: {objective}");
                thought.AppendLine("Determining best approach...");
            }
            
            return thought.ToString();
        }
        
        /// <summary>
        /// Parse action from thought
        /// </summary>
        private (string action, Dictionary<string, string> parameters) ParseAction(string thought)
        {
            // Look for [TOOL:name]...[/TOOL] pattern
            var match = Regex.Match(thought, @"\[TOOL:(\w+)\](.*?)\[/TOOL\]", RegexOptions.Singleline);
            
            if (!match.Success)
            {
                return (null, new Dictionary<string, string>());
            }
            
            string toolName = match.Groups[1].Value;
            string paramSection = match.Groups[2].Value;
            
            // Parse parameters
            var parameters = new Dictionary<string, string>();
            var paramLines = paramSection.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in paramLines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = line.Substring(0, colonIndex).Trim();
                    string value = line.Substring(colonIndex + 1).Trim();
                    parameters[key] = value;
                }
            }
            
            return (toolName, parameters);
        }
        
        /// <summary>
        /// Generate reflection on step result
        /// </summary>
        private string GenerateReflection(ReActStep step, ReActTrajectory trajectory, string objective)
        {
            if (step.isSuccessful)
            {
                if (string.IsNullOrEmpty(step.action))
                {
                    return "Reasoning step complete. Ready for next action.";
                }
                else
                {
                    return $"✅ {step.action} succeeded. Moving to next step.";
                }
            }
            else
            {
                return $"⚠️ {step.action} failed. May need to retry or adjust approach.";
            }
        }
        
        /// <summary>
        /// Generate final result summary
        /// </summary>
        private string GenerateFinalResult(ReActTrajectory trajectory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 🎉 ReAct Execution Complete");
            sb.AppendLine();
            sb.AppendLine($"**Task**: {trajectory.taskDescription}");
            sb.AppendLine($"**Steps Executed**: {trajectory.steps.Count}");
            
            int successCount = trajectory.steps.Count(s => s.isSuccessful);
            sb.AppendLine($"**Success Rate**: {successCount}/{trajectory.steps.Count}");
            sb.AppendLine($"**Duration**: {(trajectory.endTime - trajectory.startTime).TotalSeconds:F2}s");
            sb.AppendLine();
            
            sb.AppendLine("## Key Actions Taken:");
            foreach (var step in trajectory.steps)
            {
                if (!string.IsNullOrEmpty(step.action))
                {
                    string icon = step.isSuccessful ? "✅" : "❌";
                    sb.AppendLine($"- {icon} {step.action}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Get compact observation (first 100 chars)
        /// </summary>
        private string GetCompactObservation(string observation)
        {
            if (observation.Length <= 100)
                return observation;
            
            return observation.Substring(0, 100) + "...";
        }
        
        /// <summary>
        /// Extract entity name from objective
        /// </summary>
        private string ExtractEntityName(string objective)
        {
            // Simple heuristic: look for quoted strings or capitalized words
            var match = Regex.Match(objective, @"[""']([^""']+)[""']");
            if (match.Success)
                return match.Groups[1].Value;
            
            // Look for "for X" or "named X"
            match = Regex.Match(objective, @"(?:for|named)\s+(\w+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value;
            
            return "NewObject";
        }
    }
}

