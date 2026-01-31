using UnityEngine;
using UnityEditor;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Action bar with Apply, Copy, Edit, and Redo buttons for message actions
    /// Features hover effects and contextual button visibility
    /// </summary>
    public class MessageActionBar
    {
        // Callbacks
        public System.Action<string, string> OnApply;  // messageId, code
        public System.Action<string> OnCopy;           // code
        public System.Action<string> OnEdit;           // messageId
        public System.Action<string> OnRedo;           // messageId

        private string hoveredButton = null;
        private double hoverStartTime;

        // Button definitions
        private struct ButtonDef
        {
            public string id;
            public string icon;
            public string label;
            public string tooltip;
            public Color normalColor;
            public Color hoverColor;
        }

        private readonly ButtonDef[] buttons = new ButtonDef[]
        {
            new ButtonDef
            {
                id = "apply",
                icon = "✨",
                label = "Apply",
                tooltip = "Apply code to scene",
                normalColor = ChatBubbleStyles.Colors.ApplyButton,
                hoverColor = ChatBubbleStyles.Colors.ApplyButtonHover
            },
            new ButtonDef
            {
                id = "copy",
                icon = "📋",
                label = "Copy",
                tooltip = "Copy to clipboard",
                normalColor = ChatBubbleStyles.Colors.CopyButton,
                hoverColor = ChatBubbleStyles.Colors.CopyButtonHover
            },
            new ButtonDef
            {
                id = "edit",
                icon = "✏️",
                label = "Edit",
                tooltip = "Edit this message",
                normalColor = ChatBubbleStyles.Colors.EditButton,
                hoverColor = ChatBubbleStyles.Colors.EditButtonHover
            },
            new ButtonDef
            {
                id = "redo",
                icon = "🔄",
                label = "Redo",
                tooltip = "Regenerate response",
                normalColor = ChatBubbleStyles.Colors.RedoButton,
                hoverColor = ChatBubbleStyles.Colors.RedoButtonHover
            }
        };

        /// <summary>
        /// Draw the action bar
        /// </summary>
        public void Draw(string messageId, string code = null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            foreach (var button in buttons)
            {
                // Skip Apply if no code
                if (button.id == "apply" && string.IsNullOrEmpty(code))
                    continue;

                DrawButton(button, messageId, code);
                GUILayout.Space(ChatBubbleStyles.Dimensions.ActionButtonSpacing);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawButton(ButtonDef button, string messageId, string code)
        {
            string buttonKey = $"{messageId}_{button.id}";

            // Calculate button size
            GUIContent content = new GUIContent($"{button.icon} {button.label}", button.tooltip);
            float width = Mathf.Max(
                ChatBubbleStyles.Dimensions.ActionButtonMinWidth,
                ChatBubbleStyles.ActionButtonStyle.CalcSize(content).x + 12
            );

            // Check hover state
            Rect buttonRect = GUILayoutUtility.GetRect(
                content,
                ChatBubbleStyles.ActionButtonStyle,
                GUILayout.Width(width),
                GUILayout.Height(ChatBubbleStyles.Dimensions.ActionButtonHeight)
            );

            bool isHovered = buttonRect.Contains(Event.current.mousePosition);

            if (isHovered && hoveredButton != buttonKey)
            {
                hoveredButton = buttonKey;
                hoverStartTime = EditorApplication.timeSinceStartup;
            }
            else if (!isHovered && hoveredButton == buttonKey)
            {
                hoveredButton = null;
            }

            // Calculate hover progress for smooth transition
            float hoverProgress = 0f;
            if (hoveredButton == buttonKey)
            {
                float elapsed = (float)(EditorApplication.timeSinceStartup - hoverStartTime);
                hoverProgress = Mathf.Clamp01(elapsed / ChatBubbleStyles.Animation.HoverTransitionDuration);
            }

            // Draw button background
            Color bgColor = Color.Lerp(button.normalColor, button.hoverColor, hoverProgress);
            Texture2D bgTex = RoundedRectTexture.Create(
                (int)width,
                (int)ChatBubbleStyles.Dimensions.ActionButtonHeight,
                6f,
                bgColor
            );
            GUI.DrawTexture(buttonRect, bgTex);

            // Draw button label
            GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = isHovered ? FontStyle.Bold : FontStyle.Normal
            };
            labelStyle.normal.textColor = ChatBubbleStyles.Colors.ButtonText;

            GUI.Label(buttonRect, content, labelStyle);

            // Handle click
            if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
            {
                HandleButtonClick(button.id, messageId, code);
                Event.current.Use();
            }
        }

        private void HandleButtonClick(string buttonId, string messageId, string code)
        {
            switch (buttonId)
            {
                case "apply":
                    OnApply?.Invoke(messageId, code);
                    break;

                case "copy":
                    if (!string.IsNullOrEmpty(code))
                    {
                        GUIUtility.systemCopyBuffer = code;
                        OnCopy?.Invoke(code);
                    }
                    break;

                case "edit":
                    OnEdit?.Invoke(messageId);
                    break;

                case "redo":
                    OnRedo?.Invoke(messageId);
                    break;
            }
        }

        /// <summary>
        /// Draw a compact action bar (icons only)
        /// </summary>
        public void DrawCompact(string messageId, string code = null)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            foreach (var button in buttons)
            {
                if (button.id == "apply" && string.IsNullOrEmpty(code))
                    continue;

                DrawCompactButton(button, messageId, code);
                GUILayout.Space(2);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCompactButton(ButtonDef button, string messageId, string code)
        {
            GUIContent content = new GUIContent(button.icon, button.tooltip);
            float size = 24f;

            Rect buttonRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

            bool isHovered = buttonRect.Contains(Event.current.mousePosition);
            Color bgColor = isHovered ? button.hoverColor : button.normalColor;

            Texture2D bgTex = RoundedRectTexture.Create((int)size, (int)size, 4f, bgColor);
            GUI.DrawTexture(buttonRect, bgTex);

            GUIStyle iconStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
            GUI.Label(buttonRect, content, iconStyle);

            if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
            {
                HandleButtonClick(button.id, messageId, code);
                Event.current.Use();
            }
        }
    }
}
