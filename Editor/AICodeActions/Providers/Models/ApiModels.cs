using System.Collections.Generic;
using Newtonsoft.Json;

namespace AICodeActions.Providers.Models
{
    #region OpenAI / OpenRouter Models

    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class ChatCompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; }

        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonProperty("top_p")]
        public float TopP { get; set; }

        [JsonProperty("stream", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Stream { get; set; }

        [JsonProperty("reasoning", NullValueHandling = NullValueHandling.Ignore)]
        public ReasoningConfig Reasoning { get; set; }
    }

    public class ReasoningConfig
    {
        [JsonProperty("effort", NullValueHandling = NullValueHandling.Ignore)]
        public string Effort { get; set; }

        [JsonProperty("max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaxTokens { get; set; }

        [JsonProperty("exclude", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Exclude { get; set; }
    }

    public class ChatCompletionResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("choices")]
        public List<ChatChoice> Choices { get; set; }

        [JsonProperty("error")]
        public ApiError Error { get; set; }
    }

    public class ChatChoice
    {
        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("message")]
        public ChatMessage Message { get; set; }

        [JsonProperty("delta")]
        public ChatDelta Delta { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class ChatDelta
    {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("reasoning_details")]
        public List<ReasoningDetail> ReasoningDetails { get; set; }
    }

    public class ReasoningDetail
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class ApiError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
    }

    #endregion

    #region Gemini Models

    public class GeminiRequest
    {
        [JsonProperty("contents")]
        public List<GeminiContent> Contents { get; set; }

        [JsonProperty("generationConfig")]
        public GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public class GeminiContent
    {
        [JsonProperty("parts")]
        public List<GeminiPart> Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("thought", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Thought { get; set; }
    }

    public class GeminiGenerationConfig
    {
        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("maxOutputTokens")]
        public int MaxOutputTokens { get; set; }

        [JsonProperty("topP")]
        public float TopP { get; set; }
    }

    public class GeminiResponse
    {
        [JsonProperty("candidates")]
        public List<GeminiCandidate> Candidates { get; set; }

        [JsonProperty("error")]
        public GeminiError Error { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonProperty("content")]
        public GeminiContent Content { get; set; }

        [JsonProperty("finishReason")]
        public string FinishReason { get; set; }
    }

    public class GeminiError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }

    #endregion

    #region Ollama Models

    public class OllamaRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("prompt")]
        public string Prompt { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; }

        [JsonProperty("options")]
        public OllamaOptions Options { get; set; }
    }

    public class OllamaOptions
    {
        [JsonProperty("temperature")]
        public float Temperature { get; set; }

        [JsonProperty("num_predict")]
        public int NumPredict { get; set; }

        [JsonProperty("top_p")]
        public float TopP { get; set; }
    }

    public class OllamaResponse
    {
        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }

    #endregion
}
