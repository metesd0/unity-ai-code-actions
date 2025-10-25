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
                    systemPrompt = @"# Unity AI Assistant (Concise)

You are a helpful Unity editor agent. Prefer the minimum number of correct actions to satisfy the user request. Plan briefly, execute safely, verify results.

Principles:
- Use tools only when needed; avoid unnecessary chains. Prefer fewer, correct steps.
- After critical actions (create/attach/modify), verify and fix or report.
- Ask one short clarification only if ambiguity would change the outcome; otherwise choose sensible defaults.
- Scripts must compile: include usings, correct class/file names, no placeholders.
- **SCRIPT COMPILATION RULE**: After create_and_attach_script, NEVER call set_component_property on that script's component in the same tool chain. Scripts need compilation time. Inform user the script is ready and properties can be set after compilation if needed.
- Be safe: avoid destructive changes; save or modify assets only when necessary.

Response format:
- If tools are needed: output ordered [TOOL:...] blocks with required parameters.
- **CRITICAL**: After tool execution, ALWAYS analyze the results and provide a clear summary to the user:
  * For get_scene_info / get_project_stats: List key findings (object count, issues, stats)
  * For find_gameobjects: Show which objects were found and their properties
  * For read_script: Explain what the script does or note any issues
  * For creation/modification: Confirm what was created/changed and next steps
- If tools are not needed: answer directly.
- Use the user's language when possible.";
                    
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
                    maxTokens = agentMode ? 6144 : 2048 // Boosted for complex tasks
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
                            conversation.AddAssistantMessage(""); // Add empty message to update
                            Repaint();
                            
                            // ⚠️ CRITICAL: Setup callbacks BEFORE starting stream!
                            // All UI updates are queued to prevent "Hold on..." popup
                            streamManager.OnTextUpdate = (text) =>
                            {
                                streamingResponse.Append(text);
                                // Queue UI update instead of direct call (prevents blocking)
                                QueueUIUpdate(() =>
                                {
                                    conversation.UpdateLastAssistantMessage(streamingResponse.ToString());
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
                                });
                            };
                            
                            streamManager.OnError = (error) =>
                            {
                                Debug.LogError($"[AI Chat] Streaming error: {error}");
                                QueueUIUpdate(() =>
                                {
                                    response = $"⚠️ Streaming error: {error}";
                                    isStreamActive = false; // Stop update loop
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
                                Repaint();
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
                            Repaint();
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
                if (agentMode)
                {
                    // Add initial AI response (queue UI update)
                    QueueUIUpdate(() =>
                    {
                        conversation.AddAssistantMessage("🤖 Processing your request...\n");
                        Repaint();
                    });
                    
                    // CRITICAL: Tool execution MUST happen on main thread (Unity API requirement)
                    // Use TaskCompletionSource to run tool processing synchronously on main thread
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<string>();
                    
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            // Process tools with live progress updates (ON MAIN THREAD)
                            string processedResponse = agentTools.ProcessToolCallsWithProgress(response, (progress) =>
                            {
                                conversation.UpdateLastAssistantMessage(progress);
                                Repaint();
                                autoScroll = true;
                            }, currentDetailLevel.ToString());
                            
                            tcs.SetResult(processedResponse);
                        }
                        catch (System.Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    };
                    
                    // Wait for tool processing to complete on main thread
                    string finalResponse = await tcs.Task.ConfigureAwait(false);
                    
                    // Replace with final result (queue UI update)
                    QueueUIUpdate(() =>
                    {
                        conversation.UpdateLastAssistantMessage(finalResponse);
                    });
                }
                else
                {
                    QueueUIUpdate(() =>
                    {
                        conversation.AddAssistantMessage(response);
                    });
                }
                
                // Save conversation after each response
                SaveConversation();
                autoScroll = true; // Enable auto-scroll for new messages
                statusMessage = "Ready";
            }
            catch (System.OperationCanceledException)
            {
                isStreamActive = false; // Stop update loop
                statusMessage = "Request cancelled";
                conversation.AddSystemMessage("🛑 Request cancelled by user");
                SaveConversation();
                Debug.Log("[AI Chat] Request cancelled by user");
            }
            catch (Exception e)
            {
                isStreamActive = false; // Stop update loop
                statusMessage = $"Error: {e.Message}";
                conversation.AddSystemMessage($"❌ Error: {e.Message}");
                SaveConversation();
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