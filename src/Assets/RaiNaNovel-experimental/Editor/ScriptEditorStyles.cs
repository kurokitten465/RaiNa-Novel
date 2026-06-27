#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace VNFramework.Editor
{
    /// <summary>
    /// Centralized GUIStyle cache and drawing helpers for Script Editor windows.
    ///
    /// Styles are lazy-initialized on first use (Unity requires GUI skin to be
    /// ready before creating GUIStyles, so we can't init in a static constructor).
    /// </summary>
    internal static class ScriptEditorStyles
    {
        // ── Cached styles ──────────────────────────────────────────────────────

        private static GUIStyle _commandRowNormal;
        private static GUIStyle _commandRowSelected;
        private static GUIStyle _trackHeaderStyle;
        private static GUIStyle _typePillStyle;
        private static GUIStyle _summaryLabelStyle;
        private static GUIStyle _sectionHeaderStyle;

        public static GUIStyle CommandRowNormal => _commandRowNormal ??= BuildCommandRow(false);
        public static GUIStyle CommandRowSelected => _commandRowSelected ??= BuildCommandRow(true);
        public static GUIStyle TrackHeader => _trackHeaderStyle ??= BuildTrackHeader();
        public static GUIStyle TypePill => _typePillStyle ??= BuildTypePill();
        public static GUIStyle SummaryLabel => _summaryLabelStyle ??= BuildSummaryLabel();
        public static GUIStyle SectionHeader => _sectionHeaderStyle ??= BuildSectionHeader();

        // ── Drawing helpers ────────────────────────────────────────────────────

        /// <summary>Draw a 1px horizontal separator line.</summary>
        public static void DrawHorizontalLine(float alpha = 0.25f)
        {
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, alpha));
        }

        /// <summary>
        /// Draw a colour badge with centred text (command type pill, mode tag, etc.).
        /// </summary>
        public static void DrawBadge(Rect rect, string text, Color bgColor, Color textColor)
        {
            EditorGUI.DrawRect(rect, bgColor);
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = textColor }
            };
            GUI.Label(rect, text, style);
        }

        /// <summary>
        /// Draw a coloured left-edge stripe on a command row to indicate mode.
        /// </summary>
        public static void DrawModeStripe(Rect rowRect, bool isInstant)
        {
            var c = isInstant
                ? new Color(0.3f, 0.9f, 0.4f, 0.85f)
                : new Color(0.9f, 0.6f, 0.2f, 0.85f);
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 4f, rowRect.height), c);
        }

        // ── Style builders ─────────────────────────────────────────────────────

        private static GUIStyle BuildCommandRow(bool selected)
        {
            var bg = selected
                ? MakeTex(new Color(0.24f, 0.48f, 0.9f, 0.4f))
                : MakeTex(new Color(0.2f,  0.2f,  0.2f, 1f));

            return new GUIStyle
            {
                normal    = { background = bg, textColor = Color.white },
                padding   = new RectOffset(6, 6, 3, 3),
                alignment = TextAnchor.MiddleLeft,
                fontSize  = 12,
                richText  = false
            };
        }

        private static GUIStyle BuildTrackHeader()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 11,
                padding   = new RectOffset(4, 4, 2, 2),
                normal    = { textColor = Color.white }
            };
        }

        private static GUIStyle BuildTypePill()
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                padding   = new RectOffset(4, 4, 1, 1)
            };
        }

        private static GUIStyle BuildSummaryLabel()
        {
            return new GUIStyle(EditorStyles.label)
            {
                fontSize  = 12,
                alignment = TextAnchor.MiddleLeft,
                clipping  = TextClipping.Clip
            };
        }

        private static GUIStyle BuildSectionHeader()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                padding   = new RectOffset(2, 2, 4, 4),
                normal    = { textColor = new Color(0.85f, 0.85f, 0.85f) }
            };
        }

        // ── Texture helper ─────────────────────────────────────────────────────

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
#endif
