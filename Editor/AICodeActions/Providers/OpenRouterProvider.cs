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
        
        public string Name => "OpenRouter";
        public bool IsConfigured => !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(modelName);
        public bool RequiresApiKey => true;
        public bool SupportsStreaming => true; // ✅ NOW SUPPORTED!
        
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
            {
                throw new Exception("OpenRouter provider is not configured.");
            }

            parameters = parameters ?? new ModelParameters();
            string url = $"{baseUrl}/chat/completions";

            // Build request with streaming enabled
            string requestBody = BuildStreamingRequestBody(prompt, parameters);

            Debug.Log($"[OpenRouter] Starting streaming request to: {url}");

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromMinutes(5);

                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                    request.Headers.Add("HTTP-Referer", "https://unity.com");
                    request.Headers.Add("X-Title", "Unity AI Code Actions");
                    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                    var response = await httpClient.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    response.EnsureSuccessStatusCode();

                    int chunkIndex = 0;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                        {
                            string line = await reader.ReadLineAsync();

                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            // Handle SSE comments (OpenRouter timeout prevention)
                            // Example: ": OPENROUTER PROCESSING"
                            if (line.StartsWith(":"))
                            {
                                // Silently ignore SSE comments per spec (no need to log)
                                continue;
                            }

                            // OpenRouter uses same SSE format as OpenAI: "data: {...}"
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
                                    // Check for mid-stream error (after tokens already sent)
                                    if (jsonData.Contains("\"error\""))
                                    {
                                        // Try to extract error message
                                        int errorMsgStart = jsonData.IndexOf("\"message\":\"");
                                        if (errorMsgStart != -1)
                                        {
                                            errorMsgStart += 11;
                                            int errorMsgEnd = jsonData.IndexOf("\"", errorMsgStart);
                                            string errorMsg = jsonData.Substring(errorMsgStart, errorMsgEnd - errorMsgStart);
                                            
                                            Debug.LogError($"[OpenRouter] Mid-stream error: {errorMsg}");
                                            onChunk?.Invoke(new StreamChunk
                                            {
                                                Type = StreamChunkType.Error,
                                                Delta = $"Error: {errorMsg}",
                                                IsFinal = true,
                                                Index = chunkIndex++
                                            });
                                            break;
                                        }
                                    }

                                    string content = ExtractStreamContent(jsonData);

                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        onChunk?.Invoke(new StreamChunk(content, StreamChunkType.TextDelta)
                                        {
                                            Index = chunkIndex++
                                        });
                                    }
                                    
                                    // Check for error finish_reason
                                    if (jsonData.Contains("\"finish_reason\":\"error\""))
                                    {
                                        onChunk?.Invoke(new StreamChunk
                                        {
                                            Type = StreamChunkType.Done,
                                            IsFinal = true,
                                            Index = chunkIndex++
                                        });
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.LogWarning($"[OpenRouter] Failed to parse chunk: {ex.Message}");
                                }
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
                Debug.LogError($"[OpenRouter] Streaming error: {ex.Message}");
                onChunk?.Invoke(new StreamChunk
                {
                    Type = StreamChunkType.Error,
                    Delta = $"Error: {ex.Message}",
                    IsFinal = true
                });
                throw;
            }
        }

        private string BuildStreamingRequestBody(string prompt, ModelParameters parameters)
        {
            string tempStr = parameters.temperature.ToString(CultureInfo.InvariantCulture);
            string topPStr = parameters.topP.ToString(CultureInfo.InvariantCulture);

            var body = new StringBuilder();
            body.Append("{");
            body.Append($"\"model\":\"{modelName}\",");
            body.Append("\"messages\":[");
            body.Append("{\"role\":\"user\",\"content\":");
            body.Append(JsonEscape(prompt));
            body.Append("}],");
            body.Append($"\"max_tokens\":{parameters.maxTokens},");
            body.Append($"\"temperature\":{tempStr},");
            body.Append($"\"top_p\":{topPStr},");
            body.Append("\"stream\":true"); // ← Enable streaming!
            body.Append("}");

            return body.ToString();
        }

        private string ExtractStreamContent(string jsonChunk)
        {
            // OpenRouter format: {"choices":[{"delta":{"content":"text"}}]}
            try
            {
                int contentIndex = jsonChunk.IndexOf("\"content\":\"");
                if (contentIndex == -1)
                    return string.Empty;

                int contentStart = contentIndex + 11; // "content":"
                int contentEnd = contentStart;

                // Find end quote (handle escaped quotes)
                bool escaped = false;
                for (int i = contentStart; i < jsonChunk.Length; i++)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }

                    if (jsonChunk[i] == '\\')
                    {
                        escaped = true;
                        continue;
                    }

                    if (jsonChunk[i] == '"')
                    {
                        contentEnd = i;
                        break;
                    }
                }

                if (contentEnd <= contentStart)
                    return string.Empty;

                string content = jsonChunk.Substring(contentStart, contentEnd - contentStart);

                // Unescape
                content = content.Replace("\\n", "\n")
                                .Replace("\\r", "\r")
                                .Replace("\\t", "\t")
                                .Replace("\\\"", "\"")
                                .Replace("\\\\", "\\");

                return content;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OpenRouter] Extract content error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}

