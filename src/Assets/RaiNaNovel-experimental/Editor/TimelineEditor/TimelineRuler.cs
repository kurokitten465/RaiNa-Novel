#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VNFramework.Editor.TimelineEditor
{
    /// <summary>
    /// Draws the time ruler bar at the top of the timeline panel.
    ///
    /// Responsibilities
    /// ────────────────
    /// • Tick marks at adaptive intervals (0.5s, 1s, 2s, 5s … based on zoom).
    /// • Time labels (seconds).
    /// • Playhead drag: click/drag anywhere on the ruler to scrub time.
    /// • Returns the new playhead time if scrubbing occurred.
    /// </summary>
    internal static class TimelineRuler
    {
        private static readonly Color RulerBg      = new Color(0.12f, 0.12f, 0.12f);
        private static readonly Color TickMajor    = new Color(0.6f,  0.6f,  0.6f);
        private static readonly Color TickMinor    = new Color(0.35f, 0.35f, 0.35f);
        private static readonly Color PlayheadCol  = new Color(1f,    0.35f, 0.35f);
        private static readonly Color PlayheadLine = new Color(1f,    0.35f, 0.35f, 0.6f);

        /// <summary>
        /// Draw the ruler inside <paramref name="rulerRect"/> using state from <paramref name="s"/>.
        /// Handles mouse input for scrubbing. Mutates <c>s.PlayheadTime</c> and
        /// <c>s.IsScrubbing</c> directly. Returns true if playhead moved.
        /// </summary>
        public static bool Draw(Rect rulerRect, TimelineEditorState s, float totalDuration)
        {
            EditorGUI.DrawRect(rulerRect, RulerBg);

            DrawTicks(rulerRect, s);
            DrawPlayhead(rulerRect, s, totalDuration);

            return HandleInput(rulerRect, s, totalDuration);
        }

        // ── Ticks ──────────────────────────────────────────────────────────────

        private static void DrawTicks(Rect rulerRect, TimelineEditorState s)
        {
            float interval = ChooseInterval(s.PixelsPerSecond);
            float startTime = Mathf.Floor(s.XToTime(rulerRect.x) / interval) * interval;
            float endTime   = s.XToTime(rulerRect.xMax);

            for (float t = startTime; t <= endTime + interval; t += interval)
            {
                if (t < 0f) continue;

                float x = s.TimeToX(t);
                if (x < rulerRect.x || x > rulerRect.xMax) continue;

                bool isMajor = IsNearWholeSecond(t);
                float tickH  = isMajor ? 10f : 5f;
                var tickCol  = isMajor ? TickMajor : TickMinor;

                EditorGUI.DrawRect(new Rect(x, rulerRect.yMax - tickH, 1f, tickH), tickCol);

                if (isMajor)
                {
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = TickMajor },
                        alignment = TextAnchor.UpperLeft
                    };
                    GUI.Label(new Rect(x + 2f, rulerRect.y + 1f, 40f, 14f),
                              FormatTime(t), labelStyle);
                }
            }
        }

        private static float ChooseInterval(float pps)
        {
            // Pick the smallest interval that still gives ≥ 30px between ticks.
            float[] candidates = { 0.1f, 0.25f, 0.5f, 1f, 2f, 5f, 10f, 30f };
            foreach (var c in candidates)
                if (c * pps >= 30f) return c;
            return 30f;
        }

        private static bool IsNearWholeSecond(float t) =>
            Mathf.Abs(t - Mathf.Round(t)) < 0.001f;

        private static string FormatTime(float t)
        {
            int s = Mathf.FloorToInt(t);
            int ms = Mathf.RoundToInt((t - s) * 10);
            return ms == 0 ? $"{s}s" : $"{s}.{ms}s";
        }

        // ── Playhead ───────────────────────────────────────────────────────────

        private static void DrawPlayhead(Rect rulerRect, TimelineEditorState s, float totalDuration)
        {
            float x = s.TimeToX(s.PlayheadTime);
            if (x < rulerRect.x || x > rulerRect.xMax) return;

            // Diamond head on ruler
            float cy = rulerRect.yMax - 2f;
            DrawDiamond(x, cy, 5f, PlayheadCol);

            // Vertical line extending down through tracks
            var lineRect = new Rect(x, rulerRect.y, 1f, rulerRect.height);
            EditorGUI.DrawRect(lineRect, PlayheadLine);
        }

        private static void DrawDiamond(float cx, float cy, float r, Color c)
        {
            // Approximate with a small square rotated — just draw a filled rect for MVP.
            EditorGUI.DrawRect(new Rect(cx - r * 0.5f, cy - r, r, r), c);
        }

        // ── Input ──────────────────────────────────────────────────────────────

        private static bool HandleInput(Rect rulerRect, TimelineEditorState s, float totalDuration)
        {
            var ev = Event.current;
            bool moved = false;

            if (ev.type == EventType.MouseDown && rulerRect.Contains(ev.mousePosition))
            {
                s.IsScrubbing = true;
                moved = SetPlayhead(ev.mousePosition.x, s, totalDuration);
                ev.Use();
            }

            if (s.IsScrubbing && ev.type == EventType.MouseDrag)
            {
                moved = SetPlayhead(ev.mousePosition.x, s, totalDuration);
                ev.Use();
            }

            if (ev.type == EventType.MouseUp)
                s.IsScrubbing = false;

            return moved;
        }

        private static bool SetPlayhead(float mouseX, TimelineEditorState s, float totalDuration)
        {
            float newTime = Mathf.Clamp(s.XToTime(mouseX), 0f, totalDuration);
            if (Mathf.Approximately(newTime, s.PlayheadTime)) return false;
            s.PlayheadTime = newTime;
            return true;
        }
    }
}
#endif
