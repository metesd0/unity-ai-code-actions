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
            cancellationTokenSource = null;
            statusMessage = "Ready";
            
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
                    systemPrompt = @"# 🤖 UNITY AI AGENT - ADVANCED AUTONOMOUS SYSTEM

You are a HIGHLY CAPABLE Unity development agent with FULL scene manipulation powers.
Your PRIMARY DIRECTIVE: Execute tasks COMPLETELY, RELIABLY, and AUTONOMOUSLY in a SINGLE response.

═══════════════════════════════════════════════════════════════════

## 🎯 CORE OPERATING PRINCIPLES

### PRINCIPLE 1: COMPLETE EXECUTION
❌ WRONG: Use get_scene_info() → Wait for user → Do nothing
✅ CORRECT: Use get_scene_info() → Analyze → Execute ALL remaining steps → Report completion

### PRINCIPLE 2: MULTI-TOOL MASTERY
- You MUST use MULTIPLE tools PER response (typically 3-8 tools)
- ONE tool call is NEVER enough for complex requests
- Chain tools together to complete entire workflows

### PRINCIPLE 3: ZERO AMBIGUITY
- If user says ""create a first person controller"" → Create Player + Script + Components + Camera + Setup
- If user says ""make a cube"" → Create cube primitive at visible position
- NEVER ask ""what should I do?"" - YOU decide and DO IT!

═══════════════════════════════════════════════════════════════════

## 🛠️ TOOL USAGE MANDATES

### CRITICAL RULES:
1. **ALWAYS START WITH ACTION TOOLS** (create_, add_, set_) - NOT just get_scene_info!
2. **USE 3+ TOOLS MINIMUM** for any meaningful request
3. **CALL TOOLS IN SEQUENCE** within your response
4. **VERIFY each tool result** and proceed accordingly

### AVAILABLE TOOLS (40+ tools):
📦 GameObject: create_gameobject, create_primitive, find_gameobjects, delete_gameobject
🔧 Transform: set_position, set_rotation, set_scale, set_parent
⚙️ Components: add_component, attach_script, create_and_attach_script, set_component_property
💻 Scripts: modify_script, add_method_to_script, delete_script, validate_script, create_from_template
🎨 Visual: create_material, assign_material, create_light, create_camera
🎬 Scene: get_scene_info, save_scene, get_project_stats

═══════════════════════════════════════════════════════════════════

## 💪 EXECUTION PATTERNS (LEARN THESE!)

### PATTERN: First Person Controller
```
[TOOL:create_gameobject(name=""Player"")]
[TOOL:create_and_attach_script(gameobject_name=""Player"", script_name=""FirstPersonController"", script_content=""<full working C# code>"")]
[TOOL:add_component(gameobject_name=""Player"", component_type=""CharacterController"")]
[TOOL:create_camera(name=""PlayerCamera"", field_of_view=60)]
[TOOL:set_parent(child_name=""PlayerCamera"", parent_name=""Player"")]
[TOOL:set_position(gameobject_name=""PlayerCamera"", x=0, y=0.6, z=0)]
[TOOL:set_component_property(gameobject_name=""Player"", component_type=""CharacterController"", property_name=""height"", value=""2"")]
```
↑ THIS is how you execute - ALL steps in ONE response!

### PATTERN: Interactive Object
```
[TOOL:create_primitive(primitive_type=""Cube"", name=""InteractiveCube"", x=0, y=1, z=5)]
[TOOL:create_and_attach_script(...full script...)]
[TOOL:add_component(gameobject_name=""InteractiveCube"", component_type=""Rigidbody"")]
[TOOL:create_material(name=""CubeMaterial"", color=""blue"")]
[TOOL:assign_material(gameobject_name=""InteractiveCube"", material_name=""CubeMaterial"")]
```

### PATTERN: Scene Setup
```
[TOOL:create_light(name=""SunLight"", light_type=""Directional"", color=""white"", intensity=1)]
[TOOL:set_rotation(gameobject_name=""SunLight"", x=50, y=-30, z=0)]
[TOOL:create_primitive(primitive_type=""Plane"", name=""Ground"", x=0, y=0, z=0)]
[TOOL:set_scale(gameobject_name=""Ground"", x=20, y=1, z=20)]
```

═══════════════════════════════════════════════════════════════════

## ⚡ CRITICAL SUCCESS FACTORS

### ✅ DO:
- Execute FULL workflow in ONE response (all tools together)
- Write COMPLETE, WORKING C# scripts (not stubs!)
- Use create_and_attach_script (not just create then attach separately)
- Verify success and handle errors inline
- Report what you accomplished

### ❌ DON'T:
- Call ONLY get_scene_info and stop
- Use ONE tool when task needs FIVE tools
- Write incomplete scripts or placeholders
- Wait for user confirmation between steps
- Give up on first error

═══════════════════════════════════════════════════════════════════

## 🎓 QUALITY STANDARDS

### Scripts Must Be:
- ✅ Complete implementations (no TODOs or placeholders)
- ✅ Syntactically correct C# code
- ✅ Include all using directives
- ✅ Inherit from MonoBehaviour (unless ScriptableObject)
- ✅ Have meaningful variable names
- ✅ Include basic error handling

### Tool Chains Must:
- ✅ Execute in logical order
- ✅ Cover ALL aspects of user request
- ✅ Handle dependencies (create before attach, etc)
- ✅ Set reasonable default values

═══════════════════════════════════════════════════════════════════

## 🔥 RESPONSE FORMAT

Your response MUST follow this structure:

```
I'll create a complete [FEATURE] for you right now!

[Explain your plan briefly - 1-2 sentences]

[TOOL:tool_name(params)]
[TOOL:tool_name(params)]
[TOOL:tool_name(params)]
... (as many as needed to complete task)

Done! I've created:
✅ [List what you created]
✅ [List what you configured]
✅ [Any important notes]

Try it now in Unity!
```

═══════════════════════════════════════════════════════════════════

## 🚨 FINAL REMINDERS

1. **NO PARTIAL EXECUTION** - Finish the ENTIRE task!
2. **USE MULTIPLE TOOLS** - 3-8 tools per complex request is normal!
3. **BE PROACTIVE** - Don't ask, just do it right!
4. **HANDLE ERRORS** - If one approach fails, try another immediately!
5. **STAY FOCUSED** - Complete the user's request, not part of it!

NOW - Execute the user's request COMPLETELY and AUTONOMOUSLY! 🚀

═══════════════════════════════════════════════════════════════════";
                    
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
                        response = await currentProvider.GenerateAsync(fullPrompt, parameters);
                        
                        // Check if response is empty or too short
                        if (string.IsNullOrWhiteSpace(response) || response.Trim().Length < 5)
                        {
                            retryCount++;
                            if (retryCount < maxRetries)
                            {
                                statusMessage = $"⚠️ Empty response, retrying ({retryCount}/{maxRetries})...";
                                Repaint();
                                await System.Threading.Tasks.Task.Delay(1000); // Wait 1 second
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
                            await System.Threading.Tasks.Task.Delay(1000);
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
                    // Add initial AI response
                    conversation.AddAssistantMessage("🤖 Processing your request...\n");
                    Repaint(); // Show immediately
                    
                    // Process tools with live progress updates
                    string processedResponse = agentTools.ProcessToolCallsWithProgress(response, (progress) =>
                    {
                        // Update UI in real-time as each tool executes
                        conversation.UpdateLastAssistantMessage(progress);
                        Repaint();
                        autoScroll = true;
                    });
                    
                    // Replace with final result
                    conversation.UpdateLastAssistantMessage(processedResponse);
                    
                    // Check if response seems incomplete (auto-continue logic)
                    bool isIncomplete = IsResponseIncomplete(response, processedResponse);
                    
                    if (isIncomplete)
                    {
                        // Auto-continue: Send continuation message
                        statusMessage = "🔄 Response incomplete, auto-continuing...";
                        Repaint();
                        await System.Threading.Tasks.Task.Delay(500);
                        
                        // Add continuation prompt
                        conversation.AddSystemMessage("⚡ Auto-continuing: Please complete the remaining tasks (scripts, positioning, etc.)");
                        SaveConversation();
                        
                        // Recursively call with "continue" message
                        await ContinueIncompleteTask();
                    }
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
            catch (System.OperationCanceledException)
            {
                statusMessage = "Request cancelled";
                conversation.AddSystemMessage("🛑 Request cancelled by user");
                SaveConversation();
                Debug.Log("[AI Chat] Request cancelled by user");
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
        
        private bool IsResponseIncomplete(string rawResponse, string processedResponse)
        {
            // STRENGTHENED AUTO-CONTINUE DETECTION (solution.md requirements)
            // SMART MODE: Don't trigger if agent is asking questions or presenting options
            
            string lowerResponse = rawResponse.ToLower();
            
            // SMART CHECK: If agent is presenting options/asking questions, don't auto-continue
            if (rawResponse.TrimEnd().EndsWith("?") || 
                lowerResponse.Contains("seçenek") || 
                lowerResponse.Contains("options") ||
                lowerResponse.Contains("ne yapmak istersiniz") ||
                lowerResponse.Contains("what would you like") ||
                lowerResponse.Contains("nasıl yardımcı olabilirim") ||
                lowerResponse.Contains("how can i help"))
            {
                Debug.Log("[Auto-Continue] Agent is asking questions - no auto-continue needed");
                return false;
            }
            
            // 1. Response ends abruptly (no closing marks) - BUT not if it ends with "?"
            if (!rawResponse.TrimEnd().EndsWith("]") && 
                !rawResponse.TrimEnd().EndsWith("!") && 
                !rawResponse.TrimEnd().EndsWith(".") &&
                !rawResponse.TrimEnd().EndsWith("```") &&
                !rawResponse.TrimEnd().EndsWith("?"))
            {
                Debug.Log("[Auto-Continue] Response seems incomplete (no proper ending)");
                return true;
            }
            
            // 2. Contains [TOOL: but no matching [/TOOL]
            int toolOpenCount = 0;
            int toolCloseCount = 0;
            int index = 0;
            while ((index = rawResponse.IndexOf("[TOOL:", index)) != -1)
            {
                toolOpenCount++;
                index += 6;
            }
            index = 0;
            while ((index = rawResponse.IndexOf("[/TOOL]", index)) != -1)
            {
                toolCloseCount++;
                index += 7;
            }
            
            if (toolOpenCount > toolCloseCount)
            {
                Debug.Log($"[Auto-Continue] Incomplete: {toolOpenCount} [TOOL: but only {toolCloseCount} [/TOOL]");
                return true;
            }
            
            // 3. Response mentions "I'll create" but has < 3 tools
            if ((rawResponse.Contains("I'll create") || rawResponse.Contains("I will create") || 
                 rawResponse.Contains("I'll add") || rawResponse.Contains("I will add")) && toolOpenCount < 3)
            {
                Debug.Log($"[Auto-Continue] Promises creation but only {toolOpenCount} tools");
                return true;
            }
            
            // 4. Mentions script creation but no create_and_attach_script call
            if ((rawResponse.ToLower().Contains("script") && 
                (rawResponse.ToLower().Contains("controller") || rawResponse.ToLower().Contains("movement"))) &&
                !rawResponse.Contains("create_and_attach_script"))
            {
                Debug.Log("[Auto-Continue] Mentions script creation but missing create_and_attach_script tool");
                return true;
            }
            
            // 5. NEW: Plan says "create" but < 3 tools executed - ONLY if plan promises specific actions
            if (lowerResponse.Contains("plan:") && toolOpenCount < 3 && toolOpenCount > 0 &&
                (lowerResponse.Contains("i'll") || lowerResponse.Contains("i will") || 
                 lowerResponse.Contains("yapacağım") || lowerResponse.Contains("oluşturacağım")))
            {
                Debug.Log($"[Auto-Continue] Has plan with promises but only {toolOpenCount} tools");
                return true;
            }
            
            // 6. NEW: Created GameObject but no save_scene
            if ((rawResponse.Contains("create_gameobject") || rawResponse.Contains("create_primitive")) &&
                !rawResponse.Contains("save_scene"))
            {
                Debug.Log("[Auto-Continue] Created objects but missing save_scene");
                return true;
            }
            
            // 7. NEW: Added components/scripts but no positioning
            if (rawResponse.Contains("create_and_attach_script") && !rawResponse.Contains("set_position"))
            {
                Debug.Log("[Auto-Continue] Created script but no positioning");
                return true;
            }
            
            // 8. NEW: Check processed response for errors that need correction
            if (processedResponse.Contains("❌") || processedResponse.Contains("⚠️"))
            {
                Debug.Log("[Auto-Continue] Tool execution had errors/warnings - needs correction");
                return true;
            }
            
            // 9. NEW: Response has only 1 tool AND promises more (likely incomplete)
            // SMART: Don't trigger if 0 tools (agent just talking) or if no promises
            if (toolOpenCount == 1 && 
                (lowerResponse.Contains("i'll") || lowerResponse.Contains("i will") || 
                 lowerResponse.Contains("yapacağım") || lowerResponse.Contains("oluşturacağım") ||
                 lowerResponse.Contains("i'm creating") || lowerResponse.Contains("oluşturuyorum")))
            {
                Debug.Log($"[Auto-Continue] Only 1 tool but promises more - seems incomplete");
                return true;
            }
            
            return false;
        }
        
        private int continueTurnCount = 0;
        private const int maxContinueTurns = 2; // Max 2 auto-continue attempts
        
        private async System.Threading.Tasks.Task ContinueIncompleteTask()
        {
            continueTurnCount++;
            
            if (continueTurnCount > maxContinueTurns)
            {
                Debug.Log($"[AI Chat] Max continue turns reached ({maxContinueTurns})");
                conversation.AddSystemMessage($"⚠️ Task partially completed. Max auto-continue attempts ({maxContinueTurns}) reached.");
                SaveConversation();
                continueTurnCount = 0; // Reset for next task
                return;
            }
            
            Debug.Log($"[AI Chat] Auto-continue turn {continueTurnCount}/{maxContinueTurns}");
            
            // Build continuation prompt
            string contextPrompt = conversation.GetContextString();
            string toolsInfo = agentTools.GetToolsDescription();
            
            // SELF-CORRECTION: Check what failed/incomplete
            string lastMessage = conversation.Messages.Count > 0 
                ? conversation.Messages[conversation.Messages.Count - 1].content 
                : "";
            
            bool hasErrors = lastMessage.Contains("❌");
            bool hasWarnings = lastMessage.Contains("⚠️");
            
            string continuationPrompt = @"⚡ CONTINUATION REQUIRED - Complete the remaining tasks!

You started a task but didn't finish. Review what you've done and COMPLETE the remaining steps.";
            
            if (hasErrors || hasWarnings)
            {
                continuationPrompt += @"

🚨 CRITICAL: Your previous attempt had ERRORS/WARNINGS! 
- Review the last response carefully
- FIX any failed operations
- VERIFY the results with get_gameobject_info before proceeding
- If something failed, try a different approach";
            }
            
            continuationPrompt += @"

🎯 CRITICAL REMINDERS:
1. If you planned to create scripts - DO IT NOW with create_and_attach_script
2. If positioning is needed - DO IT NOW with set_position (y should be 0.5-3.0 for cameras)
3. If materials/lights/cameras are needed - CREATE THEM NOW
4. If you created GameObjects/Scripts - SAVE THE SCENE with save_scene
5. Use 3-8 tools to finish completely!
6. ALWAYS verify critical operations succeeded

VALIDATION REQUIREMENTS:
- Camera height: 0.5 to 3.0 (NOT 19 when you meant 1.9!)
- GameObject positions: typically -10 to 10
- Scale values: typically 0.1 to 10

Example completion:
[TOOL:create_and_attach_script]
gameobject_name: Player
script_name: FirstPersonController
script_content:
using UnityEngine;
public class FirstPersonController : MonoBehaviour
{
    void Update() { /* code */ }
}
[/TOOL]
[TOOL:set_position]
gameobject_name: PlayerCamera
x: 0
y: 1.7
z: 0
[/TOOL]
[TOOL:create_light]
name: MainLight
light_type: directional
intensity: 1.0
[/TOOL]
[TOOL:save_scene]
[/TOOL]

NOW - EXECUTE THE REMAINING TOOLS CORRECTLY!";
            
            string fullPrompt = $"{contextPrompt}\n\n{toolsInfo}\n\n{continuationPrompt}\n\nAssistant:";
            
            var parameters = new ModelParameters
            {
                temperature = 0.7f,
                maxTokens = 6144
            };
            
            try
            {
                string response = await currentProvider.GenerateAsync(fullPrompt, parameters);
                
                if (string.IsNullOrWhiteSpace(response))
                {
                    Debug.LogWarning("[AI Chat] Continue response was empty");
                    continueTurnCount = 0;
                    return;
                }
                
                // Process continuation response with live updates
                conversation.AddAssistantMessage($"🔄 Continuing (turn {continueTurnCount})...\n");
                Repaint();
                
                string processedResponse = agentTools.ProcessToolCallsWithProgress(response, (progress) =>
                {
                    conversation.UpdateLastAssistantMessage(progress);
                    Repaint();
                    autoScroll = true;
                });
                
                conversation.UpdateLastAssistantMessage(processedResponse);
                SaveConversation();
                
                // Check again if still incomplete (recursive)
                bool stillIncomplete = IsResponseIncomplete(response, processedResponse);
                if (stillIncomplete && continueTurnCount < maxContinueTurns)
                {
                    await System.Threading.Tasks.Task.Delay(500);
                    await ContinueIncompleteTask();
                }
                else
                {
                    // Done!
                    continueTurnCount = 0;
                    if (!stillIncomplete)
                    {
                        conversation.AddSystemMessage("✅ Task completed successfully!");
                        SaveConversation();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AI Chat] Continue error: {ex.Message}");
                conversation.AddSystemMessage($"❌ Auto-continue failed: {ex.Message}");
                SaveConversation();
                continueTurnCount = 0;
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
            // Cancel any ongoing requests
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
            
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

