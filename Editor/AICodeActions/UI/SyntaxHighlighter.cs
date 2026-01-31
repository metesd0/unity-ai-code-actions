using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// Syntax highlighter for code blocks in Unity Editor
    /// Uses Unity Rich Text for colorization
    /// </summary>
    public static class SyntaxHighlighter
    {
        // Color scheme (Unity Rich Text compatible)
        private static class Colors
        {
            // Dark theme colors (optimized for Unity's dark editor)
            public static readonly string Keyword = "#569CD6";      // Blue
            public static readonly string String = "#CE9178";       // Orange
            public static readonly string Comment = "#6A9955";      // Green
            public static readonly string Type = "#4EC9B0";         // Cyan
            public static readonly string Number = "#B5CEA8";       // Light green
            public static readonly string Method = "#DCDCAA";       // Yellow
            public static readonly string Variable = "#9CDCFE";     // Light blue
            public static readonly string Operator = "#D4D4D4";     // Light gray
            public static readonly string Preprocessor = "#C586C0"; // Purple
            
            // Light theme fallback
            public static readonly string KeywordLight = "#0000FF";
            public static readonly string StringLight = "#A31515";
            public static readonly string CommentLight = "#008000";
        }
        
        // C# Keywords
        private static readonly HashSet<string> Keywords = new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate",
            "do", "double", "else", "enum", "event", "explicit", "extern", "false",
            "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit",
            "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
            "new", "null", "object", "operator", "out", "override", "params", "private",
            "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
            "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
            "unsafe", "ushort", "using", "var", "virtual", "void", "volatile", "while"
        };
        
        // Unity-specific types
        private static readonly HashSet<string> UnityTypes = new HashSet<string>
        {
            "GameObject", "Transform", "MonoBehaviour", "Component", "Vector2", "Vector3",
            "Quaternion", "Color", "Rigidbody", "Collider", "AudioSource", "Camera",
            "Light", "Material", "Texture", "Sprite", "AnimationClip", "ScriptableObject"
        };
        
        /// <summary>
        /// Highlight C# code
        /// </summary>
        public static string HighlightCSharp(string code)
        {
            if (string.IsNullOrEmpty(code))
                return code;
            
            try
            {
                var result = new StringBuilder(code.Length * 2);
                var lines = code.Split('\n');
                
                foreach (var line in lines)
                {
                    result.AppendLine(HighlightLine(line));
                }
                
                return result.ToString();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SyntaxHighlighter] Error: {e.Message}");
                return code; // Return original if highlighting fails
            }
        }
        
        /// <summary>
        /// Highlight single line
        /// </summary>
        private static string HighlightLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return line;
            
            // Check for comments first (highest priority)
            if (line.TrimStart().StartsWith("//"))
            {
                return $"<color={Colors.Comment}>{EscapeRichText(line)}</color>";
            }
            
            // Check for preprocessor directives
            if (line.TrimStart().StartsWith("#"))
            {
                return $"<color={Colors.Preprocessor}>{EscapeRichText(line)}</color>";
            }
            
            var result = new StringBuilder();
            var tokens = TokenizeLine(line);
            
            foreach (var token in tokens)
            {
                result.Append(ColorizeToken(token));
            }
            
            return result.ToString();
        }
        
        /// <summary>
        /// Tokenize line into words/symbols
        /// </summary>
        private static List<Token> TokenizeLine(string line)
        {
            var tokens = new List<Token>();
            var current = new StringBuilder();
            bool inString = false;
            bool inChar = false;
            char stringChar = '\0';
            
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                
                // String handling
                if ((c == '"' || c == '\'') && (i == 0 || line[i - 1] != '\\'))
                {
                    if (!inString && !inChar)
                    {
                        // Flush current token
                        if (current.Length > 0)
                        {
                            tokens.Add(new Token(current.ToString(), TokenType.Word));
                            current.Clear();
                        }
                        
                        inString = c == '"';
                        inChar = c == '\'';
                        stringChar = c;
                        current.Append(c);
                    }
                    else if ((inString && c == '"') || (inChar && c == '\''))
                    {
                        current.Append(c);
                        tokens.Add(new Token(current.ToString(), TokenType.String));
                        current.Clear();
                        inString = false;
                        inChar = false;
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (inString || inChar)
                {
                    current.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(new Token(current.ToString(), TokenType.Word));
                        current.Clear();
                    }
                    tokens.Add(new Token(c.ToString(), TokenType.Whitespace));
                }
                else if (IsOperator(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(new Token(current.ToString(), TokenType.Word));
                        current.Clear();
                    }
                    tokens.Add(new Token(c.ToString(), TokenType.Operator));
                }
                else
                {
                    current.Append(c);
                }
            }
            
            if (current.Length > 0)
            {
                if (inString || inChar)
                    tokens.Add(new Token(current.ToString(), TokenType.String));
                else
                    tokens.Add(new Token(current.ToString(), TokenType.Word));
            }
            
            return tokens;
        }
        
        /// <summary>
        /// Colorize token
        /// </summary>
        private static string ColorizeToken(Token token)
        {
            string text = EscapeRichText(token.text);
            
            switch (token.type)
            {
                case TokenType.String:
                    return $"<color={Colors.String}>{text}</color>";
                
                case TokenType.Word:
                    if (Keywords.Contains(token.text))
                        return $"<color={Colors.Keyword}><b>{text}</b></color>";
                    
                    if (UnityTypes.Contains(token.text))
                        return $"<color={Colors.Type}><b>{text}</b></color>";
                    
                    if (char.IsUpper(token.text[0]) && !token.text.Contains("_"))
                        return $"<color={Colors.Type}>{text}</color>";
                    
                    if (IsNumber(token.text))
                        return $"<color={Colors.Number}>{text}</color>";
                    
                    if (token.text.EndsWith("()") || LooksLikeMethod(token.text))
                        return $"<color={Colors.Method}>{text}</color>";
                    
                    return text;
                
                case TokenType.Operator:
                    return $"<color={Colors.Operator}>{text}</color>";
                
                case TokenType.Whitespace:
                    return text;
                
                default:
                    return text;
            }
        }
        
        /// <summary>
        /// Check if character is operator
        /// </summary>
        private static bool IsOperator(char c)
        {
            return c == '+' || c == '-' || c == '*' || c == '/' || c == '=' ||
                   c == '<' || c == '>' || c == '!' || c == '&' || c == '|' ||
                   c == '^' || c == '%' || c == '?' || c == ':' ||
                   c == '(' || c == ')' || c == '[' || c == ']' ||
                   c == '{' || c == '}' || c == ';' || c == ',' || c == '.';
        }
        
        /// <summary>
        /// Check if string is number
        /// </summary>
        private static bool IsNumber(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            
            return Regex.IsMatch(str, @"^-?\d+\.?\d*f?$");
        }
        
        /// <summary>
        /// Check if looks like method call
        /// </summary>
        private static bool LooksLikeMethod(string str)
        {
            // Heuristic: starts with lowercase or has () at end
            return str.Length > 0 && (char.IsLower(str[0]) || str.Contains("("));
        }
        
        /// <summary>
        /// Escape rich text special characters
        /// </summary>
        private static string EscapeRichText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            // Escape < and > for Rich Text
            return text.Replace("<", "＜").Replace(">", "＞");
        }
        
        /// <summary>
        /// Token class
        /// </summary>
        private class Token
        {
            public string text;
            public TokenType type;
            
            public Token(string text, TokenType type)
            {
                this.text = text;
                this.type = type;
            }
        }
        
        /// <summary>
        /// Token types
        /// </summary>
        private enum TokenType
        {
            Word,
            String,
            Operator,
            Whitespace
        }
        
        /// <summary>
        /// Detect language from code content
        /// </summary>
        public static string DetectLanguage(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "text";
            
            // C# detection
            if (code.Contains("using ") || code.Contains("namespace ") ||
                code.Contains("public class") || code.Contains("MonoBehaviour"))
                return "csharp";
            
            // JSON detection
            if (code.TrimStart().StartsWith("{") && code.Contains(":"))
                return "json";
            
            // XML detection
            if (code.TrimStart().StartsWith("<") && code.Contains("</"))
                return "xml";
            
            return "text";
        }
        
        /// <summary>
        /// Apply syntax highlighting to code block
        /// </summary>
        public static string ApplyHighlighting(string code, string language = null)
        {
            if (string.IsNullOrEmpty(code))
                return code;
            
            // Auto-detect if not specified
            if (string.IsNullOrEmpty(language))
            {
                language = DetectLanguage(code);
            }
            
            switch (language.ToLower())
            {
                case "csharp":
                case "cs":
                case "c#":
                    return HighlightCSharp(code);
                
                // Future: Add more languages
                case "json":
                case "xml":
                case "javascript":
                case "python":
                default:
                    return EscapeRichText(code); // Return as-is for now
            }
        }
    }
}

