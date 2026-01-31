using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Input area UI for AI Chat Window
    /// </summary>
    public partial class AIChatWindow
    {
        private void DrawInputArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawFileContextHeader();

            if (showFileContext)
            {
                fileContextManager.DrawFileContext();
            }

            DrawDragDropArea();

            GUILayout.Label("Your Message:", EditorStyles.miniLabel);

            inputScrollPos = EditorGUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(80));
            userInput = EditorGUILayout.TextArea(userInput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(3);

            DrawQuickActionButtons();

            EditorGUILayout.Space(3);

            DrawSendButton();

            EditorGUILayout.EndVertical();
        }

        private void DrawFileContextHeader()
        {
            EditorGUILayout.BeginHorizontal();
            showFileContext = EditorGUILayout.Toggle("📎 Multi-File Context", showFileContext, GUILayout.Width(180));

            if (fileContextManager.FileCount > 0)
            {
                GUILayout.Label($"({fileContextManager.FileCount} files)", EditorStyles.miniLabel);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("➕ Add Selection", GUILayout.Width(100)))
            {
                int added = fileContextManager.AddSelection();
                if (added > 0)
                {
                    Debug.Log($"Added {added} file(s) to context");
                    showFileContext = true;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDragDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "📎 Drop files/objects here or type below", EditorStyles.helpBox);

            fileContextManager.HandleDragAndDrop(dropArea);
            HandleDragAndDrop(dropArea);
        }

        private void DrawQuickActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (agentMode)
            {
                DrawAgentQuickActions();
            }
            else
            {
                DrawChatQuickActions();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAgentQuickActions()
        {
            if (GUILayout.Button("👁️ Scene", GUILayout.Height(25)))
            {
                userInput = "Show me the current scene hierarchy";
            }

            if (GUILayout.Button("➕ Create", GUILayout.Height(25)))
            {
                userInput = "Create a new GameObject called ";
            }

            if (GUILayout.Button("📊 Stats", GUILayout.Height(25)))
            {
                userInput = "Show me project statistics";
            }

            if (GUILayout.Button("🔍 Find", GUILayout.Height(25)))
            {
                userInput = "Find all GameObjects with ";
            }
        }

        private void DrawChatQuickActions()
        {
            if (GUILayout.Button("💡 Explain", GUILayout.Height(25)))
            {
                userInput = "Explain this Unity code: ";
            }

            if (GUILayout.Button("🔧 Refactor", GUILayout.Height(25)))
            {
                userInput = "Refactor this code to improve: ";
            }

            if (GUILayout.Button("🐛 Debug", GUILayout.Height(25)))
            {
                userInput = "Help me debug this issue: ";
            }

            if (GUILayout.Button("📝 Generate", GUILayout.Height(25)))
            {
                userInput = "Generate a Unity script that: ";
            }
        }

        private void DrawSendButton()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !isProcessing && !string.IsNullOrWhiteSpace(userInput);

            if (GUILayout.Button(isProcessing ? "⏳ Processing..." : "📤 Send", GUILayout.Height(30)))
            {
                SendMessage();
            }

            // Enter key to send
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Return &&
                Event.current.control &&
                !isProcessing &&
                !string.IsNullOrWhiteSpace(userInput))
            {
                SendMessage();
                Event.current.Use();
            }

            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            GUILayout.Label("Tip: Press Ctrl+Enter to send", EditorStyles.miniLabel);
        }
    }
}
