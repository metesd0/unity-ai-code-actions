using System;
using System.Collections.Generic;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Manages conversation history for chat-based interactions
    /// </summary>
    [Serializable]
    public class ConversationManager
    {
        [SerializeField]
        private List<ChatMessage> messages = new List<ChatMessage>();
        
        private const int MAX_HISTORY = 50; // Keep last 50 messages
        private const int CONTEXT_MESSAGES = 10; // Use last 10 for context

        public IReadOnlyList<ChatMessage> Messages => messages;
        
        public string ToMarkdown()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Chat Export");
            sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();
            foreach (var msg in messages)
            {
                string role = msg.role.ToString();
                sb.AppendLine($"## {role} · {msg.timestamp:HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine(msg.content ?? "");
                sb.AppendLine();
            }
            return sb.ToString();
        }
        
        public void AddUserMessage(string content)
        {
            var message = new ChatMessage
            {
                role = MessageRole.User,
                content = content,
                timestamp = DateTime.Now
            };
            
            messages.Add(message);
            TrimHistory();
        }
        
        public void AddAssistantMessage(string content)
        {
            var message = new ChatMessage
            {
                role = MessageRole.Assistant,
                content = content,
                timestamp = DateTime.Now
            };
            
            messages.Add(message);
            TrimHistory();
        }
        
        public void AddSystemMessage(string content)
        {
            var message = new ChatMessage
            {
                role = MessageRole.System,
                content = content,
                timestamp = DateTime.Now
            };
            
            messages.Add(message);
            TrimHistory();
        }
        
        public void Clear()
        {
            messages.Clear();
        }
        
        /// <summary>
        /// Update the content of the last assistant message
        /// </summary>
        public void UpdateLastAssistantMessage(string newContent)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].role == MessageRole.Assistant)
                {
                    messages[i].content = newContent;
                    break;
                }
            }
        }

        /// <summary>
        /// Get the last assistant message
        /// </summary>
        public ChatMessage GetLastAssistantMessage()
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].role == MessageRole.Assistant)
                {
                    return messages[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Get a message by ID
        /// </summary>
        public ChatMessage GetMessageById(string id)
        {
            foreach (var msg in messages)
            {
                if (msg.id == id)
                    return msg;
            }
            return null;
        }

        /// <summary>
        /// Update message state
        /// </summary>
        public void UpdateMessageState(string id, MessageState state)
        {
            var msg = GetMessageById(id);
            if (msg != null)
            {
                msg.state = state;
                msg.isStreaming = (state == MessageState.Streaming);
            }
        }

        /// <summary>
        /// Add a code block to the last assistant message
        /// </summary>
        public void AddCodeBlockToLastMessage(CodeBlock codeBlock)
        {
            var msg = GetLastAssistantMessage();
            if (msg != null)
            {
                msg.codeBlocks.Add(codeBlock);
                msg.hasCode = true;
            }
        }

        /// <summary>
        /// Parse code blocks from message content
        /// </summary>
        public void ParseCodeBlocks(ChatMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.content))
                return;

            message.codeBlocks.Clear();

            var pattern = new System.Text.RegularExpressions.Regex(
                @"```(\w*)\s*\n?([\s\S]*?)```",
                System.Text.RegularExpressions.RegexOptions.Multiline
            );

            foreach (System.Text.RegularExpressions.Match match in pattern.Matches(message.content))
            {
                string language = match.Groups[1].Value;
                string code = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(language))
                    language = "csharp";

                message.codeBlocks.Add(new CodeBlock(code, language));
            }

            message.hasCode = message.codeBlocks.Count > 0;

            // Also set extractedCode for backwards compatibility
            if (message.codeBlocks.Count > 0)
            {
                message.extractedCode = message.codeBlocks[0].code;
            }
        }
        
        /// <summary>
        /// Get conversation history as context for AI
        /// </summary>
        public string GetContextString(int messageCount = CONTEXT_MESSAGES)
        {
            if (messages.Count == 0)
                return "";
            
            var contextMessages = messages.Count > messageCount 
                ? messages.GetRange(messages.Count - messageCount, messageCount)
                : messages;
            
            var result = new System.Text.StringBuilder();
            result.AppendLine("# Previous Conversation:");
            result.AppendLine();
            
            foreach (var msg in contextMessages)
            {
                string roleLabel = msg.role switch
                {
                    MessageRole.User => "User",
                    MessageRole.Assistant => "Assistant",
                    MessageRole.System => "System",
                    _ => "Unknown"
                };
                
                result.AppendLine($"**{roleLabel}:** {msg.content}");
                result.AppendLine();
            }
            
            return result.ToString();
        }
        
        private void TrimHistory()
        {
            if (messages.Count <= MAX_HISTORY)
                return;

            // Count system messages and calculate how many to remove
            int systemCount = 0;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].role == MessageRole.System)
                    systemCount++;
            }

            int toKeep = MAX_HISTORY;
            int toRemove = messages.Count - toKeep;

            if (toRemove <= 0)
                return;

            // Remove oldest non-system messages in-place (no new list allocation)
            int removed = 0;
            for (int i = 0; i < messages.Count && removed < toRemove; )
            {
                if (messages[i].role != MessageRole.System)
                {
                    messages.RemoveAt(i);
                    removed++;
                }
                else
                {
                    i++;
                }
            }
        }
    }
    
    [Serializable]
    public class ChatMessage
    {
        public MessageRole role;
        public string content;
        public DateTime timestamp;
        public bool hasCode;
        public string extractedCode;

        // New fields for modern chat bubbles
        public string id;                           // Unique message identifier
        public List<CodeBlock> codeBlocks;          // Parsed code blocks from content
        public MessageState state;                  // Current message state
        public bool isStreaming;                    // Whether message is still streaming

        public ChatMessage()
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 12);
            codeBlocks = new List<CodeBlock>();
            state = MessageState.Complete;
            isStreaming = false;
        }
    }

    /// <summary>
    /// Represents a code block within a message
    /// </summary>
    [Serializable]
    public class CodeBlock
    {
        public string id;               // Unique block identifier
        public string language;         // Programming language (csharp, json, etc.)
        public string code;             // The actual code content
        public string fileName;         // Optional file name/path
        public bool isCollapsed;        // UI collapse state
        public bool isApplied;          // Whether code has been applied

        public CodeBlock()
        {
            id = Guid.NewGuid().ToString("N").Substring(0, 8);
            language = "csharp";
            isCollapsed = false;
            isApplied = false;
        }

        public CodeBlock(string code, string language = "csharp") : this()
        {
            this.code = code;
            this.language = language;
        }
    }

    /// <summary>
    /// Message state for animations and UI
    /// </summary>
    public enum MessageState
    {
        Pending,        // Message is being prepared
        Streaming,      // Message is actively streaming
        Complete,       // Message is complete
        Error           // Message encountered an error
    }
    
    public enum MessageRole
    {
        System,
        User,
        Assistant
    }
}

