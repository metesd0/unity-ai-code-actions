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
        private StreamManager streamManager; // NEW: Streaming support
        private bool isStreamActive = false; // Only update when actively streaming
        
        // Thread-safe UI update queue to prevent "Hold on..." popup
        private System.Collections.Generic.Queue<System.Action> uiUpdateQueue = new System.Collections.Generic.Queue<System.Action>();
        private object queueLock = new object();
        
        private int selectedProviderIndex = 0;
        private string[] providerNames = { "OpenAI", "Gemini", "Ollama (Local)", "OpenRouter" };
        private string apiKey = "";
        private string model = "";
        private string openRouterModel = "openai/gpt-3.5-turbo";
        
        private bool agentMode = true; // Enable agent capabilities by default
        
        // Detail level control (NEW: solution.md requirement)
        private enum DetailLevel { Compact, Normal, Detailed }
        private DetailLevel currentDetailLevel = DetailLevel.Compact;
        private bool showOnlyErrors = false;
        
        // ReAct Pattern control
        private bool showThinking = true; // Show AI reasoning by default
        
        // OpenRouter Reasoning control
        private enum ReasoningLevel { Off, Low, Medium, High }
        private ReasoningLevel currentReasoningLevel = ReasoningLevel.High; // Default to high for best results
        
        // Context tracking for better AI understanding
        private string lastCreatedScript = "";
        private string lastCreatedGameObject = "";
        private string lastModifiedGameObject = "";
        private System.Collections.Generic.List<string> recentScripts = new System.Collections.Generic.List<string>();
        private System.Collections.Generic.List<string> recentGameObjects = new System.Collections.Generic.List<string>();
        
        private string userInput = "";
        private Vector2 chatScrollPos;
        private Vector2 inputScrollPos;
        
        private bool isProcessing = false;
        private string statusMessage = "Ready";
        private bool autoScroll = true;
        private float lastScrollY = 0f;
        
        // Live Thinking Footer
        private string liveThinkingText = "";
        private float thinkingAlpha = 0f;
        private float thinkingFadeTimer = 0f;
        private int thinkingTypingIndex = 0;
        private string fullThinkingBuffer = "";
        private const float THINKING_FADE_DURATION = 5.0f; // Very slow, smooth fade out
        private const float THINKING_VISIBLE_TIME = 10.0f; // Thinking stays visible for 10 seconds
        private const float THINKING_TYPING_SPEED = 0.005f; // Fast typing: 200 chars/sec
        
        // Cancellation support
        private System.Threading.CancellationTokenSource cancellationTokenSource;
        
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
            // Reset state
            isProcessing = false;
            isStreamActive = false;
            cancellationTokenSource = null;
            statusMessage = "Ready";
            
            // Use same preferences as main window
            LoadPreferencesFromMainWindow();
            agentTools = new AgentToolSystem();
            
            // Initialize streaming manager
            streamManager = new StreamManager();
            EditorApplication.update += OnEditorUpdate; // Subscribe to Unity update loop
            
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
        
        private void OnDisable()
        {
            // Stop streaming update loop
            isStreamActive = false;
            
            // Unsubscribe from update loop
            EditorApplication.update -= OnEditorUpdate;
            
            // Cancel any ongoing streams
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            
            // Save conversation history
            SaveConversation();
        }
        
        /// <summary>
        /// Called every frame by Unity Editor
        /// Updates streaming buffer (only when actively streaming)
        /// </summary>
        private void OnEditorUpdate()
        {
            // Process UI updates from background threads (prevents "Hold on..." popup)
            ProcessUIUpdateQueue();
            
            if (isStreamActive && streamManager != null)
            {
                streamManager.Update();
            }
            
            // Animate thinking footer
            UpdateThinkingAnimation();
        }
        
        private void UpdateThinkingAnimation()
        {
            if (string.IsNullOrEmpty(fullThinkingBuffer))
            {
                // Fade out if no thinking text
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
                return;
            }
            
            // Typing effect - gradually reveal characters
            if (thinkingTypingIndex < fullThinkingBuffer.Length)
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
            else
            {
                // Fully visible - maintain for THINKING_VISIBLE_TIME
                thinkingAlpha = 1f;
                thinkingFadeTimer += Time.deltaTime;
                
                if (thinkingFadeTimer >= THINKING_VISIBLE_TIME)
                {
                    // Start fade out
                    fullThinkingBuffer = "";
                    thinkingFadeTimer = 0;
                }
            }
        }
        
        /// <summary>
        /// Process queued UI updates on the main thread
        /// This prevents "Hold on..." popup by moving UI work out of streaming callbacks
        /// </summary>
        private void ProcessUIUpdateQueue()
        {
            // Process max 10 updates per frame to avoid blocking
            int maxUpdatesPerFrame = 10;
            int processedCount = 0;
            
            while (processedCount < maxUpdatesPerFrame)
            {
                System.Action updateAction = null;
                
                lock (queueLock)
                {
                    if (uiUpdateQueue.Count > 0)
                    {
                        updateAction = uiUpdateQueue.Dequeue();
                    }
                    else
                    {
                        break; // Queue empty
                    }
                }
                
                if (updateAction != null)
                {
                    try
                    {
                        updateAction.Invoke(); // Execute on main thread
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AIChatWindow] UI update error: {ex.Message}");
                    }
                }
                
                processedCount++;
            }
        }
        
        /// <summary>
        /// Queue a UI update to be executed on the main thread
        /// Safe to call from any thread (streaming callbacks, etc.)
        /// </summary>
        private void QueueUIUpdate(System.Action action)
        {
            lock (queueLock)
            {
                uiUpdateQueue.Enqueue(action);
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
        
        private void DrawThinkingFooterInline()
        {
            // Save original GUI color
            Color originalColor = GUI.color;
            Color originalBgColor = GUI.backgroundColor;
            
            // Apply fade alpha
            GUI.color = new Color(1f, 1f, 1f, thinkingAlpha * 0.6f); // Soluk efekt
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.3f, thinkingAlpha * 0.5f);
            
            // Draw background box using GUILayout
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Apply alpha to text color
            Color textColor = EditorGUIUtility.isProSkin ? 
                new Color(0.8f, 0.8f, 1f, thinkingAlpha) : 
                new Color(0.3f, 0.3f, 0.5f, thinkingAlpha);
            
            GUIStyle thinkingStyle = new GUIStyle(EditorStyles.label);
            thinkingStyle.normal.textColor = textColor;
            thinkingStyle.fontSize = 11;
            thinkingStyle.fontStyle = FontStyle.Italic;
            thinkingStyle.wordWrap = true;
            thinkingStyle.padding = new RectOffset(10, 10, 8, 8);
            
            // Draw thinking text with icon
            string displayText = $"💭 {liveThinkingText}";
            if (thinkingTypingIndex < fullThinkingBuffer.Length)
            {
                displayText += "▌"; // Typing cursor
            }
            
            GUILayout.Label(displayText, thinkingStyle);
            
            EditorGUILayout.EndVertical();
            
            // Restore original colors
            GUI.color = originalColor;
            GUI.backgroundColor = originalBgColor;
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
            // Main toolbar
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
                    SaveConversation(); // Save immediately after clearing
                    autoScroll = true;
                }
            }
            
            // Cancel button (only show when processing)
            if (isProcessing)
            {
                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f); // Red background
                if (GUILayout.Button("🛑 Cancel", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    CancelCurrentRequest();
                }
                GUI.backgroundColor = Color.white;
            }
            
            EditorGUILayout.EndHorizontal();
            
            // NEW: Detail Level Control Bar (solution.md requirement)
            if (agentMode)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                
                GUILayout.Label("📊 View:", EditorStyles.miniLabel, GUILayout.Width(40));
                
                // Detail level buttons
                GUI.backgroundColor = currentDetailLevel == DetailLevel.Compact ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("Compact", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    currentDetailLevel = DetailLevel.Compact;
                }
                
                GUI.backgroundColor = currentDetailLevel == DetailLevel.Normal ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("Normal", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    currentDetailLevel = DetailLevel.Normal;
                }
                
                GUI.backgroundColor = currentDetailLevel == DetailLevel.Detailed ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("Detailed", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    currentDetailLevel = DetailLevel.Detailed;
                }
                GUI.backgroundColor = Color.white;
                
                GUILayout.Space(10);
                
                // Reasoning Level (OpenRouter reasoning tokens)
                GUILayout.Label("🧠", EditorStyles.miniLabel, GUILayout.Width(20));
                
                GUI.backgroundColor = currentReasoningLevel == ReasoningLevel.Off ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
                if (GUILayout.Button("Off", EditorStyles.toolbarButton, GUILayout.Width(35)))
                {
                    currentReasoningLevel = ReasoningLevel.Off;
                }
                
                GUI.backgroundColor = currentReasoningLevel == ReasoningLevel.Low ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("Low", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    currentReasoningLevel = ReasoningLevel.Low;
                }
                
                GUI.backgroundColor = currentReasoningLevel == ReasoningLevel.Medium ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("Med", EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    currentReasoningLevel = ReasoningLevel.Medium;
                }
                
                GUI.backgroundColor = currentReasoningLevel == ReasoningLevel.High ? new Color(0.3f, 0.7f, 1f) : Color.white;
                if (GUILayout.Button("High", EditorStyles.toolbarButton, GUILayout.Width(45)))
                {
                    currentReasoningLevel = ReasoningLevel.High;
                }
                GUI.backgroundColor = Color.white;
                
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
                int toolCount = conversation.Messages.Count > 0 
                    ? conversation.Messages[conversation.Messages.Count - 1].content.Split(new[] { "[TOOL:" }, System.StringSplitOptions.None).Length - 1 
                    : 0;
                if (toolCount > 0)
                {
                    GUILayout.Label($"🔧 {toolCount} tools", EditorStyles.miniLabel);
                }
                
                EditorGUILayout.EndHorizontal();
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
        
        private void DrawChatArea()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            chatScrollPos = EditorGUILayout.BeginScrollView(chatScrollPos, GUILayout.ExpandHeight(true));
            
            foreach (var message in conversation.Messages)
            {
                DrawMessage(message);
                EditorGUILayout.Space(5);
            }
            
            // Draw thinking footer right after last message (inside scroll view)
            if (showThinking && thinkingAlpha > 0 && !string.IsNullOrEmpty(liveThinkingText))
            {
                DrawThinkingFooterInline();
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
            DrawContentWithCodeBlocks(message.content);
        }
        
        private void DrawContentWithCodeBlocks(string content)
        {
            // Note: [THINKING] blocks removed - OpenRouter reasoning provides real model thinking
            // Only tool [TOOL:...] blocks are parsed by AgentToolSystem
            
            // Check if message contains code blocks
            var codeBlockMatches = Regex.Matches(content, @"```(?:csharp|c#)?\n([\s\S]*?)```");
            
            if (codeBlockMatches.Count > 0)
            {
                int lastIndex = 0;
                
                foreach (Match match in codeBlockMatches)
                {
                    // Draw text before code block
                    if (match.Index > lastIndex)
                    {
                        string textBefore = content.Substring(lastIndex, match.Index - lastIndex);
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
                if (lastIndex < content.Length)
                {
                    string textAfter = content.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(textAfter))
                    {
                        EditorGUILayout.LabelField(textAfter, EditorStyles.wordWrappedLabel);
                    }
                }
            }
            else
            {
                // No code blocks, just show text
                EditorGUILayout.LabelField(content, EditorStyles.wordWrappedLabel);
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
            
            // Create new cancellation token for this request
            cancellationTokenSource?.Cancel(); // Cancel any previous request
            cancellationTokenSource = new System.Threading.CancellationTokenSource();
            
            // Save scroll position to prevent jumping
            float savedScrollY = chatScrollPos.y;
            
            // Retry logic for reliability
            int maxRetries = 3;
            int retryCount = 0;
            string response = "";
            
            try
            {
                // Build prompt with conversation context and tools (if agent mode)
                string contextPrompt = conversation.GetContextString();
                string toolsInfo = agentMode ? agentTools.GetToolsDescription() : "";
                
                string systemPrompt = "You are an expert Unity AI assistant.";
                if (agentMode)
                {
                    systemPrompt = @"# Unity AI Assistant

You are an expert Unity editor agent. Execute user requests intelligently using available tools.

## 🎯 Core Principles

Write tools naturally to complete the user's request. The system will automatically handle execution and grouping for optimal user experience.

**Think Before Acting:**
- Analyze the user's request carefully
- If you need information, get it first (use get_scene_info, find_gameobjects)
- Then take action based on real data, not assumptions

**After Tool Execution:**
- I will show you the results
- Think about the results, then decide your next action
- Continue until the task is complete

**GameObject Names:**
- ❌ NEVER assume 'Main Camera', 'Directional Light', 'Cube', etc.
- ✅ ALWAYS check scene first with get_scene_info or find_gameobjects to discover actual names
- Example: User wants to modify light → First use find_gameobjects to find lights → Use the actual name from results

**Script Compilation:**
- ❌ NEVER call set_component_property immediately after create_and_attach_script
- ✅ Scripts need compilation time (few seconds) - inform user properties can be set after compilation

**Error Recovery:**
- If a tool fails, analyze why and try an alternative approach
- Don't repeat the same failing action
- If a GameObject is not found, search for it first

## 🔧 Tool Usage

Use [TOOL:tool_name] blocks with exact parameters:

```
[TOOL:tool_name]
parameter1: value1
parameter2: value2
[/TOOL]
```

## ✅ Response Style

- Be concise and action-oriented
- Use the user's language when possible
- Write tools naturally - don't explain strategy unless helpful
- The system will automatically handle execution and show you results";
                    
                    // Add context awareness
                    string contextSummary = agentTools.GetContextSummary();
                    if (!string.IsNullOrEmpty(contextSummary))
                    {
                        systemPrompt += "\n\n## 📝 RECENT CONTEXT:\n" + contextSummary;
                        systemPrompt += "\n\n💡 When user says 'this script', 'that object', 'the last one', etc., refer to recent context above.";
                    }
                    
                    systemPrompt += "\n\n## 🚀 NOW EXECUTE THE USER'S REQUEST FULLY AND COMPLETELY!";
                }
                
                string fullPrompt = $"{systemPrompt}\n\n{toolsInfo}\n\n{contextPrompt}\n\nUser: {message}\n\nAssistant:";
                
                var parameters = new ModelParameters
                {
                    temperature = 0.7f,
                    maxTokens = agentMode ? 6144 : 2048, // Boosted for complex tasks
                    
                    // OpenRouter Reasoning Tokens (for supported models)
                    // Note: OpenRouter allows ONLY ONE of effort or max_tokens, not both
                    reasoningEffort = (currentReasoningLevel == ReasoningLevel.Low || currentReasoningLevel == ReasoningLevel.Medium) ?
                                      (currentReasoningLevel == ReasoningLevel.Low ? "low" : "medium") : null,
                    reasoningMaxTokens = currentReasoningLevel == ReasoningLevel.High ? 2000 : (int?)null,
                    reasoningExclude = !showThinking // Hide reasoning if thinking toggle is off
                };
                
                // Retry loop for reliability
                while (retryCount < maxRetries)
                {
                    // Check if cancelled
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        throw new System.OperationCanceledException("Request cancelled by user");
                    }
                    
                    try
                    {
                        // 🚀 STREAMING MAGIC HAPPENS HERE!
                        if (currentProvider.SupportsStreaming && agentMode)
                        {
                            // STREAMING MODE - Real-time text updates! 🎉
                            var streamingResponse = new System.Text.StringBuilder();
                            var reasoningResponse = new System.Text.StringBuilder(); // For reasoning tokens
                            conversation.AddAssistantMessage(""); // Add empty message to update
                            // Note: Repaint() will be called in callbacks via QueueUIUpdate
                            
                            // ⚠️ CRITICAL: Setup callbacks BEFORE starting stream!
                            // All UI updates are queued to prevent "Hold on..." popup
                            
                            // Handle reasoning tokens (OpenRouter reasoning)
                            streamManager.OnReasoningUpdate = (reasoningText) =>
                            {
                                if (showThinking && !string.IsNullOrEmpty(reasoningText))
                                {
                                    reasoningResponse.Append(reasoningText);
                                    
                                    QueueUIUpdate(() =>
                                    {
                                        // Update live thinking footer (flowing text at bottom)
                                        string cleanedThinking = reasoningResponse.ToString()
                                            .Replace("\n", " ")
                                            .Replace("\r", "")
                                            .Trim();
                                        
                                        // Limit to last 200 chars for better readability
                                        if (cleanedThinking.Length > 200)
                                        {
                                            cleanedThinking = "..." + cleanedThinking.Substring(cleanedThinking.Length - 197);
                                        }
                                        
                                        // Update thinking footer
                                        fullThinkingBuffer = cleanedThinking;
                                        thinkingTypingIndex = 0;
                                        thinkingFadeTimer = 0;
                                        
                                        // Don't save reasoning to message - it's only in footer now
                                        // conversation.UpdateLastAssistantMessage(streamingResponse.ToString());
                                        Repaint();
                                    });
                                }
                            };
                            
                            streamManager.OnTextUpdate = (text) =>
                            {
                                streamingResponse.Append(text);
                                // Queue UI update instead of direct call (prevents blocking)
                                QueueUIUpdate(() =>
                                {
                                    string fullMessage = streamingResponse.ToString();
                                    
                                    // Prepend reasoning if available
                                    if (showThinking && reasoningResponse.Length > 0)
                                    {
                                        string formattedReasoning = $"💭 **Model Thinking:**\n```\n{reasoningResponse}\n```\n\n";
                                        fullMessage = formattedReasoning + fullMessage;
                                    }
                                    
                                    conversation.UpdateLastAssistantMessage(fullMessage);
                                    autoScroll = true;
                                    Repaint();
                                });
                            };
                            
                            streamManager.OnComplete = (finalText) =>
                            {
                                QueueUIUpdate(() =>
                                {
                                    response = finalText;
                                    isStreamActive = false; // Stop update loop
                                    
                                    // Clear thinking footer when response is complete
                                    fullThinkingBuffer = "";
                                    thinkingFadeTimer = 0;
                                });
                            };
                            
                            streamManager.OnError = (error) =>
                            {
                                Debug.LogError($"[AI Chat] Streaming error: {error}");
                                QueueUIUpdate(() =>
                                {
                                    response = $"⚠️ Streaming error: {error}";
                                    isStreamActive = false; // Stop update loop
                                    
                                    // Clear thinking footer on error
                                    fullThinkingBuffer = "";
                                    thinkingFadeTimer = 0;
                                });
                            };
                            
                            // Activate streaming update loop
                            isStreamActive = true;
                            
                            // Now start streaming with callbacks ready (stay on background thread)
                            await streamManager.StartStreamAsync(
                                async (onChunk, token) =>
                                {
                                    await currentProvider.StreamGenerateAsync(
                                        fullPrompt,
                                        parameters,
                                        onChunk,
                                        token
                                    ).ConfigureAwait(false);
                                },
                                cancellationTokenSource.Token
                            ).ConfigureAwait(false);
                            
                            // Wait a bit for callbacks to finish
                            await System.Threading.Tasks.Task.Delay(100).ConfigureAwait(false);
                            
                            if (string.IsNullOrEmpty(response))
                            {
                                response = streamingResponse.ToString();
                            }
                        }
                        else
                        {
                            // FALLBACK: Old non-streaming method
                            response = await currentProvider.GenerateAsync(fullPrompt, parameters).ConfigureAwait(false);
                        }
                        
                        // Check if response is empty or too short
                        if (string.IsNullOrWhiteSpace(response) || response.Trim().Length < 5)
                        {
                            retryCount++;
                            if (retryCount < maxRetries)
                            {
                                statusMessage = $"⚠️ Empty response, retrying ({retryCount}/{maxRetries})...";
                                // Note: Repaint on background thread causes errors, status will be visible on next UI update
                                await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(false); // Wait 1 second
                                continue;
                            }
                            else
                            {
                                response = "❌ AI returned empty response after 3 attempts. Please try again or check your API settings.";
                            }
                        }
                        
                        // Success - break retry loop
                        break;
                    }
                    catch (Exception retryEx)
                    {
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            statusMessage = $"⚠️ Error, retrying ({retryCount}/{maxRetries})...";
                            // Note: Repaint on background thread causes errors, status will be visible on next UI update
                            await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(false);
                            continue;
                        }
                        else
                        {
                            throw; // Re-throw if all retries failed
                        }
                    }
                }
                
                // Process tool calls if in agent mode with real-time feedback
                if (agentMode && HasToolCalls(response))
                {
                    // Check if we need to add a message (non-streaming) or update existing (streaming)
                    bool messageAlreadyAdded = currentProvider.SupportsStreaming; // Streaming already added a message
                    
                    QueueUIUpdate(() =>
                    {
                        if (messageAlreadyAdded)
                        {
                            // Update existing message from streaming
                            conversation.UpdateLastAssistantMessage(response + "\n\n🤖 Processing your request...\n");
                        }
                        else
                        {
                            // Add new message for non-streaming
                            conversation.AddAssistantMessage(response + "\n\n🤖 Processing your request...\n");
                        }
                        Repaint();
                    });
                    
                    // CRITICAL: Tool execution MUST happen on main thread (Unity API requirement)
                    // Use TaskCompletionSource to run tool processing synchronously on main thread
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
                    
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            // 🎯 Process tools IN GROUPS with sequential execution (ON MAIN THREAD)
                            var groupResults = agentTools.ProcessToolCallsInGroups(response, (groupProgress) =>
                            {
                                conversation.UpdateLastAssistantMessage(groupProgress);
                                Repaint();
                                autoScroll = true;
                            });
                            
                            // Combine all group results
                            string processedResponse = string.Join("\n", groupResults);
                            
                            tcs.SetResult(processedResponse);
                        }
                        catch (System.Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    };
                    
                    // Wait for tool processing to complete on main thread
                    string finalResponse = await tcs.Task.ConfigureAwait(false);
                    
                    // 🔄 ReAct Loop: Continue until task is complete
                    int reactLoopCount = 0;
                    const int maxReactLoops = 5; // Safety limit
                    string accumulatedResponse = response + "\n\n" + finalResponse;
                    
                    // Loop if tools were executed (finalResponse contains tool results)
                    bool toolsWereExecuted = !string.IsNullOrWhiteSpace(finalResponse) && finalResponse.Contains("Completed");
                    
                    Debug.Log($"[ReAct Loop] Initial check - toolsWereExecuted: {toolsWereExecuted}, finalResponse length: {finalResponse?.Length ?? 0}");
                    
                    while (reactLoopCount < maxReactLoops && toolsWereExecuted)
                    {
                        reactLoopCount++;
                        Debug.Log($"[ReAct Loop] Iteration {reactLoopCount} started");
                        
                        // Check if cancelled
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            break;
                        }
                        
                        // Inject tool results back to AI for next step
                        string toolResults = ExtractToolResults(finalResponse);
                        string observation = $"\n\nTool execution results:\n{toolResults}\n\nBased on these results, continue if needed or provide a final summary.";
                        
                        // Create continuation prompt
                        string continuationPrompt = $"{systemPrompt}\n\n{toolsInfo}\n\nPrevious conversation:\nUser: {message}\nAssistant: {accumulatedResponse}\n\n{observation}\n\nAssistant:";
                        
                        // Get AI's next step
                        string continuationResponse = "";
                        if (currentProvider.SupportsStreaming && agentMode)
                        {
                            var contStreamingResponse = new System.Text.StringBuilder();
                            
                            // Stream continuation
                            await streamManager.StartStreamAsync(
                                async (onChunk, token) =>
                                {
                                    await currentProvider.StreamGenerateAsync(
                                        continuationPrompt,
                                        parameters,
                                        onChunk,
                                        token
                                    ).ConfigureAwait(false);
                                },
                                cancellationTokenSource.Token
                            ).ConfigureAwait(false);
                            
                            await System.Threading.Tasks.Task.Delay(100).ConfigureAwait(false);
                            continuationResponse = streamManager.GetBufferedText();
                        }
                        else
                        {
                            continuationResponse = await currentProvider.GenerateAsync(continuationPrompt, parameters).ConfigureAwait(false);
                        }
                        
                        if (string.IsNullOrWhiteSpace(continuationResponse))
                        {
                            Debug.Log($"[ReAct Loop] Continuation response empty, breaking loop");
                            break; // No more continuation
                        }
                        
                        Debug.Log($"[ReAct Loop] Continuation response received ({continuationResponse.Length} chars), checking for tools...");
                        
                        // Accumulate the conversation
                        accumulatedResponse += "\n\n" + continuationResponse;
                        
                        // Update UI with accumulated response
                        QueueUIUpdate(() =>
                        {
                            conversation.UpdateLastAssistantMessage(accumulatedResponse);
                            Repaint();
                        });
                        
                        // Execute new tools if any
                        bool hasTools = HasToolCalls(continuationResponse);
                        Debug.Log($"[ReAct Loop] HasToolCalls: {hasTools}");
                        
                        if (hasTools)
                        {
                            Debug.Log($"[ReAct Loop] Executing new tools from continuation...");
                            var contTcs = new System.Threading.Tasks.TaskCompletionSource<string>();
                            
                            UnityEditor.EditorApplication.delayCall += () =>
                            {
                                try
                                {
                                    // 🎯 Process tools IN GROUPS with sequential execution (ON MAIN THREAD)
                                    var contGroupResults = agentTools.ProcessToolCallsInGroups(continuationResponse, (groupProgress) =>
                                    {
                                        conversation.UpdateLastAssistantMessage(accumulatedResponse + "\n\n" + groupProgress);
                                        Repaint();
                                        autoScroll = true;
                                    });
                                    
                                    // Combine all group results
                                    string contProcessed = string.Join("\n", contGroupResults);
                                    
                                    contTcs.SetResult(contProcessed);
                                }
                                catch (System.Exception ex)
                                {
                                    contTcs.SetException(ex);
                                }
                            };
                            
                            finalResponse = await contTcs.Task.ConfigureAwait(false);
                            accumulatedResponse += "\n\n" + finalResponse;
                        }
                        else
                        {
                            // No more tools, task is complete
                            Debug.Log($"[ReAct Loop] No more tools found, task complete");
                            break;
                        }
                    }
                    
                    Debug.Log($"[ReAct Loop] Exited after {reactLoopCount} iterations");
                    
                    // Replace with final accumulated result
                    QueueUIUpdate(() =>
                    {
                        conversation.UpdateLastAssistantMessage(accumulatedResponse);
                    });
                }
                else
                {
                    // No tools or not agent mode - add/update message accordingly
                    bool messageAlreadyAdded = currentProvider.SupportsStreaming; // Streaming already added a message
                    
                    QueueUIUpdate(() =>
                    {
                        if (!messageAlreadyAdded)
                        {
                            // Non-streaming: Add message
                            conversation.AddAssistantMessage(response);
                        }
                        // If streaming: Message already added and updated, nothing to do
                    });
                }
                
                // Save conversation after each response (queue to main thread)
                QueueUIUpdate(() => SaveConversation());
                autoScroll = true; // Enable auto-scroll for new messages
                statusMessage = "Ready";
            }
            catch (System.OperationCanceledException)
            {
                isStreamActive = false; // Stop update loop
                statusMessage = "Request cancelled";
                conversation.AddSystemMessage("🛑 Request cancelled by user");
                QueueUIUpdate(() => SaveConversation()); // Queue to main thread
                Debug.Log("[AI Chat] Request cancelled by user");
            }
            catch (Exception e)
            {
                isStreamActive = false; // Stop update loop
                statusMessage = $"Error: {e.Message}";
                conversation.AddSystemMessage($"❌ Error: {e.Message}");
                QueueUIUpdate(() => SaveConversation()); // Queue to main thread
                Debug.LogError($"[AI Chat] {e}");
            }
            finally
            {
                isProcessing = false;
                isStreamActive = false; // Ensure streaming is stopped
                cancellationTokenSource = null;
                
                // Force repaint after a short delay to avoid flashing
                EditorApplication.delayCall += () => 
                {
                    if (this != null)
                        Repaint();
                };
            }
        }
        
        /// <summary>
        /// Check if response contains tool calls
        /// </summary>
        private bool HasToolCalls(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return false;
            
            return response.Contains("[TOOL:") && response.Contains("[/TOOL]");
        }
        
        /// <summary>
        /// Extract tool execution results from processed response with detailed information
        /// </summary>
        private string ExtractToolResults(string processedResponse)
        {
            var results = new System.Text.StringBuilder();
            var lines = processedResponse.Split('\n');
            
            // Extract from detailed execution log (between <details> tags or after specific markers)
            bool inDetailedLog = false;
            var detailedResults = new System.Collections.Generic.List<string>();
            
            foreach (var line in lines)
            {
                // Start capturing when we see detailed log
                if (line.Contains("Show Detailed Execution Log") || line.Contains("**1.**"))
                {
                    inDetailedLog = true;
                }
                
                // Capture tool execution details
                if (inDetailedLog && line.StartsWith("**") && line.Contains(".**"))
                {
                    // Extract the actual result text
                    // Format: **1.** `tool_name` ✅ Actual result here (0.00s)
                    
                    // Extract tool name
                    int toolStart = line.IndexOf('`');
                    int toolEnd = line.IndexOf('`', toolStart + 1);
                    string toolName = toolStart >= 0 && toolEnd > toolStart ? 
                        line.Substring(toolStart + 1, toolEnd - toolStart - 1) : "";
                    
                    // Extract result (after ✅ or ❌)
                    int resultStart = line.IndexOf("✅");
                    if (resultStart == -1) resultStart = line.IndexOf("❌");
                    
                    if (resultStart >= 0 && resultStart + 2 < line.Length)
                    {
                        // Get text after emoji until timing info
                        int timeStart = line.LastIndexOf('(');
                        string result = timeStart > resultStart ? 
                            line.Substring(resultStart + 2, timeStart - resultStart - 2).Trim() :
                            line.Substring(resultStart + 2).Trim();
                        
                        // Format the result nicely
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            detailedResults.Add($"• {toolName}: {result}");
                        }
                    }
                }
                
                // End of detailed log
                if (line.Contains("</details>") || line.Contains("---"))
                {
                    inDetailedLog = false;
                }
            }
            
            // If we found detailed results, return them
            if (detailedResults.Count > 0)
            {
                results.AppendLine("Tool Execution Results:");
                foreach (var detail in detailedResults)
                {
                    results.AppendLine(detail);
                }
                return results.ToString().TrimEnd();
            }
            
            // Fallback: Look for summary lines with ✅ or ❌
            var summaryLines = new System.Collections.Generic.List<string>();
            foreach (var line in lines)
            {
                if ((line.Contains("✅") || line.Contains("❌")) && 
                    !line.Contains("**AI Agent**") && 
                    !line.Contains("Show Detailed"))
                {
                    summaryLines.Add(line.Trim());
                }
            }
            
            if (summaryLines.Count > 0)
            {
                return string.Join("\n", summaryLines);
            }
            
            // Last fallback
            return "Tools executed. Check previous response for details.";
        }
        
        private void CancelCurrentRequest()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                statusMessage = "Cancelling...";
                Debug.Log("[AI Chat] Cancelling current request");
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