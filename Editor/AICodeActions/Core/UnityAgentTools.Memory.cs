using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AICodeActions.Core
{
    /// <summary>
    /// Long-term Memory tool wrappers
    /// </summary>
    public static partial class UnityAgentTools
    {
        private static LongTermMemoryManager memoryManager;
        private static UserPreferenceLearning preferenceLearning;
        private static ProjectContextAnalyzer projectAnalyzer;
        
        /// <summary>
        /// Initialize memory system
        /// </summary>
        public static void InitializeMemory(int maxMemories = 1000)
        {
            if (memoryManager == null)
            {
                memoryManager = new LongTermMemoryManager(maxMemories);
                preferenceLearning = new UserPreferenceLearning(memoryManager);
                projectAnalyzer = new ProjectContextAnalyzer(memoryManager);
                
                UnityEngine.Debug.Log($"[Memory] Initialized with {memoryManager.MemoryCount} existing memories");
            }
        }
        
        /// <summary>
        /// Store memory manually
        /// </summary>
        public static string StoreMemory(string type, string content, string importance = "0.5")
        {
            try
            {
                InitializeMemory();
                
                if (!Enum.TryParse<MemoryType>(type, true, out var memType))
                {
                    return $"❌ Invalid memory type: {type}\nValid types: {string.Join(", ", Enum.GetNames(typeof(MemoryType)))}";
                }
                
                float imp = 0.5f;
                if (float.TryParse(importance, out float parsed))
                {
                    imp = UnityEngine.Mathf.Clamp01(parsed);
                }
                
                var memory = memoryManager.Store(memType, content, imp);
                
                return $"✅ Memory stored\nID: {memory.id}\nType: {memType}\nImportance: {imp:F2}";
            }
            catch (Exception e)
            {
                return $"❌ Error storing memory: {e.Message}";
            }
        }
        
        /// <summary>
        /// Recall memories by type
        /// </summary>
        public static string RecallMemories(string type, string limit = "10")
        {
            try
            {
                InitializeMemory();
                
                if (!Enum.TryParse<MemoryType>(type, true, out var memType))
                {
                    return $"❌ Invalid memory type: {type}";
                }
                
                int lim = 10;
                if (int.TryParse(limit, out int parsed))
                {
                    lim = parsed;
                }
                
                var memories = memoryManager.Recall(memType, lim);
                
                if (memories.Count == 0)
                {
                    return $"ℹ️ No {memType} memories found.";
                }
                
                var result = new StringBuilder();
                result.AppendLine($"📚 {memType} MEMORIES ({memories.Count})");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine();
                
                for (int i = 0; i < memories.Count; i++)
                {
                    var mem = memories[i];
                    result.AppendLine($"**{i + 1}.** {mem.content}");
                    result.AppendLine($"   Importance: {mem.importance:F2} | Accessed: {mem.accessCount}x | {GetTimeAgo(mem.timestamp)}");
                    result.AppendLine();
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error recalling memories: {e.Message}";
            }
        }
        
        /// <summary>
        /// Search memories
        /// </summary>
        public static string SearchMemories(string query, string limit = "10")
        {
            try
            {
                InitializeMemory();
                
                int lim = 10;
                if (int.TryParse(limit, out int parsed))
                {
                    lim = parsed;
                }
                
                var memories = memoryManager.Search(query, lim);
                
                if (memories.Count == 0)
                {
                    return $"ℹ️ No memories found matching '{query}'.";
                }
                
                var result = new StringBuilder();
                result.AppendLine($"🔍 SEARCH RESULTS for \"{query}\" ({memories.Count})");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine();
                
                for (int i = 0; i < memories.Count; i++)
                {
                    var mem = memories[i];
                    result.AppendLine($"**{i + 1}.** [{mem.type}] {mem.content}");
                    result.AppendLine($"   {GetTimeAgo(mem.timestamp)}");
                    result.AppendLine();
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error searching memories: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get memory statistics
        /// </summary>
        public static string GetMemoryStats()
        {
            try
            {
                InitializeMemory();
                return memoryManager.GetStatistics();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get most important memories
        /// </summary>
        public static string GetImportantMemories(string limit = "10")
        {
            try
            {
                InitializeMemory();
                
                int lim = 10;
                if (int.TryParse(limit, out int parsed))
                {
                    lim = parsed;
                }
                
                var memories = memoryManager.GetMostImportant(lim);
                
                var result = new StringBuilder();
                result.AppendLine($"⭐ MOST IMPORTANT MEMORIES ({memories.Count})");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine();
                
                for (int i = 0; i < memories.Count; i++)
                {
                    var mem = memories[i];
                    result.AppendLine($"**{i + 1}.** [{mem.type}] {mem.content}");
                    result.AppendLine($"   Importance: {mem.importance:F2} | {GetTimeAgo(mem.timestamp)}");
                    result.AppendLine();
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Consolidate similar memories
        /// </summary>
        public static string ConsolidateMemories()
        {
            try
            {
                InitializeMemory();
                
                int consolidated = memoryManager.ConsolidateMemories();
                
                return $"✅ Consolidated {consolidated} duplicate memories\n" +
                       $"Total memories: {memoryManager.MemoryCount}";
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze project and store insights
        /// </summary>
        public static string AnalyzeProject()
        {
            try
            {
                InitializeMemory();
                return projectAnalyzer.AnalyzeProject();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get project context summary
        /// </summary>
        public static string GetProjectContext()
        {
            try
            {
                InitializeMemory();
                return projectAnalyzer.GetProjectSummary();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get user coding style profile
        /// </summary>
        public static string GetCodingStyle()
        {
            try
            {
                InitializeMemory();
                return preferenceLearning.GetCodingStyleSummary();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get memory context for AI (used internally)
        /// </summary>
        public static string GetMemoryContext(int maxItems = 10)
        {
            try
            {
                InitializeMemory();
                return memoryManager.GetContextSummary(maxItems);
            }
            catch
            {
                return "";
            }
        }
        
        /// <summary>
        /// Learn from operation (called internally)
        /// </summary>
        public static void LearnFromOperation(string operation, string result, bool success)
        {
            try
            {
                InitializeMemory();
                
                if (success)
                {
                    memoryManager.Store(MemoryType.Success,
                        $"Successfully executed: {operation}",
                        importance: 0.6f,
                        metadata: new Dictionary<string, string>
                        {
                            { "operation", operation },
                            { "result", result.Length > 100 ? result.Substring(0, 100) : result }
                        });
                }
                
                // Learn from script creation
                if (operation == "create_and_attach_script" && success)
                {
                    // Extract script content if available
                    // preferenceLearning.LearnFromScript(scriptName, scriptContent);
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Clear all memories (dangerous!)
        /// </summary>
        public static string ClearMemories(string confirm = "")
        {
            try
            {
                if (confirm.ToLower() != "yes")
                {
                    return "⚠️ This will delete ALL memories!\nTo confirm, use: clear_memories(confirm: \"yes\")";
                }
                
                InitializeMemory();
                int count = memoryManager.MemoryCount;
                memoryManager.Clear();
                
                return $"✅ Cleared {count} memories";
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        // Helper methods
        
        private static string GetTimeAgo(DateTime time)
        {
            var diff = DateTime.Now - time;
            
            if (diff.TotalMinutes < 1)
                return "just now";
            else if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes} min ago";
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour(s) ago";
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day(s) ago";
            else
                return time.ToString("MMM dd");
        }
        
        /// <summary>
        /// Get memory manager instance (for internal use)
        /// </summary>
        public static LongTermMemoryManager GetMemoryManager()
        {
            InitializeMemory();
            return memoryManager;
        }
    }
}

