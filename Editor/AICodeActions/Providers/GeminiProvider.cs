using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Core;
using AICodeActions.Providers.Models;
using Newtonsoft.Json;
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
        public bool SupportsStreaming => true;

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

            var request = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = parameters.temperature,
                    MaxOutputTokens = parameters.maxTokens,
                    TopP = parameters.topP
                }
            };

            string jsonBody = JsonConvert.SerializeObject(request);
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
            Debug.Log($"[Gemini] Raw Response Length: {json.Length}");

            try
            {
                var response = JsonConvert.DeserializeObject<GeminiResponse>(json);

                if (response.Error != null)
                {
                    Debug.LogError($"[Gemini] API Error: {response.Error.Message}");
                    return $"Error: {response.Error.Message}";
                }

                if (response.Candidates == null || response.Candidates.Count == 0)
                {
                    Debug.LogError("[Gemini] No candidates in response");
                    return "Error: No response from API";
                }

                var parts = response.Candidates[0].Content?.Parts;
                if (parts == null || parts.Count == 0)
                {
                    Debug.LogError("[Gemini] No parts in response");
                    return "Error: Empty response";
                }

                string result = parts[0].Text ?? "";
                Debug.Log($"[Gemini] Parsed text length: {result.Length} characters");
                return result;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[Gemini] JSON parse error: {ex.Message}");
                return $"Error: Failed to parse response - {ex.Message}";
            }
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

            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default"
                ? config.model
                : parameters.model;

            string url = $"{DEFAULT_ENDPOINT}{model}:streamGenerateContent?key={config.apiKey}&alt=sse";
            Debug.Log($"[Gemini] Streaming URL: {DEFAULT_ENDPOINT}{model}:streamGenerateContent?key=***&alt=sse");

            var requestBody = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = parameters.temperature,
                    MaxOutputTokens = parameters.maxTokens,
                    TopP = parameters.topP
                }
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
            Debug.Log($"[Gemini] Streaming request body: {jsonBody.Substring(0, Math.Min(200, jsonBody.Length))}...");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
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

                        if (line.StartsWith("data: "))
                        {
                            string jsonData = line.Substring(6).Trim();

                            try
                            {
                                var chunk = JsonConvert.DeserializeObject<GeminiResponse>(jsonData);
                                var parts = chunk?.Candidates?[0]?.Content?.Parts;

                                if (parts != null && parts.Count > 0)
                                {
                                    var part = parts[0];
                                    bool isThought = part.Thought == true;

                                    StreamChunkType chunkType = isThought
                                        ? StreamChunkType.ReasoningDelta
                                        : StreamChunkType.TextDelta;

                                    onChunk?.Invoke(new StreamChunk
                                    {
                                        Type = chunkType,
                                        Delta = part.Text,
                                        ReasoningText = isThought ? part.Text : null,
                                        IsFinal = false,
                                        Index = chunkIndex++
                                    });
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogWarning($"[Gemini] Error parsing chunk: {ex.Message}");
                            }
                        }
                    }

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
    }
}
