using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Self-Correction Engine: Detect errors → Analyze → Fix → Validate → Retry
    /// </summary>
    public class SelfCorrectionEngine
    {
        public class CorrectionAttempt
        {
            public int attemptNumber;
            public string originalOperation;
            public string errorMessage;
            public ErrorAnalyzer.ErrorAnalysis analysis;
            public FixStrategy.FixResult fixResult;
            public string retryResult;
            public bool successful;
            public DateTime timestamp;
            
            public CorrectionAttempt()
            {
                timestamp = DateTime.Now;
            }
        }
        
        public class CorrectionSession
        {
            public string operation;
            public List<CorrectionAttempt> attempts;
            public bool finalSuccess;
            public int maxAttempts;
            public DateTime startTime;
            public DateTime endTime;
            
            public CorrectionSession(string operation, int maxAttempts = 3)
            {
                this.operation = operation;
                this.maxAttempts = maxAttempts;
                this.attempts = new List<CorrectionAttempt>();
                this.startTime = DateTime.Now;
            }
            
            public string GetSummary()
            {
                var sb = new StringBuilder();
                sb.AppendLine("🔄 SELF-CORRECTION SESSION");
                sb.AppendLine("═══════════════════════════════════");
                sb.AppendLine($"Operation: {operation}");
                sb.AppendLine($"Attempts: {attempts.Count}/{maxAttempts}");
                sb.AppendLine($"Final Status: {(finalSuccess ? "✅ SUCCESS" : "❌ FAILED")}");
                sb.AppendLine($"Duration: {(endTime - startTime).TotalSeconds:F2}s");
                sb.AppendLine();
                
                for (int i = 0; i < attempts.Count; i++)
                {
                    var attempt = attempts[i];
                    sb.AppendLine($"## Attempt {i + 1}:");
                    sb.AppendLine($"Error: {GetShortError(attempt.errorMessage)}");
                    sb.AppendLine($"Category: {attempt.analysis.category}");
                    sb.AppendLine($"Fix: {attempt.fixResult.action}");
                    sb.AppendLine($"Result: {(attempt.successful ? "✅" : "❌")}");
                    sb.AppendLine();
                }
                
                return sb.ToString();
            }
            
            private string GetShortError(string error)
            {
                if (error.Length <= 80)
                    return error;
                return error.Substring(0, 80) + "...";
            }
        }
        
        private AgentToolSystem toolSystem;
        private int maxRetries = 3;
        
        public SelfCorrectionEngine(AgentToolSystem toolSystem, int maxRetries = 3)
        {
            this.toolSystem = toolSystem;
            this.maxRetries = maxRetries;
        }
        
        /// <summary>
        /// Execute operation with self-correction
        /// </summary>
        public string ExecuteWithCorrection(
            string toolName, 
            Dictionary<string, string> parameters,
            Action<string> progressCallback = null)
        {
            var session = new CorrectionSession(toolName, maxRetries);
            
            progressCallback?.Invoke($"🔄 Starting {toolName} with self-correction...\n");
            
            string lastResult = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                var correctionAttempt = new CorrectionAttempt
                {
                    attemptNumber = attempt + 1,
                    originalOperation = toolName
                };
                
                progressCallback?.Invoke($"\n⚡ Attempt {attempt + 1}/{maxRetries}\n");
                
                // Execute tool
                string result = toolSystem.ExecuteTool(toolName, parameters);
                lastResult = result;
                
                // Check if successful
                if (IsSuccess(result))
                {
                    correctionAttempt.successful = true;
                    correctionAttempt.retryResult = result;
                    session.attempts.Add(correctionAttempt);
                    session.finalSuccess = true;
                    session.endTime = DateTime.Now;
                    
                    progressCallback?.Invoke($"✅ Success on attempt {attempt + 1}!\n");
                    
                    return result;
                }
                
                // Error detected - analyze
                progressCallback?.Invoke("🔍 Error detected, analyzing...\n");
                
                correctionAttempt.errorMessage = result;
                correctionAttempt.analysis = ErrorAnalyzer.Analyze(result);
                
                progressCallback?.Invoke($"📊 Error Category: {correctionAttempt.analysis.category}\n");
                progressCallback?.Invoke($"🎯 Root Cause: {correctionAttempt.analysis.rootCause}\n");
                progressCallback?.Invoke($"💡 Confidence: {ErrorAnalyzer.GetConfidenceDescription(correctionAttempt.analysis.confidence)}\n");
                
                // Try to fix
                if (attempt < maxRetries - 1) // Don't fix on last attempt
                {
                    progressCallback?.Invoke("\n🔧 Applying automatic fix...\n");
                    
                    correctionAttempt.fixResult = FixStrategy.ApplyFix(
                        correctionAttempt.analysis, 
                        parameters);
                    
                    progressCallback?.Invoke(correctionAttempt.fixResult.ToString() + "\n");
                    
                    if (correctionAttempt.fixResult.success)
                    {
                        progressCallback?.Invoke("✅ Fix applied, retrying operation...\n");
                        // Continue loop to retry
                    }
                    else
                    {
                        progressCallback?.Invoke("⚠️ Automatic fix not available, retrying anyway...\n");
                    }
                }
                
                session.attempts.Add(correctionAttempt);
            }
            
            // All attempts failed
            session.finalSuccess = false;
            session.endTime = DateTime.Now;
            
            progressCallback?.Invoke($"\n❌ All {maxRetries} attempts failed.\n");
            progressCallback?.Invoke(session.GetSummary());
            
            return lastResult;
        }
        
        /// <summary>
        /// Check if result indicates success
        /// </summary>
        private bool IsSuccess(string result)
        {
            if (string.IsNullOrEmpty(result))
                return false;
            
            // Success indicators
            if (result.Contains("✅"))
                return true;
            
            // Failure indicators
            if (result.Contains("❌") || result.Contains("Error") || result.Contains("Failed"))
                return false;
            
            // Neutral result is considered success
            return true;
        }
        
        /// <summary>
        /// Validate operation result
        /// </summary>
        public bool Validate(string toolName, Dictionary<string, string> parameters, string result)
        {
            try
            {
                // Success check
                if (!IsSuccess(result))
                    return false;
                
                // Tool-specific validation
                switch (toolName)
                {
                    case "create_gameobject":
                        return ValidateGameObjectCreation(parameters, result);
                    
                    case "create_and_attach_script":
                        return ValidateScriptCreation(parameters, result);
                    
                    case "add_component":
                        return ValidateComponentAddition(parameters, result);
                    
                    default:
                        // Generic validation: check for success marker
                        return result.Contains("✅");
                }
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Validate GameObject creation
        /// </summary>
        private bool ValidateGameObjectCreation(Dictionary<string, string> parameters, string result)
        {
            if (!parameters.ContainsKey("name"))
                return false;
            
            string goName = parameters["name"];
            var go = GameObject.Find(goName);
            
            return go != null;
        }
        
        /// <summary>
        /// Validate script creation
        /// </summary>
        private bool ValidateScriptCreation(Dictionary<string, string> parameters, string result)
        {
            if (!parameters.ContainsKey("script_name"))
                return false;
            
            string scriptName = parameters["script_name"];
            
            // Check if script exists in AssetDatabase
            var scriptGuids = UnityEditor.AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
            
            return scriptGuids.Length > 0;
        }
        
        /// <summary>
        /// Validate component addition
        /// </summary>
        private bool ValidateComponentAddition(Dictionary<string, string> parameters, string result)
        {
            if (!parameters.ContainsKey("gameobject_name") || !parameters.ContainsKey("component_type"))
                return false;
            
            string goName = parameters["gameobject_name"];
            string componentType = parameters["component_type"];
            
            var go = GameObject.Find(goName);
            if (go == null)
                return false;
            
            // Check if component exists
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp != null && comp.GetType().Name == componentType)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get correction statistics
        /// </summary>
        public string GetStatistics()
        {
            // This would track stats over time
            // For now, return placeholder
            return "Self-Correction Engine Ready\nMax Retries: " + maxRetries;
        }
    }
}

