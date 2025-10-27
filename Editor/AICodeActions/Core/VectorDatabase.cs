using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Simple in-memory vector database for semantic code search
    /// Uses cosine similarity for vector comparison
    /// </summary>
    [Serializable]
    public class VectorDatabase
    {
        [Serializable]
        public class VectorEntry
        {
            public string id;
            public string content;
            public float[] embedding;
            public Dictionary<string, string> metadata;
            public DateTime timestamp;
            
            public VectorEntry(string id, string content, float[] embedding, Dictionary<string, string> metadata = null)
            {
                this.id = id;
                this.content = content;
                this.embedding = embedding;
                this.metadata = metadata ?? new Dictionary<string, string>();
                this.timestamp = DateTime.Now;
            }
        }
        
        private List<VectorEntry> entries = new List<VectorEntry>();
        private Dictionary<string, VectorEntry> indexById = new Dictionary<string, VectorEntry>();
        
        public int Count => entries.Count;
        
        /// <summary>
        /// Add entry to database
        /// </summary>
        public void Add(VectorEntry entry)
        {
            if (indexById.ContainsKey(entry.id))
            {
                // Update existing
                var existing = indexById[entry.id];
                entries.Remove(existing);
            }
            
            entries.Add(entry);
            indexById[entry.id] = entry;
        }
        
        /// <summary>
        /// Search by vector similarity (cosine similarity)
        /// </summary>
        public List<(VectorEntry entry, float similarity)> Search(float[] queryVector, int topK = 5, float threshold = 0.0f)
        {
            if (queryVector == null || queryVector.Length == 0)
                return new List<(VectorEntry, float)>();
            
            var results = new List<(VectorEntry entry, float similarity)>();
            
            foreach (var entry in entries)
            {
                float similarity = CosineSimilarity(queryVector, entry.embedding);
                if (similarity >= threshold)
                {
                    results.Add((entry, similarity));
                }
            }
            
            return results
                .OrderByDescending(r => r.similarity)
                .Take(topK)
                .ToList();
        }
        
        /// <summary>
        /// Search by metadata filter
        /// </summary>
        public List<VectorEntry> SearchByMetadata(string key, string value)
        {
            return entries
                .Where(e => e.metadata.ContainsKey(key) && e.metadata[key] == value)
                .ToList();
        }
        
        /// <summary>
        /// Get entry by ID
        /// </summary>
        public VectorEntry GetById(string id)
        {
            return indexById.ContainsKey(id) ? indexById[id] : null;
        }
        
        /// <summary>
        /// Remove entry
        /// </summary>
        public bool Remove(string id)
        {
            if (indexById.ContainsKey(id))
            {
                var entry = indexById[id];
                entries.Remove(entry);
                indexById.Remove(id);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Clear all entries
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            indexById.Clear();
        }
        
        /// <summary>
        /// Calculate cosine similarity between two vectors
        /// </summary>
        private float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
                return 0f;
            
            float dotProduct = 0f;
            float normA = 0f;
            float normB = 0f;
            
            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }
            
            if (normA == 0f || normB == 0f)
                return 0f;
            
            return dotProduct / (Mathf.Sqrt(normA) * Mathf.Sqrt(normB));
        }
        
        /// <summary>
        /// Save to JSON
        /// </summary>
        public string ToJson()
        {
            var data = new VectorDatabaseData
            {
                entries = entries
            };
            return JsonUtility.ToJson(data, true);
        }
        
        /// <summary>
        /// Load from JSON
        /// </summary>
        public static VectorDatabase FromJson(string json)
        {
            var data = JsonUtility.FromJson<VectorDatabaseData>(json);
            var db = new VectorDatabase();
            
            if (data != null && data.entries != null)
            {
                foreach (var entry in data.entries)
                {
                    db.Add(entry);
                }
            }
            
            return db;
        }
        
        [Serializable]
        private class VectorDatabaseData
        {
            public List<VectorEntry> entries;
        }
        
        /// <summary>
        /// Get statistics
        /// </summary>
        public string GetStats()
        {
            var stats = new StringBuilder();
            stats.AppendLine($"📊 Vector Database Statistics");
            stats.AppendLine($"═══════════════════════════════");
            stats.AppendLine($"Total Entries: {entries.Count}");
            
            if (entries.Count > 0)
            {
                stats.AppendLine($"Embedding Dimension: {entries[0].embedding.Length}");
                
                // Group by type
                var byType = entries
                    .GroupBy(e => e.metadata.ContainsKey("type") ? e.metadata["type"] : "unknown")
                    .OrderByDescending(g => g.Count());
                
                stats.AppendLine();
                stats.AppendLine("Entries by Type:");
                foreach (var group in byType)
                {
                    stats.AppendLine($"  - {group.Key}: {group.Count()}");
                }
            }
            
            return stats.ToString();
        }
    }
}

