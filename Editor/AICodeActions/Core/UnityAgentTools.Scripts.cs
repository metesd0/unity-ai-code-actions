using System;
using System.Linq;
using UnityEditor;
using AICodeActions.Core;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Advanced Script Manipulation: Modify, analyze, validate, templates
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Modify an existing script by adding/replacing code
        /// </summary>
        public static string ModifyScript(string scriptName, string modifications)
        {
            try
            {
                // Find the script file
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                string newContent = currentContent + "\n" + modifications;

                // Roslyn validation (best-effort)
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Changes not applied.\n{roslynReport}";
                }
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ModifyScript] Modified {scriptName}.cs");
                
                return $"✅ Modified {scriptName}.cs\nℹ️ {roslynReport}\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error modifying script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add a method to an existing script
        /// </summary>
        public static string AddMethodToScript(string scriptName, string methodCode)
        {
            try
            {
                // Find the script file
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                
                // Find the last closing brace of the class
                int lastBrace = currentContent.LastIndexOf('}');
                if (lastBrace == -1)
                    return $"❌ Invalid script structure - no closing brace found";
                
                // Clean method code
                methodCode = methodCode.Trim();
                if (methodCode.StartsWith("```csharp") || methodCode.StartsWith("```c#"))
                {
                    int firstNewline = methodCode.IndexOf('\n');
                    if (firstNewline > 0)
                        methodCode = methodCode.Substring(firstNewline + 1);
                }
                if (methodCode.EndsWith("```"))
                {
                    int lastBacktick = methodCode.LastIndexOf("```");
                    methodCode = methodCode.Substring(0, lastBacktick);
                }
                methodCode = methodCode.Trim();
                
                // Insert method before the last closing brace
                string indent = "    "; // 4 spaces
                string formattedMethod = "\n" + indent + methodCode.Replace("\n", "\n" + indent) + "\n";
                string newContent = currentContent.Insert(lastBrace, formattedMethod);

                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Method not inserted.\n{roslynReport}";
                }
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddMethodToScript] Added method to {scriptName}.cs");
                
                return $"✅ Added method to {scriptName}.cs\nℹ️ {roslynReport}\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error adding method: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add a field/property to an existing script
        /// </summary>
        public static string AddFieldToScript(string scriptName, string fieldCode)
        {
            try
            {
                // Find the script file
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                // Read current content
                string currentContent = System.IO.File.ReadAllText(scriptPath);
                
                // Find first opening brace (class start)
                int firstBrace = currentContent.IndexOf('{');
                if (firstBrace == -1)
                    return $"❌ Invalid script structure - no opening brace found";
                
                // Clean field code
                fieldCode = fieldCode.Trim();
                
                // Insert field after the first opening brace
                string indent = "    "; // 4 spaces
                string formattedField = "\n" + indent + fieldCode + "\n";
                string newContent = currentContent.Insert(firstBrace + 1, formattedField);

                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Field not added.\n{roslynReport}";
                }
                
                // Write modified content
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddFieldToScript] Added field to {scriptName}.cs");
                
                return $"✅ Added field to {scriptName}.cs\nℹ️ {roslynReport}\n💡 Script will recompile automatically";
            }
            catch (Exception e)
            {
                return $"❌ Error adding field: {e.Message}";
            }
        }
        
        /// <summary>
        /// Delete a script file
        /// </summary>
        public static string DeleteScript(string scriptName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                AssetDatabase.DeleteAsset(scriptPath);
                AssetDatabase.Refresh();
                
                Debug.Log($"[DeleteScript] Deleted {scriptName}.cs");
                
                return $"✅ Deleted {scriptName}.cs ({scriptPath})";
            }
            catch (Exception e)
            {
                return $"❌ Error deleting script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find text in a script
        /// </summary>
        public static string FindInScript(string scriptName, string searchText)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                var lines = content.Split('\n');
                
                var results = new System.Text.StringBuilder();
                results.AppendLine($"🔍 Search results for '{searchText}' in {scriptName}.cs:");
                results.AppendLine();
                
                int matchCount = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains(searchText))
                    {
                        matchCount++;
                        results.AppendLine($"Line {i + 1}: {lines[i].Trim()}");
                    }
                }
                
                if (matchCount == 0)
                {
                    results.AppendLine($"❌ No matches found for '{searchText}'");
                }
                else
                {
                    results.AppendLine();
                    results.AppendLine($"✅ Found {matchCount} match(es)");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error searching script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Replace text in a script
        /// </summary>
        public static string ReplaceInScript(string scriptName, string findText, string replaceText)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                if (!content.Contains(findText))
                    return $"❌ Text '{findText}' not found in {scriptName}.cs";
                
                int occurrences = content.Split(new[] { findText }, StringSplitOptions.None).Length - 1;
                string newContent = content.Replace(findText, replaceText);

                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Replace not applied.\n{roslynReport}";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ReplaceInScript] Replaced {occurrences} occurrence(s) in {scriptName}.cs");
                
                return $"✅ Replaced {occurrences} occurrence(s) of '{findText}' with '{replaceText}' in {scriptName}.cs\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error replacing text: {e.Message}";
            }
        }
        
        /// <summary>
        /// Validate script for syntax errors (basic check)
        /// </summary>
        public static string ValidateScript(string scriptName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                var results = new System.Text.StringBuilder();
                results.AppendLine($"🔍 Validation results for {scriptName}.cs:");
                results.AppendLine();
                
                int issueCount = 0;
                
                // Basic syntax checks
                int openBraces = 0, closeBraces = 0;
                foreach (char c in content)
                {
                    if (c == '{') openBraces++;
                    if (c == '}') closeBraces++;
                }
                
                if (openBraces != closeBraces)
                {
                    issueCount++;
                    results.AppendLine($"⚠️ Brace mismatch: {openBraces} open, {closeBraces} close");
                }
                
                // Check for common issues
                if (!content.Contains("using UnityEngine"))
                {
                    issueCount++;
                    results.AppendLine("⚠️ Missing 'using UnityEngine;'");
                }
                
                if (!content.Contains($"class {scriptName}"))
                {
                    issueCount++;
                    results.AppendLine($"⚠️ Class name doesn't match file name '{scriptName}'");
                }
                
                if (issueCount == 0)
                {
                    results.AppendLine("✅ No obvious syntax issues found");
                    results.AppendLine("💡 For detailed analysis, check Unity Console after compilation");
                }
                else
                {
                    results.AppendLine();
                    results.AppendLine($"❌ Found {issueCount} potential issue(s)");
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error validating script: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create script from template
        /// </summary>
        public static string CreateFromTemplate(string scriptName, string templateType, string gameObjectName = null)
        {
            try
            {
                string templateContent = "";
                
                switch (templateType.ToLower())
                {
                    case "singleton":
                        templateContent = $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    private static {scriptName} _instance;
    public static {scriptName} Instance
    {{
        get
        {{
            if (_instance == null)
            {{
                _instance = FindObjectOfType<{scriptName}>();
                if (_instance == null)
                {{
                    GameObject go = new GameObject(""{scriptName}"");
                    _instance = go.AddComponent<{scriptName}>();
                }}
            }}
            return _instance;
        }}
    }}

    void Awake()
    {{
        if (_instance != null && _instance != this)
        {{
            Destroy(gameObject);
            return;
        }}
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }}
}}";
                        break;
                        
                    case "statemachine":
                        templateContent = $@"using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    public enum State {{ Idle, Moving, Attacking, Dead }}
    
    private State currentState = State.Idle;
    
    void Update()
    {{
        switch (currentState)
        {{
            case State.Idle:
                HandleIdleState();
                break;
            case State.Moving:
                HandleMovingState();
                break;
            case State.Attacking:
                HandleAttackingState();
                break;
            case State.Dead:
                HandleDeadState();
                break;
        }}
    }}
    
    public void ChangeState(State newState)
    {{
        currentState = newState;
        Debug.Log(""State changed to: "" + newState);
    }}
    
    void HandleIdleState() {{ }}
    void HandleMovingState() {{ }}
    void HandleAttackingState() {{ }}
    void HandleDeadState() {{ }}
}}";
                        break;
                        
                    case "objectpool":
                        templateContent = $@"using System.Collections.Generic;
using UnityEngine;

public class {scriptName} : MonoBehaviour
{{
    [SerializeField] private GameObject prefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    void Start()
    {{
        for (int i = 0; i < poolSize; i++)
        {{
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }}
    }}
    
    public GameObject GetFromPool()
    {{
        if (pool.Count > 0)
        {{
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }}
        else
        {{
            GameObject obj = Instantiate(prefab);
            return obj;
        }}
    }}
    
    public void ReturnToPool(GameObject obj)
    {{
        obj.SetActive(false);
        pool.Enqueue(obj);
    }}
}}";
                        break;
                        
                    case "scriptableobject":
                        templateContent = $@"using UnityEngine;

[CreateAssetMenu(fileName = ""{scriptName}"", menuName = ""ScriptableObjects/{scriptName}"", order = 1)]
public class {scriptName} : ScriptableObject
{{
    public string displayName;
    public string description;
    
    // Add your custom properties here
}}";
                        break;
                        
                    default:
                        return $"❌ Unknown template type '{templateType}'. Available: singleton, statemachine, objectpool, scriptableobject";
                }
                
                // Create the script
                string path = $"Assets/{scriptName}.cs";
                System.IO.File.WriteAllText(path, templateContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[CreateFromTemplate] Created {scriptName}.cs from {templateType} template");
                
                // Attach to GameObject if specified
                if (!string.IsNullOrEmpty(gameObjectName) && templateType.ToLower() != "scriptableobject")
                {
                    return CreateAndAttachScript(gameObjectName, scriptName, templateContent);
                }
                
                return $"✅ Created {scriptName}.cs from {templateType} template at {path}";
            }
            catch (Exception e)
            {
                return $"❌ Error creating from template: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add comments/documentation to script
        /// </summary>
        public static string AddCommentsToScript(string scriptName, string comments)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Add header comment
                string dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string headerComment = $@"/*
 * {scriptName}
 * {comments}
 * Generated: {dateTime}
 */

";
                string newContent = headerComment + content;
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddCommentsToScript] Added comments to {scriptName}.cs");
                
                return $"✅ Added comments to {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error adding comments: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create multiple scripts at once
        /// </summary>
        public static string CreateMultipleScripts(string scriptNames, string baseNamespace = null)
        {
            try
            {
                var names = scriptNames.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var results = new System.Text.StringBuilder();
                results.AppendLine($"📝 Creating {names.Length} scripts:");
                results.AppendLine();
                
                int successCount = 0;
                foreach (var name in names)
                {
                    string cleanName = name.Trim();
                    if (string.IsNullOrEmpty(cleanName)) continue;
                    
                    string namespaceDecl = string.IsNullOrEmpty(baseNamespace) ? "" : $"namespace {baseNamespace}\n{{\n";
                    string namespaceEnd = string.IsNullOrEmpty(baseNamespace) ? "" : "\n}";
                    string indent = string.IsNullOrEmpty(baseNamespace) ? "" : "    ";
                    
                    string content = $@"using UnityEngine;

{namespaceDecl}{indent}public class {cleanName} : MonoBehaviour
{indent}{{
{indent}    void Start()
{indent}    {{
{indent}        
{indent}    }}

{indent}    void Update()
{indent}    {{
{indent}        
{indent}    }}
{indent}}}{namespaceEnd}";
                    
                    string path = $"Assets/{cleanName}.cs";
                    System.IO.File.WriteAllText(path, content);
                    results.AppendLine($"✅ {cleanName}.cs");
                    successCount++;
                }
                
                AssetDatabase.Refresh();
                
                results.AppendLine();
                results.AppendLine($"✅ Created {successCount} script(s) successfully!");
                
                Debug.Log($"[CreateMultipleScripts] Created {successCount} scripts");
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error creating multiple scripts: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add namespace to script
        /// </summary>
        public static string AddNamespaceToScript(string scriptName, string namespaceName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Check if namespace already exists
                if (content.Contains($"namespace {namespaceName}"))
                    return $"ℹ️ Namespace '{namespaceName}' already exists in {scriptName}.cs";
                
                // Find where to insert namespace
                var lines = content.Split('\n');
                int lastUsingIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("using "))
                        lastUsingIndex = i;
                }
                
                // Add namespace after using statements
                var newLines = new System.Collections.Generic.List<string>();
                for (int i = 0; i <= lastUsingIndex; i++)
                {
                    newLines.Add(lines[i]);
                }
                
                newLines.Add("");
                newLines.Add($"namespace {namespaceName}");
                newLines.Add("{");
                
                // Add remaining lines with indentation
                for (int i = lastUsingIndex + 1; i < lines.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(lines[i]))
                        newLines.Add("    " + lines[i]);
                    else
                        newLines.Add(lines[i]);
                }
                
                newLines.Add("}");
                
                string newContent = string.Join("\n", newLines);
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddNamespaceToScript] Added namespace '{namespaceName}' to {scriptName}.cs");
                
                return $"✅ Added namespace '{namespaceName}' to {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error adding namespace: {e.Message}";
            }
        }
        
        /// <summary>
        /// Modify an existing method in a script
        /// </summary>
        public static string ModifyMethod(string scriptName, string methodName, string newMethodBody)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Find method using regex
                var methodPattern = $@"(public|private|protected|internal)\s+(\w+\s+)?{methodName}\s*\([^\)]*\)\s*\{{";
                var match = System.Text.RegularExpressions.Regex.Match(content, methodPattern);
                
                if (!match.Success)
                    return $"❌ Method '{methodName}' not found in {scriptName}.cs";
                
                int methodStart = match.Index;
                
                // Find method end (matching braces)
                int braceCount = 1;
                int methodBodyStart = content.IndexOf('{', methodStart) + 1;
                int methodEnd = methodBodyStart;
                
                for (int i = methodBodyStart; i < content.Length && braceCount > 0; i++)
                {
                    if (content[i] == '{') braceCount++;
                    if (content[i] == '}') braceCount--;
                    methodEnd = i;
                }
                
                if (braceCount != 0)
                    return $"❌ Could not find method end - brace mismatch";
                
                // Extract method signature
                string methodSignature = content.Substring(methodStart, methodBodyStart - methodStart);
                
                // Build new method
                string newMethod = methodSignature + "\n" + newMethodBody + "\n    }";
                
                // Replace method
                string beforeMethod = content.Substring(0, methodStart);
                string afterMethod = content.Substring(methodEnd + 1);
                string newContent = beforeMethod + newMethod + afterMethod;
                
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Method not modified.\n{roslynReport}";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[ModifyMethod] Modified method '{methodName}' in {scriptName}.cs");
                
                return $"✅ Modified method '{methodName}' in {scriptName}.cs\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error modifying method: {e.Message}";
            }
        }
        
        /// <summary>
        /// Delete a method from a script
        /// </summary>
        public static string DeleteMethod(string scriptName, string methodName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Find method using regex
                var methodPattern = $@"(public|private|protected|internal)\s+(\w+\s+)?{methodName}\s*\([^\)]*\)\s*\{{";
                var match = System.Text.RegularExpressions.Regex.Match(content, methodPattern);
                
                if (!match.Success)
                    return $"❌ Method '{methodName}' not found in {scriptName}.cs";
                
                int methodStart = match.Index;
                
                // Find method end (matching braces)
                int braceCount = 1;
                int methodBodyStart = content.IndexOf('{', methodStart) + 1;
                int methodEnd = methodBodyStart;
                
                for (int i = methodBodyStart; i < content.Length && braceCount > 0; i++)
                {
                    if (content[i] == '{') braceCount++;
                    if (content[i] == '}') braceCount--;
                    methodEnd = i;
                }
                
                if (braceCount != 0)
                    return $"❌ Could not find method end - brace mismatch";
                
                // Remove method (including signature)
                string beforeMethod = content.Substring(0, methodStart);
                string afterMethod = content.Substring(methodEnd + 1);
                string newContent = beforeMethod + afterMethod;
                
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Method not deleted.\n{roslynReport}";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[DeleteMethod] Deleted method '{methodName}' from {scriptName}.cs");
                
                return $"✅ Deleted method '{methodName}' from {scriptName}.cs\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error deleting method: {e.Message}";
            }
        }
        
        /// <summary>
        /// Rename a method in a script
        /// </summary>
        public static string RenameMethod(string scriptName, string oldMethodName, string newMethodName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Use regex to find and replace method name in signature
                var methodPattern = $@"(public|private|protected|internal)\s+(\w+\s+){oldMethodName}\s*\(";
                var newContent = System.Text.RegularExpressions.Regex.Replace(content, methodPattern, 
                    m => m.Value.Replace(oldMethodName, newMethodName));
                
                if (newContent == content)
                    return $"❌ Method '{oldMethodName}' not found in {scriptName}.cs";
                
                // Also replace method calls (simple text replace for now)
                newContent = newContent.Replace($"{oldMethodName}(", $"{newMethodName}(");
                
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Method not renamed.\n{roslynReport}";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[RenameMethod] Renamed method '{oldMethodName}' to '{newMethodName}' in {scriptName}.cs");
                
                return $"✅ Renamed method '{oldMethodName}' to '{newMethodName}' in {scriptName}.cs\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error renaming method: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create a property with get/set in a script
        /// </summary>
        public static string CreateProperty(string scriptName, string propertyType, string propertyName, string getBody = null, string setBody = null)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Build property code
                string propertyCode;
                if (string.IsNullOrEmpty(getBody) && string.IsNullOrEmpty(setBody))
                {
                    // Auto-property
                    propertyCode = $"public {propertyType} {propertyName} {{ get; set; }}";
                }
                else
                {
                    // Custom get/set
                    propertyCode = $@"public {propertyType} {propertyName}
    {{
        get
        {{
            {getBody ?? "return default;"}
        }}
        set
        {{
            {setBody ?? ""}
        }}
    }}";
                }
                
                // Find first opening brace (class start)
                int firstBrace = content.IndexOf('{');
                if (firstBrace == -1)
                    return $"❌ Invalid script structure - no opening brace found";
                
                // Insert property after the first opening brace
                string indent = "    "; // 4 spaces
                string formattedProperty = "\n" + indent + propertyCode.Replace("\n", "\n" + indent) + "\n";
                string newContent = content.Insert(firstBrace + 1, formattedProperty);
                
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Property not added.\n{roslynReport}";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[CreateProperty] Added property '{propertyName}' to {scriptName}.cs");
                
                return $"✅ Added property '{propertyName}' to {scriptName}.cs\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error creating property: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add interface implementation to a class
        /// </summary>
        public static string AddInterface(string scriptName, string interfaceName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Find class declaration
                var classPattern = $@"class\s+{scriptName}\s*(:.*?)?\s*\{{";
                var match = System.Text.RegularExpressions.Regex.Match(content, classPattern);
                
                if (!match.Success)
                    return $"❌ Class '{scriptName}' not found in script";
                
                string classDeclaration = match.Value;
                string newClassDeclaration;
                
                if (classDeclaration.Contains(":"))
                {
                    // Already has inheritance/interfaces
                    newClassDeclaration = classDeclaration.Replace("{", $", {interfaceName}\n{{");
                }
                else
                {
                    // No inheritance yet
                    newClassDeclaration = classDeclaration.Replace("{", $" : {interfaceName}\n{{");
                }
                
                string newContent = content.Replace(classDeclaration, newClassDeclaration);
                
                if (!RoslynScriptAnalyzer.TryValidate(newContent, out var roslynReport))
                {
                    return $"❌ Roslyn validation failed. Interface not added.\n{roslynReport}\n💡 You may need to implement interface members manually.";
                }
                
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddInterface] Added interface '{interfaceName}' to {scriptName}.cs");
                
                return $"✅ Added interface '{interfaceName}' to {scriptName}.cs\n⚠️ Don't forget to implement interface members!\nℹ️ {roslynReport}";
            }
            catch (Exception e)
            {
                return $"❌ Error adding interface: {e.Message}";
            }
        }
        
        /// <summary>
        /// Add using statement to a script
        /// </summary>
        public static string AddUsingStatement(string scriptName, string namespaceName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                
                // Check if using statement already exists
                string usingStatement = $"using {namespaceName};";
                if (content.Contains(usingStatement))
                    return $"ℹ️ Using statement 'using {namespaceName};' already exists in {scriptName}.cs";
                
                // Find last using statement
                var lines = content.Split('\n').ToList();
                int lastUsingIndex = -1;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].Trim().StartsWith("using "))
                        lastUsingIndex = i;
                }
                
                if (lastUsingIndex >= 0)
                {
                    // Add after last using
                    lines.Insert(lastUsingIndex + 1, usingStatement);
                }
                else
                {
                    // No using statements, add at beginning
                    lines.Insert(0, usingStatement);
                }
                
                string newContent = string.Join("\n", lines);
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[AddUsingStatement] Added 'using {namespaceName};' to {scriptName}.cs");
                
                return $"✅ Added 'using {namespaceName};' to {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error adding using statement: {e.Message}";
            }
        }
        
        /// <summary>
        /// Remove unused using statements from a script
        /// </summary>
        public static string RemoveUnusedUsing(string scriptName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                var lines = content.Split('\n').ToList();
                
                var usingLines = new System.Collections.Generic.List<int>();
                var usingNamespaces = new System.Collections.Generic.List<string>();
                
                // Find all using statements
                for (int i = 0; i < lines.Count; i++)
                {
                    var trimmed = lines[i].Trim();
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                    {
                        usingLines.Add(i);
                        var ns = trimmed.Substring(6, trimmed.Length - 7).Trim();
                        usingNamespaces.Add(ns);
                    }
                }
                
                if (usingNamespaces.Count == 0)
                    return $"ℹ️ No using statements found in {scriptName}.cs";
                
                // Check which namespaces are used in the code
                var usedNamespaces = new System.Collections.Generic.List<string>();
                var codeWithoutUsings = string.Join("\n", lines.Skip(usingLines.Count));
                
                foreach (var ns in usingNamespaces)
                {
                    // Check if namespace types are used in code
                    // This is a simple check - more sophisticated would use Roslyn
                    var nsTypes = ns.Split('.').Last();
                    if (codeWithoutUsings.Contains(nsTypes) || ns == "UnityEngine" || ns == "System")
                    {
                        usedNamespaces.Add(ns);
                    }
                }
                
                // Remove unused using statements
                int removedCount = 0;
                for (int i = usingLines.Count - 1; i >= 0; i--)
                {
                    if (!usedNamespaces.Contains(usingNamespaces[i]))
                    {
                        lines.RemoveAt(usingLines[i]);
                        removedCount++;
                    }
                }
                
                if (removedCount == 0)
                    return $"ℹ️ No unused using statements found in {scriptName}.cs";
                
                string newContent = string.Join("\n", lines);
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[RemoveUnusedUsing] Removed {removedCount} unused using statement(s) from {scriptName}.cs");
                
                return $"✅ Removed {removedCount} unused using statement(s) from {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error removing unused using statements: {e.Message}";
            }
        }
        
        /// <summary>
        /// Format code (basic indentation and spacing)
        /// </summary>
        public static string FormatCode(string scriptName)
        {
            try
            {
                var scriptGuids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                if (scriptGuids.Length == 0)
                    return $"❌ Script '{scriptName}' not found";
                
                string scriptPath = null;
                foreach (var guid in scriptGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null && script.name == scriptName)
                    {
                        scriptPath = path;
                        break;
                    }
                }
                
                if (string.IsNullOrEmpty(scriptPath))
                    return $"❌ Script '{scriptName}' not found";
                
                string content = System.IO.File.ReadAllText(scriptPath);
                var lines = content.Split('\n');
                var formattedLines = new System.Collections.Generic.List<string>();
                
                int indentLevel = 0;
                string indent = "    "; // 4 spaces
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    
                    // Decrease indent before closing brace
                    if (trimmed.StartsWith("}"))
                        indentLevel = Math.Max(0, indentLevel - 1);
                    
                    // Add formatted line
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        formattedLines.Add(new string(' ', indentLevel * indent.Length) + trimmed);
                    }
                    else
                    {
                        formattedLines.Add("");
                    }
                    
                    // Increase indent after opening brace
                    if (trimmed.EndsWith("{"))
                        indentLevel++;
                }
                
                string newContent = string.Join("\n", formattedLines);
                System.IO.File.WriteAllText(scriptPath, newContent);
                AssetDatabase.Refresh();
                
                Debug.Log($"[FormatCode] Formatted {scriptName}.cs");
                
                return $"✅ Formatted {scriptName}.cs";
            }
            catch (Exception e)
            {
                return $"❌ Error formatting code: {e.Message}";
            }
        }
    }
}

