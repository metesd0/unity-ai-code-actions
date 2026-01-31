using System;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers;
using UnityEngine;

namespace AICodeActions.Actions
{
    /// <summary>
    /// Generates NUnit unit tests for selected code
    /// </summary>
    public class GenerateTestAction : CodeActionBase
    {
        public GenerateTestAction(IModelProvider provider, ProjectContext context = null) 
            : base(provider, context)
        {
            Name = "Generate Unit Test";
            Description = "Generate NUnit test cases for the selected code with EditMode and PlayMode tests";
            RequiresSelection = true;
        }

        public override async Task<CodeActionResult> ExecuteAsync(CodeActionInput input)
        {
            if (!CanExecute(input))
                return CodeActionResult.Failure("Cannot execute: No code selected or provider not configured");

            try
            {
                Debug.Log("[Generate Test] Building test generation prompt...");
                
                // Extract class name from code
                string className = ExtractClassName(input.selectedCode);
                
                string prompt = PromptBuilder.BuildGenerateTestPrompt(input.selectedCode, className);

                Debug.Log("[Generate Test] Sending request to LLM...");
                string response = await provider.GenerateAsync(prompt, input.parameters);

                string cleanedCode = CleanGeneratedCode(response);

                Debug.Log("[Generate Test] Test code generated successfully");
                return CodeActionResult.Success(
                    cleanedCode,
                    $"NUnit tests generated for {className}"
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[Generate Test] Error: {e.Message}");
                return CodeActionResult.Failure($"Failed to generate tests: {e.Message}");
            }
        }

        private string ExtractClassName(string code)
        {
            // Simple class name extraction
            var match = System.Text.RegularExpressions.Regex.Match(
                code, 
                @"class\s+(\w+)"
            );
            return match.Success ? match.Groups[1].Value : "UnknownClass";
        }

        private string CleanGeneratedCode(string response)
        {
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

