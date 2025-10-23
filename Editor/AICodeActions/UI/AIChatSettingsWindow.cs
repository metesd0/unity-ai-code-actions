using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Settings window for AI Chat
    /// </summary>
    public class AIChatSettingsWindow : EditorWindow
    {
        private static AIChatWindow parentWindow;
        
        private int selectedProviderIndex = 0;
        private string[] providerNames = { "OpenAI", "Gemini", "Ollama (Local)", "OpenRouter" };
        private string apiKey = "";
        private string model = "";
        private string openRouterModel = "";
        private string endpoint = "";
        
        public static void ShowWindow(AIChatWindow parent)
        {
            parentWindow = parent;
            var window = GetWindow<AIChatSettingsWindow>("Chat Settings");
            window.minSize = new Vector2(450, 400);
            window.maxSize = new Vector2(450, 400);
            window.LoadCurrentSettings();
            window.Show();
        }
        
        private void LoadCurrentSettings()
        {
            selectedProviderIndex = EditorPrefs.GetInt("AICodeActions_Provider", 0);
            apiKey = EditorPrefs.GetString("AICodeActions_APIKey", "");
            model = EditorPrefs.GetString("AICodeActions_Model", "gpt-4");
            openRouterModel = EditorPrefs.GetString("AICodeActions_OpenRouterModel", "openai/gpt-3.5-turbo");
            endpoint = EditorPrefs.GetString("AICodeActions_Endpoint", "");
        }
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            GUILayout.Label("AI Chat Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Configure your AI provider for the chat window. Settings are shared with the main AI Code Actions window.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Provider selection
            EditorGUILayout.LabelField("Provider", EditorStyles.boldLabel);
            selectedProviderIndex = EditorGUILayout.Popup("Select Provider", selectedProviderIndex, providerNames);
            
            EditorGUILayout.Space(5);
            
            // API Key (for cloud providers)
            if (selectedProviderIndex == 0 || selectedProviderIndex == 1 || selectedProviderIndex == 3)
            {
                EditorGUILayout.LabelField("Authentication", EditorStyles.boldLabel);
                apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
                
                if (string.IsNullOrEmpty(apiKey))
                {
                    EditorGUILayout.HelpBox("⚠️ API key is required", MessageType.Warning);
                }
                
                // Quick links
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Get API Key:", GUILayout.Width(80));
                
                string linkText = selectedProviderIndex switch
                {
                    0 => "OpenAI",
                    1 => "Gemini",
                    3 => "OpenRouter",
                    _ => ""
                };
                
                string linkUrl = selectedProviderIndex switch
                {
                    0 => "https://platform.openai.com/api-keys",
                    1 => "https://makersuite.google.com/app/apikey",
                    3 => "https://openrouter.ai/keys",
                    _ => ""
                };
                
                if (GUILayout.Button(linkText, EditorStyles.linkLabel))
                {
                    Application.OpenURL(linkUrl);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
            }
            
            // Model configuration
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);
            
            if (selectedProviderIndex == 3) // OpenRouter
            {
                openRouterModel = EditorGUILayout.TextField("Model Name", openRouterModel);
                
                EditorGUILayout.HelpBox("💡 Enter full model name (e.g., openai/gpt-4, anthropic/claude-3.5-sonnet)\n\nSee all models at: openrouter.ai/models", MessageType.Info);
                
                // Quick model selection buttons
                EditorGUILayout.LabelField("Quick Select:", EditorStyles.miniLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("GPT-4o\n$$", GUILayout.Height(40)))
                    openRouterModel = "openai/gpt-4o";
                if (GUILayout.Button("GPT-4\n$$", GUILayout.Height(40)))
                    openRouterModel = "openai/gpt-4";
                if (GUILayout.Button("GPT-3.5\n$", GUILayout.Height(40)))
                    openRouterModel = "openai/gpt-3.5-turbo";
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Claude 3.5\n$$", GUILayout.Height(40)))
                    openRouterModel = "anthropic/claude-3.5-sonnet";
                if (GUILayout.Button("Claude 3\n$$", GUILayout.Height(40)))
                    openRouterModel = "anthropic/claude-3-opus";
                if (GUILayout.Button("Gemini Pro\nFREE", GUILayout.Height(40)))
                    openRouterModel = "google/gemini-pro";
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Llama 3 70B\nFREE", GUILayout.Height(40)))
                    openRouterModel = "meta-llama/llama-3-70b-instruct";
                if (GUILayout.Button("Llama 3 8B\nFREE", GUILayout.Height(40)))
                    openRouterModel = "meta-llama/llama-3-8b-instruct";
                if (GUILayout.Button("Mistral 7B\nFREE", GUILayout.Height(40)))
                    openRouterModel = "mistralai/mistral-7b-instruct";
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("🌐 Browse All Models on OpenRouter"))
                {
                    Application.OpenURL("https://openrouter.ai/models");
                }
            }
            else if (selectedProviderIndex == 2) // Ollama
            {
                model = EditorGUILayout.TextField("Model Name", model);
                endpoint = EditorGUILayout.TextField("Endpoint", endpoint);
                
                EditorGUILayout.HelpBox("💡 Make sure Ollama is running locally\nDefault: http://localhost:11434/api/generate", MessageType.Info);
                
                EditorGUILayout.LabelField("Popular Models:", EditorStyles.miniLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("mistral"))
                    model = "mistral";
                if (GUILayout.Button("llama2"))
                    model = "llama2";
                if (GUILayout.Button("codellama"))
                    model = "codellama";
                EditorGUILayout.EndHorizontal();
            }
            else // OpenAI or Gemini
            {
                model = EditorGUILayout.TextField("Model Name", model);
                
                if (selectedProviderIndex == 0) // OpenAI
                {
                    EditorGUILayout.LabelField("Popular Models:", EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("gpt-4"))
                        model = "gpt-4";
                    if (GUILayout.Button("gpt-3.5-turbo"))
                        model = "gpt-3.5-turbo";
                    EditorGUILayout.EndHorizontal();
                }
                else if (selectedProviderIndex == 1) // Gemini
                {
                    EditorGUILayout.LabelField("Popular Models:", EditorStyles.miniLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("gemini-pro"))
                        model = "gemini-pro";
                    if (GUILayout.Button("gemini-1.5-pro"))
                        model = "gemini-1.5-pro";
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("✓ Apply Settings", GUILayout.Height(30)))
            {
                ApplySettings();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Status
            string providerName = providerNames[selectedProviderIndex];
            string modelName = selectedProviderIndex == 3 ? openRouterModel : model;
            EditorGUILayout.HelpBox($"📊 Current: {providerName} - {modelName}", MessageType.None);
        }
        
        private void ApplySettings()
        {
            // Validate
            if ((selectedProviderIndex == 0 || selectedProviderIndex == 1 || selectedProviderIndex == 3) && string.IsNullOrEmpty(apiKey))
            {
                EditorUtility.DisplayDialog("Missing API Key", "Please enter your API key before applying settings.", "OK");
                return;
            }
            
            if (selectedProviderIndex == 3 && string.IsNullOrEmpty(openRouterModel))
            {
                EditorUtility.DisplayDialog("Missing Model", "Please enter a model name for OpenRouter.", "OK");
                return;
            }
            
            // Apply to parent window
            if (parentWindow != null)
            {
                parentWindow.ApplySettings(selectedProviderIndex, apiKey, model, openRouterModel);
            }
            
            EditorUtility.DisplayDialog("Settings Applied", 
                $"Provider: {providerNames[selectedProviderIndex]}\nModel: {(selectedProviderIndex == 3 ? openRouterModel : model)}\n\nSettings saved successfully!", 
                "OK");
            
            Close();
        }
    }
}

