#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Core;
using VNFramework.Runtime.Player;

namespace VNFramework.Editor.TimelineEditor
{
    /// <summary>
    /// Timeline Editor — sub-editor for visual timing adjustment and seek preview.
    ///
    /// Layout
    /// ──────
    ///  ┌─ Toolbar (play/pause/seek, zoom, timeline picker) ──────────────────┐
    ///  ├─ Track area ────────────────────────────────────────────┬─ Inspector ┤
    ///  │  Ruler (time ticks + playhead)                          │  timing    │
    ///  │  Tracks with command blocks (drag/resize/select)        │  + fields  │
    ///  └─────────────────────────────────────────────────────────┴────────────┘
    ///
    /// Responsibilities
    /// ────────────────
    /// • Visual timing display — drag blocks to change startTime / duration.
    /// • Playhead scrub on ruler → VNPlayer.Seek() if player is in Play mode.
    /// • Zoom (scroll wheel / slider).
    /// • Horizontal / vertical scroll of the track area.
    /// • Per-command Inspector in the right panel.
    /// • Partial graph rebuild notification to VNPlayer after edits.
    ///
    /// Editor-only. Reflection allowed per project rules.
    /// </summary>
    public sealed class TimelineEditorWindow : EditorWindow
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const float INSPECTOR_W     = 280f;
        private const float MIN_WINDOW_W    = 600f;
        private const string PREF_ZOOM      = "VNTimeline_Zoom";
        private const string PREF_SCROLL_X  = "VNTimeline_ScrollX";

        // ── State ──────────────────────────────────────────────────────────────
        private readonly TimelineEditorState _s = new();

        // ── Menu ───────────────────────────────────────────────────────────────

        [MenuItem("VN Framework/Timeline Editor", priority = 11)]
        public static void Open()
        {
            var win = GetWindow<TimelineEditorWindow>("VN Timeline Editor");
            win.minSize = new Vector2(MIN_WINDOW_W, 300f);
        }

        public static void OpenWith(VNTimeline timeline)
        {
            var win = GetWindow<TimelineEditorWindow>("VN Timeline Editor");
            win.minSize = new Vector2(MIN_WINDOW_W, 300f);
            win.LoadTimeline(timeline);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _s.PixelsPerSecond = EditorPrefs.GetFloat(PREF_ZOOM,     80f);
            _s.ScrollX         = EditorPrefs.GetFloat(PREF_SCROLL_X, 0f);

            Undo.undoRedoPerformed += OnUndoRedo;

            if (Selection.activeObject is VNTimeline tl)
                LoadTimeline(tl);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorPrefs.SetFloat(PREF_ZOOM,     _s.PixelsPerSecond);
            EditorPrefs.SetFloat(PREF_SCROLL_X, _s.ScrollX);
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is VNTimeline tl)
            {
                LoadTimeline(tl);
                Repaint();
            }
        }

        private void OnUndoRedo()
        {
            Repaint();
        }

        private void LoadTimeline(VNTimeline timeline)
        {
            _s.Timeline         = timeline;
            _s.PlayheadTime     = 0f;
            _s.SelectedCommand  = null;
            _s.ScrollX          = 0f;
        }

        // ── GUI root ───────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawToolbar();

            if (_s.Timeline == null)
            {
                DrawEmptyState();
                return;
            }

            float totalW    = position.width;
            float totalH    = position.height - EditorStyles.toolbar.fixedHeight;
            float toolbarH  = EditorStyles.toolbar.fixedHeight;

            float trackAreaW = totalW - INSPECTOR_W;
            float trackAreaH = totalH;

            // ── Ruler ──────────────────────────────────────────────────────────
            var rulerRect = new Rect(0, toolbarH,
                                     trackAreaW, TimelineEditorState.RulerHeight);

            float totalDuration = Mathf.Max(10f, _s.Timeline.TotalDuration + 2f);
            bool playheadMoved = TimelineRuler.Draw(rulerRect, _s, totalDuration);

            if (playheadMoved)
                NotifyPlayerSeek();

            // ── Track area ─────────────────────────────────────────────────────
            var trackRect = new Rect(0,
                                     toolbarH + TimelineEditorState.RulerHeight,
                                     trackAreaW,
                                     trackAreaH - TimelineEditorState.RulerHeight);

            bool trackDirty = TimelineTrackArea.Draw(trackRect, _s, _s.Timeline);
            if (trackDirty)
            {
                NotifyPlayerEdited();
                Repaint();
            }

            // Handle drag/resize updates during MouseDrag
            if (Event.current.type == EventType.MouseDrag)
            {
                bool dragDirty = TimelineTrackArea.HandleMouseDrag(
                    _s, _s.Timeline,
                    Event.current.mousePosition.x,
                    Event.current.mousePosition.y - toolbarH - TimelineEditorState.RulerHeight);

                if (dragDirty)
                {
                    NotifyPlayerEdited();
                    Repaint();
                    Event.current.Use();
                }
            }

            // ── Vertical divider ───────────────────────────────────────────────
            EditorGUI.DrawRect(new Rect(trackAreaW, toolbarH, 1f, totalH),
                               new Color(0.08f, 0.08f, 0.08f));

            // ── Inspector panel ────────────────────────────────────────────────
            var inspectorRect = new Rect(trackAreaW + 1f, toolbarH,
                                         INSPECTOR_W - 1f, totalH);
            TimelineInspectorPanel.Draw(inspectorRect, _s);

            // ── Scroll + zoom ──────────────────────────────────────────────────
            HandleZoomAndScroll(trackRect, totalDuration);

            // Keep repainting while scrubbing so playhead trails smoothly
            if (_s.IsScrubbing || _s.IsDragging || _s.IsResizing)
                Repaint();
        }

        // ── Toolbar ────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Timeline picker
                EditorGUI.BeginChangeCheck();
                var picked = (VNTimeline)EditorGUILayout.ObjectField(
                    _s.Timeline, typeof(VNTimeline), false,
                    GUILayout.Width(200f));
                if (EditorGUI.EndChangeCheck())
                    LoadTimeline(picked);

                GUILayout.Space(8f);

                // Play/Pause in editor (only meaningful if VNPlayer is in scene)
                if (Application.isPlaying)
                {
                    DrawPlaybackControls();
                }
                else
                {
                    GUI.color = new Color(0.6f, 0.6f, 0.6f);
                    GUILayout.Label("(Enter Play Mode to preview)",
                                    EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }

                GUILayout.FlexibleSpace();

                // Playhead time display
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label($"▶ {_s.PlayheadTime:F2}s", EditorStyles.toolbarButton,
                                GUILayout.Width(72f));
                GUI.color = Color.white;

                // Zoom slider
                GUILayout.Label("Zoom", EditorStyles.miniLabel, GUILayout.Width(36f));
                _s.PixelsPerSecond = GUILayout.HorizontalSlider(
                    _s.PixelsPerSecond,
                    TimelineEditorState.MinPixelsPerSec,
                    TimelineEditorState.MaxPixelsPerSec,
                    GUILayout.Width(90f));

                // Reset zoom
                if (GUILayout.Button("1:1", EditorStyles.toolbarButton, GUILayout.Width(28f)))
                    _s.PixelsPerSecond = 80f;

                // Scroll to playhead
                if (GUILayout.Button("⊙", EditorStyles.toolbarButton, GUILayout.Width(22f)))
                    ScrollToPlayhead();
            }
        }

        private void DrawPlaybackControls()
        {
            var player = FindObjectOfType<VNPlayer>();
            if (player == null)
            {
                GUILayout.Label("No VNPlayer in scene", EditorStyles.miniLabel);
                return;
            }

            // Rewind
            if (GUILayout.Button("⏮", EditorStyles.toolbarButton, GUILayout.Width(24f)))
            {
                _s.PlayheadTime = 0f;
                player.Seek(0f);
            }

            // Play / Pause toggle
            bool isPlaying = player.State == PlaybackState.Playing;
            string playLabel = isPlaying ? "⏸" : "▶";

            if (GUILayout.Button(playLabel, EditorStyles.toolbarButton, GUILayout.Width(24f)))
            {
                if (isPlaying) player.Pause();
                else           player.Resume();
            }
        }

        // ── Zoom and scroll ────────────────────────────────────────────────────

        private void HandleZoomAndScroll(Rect trackRect, float totalDuration)
        {
            var ev = Event.current;
            if (!trackRect.Contains(ev.mousePosition)) return;

            if (ev.type == EventType.ScrollWheel)
            {
                if (ev.control || ev.command)
                {
                    // Ctrl+scroll = zoom around mouse
                    float mouseTime = _s.XToTime(ev.mousePosition.x);
                    _s.PixelsPerSecond = Mathf.Clamp(
                        _s.PixelsPerSecond * (ev.delta.y > 0 ? 0.9f : 1.1f),
                        TimelineEditorState.MinPixelsPerSec,
                        TimelineEditorState.MaxPixelsPerSec);
                    // Keep the time under mouse stable
                    _s.ScrollX = _s.TimeToX(mouseTime) - ev.mousePosition.x
                                 + TimelineEditorState.TrackHeaderW;
                }
                else
                {
                    // Horizontal scroll
                    _s.ScrollX = Mathf.Clamp(
                        _s.ScrollX + ev.delta.y * 20f,
                        0f, totalDuration * _s.PixelsPerSecond);
                }

                _s.ScrollY = Mathf.Clamp(
                    _s.ScrollY + ev.delta.x * 5f,
                    0f, Mathf.Max(0f,
                        _s.Timeline.tracks.Count * TimelineEditorState.TrackHeight
                        - trackRect.height));

                Repaint();
                ev.Use();
            }
        }

        // ── Runtime player bridge ──────────────────────────────────────────────

        private static VNPlayer _cachedPlayer;

        private static VNPlayer FindPlayer()
        {
            if (_cachedPlayer != null) return _cachedPlayer;
            _cachedPlayer = Object.FindObjectOfType<VNPlayer>();
            return _cachedPlayer;
        }

        private void NotifyPlayerSeek()
        {
            if (!Application.isPlaying) return;
            FindPlayer()?.Seek(_s.PlayheadTime);
        }

        private void NotifyPlayerEdited()
        {
            if (!Application.isPlaying) return;
            // Phase 2: partial rebuild from index 0 as a safe default.
            // A smarter version would track the minimum changed index.
            FindPlayer()?.NotifyTimelineEdited(0);
        }

        private void ScrollToPlayhead()
        {
            _s.ScrollX = Mathf.Max(0f,
                _s.PlayheadTime * _s.PixelsPerSecond
                - (position.width - INSPECTOR_W - TimelineEditorState.TrackHeaderW) * 0.3f);
            Repaint();
        }

        // ── Empty state ────────────────────────────────────────────────────────

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("No VNTimeline selected.\nSelect one in the Project window.",
                    new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true });
                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Open Script Editor", GUILayout.Width(160f)))
                        ScriptEditorWindow.Open();
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.FlexibleSpace();
        }
    }
}
#endif
