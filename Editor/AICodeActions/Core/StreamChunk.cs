using System;
using System.Collections.Generic;

namespace AICodeActions.Core
{
    /// <summary>
    /// Represents a chunk of data received during streaming
    /// </summary>
    public class StreamChunk
    {
        /// <summary>
        /// The text delta (new text fragment)
        /// </summary>
        public string Delta { get; set; }
        
        /// <summary>
        /// Type of chunk
        /// </summary>
        public StreamChunkType Type { get; set; }
        
        /// <summary>
        /// Sequential index of this chunk
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// Tool name if this is a tool call chunk
        /// </summary>
        public string ToolName { get; set; }
        
        /// <summary>
        /// Tool parameters if this is a tool call chunk
        /// </summary>
        public Dictionary<string, string> ToolParams { get; set; }
        
        /// <summary>
        /// Reasoning text if this is a reasoning chunk
        /// </summary>
        public string ReasoningText { get; set; }
        
        /// <summary>
        /// Reasoning ID for tracking reasoning blocks
        /// </summary>
        public string ReasoningId { get; set; }
        
        /// <summary>
        /// Timestamp when chunk was received
        /// </summary>
        public DateTime ReceivedAt { get; set; }
        
        /// <summary>
        /// Whether this chunk marks the end of stream
        /// </summary>
        public bool IsFinal { get; set; }
        
        public StreamChunk()
        {
            ReceivedAt = DateTime.Now;
            ToolParams = new Dictionary<string, string>();
        }
        
        public StreamChunk(string delta, StreamChunkType type = StreamChunkType.TextDelta)
        {
            Delta = delta;
            Type = type;
            ReceivedAt = DateTime.Now;
            ToolParams = new Dictionary<string, string>();
        }
        
        public override string ToString()
        {
            return $"[{Type}] Delta: '{Delta}', Tool: {ToolName}, Index: {Index}";
        }
    }
    
    /// <summary>
    /// Types of stream chunks
    /// </summary>
    public enum StreamChunkType
    {
        /// <summary>
        /// Normal text content
        /// </summary>
        TextDelta,
        
        /// <summary>
        /// Start of a tool call
        /// </summary>
        ToolCallStart,
        
        /// <summary>
        /// Tool parameter data
        /// </summary>
        ToolCallDelta,
        
        /// <summary>
        /// End of a tool call
        /// </summary>
        ToolCallEnd,
        
        /// <summary>
        /// Stream completed successfully
        /// </summary>
        Done,
        
        /// <summary>
        /// Stream encountered an error
        /// </summary>
        Error,
        
        /// <summary>
        /// Metadata or system message
        /// </summary>
        Metadata,
        
        /// <summary>
        /// Reasoning/thinking text from model (OpenRouter reasoning tokens)
        /// </summary>
        ReasoningDelta,
        
        /// <summary>
        /// Start of reasoning block
        /// </summary>
        ReasoningStart,
        
        /// <summary>
        /// End of reasoning block
        /// </summary>
        ReasoningEnd
    }
}