using UnityEngine;
using System.Collections.Generic;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Generates rounded rectangle textures using SDF (Signed Distance Field) algorithm
    /// Includes caching for performance optimization
    /// </summary>
    public static class RoundedRectTexture
    {
        private static Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        /// <summary>
        /// Create a rounded rectangle texture with anti-aliasing
        /// </summary>
        public static Texture2D Create(int width, int height, float radius, Color color, Color? borderColor = null, float borderWidth = 0f)
        {
            string cacheKey = $"{width}_{height}_{radius}_{color}_{borderColor}_{borderWidth}";

            if (textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];

            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            // Clamp radius to half of smallest dimension
            radius = Mathf.Min(radius, Mathf.Min(halfW, halfH));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = CalculateSDF(x, y, width, height, radius);

                    Color pixelColor = color;

                    // Apply border if specified
                    if (borderColor.HasValue && borderWidth > 0)
                    {
                        float innerAlpha = CalculateSDF(x, y, width, height, radius - borderWidth);
                        float borderAlpha = Mathf.Clamp01(alpha - innerAlpha);

                        pixelColor = Color.Lerp(color, borderColor.Value, borderAlpha);
                    }

                    pixelColor.a *= alpha;
                    pixels[y * width + x] = pixelColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            textureCache[cacheKey] = texture;

            return texture;
        }

        /// <summary>
        /// Calculate SDF value for rounded rectangle
        /// Returns alpha value 0-1 for anti-aliasing
        /// </summary>
        private static float CalculateSDF(int x, int y, int width, int height, float radius)
        {
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;

            // Transform to center-origin coordinates
            float px = x - halfW + 0.5f;
            float py = y - halfH + 0.5f;

            // Calculate distance to rounded rectangle edge
            float dx = Mathf.Max(Mathf.Abs(px) - (halfW - radius), 0);
            float dy = Mathf.Max(Mathf.Abs(py) - (halfH - radius), 0);
            float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;

            // Anti-aliased edge (smooth over 1.5 pixels)
            float alpha = Mathf.Clamp01(-dist + 0.75f);

            return alpha;
        }

        /// <summary>
        /// Create a chat bubble texture with optional tail
        /// </summary>
        public static Texture2D CreateBubble(int width, int height, float radius, Color color, bool tailOnRight = false, bool showTail = true)
        {
            string cacheKey = $"bubble_{width}_{height}_{radius}_{color}_{tailOnRight}_{showTail}";

            if (textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            int tailSize = showTail ? 8 : 0;
            int totalWidth = width + tailSize;

            Texture2D texture = new Texture2D(totalWidth, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[totalWidth * height];

            // Fill with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            // Draw main bubble
            int bubbleOffsetX = tailOnRight ? 0 : tailSize;
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(halfW, halfH));

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = CalculateSDF(x, y, width, height, radius);
                    Color pixelColor = color;
                    pixelColor.a *= alpha;
                    pixels[y * totalWidth + (x + bubbleOffsetX)] = pixelColor;
                }
            }

            // Draw tail triangle
            if (showTail)
            {
                int tailY = height / 3; // Position tail at 1/3 from bottom
                int tailHeight = 12;

                for (int ty = 0; ty < tailHeight; ty++)
                {
                    int y = tailY + ty;
                    if (y >= height) break;

                    float progress = (float)ty / tailHeight;
                    int tailWidth = (int)(tailSize * (1f - progress));

                    for (int tx = 0; tx < tailWidth; tx++)
                    {
                        int x = tailOnRight ? (width + tx) : (tailSize - 1 - tx);
                        if (x >= 0 && x < totalWidth)
                        {
                            // Smooth edge
                            float edgeAlpha = 1f - Mathf.Abs(tx - tailWidth * 0.5f) / (tailWidth * 0.5f + 1);
                            Color pixelColor = color;
                            pixelColor.a *= Mathf.Clamp01(edgeAlpha * 2f);

                            // Blend with existing
                            Color existing = pixels[y * totalWidth + x];
                            pixels[y * totalWidth + x] = Color.Lerp(existing, pixelColor, pixelColor.a);
                        }
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            textureCache[cacheKey] = texture;

            return texture;
        }

        /// <summary>
        /// Create a simple solid color texture
        /// </summary>
        public static Texture2D CreateSolid(int width, int height, Color color)
        {
            string cacheKey = $"solid_{width}_{height}_{color}";

            if (textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();

            textureCache[cacheKey] = texture;

            return texture;
        }

        /// <summary>
        /// Create a gradient texture
        /// </summary>
        public static Texture2D CreateGradient(int width, int height, Color topColor, Color bottomColor, float radius = 0)
        {
            string cacheKey = $"gradient_{width}_{height}_{topColor}_{bottomColor}_{radius}";

            if (textureCache.TryGetValue(cacheKey, out Texture2D cached) && cached != null)
            {
                return cached;
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color rowColor = Color.Lerp(bottomColor, topColor, t);

                for (int x = 0; x < width; x++)
                {
                    if (radius > 0)
                    {
                        float alpha = CalculateSDF(x, y, width, height, radius);
                        rowColor.a *= alpha;
                    }
                    pixels[y * width + x] = rowColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            textureCache[cacheKey] = texture;

            return texture;
        }

        /// <summary>
        /// Clear texture cache
        /// </summary>
        public static void ClearCache()
        {
            foreach (var texture in textureCache.Values)
            {
                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }
            }
            textureCache.Clear();
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public static string GetCacheStats()
        {
            return $"Texture cache: {textureCache.Count} entries";
        }
    }
}
