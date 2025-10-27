using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AICodeActions.Actions;
using AICodeActions.Core;
using AICodeActions.Indexer;
using AICodeActions.Providers;
using AICodeActions.Utils;

namespace AICodeActions.UI
{
    /// <summary>
    /// Main Editor Window for AI Code Actions
    /// </summary>
    public class AICodeActionsWindow : EditorWindow
    {
        private const string PREFS_PROVIDER = "AICodeActions_Provider";
        private const string PREFS_API_KEY = "AICodeActions_APIKey";
        private const string PREFS_MODEL = "AICodeActions_Model";
        private const string PREFS_ENDPOINT = "AICodeActions_Endpoint";
        private const string PREFS_OPENROUTER_MODEL = "AICodeActions_OpenRouterModel";

        // State
        private int selectedProviderIndex = 0;
        private string[] providerNames = { "OpenAI", "Gemini", "Ollama (Local)", "OpenRouter" };
        private string apiKey = "";
        private string model = "";
        private string endpoint = "";
        private string openRouterModel = "openai/gpt-3.5-turbo";
        
        private int selectedActionIndex = 0;
        private string[] actionNames = { "Generate Script", "Explain Code", "Refactor Code" };
        
        private string specification = "";
        private string selectedCode = "";
        private string resultCode = "";
        private string explanation = "";
        private bool showDiff = false;
        private DiffViewer.DiffResult currentDiff;
        private DiffViewer diffViewer = new DiffViewer();
        
        private Vector2 scrollPos;
        private Vector2 resultScrollPos;
        private Vector2 diffScrollPos;
        
        private bool isProcessing = false;
        private string statusMessage = "Ready";
        
        private CodeIndexer indexer;
        private IModelProvider currentProvider;
        
        // Temperature and token settings
        private float temperature = 0.7f;
        private int maxTokens = 2048;

        [MenuItem("Window/AI Code Actions")]
        public static void ShowWindow()
        {
            var window = GetWindow<AICodeActionsWindow>("AI Code Actions");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPreferences();
            indexer = new CodeIndexer();
            
            // Index project on first load
            EditorApplication.delayCall += () =>
            {
                statusMessage = "Indexing project...";
                indexer.IndexProject();
                statusMessage = "Ready";
            };
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawProviderSettings();
            EditorGUILayout.Space(10);

            DrawActionSelection();
            EditorGUILayout.Space(10);

            DrawInputSection();
            EditorGUILayout.Space(10);

            DrawResultSection();
            
            EditorGUILayout.Space(10);
            DrawStatusBar();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            GUILayout.Label("AI Code Actions for Unity", EditorStyles.boldLabel);
            GUILayout.Label("Context-aware code generation with offline support", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reindex Project", GUILayout.Width(120)))
            {
                ReindexProject();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Scripts: {indexer?.GetContext()?.scripts.Count ?? 0}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawProviderSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Provider Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            selectedProviderIndex = EditorGUILayout.Popup("Provider", selectedProviderIndex, providerNames);
            if (EditorGUI.EndChangeCheck())
            {
                OnProviderChanged();
            }

            // Show API key field only for cloud providers
            if (selectedProviderIndex == 0 || selectedProviderIndex == 1 || selectedProviderIndex == 3) // OpenAI, Gemini, or OpenRouter
            {
                apiKey = EditorGUILayout.PasswordField("API Key", apiKey);
                if (string.IsNullOrEmpty(apiKey))
                {
                    EditorGUILayout.HelpBox("API key is required for this provider", MessageType.Warning);
                }
            }

            // Model field - different for OpenRouter
            if (selectedProviderIndex == 3) // OpenRouter
            {
                openRouterModel = EditorGUILayout.TextField("Model", openRouterModel);
                EditorGUILayout.HelpBox("Enter full model name (e.g., openai/gpt-4, anthropic/claude-2, meta-llama/llama-3-8b-instruct)\nSee all models at: https://openrouter.ai/models", MessageType.Info);
                
                // Quick model buttons
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("GPT-4o", GUILayout.Width(80)))
                    openRouterModel = "openai/gpt-4o";
                if (GUILayout.Button("GPT-4", GUILayout.Width(80)))
                    openRouterModel = "openai/gpt-4";
                if (GUILayout.Button("Claude-3.5", GUILayout.Width(80)))
                    openRouterModel = "anthropic/claude-3.5-sonnet";
                if (GUILayout.Button("Llama-3", GUILayout.Width(80)))
                    openRouterModel = "meta-llama/llama-3-70b-instruct";
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                model = EditorGUILayout.TextField("Model", model);
            }
            
            if (selectedProviderIndex == 2) // Ollama
            {
                endpoint = EditorGUILayout.TextField("Endpoint", endpoint);
                EditorGUILayout.HelpBox("Make sure Ollama is running locally (default: http://localhost:11434)", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            temperature = EditorGUILayout.Slider("Temperature", temperature, 0f, 2f);
            maxTokens = EditorGUILayout.IntSlider("Max Tokens", maxTokens, 256, 4096);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Save Settings"))
            {
                SavePreferences();
                UpdateProvider();
                EditorUtility.DisplayDialog("Settings Saved", "Provider settings have been saved", "OK");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionSelection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Action", EditorStyles.boldLabel);
            
            selectedActionIndex = GUILayout.SelectionGrid(selectedActionIndex, actionNames, 3);
            
            // Show action description
            string description = selectedActionIndex switch
            {
                0 => "Generate a new Unity C# script from text specification",
                1 => "Explain selected code with Unity-specific insights",
                2 => "Refactor selected code to improve quality and performance",
                _ => ""
            };
            EditorGUILayout.HelpBox(description, MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawInputSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Input", EditorStyles.boldLabel);

            // Different input based on action
            if (selectedActionIndex == 0) // Generate Script
            {
                GUILayout.Label("Specification:", EditorStyles.miniLabel);
                specification = EditorGUILayout.TextArea(specification, GUILayout.Height(100));
                
                if (GUILayout.Button("📋 Load from Clipboard", GUILayout.Height(25)))
                {
                    specification = GUIUtility.systemCopyBuffer;
                }
            }
            else // Explain or Refactor
            {
                GUILayout.Label("Selected Code:", EditorStyles.miniLabel);
                selectedCode = EditorGUILayout.TextArea(selectedCode, GUILayout.Height(150));
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📋 Paste from Clipboard", GUILayout.Height(25)))
                {
                    selectedCode = GUIUtility.systemCopyBuffer;
                }
                if (GUILayout.Button("📄 Load from Script", GUILayout.Height(25)))
                {
                    LoadFromScript();
                }
                EditorGUILayout.EndHorizontal();

                if (selectedActionIndex == 2) // Refactor
                {
                    GUILayout.Label("Refactor Goal (optional):", EditorStyles.miniLabel);
                    specification = EditorGUILayout.TextField(specification);
                }
            }

            EditorGUILayout.Space(5);
            
            // Execute button
            GUI.enabled = !isProcessing;
            if (GUILayout.Button(isProcessing ? "Processing..." : $"▶ Execute {actionNames[selectedActionIndex]}", GUILayout.Height(35)))
            {
                ExecuteAction();
            }
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        private void DrawResultSection()
        {
            if (string.IsNullOrEmpty(resultCode) && string.IsNullOrEmpty(explanation))
                return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Result", EditorStyles.boldLabel);

            // Show explanation if available (for Explain action)
            if (!string.IsNullOrEmpty(explanation))
            {
                resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(200));
                EditorGUILayout.TextArea(explanation, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("📋 Copy Explanation"))
                {
                    GUIUtility.systemCopyBuffer = explanation;
                    ShowNotification(new GUIContent("Copied to clipboard"));
                }
            }

            // Show generated code
            if (!string.IsNullOrEmpty(resultCode))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Toggle(showDiff, "Show Diff", "Button", GUILayout.Width(100)))
                {
                    if (!showDiff)
                    {
                        showDiff = true;
                        GenerateDiff();
                    }
                }
                else
                {
                    showDiff = false;
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                if (showDiff && currentDiff != null)
                {
                    DrawDiffView();
                }
                else
                {
                    resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos, GUILayout.Height(250));
                    EditorGUILayout.TextArea(resultCode, GUILayout.ExpandHeight(true));
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("📋 Copy to Clipboard"))
                {
                    GUIUtility.systemCopyBuffer = resultCode;
                    ShowNotification(new GUIContent("Copied to clipboard"));
                }
                
                if (GUILayout.Button("💾 Save as Script"))
                {
                    SaveAsScript();
                }
                
                if (!string.IsNullOrEmpty(selectedCode) && GUILayout.Button("✓ Apply Changes"))
                {
                    ApplyChanges();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDiffView()
        {
            if (currentDiff == null || currentDiff.Count == 0)
                return;

            diffScrollPos = EditorGUILayout.BeginScrollView(diffScrollPos, GUILayout.Height(250));
            
            var oldStyle = new GUIStyle(EditorStyles.label);
            oldStyle.richText = true;
            oldStyle.font = Font.CreateDynamicFontFromOSFont("Consolas", 11);

            foreach (var line in currentDiff.lines)
            {
                Color bgColor = line.type switch
                {
                    DiffViewer.DiffType.Added => new Color(0.2f, 0.8f, 0.2f, 0.2f),
                    DiffViewer.DiffType.Deleted => new Color(0.8f, 0.2f, 0.2f, 0.2f),
                    _ => Color.clear
                };

                if (bgColor != Color.clear)
                {
                    var rect = EditorGUILayout.GetControlRect(GUILayout.Height(15));
                    EditorGUI.DrawRect(rect, bgColor);
                }

                string prefix = line.type switch
                {
                    DiffViewer.DiffType.Added => "+ ",
                    DiffViewer.DiffType.Deleted => "- ",
                    _ => "  "
                };

                EditorGUILayout.LabelField($"{line.lineNumber,4} {prefix}{line.content}", oldStyle);
            }

            EditorGUILayout.EndScrollView();
            
            GUILayout.Label(currentDiff.GetSummary(), EditorStyles.miniLabel);
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(statusMessage, EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            
            if (currentProvider != null)
            {
                string status = currentProvider.IsConfigured ? "✓ Configured" : "⚠ Not Configured";
                GUILayout.Label($"{currentProvider.Name}: {status}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private async void ExecuteAction()
        {
            if (currentProvider == null || !currentProvider.IsConfigured)
            {
                EditorUtility.DisplayDialog("Provider Not Configured", 
                    "Please configure a provider and API key first", "OK");
                return;
            }

            isProcessing = true;
            statusMessage = $"Executing {actionNames[selectedActionIndex]}...";
            
            try
            {
                var input = new CodeActionInput
                {
                    specification = specification,
                    selectedCode = selectedCode,
                    parameters = new ModelParameters
                    {
                        temperature = temperature,
                        maxTokens = maxTokens,
                        model = string.IsNullOrEmpty(model) ? "default" : model
                    }
                };

                CodeActionBase action = selectedActionIndex switch
                {
                    0 => new GenerateScriptAction(currentProvider, indexer.GetContext()),
                    1 => new ExplainCodeAction(currentProvider, indexer.GetContext()),
                    2 => new RefactorMethodAction(currentProvider, indexer.GetContext()),
                    _ => null
                };

                if (action != null)
                {
                    var result = await action.ExecuteAsync(input);

                    if (result.success)
                    {
                        resultCode = result.generatedCode;
                        explanation = result.explanation;
                        statusMessage = "Success!";
                        
                        ShowNotification(new GUIContent("Action completed successfully"));
                    }
                    else
                    {
                        statusMessage = $"Error: {result.error}";
                        EditorUtility.DisplayDialog("Error", result.error, "OK");
                    }
                }
            }
            catch (Exception e)
            {
                statusMessage = $"Error: {e.Message}";
                Debug.LogError($"[AI Code Actions] {e}");
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
            finally
            {
                isProcessing = false;
                Repaint();
            }
        }

        private void SaveAsScript()
        {
            string path = EditorUtility.SaveFilePanel("Save Script", "Assets", "NewScript.cs", "cs");
            if (!string.IsNullOrEmpty(path))
            {
                // Ensure path is within Assets folder
                if (!path.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayDialog("Invalid Path", "Script must be saved within the Assets folder", "OK");
                    return;
                }

                File.WriteAllText(path, resultCode);
                AssetDatabase.Refresh();
                
                statusMessage = $"Script saved: {Path.GetFileName(path)}";
                ShowNotification(new GUIContent("Script saved successfully"));
            }
        }

        private void ApplyChanges()
        {
            if (string.IsNullOrEmpty(selectedCode) || string.IsNullOrEmpty(resultCode))
                return;

            // This would need file path tracking to properly apply changes
            // For now, just copy to clipboard with instruction
            GUIUtility.systemCopyBuffer = resultCode;
            EditorUtility.DisplayDialog("Apply Changes", 
                "Modified code has been copied to clipboard.\n\nPlease paste it into your script file manually.\n\n(Automatic file modification will be added in a future update)", 
                "OK");
        }

        private void LoadFromScript()
        {
            string path = EditorUtility.OpenFilePanel("Select C# Script", "Assets", "cs");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                selectedCode = File.ReadAllText(path);
                statusMessage = $"Loaded: {Path.GetFileName(path)}";
            }
        }

        private void GenerateDiff()
        {
            if (string.IsNullOrEmpty(selectedCode) || string.IsNullOrEmpty(resultCode))
                return;

            currentDiff = diffViewer.ComputeDiff(selectedCode, resultCode);
        }

        private void ReindexProject()
        {
            statusMessage = "Reindexing project...";
            indexer.IndexProject();
            statusMessage = "Reindexing complete";
            ShowNotification(new GUIContent("Project reindexed"));
        }

        private void UpdateProvider()
        {
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

            statusMessage = $"Provider set to: {currentProvider?.Name ?? "None"}";
        }
        
        private IModelProvider CreateOpenRouterProvider()
        {
            var provider = new OpenRouterProvider();
            var settings = new Dictionary<string, object>
            {
                { "modelName", openRouterModel }
            };
            provider.Configure(apiKey, settings);
            return provider;
        }

        private void OnProviderChanged()
        {
            // Set default values for new provider
            switch (selectedProviderIndex)
            {
                case 0: // OpenAI
                    model = "gpt-4";
                    endpoint = "";
                    break;
                case 1: // Gemini
                    model = "gemini-pro";
                    endpoint = "";
                    break;
                case 2: // Ollama
                    model = "mistral";
                    endpoint = "http://localhost:11434/api/generate";
                    break;
                case 3: // OpenRouter
                    openRouterModel = "openai/gpt-3.5-turbo";
                    endpoint = "";
                    break;
            }
            
            UpdateProvider();
        }

        private void LoadPreferences()
        {
            selectedProviderIndex = EditorPrefs.GetInt(PREFS_PROVIDER, 0);
            apiKey = EditorPrefs.GetString(PREFS_API_KEY, "");
            model = EditorPrefs.GetString(PREFS_MODEL, "gpt-4");
            endpoint = EditorPrefs.GetString(PREFS_ENDPOINT, "");
            openRouterModel = EditorPrefs.GetString(PREFS_OPENROUTER_MODEL, "openai/gpt-3.5-turbo");
            
            UpdateProvider();
        }

        private void SavePreferences()
        {
            EditorPrefs.SetInt(PREFS_PROVIDER, selectedProviderIndex);
            EditorPrefs.SetString(PREFS_API_KEY, apiKey);
            EditorPrefs.SetString(PREFS_MODEL, model);
            EditorPrefs.SetString(PREFS_ENDPOINT, endpoint);
            EditorPrefs.SetString(PREFS_OPENROUTER_MODEL, openRouterModel);
        }

        private void OnDisable()
        {
            // Auto-save preferences on close
            SavePreferences();
        }
    }
}

