#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Editor.TimelineEditor
{
    /// <summary>
    /// Draws the scrollable track area: track headers on the left,
    /// command blocks (clips) on the right.
    ///
    /// Handles all mouse interactions:
    ///   • Click block          → select command
    ///   • Drag block body      → move in time (startTime) or across tracks
    ///   • Drag right edge      → resize duration
    ///   • Click empty track    → deselect
    ///
    /// Mutates <see cref="TimelineEditorState"/> and records Undo.
    /// Returns true when a repaint is needed.
    /// </summary>
    internal static class TimelineTrackArea
    {
        // ── Colours ────────────────────────────────────────────────────────────
        private static readonly Color TrackBgEven  = new Color(0.17f, 0.17f, 0.17f);
        private static readonly Color TrackBgOdd   = new Color(0.20f, 0.20f, 0.20f);
        private static readonly Color TrackBorder  = new Color(0.10f, 0.10f, 0.10f);
        private static readonly Color HeaderBg     = new Color(0.14f, 0.14f, 0.14f);
        private static readonly Color SelectOutline= new Color(1f,    0.85f, 0.2f);
        private static readonly Color ResizeHandle = new Color(1f,    1f,    1f,   0.35f);

        // ── Entry point ────────────────────────────────────────────────────────

        /// <summary>
        /// Draw the full track area inside <paramref name="areaRect"/>.
        /// Returns true if state changed and a repaint is needed.
        /// </summary>
        public static bool Draw(Rect areaRect, TimelineEditorState s, VNTimeline timeline)
        {
            bool dirty = false;

            if (timeline == null || timeline.tracks == null) return false;

            // Clip to the area so blocks don't bleed outside.
            GUI.BeginClip(areaRect);
            var localRect = new Rect(0, 0, areaRect.width, areaRect.height);

            for (int t = 0; t < timeline.tracks.Count; t++)
            {
                var track = timeline.tracks[t];
                if (track == null) continue;

                float trackTop = s.TrackTop(t);

                // Cull: skip tracks fully out of view
                if (trackTop + TimelineEditorState.TrackHeight < 0 ||
                    trackTop > localRect.height) continue;

                // Track background
                var bgColor = t % 2 == 0 ? TrackBgEven : TrackBgOdd;
                EditorGUI.DrawRect(
                    new Rect(TimelineEditorState.TrackHeaderW, trackTop,
                             localRect.width - TimelineEditorState.TrackHeaderW,
                             TimelineEditorState.TrackHeight),
                    bgColor);

                // Track header
                DrawTrackHeader(track, t, trackTop, areaRect.width, s, timeline, ref dirty);

                // Bottom border
                EditorGUI.DrawRect(
                    new Rect(0, trackTop + TimelineEditorState.TrackHeight - 1f,
                             localRect.width, 1f),
                    TrackBorder);

                // Command blocks
                if (track.commands != null)
                {
                    foreach (var cmd in track.commands)
                    {
                        if (cmd == null) continue;
                        DrawCommandBlock(cmd, t, trackTop, areaRect.width, s, timeline, ref dirty);
                    }
                }
            }

            // Playhead line on top of blocks
            DrawPlayheadLine(localRect, s);

            GUI.EndClip();

            // Handle drag/resize finalization outside clip
            dirty |= HandleMouseUp(s, timeline);

            return dirty;
        }

        // ── Track header ───────────────────────────────────────────────────────

        private static void DrawTrackHeader(VNTrack track, int trackIdx, float trackTop,
                                            float panelW, TimelineEditorState s,
                                            VNTimeline timeline, ref bool dirty)
        {
            var headerRect = new Rect(0, trackTop,
                                      TimelineEditorState.TrackHeaderW,
                                      TimelineEditorState.TrackHeight);
            EditorGUI.DrawRect(headerRect, HeaderBg);

            // Left colour strip
            EditorGUI.DrawRect(new Rect(0, trackTop, 4f,
                                        TimelineEditorState.TrackHeight),
                               track.trackColor);

            // Track name label (truncated)
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal  = { textColor = Color.white },
                padding = new RectOffset(8, 4, 0, 0),
                clipping = TextClipping.Clip
            };
            GUI.Label(new Rect(4f, trackTop + 4f,
                               TimelineEditorState.TrackHeaderW - 8f,
                               TimelineEditorState.TrackHeight - 8f),
                      track.trackName, labelStyle);
        }

        // ── Command block ──────────────────────────────────────────────────────

        private static void DrawCommandBlock(BaseCommand cmd, int trackIdx, float trackTop,
                                             float panelW, TimelineEditorState s,
                                             VNTimeline timeline, ref bool dirty)
        {
            float blockX = s.TimeToX(cmd.startTime);
            float blockW = Mathf.Max(TimelineEditorState.BlockMinW,
                                     cmd.duration * s.PixelsPerSecond);
            float blockY = trackTop + 3f;
            float blockH = TimelineEditorState.TrackHeight - 6f;

            var blockRect = new Rect(blockX, blockY, blockW, blockH);

            // Cull off-screen
            if (blockRect.xMax < TimelineEditorState.TrackHeaderW || blockX > panelW)
                return;

            // ── Body ──────────────────────────────────────────────────────────
            bool isSelected = cmd == s.SelectedCommand;
            bool isInstant  = cmd.ExecutionMode == CommandExecutionMode.Instant;

            // Base colour from track, adjusted for mode
            Color baseCol = Color.Lerp(timeline.tracks[trackIdx].trackColor,
                                       isInstant ? Color.green : Color.yellow, 0.2f);
            baseCol.a = isSelected ? 0.95f : 0.75f;

            EditorGUI.DrawRect(blockRect, baseCol * (isSelected ? 1.15f : 0.85f));

            // Mode stripe on left edge
            var stripeCol = isInstant
                ? new Color(0.3f, 1f, 0.4f, 0.9f)
                : new Color(1f,  0.65f, 0.2f, 0.9f);
            EditorGUI.DrawRect(new Rect(blockX, blockY, 3f, blockH), stripeCol);

            // Selection outline
            if (isSelected)
                DrawOutline(blockRect, SelectOutline, 2f);

            // Label (clipped inside block)
            if (blockW > 24f)
            {
                var typeName = cmd.GetType().Name.Replace("Command", "");
                var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal   = { textColor = Color.white },
                    clipping = TextClipping.Clip,
                    padding  = new RectOffset(5, 4, 2, 0)
                };
                GUI.Label(new Rect(blockX + 4f, blockY, blockW - 10f, blockH / 2f),
                          typeName, labelStyle);

                if (blockW > 60f)
                {
                    var summaryStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal   = { textColor = new Color(0.85f, 0.85f, 0.85f) },
                        clipping = TextClipping.Clip,
                        padding  = new RectOffset(5, 4, 0, 0),
                        fontSize = 9
                    };
                    GUI.Label(new Rect(blockX + 4f, blockY + blockH * 0.5f,
                                       blockW - 10f, blockH * 0.5f),
                              cmd.EditorSummary, summaryStyle);
                }
            }

            // ── Resize handle (right edge) ────────────────────────────────────
            var resizeRect = new Rect(blockRect.xMax - TimelineEditorState.ResizeHandleW,
                                      blockY, TimelineEditorState.ResizeHandleW, blockH);
            EditorGUI.DrawRect(resizeRect, ResizeHandle);
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeHorizontal);

            // ── Mouse interaction ─────────────────────────────────────────────
            HandleBlockMouse(cmd, trackIdx, blockRect, resizeRect, s, timeline, ref dirty);
        }

        private static void HandleBlockMouse(BaseCommand cmd, int trackIdx,
                                             Rect blockRect, Rect resizeRect,
                                             TimelineEditorState s, VNTimeline timeline,
                                             ref bool dirty)
        {
            var ev = Event.current;
            if (ev.type != EventType.MouseDown) return;
            if (!blockRect.Contains(ev.mousePosition)) return;

            // Select
            s.SelectedCommand = cmd;
            dirty = true;

            if (resizeRect.Contains(ev.mousePosition))
            {
                // Begin resize
                s.ResizeCommand        = cmd;
                s.ResizeStartMouseX    = ev.mousePosition.x;
                s.ResizeStartDuration  = cmd.duration;
            }
            else
            {
                // Begin drag
                s.DragCommand     = cmd;
                s.DragSourceTrack = trackIdx;
                s.DragOffsetX     = ev.mousePosition.x - s.TimeToX(cmd.startTime);
            }

            ev.Use();
        }

        // ── Drag / resize update (called per MouseDrag event) ─────────────────

        public static bool HandleMouseDrag(TimelineEditorState s, VNTimeline timeline,
                                           float mouseX, float mouseY)
        {
            bool dirty = false;

            if (s.IsDragging)
            {
                float newTime = Mathf.Max(0f, s.XToTime(mouseX - s.DragOffsetX));
                // Snap to 0.1s grid
                newTime = Mathf.Round(newTime * 10f) / 10f;

                if (!Mathf.Approximately(newTime, s.DragCommand.startTime))
                {
                    Undo.RecordObject(s.DragCommand, "Move Command");
                    s.DragCommand.startTime = newTime;
                    EditorUtility.SetDirty(s.DragCommand);
                    dirty = true;
                }

                // Detect track change
                int newTrack = Mathf.Clamp(
                    Mathf.FloorToInt((mouseY - TimelineEditorState.RulerHeight + s.ScrollY)
                                     / TimelineEditorState.TrackHeight),
                    0, timeline.tracks.Count - 1);

                if (newTrack != s.DragSourceTrack)
                {
                    var srcTrack = timeline.tracks[s.DragSourceTrack];
                    var dstTrack = timeline.tracks[newTrack];

                    Undo.RecordObject(timeline, "Move Command to Track");
                    srcTrack.commands.Remove(s.DragCommand);
                    dstTrack.commands.Add(s.DragCommand);
                    s.DragCommand.trackIndex = newTrack;
                    s.DragSourceTrack = newTrack;
                    EditorUtility.SetDirty(timeline);
                    dirty = true;
                }
            }

            if (s.IsResizing)
            {
                float delta   = (mouseX - s.ResizeStartMouseX) / s.PixelsPerSecond;
                float newDur  = Mathf.Max(0.1f, s.ResizeStartDuration + delta);
                newDur        = Mathf.Round(newDur * 10f) / 10f;

                if (!Mathf.Approximately(newDur, s.ResizeCommand.duration))
                {
                    Undo.RecordObject(s.ResizeCommand, "Resize Command");
                    s.ResizeCommand.duration = newDur;
                    EditorUtility.SetDirty(s.ResizeCommand);
                    dirty = true;
                }
            }

            return dirty;
        }

        private static bool HandleMouseUp(TimelineEditorState s, VNTimeline timeline)
        {
            var ev = Event.current;
            if (ev.type != EventType.MouseUp) return false;

            bool wasDirty = s.IsDragging || s.IsResizing;
            s.ClearDrag();
            s.ClearResize();
            return wasDirty;
        }

        // ── Playhead vertical line ─────────────────────────────────────────────

        private static void DrawPlayheadLine(Rect localRect, TimelineEditorState s)
        {
            float x = s.TimeToX(s.PlayheadTime);
            if (x < TimelineEditorState.TrackHeaderW || x > localRect.width) return;

            EditorGUI.DrawRect(
                new Rect(x, 0, 1f, localRect.height),
                new Color(1f, 0.35f, 0.35f, 0.85f));
        }

        // ── Outline helper ─────────────────────────────────────────────────────

        private static void DrawOutline(Rect r, Color c, float t)
        {
            EditorGUI.DrawRect(new Rect(r.x,           r.y,            r.width, t),     c);
            EditorGUI.DrawRect(new Rect(r.x,           r.yMax - t,     r.width, t),     c);
            EditorGUI.DrawRect(new Rect(r.x,           r.y,            t, r.height),    c);
            EditorGUI.DrawRect(new Rect(r.xMax - t,    r.y,            t, r.height),    c);
        }
    }
}
#endif
