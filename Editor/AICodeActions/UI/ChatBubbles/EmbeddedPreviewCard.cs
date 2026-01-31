using UnityEngine;
using UnityEditor;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Embedded preview cards for Scripts, Components, and GameObjects
    /// Shows compact visual previews within chat messages
    /// </summary>
    public class EmbeddedPreviewCard
    {
        public enum CardType
        {
            Script,
            Component,
            GameObject,
            Prefab,
            Asset
        }

        /// <summary>
        /// Draw a preview card for a script reference
        /// </summary>
        public static void DrawScriptCard(string scriptName, string scriptPath, bool canOpen = true)
        {
            Rect cardRect = EditorGUILayout.BeginVertical(GUILayout.Height(50));

            // Background
            GUI.DrawTexture(cardRect, RoundedRectTexture.Create(
                (int)cardRect.width,
                50,
                ChatBubbleStyles.Dimensions.CardRadius,
                ChatBubbleStyles.Colors.CardBg
            ));

            EditorGUILayout.BeginHorizontal();

            // Icon
            GUILayout.Space(8);
            GUILayout.Label("📄", GUILayout.Width(24), GUILayout.Height(40));

            // Info
            EditorGUILayout.BeginVertical();
            GUILayout.Space(4);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUILayout.Label(scriptName, titleStyle);

            var pathStyle = new GUIStyle(EditorStyles.miniLabel);
            pathStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUILayout.Label(TruncatePath(scriptPath, 40), pathStyle);

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Open button
            if (canOpen)
            {
                EditorGUILayout.BeginVertical();
                GUILayout.Space(12);
                if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    OpenScript(scriptPath);
                }
                GUILayout.Space(12);
                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw a preview card for a component
        /// </summary>
        public static void DrawComponentCard(string componentName, string description = null)
        {
            Rect cardRect = EditorGUILayout.BeginVertical(GUILayout.Height(45));

            GUI.DrawTexture(cardRect, RoundedRectTexture.Create(
                (int)cardRect.width,
                45,
                ChatBubbleStyles.Dimensions.CardRadius,
                ChatBubbleStyles.Colors.CardBg
            ));

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("🧩", GUILayout.Width(24), GUILayout.Height(35));

            EditorGUILayout.BeginVertical();
            GUILayout.Space(4);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            titleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUILayout.Label(componentName, titleStyle);

            if (!string.IsNullOrEmpty(description))
            {
                var descStyle = new GUIStyle(EditorStyles.miniLabel);
                descStyle.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
                GUILayout.Label(description, descStyle);
            }

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw a preview card for a GameObject
        /// </summary>
        public static void DrawGameObjectCard(string objectName, string[] components = null, bool isNew = true)
        {
            int height = 50 + (components != null ? Mathf.Min(components.Length, 3) * 16 : 0);

            Rect cardRect = EditorGUILayout.BeginVertical(GUILayout.Height(height));

            GUI.DrawTexture(cardRect, RoundedRectTexture.Create(
                (int)cardRect.width,
                height,
                ChatBubbleStyles.Dimensions.CardRadius,
                ChatBubbleStyles.Colors.CardBg
            ));

            EditorGUILayout.BeginHorizontal();

            GUILayout.Space(8);
            GUILayout.Label(isNew ? "🆕" : "🎮", GUILayout.Width(24), GUILayout.Height(40));

            EditorGUILayout.BeginVertical();
            GUILayout.Space(6);

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
            titleStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            GUILayout.Label(objectName, titleStyle);

            if (components != null && components.Length > 0)
            {
                var compStyle = new GUIStyle(EditorStyles.miniLabel);
                compStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);

                int showCount = Mathf.Min(components.Length, 3);
                for (int i = 0; i < showCount; i++)
                {
                    GUILayout.Label($"  • {components[i]}", compStyle);
                }

                if (components.Length > 3)
                {
                    GUILayout.Label($"  + {components.Length - 3} more...", compStyle);
                }
            }

            GUILayout.Space(6);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draw a compact inline preview
        /// </summary>
        public static void DrawInlinePreview(CardType type, string name)
        {
            string icon = type switch
            {
                CardType.Script => "📄",
                CardType.Component => "🧩",
                CardType.GameObject => "🎮",
                CardType.Prefab => "📦",
                CardType.Asset => "🗂",
                _ => "📎"
            };

            EditorGUILayout.BeginHorizontal();

            var bgRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label, GUILayout.Height(22));
            GUI.DrawTexture(bgRect, RoundedRectTexture.Create(
                (int)bgRect.width, 22, 4f, new Color(0.2f, 0.2f, 0.22f, 0.8f)
            ));

            Rect contentRect = new Rect(bgRect.x + 6, bgRect.y + 2, bgRect.width - 12, 18);
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUI.Label(contentRect, $"{icon} {name}", style);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a creation preview (shows what will be created)
        /// </summary>
        public static void DrawCreationPreview(string title, string[] items)
        {
            EditorGUILayout.BeginVertical();

            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            headerStyle.normal.textColor = new Color(0.7f, 0.85f, 0.7f);
            GUILayout.Label($"✨ {title}", headerStyle);

            // Items list
            var itemStyle = new GUIStyle(EditorStyles.miniLabel);
            itemStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            foreach (var item in items)
            {
                GUILayout.Label($"  → {item}", itemStyle);
            }

            EditorGUILayout.EndVertical();
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            return "..." + path.Substring(path.Length - maxLength + 3);
        }

        private static void OpenScript(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }
    }
}
