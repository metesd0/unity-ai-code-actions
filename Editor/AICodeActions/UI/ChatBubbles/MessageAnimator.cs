using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace AICodeActions.UI.ChatBubbles
{
    /// <summary>
    /// Handles smooth animations for chat messages
    /// Supports slide-in, fade-in, and hover effects
    /// </summary>
    public class MessageAnimator
    {
        private Dictionary<string, AnimationState> animations = new Dictionary<string, AnimationState>();
        private double lastUpdateTime;

        public class AnimationState
        {
            public float slideProgress = 0f;      // 0 = off-screen, 1 = in position
            public float fadeProgress = 0f;       // 0 = invisible, 1 = fully visible
            public float hoverProgress = 0f;      // 0 = normal, 1 = hovered
            public bool isHovered = false;
            public double startTime;
            public bool isComplete = false;

            // Computed values
            public float SlideOffset => (1f - ChatBubbleStyles.Animation.EaseOutCubic(slideProgress)) * 30f;
            public float Alpha => fadeProgress;
        }

        /// <summary>
        /// Get or create animation state for a message
        /// </summary>
        public AnimationState GetState(string messageId)
        {
            if (!animations.TryGetValue(messageId, out AnimationState state))
            {
                state = new AnimationState
                {
                    startTime = EditorApplication.timeSinceStartup
                };
                animations[messageId] = state;
            }
            return state;
        }

        /// <summary>
        /// Update all animations
        /// </summary>
        public bool Update()
        {
            bool needsRepaint = false;
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastUpdateTime);
            lastUpdateTime = currentTime;

            foreach (var kvp in animations)
            {
                var state = kvp.Value;

                if (state.isComplete)
                    continue;

                float elapsed = (float)(currentTime - state.startTime);

                // Slide-in animation
                if (state.slideProgress < 1f)
                {
                    state.slideProgress = Mathf.Clamp01(elapsed / ChatBubbleStyles.Animation.SlideInDuration);
                    needsRepaint = true;
                }

                // Fade-in animation (starts slightly after slide)
                float fadeDelay = 0.05f;
                if (elapsed > fadeDelay && state.fadeProgress < 1f)
                {
                    state.fadeProgress = Mathf.Clamp01((elapsed - fadeDelay) / ChatBubbleStyles.Animation.FadeInDuration);
                    needsRepaint = true;
                }

                // Hover transition
                float targetHover = state.isHovered ? 1f : 0f;
                if (Mathf.Abs(state.hoverProgress - targetHover) > 0.01f)
                {
                    float hoverSpeed = 1f / ChatBubbleStyles.Animation.HoverTransitionDuration;
                    state.hoverProgress = Mathf.MoveTowards(state.hoverProgress, targetHover, deltaTime * hoverSpeed);
                    needsRepaint = true;
                }

                // Check if animation is complete
                if (state.slideProgress >= 1f && state.fadeProgress >= 1f &&
                    Mathf.Abs(state.hoverProgress - targetHover) < 0.01f)
                {
                    state.isComplete = true;
                }
            }

            return needsRepaint;
        }

        /// <summary>
        /// Set hover state for a message
        /// </summary>
        public void SetHovered(string messageId, bool hovered)
        {
            var state = GetState(messageId);
            if (state.isHovered != hovered)
            {
                state.isHovered = hovered;
                state.isComplete = false; // Resume animation updates
            }
        }

        /// <summary>
        /// Reset animation for a message (for re-entry animations)
        /// </summary>
        public void ResetAnimation(string messageId)
        {
            if (animations.ContainsKey(messageId))
            {
                animations[messageId] = new AnimationState
                {
                    startTime = EditorApplication.timeSinceStartup
                };
            }
        }

        /// <summary>
        /// Remove animation state for a message
        /// </summary>
        public void RemoveState(string messageId)
        {
            animations.Remove(messageId);
        }

        /// <summary>
        /// Clear all animation states
        /// </summary>
        public void Clear()
        {
            animations.Clear();
        }

        /// <summary>
        /// Check if any animations are currently running
        /// </summary>
        public bool HasActiveAnimations()
        {
            foreach (var state in animations.Values)
            {
                if (!state.isComplete)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Apply animation matrix for drawing
        /// </summary>
        public void BeginAnimatedDraw(string messageId, Rect rect, bool isUserMessage)
        {
            var state = GetState(messageId);

            // Apply slide offset
            float slideX = state.SlideOffset * (isUserMessage ? 1f : -1f);

            // Store original matrix
            GUI.matrix = Matrix4x4.TRS(
                new Vector3(slideX, 0, 0),
                Quaternion.identity,
                Vector3.one
            );

            // Apply alpha
            GUI.color = new Color(1f, 1f, 1f, state.Alpha);
        }

        /// <summary>
        /// End animated draw (restore matrix)
        /// </summary>
        public void EndAnimatedDraw()
        {
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
        }

        /// <summary>
        /// Get hover blend color
        /// </summary>
        public Color GetHoverBlendColor(string messageId, Color normalColor, Color hoverColor)
        {
            var state = GetState(messageId);
            return Color.Lerp(normalColor, hoverColor, state.hoverProgress);
        }
    }
}
