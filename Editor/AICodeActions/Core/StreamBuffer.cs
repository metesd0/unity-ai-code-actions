using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Buffers streaming chunks for smooth UI updates
    /// Prevents overwhelming the UI with too many rapid updates
    /// </summary>
    public class StreamBuffer
    {
        private StringBuilder buffer;
        private Queue<StreamChunk> chunkQueue;
        private double lastFlushTime;
        private int totalChunksReceived;
        private int totalCharsReceived;
        
        // Configuration
        public float MinFlushInterval { get; set; } = 0.05f;  // 50ms minimum between flushes
        public int MaxBufferSize { get; set; } = 100;          // Max chars before force flush
        public bool AutoFlush { get; set; } = true;
        
        // Stats
        public int TotalChunksReceived => totalChunksReceived;
        public int TotalCharsReceived => totalCharsReceived;
        public int CurrentBufferSize => buffer.Length;
        public bool HasContent => buffer.Length > 0;
        
        public StreamBuffer()
        {
            buffer = new StringBuilder(256);
            chunkQueue = new Queue<StreamChunk>();
            lastFlushTime = UnityEditor.EditorApplication.timeSinceStartup;
        }
        
        /// <summary>
        /// Add a chunk to the buffer
        /// </summary>
        public void Append(StreamChunk chunk)
        {
            if (chunk == null || string.IsNullOrEmpty(chunk.Delta))
                return;
            
            buffer.Append(chunk.Delta);
            chunkQueue.Enqueue(chunk);
            totalChunksReceived++;
            totalCharsReceived += chunk.Delta.Length;
            
            Debug.Log($"[StreamBuffer] Added chunk: '{chunk.Delta}' (Buffer size: {buffer.Length})");
        }
        
        /// <summary>
        /// Add text directly to buffer
        /// </summary>
        public void Append(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            
            buffer.Append(text);
            totalCharsReceived += text.Length;
        }
        
        /// <summary>
        /// Check if enough time has passed for a flush
        /// </summary>
        public bool ShouldFlush()
        {
            double timeSinceFlush = UnityEditor.EditorApplication.timeSinceStartup - lastFlushTime;
            
            // Force flush if buffer is too large
            if (buffer.Length >= MaxBufferSize)
                return true;
            
            // Flush if enough time passed
            if (timeSinceFlush >= MinFlushInterval)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// Flush partial content (smooth streaming)
        /// </summary>
        public string FlushPartial(int maxChars = 50)
        {
            if (buffer.Length == 0)
                return string.Empty;
            
            int charsToFlush = Math.Min(maxChars, buffer.Length);
            string result = buffer.ToString(0, charsToFlush);
            
            buffer.Remove(0, charsToFlush);
            lastFlushTime = UnityEditor.EditorApplication.timeSinceStartup;
            
            Debug.Log($"[StreamBuffer] Flushed {charsToFlush} chars: '{result}'");
            
            return result;
        }
        
        /// <summary>
        /// Flush all buffered content
        /// </summary>
        public string FlushAll()
        {
            if (buffer.Length == 0)
                return string.Empty;
            
            string result = buffer.ToString();
            buffer.Clear();
            chunkQueue.Clear();
            lastFlushTime = UnityEditor.EditorApplication.timeSinceStartup;
            
            Debug.Log($"[StreamBuffer] Flushed ALL: {result.Length} chars");
            
            return result;
        }
        
        /// <summary>
        /// Peek at buffered content without flushing
        /// </summary>
        public string Peek()
        {
            return buffer.ToString();
        }
        
        /// <summary>
        /// Clear all buffer content
        /// </summary>
        public void Clear()
        {
            buffer.Clear();
            chunkQueue.Clear();
            Debug.Log("[StreamBuffer] Cleared");
        }
        
        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            totalChunksReceived = 0;
            totalCharsReceived = 0;
        }
        
        /// <summary>
        /// Get buffer statistics
        /// </summary>
        public string GetStats()
        {
            return $"Chunks: {totalChunksReceived}, Chars: {totalCharsReceived}, Buffer: {buffer.Length}";
        }
    }
}

