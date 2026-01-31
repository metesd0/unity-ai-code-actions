using UnityEngine;
using UnityEditor;
using AICodeActions.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Main renderer for chat bubbles
    /// Handles layout, alignment, and drawing of messages with modern bubble style
    /// </summary>
    public class ChatBubbleRenderer
    {
        private MessageAnimator animator;
        private CollapsibleCodeBlock codeBlockRenderer;
        private MessageActionBar actionBar;
        private Dictionary<string, float> messageHeights = new Dictionary<string, float>();

        // Avatar textures
        private Texture2D userAvatarTex;
        private Texture2D aiAvatarTex;

        // Events
        public System.Action<string, string> OnApplyCode;   // messageId, code
        public System.Action<string> OnCopyCode;            // code
        public System.Action<string> OnEditMessage;         // messageId
        public System.Action<string> OnRedoMessage;         // messageId

        public ChatBubbleRenderer()
        {
            animator = new MessageAnimator();
            codeBlockRenderer = new CollapsibleCodeBlock();
            actionBar = new MessageActionBar();

            // Wire up action bar events
            actionBar.OnApply = (id, code) => OnApplyCode?.Invoke(id, code);
            actionBar.OnCopy = (code) => OnCopyCode?.Invoke(code);
            actionBar.OnEdit = (id) => OnEditMessage?.Invoke(id);
            actionBar.OnRedo = (id) => OnRedoMessage?.Invoke(id);

            CreateAvatarTextures();
        }

        private void CreateAvatarTextures()
        {
            int size = (int)ChatBubbleStyles.Dimensions.AvatarSize;
            userAvatarTex = RoundedRectTexture.Create(size, size, size / 2f, ChatBubbleStyles.Colors.UserAvatar);
            aiAvatarTex = RoundedRectTexture.Create(size, size, size / 2f, ChatBubbleStyles.Colors.AIAvatar);
        }

        /// <summary>
        /// Update animations - call from EditorApplication.update
        /// </summary>
        public bool Update()
        {
            return animator.Update();
        }

        /// <summary>
        /// Draw a single message with bubble style
        /// </summary>
        public void DrawMessage(ChatMessage message, float availableWidth)
        {
            string messageId = GetMessageId(message);
            bool isUser = message.role == MessageRole.User;
            bool isSystem = message.role == MessageRole.System;

            // Get animation state
            var animState = animator.GetState(messageId);

            // Calculate bubble width
            float maxBubbleWidth = availableWidth * ChatBubbleStyles.Dimensions.BubbleMaxWidthRatio;
            float avatarSpace = ChatBubbleStyles.Dimensions.AvatarSize + ChatBubbleStyles.Dimensions.AvatarMargin * 2;

            // Begin horizontal layout
            EditorGUILayout.BeginHorizontal();

            if (isSystem)
            {
                // System messages are centered
                GUILayout.FlexibleSpace();
                DrawSystemBubble(message, messageId, maxBubbleWidth);
                GUILayout.FlexibleSpace();
            }
            else if (isUser)
            {
                // User messages: right-aligned with avatar on right
                GUILayout.FlexibleSpace();
                DrawUserBubble(message, messageId, maxBubbleWidth - avatarSpace, animState);
                GUILayout.Space(ChatBubbleStyles.Dimensions.AvatarMargin);
                DrawAvatar(true);
            }
            else
            {
                // AI messages: left-aligned with avatar on left
                DrawAvatar(false);
                GUILayout.Space(ChatBubbleStyles.Dimensions.AvatarMargin);
                DrawAIBubble(message, messageId, maxBubbleWidth - avatarSpace, animState);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.EndHorizontal();

            // Check hover state
            Rect lastRect = GUILayoutUtility.GetLastRect();
            bool isHovered = lastRect.Contains(Event.current.mousePosition);
            animator.SetHovered(messageId, isHovered);
        }

        private void DrawAvatar(bool isUser)
        {
            float size = ChatBubbleStyles.Dimensions.AvatarSize;
            Rect avatarRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            // Draw avatar background
            GUI.DrawTexture(avatarRect, isUser ? userAvatarTex : aiAvatarTex);

            // Draw icon
            string icon = isUser ? "👤" : "🤖";
            var iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            GUI.Label(avatarRect, icon, iconStyle);
        }

        private void DrawSystemBubble(ChatMessage message, string messageId, float maxWidth)
        {
            EditorGUILayout.BeginVertical(ChatBubbleStyles.SystemBubbleStyle, GUILayout.MaxWidth(maxWidth));

            // Content
            GUILayout.Label(message.content, ChatBubbleStyles.BubbleTextStyle);

            // Timestamp
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message.timestamp.ToString("HH:mm"), ChatBubbleStyles.TimestampStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawUserBubble(ChatMessage message, string messageId, float maxWidth, MessageAnimator.AnimationState animState)
        {
            // Apply animation
            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, animState.Alpha);

            EditorGUILayout.BeginVertical(ChatBubbleStyles.UserBubbleStyle, GUILayout.MaxWidth(maxWidth));

            // Content
            GUILayout.Label(message.content, ChatBubbleStyles.BubbleTextStyle);

            // Timestamp
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message.timestamp.ToString("HH:mm"), ChatBubbleStyles.TimestampStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUI.color = originalColor;
        }

        private void DrawAIBubble(ChatMessage message, string messageId, float maxWidth, MessageAnimator.AnimationState animState)
        {
            // Apply animation
            Color originalColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, animState.Alpha);

            EditorGUILayout.BeginVertical(ChatBubbleStyles.AIBubbleStyle, GUILayout.MaxWidth(maxWidth));

            // Parse and render content with code blocks
            DrawAIContent(message, messageId);

            // Timestamp
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(message.timestamp.ToString("HH:mm"), ChatBubbleStyles.TimestampStyle);
            EditorGUILayout.EndHorizontal();

            // Action bar for AI messages with code
            if (message.hasCode || ContainsCodeBlock(message.content))
            {
                EditorGUILayout.Space(4);
                string code = ExtractFirstCodeBlock(message.content);
                actionBar.Draw(messageId, code);
            }

            EditorGUILayout.EndVertical();

            GUI.color = originalColor;
        }

        private void DrawAIContent(ChatMessage message, string messageId)
        {
            string content = message.content;

            // Parse content for code blocks
            var segments = ParseContentSegments(content);

            foreach (var segment in segments)
            {
                if (segment.isCode)
                {
                    codeBlockRenderer.Draw(messageId, segment.content, segment.language);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(segment.content))
                    {
                        GUILayout.Label(segment.content.Trim(), ChatBubbleStyles.BubbleTextStyle);
                    }
                }
            }
        }

        /// <summary>
        /// Parse content into text and code segments
        /// </summary>
        private List<ContentSegment> ParseContentSegments(string content)
        {
            var segments = new List<ContentSegment>();

            if (string.IsNullOrEmpty(content))
                return segments;

            // Regex to match code blocks: ```language\ncode\n```
            var codeBlockPattern = new Regex(@"```(\w*)\s*\n?([\s\S]*?)```", RegexOptions.Multiline);
            int lastIndex = 0;

            foreach (Match match in codeBlockPattern.Matches(content))
            {
                // Add text before code block
                if (match.Index > lastIndex)
                {
                    string textBefore = content.Substring(lastIndex, match.Index - lastIndex);
                    if (!string.IsNullOrWhiteSpace(textBefore))
                    {
                        segments.Add(new ContentSegment { content = textBefore, isCode = false });
                    }
                }

                // Add code block
                string language = match.Groups[1].Value;
                string code = match.Groups[2].Value;

                if (string.IsNullOrEmpty(language))
                    language = "csharp"; // Default to C#

                segments.Add(new ContentSegment
                {
                    content = code.Trim(),
                    isCode = true,
                    language = language
                });

                lastIndex = match.Index + match.Length;
            }

            // Add remaining text
            if (lastIndex < content.Length)
            {
                string remaining = content.Substring(lastIndex);
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    segments.Add(new ContentSegment { content = remaining, isCode = false });
                }
            }

            // If no segments found, add entire content as text
            if (segments.Count == 0)
            {
                segments.Add(new ContentSegment { content = content, isCode = false });
            }

            return segments;
        }

        private class ContentSegment
        {
            public string content;
            public bool isCode;
            public string language;
        }

        private bool ContainsCodeBlock(string content)
        {
            return content != null && content.Contains("```");
        }

        private string ExtractFirstCodeBlock(string content)
        {
            if (string.IsNullOrEmpty(content))
                return "";

            var match = Regex.Match(content, @"```\w*\s*\n?([\s\S]*?)```");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        private string GetMessageId(ChatMessage message)
        {
            // Generate a unique ID based on message properties
            return $"{message.role}_{message.timestamp.Ticks}_{message.content?.GetHashCode() ?? 0}";
        }

        /// <summary>
        /// Clear cached data
        /// </summary>
        public void Clear()
        {
            animator.Clear();
            codeBlockRenderer.ClearCollapseStates();
            messageHeights.Clear();
        }
    }
}
