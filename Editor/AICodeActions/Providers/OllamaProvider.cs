using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;
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
        public bool SupportsStreaming => false; // TODO: Implement later

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
            Debug.Log($"[Ollama] Raw Response Length: {json.Length}");
            
            int responseStart = json.IndexOf("\"response\":");
            if (responseStart == -1)
            {
                Debug.LogError("[Ollama] Could not find 'response' field in response");
                return "Error: Could not parse response";
            }

            responseStart = json.IndexOf("\"", responseStart + 11) + 1;
            
            // Find the closing quote, accounting for escaped quotes
            int responseEnd = responseStart;
            bool escaped = false;
            
            while (responseEnd < json.Length)
            {
                if (json[responseEnd] == '\\' && !escaped)
                {
                    escaped = true;
                    responseEnd++;
                    continue;
                }
                
                if (json[responseEnd] == '"' && !escaped)
                {
                    break;
                }
                
                escaped = false;
                responseEnd++;
            }
            
            if (responseEnd >= json.Length)
            {
                Debug.LogError("[Ollama] Could not find end of response field");
                return "Error: Incomplete response";
            }
            
            string result = json.Substring(responseStart, responseEnd - responseStart)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
            
            Debug.Log($"[Ollama] Parsed text length: {result.Length} characters");
            return result;
        }

        public Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Ollama streaming will be implemented in Phase 2");
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

