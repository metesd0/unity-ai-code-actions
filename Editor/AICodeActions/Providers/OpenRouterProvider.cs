using System;
using System.Collections.Generic;
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
        public bool SupportsStreaming => true; // SSE streaming enabled
        
        public void Configure(string apiKey, Dictionary<string, object> settings = null)
        {
            this.apiKey = apiKey;
            
            // Get custom model name from settings
            if (settings != null && settings.ContainsKey("modelName"))
            {
                this.modelName = settings["modelName"].ToString();
            }
            else
            {
                // Default model
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
            
            // Build request body (OpenAI-compatible format)
            string requestBody = BuildRequestBody(prompt, parameters);
            
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
                www.SetRequestHeader("HTTP-Referer", "https://unity.com"); // Optional but recommended
                www.SetRequestHeader("X-Title", "Unity AI Code Actions"); // Optional but recommended
                
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
                Debug.Log($"[OpenRouter] Raw response: {responseText.Substring(0, Math.Min(500, responseText.Length))}...");
                
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
                
                // Try a simple test request
                await GenerateAsync("Say 'OK' if you can read this.", new ModelParameters { maxTokens = 10 });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[OpenRouter] Connection validation failed: {e.Message}");
                return false;
            }
        }
        
        private string BuildRequestBody(string prompt, ModelParameters parameters)
        {
            // Use InvariantCulture for consistent decimal formatting (dot instead of comma)
            string tempStr = parameters.temperature.ToString(CultureInfo.InvariantCulture);
            string topPStr = parameters.topP.ToString(CultureInfo.InvariantCulture);
            
            // OpenAI-compatible format
            var body = new StringBuilder();
            body.Append("{");
            body.Append($"\"model\":\"{modelName}\",");
            body.Append("\"messages\":[");
            body.Append("{\"role\":\"user\",\"content\":");
            body.Append(JsonEscape(prompt));
            body.Append("}],");
            body.Append($"\"max_tokens\":{parameters.maxTokens},");
            body.Append($"\"temperature\":{tempStr},");
            body.Append($"\"top_p\":{topPStr}");
            body.Append("}");
            
            return body.ToString();
        }
        
        private string ParseResponse(string jsonResponse)
        {
            try
            {
                // Find the content field in the response
                // Format: {"choices":[{"message":{"content":"..."}}]}
                
                int contentStart = jsonResponse.IndexOf("\"content\":\"");
                if (contentStart == -1)
                {
                    Debug.LogError("[OpenRouter] Could not find 'content' field in response");
                    return "Error: Could not parse OpenRouter response";
                }
                
                contentStart += 11; // Length of "\"content\":\""
                
                // Find the end of the content string (properly handle escaped quotes)
                int contentEnd = contentStart;
                bool escaped = false;
                int braceDepth = 0;
                
                for (int i = contentStart; i < jsonResponse.Length; i++)
                {
                    char c = jsonResponse[i];
                    
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    
                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    
                    if (c == '{')
                    {
                        braceDepth++;
                    }
                    else if (c == '}')
                    {
                        braceDepth--;
                    }
                    else if (c == '"' && braceDepth == 0)
                    {
                        contentEnd = i;
                        break;
                    }
                }
                
                string content = jsonResponse.Substring(contentStart, contentEnd - contentStart);
                
                // Unescape JSON string
                content = content.Replace("\\\"", "\"")
                                .Replace("\\n", "\n")
                                .Replace("\\r", "\r")
                                .Replace("\\t", "\t")
                                .Replace("\\\\", "\\");
                
                Debug.Log($"[OpenRouter] Parsed content length: {content.Length}");
                
                return content.Trim();
            }
            catch (Exception e)
            {
                Debug.LogError($"[OpenRouter] Parse error: {e.Message}");
                return $"Error parsing response: {e.Message}";
            }
        }
        
        private string JsonEscape(string str)
        {
            if (string.IsNullOrEmpty(str))
                return "\"\"";
            
            var sb = new StringBuilder();
            sb.Append("\"");
            
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            sb.AppendFormat("\\u{0:x4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            
            sb.Append("\"");
            return sb.ToString();
        }
        
        public async Task StreamGenerateAsync(
            string prompt,
            ModelParameters parameters,
            Action<StreamChunk> onChunk,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("OpenRouter provider is not configured.");

            // Initialize HttpClient once
            if (httpClient == null)
            {
                httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(5);
            }

            parameters = parameters ?? new ModelParameters();

            // Build request JSON with stream=true
            string jsonBody = BuildStreamingRequestBody(prompt, parameters);

            Debug.Log("[OpenRouter] Starting SSE streaming request...");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            request.Headers.Add("HTTP-Referer", "https://unity-ai-agent.local"); // Optional: For OpenRouter analytics
            request.Headers.Add("X-Title", "Unity AI Agent"); // Optional: For OpenRouter analytics

            try
            {
                var response = await httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead, // Important for streaming!
                    cancellationToken);

                // Check for pre-stream errors (HTTP status != 200)
                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[OpenRouter] HTTP {response.StatusCode}: {errorBody}");
                    
                    onChunk?.Invoke(new StreamChunk
                    {
                        Type = StreamChunkType.Error,
                        Content = $"OpenRouter error ({response.StatusCode}): {errorBody}",
                        IsFinal = true
                    });
                    return;
                }

                int chunkIndex = 0;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                    {
                        string line = await reader.ReadLineAsync();

                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        // SSE Comments (e.g., ": OPENROUTER PROCESSING") - ignore per SSE spec
                        if (line.StartsWith(":"))
                        {
                            Debug.Log($"[OpenRouter] SSE Comment: {line}");
                            continue;
                        }

                        // OpenRouter SSE format: "data: {...}"
                        if (line.StartsWith("data: "))
                        {
                            string jsonData = line.Substring(6).Trim();

                            // Check for [DONE] signal
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

                            // Parse JSON chunk
                            try
                            {
                                // Check for mid-stream error (after tokens have been sent)
                                if (jsonData.Contains("\"error\"") && jsonData.Contains("\"finish_reason\":\"error\""))
                                {
                                    string errorMsg = ExtractMidStreamError(jsonData);
                                    Debug.LogError($"[OpenRouter] Mid-stream error: {errorMsg}");
                                    
                                    onChunk?.Invoke(new StreamChunk
                                    {
                                        Type = StreamChunkType.Error,
                                        Content = $"Stream error: {errorMsg}",
                                        IsFinal = true,
                                        Index = chunkIndex++
                                    });
                                    break;
                                }

                                // Extract content from delta
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
                    Content = $"Streaming failed: {ex.Message}",
                    IsFinal = true
                });
            }
        }
        
        private string BuildStreamingRequestBody(string prompt, ModelParameters parameters)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append($"\"model\":\"{modelName}\",");
            sb.Append("\"messages\":[");
            
            // Escape the prompt content properly
            string escapedPrompt = prompt
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
            
            sb.Append($"{{\"role\":\"user\",\"content\":\"{escapedPrompt}\"}}");
            sb.Append("],");
            sb.Append($"\"temperature\":{parameters.temperature.ToString(CultureInfo.InvariantCulture)},");
            sb.Append($"\"max_tokens\":{parameters.maxTokens},");
            sb.Append($"\"top_p\":{parameters.topP.ToString(CultureInfo.InvariantCulture)},");
            sb.Append("\"stream\":true");
            sb.Append("}");
            
            return sb.ToString();
        }
        
        private string ExtractStreamContent(string jsonData)
        {
            // Simple parser for: {"choices":[{"delta":{"content":"text"},...}],...}
            int contentIndex = jsonData.IndexOf("\"content\":\"");
            if (contentIndex == -1)
                return null;

            int startQuote = contentIndex + 11; // Length of "content":"
            int endQuote = startQuote;
            
            // Find the closing quote, handling escaped quotes
            while (endQuote < jsonData.Length)
            {
                if (jsonData[endQuote] == '\"' && (endQuote == startQuote || jsonData[endQuote - 1] != '\\'))
                    break;
                endQuote++;
            }

            if (endQuote >= jsonData.Length)
                return null;

            string content = jsonData.Substring(startQuote, endQuote - startQuote);
            
            // Unescape JSON special characters
            return content
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
        
        private string ExtractMidStreamError(string jsonData)
        {
            // Extract error message from: {"error":{"message":"..."},"finish_reason":"error",...}
            int msgIndex = jsonData.IndexOf("\"message\":\"");
            if (msgIndex == -1)
                return "Unknown error";

            int startQuote = msgIndex + 11;
            int endQuote = jsonData.IndexOf("\"", startQuote);
            
            if (endQuote == -1)
                return "Unknown error";
                
            return jsonData.Substring(startQuote, endQuote - startQuote);
        }
    }
}

