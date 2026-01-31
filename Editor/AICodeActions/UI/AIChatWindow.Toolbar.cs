using System;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Toolbar and settings UI for AI Chat Window
    /// </summary>
    public partial class AIChatWindow
    {
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(agentMode ? "🤖 AI Agent" : "💬 AI Chat", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            // Agent mode toggle
            bool newAgentMode = GUILayout.Toggle(agentMode, "Agent Mode", EditorStyles.toolbarButton, GUILayout.Width(90));
            if (newAgentMode != agentMode)
            {
                agentMode = newAgentMode;
                string msg = agentMode
                    ? "🤖 Agent Mode ON - I can now interact with Unity!"
                    : "💬 Chat Mode - Agent tools disabled";
                conversation.AddSystemMessage(msg);
            }

            // ReAct thinking toggle (only visible in agent mode)
            if (agentMode)
            {
                showThinking = GUILayout.Toggle(showThinking, "💭 Thinking", EditorStyles.toolbarButton, GUILayout.Width(80));
            }

            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ShowSettings();
            }

            if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ExportConversation();
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Clear Chat", "Are you sure you want to clear the conversation?", "Yes", "No"))
                {
                    conversation.Clear();
                    conversation.AddSystemMessage("Conversation cleared. How can I help you?");
                    SaveConversation();
                    autoScroll = true;
                }
            }

            // Cancel button (only show when processing)
            if (isProcessing)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("🛑 Cancel", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    CancelCurrentRequest();
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            // Detail Level Control Bar
            if (agentMode)
            {
                DrawDetailLevelBar();
            }
        }

        private void DrawDetailLevelBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("📊 View:", EditorStyles.miniLabel, GUILayout.Width(40));

            // Detail level buttons
            DrawDetailLevelButton(DetailLevel.Compact, "Compact", 65);
            DrawDetailLevelButton(DetailLevel.Normal, "Normal", 60);
            DrawDetailLevelButton(DetailLevel.Detailed, "Detailed", 65);

            GUILayout.Space(10);

            // Reasoning Level
            GUILayout.Label("🧠", EditorStyles.miniLabel, GUILayout.Width(20));
            DrawReasoningLevelButton(ReasoningLevel.Off, "Off", 35);
            DrawReasoningLevelButton(ReasoningLevel.Low, "Low", 40);
            DrawReasoningLevelButton(ReasoningLevel.Medium, "Med", 40);
            DrawReasoningLevelButton(ReasoningLevel.High, "High", 45);

            GUILayout.Space(10);

            // Filter toggle
            GUI.backgroundColor = showOnlyErrors ? new Color(1f, 0.5f, 0.3f) : Color.white;
            bool newShowOnlyErrors = GUILayout.Toggle(showOnlyErrors, "❌ Errors Only", EditorStyles.toolbarButton, GUILayout.Width(90));
            if (newShowOnlyErrors != showOnlyErrors)
            {
                showOnlyErrors = newShowOnlyErrors;
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.FlexibleSpace();

            // Stats
            DrawToolStats();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDetailLevelButton(DetailLevel level, string label, int width)
        {
            GUI.backgroundColor = currentDetailLevel == level ? new Color(0.3f, 0.7f, 1f) : Color.white;
            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(width)))
            {
                currentDetailLevel = level;
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawReasoningLevelButton(ReasoningLevel level, string label, int width)
        {
            Color bgColor = level == ReasoningLevel.Off
                ? new Color(0.5f, 0.5f, 0.5f)
                : new Color(0.3f, 0.7f, 1f);
            GUI.backgroundColor = currentReasoningLevel == level ? bgColor : Color.white;
            if (GUILayout.Button(label, EditorStyles.toolbarButton, GUILayout.Width(width)))
            {
                currentReasoningLevel = level;
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawToolStats()
        {
            int toolCount = conversation.Messages.Count > 0
                ? conversation.Messages[conversation.Messages.Count - 1].content.Split(new[] { "[TOOL:" }, StringSplitOptions.None).Length - 1
                : 0;
            if (toolCount > 0)
            {
                GUILayout.Label($"🔧 {toolCount} tools", EditorStyles.miniLabel);
            }
        }

        private void ExportConversation()
        {
            try
            {
                string defaultName = $"Chat_{DateTime.Now:yyyyMMdd_HHmm}.md";
                string path = EditorUtility.SaveFilePanel("Export Chat", Application.dataPath, defaultName, "md");
                if (string.IsNullOrEmpty(path)) return;

                string md = conversation.ToMarkdown();
                System.IO.File.WriteAllText(path, md, System.Text.Encoding.UTF8);
                EditorUtility.RevealInFinder(path);
                EditorUtility.DisplayDialog("Exported", "Chat exported successfully.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI Chat] Export failed: {ex.Message}");
                EditorUtility.DisplayDialog("Export failed", ex.Message, "OK");
            }
        }
    }
}
