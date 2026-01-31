using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Manage multiple file attachments for context
    /// Drag & drop support, preview, remove
    /// </summary>
    [Serializable]
    public class MultiFileContextManager
    {
        [Serializable]
        public class AttachedFile
        {
            public string path;
            public string name;
            public FileType type;
            public long sizeBytes;
            public bool isExpanded;
            public string preview;
            
            public AttachedFile(string path)
            {
                this.path = path;
                this.name = Path.GetFileName(path);
                this.type = DetectFileType(path);
                this.isExpanded = false;
                
                try
                {
                    var fileInfo = new FileInfo(path);
                    this.sizeBytes = fileInfo.Length;
                    
                    // Generate preview
                    if (fileInfo.Length < 100000) // Max 100KB for preview
                    {
                        string content = File.ReadAllText(path);
                        this.preview = content.Length > 500 
                            ? content.Substring(0, 500) + "..." 
                            : content;
                    }
                    else
                    {
                        this.preview = "[File too large for preview]";
                    }
                }
                catch
                {
                    this.sizeBytes = 0;
                    this.preview = "[Cannot read file]";
                }
            }
            
            private static FileType DetectFileType(string path)
            {
                string ext = Path.GetExtension(path).ToLower();
                return ext switch
                {
                    ".cs" => FileType.CSharp,
                    ".json" => FileType.JSON,
                    ".xml" => FileType.XML,
                    ".txt" => FileType.Text,
                    ".md" => FileType.Markdown,
                    ".shader" => FileType.Shader,
                    ".prefab" => FileType.Prefab,
                    ".unity" => FileType.Scene,
                    _ => FileType.Other
                };
            }
            
            public string GetSizeString()
            {
                if (sizeBytes < 1024)
                    return $"{sizeBytes} B";
                else if (sizeBytes < 1024 * 1024)
                    return $"{sizeBytes / 1024} KB";
                else
                    return $"{sizeBytes / (1024 * 1024)} MB";
            }
            
            public string GetIcon()
            {
                return type switch
                {
                    FileType.CSharp => "📄",
                    FileType.JSON => "📋",
                    FileType.XML => "📋",
                    FileType.Text => "📝",
                    FileType.Markdown => "📖",
                    FileType.Shader => "🎨",
                    FileType.Prefab => "🎮",
                    FileType.Scene => "🗺️",
                    _ => "📁"
                };
            }
        }
        
        public enum FileType
        {
            CSharp,
            JSON,
            XML,
            Text,
            Markdown,
            Shader,
            Prefab,
            Scene,
            Other
        }
        
        private List<AttachedFile> attachedFiles = new List<AttachedFile>();
        private Vector2 scrollPos;
        private GUIStyle fileBoxStyle;
        private GUIStyle fileHeaderStyle;
        private GUIStyle fileContentStyle;
        private bool stylesInitialized = false;
        
        public int FileCount => attachedFiles.Count;
        public List<AttachedFile> Files => attachedFiles;
        
        /// <summary>
        /// Add file to context
        /// </summary>
        public bool AddFile(string path)
        {
            try
            {
                // Check if already added
                if (attachedFiles.Any(f => f.path == path))
                {
                    Debug.LogWarning($"File already attached: {path}");
                    return false;
                }
                
                // Check if file exists
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"File not found: {path}");
                    return false;
                }
                
                var file = new AttachedFile(path);
                attachedFiles.Add(file);
                
                Debug.Log($"[MultiFile] Added: {file.name} ({file.GetSizeString()})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MultiFile] Error adding file: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Remove file from context
        /// </summary>
        public void RemoveFile(AttachedFile file)
        {
            attachedFiles.Remove(file);
        }
        
        /// <summary>
        /// Clear all files
        /// </summary>
        public void ClearAll()
        {
            attachedFiles.Clear();
        }
        
        /// <summary>
        /// Draw file context UI
        /// </summary>
        public void DrawFileContext()
        {
            InitializeStyles();
            
            if (attachedFiles.Count == 0)
            {
                DrawEmptyState();
                return;
            }
            
            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"📎 <b>Attached Files ({attachedFiles.Count})</b>", fileHeaderStyle);
            
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Clear All Files", 
                    "Remove all attached files from context?", "Yes", "No"))
                {
                    ClearAll();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(4);
            
            // File list
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(200));
            
            for (int i = attachedFiles.Count - 1; i >= 0; i--)
            {
                DrawFile(attachedFiles[i]);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(4);
        }
        
        /// <summary>
        /// Draw single file
        /// </summary>
        private void DrawFile(AttachedFile file)
        {
            EditorGUILayout.BeginVertical(fileBoxStyle);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            // Icon
            GUILayout.Label(file.GetIcon(), GUILayout.Width(20));
            
            // Expand/collapse
            string arrow = file.isExpanded ? "▼" : "▶";
            if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(15)))
            {
                file.isExpanded = !file.isExpanded;
            }
            
            // File name
            GUILayout.Label($"<b>{file.name}</b>", fileHeaderStyle);
            
            GUILayout.FlexibleSpace();
            
            // Size
            GUILayout.Label(file.GetSizeString(), EditorStyles.miniLabel);
            
            // Remove button
            if (GUILayout.Button("✖", GUILayout.Width(20)))
            {
                RemoveFile(file);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Preview (if expanded)
            if (file.isExpanded)
            {
                DrawFilePreview(file);
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        }
        
        /// <summary>
        /// Draw file preview
        /// </summary>
        private void DrawFilePreview(AttachedFile file)
        {
            EditorGUI.indentLevel++;
            
            GUILayout.Space(4);
            GUILayout.Label($"<color=#888888><i>{file.path}</i></color>", fileContentStyle);
            
            GUILayout.Space(4);
            GUILayout.Label("<b>Preview:</b>", fileContentStyle);
            
            // Show preview in scrollable area
            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.MaxHeight(100));
            GUILayout.Label(file.preview, fileContentStyle);
            EditorGUILayout.EndVertical();
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Copy Path", GUILayout.Width(80)))
            {
                GUIUtility.systemCopyBuffer = file.path;
                Debug.Log($"Path copied: {file.path}");
            }
            
            if (GUILayout.Button("Open", GUILayout.Width(60)))
            {
                if (file.type == FileType.CSharp)
                {
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<MonoScript>(file.path));
                }
                else
                {
                    System.Diagnostics.Process.Start(file.path);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
        }
        
        /// <summary>
        /// Draw empty state (drag & drop hint)
        /// </summary>
        private void DrawEmptyState()
        {
            EditorGUILayout.BeginVertical(fileBoxStyle, GUILayout.MinHeight(60));
            
            GUILayout.FlexibleSpace();
            
            var centerStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                richText = true,
                fontSize = 11
            };
            
            GUILayout.Label("📎 <color=#888888>No files attached</color>", centerStyle);
            GUILayout.Label("<color=#666666><i>Drag & drop files here or use Selection button</i></color>", centerStyle);
            
            GUILayout.FlexibleSpace();
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Handle drag & drop
        /// </summary>
        public void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            
            if (dropArea.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                }
                else if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    
                    foreach (string path in DragAndDrop.paths)
                    {
                        AddFile(path);
                    }
                    
                    foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                    {
                        string path = AssetDatabase.GetAssetPath(obj);
                        if (!string.IsNullOrEmpty(path))
                        {
                            AddFile(path);
                        }
                    }
                    
                    evt.Use();
                }
            }
        }
        
        /// <summary>
        /// Get context summary for AI
        /// </summary>
        public string GetContextSummary()
        {
            if (attachedFiles.Count == 0)
                return "";
            
            var sb = new StringBuilder();
            sb.AppendLine("# Attached Files Context");
            sb.AppendLine();
            
            foreach (var file in attachedFiles)
            {
                sb.AppendLine($"## {file.name}");
                sb.AppendLine($"Path: {file.path}");
                sb.AppendLine($"Type: {file.type}");
                sb.AppendLine();
                
                if (file.type == FileType.CSharp && File.Exists(file.path))
                {
                    try
                    {
                        string content = File.ReadAllText(file.path);
                        sb.AppendLine("```csharp");
                        sb.AppendLine(content.Length > 2000 ? content.Substring(0, 2000) + "\n// ..." : content);
                        sb.AppendLine("```");
                    }
                    catch { }
                }
                else
                {
                    sb.AppendLine(file.preview);
                }
                
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Initialize GUI styles
        /// </summary>
        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;
            
            fileBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(2, 2, 1, 1)
            };
            
            fileHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 11
            };
            
            fileContentStyle = new GUIStyle(EditorStyles.label)
            {
                richText = true,
                fontSize = 10,
                wordWrap = true
            };
            
            stylesInitialized = true;
        }
        
        /// <summary>
        /// Add current selection to context
        /// </summary>
        public int AddSelection()
        {
            int added = 0;
            
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && AddFile(path))
                {
                    added++;
                }
            }
            
            return added;
        }
    }
}

