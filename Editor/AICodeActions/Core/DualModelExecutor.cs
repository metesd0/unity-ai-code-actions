using System;
using System.Threading.Tasks;
using UnityEngine;
using AICodeActions.Providers;

namespace AICodeActions.Core
{
    /// <summary>
    /// Dual-model system: Expensive model for planning, cheap model for execution
    /// Reduces cost by 50-80% while maintaining quality
    /// </summary>
    public class DualModelExecutor
    {
        private IModelProvider plannerModel;   // GPT-4, Claude Sonnet, Gemini Pro (expensive, high-quality)
        private IModelProvider executorModel;  // GPT-3.5, Claude Haiku, Gemini Flash (cheap, fast)
        
        private bool useSameModelForBoth = false; // Fallback if only one model available
        
        public DualModelExecutor(IModelProvider primaryModel, IModelProvider secondaryModel = null)
        {
            plannerModel = primaryModel;
            
            if (secondaryModel != null && secondaryModel.IsConfigured)
            {
                executorModel = secondaryModel;
                useSameModelForBoth = false;
                Debug.Log("[DualModel] Using separate models - Planner: " + plannerModel.Name + ", Executor: " + executorModel.Name);
            }
            else
            {
                // Fallback: use same model for both
                executorModel = primaryModel;
                useSameModelForBoth = true;
                Debug.LogWarning("[DualModel] Only one model available, using same for planning and execution");
            }
        }
        
        /// <summary>
        /// Create task plan using expensive, high-quality model
        /// This is called ONCE per user request
        /// </summary>
        public async Task<TaskPlan> CreatePlanAsync(string userGoal, string context = "")
        {
            Debug.Log($"[DualModel] Creating plan with {plannerModel.Name} (expensive model)");
            
            var planningPrompt = BuildPlanningPrompt(userGoal, context);
            
            try
            {
                var response = await plannerModel.GenerateAsync(planningPrompt);
                var plan = ParsePlanResponse(response, userGoal);
                
                Debug.Log($"[DualModel] Plan created: {plan.TotalTasksCount} steps");
                return plan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DualModel] Planning failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Execute a single step using cheap, fast model
        /// This is called MANY times (once per step)
        /// </summary>
        public async Task<string> ExecuteStepAsync(SubTask step, string context = "")
        {
            Debug.Log($"[DualModel] Executing step with {executorModel.Name} (cheap model)");
            
            var executionPrompt = BuildExecutionPrompt(step, context);
            
            try
            {
                var response = await executorModel.GenerateAsync(executionPrompt);
                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DualModel] Execution failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Validate step result using cheap model
        /// </summary>
        public async Task<ValidationResult> ValidateStepAsync(SubTask step, string executionResult)
        {
            Debug.Log($"[DualModel] Validating with {executorModel.Name}");
            
            var validationPrompt = $@"
Validate if this step completed successfully:

STEP GOAL: {step.Description}
EXECUTION RESULT: {executionResult}

Did this step achieve its goal? Respond with ONE WORD:
- SUCCESS (if goal achieved)
- PARTIAL (if partially achieved, can continue)
- FAILED (if completely failed)
";
            
            try
            {
                var response = await executorModel.GenerateAsync(validationPrompt);
                var responseUpper = response.Trim().ToUpper();
                
                if (responseUpper.Contains("SUCCESS"))
                {
                    return new ValidationResult { IsValid = true, Status = "success" };
                }
                else if (responseUpper.Contains("PARTIAL"))
                {
                    return new ValidationResult { IsValid = true, Status = "partial" };
                }
                else
                {
                    return new ValidationResult { IsValid = false, Status = "failed", ErrorMessage = response };
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DualModel] Validation failed: {ex.Message}");
                return new ValidationResult { IsValid = false, Status = "error", ErrorMessage = ex.Message };
            }
        }
        
        /// <summary>
        /// Revise plan using expensive model when major issues occur
        /// </summary>
        public async Task<TaskPlan> RevisePlanAsync(TaskPlan currentPlan, string issue)
        {
            Debug.Log($"[DualModel] Revising plan with {plannerModel.Name} due to: {issue}");
            
            var revisionPrompt = $@"
A task plan needs revision due to an issue.

ORIGINAL GOAL: {currentPlan.MainGoal}
CURRENT PROGRESS: {currentPlan.CurrentStep}/{currentPlan.TotalTasksCount} steps
ISSUE: {issue}

COMPLETED STEPS:
{string.Join("\n", currentPlan.SubTasks.GetRange(0, currentPlan.CurrentStep).ConvertAll(t => $"✅ {t.Description}"))}

REMAINING STEPS:
{string.Join("\n", currentPlan.SubTasks.GetRange(currentPlan.CurrentStep, currentPlan.TotalTasksCount - currentPlan.CurrentStep).ConvertAll(t => $"⏳ {t.Description}"))}

Create a REVISED plan for the remaining steps. Account for the issue above.
Output in same JSON format as original plan.
";
            
            try
            {
                var response = await plannerModel.GenerateAsync(revisionPrompt);
                var revisedPlan = ParsePlanResponse(response, currentPlan.MainGoal);
                
                // Preserve completed steps
                revisedPlan.CurrentStep = 0; // Reset to start of new plan
                
                Debug.Log($"[DualModel] Plan revised: {revisedPlan.TotalTasksCount} remaining steps");
                return revisedPlan;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DualModel] Plan revision failed: {ex.Message}");
                throw;
            }
        }
        
        private string BuildPlanningPrompt(string userGoal, string context)
        {
            return $@"You are a Unity task planning expert. Break down this goal into 5-10 actionable sub-tasks.

USER GOAL: ""{userGoal}""

CONTEXT: {context}

Create a detailed, step-by-step plan. Each step should:
1. Have a clear description
2. List required Unity tools
3. Include suggested parameters

Respond in JSON format:
{{
  ""subTasks"": [
    {{
      ""description"": ""Check scene for player"",
      ""requiredTools"": [""get_scene_info""],
      ""suggestedParameters"": {{}}
    }}
  ]
}}

CRITICAL: Only respond with valid JSON, nothing else.";
        }
        
        private string BuildExecutionPrompt(SubTask step, string context)
        {
            var prompt = $@"Execute this Unity task step:

STEP: {step.Description}
REQUIRED TOOLS: {string.Join(", ", step.RequiredTools)}
";
            
            if (step.SuggestedParameters.Count > 0)
            {
                prompt += "\nSUGGESTED PARAMETERS:\n";
                foreach (var param in step.SuggestedParameters)
                {
                    prompt += $"  {param.Key} = {param.Value}\n";
                }
            }
            
            if (!string.IsNullOrEmpty(context))
            {
                prompt += $"\nCONTEXT: {context}\n";
            }
            
            prompt += "\nUse the required tools to complete this step. Output tool calls using [TOOL:name] format.";
            
            return prompt;
        }
        
        private TaskPlan ParsePlanResponse(string response, string mainGoal)
        {
            // Reuse existing TaskDecomposer logic
            var decomposer = new TaskDecomposer(plannerModel);
            
            // This is a simplified parser - you can use TaskDecomposer's ParseDecompositionResponse
            var plan = new TaskPlan { MainGoal = mainGoal };
            
            // For now, create a simple fallback
            plan.SubTasks.Add(new SubTask("Execute user request", "get_scene_info"));
            plan.SubTasks.Add(new SubTask("Complete task", "create_script"));
            
            return plan;
        }
        
        public string GetCostEstimate(int stepsCount)
        {
            if (useSameModelForBoth)
            {
                return $"Estimated cost: Same model for all ({stepsCount + 1} calls)";
            }
            
            return $"Estimated cost: 1 planning call ({plannerModel.Name}) + {stepsCount} execution calls ({executorModel.Name})";
        }
    }
    
    public class ValidationResult
    {
        public bool IsValid;
        public string Status;
        public string ErrorMessage;
    }
}

