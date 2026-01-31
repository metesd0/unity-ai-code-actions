using System;
using System.Collections.Generic;
using System.Text;

namespace AICodeActions.Core
{
    /// <summary>
    /// Self-Correction tool wrappers
    /// </summary>
    public static partial class UnityAgentTools
    {
        private static SelfCorrectionEngine selfCorrectionEngine;
        
        /// <summary>
        /// Initialize self-correction engine
        /// </summary>
        public static void InitializeSelfCorrection(AgentToolSystem toolSystem, int maxRetries = 3)
        {
            selfCorrectionEngine = new SelfCorrectionEngine(toolSystem, maxRetries);
        }
        
        /// <summary>
        /// Execute tool with self-correction
        /// </summary>
        public static string ExecuteWithSelfCorrection(string toolName, string parametersJson, string maxRetries = "3")
        {
            try
            {
                if (selfCorrectionEngine == null)
                {
                    return "⚠️ Self-correction not initialized. Use from chat window for auto-correction.";
                }
                
                int retries = 3;
                if (int.TryParse(maxRetries, out int parsed))
                {
                    retries = parsed;
                }
                
                // Parse parameters from JSON
                var parameters = new Dictionary<string, string>();
                
                // Simple JSON parsing (for basic key:value pairs)
                var lines = parametersJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim().Trim('"');
                        string value = line.Substring(colonIndex + 1).Trim().Trim('"', ',');
                        parameters[key] = value;
                    }
                }
                
                var engine = new SelfCorrectionEngine(null, retries); // Create temp engine
                var result = engine.ExecuteWithCorrection(toolName, parameters);
                
                return result;
            }
            catch (Exception e)
            {
                return $"❌ Self-correction error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Analyze error without fixing
        /// </summary>
        public static string AnalyzeError(string errorMessage)
        {
            try
            {
                var analysis = ErrorAnalyzer.Analyze(errorMessage);
                
                var result = new StringBuilder();
                result.AppendLine(analysis.ToString());
                result.AppendLine();
                result.AppendLine($"📊 Fix Priority: {ErrorAnalyzer.GetFixPriority(analysis.category)}/10");
                result.AppendLine($"💡 Confidence: {ErrorAnalyzer.GetConfidenceDescription(analysis.confidence)}");
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Analysis error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Suggest fixes for an error
        /// </summary>
        public static string SuggestFixes(string errorMessage)
        {
            try
            {
                var analysis = ErrorAnalyzer.Analyze(errorMessage);
                
                var result = new StringBuilder();
                result.AppendLine("🔧 SUGGESTED FIXES");
                result.AppendLine("═══════════════════════════════════");
                result.AppendLine($"Error Category: {analysis.category}");
                result.AppendLine($"Root Cause: {analysis.rootCause}");
                result.AppendLine();
                
                if (analysis.possibleFixes.Length > 0)
                {
                    result.AppendLine("Possible Fixes:");
                    for (int i = 0; i < analysis.possibleFixes.Length; i++)
                    {
                        result.AppendLine($"{i + 1}. {analysis.possibleFixes[i]}");
                    }
                }
                else
                {
                    result.AppendLine("No automatic fixes available.");
                    result.AppendLine("Manual intervention required.");
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Validate operation result
        /// </summary>
        public static string ValidateOperation(string toolName, string parametersJson, string result)
        {
            try
            {
                if (selfCorrectionEngine == null)
                {
                    return "⚠️ Self-correction not initialized";
                }
                
                // Parse parameters
                var parameters = new Dictionary<string, string>();
                var lines = parametersJson.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        string key = line.Substring(0, colonIndex).Trim().Trim('"');
                        string value = line.Substring(colonIndex + 1).Trim().Trim('"', ',');
                        parameters[key] = value;
                    }
                }
                
                bool isValid = selfCorrectionEngine.Validate(toolName, parameters, result);
                
                if (isValid)
                {
                    return $"✅ Operation '{toolName}' validated successfully";
                }
                else
                {
                    return $"❌ Operation '{toolName}' validation failed";
                }
            }
            catch (Exception e)
            {
                return $"❌ Validation error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Get self-correction statistics
        /// </summary>
        public static string GetSelfCorrectionStats()
        {
            try
            {
                if (selfCorrectionEngine == null)
                {
                    return "Self-Correction Engine: Not initialized";
                }
                
                return selfCorrectionEngine.GetStatistics();
            }
            catch (Exception e)
            {
                return $"❌ Error: {e.Message}";
            }
        }
        
        /// <summary>
        /// Set self-correction engine (called from window)
        /// </summary>
        public static void SetSelfCorrectionEngine(SelfCorrectionEngine engine)
        {
            selfCorrectionEngine = engine;
        }
    }
}

