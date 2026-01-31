using System;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers;
using UnityEngine;

namespace AICodeActions.Actions
{
    /// <summary>
    /// Base class for all code actions
    /// </summary>
    public abstract class CodeActionBase
    {
        protected IModelProvider provider;
        protected ProjectContext context;

        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool RequiresSelection { get; protected set; }

        protected CodeActionBase(IModelProvider provider, ProjectContext context = null)
        {
            this.provider = provider;
            this.context = context;
        }

        /// <summary>
        /// Execute the action
        /// </summary>
        public abstract Task<CodeActionResult> ExecuteAsync(CodeActionInput input);

        /// <summary>
        /// Validate if the action can be executed
        /// </summary>
        public virtual bool CanExecute(CodeActionInput input)
        {
            if (provider == null || !provider.IsConfigured)
            {
                Debug.LogWarning($"[{Name}] Provider not configured");
                return false;
            }

            if (RequiresSelection && string.IsNullOrEmpty(input.selectedCode))
            {
                Debug.LogWarning($"[{Name}] Requires code selection");
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class CodeActionInput
    {
        public string specification;
        public string selectedCode;
        public string filePath;
        public int selectionStartLine;
        public int selectionEndLine;
        public string additionalContext;
        public ModelParameters parameters;

        public CodeActionInput()
        {
            parameters = new ModelParameters();
        }
    }

    [Serializable]
    public class CodeActionResult
    {
        public bool success;
        public string generatedCode;
        public string explanation;
        public string error;
        public string originalCode;
        public int tokensUsed;
        public float estimatedCost;

        public static CodeActionResult Success(string code, string explanation = "")
        {
            return new CodeActionResult
            {
                success = true,
                generatedCode = code,
                explanation = explanation
            };
        }

        public static CodeActionResult Failure(string error)
        {
            return new CodeActionResult
            {
                success = false,
                error = error
            };
        }
    }
}

