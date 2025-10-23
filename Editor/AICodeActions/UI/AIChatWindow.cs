using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using AICodeActions.Core;
using AICodeActions.Providers;

namespace AICodeActions.UI
{
    /// <summary>
    /// Chat-style AI interaction window
    /// </summary>
    public class AIChatWindow : EditorWindow
    {
        private const string PREFS_CHAT_PROVIDER = "AICodeActions_ChatProvider";
        private const string PREFS_CHAT_API_KEY = "AICodeActions_ChatAPIKey";
        private const string PREFS_CHAT_MODEL = "AICodeActions_ChatModel";

        private ConversationManager conversation;
        private IModelProvider currentProvider;
        
        private int selectedProviderIndex = 0;
        private string[] providerNames = { "OpenAI", "Gemini", "Ollama (Local)" };
        private string apiKey = "";
        private string model = "";
        
        private string userInput = "";
        private Vector2 chatScrollPos;
        private Vector2 inputScrollPos;
        
        private bool isProcessing = false;
        private string statusMessage = "Ready";
        
        private GUIStyle userMessageStyle;
        private GUIStyle assistantMessageStyle;
        private GUIStyle systemMessageStyle;
        private GUIStyle codeBlockStyle;
        
        [MenuItem("Window/AI Chat")]
        public static void ShowWindow()
        {
            var window = GetWindow<AIChatWindow>("AI Chat");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadPreferences();
            conversation = new ConversationManager();
            
            // Add welcome message
            conversation.AddSystemMessage("Hello! I'm your Unity AI assistant. Ask me anything about Unity, C#, or request code modifications.");
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            DrawToolbar();
            EditorGUILayout.Space(5);
            
            DrawChatArea();
            EditorGUILayout.Space(5);
            
            DrawInputArea();
            EditorGUILayout.Space(5);
            
            DrawStatusBar();
        }
        
        private void InitializeStyles()
        {
            if (userMessageStyle == null)
            {
                userMessageStyle = new GUIStyle(EditorStyles.helpBox);
                userMessageStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.8f, 0.2f));
                userMessageStyle.padding = new RectOffset(10, 10, 10, 10);
                userMessageStyle.wordWrap = true;
                userMessageStyle.richText = true;
            }
            
            if (assistantMessageStyle == null)
            {
                assistantMessageStyle = new GUIStyle(EditorStyles.helpBox);
                assistantMessageStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                assistantMessageStyle.padding = new RectOffset(10, 10, 10, 10);
                assistantMessageStyle.wordWrap = true;
                assistantMessageStyle.richText = true;
            }
            
            if (systemMessageStyle == null)
            {
                systemMessageStyle = new GUIStyle(EditorStyles.helpBox);
                systemMessageStyle.normal.background = MakeTex(2, 2, new Color(0.5f, 0.5f, 0.2f, 0.2f));
                systemMessageStyle.padding = new RectOffset(10, 10, 10, 10);
                systemMessageStyle.wordWrap = true;
                systemMessageStyle.alignment = TextAnchor.MiddleCenter;
                systemMessageStyle.fontStyle = FontStyle.Italic;
            }
            
            if (codeBlockStyle == null)
            {
                codeBlockStyle = new GUIStyle(EditorStyles.textArea);
                codeBlockStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 11);
                codeBlockStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
                codeBlockStyle.padding = new RectOffset(10, 10, 10, 10);
            }
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
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("AI Chat", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Settings", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                ShowSettings();
            }
            
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("Clear Chat", "Are you sure you want to clear the conversation?", "Yes", "No"))
                {
                    conversation.Clear();
                    conversation.AddSystemMessage("Conversation cleared. How can I help you?");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawChatArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));
            
            foreach (var message in conversation.Messages)
            {
                DrawMessage(message);
                EditorGUILayout.Space(5);
            }
            
            // Auto-scroll to bottom
            if (Event.current.type == EventType.Repaint)
            {
                chatScrollPos.y = Mathf.Infinity;
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMessage(ChatMessage message)
        {
            GUIStyle style = message.role switch
            {
                MessageRole.User => userMessageStyle,
                MessageRole.Assistant => assistantMessageStyle,
                MessageRole.System => systemMessageStyle,
                _ => EditorStyles.label
            };
            
            EditorGUILayout.BeginVertical(style);
            
            // Header
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
            
            EditorGUILayout.Space(3);
            
            // Content with code block detection
            DrawMessageContent(message);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawMessageContent(ChatMessage message)
        {
            // Check if message contains code blocks
            var codeBlockMatches = Regex.Matches(message.content, @"```(?:csharp|c#)?\n([\s\S]*?)```");
            
            if (codeBlockMatches.Count > 0)
            {
                int lastIndex = 0;
                
                foreach (Match match in codeBlockMatches)
                {
                    // Draw text before code block
                    if (match.Index > lastIndex)
                    {
                        string textBefore = message.content.Substring(lastIndex, match.Index - lastIndex);
                        if (!string.IsNullOrWhiteSpace(textBefore))
                        {
                            EditorGUILayout.LabelField(textBefore, EditorStyles.wordWrappedLabel);
                        }
                    }
                    
                    // Draw code block
                    string code = match.Groups[1].Value.Trim();
                    EditorGUILayout.LabelField(code, codeBlockStyle);
                    
                    // Copy button
                    if (GUILayout.Button("📋 Copy Code", GUILayout.Height(20)))
                    {
                        GUIUtility.systemCopyBuffer = code;
                        ShowNotification(new GUIContent("Code copied to clipboard!"));
                    }
                    
                    lastIndex = match.Index + match.Length;
                }
                
                // Draw remaining text
                if (lastIndex < message.content.Length)
                {
                    string textAfter = message.content.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(textAfter))
                    {
                        EditorGUILayout.LabelField(textAfter, EditorStyles.wordWrappedLabel);
                    }
                }
            }
            else
            {
                // No code blocks, just show text
                EditorGUILayout.LabelField(message.content, EditorStyles.wordWrappedLabel);
            }
        }
        
        private void DrawInputArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("Your Message:", EditorStyles.miniLabel);
            
            inputScrollPos = EditorGUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(80));
            userInput = EditorGUILayout.TextArea(userInput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(3);
            
            // Quick action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("💡 Explain", GUILayout.Height(25)))
            {
                userInput = "Explain this Unity code: ";
            }
            
            if (GUILayout.Button("🔧 Refactor", GUILayout.Height(25)))
            {
                userInput = "Refactor this code to improve performance: ";
            }
            
            if (GUILayout.Button("🐛 Debug", GUILayout.Height(25)))
            {
                userInput = "What's wrong with this code: ";
            }
            
            if (GUILayout.Button("✨ Optimize", GUILayout.Height(25)))
            {
                userInput = "Optimize this Unity code: ";
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            
            // Send button
            GUI.enabled = !isProcessing && !string.IsNullOrWhiteSpace(userInput);
            if (GUILayout.Button(isProcessing ? "⏳ Processing..." : "📤 Send", GUILayout.Height(30)))
            {
                SendMessage();
            }
            GUI.enabled = true;
            
            // Enter key support
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return && Event.current.control)
            {
                if (!isProcessing && !string.IsNullOrWhiteSpace(userInput))
                {
                    SendMessage();
                    Event.current.Use();
                }
            }
            
            GUILayout.Label("Press Ctrl+Enter to send", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(statusMessage, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            if (currentProvider != null)
            {
                string status = currentProvider.IsConfigured ? "✓ Connected" : "⚠ Not Configured";
                GUILayout.Label($"{currentProvider.Name}: {status}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private async void SendMessage()
        {
            if (currentProvider == null || !currentProvider.IsConfigured)
            {
                EditorUtility.DisplayDialog("Provider Not Configured", 
                    "Please configure a provider first (Settings button)", "OK");
                return;
            }
            
            string message = userInput.Trim();
            if (string.IsNullOrEmpty(message))
                return;
            
            // Add user message
            conversation.AddUserMessage(message);
            userInput = "";
            
            isProcessing = true;
            statusMessage = "AI is thinking...";
            Repaint();
            
            try
            {
                // Build prompt with conversation context
                string contextPrompt = conversation.GetContextString();
                string fullPrompt = $"{contextPrompt}\n\nUser: {message}\n\nAssistant:";
                
                var parameters = new ModelParameters
                {
                    temperature = 0.7f,
                    maxTokens = 2048
                };
                
                string response = await currentProvider.GenerateAsync(fullPrompt, parameters);
                
                // Add assistant response
                conversation.AddAssistantMessage(response);
                statusMessage = "Ready";
            }
            catch (Exception e)
            {
                statusMessage = $"Error: {e.Message}";
                conversation.AddSystemMessage($"❌ Error: {e.Message}");
                Debug.LogError($"[AI Chat] {e}");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }
        
        private void ShowSettings()
        {
            // Simple settings dialog (you can expand this to a proper window)
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Provider/OpenAI"), selectedProviderIndex == 0, () => selectedProviderIndex = 0);
            menu.AddItem(new GUIContent("Provider/Gemini"), selectedProviderIndex == 1, () => selectedProviderIndex = 1);
            menu.AddItem(new GUIContent("Provider/Ollama (Local)"), selectedProviderIndex == 2, () => selectedProviderIndex = 2);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Configure API Key"), false, ShowAPIKeyDialog);
            
            menu.ShowAsContext();
        }
        
        private void ShowAPIKeyDialog()
        {
            // This would open a proper dialog - for now just log
            Debug.Log("Use the main AI Code Actions window to configure API keys");
            EditorUtility.DisplayDialog("Configure Provider", 
                "Please use Window > AI Code Actions to configure your provider and API keys.", "OK");
        }
        
        private void UpdateProvider()
        {
            var config = new ProviderConfig
            {
                apiKey = apiKey,
                model = model
            };
            
            currentProvider = selectedProviderIndex switch
            {
                0 => new OpenAIProvider(config),
                1 => new GeminiProvider(config),
                2 => new OllamaProvider(config),
                _ => null
            };
        }
        
        private void LoadPreferences()
        {
            selectedProviderIndex = EditorPrefs.GetInt(PREFS_CHAT_PROVIDER, 0);
            apiKey = EditorPrefs.GetString(PREFS_CHAT_API_KEY, "");
            model = EditorPrefs.GetString(PREFS_CHAT_MODEL, "gpt-4");
            
            UpdateProvider();
        }
        
        private void SavePreferences()
        {
            EditorPrefs.SetInt(PREFS_CHAT_PROVIDER, selectedProviderIndex);
            EditorPrefs.SetString(PREFS_CHAT_API_KEY, apiKey);
            EditorPrefs.SetString(PREFS_CHAT_MODEL, model);
        }
        
        private void OnDisable()
        {
            SavePreferences();
        }
    }
}

