using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages streaming responses from AI providers
    /// Coordinates chunk buffering, tool detection, and UI updates
    /// </summary>
    public class StreamManager
    {
        private StreamBuffer buffer;
        private StringBuilder fullResponse;
        private CancellationTokenSource cancellationTokenSource;
        private bool isStreaming;
        private double streamStartTime;
        
        // Callbacks
        public Action<string> OnTextUpdate;          // Incremental text update
        public Action<string> OnToolDetected;        // Tool call detected
        public Action<string> OnComplete;            // Stream completed with full text
        public Action<string> OnError;               // Stream error
        public Action<float> OnProgress;             // Progress update (0-1)
        
        // Configuration
        public float UpdateInterval { get; set; } = 0.05f;  // 50ms between UI updates
        public int CharsPerUpdate { get; set; } = 50;       // Max chars per update
        public bool EnableToolDetection { get; set; } = true;
        
        // State
        public bool IsStreaming => isStreaming;
        public string CurrentText => fullResponse?.ToString() ?? "";
        public double ElapsedTime => UnityEditor.EditorApplication.timeSinceStartup - streamStartTime;
        
        private double lastUpdateTime;
        
        public StreamManager()
        {
            buffer = new StreamBuffer();
            fullResponse = new StringBuilder();
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
                streamStartTime = UnityEditor.EditorApplication.timeSinceStartup;
                fullResponse.Clear();
                buffer.Clear();
                
                cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                
                Debug.Log("[StreamManager] Stream started");
                
                // Start the stream
                await streamFunction(OnChunkReceived, cancellationTokenSource.Token);
                
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
            
            // Check if we should update UI
            if (buffer.ShouldFlush())
            {
                FlushBuffer();
            }
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
                lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
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
            
            double timeSinceUpdate = UnityEditor.EditorApplication.timeSinceStartup - lastUpdateTime;
            
            // Periodic flush even if buffer isn't full
            if (timeSinceUpdate >= UpdateInterval && buffer.HasContent)
            {
                string text = buffer.FlushPartial(CharsPerUpdate);
                
                if (!string.IsNullOrEmpty(text))
                {
                    OnTextUpdate?.Invoke(text);
                    lastUpdateTime = UnityEditor.EditorApplication.timeSinceStartup;
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
    }
}

