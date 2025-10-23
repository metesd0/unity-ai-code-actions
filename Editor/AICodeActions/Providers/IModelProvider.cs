using System;
using System.Threading.Tasks;

namespace AICodeActions.Providers
{
    /// <summary>
    /// Base interface for all LLM providers (OpenAI, Gemini, Ollama, etc.)
    /// </summary>
    public interface IModelProvider
    {
        string Name { get; }
        bool IsConfigured { get; }
        bool RequiresApiKey { get; }
        
        Task<string> GenerateAsync(string prompt, ModelParameters parameters = null);
        Task<bool> ValidateConnectionAsync();
    }

    [Serializable]
    public class ModelParameters
    {
        public float temperature = 0.7f;
        public int maxTokens = 2048;
        public float topP = 1.0f;
        public string model = "default";
    }

    [Serializable]
    public class ProviderConfig
    {
        public string apiKey = "";
        public string endpoint = "";
        public string model = "";
    }
}

