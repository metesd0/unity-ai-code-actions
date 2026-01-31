using UnityEditor;
using UnityEngine;
using AICodeActions.UI.ChatBubbles;

namespace AICodeActions.UI
{
    /// <summary>
    /// Additional style utilities for AI Chat Window
    /// Extends existing InitializeStyles with bubble texture support
    /// </summary>
    public partial class AIChatWindow
    {
        // Note: InitializeStyles and MakeTex are defined in main AIChatWindow.cs
        // This partial class adds helper methods for bubble textures

        private void InitializeBubbleTextures()
        {
            // Pre-create commonly used bubble textures for better performance
            // These are cached internally by RoundedRectTexture

            // User bubble (right side - blue)
            RoundedRectTexture.Create(
                64, 64,
                ChatBubbleStyles.Dimensions.BubbleRadius,
                ChatBubbleStyles.Colors.UserBubbleBg
            );

            // AI bubble (left side - dark gray)
            RoundedRectTexture.Create(
                64, 64,
                ChatBubbleStyles.Dimensions.BubbleRadius,
                ChatBubbleStyles.Colors.AIBubbleBg
            );

            // System bubble (centered - yellow-ish)
            RoundedRectTexture.Create(
                64, 64,
                ChatBubbleStyles.Dimensions.BubbleRadius,
                ChatBubbleStyles.Colors.SystemBubbleBg
            );

            // Code block background
            RoundedRectTexture.Create(
                32, 32,
                ChatBubbleStyles.Dimensions.CodeBlockRadius,
                ChatBubbleStyles.Colors.CodeBlockBg
            );

            // Action buttons
            RoundedRectTexture.Create(
                64, (int)ChatBubbleStyles.Dimensions.ActionButtonHeight,
                6f,
                ChatBubbleStyles.Colors.ButtonNormal
            );
        }

        /// <summary>
        /// Clear all cached styles and textures
        /// </summary>
        private void ClearAllStyles()
        {
            userMessageStyle = null;
            assistantMessageStyle = null;
            systemMessageStyle = null;
            codeBlockStyle = null;

            // Clear bubble style cache
            ChatBubbleStyles.ClearStyles();
        }
    }
}
