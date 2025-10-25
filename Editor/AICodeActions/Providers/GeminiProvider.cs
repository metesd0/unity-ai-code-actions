using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;
using UnityEngine;

namespace AICodeActions.Providers
{
    public class GeminiProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string DEFAULT_MODEL = "gemini-pro";

        private ProviderConfig config;
        private static HttpClient httpClient;

        public string Name => "Gemini";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.apiKey);
        public bool RequiresApiKey => true;
        public bool SupportsStreaming => false; // TODO: Implement later

        public GeminiProvider(ProviderConfig config)
        {
            this.config = config ?? new ProviderConfig();
            if (string.IsNullOrEmpty(this.config.model))
                this.config.model = DEFAULT_MODEL;
        }

        public async Task<string> GenerateAsync(string prompt, ModelParameters parameters = null)
        {
            Debug.Log("[Gemini] GenerateAsync called");
            
            if (!IsConfigured)
                throw new InvalidOperationException("Gemini provider is not configured. Please set API key.");

                // Initialize HttpClient if needed (thread-safe, background-compatible)
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default" 
                ? config.model 
                : parameters.model;

            string url = $"{DEFAULT_ENDPOINT}{model}:generateContent?key={config.apiKey}";
            Debug.Log($"[Gemini] Request URL: {DEFAULT_ENDPOINT}{model}:generateContent?key=***");

            string jsonBody = $@"{{
                ""contents"": [{{
                    ""parts"": [{{
                        ""text"": {EscapeJson(prompt)}
                    }}]
                }}],
                ""generationConfig"": {{
                    ""temperature"": {parameters.temperature.ToString(CultureInfo.InvariantCulture)},
                    ""maxOutputTokens"": {parameters.maxTokens},
                    ""topP"": {parameters.topP.ToString(CultureInfo.InvariantCulture)}
                }}
            }}";
            
            Debug.Log($"[Gemini] Request Body: {jsonBody.Substring(0, Math.Min(200, jsonBody.Length))}...");

            try
            {
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                
                Debug.Log("[Gemini] Sending HTTP request...");
                var response = await httpClient.PostAsync(url, content).ConfigureAwait(false);
                
                Debug.Log($"[Gemini] Response Status: {response.StatusCode}");
                
                string rawResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogError($"[Gemini] HTTP Error: {response.StatusCode}");
                    Debug.LogError($"[Gemini] Response Body: {rawResponse}");
                    throw new Exception($"Gemini API Error: {response.StatusCode}\n{rawResponse}");
                }

                Debug.Log($"[Gemini] Raw JSON Response: {rawResponse.Substring(0, Math.Min(500, rawResponse.Length))}...");
                return ParseResponse(rawResponse);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Gemini] Exception: {ex.Message}");
                throw;
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
            // Debug: Log raw response
            Debug.Log($"[Gemini] Raw Response Length: {json.Length}");
            
            // Find the text field in Gemini response format
            // Response format: {"candidates":[{"content":{"parts":[{"text":"..."}]}}]}
            int textStart = json.IndexOf("\"text\":");
            if (textStart == -1)
            {
                Debug.LogError("[Gemini] Could not find 'text' field in response");
                return "Error: Could not parse response";
            }

            // Move to the opening quote of the text value
            textStart = json.IndexOf("\"", textStart + 7) + 1;
            
            // Find the closing quote, accounting for escaped quotes
            int textEnd = textStart;
            bool escaped = false;
            
            while (textEnd < json.Length)
            {
                if (json[textEnd] == '\\' && !escaped)
                {
                    escaped = true;
                    textEnd++;
                    continue;
                }
                
                if (json[textEnd] == '"' && !escaped)
                {
                    break;
                }
                
                escaped = false;
                textEnd++;
            }
            
            if (textEnd >= json.Length)
            {
                Debug.LogError("[Gemini] Could not find end of text field");
                return "Error: Incomplete response";
            }
            
            string result = json.Substring(textStart, textEnd - textStart)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
            
            Debug.Log($"[Gemini] Parsed text length: {result.Length} characters");
            return result;
        }

        public Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Gemini streaming will be implemented in Phase 2");
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

