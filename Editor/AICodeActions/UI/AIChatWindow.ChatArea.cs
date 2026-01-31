using UnityEditor;
using UnityEngine;
using AICodeActions.Core;
using AICodeActions.UI.ChatBubbles;

namespace AICodeActions.UI
{
    /// <summary>
    /// Chat area rendering for AI Chat Window
    /// Updated with modern chat bubbles support
    /// </summary>
    public partial class AIChatWindow
    {
        // Chat bubble renderer
        private ChatBubbleRenderer bubbleRenderer;
        private bool useModernBubbles = true;

        private void InitializeBubbleRenderer()
        {
            if (bubbleRenderer == null)
            {
                bubbleRenderer = new ChatBubbleRenderer();

                // Wire up callbacks
                bubbleRenderer.OnApplyCode = HandleApplyCode;
                bubbleRenderer.OnCopyCode = HandleCopyCode;
                bubbleRenderer.OnEditMessage = HandleEditMessage;
                bubbleRenderer.OnRedoMessage = HandleRedoMessage;
            }
        }

        private void DrawChatArea()
        {
            InitializeBubbleRenderer();

            // Check if we should use split view with preview panel
            if (showLivePreview && livePreviewPanel != null)
            {
                DrawSplitView();
            }
            else
            {
                DrawChatOnly();
            }
        }

        private void DrawChatOnly()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));

            float availableWidth = EditorGUIUtility.currentViewWidth - 40;

            foreach (var message in conversation.Messages)
            {
                if (useModernBubbles)
                {
                    bubbleRenderer.DrawMessage(message, availableWidth);
                    EditorGUILayout.Space(ChatBubbleStyles.Dimensions.MessageSpacing);
                }
                else
                {
                    DrawMessageLegacy(message);
                    EditorGUILayout.Space(5);
                }
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

        private void DrawSplitView()
        {
            EditorGUILayout.BeginHorizontal();

            // Left side: Chat area
            float totalWidth = EditorGUIUtility.currentViewWidth;
            float chatWidth = totalWidth * (1f - previewPanelWidthRatio) - splitterWidth;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(chatWidth));

            chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));

            float availableWidth = chatWidth - 20;

            foreach (var message in conversation.Messages)
            {
                if (useModernBubbles)
                {
                    bubbleRenderer.DrawMessage(message, availableWidth);
                    EditorGUILayout.Space(ChatBubbleStyles.Dimensions.MessageSpacing);
                }
                else
                {
                    DrawMessageLegacy(message);
                    EditorGUILayout.Space(5);
                }
            }

            if (showThinking && thinkingAlpha > 0 && !string.IsNullOrEmpty(liveThinkingText))
            {
                DrawThinkingFooterInline();
            }

            EditorGUILayout.EndScrollView();

            HandleAutoScroll();

            GUI.enabled = !autoScroll;
            if (GUILayout.Button("⬇ Scroll to Bottom", GUILayout.Height(20)))
            {
                autoScroll = true;
                chatScrollPos.y = Mathf.Infinity;
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();

            // Splitter
            DrawSplitter();

            // Right side: Preview panel
            float previewWidth = totalWidth * previewPanelWidthRatio;
            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, position.height - 100);

            if (livePreviewPanel != null)
            {
                livePreviewPanel.Draw(previewRect);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(splitterWidth, position.height - 100);

            // Draw splitter background
            EditorGUI.DrawRect(splitterRect, LivePreview.PreviewStyles.Colors.Splitter);

            // Handle hover
            bool isHovered = splitterRect.Contains(Event.current.mousePosition);
            if (isHovered)
            {
                EditorGUI.DrawRect(splitterRect, LivePreview.PreviewStyles.Colors.SplitterHover);
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            }

            // Handle drag
            if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
            {
                isDraggingSplitter = true;
                Event.current.Use();
            }

            if (isDraggingSplitter)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    float totalWidth = EditorGUIUtility.currentViewWidth;
                    float newRatio = 1f - (Event.current.mousePosition.x / totalWidth);
                    previewPanelWidthRatio = Mathf.Clamp(newRatio, 0.2f, 0.6f);
                    Repaint();
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    isDraggingSplitter = false;
                    Event.current.Use();
                }
            }
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

        // Legacy message drawing (fallback)
        private void DrawMessageLegacy(ChatMessage message)
        {
            GUIStyle style = GetMessageStyle(message.role);

            EditorGUILayout.BeginVertical(style);

            DrawMessageHeader(message);
            EditorGUILayout.Space(3);
            DrawMessageContent(message);

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

        // Action handlers for bubble renderer
        private void HandleApplyCode(string messageId, string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            // Apply code to scene using existing mechanism
            Debug.Log($"[AI Chat] Applying code from message {messageId}");
            ShowNotification(new GUIContent("✨ Code applied!"));

            // TODO: Integrate with actual code application logic
        }

        private void HandleCopyCode(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                GUIUtility.systemCopyBuffer = code;
                ShowNotification(new GUIContent("📋 Code copied to clipboard!"));
            }
        }

        private void HandleEditMessage(string messageId)
        {
            Debug.Log($"[AI Chat] Edit message: {messageId}");
            // TODO: Implement message editing
        }

        private void HandleRedoMessage(string messageId)
        {
            Debug.Log($"[AI Chat] Redo message: {messageId}");
            // TODO: Implement message regeneration
        }
    }
}
