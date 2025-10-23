using System;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    public class OpenAIProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string DEFAULT_MODEL = "gpt-4";

        private ProviderConfig config;

        public string Name => "OpenAI";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.apiKey);
        public bool RequiresApiKey => true;

        public OpenAIProvider(ProviderConfig config)
        {
            this.config = config ?? new ProviderConfig();
            if (string.IsNullOrEmpty(this.config.endpoint))
                this.config.endpoint = DEFAULT_ENDPOINT;
            if (string.IsNullOrEmpty(this.config.model))
                this.config.model = DEFAULT_MODEL;
        }

        public async Task<string> GenerateAsync(string prompt, ModelParameters parameters = null)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI provider is not configured. Please set API key.");

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default" 
                ? config.model 
                : parameters.model;

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are an expert Unity C# developer assistant." },
                    new { role = "user", content = prompt }
                },
                temperature = parameters.temperature,
                max_tokens = parameters.maxTokens,
                top_p = parameters.topP
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            // Note: JsonUtility doesn't handle arrays well, using manual JSON for now
            jsonBody = $@"{{
                ""model"": ""{model}"",
                ""messages"": [
                    {{""role"": ""system"", ""content"": ""You are an expert Unity C# developer assistant.""}},
                    {{""role"": ""user"", ""content"": {EscapeJson(prompt)}}}
                ],
                ""temperature"": {parameters.temperature.ToString(CultureInfo.InvariantCulture)},
                ""max_tokens"": {parameters.maxTokens},
                ""top_p"": {parameters.topP.ToString(CultureInfo.InvariantCulture)}
            }}";

            using (UnityWebRequest request = new UnityWebRequest(config.endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"OpenAI API Error: {request.error}\n{request.downloadHandler.text}");
                }

                return ParseResponse(request.downloadHandler.text);
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                await GenerateAsync("test", new ModelParameters { maxTokens = 5 });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string ParseResponse(string json)
        {
            // Simple JSON parsing for response
            // In production, use proper JSON library
            int contentStart = json.IndexOf("\"content\":");
            if (contentStart == -1) return "Error parsing response";

            contentStart = json.IndexOf("\"", contentStart + 10) + 1;
            int contentEnd = json.IndexOf("\"", contentStart);
            
            return json.Substring(contentStart, contentEnd - contentStart)
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

