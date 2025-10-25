using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    public class OpenAIProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string DEFAULT_MODEL = "gpt-4";

        private ProviderConfig config;
        private static HttpClient httpClient; // For streaming

        public string Name => "OpenAI";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.apiKey);
        public bool RequiresApiKey => true;
        public bool SupportsStreaming => true; // ✅ OpenAI supports streaming!

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
            Debug.Log($"[OpenAI] Raw Response Length: {json.Length}");
            
            int contentStart = json.IndexOf("\"content\":");
            if (contentStart == -1)
            {
                Debug.LogError("[OpenAI] Could not find 'content' field in response");
                return "Error: Could not parse response";
            }

            contentStart = json.IndexOf("\"", contentStart + 10) + 1;
            
            // Find the closing quote, accounting for escaped quotes
            int contentEnd = contentStart;
            bool escaped = false;
            
            while (contentEnd < json.Length)
            {
                if (json[contentEnd] == '\\' && !escaped)
                {
                    escaped = true;
                    contentEnd++;
                    continue;
                }
                
                if (json[contentEnd] == '"' && !escaped)
                {
                    break;
                }
                
                escaped = false;
                contentEnd++;
            }
            
            if (contentEnd >= json.Length)
            {
                Debug.LogError("[OpenAI] Could not find end of content field");
                return "Error: Incomplete response";
            }
            
            string result = json.Substring(contentStart, contentEnd - contentStart)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
            
            Debug.Log($"[OpenAI] Parsed text length: {result.Length} characters");
            return result;
        }

        /// <summary>
        /// Stream generation with real-time chunk callbacks
        /// </summary>
        public async Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI provider is not configured.");

            // Initialize HttpClient once
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default"
                ? config.model
                : parameters.model;

            // Build request JSON with stream=true
            string jsonBody = $@"{{
                ""model"": ""{model}"",
                ""messages"": [
                    {{""role"": ""system"", ""content"": ""You are an expert Unity C# developer assistant.""}},
                    {{""role"": ""user"", ""content"": {EscapeJson(prompt)}}}
                ],
                ""temperature"": {parameters.temperature.ToString(CultureInfo.InvariantCulture)},
                ""max_tokens"": {parameters.maxTokens},
                ""top_p"": {parameters.topP.ToString(CultureInfo.InvariantCulture)},
                ""stream"": true
            }}";

            Debug.Log("[OpenAI] Starting streaming request...");

            var request = new HttpRequestMessage(HttpMethod.Post, config.endpoint)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {config.apiKey}");

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead, // Important for streaming!
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                int chunkIndex = 0; // Moved outside using block

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                    {
                        string line = await reader.ReadLineAsync();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // OpenAI SSE format: "data: {...}"
                        if (line.StartsWith("data: "))
                        {
                            string jsonData = line.Substring(6).Trim();

                            // Check for [DONE] signal
                            if (jsonData == "[DONE]")
                            {
                                onChunk?.Invoke(new StreamChunk
                                {
                                    Type = StreamChunkType.Done,
                                    IsFinal = true,
                                    Index = chunkIndex++
                                });
                                break;
                            }

                            // Parse JSON chunk
                            try
                            {
                                string content = ExtractStreamContent(jsonData);

                                if (!string.IsNullOrEmpty(content))
                                {
                                    onChunk?.Invoke(new StreamChunk(content, StreamChunkType.TextDelta)
                                    {
                                        Index = chunkIndex++
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[OpenAI] Failed to parse chunk: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenAI] Streaming error: {ex.Message}");
                onChunk?.Invoke(new StreamChunk
                {
                    Delta = $"Error: {ex.Message}",
                    Type = StreamChunkType.Error
                });
                throw;
            }
        }

        /// <summary>
        /// Extract content from OpenAI streaming JSON response
        /// </summary>
        private string ExtractStreamContent(string json)
        {
            // Quick & dirty JSON parsing for: {"choices":[{"delta":{"content":"text"}}]}
            int contentIndex = json.IndexOf("\"content\":\"");
            if (contentIndex == -1)
                return null;

            int startIndex = contentIndex + 11; // Length of "content":"
            int endIndex = json.IndexOf("\"", startIndex);

            if (endIndex == -1)
                return null;

            string content = json.Substring(startIndex, endIndex - startIndex);

            // Unescape JSON
            return content
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
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

