using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Chat area rendering for AI Chat Window
    /// </summary>
    public partial class AIChatWindow
    {
        private void DrawChatArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));

            foreach (var message in conversation.Messages)
            {
                DrawMessage(message);
                EditorGUILayout.Space(5);
            }

            // Draw thinking footer right after last message
            if (showThinking && thinkingAlpha > 0 && !string.IsNullOrEmpty(liveThinkingText))
            {
                DrawThinkingFooterInline();
            }

            EditorGUILayout.EndScrollView();

            HandleAutoScroll();

            // Scroll to bottom button
            GUI.enabled = !autoScroll;
            if (GUILayout.Button("⬇ Scroll to Bottom", GUILayout.Height(20)))
            {
                autoScroll = true;
                chatScrollPos.y = Mathf.Infinity;
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void HandleAutoScroll()
        {
            if (Event.current.type == EventType.Repaint)
            {
                // Detect if user scrolled up manually
                if (Mathf.Abs(chatScrollPos.y - lastScrollY) > 10f)
                {
                    if (chatScrollPos.y < lastScrollY)
                    {
                        autoScroll = false;
                    }
                }

                if (autoScroll)
                {
                    chatScrollPos.y = Mathf.Infinity;
                }

                lastScrollY = chatScrollPos.y;
            }
        }

        private void DrawMessage(ChatMessage message)
        {
            GUIStyle style = GetMessageStyle(message.role);

            EditorGUILayout.BeginVertical(style);

            DrawMessageHeader(message);
            EditorGUILayout.Space(3);
            DrawMessageContent(message);

            // Copy response button for AI messages
            if (message.role == MessageRole.Assistant)
            {
                DrawCopyButton(message);
            }

            EditorGUILayout.EndVertical();
        }

        private GUIStyle GetMessageStyle(MessageRole role)
        {
            return role switch
            {
                MessageRole.User => userMessageStyle,
                MessageRole.Assistant => assistantMessageStyle,
                MessageRole.System => systemMessageStyle,
                _ => EditorStyles.label
            };
        }

        private void DrawMessageHeader(ChatMessage message)
        {
            EditorGUILayout.BeginHorizontal();

            string roleLabel = message.role switch
            {
                MessageRole.User => "👤 You",
                MessageRole.Assistant => "🤖 AI Assistant",
                MessageRole.System => "ℹ️ System",
                _ => "Unknown"
            };

            GUILayout.Label(roleLabel, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label(message.timestamp.ToString("HH:mm"), EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCopyButton(ChatMessage message)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("📋 Copy Response", GUILayout.Height(22), GUILayout.Width(130)))
            {
                GUIUtility.systemCopyBuffer = message.content;
                ShowNotification(new GUIContent("✅ Response copied to clipboard!"));
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
