using System.Text;
using AICodeActions.Indexer;

namespace AICodeActions.Core
{
    /// <summary>
    /// Builds prompts with Unity-specific best practices and context
    /// </summary>
    public static class PromptBuilder
    {
        private const string UNITY_SYSTEM_PROMPT = @"You are an expert Unity C# developer assistant.

Follow these Unity best practices:
1. Avoid Update() for constant checks - prefer event-driven patterns
2. Use object pooling for frequently instantiated objects
3. Cache component references instead of repeated GetComponent calls
4. Use Burst-compilable code when possible for performance-critical paths
5. Properly serialize fields with [SerializeField] and use [Tooltip] for designer clarity
6. Avoid GC.Alloc in hot paths - use structs, arrays, and pooling
7. Follow Unity naming conventions (PascalCase for public, camelCase for private)
8. Use UnityEngine.Debug for logging, not System.Console
9. Implement proper null checks for Unity objects (they override == operator)
10. Use coroutines or async/await appropriately for delayed execution

Your responses should be:
- Clear, concise C# code
- Well-commented for clarity
- Production-ready and following best practices
- Properly formatted and indented";

        public static string BuildSystemPrompt()
        {
            return UNITY_SYSTEM_PROMPT;
        }

        public static string BuildGenerateScriptPrompt(string specification, ProjectContext context = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Generate a Unity C# script based on the following specification:");
            sb.AppendLine();
            sb.AppendLine(specification);
            sb.AppendLine();

            if (context != null && context.scripts.Count > 0)
            {
                sb.AppendLine("Project Context (for reference):");
                sb.AppendLine(context.BuildContextString(1000));
            }

            sb.AppendLine("Requirements:");
            sb.AppendLine("- Provide ONLY the C# code, no explanations");
            sb.AppendLine("- Include necessary using statements");
            sb.AppendLine("- Follow Unity best practices");
            sb.AppendLine("- Add [SerializeField] and [Tooltip] for designer-facing fields");
            sb.AppendLine("- Include XML documentation comments");

            return sb.ToString();
        }

        public static string BuildExplainCodePrompt(string code, string selectionContext = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Explain the following Unity C# code:");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(selectionContext))
            {
                sb.AppendLine("Context:");
                sb.AppendLine(selectionContext);
                sb.AppendLine();
            }

            sb.AppendLine("Please provide:");
            sb.AppendLine("1. High-level overview of what this code does");
            sb.AppendLine("2. Key Unity-specific patterns or features used");
            sb.AppendLine("3. Potential issues or improvements");
            sb.AppendLine("4. Performance considerations");

            return sb.ToString();
        }

        public static string BuildRefactorPrompt(string code, string refactorGoal)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Refactor the following Unity C# code to: {refactorGoal}");
            sb.AppendLine();
            sb.AppendLine("Original code:");
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("Requirements:");
            sb.AppendLine("- Provide ONLY the refactored C# code");
            sb.AppendLine("- Maintain the same functionality");
            sb.AppendLine("- Follow Unity best practices");
            sb.AppendLine("- Improve performance and readability");
            sb.AppendLine("- Add comments explaining significant changes");

            return sb.ToString();
        }

        public static string BuildGenerateTestPrompt(string code, string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Generate NUnit test cases for the following Unity C# class:");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("Requirements:");
            sb.AppendLine("- Use NUnit framework (Unity Test Framework)");
            sb.AppendLine("- Include both EditMode and PlayMode tests if applicable");
            sb.AppendLine("- Test public methods and edge cases");
            sb.AppendLine("- Use proper Unity test attributes ([Test], [UnityTest])");
            sb.AppendLine("- Include setup and teardown if needed");
            sb.AppendLine("- Provide ONLY the test class code");

            return sb.ToString();
        }

        public static string BuildFixPerformancePrompt(string code, string performanceIssue = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("Optimize the following Unity C# code for better performance:");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(performanceIssue))
            {
                sb.AppendLine($"Specific issue to address: {performanceIssue}");
                sb.AppendLine();
            }

            sb.AppendLine("Focus on:");
            sb.AppendLine("- Reducing GC allocations");
            sb.AppendLine("- Caching component references");
            sb.AppendLine("- Replacing Update() with event-driven patterns where appropriate");
            sb.AppendLine("- Using object pooling for instantiation");
            sb.AppendLine("- Burst-compatible code structures");
            sb.AppendLine();
            sb.AppendLine("Provide ONLY the optimized code with comments explaining changes.");

            return sb.ToString();
        }

        public static string BuildAddObjectPoolPrompt(string code, string instantiateLine)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Add object pooling to the following Unity C# code:");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(code);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine($"Focus on pooling this instantiation: {instantiateLine}");
            sb.AppendLine();
            sb.AppendLine("Requirements:");
            sb.AppendLine("- Create a simple, efficient object pool");
            sb.AppendLine("- Replace Instantiate/Destroy with Get/Return pool methods");
            sb.AppendLine("- Include pool initialization and cleanup");
            sb.AppendLine("- Add comments explaining the pooling logic");
            sb.AppendLine("- Provide the complete modified code");

            return sb.ToString();
        }
    }
}

