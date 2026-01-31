using UnityEditor;
using UnityEngine;

namespace AICodeActions.UI
{
    /// <summary>
    /// GUI styles initialization for AI Chat Window
    /// </summary>
    public partial class AIChatWindow
    {
        private void InitializeStyles()
        {
            if (userMessageStyle == null)
            {
                userMessageStyle = CreateMessageStyle(new Color(0.3f, 0.5f, 0.8f, 0.2f));
            }

            if (assistantMessageStyle == null)
            {
                assistantMessageStyle = CreateMessageStyle(new Color(0.2f, 0.2f, 0.2f, 0.3f));
            }

            if (systemMessageStyle == null)
            {
                systemMessageStyle = CreateMessageStyle(new Color(0.5f, 0.5f, 0.2f, 0.2f));
                systemMessageStyle.alignment = TextAnchor.MiddleCenter;
                systemMessageStyle.fontStyle = FontStyle.Italic;
            }

            if (codeBlockStyle == null)
            {
                codeBlockStyle = new GUIStyle(EditorStyles.textArea)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 11),
                    padding = new RectOffset(10, 10, 10, 10)
                };
                codeBlockStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            }
        }

        private GUIStyle CreateMessageStyle(Color backgroundColor)
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true,
                richText = true
            };
            style.normal.background = MakeTex(2, 2, backgroundColor);
            return style;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
