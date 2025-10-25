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
            if (messages.Count > MAX_HISTORY)
            {
                // Keep system messages and recent history
                var systemMessages = messages.FindAll(m => m.role == MessageRole.System);
                var recentMessages = messages.GetRange(messages.Count - (MAX_HISTORY - systemMessages.Count), MAX_HISTORY - systemMessages.Count);
                
                messages.Clear();
                messages.AddRange(systemMessages);
                messages.AddRange(recentMessages);
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
    }
    
    public enum MessageRole
    {
        System,
        User,
        Assistant
    }
}

