#if UNITY_EDITOR
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Editor.TimelineEditor
{
    /// <summary>
    /// All mutable editor state for the Timeline Editor window.
    /// Kept in one place so the window class stays focused on drawing.
    ///
    /// Not serialized — resets on domain reload, which is intentional.
    /// Persistent prefs (zoom, scroll) use EditorPrefs via the window.
    /// </summary>
    internal sealed class TimelineEditorState
    {
        // ── Timeline ───────────────────────────────────────────────────────────
        public VNTimeline Timeline;

        // ── View ───────────────────────────────────────────────────────────────
        /// <summary>Pixels per second on the timeline ruler.</summary>
        public float PixelsPerSecond = 80f;

        /// <summary>Horizontal scroll offset in pixels.</summary>
        public float ScrollX = 0f;

        /// <summary>Vertical scroll for the track area.</summary>
        public float ScrollY = 0f;

        // ── Playhead ───────────────────────────────────────────────────────────
        /// <summary>Playhead position in seconds.</summary>
        public float PlayheadTime = 0f;

        /// <summary>True while the user is scrubbing the playhead by dragging.</summary>
        public bool IsScrubbing = false;

        // ── Selection ──────────────────────────────────────────────────────────
        /// <summary>Currently selected command block (null = none).</summary>
        public BaseCommand SelectedCommand;

        // ── Drag ───────────────────────────────────────────────────────────────
        public BaseCommand DragCommand;
        public int         DragSourceTrack  = -1;
        public float       DragOffsetX      = 0f;   // mouse x offset from block left edge
        public bool        IsDragging       => DragCommand != null;

        // ── Resize ─────────────────────────────────────────────────────────────
        public BaseCommand ResizeCommand;
        public bool        IsResizing       => ResizeCommand != null;
        public float       ResizeStartMouseX;
        public float       ResizeStartDuration;

        // ── Layout constants ───────────────────────────────────────────────────
        public const float RulerHeight     = 24f;
        public const float TrackHeaderW    = 120f;
        public const float TrackHeight     = 40f;
        public const float BlockMinW       = 6f;
        public const float ResizeHandleW   = 8f;
        public const float MinPixelsPerSec = 20f;
        public const float MaxPixelsPerSec = 400f;

        // ── Helpers ────────────────────────────────────────────────────────────

        public float TimeToX(float time)  => TrackHeaderW + time * PixelsPerSecond - ScrollX;
        public float XToTime(float x)     => (x - TrackHeaderW + ScrollX) / PixelsPerSecond;

        public float TrackTop(int trackIdx) => RulerHeight + trackIdx * TrackHeight - ScrollY;

        /// <summary>Visible time range for culling off-screen blocks.</summary>
        public float VisibleTimeStart(float panelWidth) =>
            Mathf.Max(0f, XToTime(TrackHeaderW));
        public float VisibleTimeEnd(float panelWidth) =>
            XToTime(panelWidth);

        // ── Reset helpers ──────────────────────────────────────────────────────
        public void ClearDrag()
        {
            DragCommand     = null;
            DragSourceTrack = -1;
            DragOffsetX     = 0f;
        }

        public void ClearResize()
        {
            ResizeCommand       = null;
            ResizeStartMouseX   = 0f;
            ResizeStartDuration = 0f;
        }
    }
}
#endif
