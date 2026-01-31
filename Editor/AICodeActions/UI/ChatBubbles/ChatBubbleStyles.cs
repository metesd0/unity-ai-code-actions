using UnityEngine;
using UnityEditor;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Centralized style definitions for chat bubbles
    /// Provides consistent theming across all bubble components
    /// </summary>
    public static class ChatBubbleStyles
    {
        // Colors - Dark Theme
        public static class Colors
        {
            // User bubble (right side)
            public static readonly Color UserBubbleBg = new Color(0.2f, 0.45f, 0.75f, 0.95f);      // Blue
            public static readonly Color UserBubbleText = new Color(1f, 1f, 1f, 1f);               // White
            public static readonly Color UserBubbleBorder = new Color(0.3f, 0.55f, 0.85f, 1f);     // Light blue

            // AI bubble (left side)
            public static readonly Color AIBubbleBg = new Color(0.18f, 0.18f, 0.2f, 0.95f);        // Dark gray
            public static readonly Color AIBubbleText = new Color(0.9f, 0.9f, 0.9f, 1f);           // Light gray
            public static readonly Color AIBubbleBorder = new Color(0.28f, 0.28f, 0.3f, 1f);       // Medium gray

            // System message
            public static readonly Color SystemBubbleBg = new Color(0.35f, 0.35f, 0.2f, 0.85f);    // Yellow-ish
            public static readonly Color SystemBubbleText = new Color(0.95f, 0.95f, 0.8f, 1f);     // Cream

            // Code blocks
            public static readonly Color CodeBlockBg = new Color(0.1f, 0.1f, 0.12f, 0.98f);        // Near black
            public static readonly Color CodeBlockBorder = new Color(0.25f, 0.25f, 0.28f, 1f);     // Dark gray
            public static readonly Color CodeBlockHeader = new Color(0.15f, 0.15f, 0.17f, 1f);     // Slightly lighter

            // Action buttons
            public static readonly Color ButtonNormal = new Color(0.25f, 0.25f, 0.28f, 1f);
            public static readonly Color ButtonHover = new Color(0.35f, 0.35f, 0.4f, 1f);
            public static readonly Color ButtonPressed = new Color(0.2f, 0.5f, 0.8f, 1f);
            public static readonly Color ButtonText = new Color(0.85f, 0.85f, 0.85f, 1f);

            // Special buttons
            public static readonly Color ApplyButton = new Color(0.2f, 0.6f, 0.3f, 1f);            // Green
            public static readonly Color ApplyButtonHover = new Color(0.25f, 0.7f, 0.35f, 1f);
            public static readonly Color CopyButton = new Color(0.3f, 0.45f, 0.65f, 1f);           // Blue
            public static readonly Color CopyButtonHover = new Color(0.35f, 0.55f, 0.75f, 1f);
            public static readonly Color EditButton = new Color(0.55f, 0.45f, 0.25f, 1f);          // Orange
            public static readonly Color EditButtonHover = new Color(0.65f, 0.55f, 0.3f, 1f);
            public static readonly Color RedoButton = new Color(0.5f, 0.35f, 0.55f, 1f);           // Purple
            public static readonly Color RedoButtonHover = new Color(0.6f, 0.45f, 0.65f, 1f);

            // Preview cards
            public static readonly Color CardBg = new Color(0.15f, 0.15f, 0.17f, 0.95f);
            public static readonly Color CardBorder = new Color(0.3f, 0.3f, 0.33f, 1f);
            public static readonly Color CardHeader = new Color(0.12f, 0.12f, 0.14f, 1f);

            // Timestamp
            public static readonly Color Timestamp = new Color(0.6f, 0.6f, 0.6f, 0.8f);

            // Avatar backgrounds
            public static readonly Color UserAvatar = new Color(0.3f, 0.55f, 0.85f, 1f);
            public static readonly Color AIAvatar = new Color(0.4f, 0.7f, 0.5f, 1f);
        }

        // Dimensions
        public static class Dimensions
        {
            public const float BubbleRadius = 12f;
            public const float BubbleMaxWidthRatio = 0.75f;  // 75% of available width
            public const float BubbleMinWidth = 100f;
            public const float BubblePadding = 12f;
            public const float BubbleMargin = 8f;

            public const float AvatarSize = 32f;
            public const float AvatarMargin = 8f;

            public const float CodeBlockRadius = 8f;
            public const float CodeBlockPadding = 10f;
            public const float CodeBlockHeaderHeight = 28f;

            public const float ActionBarHeight = 32f;
            public const float ActionButtonHeight = 24f;
            public const float ActionButtonMinWidth = 60f;
            public const float ActionButtonSpacing = 6f;

            public const float CardRadius = 8f;
            public const float CardPadding = 10f;

            public const float MessageSpacing = 12f;
            public const float TimestampFontSize = 10f;
        }

        // Animation
        public static class Animation
        {
            public const float SlideInDuration = 0.3f;
            public const float FadeInDuration = 0.2f;
            public const float HoverTransitionDuration = 0.15f;

            // Easing functions
            public static float EaseOutCubic(float t)
            {
                return 1f - Mathf.Pow(1f - t, 3f);
            }

            public static float EaseOutQuad(float t)
            {
                return 1f - (1f - t) * (1f - t);
            }

            public static float EaseInOutCubic(float t)
            {
                return t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            }
        }

        // Cached GUIStyles
        private static GUIStyle _userBubbleStyle;
        private static GUIStyle _aiBubbleStyle;
        private static GUIStyle _systemBubbleStyle;
        private static GUIStyle _codeBlockStyle;
        private static GUIStyle _codeHeaderStyle;
        private static GUIStyle _actionButtonStyle;
        private static GUIStyle _timestampStyle;
        private static GUIStyle _bubbleTextStyle;

        public static GUIStyle UserBubbleStyle
        {
            get
            {
                if (_userBubbleStyle == null)
                {
                    _userBubbleStyle = CreateBubbleStyle(Colors.UserBubbleBg, Colors.UserBubbleText);
                }
                return _userBubbleStyle;
            }
        }

        public static GUIStyle AIBubbleStyle
        {
            get
            {
                if (_aiBubbleStyle == null)
                {
                    _aiBubbleStyle = CreateBubbleStyle(Colors.AIBubbleBg, Colors.AIBubbleText);
                }
                return _aiBubbleStyle;
            }
        }

        public static GUIStyle SystemBubbleStyle
        {
            get
            {
                if (_systemBubbleStyle == null)
                {
                    _systemBubbleStyle = CreateBubbleStyle(Colors.SystemBubbleBg, Colors.SystemBubbleText);
                    _systemBubbleStyle.alignment = TextAnchor.MiddleCenter;
                    _systemBubbleStyle.fontStyle = FontStyle.Italic;
                }
                return _systemBubbleStyle;
            }
        }

        public static GUIStyle CodeBlockStyle
        {
            get
            {
                if (_codeBlockStyle == null)
                {
                    _codeBlockStyle = new GUIStyle(EditorStyles.textArea)
                    {
                        font = GetMonospaceFont(),
                        fontSize = 11,
                        richText = true,
                        wordWrap = false,
                        padding = new RectOffset(
                            (int)Dimensions.CodeBlockPadding,
                            (int)Dimensions.CodeBlockPadding,
                            (int)Dimensions.CodeBlockPadding,
                            (int)Dimensions.CodeBlockPadding
                        )
                    };
                    _codeBlockStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                    _codeBlockStyle.normal.background = RoundedRectTexture.Create(
                        32, 32, Dimensions.CodeBlockRadius, Colors.CodeBlockBg
                    );
                }
                return _codeBlockStyle;
            }
        }

        public static GUIStyle CodeHeaderStyle
        {
            get
            {
                if (_codeHeaderStyle == null)
                {
                    _codeHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleLeft,
                        padding = new RectOffset(8, 8, 4, 4)
                    };
                    _codeHeaderStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                    _codeHeaderStyle.normal.background = RoundedRectTexture.Create(
                        32, 32, Dimensions.CodeBlockRadius, Colors.CodeBlockHeader
                    );
                }
                return _codeHeaderStyle;
            }
        }

        public static GUIStyle ActionButtonStyle
        {
            get
            {
                if (_actionButtonStyle == null)
                {
                    _actionButtonStyle = new GUIStyle(EditorStyles.miniButton)
                    {
                        fontSize = 11,
                        alignment = TextAnchor.MiddleCenter,
                        padding = new RectOffset(8, 8, 4, 4),
                        margin = new RectOffset(2, 2, 2, 2),
                        fixedHeight = Dimensions.ActionButtonHeight
                    };
                    _actionButtonStyle.normal.textColor = Colors.ButtonText;
                    _actionButtonStyle.normal.background = RoundedRectTexture.Create(
                        64, (int)Dimensions.ActionButtonHeight, 6f, Colors.ButtonNormal
                    );
                    _actionButtonStyle.hover.background = RoundedRectTexture.Create(
                        64, (int)Dimensions.ActionButtonHeight, 6f, Colors.ButtonHover
                    );
                    _actionButtonStyle.active.background = RoundedRectTexture.Create(
                        64, (int)Dimensions.ActionButtonHeight, 6f, Colors.ButtonPressed
                    );
                }
                return _actionButtonStyle;
            }
        }

        public static GUIStyle TimestampStyle
        {
            get
            {
                if (_timestampStyle == null)
                {
                    _timestampStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = (int)Dimensions.TimestampFontSize,
                        alignment = TextAnchor.MiddleRight
                    };
                    _timestampStyle.normal.textColor = Colors.Timestamp;
                }
                return _timestampStyle;
            }
        }

        public static GUIStyle BubbleTextStyle
        {
            get
            {
                if (_bubbleTextStyle == null)
                {
                    _bubbleTextStyle = new GUIStyle(EditorStyles.label)
                    {
                        wordWrap = true,
                        richText = true,
                        fontSize = 12,
                        padding = new RectOffset(0, 0, 0, 0)
                    };
                }
                return _bubbleTextStyle;
            }
        }

        private static GUIStyle CreateBubbleStyle(Color bgColor, Color textColor)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                richText = true,
                fontSize = 12,
                padding = new RectOffset(
                    (int)Dimensions.BubblePadding,
                    (int)Dimensions.BubblePadding,
                    (int)Dimensions.BubblePadding,
                    (int)Dimensions.BubblePadding
                )
            };
            style.normal.textColor = textColor;
            style.normal.background = RoundedRectTexture.Create(
                64, 64, Dimensions.BubbleRadius, bgColor
            );
            return style;
        }

        private static Font GetMonospaceFont()
        {
            // Try to get a monospace font
            Font font = Font.CreateDynamicFontFromOSFont("Consolas", 11);
            if (font == null)
            {
                font = Font.CreateDynamicFontFromOSFont("Courier New", 11);
            }
            return font;
        }

        /// <summary>
        /// Clear cached styles (call when reloading)
        /// </summary>
        public static void ClearStyles()
        {
            _userBubbleStyle = null;
            _aiBubbleStyle = null;
            _systemBubbleStyle = null;
            _codeBlockStyle = null;
            _codeHeaderStyle = null;
            _actionButtonStyle = null;
            _timestampStyle = null;
            _bubbleTextStyle = null;

            RoundedRectTexture.ClearCache();
        }
    }
}
