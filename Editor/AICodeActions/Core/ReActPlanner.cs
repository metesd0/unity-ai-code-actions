using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// ReAct Planner: Breaks complex tasks into steps
    /// Uses heuristics and pattern matching
    /// </summary>
    public static class ReActPlanner
    {
        /// <summary>
        /// Task plan (sequence of subtasks)
        /// </summary>
        public class TaskPlan
        {
            public string mainTask;
            public List<string> steps;
            public Dictionary<string, string[]> stepDependencies;
            public string strategy;
            
            public TaskPlan(string task)
            {
                this.mainTask = task;
                this.steps = new List<string>();
                this.stepDependencies = new Dictionary<string, string[]>();
            }
            
            public string ToPrompt()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# Task: {mainTask}");
                sb.AppendLine($"# Strategy: {strategy}");
                sb.AppendLine();
                sb.AppendLine("## Execution Plan:");
                
                for (int i = 0; i < steps.Count; i++)
                {
                    sb.AppendLine($"{i + 1}. {steps[i]}");
                    
                    if (stepDependencies.ContainsKey(steps[i]))
                    {
                        var deps = stepDependencies[steps[i]];
                        if (deps.Length > 0)
                        {
                            sb.AppendLine($"   ⚠️ Requires: {string.Join(", ", deps)}");
                        }
                    }
                }
                
                sb.AppendLine();
                sb.AppendLine("Now execute each step using ReAct loop (Thought → Action → Observation → Reflection)");
                
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Plan task execution based on task type
        /// </summary>
        public static TaskPlan PlanTask(string task)
        {
            var plan = new TaskPlan(task);
            
            // Normalize task
            string taskLower = task.ToLower();
            
            // Pattern matching for common tasks
            if (ContainsAny(taskLower, "create", "make", "add") && ContainsAny(taskLower, "player", "character", "fps", "controller"))
            {
                plan.strategy = "Player/Character Creation";
                plan.steps.AddRange(new[]
                {
                    "Create GameObject for player",
                    "Add required components (CharacterController, Rigidbody, etc.)",
                    "Create movement script",
                    "Create camera/look script",
                    "Attach scripts to player",
                    "Configure input system",
                    "Test basic movement"
                });
            }
            else if (ContainsAny(taskLower, "enemy", "ai", "npc") && ContainsAny(taskLower, "create", "make"))
            {
                plan.strategy = "AI/Enemy Creation";
                plan.steps.AddRange(new[]
                {
                    "Create GameObject for AI",
                    "Add NavMeshAgent component",
                    "Create AI behavior script",
                    "Set up patrol/chase logic",
                    "Attach scripts",
                    "Configure NavMesh",
                    "Test AI behavior"
                });
            }
            else if (ContainsAny(taskLower, "ui", "menu", "hud", "canvas"))
            {
                plan.strategy = "UI Creation";
                plan.steps.AddRange(new[]
                {
                    "Create Canvas",
                    "Add UI elements (buttons, text, panels)",
                    "Create UI controller script",
                    "Wire up button events",
                    "Apply styling",
                    "Test UI interactions"
                });
            }
            else if (ContainsAny(taskLower, "script", "code", "class") && ContainsAny(taskLower, "create", "write", "generate"))
            {
                plan.strategy = "Script Creation";
                plan.steps.AddRange(new[]
                {
                    "Analyze requirements",
                    "Design class structure",
                    "Generate script code",
                    "Validate syntax",
                    "Add to project",
                    "Test compilation"
                });
            }
            else if (ContainsAny(taskLower, "fix", "bug", "error", "issue", "problem"))
            {
                plan.strategy = "Bug Fix";
                plan.steps.AddRange(new[]
                {
                    "Identify the problem",
                    "Analyze error messages",
                    "Find root cause",
                    "Propose solution",
                    "Apply fix",
                    "Verify fix works",
                    "Test for side effects"
                });
            }
            else if (ContainsAny(taskLower, "refactor", "improve", "optimize", "clean"))
            {
                plan.strategy = "Code Improvement";
                plan.steps.AddRange(new[]
                {
                    "Analyze current code",
                    "Identify improvement areas",
                    "Plan refactoring",
                    "Apply changes incrementally",
                    "Validate still works",
                    "Check performance"
                });
            }
            else if (ContainsAny(taskLower, "scene", "level") && ContainsAny(taskLower, "create", "setup"))
            {
                plan.strategy = "Scene Setup";
                plan.steps.AddRange(new[]
                {
                    "Create new scene",
                    "Add lighting",
                    "Create terrain/ground",
                    "Add player spawn",
                    "Place objects",
                    "Configure scene settings",
                    "Save scene"
                });
            }
            else
            {
                // Generic task breakdown
                plan.strategy = "Generic Task Execution";
                plan.steps.AddRange(new[]
                {
                    "Understand task requirements",
                    "Gather necessary information",
                    "Plan approach",
                    "Execute main steps",
                    "Validate results",
                    "Handle any issues"
                });
            }
            
            Debug.Log($"[ReActPlanner] Planned strategy '{plan.strategy}' with {plan.steps.Count} steps");
            
            return plan;
        }
        
        /// <summary>
        /// Estimate task complexity (1-10)
        /// </summary>
        public static int EstimateComplexity(string task)
        {
            int complexity = 3; // Base complexity
            
            string taskLower = task.ToLower();
            
            // Increase complexity based on keywords
            if (ContainsAny(taskLower, "complex", "advanced", "multiple", "many"))
                complexity += 2;
            
            if (ContainsAny(taskLower, "fps", "multiplayer", "networking"))
                complexity += 3;
            
            if (ContainsAny(taskLower, "ai", "pathfinding", "machine learning"))
                complexity += 2;
            
            if (ContainsAny(taskLower, "physics", "ragdoll", "cloth"))
                complexity += 2;
            
            if (ContainsAny(taskLower, "ui", "menu", "hud"))
                complexity += 1;
            
            // Count "and" clauses (indicates multiple requirements)
            int andCount = System.Text.RegularExpressions.Regex.Matches(taskLower, @"\band\b").Count;
            complexity += andCount;
            
            return Math.Min(complexity, 10);
        }
        
        /// <summary>
        /// Get recommended max steps based on complexity
        /// </summary>
        public static int GetRecommendedMaxSteps(string task)
        {
            int complexity = EstimateComplexity(task);
            
            // Simple: 5 steps, Complex: 15 steps
            return 5 + (complexity * 1);
        }
        
        /// <summary>
        /// Check if string contains any of the keywords
        /// </summary>
        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(k => text.Contains(k));
        }
        
        /// <summary>
        /// Generate thought prompt for next step
        /// </summary>
        public static string GenerateThoughtPrompt(ReActTrajectory trajectory, string currentObjective)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🧠 REASONING STEP");
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"**Main Task**: {trajectory.taskDescription}");
            sb.AppendLine($"**Current Objective**: {currentObjective}");
            sb.AppendLine($"**Steps Completed**: {trajectory.steps.Count}/{trajectory.maxSteps}");
            sb.AppendLine();
            
            if (trajectory.steps.Count > 0)
            {
                sb.AppendLine("**Previous Steps**:");
                for (int i = Math.Max(0, trajectory.steps.Count - 3); i < trajectory.steps.Count; i++)
                {
                    var step = trajectory.steps[i];
                    sb.AppendLine($"  {i + 1}. {step.action} → {(step.isSuccessful ? "✅" : "❌")}");
                }
                sb.AppendLine();
            }
            
            sb.AppendLine("**What should I do next?**");
            sb.AppendLine("Think step by step:");
            sb.AppendLine("1. What did we accomplish so far?");
            sb.AppendLine("2. What's the next logical step?");
            sb.AppendLine("3. Which tool should I use?");
            sb.AppendLine("4. Are we done yet?");
            sb.AppendLine();
            sb.AppendLine("Respond with:");
            sb.AppendLine("[THOUGHT]");
            sb.AppendLine("Your reasoning here...");
            sb.AppendLine("[/THOUGHT]");
            sb.AppendLine();
            sb.AppendLine("[ACTION:tool_name]");
            sb.AppendLine("param1: value1");
            sb.AppendLine("[/ACTION]");
            
            return sb.ToString();
        }
    }
}

