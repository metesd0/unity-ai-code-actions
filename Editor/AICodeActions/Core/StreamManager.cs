using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch; // Alias to avoid Debug conflict

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages streaming responses from AI providers
    /// Coordinates chunk buffering, tool detection, and UI updates
    /// Thread-safe: doesn't use Unity Editor API from background threads
    /// </summary>
    public class StreamManager
    {
        private StreamBuffer buffer;
        private StringBuilder fullResponse;
        private StringBuilder reasoningText; // Accumulate reasoning tokens separately
        private CancellationTokenSource cancellationTokenSource;
        private bool isStreaming;
        private Stopwatch streamStopwatch; // Thread-safe timing
        
        // Callbacks
        public Action<string> OnTextUpdate;          // Incremental text update
        public Action<string> OnToolDetected;        // Tool call detected
        public Action<string> OnComplete;            // Stream completed with full text
        public Action<string> OnError;               // Stream error
        public Action<float> OnProgress;             // Progress update (0-1)
        public Action<string> OnReasoningUpdate;     // Reasoning/thinking tokens update
        public Action<CodeBlockInfo> OnCodeBlockDetected;  // Code block detected during streaming
        public Action<string> OnCodeBlockUpdate;     // Live code update for preview panel
        
        // Configuration
        public float UpdateInterval { get; set; } = 0.05f;  // 50ms between UI updates
        public int CharsPerUpdate { get; set; } = 50;       // Max chars per update
        public bool EnableToolDetection { get; set; } = true;
        public bool EnableCodeBlockDetection { get; set; } = true;  // Enable live code block detection

        // State
        public bool IsStreaming => isStreaming;
        public string CurrentText => fullResponse?.ToString() ?? "";
        public double ElapsedTime => streamStopwatch?.Elapsed.TotalSeconds ?? 0;

        private double lastUpdateTime;

        // Code block detection state
        private bool inCodeBlock = false;
        private StringBuilder currentCodeBlock = new StringBuilder();
        private string currentCodeLanguage = "csharp";
        
        public StreamManager()
        {
            buffer = new StreamBuffer();
            fullResponse = new StringBuilder();
            reasoningText = new StringBuilder();
            currentCodeBlock = new StringBuilder();
            streamStopwatch = new Stopwatch();
        }
        
        /// <summary>
        /// Start streaming from a provider
        /// </summary>
        public async Task StartStreamAsync(
            Func<Action<StreamChunk>, CancellationToken, Task> streamFunction,
            CancellationToken externalToken = default)
        {
            if (isStreaming)
            {
                Debug.LogWarning("[StreamManager] Already streaming");
                return;
            }
            
            try
            {
                isStreaming = true;
                streamStopwatch.Restart(); // Thread-safe timing
                fullResponse.Clear();
                buffer.Clear();
                
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                
                Debug.Log("[StreamManager] Stream started");
                
                // Start the stream (stay on background thread)
                await streamFunction(OnChunkReceived, cancellationTokenSource.Token).ConfigureAwait(false);
                
                // Flush any remaining buffer
                FlushBuffer();
                
                // Complete
                string finalText = fullResponse.ToString();
                OnComplete?.Invoke(finalText);
                OnProgress?.Invoke(1.0f);
                
                Debug.Log($"[StreamManager] Stream completed: {finalText.Length} chars in {ElapsedTime:F2}s");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StreamManager] Stream cancelled");
                OnError?.Invoke("Stream cancelled by user");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StreamManager] Stream error: {ex.Message}");
                OnError?.Invoke($"Stream error: {ex.Message}");
            }
            finally
            {
                isStreaming = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Handle incoming chunk
        /// </summary>
        private void OnChunkReceived(StreamChunk chunk)
        {
            if (chunk == null)
                return; 
            
            switch (chunk.Type)
            {
                case StreamChunkType.TextDelta:
                    HandleTextChunk(chunk);
                    break;
                    
                case StreamChunkType.ReasoningDelta:
                    HandleReasoningChunk(chunk);
                    break;
                    
                case StreamChunkType.ToolCallStart:
                    HandleToolStart(chunk);
                    break;
                    
                case StreamChunkType.ToolCallDelta:
                    HandleToolDelta(chunk);
                    break;
                    
                case StreamChunkType.ToolCallEnd:
                    HandleToolEnd(chunk);
                    break;
                    
                case StreamChunkType.Done:
                    Debug.Log("[StreamManager] Received DONE signal");
                    break;
                    
                case StreamChunkType.Error:
                    OnError?.Invoke(chunk.Delta);
                    break;
            }
        }
        
        /// <summary>
        /// Handle text chunk
        /// </summary>
        private void HandleTextChunk(StreamChunk chunk)
        {
            buffer.Append(chunk);
            fullResponse.Append(chunk.Delta);

            // Detect code blocks for live preview
            if (EnableCodeBlockDetection)
            {
                DetectCodeBlocks(chunk.Delta);
            }

            // Check if we should update UI
            if (buffer.ShouldFlush())
            {
                FlushBuffer();
            }

            // If a full tool block just closed, flush immediately for responsiveness
            if (EnableToolDetection && chunk.Delta != null && chunk.Delta.Contains("[/TOOL]"))
            {
                FlushBuffer();
            }
        }

        /// <summary>
        /// Detect code blocks in streaming text
        /// </summary>
        private void DetectCodeBlocks(string delta)
        {
            if (string.IsNullOrEmpty(delta))
                return;

            string fullText = fullResponse.ToString();

            // Check for code block start: ```language
            if (!inCodeBlock)
            {
                int startIdx = fullText.LastIndexOf("```");
                if (startIdx >= 0)
                {
                    // Check if this is an opening block (not a closing one)
                    string afterMarker = fullText.Substring(startIdx + 3);
                    if (!afterMarker.Contains("```"))
                    {
                        inCodeBlock = true;
                        currentCodeBlock.Clear();

                        // Extract language
                        int newlineIdx = afterMarker.IndexOf('\n');
                        if (newlineIdx > 0)
                        {
                            currentCodeLanguage = afterMarker.Substring(0, newlineIdx).Trim();
                            if (string.IsNullOrEmpty(currentCodeLanguage))
                                currentCodeLanguage = "csharp";

                            // Start accumulating code
                            string codeStart = afterMarker.Substring(newlineIdx + 1);
                            currentCodeBlock.Append(codeStart);

                            // Notify live preview
                            OnCodeBlockUpdate?.Invoke(currentCodeBlock.ToString());
                        }
                    }
                }
            }
            else
            {
                // We're inside a code block, accumulate and check for end
                currentCodeBlock.Append(delta);

                string codeContent = currentCodeBlock.ToString();

                // Check for closing ```
                int endIdx = codeContent.LastIndexOf("```");
                if (endIdx >= 0)
                {
                    // Code block ended
                    inCodeBlock = false;
                    string finalCode = codeContent.Substring(0, endIdx).Trim();

                    // Notify complete code block
                    var codeBlockInfo = new CodeBlockInfo
                    {
                        Language = currentCodeLanguage,
                        Code = finalCode,
                        IsComplete = true
                    };
                    OnCodeBlockDetected?.Invoke(codeBlockInfo);

                    currentCodeBlock.Clear();
                }
                else
                {
                    // Still streaming, update live preview
                    OnCodeBlockUpdate?.Invoke(codeContent);
                }
            }
        }
        
        /// <summary>
        /// Handle reasoning/thinking chunk (OpenRouter reasoning tokens)
        /// </summary>
        private void HandleReasoningChunk(StreamChunk chunk)
        {
            if (string.IsNullOrEmpty(chunk.ReasoningText))
                return;
            
            reasoningText.Append(chunk.ReasoningText);
            
            // Notify callback with accumulated reasoning text
            OnReasoningUpdate?.Invoke(chunk.ReasoningText);
            
            Debug.Log($"[StreamManager] Reasoning chunk received: {chunk.ReasoningText.Substring(0, Math.Min(50, chunk.ReasoningText.Length))}...");
        }
        
        /// <summary>
        /// Flush buffer to UI
        /// </summary>
        private void FlushBuffer()
        {
            if (!buffer.HasContent)
                return;
            
            string text = buffer.FlushAll();
            
            if (!string.IsNullOrEmpty(text))
            {
                OnTextUpdate?.Invoke(text);
                lastUpdateTime = streamStopwatch.Elapsed.TotalSeconds;
            }
        }
        
        /// <summary>
        /// Handle tool call start
        /// </summary>
        private void HandleToolStart(StreamChunk chunk)
        {
            // Flush any pending text before tool
            FlushBuffer();
            
            Debug.Log($"[StreamManager] Tool call started: {chunk.ToolName}");
        }
        
        /// <summary>
        /// Handle tool parameter delta
        /// </summary>
        private void HandleToolDelta(StreamChunk chunk)
        {
            // Buffer tool parameters
            Debug.Log($"[StreamManager] Tool param: {chunk.Delta}");
        }
        
        /// <summary>
        /// Handle tool call end
        /// </summary>
        private void HandleToolEnd(StreamChunk chunk)
        {
            if (EnableToolDetection)
            {
                string toolInfo = $"[TOOL:{chunk.ToolName}]";
                OnToolDetected?.Invoke(toolInfo);
            }
            
            Debug.Log($"[StreamManager] Tool call ended: {chunk.ToolName}");
        }
        
        /// <summary>
        /// Update method - call this from EditorApplication.update
        /// </summary>
        public void Update()
        {
            if (!isStreaming)
                return;
            
            double timeSinceUpdate = streamStopwatch.Elapsed.TotalSeconds - lastUpdateTime;
            
            // Periodic flush even if buffer isn't full
            if (timeSinceUpdate >= UpdateInterval && buffer.HasContent)
            {
                string text = buffer.FlushPartial(CharsPerUpdate);
                
                if (!string.IsNullOrEmpty(text))
                {
                    OnTextUpdate?.Invoke(text);
                    lastUpdateTime = streamStopwatch.Elapsed.TotalSeconds;
                }
            }
        }
        
        /// <summary>
        /// Cancel streaming
        /// </summary>
        public void Cancel()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                Debug.Log("[StreamManager] Cancelling stream");
                cancellationTokenSource.Cancel();
            }
        }
        
        /// <summary>
        /// Get streaming statistics
        /// </summary>
        public string GetStats()
        {
            return $"Streaming: {isStreaming}, Elapsed: {ElapsedTime:F2}s, {buffer.GetStats()}";
        }
        
        /// <summary>
        /// Get all buffered text accumulated so far
        /// </summary>
        public string GetBufferedText()
        {
            return fullResponse?.ToString() ?? "";
        }

        /// <summary>
        /// Check if currently inside a code block
        /// </summary>
        public bool IsInCodeBlock => inCodeBlock;

        /// <summary>
        /// Get current code block content (while streaming)
        /// </summary>
        public string GetCurrentCodeBlock()
        {
            return currentCodeBlock?.ToString() ?? "";
        }
    }

    /// <summary>
    /// Information about a detected code block
    /// </summary>
    public class CodeBlockInfo
    {
        public string Language { get; set; } = "csharp";
        public string Code { get; set; } = "";
        public bool IsComplete { get; set; } = false;
        public string FileName { get; set; } = "";
    }
}