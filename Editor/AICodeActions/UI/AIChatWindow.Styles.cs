using UnityEditor;
using UnityEngine;
using AICodeActions.UI.ChatBubbles;

namespace AICodeActions.UI
{
    /// <summary>
    /// GUI styles initialization for AI Chat Window
    /// Updated with modern bubble texture support
    /// </summary>
    public partial class AIChatWindow
    {
        private void InitializeStyles()
        {
            // Initialize legacy styles for backward compatibility
            if (userMessageStyle == null)
            {
                userMessageStyle = CreateMessageStyle(new Color(0.3f, 0.5f, 0.8f, 0.2f));
            }

            if (assistantMessageStyle == null)
            {
                assistantMessageStyle = CreateMessageStyle(new Color(0.2f, 0.2f, 0.2f, 0.3f));
            }

            if (systemMessageStyle == null)
            {
                systemMessageStyle = CreateMessageStyle(new Color(0.5f, 0.5f, 0.2f, 0.2f));
                systemMessageStyle.alignment = TextAnchor.MiddleCenter;
                systemMessageStyle.fontStyle = FontStyle.Italic;
            }

            if (codeBlockStyle == null)
            {
                codeBlockStyle = new GUIStyle(EditorStyles.textArea)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                    padding = new RectOffset(10, 10, 10, 10)
                };
                codeBlockStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            }

            // Initialize modern bubble textures
            InitializeBubbleTextures();
        }

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

        private GUIStyle CreateMessageStyle(Color backgroundColor)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true,
                richText = true
            };
            style.normal.background = MakeTex(2, 2, backgroundColor);
            return style;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        /// <summary>
        /// Clear all cached styles and textures
        /// </summary>
        private void ClearStyles()
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
