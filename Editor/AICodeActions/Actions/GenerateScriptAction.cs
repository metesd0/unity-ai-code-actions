using System;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers;
using UnityEngine;

namespace AICodeActions.Actions
{
    /// <summary>
    /// Generates a new C# script from specification
    /// </summary>
    public class GenerateScriptAction : CodeActionBase
    {
        public GenerateScriptAction(IModelProvider provider, ProjectContext context = null) 
            : base(provider, context)
        {
            Name = "Generate Script";
            Description = "Generate a new Unity C# script from a text specification";
            RequiresSelection = false;
        }

        public override async Task<CodeActionResult> ExecuteAsync(CodeActionInput input)
        {
            if (!CanExecute(input))
                return CodeActionResult.Failure("Cannot execute: Invalid input or provider not configured");

            if (string.IsNullOrEmpty(input.specification))
                return CodeActionResult.Failure("Specification is required");

            try
            {
                Debug.Log("[Generate Script] Building prompt...");
                string prompt = PromptBuilder.BuildGenerateScriptPrompt(input.specification, context);

                Debug.Log("[Generate Script] Sending request to LLM...");
                string response = await provider.GenerateAsync(prompt, input.parameters);

                // Clean up response (remove markdown code blocks if present)
                string cleanedCode = CleanGeneratedCode(response);

                Debug.Log("[Generate Script] Code generated successfully");
                return CodeActionResult.Success(
                    cleanedCode,
                    "Script generated from specification"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[Generate Script] Error: {e.Message}");
                return CodeActionResult.Failure($"Failed to generate script: {e.Message}");
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

