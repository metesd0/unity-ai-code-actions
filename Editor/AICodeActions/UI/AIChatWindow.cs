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
        // Chat window uses main window preferences (no separate keys needed)

        private ConversationManager conversation;
        private IModelProvider currentProvider;
        private AgentToolSystem agentTools;
        
        private int selectedProviderIndex = 0;
        private string[] providerNames = { "OpenAI", "Gemini", "Ollama (Local)", "OpenRouter" };
        private string apiKey = "";
        private string model = "";
        private string openRouterModel = "openai/gpt-3.5-turbo";
        
        private bool agentMode = true; // Enable agent capabilities by default
        
        private string userInput = "";
        private Vector2 chatScrollPos;
        private Vector2 inputScrollPos;
        
        private bool isProcessing = false;
        private string statusMessage = "Ready";
        private bool autoScroll = true;
        private float lastScrollY = 0f;
        
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
            // Use same preferences as main window
            LoadPreferencesFromMainWindow();
            agentTools = new AgentToolSystem();
            
            // Load conversation history
            LoadConversation();
            
            // Add welcome message only if conversation is empty
            if (conversation.Messages.Count == 0)
            {
                if (agentMode)
                {
                    conversation.AddSystemMessage("🤖 AI Agent Mode Enabled!\n\nI can see your Unity scene, create GameObjects, add components, and even create and attach scripts automatically.\n\nTry asking: 'Show me the current scene' or 'Create a player object with a controller script'");
                }
                else
                {
                    conversation.AddSystemMessage("Hello! I'm your Unity AI assistant. Ask me anything about Unity, C#, or request code modifications.");
                }
            }
        }
        
        private void LoadPreferencesFromMainWindow()
        {
            // Use the same preference keys as AICodeActionsWindow
            selectedProviderIndex = EditorPrefs.GetInt("AICodeActions_Provider", 0);
            apiKey = EditorPrefs.GetString("AICodeActions_APIKey", "");
            model = EditorPrefs.GetString("AICodeActions_Model", "");
            openRouterModel = EditorPrefs.GetString("AICodeActions_OpenRouterModel", "openai/gpt-3.5-turbo");
            string endpoint = EditorPrefs.GetString("AICodeActions_Endpoint", "");
            
            var config = new ProviderConfig
            {
                apiKey = apiKey,
                model = selectedProviderIndex == 3 ? openRouterModel : model,
                endpoint = endpoint
            };
            
            currentProvider = selectedProviderIndex switch
            {
                0 => new OpenAIProvider(config),
                1 => new GeminiProvider(config),
                2 => new OllamaProvider(config),
                3 => CreateOpenRouterProvider(),
                _ => null
            };
            
            Debug.Log($"[AI Chat] Loaded provider: {currentProvider?.Name} (IsConfigured: {currentProvider?.IsConfigured})");
        }
        
        private IModelProvider CreateOpenRouterProvider()
        {
            var provider = new OpenRouterProvider();
            var settings = new System.Collections.Generic.Dictionary<string, object>
            {
                { "modelName", openRouterModel }
            };
            provider.Configure(apiKey, settings);
            return provider;
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
                    SaveConversation(); // Save immediately after clearing
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
            
            EditorGUILayout.EndScrollView();
            
            // Auto-scroll logic
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
                
                // Auto-scroll if enabled
                if (autoScroll)
                {
                    chatScrollPos.y = Mathf.Infinity;
                }
                
                lastScrollY = chatScrollPos.y;
            }
            
            // Always show scroll to bottom button (enabled only when needed)
            GUI.enabled = !autoScroll;
            if (GUILayout.Button("⬇ Scroll to Bottom", GUILayout.Height(20)))
            {
                autoScroll = true;
                chatScrollPos.y = Mathf.Infinity;
            }
            GUI.enabled = true;
            
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
            
            // Copy response button for AI messages
            if (message.role == MessageRole.Assistant)
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
            
            // Drag & Drop area
            Rect dropArea = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "📎 Drop files/objects here or type below", EditorStyles.helpBox);
            
            HandleDragAndDrop(dropArea);
            
            GUILayout.Label("Your Message:", EditorStyles.miniLabel);
            
            inputScrollPos = EditorGUILayout.BeginScrollView(inputScrollPos, GUILayout.Height(80));
            userInput = EditorGUILayout.TextArea(userInput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(3);
            
            // Quick action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (agentMode)
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
            else
            {
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
            autoScroll = true; // Enable auto-scroll for new messages
            
            isProcessing = true;
            statusMessage = "AI is thinking...";
            
            // Save scroll position to prevent jumping
            float savedScrollY = chatScrollPos.y;
            
            try
            {
                // Build prompt with conversation context and tools (if agent mode)
                string contextPrompt = conversation.GetContextString();
                string toolsInfo = agentMode ? agentTools.GetToolsDescription() : "";
                
                string systemPrompt = "You are an expert Unity AI assistant.";
                if (agentMode)
                {
                    systemPrompt += " You have access to Unity tools that let you see and modify the scene. Use them when appropriate to help the user.";
                }
                
                string fullPrompt = $"{systemPrompt}\n\n{toolsInfo}\n\n{contextPrompt}\n\nUser: {message}\n\nAssistant:";
                
                var parameters = new ModelParameters
                {
                    temperature = 0.7f,
                    maxTokens = agentMode ? 3072 : 2048
                };
                
                string response = await currentProvider.GenerateAsync(fullPrompt, parameters);
                
                // Process tool calls if in agent mode
                if (agentMode)
                {
                    string processedResponse = agentTools.ProcessToolCalls(response);
                    conversation.AddAssistantMessage(processedResponse);
                }
                else
                {
                    conversation.AddAssistantMessage(response);
                }
                
                // Save conversation after each response
                SaveConversation();
                autoScroll = true; // Enable auto-scroll for new messages
                statusMessage = "Ready";
            }
            catch (Exception e)
            {
                statusMessage = $"Error: {e.Message}";
                conversation.AddSystemMessage($"❌ Error: {e.Message}");
                SaveConversation();
                Debug.LogError($"[AI Chat] {e}");
            }
            finally
            {
                isProcessing = false;
                
                // Force repaint after a short delay to avoid flashing
                EditorApplication.delayCall += () => 
                {
                    if (this != null)
                        Repaint();
                };
            }
        }
        
        private void ShowSettings()
        {
            AIChatSettingsWindow.ShowWindow(this);
        }
        
        public void ApplySettings(int providerIndex, string newApiKey, string newModel, string newOpenRouterModel)
        {
            selectedProviderIndex = providerIndex;
            apiKey = newApiKey;
            model = newModel;
            openRouterModel = newOpenRouterModel;
            
            // Save to EditorPrefs so main window also uses these
            EditorPrefs.SetInt("AICodeActions_Provider", selectedProviderIndex);
            EditorPrefs.SetString("AICodeActions_APIKey", apiKey);
            EditorPrefs.SetString("AICodeActions_Model", model);
            EditorPrefs.SetString("AICodeActions_OpenRouterModel", openRouterModel);
            
            // Recreate provider with new settings
            LoadPreferencesFromMainWindow();
            
            conversation.AddSystemMessage($"✅ Settings updated! Provider: {currentProvider?.Name}, Model: {(selectedProviderIndex == 3 ? openRouterModel : model)}");
            statusMessage = "Settings applied";
            Repaint();
        }
        
        private void OnDisable()
        {
            // Save conversation history
            SaveConversation();
        }
        
        private void SaveConversation()
        {
            try
            {
                string json = JsonUtility.ToJson(conversation, false);
                EditorPrefs.SetString("AICodeActions_ChatHistory", json);
                Debug.Log("[AI Chat] Conversation saved");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AI Chat] Could not save conversation: {e.Message}");
            }
        }
        
        private void LoadConversation()
        {
            try
            {
                string json = EditorPrefs.GetString("AICodeActions_ChatHistory", "");
                
                if (!string.IsNullOrEmpty(json))
                {
                    conversation = JsonUtility.FromJson<ConversationManager>(json);
                    if (conversation == null)
                    {
                        conversation = new ConversationManager();
                    }
                    Debug.Log($"[AI Chat] Loaded conversation with {conversation.Messages.Count} messages");
                }
                else
                {
                    conversation = new ConversationManager();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AI Chat] Could not load conversation: {e.Message}");
                conversation = new ConversationManager();
            }
        }
        
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                    
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                        {
                            ProcessDraggedObject(draggedObject);
                        }
                        
                        // Also handle file paths
                        foreach (string path in DragAndDrop.paths)
                        {
                            ProcessDraggedPath(path);
                        }
                    }
                    
                    Event.current.Use();
                    break;
            }
        }
        
        private void ProcessDraggedObject(UnityEngine.Object obj)
        {
            if (obj == null) return;
            
            // MonoScript (C# files)
            if (obj is MonoScript script)
            {
                string scriptPath = AssetDatabase.GetAssetPath(script);
                
                // Only add reference, let AI read it with read_script tool
                userInput += $"\n📎 Script: `{script.name}.cs` (Path: {scriptPath})\n";
                conversation.AddSystemMessage($"📎 Attached script reference: {script.name}.cs - AI can read it with 'read_script' tool");
                Repaint();
                return;
            }
            
            // GameObject (from Hierarchy)
            if (obj is GameObject go)
            {
                // Only add reference, let AI read it with get_gameobject_info tool
                userInput += $"\n🎮 GameObject: `{go.name}` (in Hierarchy)\n";
                conversation.AddSystemMessage($"📎 Attached GameObject reference: {go.name} - AI can inspect it with 'get_gameobject_info' tool");
                Repaint();
                return;
            }
            
            // Prefab
            if (PrefabUtility.IsPartOfPrefabAsset(obj))
            {
                string prefabPath = AssetDatabase.GetAssetPath(obj);
                userInput += $"\n📦 Prefab: `{obj.name}` (Path: {prefabPath})\n";
                conversation.AddSystemMessage($"📎 Attached Prefab reference: {obj.name}");
                Repaint();
                return;
            }
            
            // Scene Asset
            if (obj is SceneAsset)
            {
                string scenePath = AssetDatabase.GetAssetPath(obj);
                userInput += $"\n\n**Scene:** {obj.name}\n**Path:** {scenePath}\n";
                conversation.AddSystemMessage($"📎 Attached Scene: {obj.name}");
                Repaint();
                return;
            }
            
            // TextAsset (txt, json, xml, etc.)
            if (obj is TextAsset textAsset)
            {
                string textPath = AssetDatabase.GetAssetPath(textAsset);
                userInput += $"\n📄 File: `{textAsset.name}` (Path: {textPath})\n";
                conversation.AddSystemMessage($"📎 Attached file reference: {textAsset.name} - AI can read it with 'read_file' tool");
                Repaint();
                return;
            }
            
            // Default: just add the name and type
            userInput += $"\n\n**Asset:** {obj.name} (Type: {obj.GetType().Name})\n";
            conversation.AddSystemMessage($"📎 Attached: {obj.name}");
            Repaint();
        }
        
        private void ProcessDraggedPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            // Check if it's a .cs file
            if (path.EndsWith(".cs") && System.IO.File.Exists(path))
            {
                string fileName = System.IO.Path.GetFileName(path);
                string scriptName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                userInput += $"\n📎 Script: `{fileName}` (Path: {path})\n";
                conversation.AddSystemMessage($"📎 Attached script reference: {fileName} - AI can read it with 'read_script' tool");
                Repaint();
            }
            else if (System.IO.File.Exists(path))
            {
                string fileName = System.IO.Path.GetFileName(path);
                userInput += $"\n📄 File: `{fileName}` (Path: {path})\n";
                conversation.AddSystemMessage($"📎 Attached file reference: {fileName} - AI can read it with 'read_file' tool");
                Repaint();
            }
        }
    }
}

