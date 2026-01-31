using System;
using System.Linq;
using System.Reflection;

namespace AICodeActions.Core
{
    /// <summary>
    /// Lightweight Roslyn-based validator via reflection.
    /// Does not hard-reference Microsoft.CodeAnalysis.* so it won't break if assemblies are missing.
    /// If Roslyn assemblies are unavailable at runtime, validation degrades gracefully to 'pass'.
    /// </summary>
    public static class RoslynScriptAnalyzer
    {
        public static bool IsAvailable()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp");
                if (asm == null) return false;
                var type = asm.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
                return type != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Try validate C# source using Roslyn (syntax diagnostics). Returns true on success or when Roslyn is unavailable.
        /// Returns false when Roslyn is present and diagnostics contain errors.
        /// </summary>
        public static bool TryValidate(string sourceCode, out string report)
        {
            report = string.Empty;

            if (!IsAvailable())
            {
                report = "Roslyn not available - skipping deep validation.";
                return true; // Graceful pass
            }

            try
            {
                // Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(string)
                var csharpAsm = AppDomain.CurrentDomain.GetAssemblies()
                    .First(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp");
                var treeType = csharpAsm.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
                var parseText = treeType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "ParseText" && m.GetParameters().Length >= 1);

                var syntaxTree = parseText.Invoke(null, new object[] { sourceCode });
                var treeTypeRuntime = syntaxTree.GetType();
                var getDiagnostics = treeTypeRuntime.GetMethod("GetDiagnostics", BindingFlags.Public | BindingFlags.Instance);
                var diags = (System.Collections.IEnumerable)getDiagnostics.Invoke(syntaxTree, null);

                var hasErrors = false;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Roslyn syntax validation results:");

                foreach (var d in diags)
                {
                    // Diagnostic: Severity, Id, GetMessage()
                    var dType = d.GetType();
                    var severityProp = dType.GetProperty("Severity");
                    var idProp = dType.GetProperty("Id");
                    var getMessage = dType.GetMethod("GetMessage");

                    var severity = severityProp?.GetValue(d)?.ToString();
                    var id = idProp?.GetValue(d)?.ToString();
                    var message = getMessage?.Invoke(d, null)?.ToString();

                    if (!string.IsNullOrEmpty(severity) && severity.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasErrors = true;
                    }

                    sb.AppendLine($"- {severity} {id}: {message}");
                }

                report = sb.ToString();
                return !hasErrors;
            }
            catch (Exception ex)
            {
                // In case Roslyn API shape is different; do not block writes.
                report = $"Roslyn validation failed unexpectedly: {ex.Message}";
                return true; // Fail-open to avoid blocking edits due to analyzer issues
            }
        }
    }
}


