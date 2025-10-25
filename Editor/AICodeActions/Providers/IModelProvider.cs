using System;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;

namespace AICodeActions.Providers
{
    /// <summary>
    /// Base interface for all LLM providers (OpenAI, Gemini, Ollama, etc.)
    /// NOW WITH STREAMING SUPPORT! 🚀
    /// </summary>
    public interface IModelProvider
    {
        string Name { get; }
        bool IsConfigured { get; }
        bool RequiresApiKey { get; }
        
        /// <summary>
        /// Whether this provider supports streaming responses
        /// </summary>
        bool SupportsStreaming { get; }
        
        /// <summary>
        /// Generate response (legacy, non-streaming)
        /// </summary>
        Task<string> GenerateAsync(string prompt, ModelParameters parameters = null);
        
        /// <summary>
        /// Generate response with streaming support
        /// Calls onChunk for each received chunk
        /// </summary>
        Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default);
        
        Task<bool> ValidateConnectionAsync();
    }

    [Serializable]
    public class ModelParameters
    {
        public float temperature = 0.7f;
        public int maxTokens = 2048;
        public float topP = 1.0f;
        public string model = "default";
        
        // OpenRouter Reasoning Tokens support
        public string reasoningEffort = null; // "low", "medium", "high", or null to disable
        public int? reasoningMaxTokens = null; // For Anthropic-style models
        public bool reasoningExclude = false; // Hide reasoning from response
        
        // Gemini Thinking support
        public int? thinkingBudget = null; // -1: dynamic, 0: off, 128-32768: token budget (null: default)
    }

    [Serializable]
    public class ProviderConfig
    {
        public string apiKey = "";
        public string endpoint = "";
        public string model = "";
    }
}

