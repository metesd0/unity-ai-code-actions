using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Semantic Search with RAG (Retrieval-Augmented Generation)
    /// Provides context-aware code search and relevant examples
    /// </summary>
    public static partial class UnityAgentTools
    {
        private static VectorDatabase codeVectorDB;
        private static VectorDatabase conversationVectorDB;
        private static bool isIndexed = false;
        
        /// <summary>
        /// Initialize vector databases
        /// </summary>
        private static void InitializeVectorDB()
        {
            if (codeVectorDB == null)
            {
                codeVectorDB = new VectorDatabase();
                conversationVectorDB = new VectorDatabase();
                
                // Try to load cached index
                LoadVectorDBCache();
            }
        }
        
        /// <summary>
        /// Index entire project for semantic search
        /// </summary>
        public static string IndexProjectForSemanticSearch(bool forceReindex = false)
        {
            try
            {
                InitializeVectorDB();
                
                if (isIndexed && !forceReindex)
                    return "ℹ️ Project already indexed. Use force_reindex=true to reindex.";
                
                var results = new StringBuilder();
                results.AppendLine("🔍 Indexing project for semantic search...");
                results.AppendLine();
                
                // Clear existing index
                codeVectorDB.Clear();
                
                // Find all C# scripts
                var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
                var scripts = scriptGuids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => path.StartsWith("Assets/") && path.EndsWith(".cs"))
                    .ToList();
                
                int indexed = 0;
                int skipped = 0;
                
                foreach (var scriptPath in scripts)
                {
                    try
                    {
                        string content = File.ReadAllText(scriptPath);
                        string scriptName = Path.GetFileNameWithoutExtension(scriptPath);
                        
                        // Skip very small files (likely empty)
                        if (content.Length < 100)
                        {
                            skipped++;
                            continue;
                        }
                        
                        // Create embedding (simple TF-IDF style for now - can be upgraded to OpenAI embeddings)
                        var embedding = CreateSimpleEmbedding(content);
                        
                        // Add to vector DB
                        var entry = new VectorDatabase.VectorEntry(
                            id: scriptPath,
                            content: content,
                            embedding: embedding,
                            metadata: new Dictionary<string, string>
                            {
                                { "type", "script" },
                                { "name", scriptName },
                                { "path", scriptPath }
                            }
                        );
                        
                        codeVectorDB.Add(entry);
                        indexed++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to index {scriptPath}: {ex.Message}");
                        skipped++;
                    }
                }
                
                isIndexed = true;
                
                results.AppendLine($"✅ Indexing complete!");
                results.AppendLine($"  - Indexed: {indexed} scripts");
                results.AppendLine($"  - Skipped: {skipped} scripts");
                results.AppendLine();
                results.AppendLine("💡 Now you can use semantic_search to find code by meaning!");
                
                // Save cache
                SaveVectorDBCache();
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error indexing project: {e.Message}";
            }
        }
        
        /// <summary>
        /// Semantic search - find code by meaning, not just text
        /// </summary>
        public static string SemanticSearch(string query, int topK = 5)
        {
            try
            {
                InitializeVectorDB();
                
                if (codeVectorDB.Count == 0)
                {
                    return "❌ Project not indexed yet. Use index_project_for_semantic_search first!";
                }
                
                var results = new StringBuilder();
                results.AppendLine($"🔍 Semantic Search: \"{query}\"");
                results.AppendLine();
                
                // Create query embedding
                var queryEmbedding = CreateSimpleEmbedding(query);
                
                // Search
                var searchResults = codeVectorDB.Search(queryEmbedding, topK, threshold: 0.1f);
                
                if (searchResults.Count == 0)
                {
                    results.AppendLine("❌ No relevant code found.");
                    results.AppendLine("💡 Try different keywords or reindex project.");
                    return results.ToString();
                }
                
                results.AppendLine($"✅ Found {searchResults.Count} relevant result(s):");
                results.AppendLine();
                
                int rank = 1;
                foreach (var (entry, similarity) in searchResults)
                {
                    string scriptName = entry.metadata.ContainsKey("name") ? entry.metadata["name"] : "Unknown";
                    string path = entry.metadata.ContainsKey("path") ? entry.metadata["path"] : "";
                    
                    results.AppendLine($"**{rank}. {scriptName}** (similarity: {similarity:F3})");
                    results.AppendLine($"   📍 {path}");
                    
                    // Show code preview (first 200 chars)
                    string preview = entry.content.Length > 200 
                        ? entry.content.Substring(0, 200) + "..." 
                        : entry.content;
                    
                    results.AppendLine($"   📄 Preview:");
                    var previewLines = preview.Split('\n').Take(5);
                    foreach (var line in previewLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                            results.AppendLine($"      {line.Trim()}");
                    }
                    
                    results.AppendLine();
                    rank++;
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error in semantic search: {e.Message}";
            }
        }
        
        /// <summary>
        /// Find similar code to a given code snippet
        /// </summary>
        public static string FindSimilarCode(string codeSnippet, int topK = 5)
        {
            try
            {
                InitializeVectorDB();
                
                if (codeVectorDB.Count == 0)
                {
                    return "❌ Project not indexed yet. Use index_project_for_semantic_search first!";
                }
                
                var results = new StringBuilder();
                results.AppendLine($"🔍 Finding similar code...");
                results.AppendLine();
                
                // Create embedding for snippet
                var embedding = CreateSimpleEmbedding(codeSnippet);
                
                // Search
                var searchResults = codeVectorDB.Search(embedding, topK + 1, threshold: 0.1f);
                
                // Filter out the exact same code if it exists
                searchResults = searchResults
                    .Where(r => !r.entry.content.Contains(codeSnippet))
                    .Take(topK)
                    .ToList();
                
                if (searchResults.Count == 0)
                {
                    results.AppendLine("❌ No similar code found.");
                    return results.ToString();
                }
                
                results.AppendLine($"✅ Found {searchResults.Count} similar code snippet(s):");
                results.AppendLine();
                
                int rank = 1;
                foreach (var (entry, similarity) in searchResults)
                {
                    string scriptName = entry.metadata.ContainsKey("name") ? entry.metadata["name"] : "Unknown";
                    string path = entry.metadata.ContainsKey("path") ? entry.metadata["path"] : "";
                    
                    results.AppendLine($"**{rank}. {scriptName}** (similarity: {similarity:F3})");
                    results.AppendLine($"   📍 {path}");
                    results.AppendLine();
                    rank++;
                }
                
                return results.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error finding similar code: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get relevant code context for a task/query (RAG augmentation)
        /// </summary>
        public static string GetRelevantContext(string taskDescription, int maxResults = 3)
        {
            try
            {
                InitializeVectorDB();
                
                if (codeVectorDB.Count == 0)
                {
                    return ""; // Silent fail - no context available
                }
                
                // Create query embedding
                var queryEmbedding = CreateSimpleEmbedding(taskDescription);
                
                // Search for relevant code
                var searchResults = codeVectorDB.Search(queryEmbedding, maxResults, threshold: 0.2f);
                
                if (searchResults.Count == 0)
                    return "";
                
                var context = new StringBuilder();
                context.AppendLine("# Relevant Code Context (from your project):");
                context.AppendLine();
                
                foreach (var (entry, similarity) in searchResults)
                {
                    string scriptName = entry.metadata.ContainsKey("name") ? entry.metadata["name"] : "Unknown";
                    
                    context.AppendLine($"## From {scriptName}:");
                    context.AppendLine("```csharp");
                    
                    // Include first 500 chars of relevant code
                    string codePreview = entry.content.Length > 500 
                        ? entry.content.Substring(0, 500) + "\n// ..." 
                        : entry.content;
                    
                    context.AppendLine(codePreview);
                    context.AppendLine("```");
                    context.AppendLine();
                }
                
                return context.ToString();
            }
            catch
            {
                return ""; // Silent fail
            }
        }
        
        /// <summary>
        /// Add conversation to memory for future reference
        /// </summary>
        public static string AddToConversationMemory(string query, string response)
        {
            try
            {
                InitializeVectorDB();
                
                string combinedText = $"Query: {query}\nResponse: {response}";
                var embedding = CreateSimpleEmbedding(combinedText);
                
                var entry = new VectorDatabase.VectorEntry(
                    id: $"conv_{DateTime.Now.Ticks}",
                    content: combinedText,
                    embedding: embedding,
                    metadata: new Dictionary<string, string>
                    {
                        { "type", "conversation" },
                        { "query", query }
                    }
                );
                
                conversationVectorDB.Add(entry);
                
                return "✅ Added to conversation memory";
            }
            catch (Exception e)
            {
                return $"⚠️ Failed to add to memory: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get vector database statistics
        /// </summary>
        public static string GetVectorDBStats()
        {
            try
            {
                InitializeVectorDB();
                
                var stats = new StringBuilder();
                stats.AppendLine("📊 VECTOR DATABASE STATISTICS");
                stats.AppendLine("══════════════════════════════");
                stats.AppendLine();
                
                stats.AppendLine("## Code Database:");
                stats.AppendLine(codeVectorDB.GetStats());
                stats.AppendLine();
                
                stats.AppendLine("## Conversation Database:");
                stats.AppendLine(conversationVectorDB.GetStats());
                
                return stats.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error getting stats: {e.Message}";
            }
        }
        
        /// <summary>
        /// Create simple TF-IDF style embedding
        /// NOTE: Can be upgraded to OpenAI embeddings for better quality
        /// </summary>
        private static float[] CreateSimpleEmbedding(string text, int dimensions = 128)
        {
            // Simple bag-of-words + TF-IDF style embedding
            // This is a placeholder - for production, use OpenAI embeddings API
            
            var embedding = new float[dimensions];
            
            // Normalize text
            text = text.ToLower();
            
            // Common programming keywords (weighted)
            var keywords = new Dictionary<string, float>
            {
                { "class", 1.0f }, { "public", 0.8f }, { "private", 0.8f },
                { "void", 0.9f }, { "return", 0.9f }, { "if", 0.7f },
                { "for", 0.7f }, { "while", 0.7f }, { "foreach", 0.7f },
                { "update", 1.2f }, { "start", 1.2f }, { "awake", 1.2f },
                { "gameobject", 1.5f }, { "transform", 1.5f }, { "vector", 1.3f },
                { "monobehaviour", 1.8f }, { "component", 1.5f },
                { "player", 1.4f }, { "enemy", 1.4f }, { "health", 1.4f },
                { "damage", 1.4f }, { "movement", 1.4f }, { "controller", 1.6f }
            };
            
            // Hash text into embedding space
            foreach (var kvp in keywords)
            {
                if (text.Contains(kvp.Key))
                {
                    int hash = kvp.Key.GetHashCode();
                    int index = Math.Abs(hash % dimensions);
                    embedding[index] += kvp.Value;
                }
            }
            
            // Add character-level features
            for (int i = 0; i < text.Length && i < 1000; i += 10)
            {
                if (i + 5 < text.Length)
                {
                    string ngram = text.Substring(i, 5);
                    int hash = ngram.GetHashCode();
                    int index = Math.Abs(hash % dimensions);
                    embedding[index] += 0.1f;
                }
            }
            
            // Normalize
            float norm = 0f;
            for (int i = 0; i < dimensions; i++)
            {
                norm += embedding[i] * embedding[i];
            }
            norm = Mathf.Sqrt(norm);
            
            if (norm > 0f)
            {
                for (int i = 0; i < dimensions; i++)
                {
                    embedding[i] /= norm;
                }
            }
            
            return embedding;
        }
        
        /// <summary>
        /// Save vector DB cache
        /// </summary>
        private static void SaveVectorDBCache()
        {
            try
            {
                string cachePath = "Library/AICodeActions_VectorDB_Cache.json";
                string json = codeVectorDB.ToJson();
                File.WriteAllText(cachePath, json);
                Debug.Log("[VectorDB] Cache saved");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VectorDB] Failed to save cache: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load vector DB cache
        /// </summary>
        private static void LoadVectorDBCache()
        {
            try
            {
                string cachePath = "Library/AICodeActions_VectorDB_Cache.json";
                if (File.Exists(cachePath))
                {
                    string json = File.ReadAllText(cachePath);
                    codeVectorDB = VectorDatabase.FromJson(json);
                    isIndexed = codeVectorDB.Count > 0;
                    Debug.Log($"[VectorDB] Loaded cache: {codeVectorDB.Count} entries");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VectorDB] Failed to load cache: {e.Message}");
                codeVectorDB = new VectorDatabase();
            }
        }
    }
}

