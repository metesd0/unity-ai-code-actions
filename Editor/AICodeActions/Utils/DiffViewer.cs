using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AICodeActions.Utils
{
    /// <summary>
    /// Simple diff viewer for code changes
    /// </summary>
    public static class DiffViewer
    {
        public enum DiffType
        {
            None,
            Added,
            Removed,
            Modified
        }

        [Serializable]
        public class DiffLine
        {
            public int lineNumber;
            public string content;
            public DiffType type;

            public DiffLine(int line, string text, DiffType diffType)
            {
                lineNumber = line;
                content = text;
                type = diffType;
            }
        }

        /// <summary>
        /// Generates a simple line-by-line diff
        /// </summary>
        public static List<DiffLine> GenerateDiff(string original, string modified)
        {
            var originalLines = original.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var modifiedLines = modified.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            return GenerateDiff(originalLines, modifiedLines);
        }

        public static List<DiffLine> GenerateDiff(string[] originalLines, string[] modifiedLines)
        {
            var diff = new List<DiffLine>();
            int maxLen = Math.Max(originalLines.Length, modifiedLines.Length);

            // Simple line-by-line comparison (Myers diff algorithm can be added later)
            for (int i = 0; i < maxLen; i++)
            {
                string origLine = i < originalLines.Length ? originalLines[i] : null;
                string modLine = i < modifiedLines.Length ? modifiedLines[i] : null;

                if (origLine == null && modLine != null)
                {
                    // Added line
                    diff.Add(new DiffLine(i + 1, modLine, DiffType.Added));
                }
                else if (origLine != null && modLine == null)
                {
                    // Removed line
                    diff.Add(new DiffLine(i + 1, origLine, DiffType.Removed));
                }
                else if (origLine != modLine)
                {
                    // Modified line
                    diff.Add(new DiffLine(i + 1, origLine, DiffType.Removed));
                    diff.Add(new DiffLine(i + 1, modLine, DiffType.Added));
                }
                else
                {
                    // Unchanged line (include for context)
                    diff.Add(new DiffLine(i + 1, origLine, DiffType.None));
                }
            }

            return diff;
        }

        /// <summary>
        /// Formats diff for display with color codes
        /// </summary>
        public static string FormatDiffForConsole(List<DiffLine> diff)
        {
            var lines = new List<string>();
            
            foreach (var line in diff)
            {
                string prefix = line.type switch
                {
                    DiffType.Added => "+ ",
                    DiffType.Removed => "- ",
                    _ => "  "
                };

                lines.Add($"{line.lineNumber,4} {prefix}{line.content}");
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Gets a summary of changes
        /// </summary>
        public static string GetDiffSummary(List<DiffLine> diff)
        {
            int added = diff.Count(d => d.type == DiffType.Added);
            int removed = diff.Count(d => d.type == DiffType.Removed);
            int unchanged = diff.Count(d => d.type == DiffType.None);

            return $"Changes: +{added} -{removed} (total {diff.Count} lines)";
        }
    }
}

