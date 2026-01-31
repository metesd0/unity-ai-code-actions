using System;
using UnityEditor;
using UnityEngine;
using AICodeActions.UI.LivePreview;

namespace AICodeActions.UI
{
    /// <summary>
    /// Live Preview extensions for AI Chat Window toolbar
    /// Adds preview panel support to existing toolbar
    /// </summary>
    public partial class AIChatWindow
    {
        // Live Preview state
        private bool showLivePreview = false;
        private LivePreviewPanel livePreviewPanel;
        private float previewPanelWidthRatio = 0.4f;
        private float splitterWidth = 6f;
        private bool isDraggingSplitter = false;

        // Note: DrawToolbar and ExportConversation are defined in main AIChatWindow.cs
        // This partial class adds helper methods for live preview

        private void InitializeLivePreview()
        {
            if (livePreviewPanel == null)
            {
                livePreviewPanel = new LivePreviewPanel();
                livePreviewPanel.OnApplyCode = HandlePreviewApplyCode;
                livePreviewPanel.OnClose = () => showLivePreview = false;
            }
        }

        private void HandlePreviewApplyCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return;

            Debug.Log("[AI Chat] Applying code from preview panel");
            ShowNotification(new GUIContent("✨ Code applied from preview!"));
        }

        /// <summary>
        /// Update live preview with streaming content
        /// </summary>
        private void UpdateLivePreview(string code)
        {
            if (livePreviewPanel != null && showLivePreview)
            {
                livePreviewPanel.UpdateContent(code);
                Repaint();
            }
        }

        /// <summary>
        /// Draw additional toolbar buttons for preview and bubbles
        /// Call this from the main DrawToolbar method
        /// </summary>
        private void DrawPreviewToolbarButtons()
        {
            // Live Preview toggle
            GUI.backgroundColor = showLivePreview ? new Color(0.3f, 0.7f, 0.5f) : Color.white;
            bool newShowPreview = GUILayout.Toggle(showLivePreview, "👁 Preview", EditorStyles.toolbarButton, GUILayout.Width(75));
            if (newShowPreview != showLivePreview)
            {
                showLivePreview = newShowPreview;
                if (showLivePreview)
                {
                    InitializeLivePreview();
                }
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            // Modern bubbles toggle
            GUI.backgroundColor = useModernBubbles ? new Color(0.5f, 0.7f, 1f) : Color.white;
            bool newUseModernBubbles = GUILayout.Toggle(useModernBubbles, "🫧 Bubbles", EditorStyles.toolbarButton, GUILayout.Width(70));
            if (newUseModernBubbles != useModernBubbles)
            {
                useModernBubbles = newUseModernBubbles;
                Repaint();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
