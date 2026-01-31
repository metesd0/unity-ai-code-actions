using System;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers;
using UnityEngine;

namespace AICodeActions.Actions
{
    /// <summary>
    /// Explains selected code with Unity-specific insights
    /// </summary>
    public class ExplainCodeAction : CodeActionBase
    {
        public ExplainCodeAction(IModelProvider provider, ProjectContext context = null) 
            : base(provider, context)
        {
            Name = "Explain Code";
            Description = "Get a detailed explanation of selected code with Unity-specific insights";
            RequiresSelection = true;
        }

        public override async Task<CodeActionResult> ExecuteAsync(CodeActionInput input)
        {
            if (!CanExecute(input))
                return CodeActionResult.Failure("Cannot execute: No code selected or provider not configured");

            try
            {
                Debug.Log("[Explain Code] Building prompt...");
                string prompt = PromptBuilder.BuildExplainCodePrompt(input.selectedCode, input.additionalContext);

                Debug.Log("[Explain Code] Sending request to LLM...");
                string response = await provider.GenerateAsync(prompt, input.parameters);

                Debug.Log("[Explain Code] Explanation generated successfully");
                return new CodeActionResult
                {
                    success = true,
                    explanation = response,
                    originalCode = input.selectedCode
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Explain Code] Error: {e.Message}");
                return CodeActionResult.Failure($"Failed to explain code: {e.Message}");
            }
        }
    }
}

