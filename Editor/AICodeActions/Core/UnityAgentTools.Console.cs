using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// Console Operations: Read Unity Console messages (errors, warnings, logs)
    /// </summary>
    public static partial class UnityAgentTools
    {
        /// <summary>
        /// Read recent Unity Console messages
        /// </summary>
        public static string ReadConsole(int count = 10, string filterType = "all")
        {
            try
            {
                // Use reflection to access Unity's internal LogEntries class
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null)
                {
                    return "❌ Could not access Unity Console API";
                }

                // Get count of log entries
                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public);
                if (getCountMethod == null)
                {
                    return "❌ Could not access Console entry count";
                }

                int totalCount = (int)getCountMethod.Invoke(null, null);
                
                if (totalCount == 0)
                {
                    return "✅ Console is empty - no errors, warnings, or messages";
                }

                // Get entry info
                var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public);
                if (getEntryInternalMethod == null)
                {
                    return "❌ Could not access Console entry reader";
                }

                var result = new StringBuilder();
                result.AppendLine($"📋 **Unity Console** ({totalCount} total messages)");
                result.AppendLine();

                // Read recent messages (from end, newest first)
            int readCount = Math.Min(count, totalCount);
            int errorCount = 0;
            int warningCount = 0;
            int logCount = 0;

            // Get LogEntry type for reflection (it's also internal!)
            var logEntryType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LogEntry");
            if (logEntryType == null)
            {
                return "❌ LogEntry type not found (Unity internal API changed).";
            }

            var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var messageField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            for (int i = totalCount - readCount; i < totalCount; i++)
            {
                var entry = Activator.CreateInstance(logEntryType);
                getEntryInternalMethod.Invoke(null, new object[] { i, entry });

                    // Get fields via reflection
                    int mode = modeField != null ? (int)modeField.GetValue(entry) : 0;
                    string message = messageField != null ? (string)messageField.GetValue(entry) : "";
                    string file = fileField != null ? (string)fileField.GetValue(entry) : "";
                    int line = lineField != null ? (int)lineField.GetValue(entry) : 0;

                    // Filter by type
                    string logType = GetLogType(mode);
                    if (filterType != "all" && !logType.ToLower().Contains(filterType.ToLower()))
                    {
                        continue;
                    }

                    // Count by type
                    if (logType == "Error" || logType == "Exception") errorCount++;
                    else if (logType == "Warning") warningCount++;
                    else logCount++;

                    // Format message
                    string icon = GetLogIcon(mode);
                    result.AppendLine($"{icon} **{logType}:** {message}");
                    
                    if (!string.IsNullOrEmpty(file))
                    {
                        result.AppendLine($"   📁 {file}:{line}");
                    }
                    result.AppendLine();
                }

                // Summary
                result.AppendLine("---");
                result.AppendLine($"**Summary:** {errorCount} errors, {warningCount} warnings, {logCount} logs");

                return result.ToString();
            }
            catch (Exception e)
            {
                return $"❌ Error reading Console: {e.Message}\n\nNote: This is expected - Unity's Console API is internal.";
            }
        }

        /// <summary>
        /// Get the last compilation errors
        /// </summary>
        public static string GetCompilationErrors()
        {
            try
            {
                // Check if there are compilation errors
                if (!EditorUtility.scriptCompilationFailed)
                {
                    return "✅ No compilation errors - all scripts compiled successfully!";
                }

                // Read console for error messages
                return ReadConsole(20, "error");
            }
            catch (Exception e)
            {
                return $"❌ Error checking compilation: {e.Message}";
            }
        }

        private static string GetLogType(int mode)
        {
            // Unity LogEntry mode flags
            // 0 = Log, 1 = Warning, 2 = Error, 4 = Exception
            if ((mode & 2) != 0) return "Error";
            if ((mode & 4) != 0) return "Exception";
            if ((mode & 1) != 0) return "Warning";
            return "Log";
        }

        private static string GetLogIcon(int mode)
        {
            if ((mode & 2) != 0 || (mode & 4) != 0) return "❌";
            if ((mode & 1) != 0) return "⚠️";
            return "ℹ️";
        }
    }
}

