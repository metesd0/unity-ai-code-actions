using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers.Models;
using Newtonsoft.Json;
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
        private const string SYSTEM_PROMPT = "You are an expert Unity C# developer assistant.";

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

            string fullPrompt = $"{SYSTEM_PROMPT}\n\n{prompt}";

            var request = new OllamaRequest
            {
                Model = model,
                Prompt = fullPrompt,
                Stream = false,
                Options = new OllamaOptions
                {
                    Temperature = parameters.temperature,
                    NumPredict = parameters.maxTokens,
                    TopP = parameters.topP
                }
            };

            string jsonBody = JsonConvert.SerializeObject(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(config.endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = 120; // Local models can be slower

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Ollama Error: {webRequest.error}\nMake sure Ollama is running at {config.endpoint}");
                }

                return ParseResponse(webRequest.downloadHandler.text);
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
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

            try
            {
                var response = JsonConvert.DeserializeObject<OllamaResponse>(json);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    Debug.LogError($"[Ollama] API Error: {response.Error}");
                    return $"Error: {response.Error}";
                }

                string result = response.Response ?? "";
                Debug.Log($"[Ollama] Parsed text length: {result.Length} characters");
                return result;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[Ollama] JSON parse error: {ex.Message}");
                return $"Error: Failed to parse response - {ex.Message}";
            }
        }

        public Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Ollama streaming will be implemented in Phase 2");
        }
    }
}
