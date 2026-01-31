using UnityEditor;
using UnityEngine;
using AICodeActions.Core;
using AICodeActions.UI.ChatBubbles;

namespace AICodeActions.UI
{
    /// <summary>
    /// Chat area rendering extensions for AI Chat Window
    /// Adds modern chat bubbles support to existing DrawChatArea
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

        // Note: DrawChatArea is defined in main AIChatWindow.cs
        // This partial class adds helper methods for modern bubbles

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
                if (useModernBubbles && bubbleRenderer != null)
                {
                    bubbleRenderer.DrawMessage(message, availableWidth);
                    EditorGUILayout.Space(ChatBubbleStyles.Dimensions.MessageSpacing);
                }
                else
                {
                    DrawMessage(message);
                    EditorGUILayout.Space(5);
                }
            }

            if (showThinking && thinkingAlpha > 0 && !string.IsNullOrEmpty(liveThinkingText))
            {
                DrawThinkingFooterInline();
            }

            EditorGUILayout.EndScrollView();

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
                    GUI.changed = true;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    isDraggingSplitter = false;
                    Event.current.Use();
                }
            }
        }

        // Action handlers for bubble renderer
        private void HandleApplyCode(string messageId, string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            Debug.Log($"[AI Chat] Applying code from message {messageId}");
            ShowNotification(new GUIContent("✨ Code applied!"));
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
        }

        private void HandleRedoMessage(string messageId)
        {
            Debug.Log($"[AI Chat] Redo message: {messageId}");
        }
    }
}
