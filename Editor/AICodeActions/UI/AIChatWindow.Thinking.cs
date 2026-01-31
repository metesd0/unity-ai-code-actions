using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Thinking animation and footer for AI Chat Window
    /// </summary>
    public partial class AIChatWindow
    {
        private void UpdateThinkingAnimation()
        {
            if (string.IsNullOrEmpty(fullThinkingBuffer))
            {
                FadeOutThinking();
                return;
            }

            if (thinkingTypingIndex < fullThinkingBuffer.Length)
            {
                AnimateTyping();
            }
            else
            {
                HandleThinkingComplete();
            }
        }

        private void FadeOutThinking()
        {
            if (thinkingAlpha > 0)
            {
                thinkingAlpha -= Time.deltaTime / THINKING_FADE_DURATION;
                if (thinkingAlpha <= 0)
                {
                    thinkingAlpha = 0;
                    liveThinkingText = "";
                    thinkingTypingIndex = 0;
                }
                Repaint();
            }
        }

        private void AnimateTyping()
        {
            thinkingFadeTimer += Time.deltaTime;

            if (thinkingFadeTimer >= THINKING_TYPING_SPEED)
            {
                thinkingFadeTimer = 0;
                thinkingTypingIndex++;
                liveThinkingText = fullThinkingBuffer.Substring(0, thinkingTypingIndex);

                // Fade in while typing
                thinkingAlpha = Mathf.Min(1f, thinkingAlpha + Time.deltaTime * 2);
                Repaint();
            }
        }

        private void HandleThinkingComplete()
        {
            thinkingAlpha = 1f;

            if (shouldFadeImmediately)
            {
                fullThinkingBuffer = "";
                thinkingFadeTimer = 0;
                shouldFadeImmediately = false;
            }
            else
            {
                thinkingFadeTimer += Time.deltaTime;

                if (thinkingFadeTimer >= THINKING_VISIBLE_TIME)
                {
                    fullThinkingBuffer = "";
                    thinkingFadeTimer = 0;
                }
            }
        }

        private void DrawThinkingFooterInline()
        {
            Color originalColor = GUI.color;
            Color originalBgColor = GUI.backgroundColor;

            GUI.color = new Color(1f, 1f, 1f, thinkingAlpha * 0.6f);
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.3f, thinkingAlpha * 0.5f);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            Color textColor = EditorGUIUtility.isProSkin
                ? new Color(0.8f, 0.8f, 1f, thinkingAlpha)
                : new Color(0.3f, 0.3f, 0.5f, thinkingAlpha);

            GUIStyle thinkingStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = textColor },
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };

            string displayText = $"💭 {liveThinkingText}";
            if (thinkingTypingIndex < fullThinkingBuffer.Length)
            {
                displayText += "▌"; // Typing cursor
            }

            GUILayout.Label(displayText, thinkingStyle);

            EditorGUILayout.EndVertical();

            GUI.color = originalColor;
            GUI.backgroundColor = originalBgColor;
        }

        private void SetThinkingText(string text)
        {
            fullThinkingBuffer = text;
            thinkingTypingIndex = 0;
            thinkingFadeTimer = 0;
            thinkingAlpha = 0;
            shouldFadeImmediately = false;
        }

        private void ClearThinking()
        {
            shouldFadeImmediately = true;
        }
    }
}
