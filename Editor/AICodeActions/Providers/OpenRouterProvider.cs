using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AICodeActions.Providers.Models;
using ChatMessage = AICodeActions.Providers.Models.ChatMessage;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    /// <summary>
    /// OpenRouter API Provider - Access to 100+ AI models
    /// Compatible with OpenAI API format
    /// </summary>
    public class OpenRouterProvider : IModelProvider
    {
        private string apiKey;
        private string modelName;
        private const string baseUrl = "https://openrouter.ai/api/v1";
        private static HttpClient httpClient;

        public string Name => "OpenRouter";
        public bool IsConfigured => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(modelName);
        public bool RequiresApiKey => true;
        public bool SupportsStreaming => true;

        public void Configure(string apiKey, Dictionary<string, object> settings = null)
        {
            this.apiKey = apiKey;

            if (settings != null && settings.ContainsKey("modelName"))
            {
                this.modelName = settings["modelName"].ToString();
            }
            else
            {
                this.modelName = "openai/gpt-3.5-turbo";
            }

            Debug.Log($"[OpenRouter] Configured with model: {modelName}");
        }

        public async Task<string> GenerateAsync(string prompt, ModelParameters parameters = null)
        {
            if (!IsConfigured)
            {
                throw new Exception("OpenRouter provider is not configured. Please set API key and model.");
            }

            parameters = parameters ?? new ModelParameters();

            string url = $"{baseUrl}/chat/completions";

            var request = BuildRequest(prompt, parameters, stream: false);
            string requestBody = JsonConvert.SerializeObject(request);

            Debug.Log($"[OpenRouter] Sending request to: {url}");
            Debug.Log($"[OpenRouter] Model: {modelName}");
            Debug.Log($"[OpenRouter] Request body length: {requestBody.Length}");

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(requestBody);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();

                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                www.SetRequestHeader("HTTP-Referer", "https://unity.com");
                www.SetRequestHeader("X-Title", "Unity AI Code Actions");

                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    string errorMsg = $"OpenRouter API Error: {www.error}\n{www.downloadHandler.text}";
                    Debug.LogError($"[OpenRouter] {errorMsg}");
                    throw new Exception(errorMsg);
                }

                string responseText = www.downloadHandler.text;
                Debug.Log($"[OpenRouter] Raw response length: {responseText.Length}");

                return ParseResponse(responseText);
            }
        }

        public async Task<bool> ValidateConnectionAsync()
        {
            try
            {
                if (!IsConfigured)
                {
                    return false;
                }

                await GenerateAsync("Say 'OK' if you can read this.", new ModelParameters { maxTokens = 10 });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenRouter] Connection validation failed: {e.Message}");
                return false;
            }
        }

        private ChatCompletionRequest BuildRequest(string prompt, ModelParameters parameters, bool stream)
        {
            var request = new ChatCompletionRequest
            {
                Model = modelName,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "user", Content = prompt }
                },
                MaxTokens = parameters.maxTokens,
                Temperature = parameters.temperature,
                TopP = parameters.topP,
                Stream = stream ? true : (bool?)null
            };

            // Add OpenRouter Reasoning Tokens support
            if (!string.IsNullOrEmpty(parameters.reasoningEffort) || parameters.reasoningMaxTokens.HasValue)
            {
                request.Reasoning = new ReasoningConfig
                {
                    Effort = parameters.reasoningEffort,
                    MaxTokens = parameters.reasoningMaxTokens,
                    Exclude = parameters.reasoningExclude ? true : (bool?)null
                };
            }

            return request;
        }

        private string ParseResponse(string jsonResponse)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse);

                if (response.Error != null)
                {
                    Debug.LogError($"[OpenRouter] API Error: {response.Error.Message}");
                    return $"Error: {response.Error.Message}";
                }

                if (response.Choices == null || response.Choices.Count == 0)
                {
                    Debug.LogError("[OpenRouter] No choices in response");
                    return "Error: No response from API";
                }

                var content = response.Choices[0].Message?.Content ?? "";
                Debug.Log($"[OpenRouter] Parsed content length: {content.Length}");

                return content.Trim();
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[OpenRouter] Parse error: {ex.Message}");
                return $"Error parsing response: {ex.Message}";
            }
        }

        public async Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenRouter provider is not configured.");

            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();

            var requestBody = BuildRequest(prompt, parameters, stream: true);
            string jsonBody = JsonConvert.SerializeObject(requestBody);

            Debug.Log("[OpenRouter] Starting SSE streaming request...");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("HTTP-Referer", "https://unity-ai-agent.local");
            request.Headers.Add("X-Title", "Unity AI Agent");

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Debug.LogError($"[OpenRouter] HTTP {response.StatusCode}: {errorBody}");

                    onChunk?.Invoke(new StreamChunk
                    {
                        Type = StreamChunkType.Error,
                        Delta = $"OpenRouter error ({response.StatusCode}): {errorBody}",
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

                        // SSE Comments - ignore per SSE spec
                        if (line.StartsWith(":"))
                            continue;

                        if (line.StartsWith("data: "))
                        {
                            string jsonData = line.Substring(6).Trim();

                            if (jsonData == "[DONE]")
                            {
                                Debug.Log("[OpenRouter] Stream completed with [DONE] signal");
                                onChunk?.Invoke(new StreamChunk
                                {
                                    Type = StreamChunkType.Done,
                                    IsFinal = true,
                                    Index = chunkIndex++
                                });
                                break;
                            }

                            try
                            {
                                var chunk = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonData);

                                // Check for mid-stream error
                                if (chunk.Choices?[0]?.FinishReason == "error")
                                {
                                    string errorMsg = chunk.Error?.Message ?? "Unknown error";
                                    Debug.LogError($"[OpenRouter] Mid-stream error: {errorMsg}");

                                    onChunk?.Invoke(new StreamChunk
                                    {
                                        Type = StreamChunkType.Error,
                                        Delta = $"Stream error: {errorMsg}",
                                        IsFinal = true,
                                        Index = chunkIndex++
                                    });
                                    break;
                                }

                                // Extract reasoning details
                                var reasoningDetails = chunk.Choices?[0]?.Delta?.ReasoningDetails;
                                if (reasoningDetails != null)
                                {
                                    foreach (var detail in reasoningDetails)
                                    {
                                        if (!string.IsNullOrEmpty(detail.Text))
                                        {
                                            onChunk?.Invoke(new StreamChunk
                                            {
                                                Type = StreamChunkType.ReasoningDelta,
                                                ReasoningText = detail.Text,
                                                ReasoningId = detail.Id,
                                                Delta = detail.Text,
                                                Index = chunkIndex++
                                            });
                                        }
                                    }
                                }

                                // Extract content
                                var content = chunk.Choices?[0]?.Delta?.Content;
                                if (!string.IsNullOrEmpty(content))
                                {
                                    onChunk?.Invoke(new StreamChunk(content, StreamChunkType.TextDelta)
                                    {
                                        Index = chunkIndex++
                                    });
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogWarning($"[OpenRouter] Failed to parse chunk: {ex.Message}\nData: {jsonData}");
                            }
                        }
                    }
                }

                Debug.Log($"[OpenRouter] Streaming completed. Total chunks: {chunkIndex}");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[OpenRouter] Streaming cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenRouter] Streaming error: {ex.Message}");
                onChunk?.Invoke(new StreamChunk
                {
                    Type = StreamChunkType.Error,
                    Delta = $"Streaming failed: {ex.Message}",
                    IsFinal = true
                });
            }
        }
    }
}
