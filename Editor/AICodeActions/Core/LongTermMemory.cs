using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Long-term Memory System: Persistent memory across sessions
    /// Types: Episodic (events), Semantic (facts), User Preferences, Project Context
    /// </summary>
    [Serializable]
    public class Memory
    {
        public string id;
        public MemoryType type;
        public string content;
        public Dictionary<string, string> metadata;
        public DateTime timestamp;
        public int accessCount;
        public float importance; // 0.0 - 1.0
        public DateTime lastAccessed;
        
        public Memory()
        {
            id = Guid.NewGuid().ToString();
            metadata = new Dictionary<string, string>();
            timestamp = DateTime.Now;
            lastAccessed = DateTime.Now;
            accessCount = 0;
            importance = 0.5f;
        }
        
        public Memory(MemoryType type, string content, float importance = 0.5f) : this()
        {
            this.type = type;
            this.content = content;
            this.importance = importance;
        }
        
        public void Access()
        {
            accessCount++;
            lastAccessed = DateTime.Now;
            
            // Increase importance with access (spaced repetition)
            importance = Mathf.Min(1.0f, importance + 0.05f);
        }
        
        public float GetRelevanceScore(DateTime currentTime)
        {
            // Combine importance with recency
            float timeDiff = (float)(currentTime - lastAccessed).TotalHours;
            float recencyScore = 1.0f / (1.0f + timeDiff / 24.0f); // Decay over days
            
            return (importance * 0.7f) + (recencyScore * 0.3f);
        }
        
        public override string ToString()
        {
            return $"[{type}] {content} (Importance: {importance:F2}, Accessed: {accessCount}x)";
        }
    }
    
    public enum MemoryType
    {
        Episodic,           // Events: "Created PlayerController script"
        Semantic,           // Facts: "Project uses PascalCase naming"
        UserPreference,     // User preferences: "Prefers verbose logging"
        ProjectContext,     // Project info: "FPS game with multiplayer"
        Success,            // Successful operations
        Failure,            // Failed operations (to avoid repeat)
        Insight,            // Learned insights
        Tool,               // Tool usage patterns
    }
    
    /// <summary>
    /// Long-term Memory Manager
    /// </summary>
    public class LongTermMemoryManager
    {
        private List<Memory> memories;
        private int maxMemories = 1000;
        private string memoryFilePath;
        
        public int MemoryCount => memories.Count;
        
        public LongTermMemoryManager(int maxMemories = 1000)
        {
            this.maxMemories = maxMemories;
            this.memories = new List<Memory>();
            
            // Use Unity Library folder for persistence
            memoryFilePath = Path.Combine("Library", "AICodeActions_LongTermMemory.json");
            
            Load();
        }
        
        /// <summary>
        /// Store new memory
        /// </summary>
        public Memory Store(MemoryType type, string content, float importance = 0.5f, Dictionary<string, string> metadata = null)
        {
            var memory = new Memory(type, content, importance);
            
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    memory.metadata[kvp.Key] = kvp.Value;
                }
            }
            
            memories.Add(memory);
            
            // Prune if exceeding max
            if (memories.Count > maxMemories)
            {
                PruneOldMemories();
            }
            
            Save();
            
            Debug.Log($"[Memory] Stored: {memory}");
            
            return memory;
        }
        
        /// <summary>
        /// Recall memories by type
        /// </summary>
        public List<Memory> Recall(MemoryType type, int limit = 10)
        {
            return memories
                .Where(m => m.type == type)
                .OrderByDescending(m => m.GetRelevanceScore(DateTime.Now))
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Search memories by content
        /// </summary>
        public List<Memory> Search(string query, int limit = 10)
        {
            query = query.ToLower();
            
            return memories
                .Where(m => m.content.ToLower().Contains(query) || 
                           m.metadata.Values.Any(v => v.ToLower().Contains(query)))
                .OrderByDescending(m => m.GetRelevanceScore(DateTime.Now))
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Get most important memories
        /// </summary>
        public List<Memory> GetMostImportant(int limit = 10)
        {
            return memories
                .OrderByDescending(m => m.importance)
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Get recent memories
        /// </summary>
        public List<Memory> GetRecent(int limit = 10, MemoryType? filterType = null)
        {
            var query = filterType.HasValue 
                ? memories.Where(m => m.type == filterType.Value)
                : memories;
            
            return query
                .OrderByDescending(m => m.timestamp)
                .Take(limit)
                .ToList();
        }
        
        /// <summary>
        /// Get memory by ID
        /// </summary>
        public Memory GetById(string id)
        {
            var memory = memories.FirstOrDefault(m => m.id == id);
            
            if (memory != null)
            {
                memory.Access();
                Save();
            }
            
            return memory;
        }
        
        /// <summary>
        /// Update memory importance
        /// </summary>
        public void UpdateImportance(string id, float importance)
        {
            var memory = memories.FirstOrDefault(m => m.id == id);
            
            if (memory != null)
            {
                memory.importance = Mathf.Clamp01(importance);
                Save();
            }
        }
        
        /// <summary>
        /// Delete memory
        /// </summary>
        public bool Delete(string id)
        {
            var memory = memories.FirstOrDefault(m => m.id == id);
            
            if (memory != null)
            {
                memories.Remove(memory);
                Save();
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Clear all memories
        /// </summary>
        public void Clear()
        {
            memories.Clear();
            Save();
        }
        
        /// <summary>
        /// Consolidate similar memories
        /// </summary>
        public int ConsolidateMemories()
        {
            int consolidated = 0;
            
            // Group similar memories and keep most important
            var groups = memories
                .GroupBy(m => new { m.type, Content = NormalizeContent(m.content) })
                .Where(g => g.Count() > 1);
            
            foreach (var group in groups)
            {
                var toKeep = group.OrderByDescending(m => m.importance).First();
                var toRemove = group.Where(m => m.id != toKeep.id).ToList();
                
                // Merge access counts and importance
                toKeep.accessCount += toRemove.Sum(m => m.accessCount);
                toKeep.importance = Mathf.Min(1.0f, toKeep.importance + toRemove.Sum(m => m.importance * 0.1f));
                
                foreach (var mem in toRemove)
                {
                    memories.Remove(mem);
                    consolidated++;
                }
            }
            
            if (consolidated > 0)
            {
                Save();
                Debug.Log($"[Memory] Consolidated {consolidated} duplicate memories");
            }
            
            return consolidated;
        }
        
        /// <summary>
        /// Prune old, unimportant memories
        /// </summary>
        private void PruneOldMemories()
        {
            // Keep memories with high importance or recent access
            int toRemove = memories.Count - (int)(maxMemories * 0.9f);
            
            var candidates = memories
                .OrderBy(m => m.GetRelevanceScore(DateTime.Now))
                .Take(toRemove)
                .ToList();
            
            foreach (var memory in candidates)
            {
                memories.Remove(memory);
            }
            
            Debug.Log($"[Memory] Pruned {toRemove} old memories");
        }
        
        /// <summary>
        /// Normalize content for comparison
        /// </summary>
        private string NormalizeContent(string content)
        {
            return content.ToLower().Trim().Substring(0, Math.Min(50, content.Length));
        }
        
        /// <summary>
        /// Get statistics
        /// </summary>
        public string GetStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📚 LONG-TERM MEMORY STATISTICS");
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine($"Total Memories: {memories.Count}/{maxMemories}");
            sb.AppendLine();
            
            // By type
            var byType = memories.GroupBy(m => m.type).OrderByDescending(g => g.Count());
            sb.AppendLine("By Type:");
            foreach (var group in byType)
            {
                sb.AppendLine($"  {group.Key}: {group.Count()}");
            }
            sb.AppendLine();
            
            // Most accessed
            var mostAccessed = memories.OrderByDescending(m => m.accessCount).Take(3);
            sb.AppendLine("Most Accessed:");
            foreach (var mem in mostAccessed)
            {
                sb.AppendLine($"  • {mem.content.Substring(0, Math.Min(50, mem.content.Length))}... ({mem.accessCount}x)");
            }
            sb.AppendLine();
            
            // Average importance
            float avgImportance = memories.Count > 0 ? memories.Average(m => m.importance) : 0;
            sb.AppendLine($"Average Importance: {avgImportance:F2}");
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Save to disk
        /// </summary>
        public void Save()
        {
            try
            {
                var data = new MemoryData { memories = memories };
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(memoryFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Memory] Failed to save: {e.Message}");
            }
        }
        
        /// <summary>
        /// Load from disk
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(memoryFilePath))
                {
                    string json = File.ReadAllText(memoryFilePath);
                    var data = JsonUtility.FromJson<MemoryData>(json);
                    
                    if (data != null && data.memories != null)
                    {
                        memories = data.memories;
                        Debug.Log($"[Memory] Loaded {memories.Count} memories from disk");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Memory] Failed to load: {e.Message}");
                memories = new List<Memory>();
            }
        }
        
        [Serializable]
        private class MemoryData
        {
            public List<Memory> memories;
        }
        
        /// <summary>
        /// Get context summary for AI
        /// </summary>
        public string GetContextSummary(int maxItems = 10)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Long-Term Memory Context");
            sb.AppendLine();
            
            // Recent important memories
            var important = GetMostImportant(maxItems);
            if (important.Count > 0)
            {
                sb.AppendLine("## Key Information:");
                foreach (var mem in important)
                {
                    sb.AppendLine($"- {mem.content}");
                }
                sb.AppendLine();
            }
            
            // User preferences
            var prefs = Recall(MemoryType.UserPreference, 5);
            if (prefs.Count > 0)
            {
                sb.AppendLine("## User Preferences:");
                foreach (var mem in prefs)
                {
                    sb.AppendLine($"- {mem.content}");
                }
                sb.AppendLine();
            }
            
            // Project context
            var context = Recall(MemoryType.ProjectContext, 5);
            if (context.Count > 0)
            {
                sb.AppendLine("## Project Context:");
                foreach (var mem in context)
                {
                    sb.AppendLine($"- {mem.content}");
                }
            }
            
            return sb.ToString();
        }
    }
}

