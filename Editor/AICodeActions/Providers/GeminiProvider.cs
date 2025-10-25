using System;
using System.Globalization;
using System.IO;
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
        public bool SupportsStreaming => true; // SSE streaming enabled!

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

            // Build thinking config if specified
            string thinkingConfig = "";
            if (parameters.thinkingBudget.HasValue)
            {
                thinkingConfig = $@",
                ""thinkingConfig"": {{
                    ""thinkingBudget"": {parameters.thinkingBudget.Value}
                }}";
            }

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
                }}{thinkingConfig}
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

        public async Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            Debug.Log("[Gemini] StreamGenerateAsync called");
            
            if (!IsConfigured)
                throw new InvalidOperationException("Gemini provider is not configured. Please set API key.");

            // Initialize HttpClient if needed
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default" 
                ? config.model 
                : parameters.model;

            // Use streamGenerateContent endpoint for streaming
            string url = $"{DEFAULT_ENDPOINT}{model}:streamGenerateContent?key={config.apiKey}&alt=sse";
            Debug.Log($"[Gemini] Streaming URL: {DEFAULT_ENDPOINT}{model}:streamGenerateContent?key=***&alt=sse");

            // Build thinking config if specified
            string thinkingConfig = "";
            if (parameters.thinkingBudget.HasValue)
            {
                thinkingConfig = $@",
                ""thinkingConfig"": {{
                    ""thinkingBudget"": {parameters.thinkingBudget.Value}
                }}";
            }

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
                }}{thinkingConfig}
            }}";

            Debug.Log($"[Gemini] Streaming request body: {jsonBody.Substring(0, Math.Min(200, jsonBody.Length))}...");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead, // Important for streaming!
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Debug.LogError($"[Gemini] HTTP {response.StatusCode}: {errorBody}");
                    
                    onChunk?.Invoke(new StreamChunk
                    {
                        Type = StreamChunkType.Error,
                        Delta = $"Gemini error ({response.StatusCode}): {errorBody}",
                        IsFinal = true
                    });
                    return;
                }

                int chunkIndex = 0;

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                    {
                        string line = await reader.ReadLineAsync().ConfigureAwait(false);

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // SSE format: "data: {...}"
                        if (line.StartsWith("data: "))
                        {
                            string jsonData = line.Substring(6).Trim();

                            // Parse JSON chunk
                            try
                            {
                                // Extract text and thought status from Gemini streaming response
                                // Format: {"candidates":[{"content":{"parts":[{"text":"...","thought":true}]}}]}
                                var (text, isThought) = ExtractStreamChunk(jsonData);
                                
                                if (!string.IsNullOrEmpty(text))
                                {
                                    // Determine chunk type based on thought flag
                                    StreamChunkType chunkType = isThought 
                                        ? StreamChunkType.ReasoningDelta 
                                        : StreamChunkType.Content;
                                    
                                    onChunk?.Invoke(new StreamChunk
                                    {
                                        Type = chunkType,
                                        Delta = text,
                                        ReasoningText = isThought ? text : null,
                                        IsFinal = false,
                                        Index = chunkIndex++
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[Gemini] Error parsing chunk: {ex.Message}");
                            }
                        }
                    }

                    // Send done signal
                    Debug.Log("[Gemini] Stream completed");
                    onChunk?.Invoke(new StreamChunk
                    {
                        Type = StreamChunkType.Done,
                        IsFinal = true,
                        Index = chunkIndex++
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Gemini] Streaming error: {ex.Message}");
                onChunk?.Invoke(new StreamChunk
                {
                    Type = StreamChunkType.Error,
                    Delta = $"Gemini streaming error: {ex.Message}",
                    IsFinal = true
                });
            }
        }

        /// <summary>
        /// Extract text and thought status from Gemini streaming response
        /// Returns (text, isThought) tuple
        /// </summary>
        private (string text, bool isThought) ExtractStreamChunk(string jsonData)
        {
            // Extract text from streaming response chunk
            int textStart = jsonData.IndexOf("\"text\":");
            if (textStart == -1)
                return (null, false);

            textStart = jsonData.IndexOf("\"", textStart + 7) + 1;
            
            int textEnd = textStart;
            bool escaped = false;
            
            while (textEnd < jsonData.Length)
            {
                if (jsonData[textEnd] == '\\' && !escaped)
                {
                    escaped = true;
                    textEnd++;
                    continue;
                }
                
                if (jsonData[textEnd] == '"' && !escaped)
                {
                    break;
                }
                
                escaped = false;
                textEnd++;
            }
            
            if (textEnd >= jsonData.Length)
                return (null, false);
            
            string text = jsonData.Substring(textStart, textEnd - textStart)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
            
            // Check if this is a thought (thinking) chunk
            bool isThought = jsonData.Contains("\"thought\":true") || jsonData.Contains("\"thought\": true");
            
            return (text, isThought);
        }

        private string ExtractStreamText(string jsonData)
        {
            // Legacy method for backward compatibility
            var (text, _) = ExtractStreamChunk(jsonData);
            return text;
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

