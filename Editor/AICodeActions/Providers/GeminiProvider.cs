using System;
using System.Globalization;
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
            Debug.Log("[Gemini] GenerateAsync called");
            
            if (!IsConfigured)
                throw new InvalidOperationException("Gemini provider is not configured. Please set API key.");

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
            
            Debug.Log($"[Gemini] Request Body: {jsonBody.Substring(0, Mathf.Min(200, jsonBody.Length))}...");

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                Debug.Log("[Gemini] Sending request...");
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                Debug.Log($"[Gemini] Request completed. Result: {request.result}");
                Debug.Log($"[Gemini] Response Code: {request.responseCode}");

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[Gemini] HTTP Error: {request.error}");
                    Debug.LogError($"[Gemini] Response Body: {request.downloadHandler.text}");
                    throw new Exception($"Gemini API Error: {request.error}\n{request.downloadHandler.text}");
                }

                string rawResponse = request.downloadHandler.text;
                Debug.Log($"[Gemini] Raw JSON Response: {rawResponse.Substring(0, Mathf.Min(500, rawResponse.Length))}...");
                return ParseResponse(rawResponse);
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

