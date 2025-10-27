using System;

namespace AICodeActions.Core
{
    /// <summary>
    /// Semantic analysis and RAG tool wrappers
    /// </summary>
    public static partial class UnityAgentTools
    {
        // ===== ROSLYN SEMANTIC ANALYSIS =====
        
        /// <summary>
        /// Get call graph for a method
        /// </summary>
        public static string GetMethodCallGraph(string scriptName, string methodName)
        {
            return RoslynSemanticAnalyzer.GetCallGraph(scriptName, methodName);
        }
        
        /// <summary>
        /// Find all usages of a symbol (variable, field, property)
        /// </summary>
        public static string FindSymbolUsages(string scriptName, string symbolName)
        {
            return RoslynSemanticAnalyzer.FindSymbolUsages(scriptName, symbolName);
        }
        
        /// <summary>
        /// Analyze data flow for a variable
        /// </summary>
        public static string AnalyzeVariableDataFlow(string scriptName, string variableName)
        {
            return RoslynSemanticAnalyzer.AnalyzeDataFlow(scriptName, variableName);
        }
        
        /// <summary>
        /// Get all symbols in a script
        /// </summary>
        public static string GetScriptSymbols(string scriptName)
        {
            return RoslynSemanticAnalyzer.GetAllSymbols(scriptName);
        }
        
        // ===== RAG + SEMANTIC SEARCH =====
        
        /// <summary>
        /// Index project for semantic search (wrapper)
        /// </summary>
        public static string IndexProject(string forceReindex = "false")
        {
            bool force = forceReindex.ToLower() == "true";
            return IndexProjectForSemanticSearch(force);
        }
        
        /// <summary>
        /// Semantic search wrapper
        /// </summary>
        public static string SearchSemantic(string query, string topK = "5")
        {
            int k = 5;
            if (int.TryParse(topK, out int parsed))
                k = parsed;
            
            return SemanticSearch(query, k);
        }
        
        /// <summary>
        /// Find similar code wrapper
        /// </summary>
        public static string FindSimilar(string codeSnippet, string topK = "5")
        {
            int k = 5;
            if (int.TryParse(topK, out int parsed))
                k = parsed;
            
            return FindSimilarCode(codeSnippet, k);
        }
        
        /// <summary>
        /// Get vector database stats wrapper
        /// </summary>
        public static string GetRAGStats()
        {
            return GetVectorDBStats();
        }
    }
}

