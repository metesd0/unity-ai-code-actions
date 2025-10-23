using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    /// <summary>
    /// Local LLM provider via Ollama
    /// Supports offline mode - no API key required
    /// </summary>
    public class OllamaProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "http://localhost:11434/api/generate";
        private const string DEFAULT_MODEL = "mistral";

        private ProviderConfig config;

        public string Name => "Ollama (Local)";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.endpoint);
        public bool RequiresApiKey => false;

        public OllamaProvider(ProviderConfig config)
        {
            this.config = config ?? new ProviderConfig();
            if (string.IsNullOrEmpty(this.config.endpoint))
                this.config.endpoint = DEFAULT_ENDPOINT;
            if (string.IsNullOrEmpty(this.config.model))
                this.config.model = DEFAULT_MODEL;
        }

        public async Task<string> GenerateAsync(string prompt, ModelParameters parameters = null)
        {
            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default" 
                ? config.model 
                : parameters.model;

            string systemPrompt = "You are an expert Unity C# developer assistant.";
            string fullPrompt = $"{systemPrompt}\n\n{prompt}";

            string jsonBody = $@"{{
                ""model"": ""{model}"",
                ""prompt"": {EscapeJson(fullPrompt)},
                ""stream"": false,
                ""options"": {{
                    ""temperature"": {parameters.temperature.ToString(CultureInfo.InvariantCulture)},
                    ""num_predict"": {parameters.maxTokens},
                    ""top_p"": {parameters.topP.ToString(CultureInfo.InvariantCulture)}
                }}
            }}";

            using (UnityWebRequest request = new UnityWebRequest(config.endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 120; // Local models can be slower

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Ollama Error: {request.error}\nMake sure Ollama is running at {config.endpoint}");
                }

                return ParseResponse(request.downloadHandler.text);
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                // Try to connect to Ollama server
                string healthUrl = config.endpoint.Replace("/api/generate", "");
                using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
                {
                    request.timeout = 5;
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await Task.Yield();

                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        private string ParseResponse(string json)
        {
            // Ollama response format: {"response": "text"}
            int responseStart = json.IndexOf("\"response\":");
            if (responseStart == -1) return "Error parsing response";

            responseStart = json.IndexOf("\"", responseStart + 11) + 1;
            int responseEnd = json.IndexOf("\"", responseStart);
            
            return json.Substring(responseStart, responseEnd - responseStart)
                .Replace("\\n", "\n")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"");
        }

        private string EscapeJson(string text)
        {
            return "\"" + text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t") + "\"";
        }
    }
}

