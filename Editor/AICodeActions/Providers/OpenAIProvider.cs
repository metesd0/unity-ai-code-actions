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
using UnityEngine.Networking;

namespace AICodeActions.Providers
{
    public class OpenAIProvider : IModelProvider
    {
        private const string DEFAULT_ENDPOINT = "https://api.openai.com/v1/chat/completions";
        private const string DEFAULT_MODEL = "gpt-4";
        private const string SYSTEM_PROMPT = "You are an expert Unity C# developer assistant.";

        private ProviderConfig config;
        private static HttpClient httpClient;

        public string Name => "OpenAI";
        public bool IsConfigured => !string.IsNullOrEmpty(config?.apiKey);
        public bool RequiresApiKey => true;
        public bool SupportsStreaming => true;

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

            var request = new ChatCompletionRequest
            {
                Model = model,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = SYSTEM_PROMPT },
                    new ChatMessage { Role = "user", Content = prompt }
                },
                Temperature = parameters.temperature,
                MaxTokens = parameters.maxTokens,
                TopP = parameters.topP
            };

            string jsonBody = JsonConvert.SerializeObject(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(config.endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Authorization", $"Bearer {config.apiKey}");

                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"OpenAI API Error: {webRequest.error}\n{webRequest.downloadHandler.text}");
                }

                return ParseResponse(webRequest.downloadHandler.text);
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

            try
            {
                var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(json);

                if (response.Error != null)
                {
                    Debug.LogError($"[OpenAI] API Error: {response.Error.Message}");
                    return $"Error: {response.Error.Message}";
                }

                if (response.Choices == null || response.Choices.Count == 0)
                {
                    Debug.LogError("[OpenAI] No choices in response");
                    return "Error: No response from API";
                }

                var content = response.Choices[0].Message?.Content ?? "";
                Debug.Log($"[OpenAI] Parsed text length: {content.Length} characters");
                return content;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[OpenAI] JSON parse error: {ex.Message}");
                return $"Error: Failed to parse response - {ex.Message}";
            }
        }

        public async Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenAI provider is not configured.");

            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();
            var model = string.IsNullOrEmpty(parameters.model) || parameters.model == "default"
                ? config.model
                : parameters.model;

            var requestBody = new ChatCompletionRequest
            {
                Model = model,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "system", Content = SYSTEM_PROMPT },
                    new ChatMessage { Role = "user", Content = prompt }
                },
                Temperature = parameters.temperature,
                MaxTokens = parameters.maxTokens,
                TopP = parameters.topP,
                Stream = true
            };

            string jsonBody = JsonConvert.SerializeObject(requestBody);
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
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

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

                            try
                            {
                                var chunk = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonData);
                                var content = chunk?.Choices?[0]?.Delta?.Content;

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
    }
}
