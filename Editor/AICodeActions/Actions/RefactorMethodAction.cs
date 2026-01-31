using System;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers;
using UnityEngine;

namespace AICodeActions.Actions
{
    /// <summary>
    /// Refactors selected code based on specified goal
    /// </summary>
    public class RefactorMethodAction : CodeActionBase
    {
        public RefactorMethodAction(IModelProvider provider, ProjectContext context = null) 
            : base(provider, context)
        {
            Name = "Refactor Code";
            Description = "Refactor selected code to improve readability, performance, or structure";
            RequiresSelection = true;
        }

        public override async Task<CodeActionResult> ExecuteAsync(CodeActionInput input)
        {
            if (!CanExecute(input))
                return CodeActionResult.Failure("Cannot execute: No code selected or provider not configured");

            string refactorGoal = !string.IsNullOrEmpty(input.specification) 
                ? input.specification 
                : "improve code quality, readability and performance";

            try
            {
                Debug.Log($"[Refactor] Building prompt with goal: {refactorGoal}");
                string prompt = PromptBuilder.BuildRefactorPrompt(input.selectedCode, refactorGoal);

                Debug.Log("[Refactor] Sending request to LLM...");
                string response = await provider.GenerateAsync(prompt, input.parameters);

                // Clean up response
                string cleanedCode = CleanGeneratedCode(response);

                Debug.Log("[Refactor] Refactored code generated successfully");
                return new CodeActionResult
                {
                    success = true,
                    generatedCode = cleanedCode,
                    originalCode = input.selectedCode,
                    explanation = $"Refactored to: {refactorGoal}"
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[Refactor] Error: {e.Message}");
                return CodeActionResult.Failure($"Failed to refactor code: {e.Message}");
            }
        }

        private string CleanGeneratedCode(string response)
        {
            // Remove markdown code blocks
            string cleaned = response.Trim();

            if (cleaned.StartsWith("```csharp") || cleaned.StartsWith("```c#"))
            {
                int firstNewline = cleaned.IndexOf('\n');
                if (firstNewline > 0)
                    cleaned = cleaned.Substring(firstNewline + 1);
            }
            else if (cleaned.StartsWith("```"))
            {
                int firstNewline = cleaned.IndexOf('\n');
                if (firstNewline > 0)
                    cleaned = cleaned.Substring(firstNewline + 1);
            }

            if (cleaned.EndsWith("```"))
            {
                int lastBacktick = cleaned.LastIndexOf("```");
                cleaned = cleaned.Substring(0, lastBacktick);
            }

            return cleaned.Trim();
        }
    }
}

