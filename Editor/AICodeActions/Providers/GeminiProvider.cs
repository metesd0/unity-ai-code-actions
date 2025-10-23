using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    public class GeminiProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DEFAULT_MODEL = "gemini-pro";

        private ProviderConfig config;

        public string Name => "Gemini";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.apiKey);
        public bool RequiresApiKey => true;

        public GeminiProvider(ProviderConfig config)
        {
            this.config = config ?? new ProviderConfig();
            if (string.IsNullOrEmpty(this.config.model))
                this.config.model = DEFAULT_MODEL;
        }

        public async Task<string> GenerateAsync(string prompt, ModelParameters parameters = null)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Gemini provider is not configured. Please set API key.");

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default" 
                ? config.model 
                : parameters.model;

            string url = $"{DEFAULT_ENDPOINT}{model}:generateContent?key={config.apiKey}";

            string jsonBody = $@"{{
                ""contents"": [{{
                    ""parts"": [{{
                        ""text"": {EscapeJson(prompt)}
                    }}]
                }}],
                ""generationConfig"": {{
                    ""temperature"": {parameters.temperature},
                    ""maxOutputTokens"": {parameters.maxTokens},
                    ""topP"": {parameters.topP}
                }}
            }}";

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"Gemini API Error: {request.error}\n{request.downloadHandler.text}");
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
            // Simple JSON parsing for Gemini response format
            int textStart = json.IndexOf("\"text\":");
            if (textStart == -1) return "Error parsing response";

            textStart = json.IndexOf("\"", textStart + 7) + 1;
            int textEnd = json.IndexOf("\"", textStart);
            
            return json.Substring(textStart, textEnd - textStart)
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

