#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Editor
{
    /// <summary>
    /// Primary Script Editor window.
    ///
    /// Layout
    /// ──────
    ///  ┌─ Toolbar ──────────────────────────────────────────────────┐
    ///  ├─ Left panel (command list) ──┬─ Right panel (inspector) ──┤
    ///  │  Track headers + rows        │  Per-command inspector      │
    ///  │  Search bar                  │  (SerializedObject fields)  │
    ///  └──────────────────────────────┴────────────────────────────┘
    ///
    /// Responsibilities
    /// ────────────────
    /// • Load / display any VNTimeline asset.
    /// • Add, remove, duplicate, reorder commands within tracks.
    /// • Group commands (collapse / expand track).
    /// • Search commands by label or type name.
    /// • Full Undo / Redo via SerializedObject + Undo.RecordObject.
    /// • Notify the runtime VNPlayer of edits (partial graph rebuild).
    ///
    /// Editor-only — no runtime impact.
    /// Reflection is allowed here (editor only per project rules).
    /// </summary>
    public sealed class ScriptEditorWindow : EditorWindow
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const float LEFT_PANEL_MIN  = 280f;
        private const float LEFT_PANEL_MAX  = 600f;
        private const float ROW_HEIGHT      = 26f;
        private const float TRACK_HEADER_H  = 22f;
        private const float DRAG_HANDLE_W   = 12f;
        private const string PREF_LEFT_W    = "VNScriptEditor_LeftWidth";

        // ── State ──────────────────────────────────────────────────────────────
        private VNTimeline          _timeline;
        private SerializedObject    _timelineSO;
        private BaseCommand         _selectedCommand;
        private SerializedObject    _selectedCmdSO;

        private string  _searchQuery  = string.Empty;
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private float   _leftPanelWidth;
        private bool    _resizingPanel;

        // Per-track collapse state (not serialized — resets on reopen, which is fine)
        private readonly Dictionary<int, bool> _trackCollapsed = new();

        // Drag-reorder state
        private BaseCommand _dragCmd;
        private int         _dragFromTrack;
        private int         _dragFromIndex;
        private int         _dropTrack  = -1;
        private int         _dropIndex  = -1;

        // ── Menu entry ─────────────────────────────────────────────────────────

        [MenuItem("VN Framework/Script Editor", priority = 10)]
        public static void Open()
        {
            var win = GetWindow<ScriptEditorWindow>("VN Script Editor");
            win.minSize = new Vector2(700f, 400f);
        }

        /// <summary>Open the editor pre-loaded with a specific timeline.</summary>
        public static void OpenWith(VNTimeline timeline)
        {
            var win = GetWindow<ScriptEditorWindow>("VN Script Editor");
            win.minSize = new Vector2(700f, 400f);
            win.LoadTimeline(timeline);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _leftPanelWidth = EditorPrefs.GetFloat(PREF_LEFT_W, 320f);
            Undo.undoRedoPerformed += OnUndoRedo;

            // Auto-load if selection is a VNTimeline.
            if (Selection.activeObject is VNTimeline tl)
                LoadTimeline(tl);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorPrefs.SetFloat(PREF_LEFT_W, _leftPanelWidth);
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
            if (_timeline != null)
            {
                _timelineSO?.Update();
                Repaint();
            }
        }

        // ── Timeline loading ───────────────────────────────────────────────────

        private void LoadTimeline(VNTimeline timeline)
        {
            _timeline      = timeline;
            _timelineSO    = timeline != null ? new SerializedObject(timeline) : null;
            _selectedCommand = null;
            _selectedCmdSO   = null;
            _trackCollapsed.Clear();
        }

        // ── GUI root ───────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawToolbar();

            if (_timeline == null)
            {
                DrawEmptyState();
                return;
            }

            _timelineSO.Update();

            // Horizontal split
            using (new EditorGUILayout.HorizontalScope())
            {
                // ── Left panel: command list ───────────────────────────────────
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(_leftPanelWidth)))
                {
                    DrawSearchBar();
                    _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                    DrawTrackList();
                    EditorGUILayout.EndScrollView();
                    DrawAddCommandBar();
                }

                // ── Resize handle ──────────────────────────────────────────────
                DrawResizeHandle();

                // ── Right panel: inspector ─────────────────────────────────────
                using (new EditorGUILayout.VerticalScope())
                {
                    _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                    DrawInspector();
                    EditorGUILayout.EndScrollView();
                }
            }

            _timelineSO.ApplyModifiedProperties();

            // Finalize drag
            if (Event.current.type == EventType.MouseUp)
                FinalizeDrag();
        }

        // ── Toolbar ────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // Timeline picker
                EditorGUI.BeginChangeCheck();
                var picked = (VNTimeline)EditorGUILayout.ObjectField(
                    _timeline, typeof(VNTimeline), false,
                    GUILayout.Width(220f));
                if (EditorGUI.EndChangeCheck())
                    LoadTimeline(picked);

                GUILayout.FlexibleSpace();

                if (_timeline == null) return;

                // Scene name
                GUI.color = Color.cyan * 1.2f;
                GUILayout.Label(_timeline.sceneName, EditorStyles.toolbarButton);
                GUI.color = Color.white;

                GUILayout.Space(8f);

                // Add track
                if (GUILayout.Button("+ Track", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    AddTrack();

                // Collapse all
                if (GUILayout.Button("⊟ All", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    SetAllCollapsed(true);

                // Expand all
                if (GUILayout.Button("⊞ All", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    SetAllCollapsed(false);

                GUILayout.Space(4f);

                // Command count badge
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label($"{_timeline.TotalCommandCount} cmd(s)", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }
        }

        // ── Search bar ─────────────────────────────────────────────────────────

        private void DrawSearchBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUI.SetNextControlName("SearchField");
                _searchQuery = EditorGUILayout.TextField(
                    _searchQuery, EditorStyles.toolbarSearchField);

                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20f)))
                    _searchQuery = string.Empty;
            }
        }

        // ── Track list ─────────────────────────────────────────────────────────

        private void DrawTrackList()
        {
            if (_timeline.tracks == null) return;

            for (int t = 0; t < _timeline.tracks.Count; t++)
            {
                var track = _timeline.tracks[t];
                if (track == null) continue;

                DrawTrackHeader(track, t);

                bool collapsed = _trackCollapsed.TryGetValue(t, out var c) && c;
                if (!collapsed)
                    DrawTrackCommands(track, t);
            }

            // Drop zone at the very bottom (new track drop)
            HandleDropZone(-1, _timeline.tracks.Count, new Rect(0,
                GUILayoutUtility.GetLastRect().yMax, _leftPanelWidth, ROW_HEIGHT));
        }

        private void DrawTrackHeader(VNTrack track, int trackIdx)
        {
            bool collapsed = _trackCollapsed.TryGetValue(trackIdx, out var c) && c;

            var headerRect = EditorGUILayout.GetControlRect(false, TRACK_HEADER_H);
            EditorGUI.DrawRect(headerRect, track.trackColor * 0.45f);

            // Collapse toggle
            var toggleRect = new Rect(headerRect.x + 2f, headerRect.y + 3f, 16f, 16f);
            bool newCollapsed = !EditorGUI.Toggle(toggleRect, !collapsed, EditorStyles.foldout);
            if (newCollapsed != collapsed)
                _trackCollapsed[trackIdx] = newCollapsed;

            // Track name (editable inline)
            var nameRect = new Rect(toggleRect.xMax + 4f, headerRect.y,
                                    headerRect.width - 130f, TRACK_HEADER_H);
            EditorGUI.BeginChangeCheck();
            string newName = EditorGUI.DelayedTextField(nameRect, track.trackName,
                                                        EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_timeline, "Rename Track");
                track.trackName = newName;
                EditorUtility.SetDirty(_timeline);
            }

            // Track color swatch
            var colorRect = new Rect(headerRect.xMax - 118f, headerRect.y + 2f, 18f, 18f);
            EditorGUI.BeginChangeCheck();
            Color newColor = EditorGUI.ColorField(colorRect, GUIContent.none, track.trackColor,
                                                  false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_timeline, "Change Track Color");
                track.trackColor = newColor;
                EditorUtility.SetDirty(_timeline);
            }

            // Visibility toggle
            var visRect = new Rect(colorRect.xMax + 4f, headerRect.y + 2f, 18f, 18f);
            bool newVis = EditorGUI.Toggle(visRect, track.isVisible);
            if (newVis != track.isVisible)
            {
                Undo.RecordObject(_timeline, "Toggle Track Visibility");
                track.isVisible = newVis;
                EditorUtility.SetDirty(_timeline);
            }

            // Remove track button
            var removeRect = new Rect(headerRect.xMax - 42f, headerRect.y + 2f, 38f, 18f);
            if (GUI.Button(removeRect, "✕ trk", EditorStyles.miniButton))
                RemoveTrack(trackIdx);
        }

        private void DrawTrackCommands(VNTrack track, int trackIdx)
        {
            bool hasFilter = !string.IsNullOrEmpty(_searchQuery);

            for (int i = 0; i < track.commands.Count; i++)
            {
                var cmd = track.commands[i];
                if (cmd == null) continue;

                // Search filter
                if (hasFilter)
                {
                    bool match = cmd.EditorSummary.IndexOf(_searchQuery,
                                     StringComparison.OrdinalIgnoreCase) >= 0
                              || cmd.GetType().Name.IndexOf(_searchQuery,
                                     StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                DrawCommandRow(cmd, track, trackIdx, i);
            }

            // Empty-track drop hint
            if (track.commands.Count == 0)
            {
                var hintRect = EditorGUILayout.GetControlRect(false, ROW_HEIGHT);
                EditorGUI.DrawRect(hintRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                GUI.Label(new Rect(hintRect.x + 8f, hintRect.y + 4f,
                                   hintRect.width, hintRect.height),
                          "— empty track —", EditorStyles.centeredGreyMiniLabel);
                HandleDropZone(trackIdx, 0, hintRect);
            }
        }

        private void DrawCommandRow(BaseCommand cmd, VNTrack track, int trackIdx, int cmdIdx)
        {
            var rowRect = EditorGUILayout.GetControlRect(false, ROW_HEIGHT);

            // Selection + hover highlight
            bool isSelected = cmd == _selectedCommand;
            Color rowBg = isSelected
                ? new Color(0.24f, 0.48f, 0.9f, 0.55f)
                : (cmdIdx % 2 == 0
                    ? new Color(0.18f, 0.18f, 0.18f)
                    : new Color(0.22f, 0.22f, 0.22f));
            EditorGUI.DrawRect(rowRect, rowBg);

            // Execution mode colour strip (left edge)
            var modeColor = cmd.ExecutionMode == CommandExecutionMode.Instant
                ? new Color(0.3f, 0.9f, 0.4f, 0.8f)
                : new Color(0.9f, 0.6f, 0.2f, 0.8f);
            EditorGUI.DrawRect(new Rect(rowRect.x, rowRect.y, 4f, rowRect.height), modeColor);

            // Drag handle
            var dragRect = new Rect(rowRect.x + 4f, rowRect.y + 4f, DRAG_HANDLE_W, rowRect.height - 8f);
            GUI.Label(dragRect, "≡", EditorStyles.centeredGreyMiniLabel);

            // Command type chip
            var typeName = cmd.GetType().Name.Replace("Command", "");
            var typeRect = new Rect(rowRect.x + 18f, rowRect.y + 4f, 88f, ROW_HEIGHT - 8f);
            EditorGUI.DrawRect(typeRect, track.trackColor * 0.6f);
            GUI.Label(typeRect, typeName, EditorStyles.centeredGreyMiniLabel);

            // Summary label
            var summaryRect = new Rect(typeRect.xMax + 6f, rowRect.y + 5f,
                                       rowRect.width - typeRect.width - 80f, ROW_HEIGHT - 8f);
            GUI.Label(summaryRect, cmd.EditorSummary, EditorStyles.label);

            // Row buttons (shown on hover / selection)
            if (isSelected || rowRect.Contains(Event.current.mousePosition))
            {
                DrawRowButtons(cmd, track, trackIdx, cmdIdx, rowRect);
            }

            // Click = select
            if (Event.current.type == EventType.MouseDown &&
                rowRect.Contains(Event.current.mousePosition))
            {
                SelectCommand(cmd);
                Event.current.Use();
            }

            // Drag start
            if (Event.current.type == EventType.MouseDrag &&
                dragRect.Contains(Event.current.mousePosition) &&
                _dragCmd == null)
            {
                _dragCmd       = cmd;
                _dragFromTrack = trackIdx;
                _dragFromIndex = cmdIdx;
                Event.current.Use();
            }

            // Drop zone
            HandleDropZone(trackIdx, cmdIdx, rowRect);
        }

        private void DrawRowButtons(BaseCommand cmd, VNTrack track,
                                    int trackIdx, int cmdIdx, Rect rowRect)
        {
            float btnW = 22f;
            float x    = rowRect.xMax - btnW - 2f;
            float y    = rowRect.y + 2f;
            float h    = ROW_HEIGHT - 4f;

            // Remove
            if (GUI.Button(new Rect(x, y, btnW, h), "✕", EditorStyles.miniButton))
            { RemoveCommand(track, trackIdx, cmdIdx); return; }
            x -= btnW + 2f;

            // Duplicate
            if (GUI.Button(new Rect(x, y, btnW, h), "⧉", EditorStyles.miniButton))
            { DuplicateCommand(track, trackIdx, cmd); return; }
            x -= btnW + 2f;

            // Move down
            if (cmdIdx < track.commands.Count - 1 &&
                GUI.Button(new Rect(x, y, btnW, h), "↓", EditorStyles.miniButton))
            { MoveCommand(track, trackIdx, cmdIdx, cmdIdx + 1); return; }
            x -= btnW + 2f;

            // Move up
            if (cmdIdx > 0 &&
                GUI.Button(new Rect(x, y, btnW, h), "↑", EditorStyles.miniButton))
            { MoveCommand(track, trackIdx, cmdIdx, cmdIdx - 1); return; }
        }

        // ── Drop zone ──────────────────────────────────────────────────────────

        private void HandleDropZone(int trackIdx, int insertIdx, Rect rect)
        {
            if (_dragCmd == null) return;

            if (rect.Contains(Event.current.mousePosition))
            {
                _dropTrack = trackIdx;
                _dropIndex = insertIdx;

                // Visual insertion line
                EditorGUI.DrawRect(
                    new Rect(rect.x, rect.y - 1f, rect.width, 2f),
                    Color.yellow);
            }
        }

        private void FinalizeDrag()
        {
            if (_dragCmd == null) return;

            if (_dropTrack >= 0 && _dropTrack < _timeline.tracks.Count)
            {
                var srcTrack = _timeline.tracks[_dragFromTrack];
                var dstTrack = _timeline.tracks[_dropTrack];

                Undo.RecordObject(_timeline, "Move Command");

                srcTrack.commands.RemoveAt(_dragFromIndex);

                int insertAt = Mathf.Clamp(_dropIndex, 0, dstTrack.commands.Count);
                if (srcTrack == dstTrack && _dropIndex > _dragFromIndex)
                    insertAt--;

                dstTrack.commands.Insert(insertAt, _dragCmd);
                EditorUtility.SetDirty(_timeline);
            }

            _dragCmd   = null;
            _dropTrack = -1;
            _dropIndex = -1;
            Repaint();
        }

        // ── Add command bar ────────────────────────────────────────────────────

        private void DrawAddCommandBar()
        {
            EditorGUILayout.Space(2f);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Add to track:", EditorStyles.miniLabel, GUILayout.Width(72f));

                // Track picker
                var trackNames = new string[_timeline.tracks.Count];
                for (int i = 0; i < _timeline.tracks.Count; i++)
                    trackNames[i] = _timeline.tracks[i]?.trackName ?? $"Track {i}";

                if (_addTargetTrack >= _timeline.tracks.Count)
                    _addTargetTrack = 0;

                _addTargetTrack = EditorGUILayout.Popup(_addTargetTrack, trackNames,
                                                        EditorStyles.toolbarPopup,
                                                        GUILayout.Width(96f));
                GUILayout.Space(4f);

                if (GUILayout.Button("Dialogue",   EditorStyles.toolbarButton)) AddCommand<ShowDialogueCommand>(_addTargetTrack);
                if (GUILayout.Button("Choice",     EditorStyles.toolbarButton)) AddCommand<ChoiceCommand>(_addTargetTrack);
                if (GUILayout.Button("Background", EditorStyles.toolbarButton)) AddCommand<ShowBackgroundCommand>(_addTargetTrack);
                if (GUILayout.Button("ShowChar",   EditorStyles.toolbarButton)) AddCommand<ShowCharacterCommand>(_addTargetTrack);
                if (GUILayout.Button("HideChar",   EditorStyles.toolbarButton)) AddCommand<HideCharacterCommand>(_addTargetTrack);
                if (GUILayout.Button("BGM",        EditorStyles.toolbarButton)) AddCommand<PlayBGMCommand>(_addTargetTrack);
            }
        }

        private int _addTargetTrack = 0;

        // ── Inspector panel ────────────────────────────────────────────────────

        private void DrawInspector()
        {
            if (_selectedCommand == null)
            {
                GUILayout.Space(16f);
                GUILayout.Label("Select a command to inspect.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Header
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(_selectedCommand.GetType().Name.Replace("Command", ""),
                                EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                var modeLabel = _selectedCommand.ExecutionMode == CommandExecutionMode.Instant
                    ? "INSTANT" : "AWAIT";
                var modeCol = _selectedCommand.ExecutionMode == CommandExecutionMode.Instant
                    ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.9f, 0.6f, 0.2f);
                GUI.color = modeCol;
                GUILayout.Label(modeLabel, EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.Space(2f);
            ScriptEditorStyles.DrawHorizontalLine();
            EditorGUILayout.Space(4f);

            // Draw all serialized fields of the selected command via SerializedObject.
            // Reflection is allowed in the editor per project rules.
            _selectedCmdSO.Update();

            var iter = _selectedCmdSO.GetIterator();
            iter.NextVisible(true); // skip m_Script

            while (iter.NextVisible(false))
                EditorGUILayout.PropertyField(iter, true);

            _selectedCmdSO.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);

            // Ping asset button
            if (GUILayout.Button("Ping Asset", EditorStyles.miniButton))
                EditorGUIUtility.PingObject(_selectedCommand);
        }

        // ── Resize handle ──────────────────────────────────────────────────────

        private void DrawResizeHandle()
        {
            var handleRect = GUILayoutUtility.GetRect(6f, position.height,
                                                      GUILayout.Width(6f));
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);

            EditorGUI.DrawRect(handleRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            if (Event.current.type == EventType.MouseDown && handleRect.Contains(Event.current.mousePosition))
            { _resizingPanel = true; Event.current.Use(); }

            if (_resizingPanel && Event.current.type == EventType.MouseDrag)
            {
                _leftPanelWidth = Mathf.Clamp(Event.current.mousePosition.x,
                                              LEFT_PANEL_MIN, LEFT_PANEL_MAX);
                Repaint();
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
                _resizingPanel = false;
        }

        // ── Empty state ────────────────────────────────────────────────────────

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.Label("No Timeline selected.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(8f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Create New Timeline", GUILayout.Width(180f)))
                        CreateNewTimeline();
                    GUILayout.FlexibleSpace();
                }
            }
            GUILayout.FlexibleSpace();
        }

        // ── Operations ─────────────────────────────────────────────────────────

        private void SelectCommand(BaseCommand cmd)
        {
            _selectedCommand = cmd;
            _selectedCmdSO   = cmd != null ? new SerializedObject(cmd) : null;
        }

        private void AddCommand<T>(int trackIdx) where T : BaseCommand
        {
            if (trackIdx < 0 || trackIdx >= _timeline.tracks.Count) return;

            Undo.RecordObject(_timeline, $"Add {typeof(T).Name}");

            var cmd = ScriptableObject.CreateInstance<T>();
            cmd.name = typeof(T).Name;

            // Store as sub-asset of the timeline.
            AssetDatabase.AddObjectToAsset(cmd, _timeline);
            AssetDatabase.SaveAssets();

            _timeline.tracks[trackIdx].commands.Add(cmd);
            EditorUtility.SetDirty(_timeline);

            SelectCommand(cmd);
            Repaint();
        }

        private void RemoveCommand(VNTrack track, int trackIdx, int cmdIdx)
        {
            var cmd = track.commands[cmdIdx];
            if (cmd == null) return;

            Undo.RecordObject(_timeline, "Remove Command");

            if (_selectedCommand == cmd)
                SelectCommand(null);

            track.commands.RemoveAt(cmdIdx);
            AssetDatabase.RemoveObjectFromAsset(cmd);
            DestroyImmediate(cmd, true);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_timeline);
            Repaint();
        }

        private void DuplicateCommand(VNTrack track, int trackIdx, BaseCommand src)
        {
            Undo.RecordObject(_timeline, "Duplicate Command");

            var dupe = Instantiate(src);
            dupe.name = src.name;
            dupe.commandLabel = src.commandLabel + " (copy)";

            AssetDatabase.AddObjectToAsset(dupe, _timeline);
            AssetDatabase.SaveAssets();

            int insertAt = track.commands.IndexOf(src) + 1;
            track.commands.Insert(insertAt, dupe);
            EditorUtility.SetDirty(_timeline);

            SelectCommand(dupe);
            Repaint();
        }

        private void MoveCommand(VNTrack track, int trackIdx, int fromIdx, int toIdx)
        {
            Undo.RecordObject(_timeline, "Reorder Command");
            var cmd = track.commands[fromIdx];
            track.commands.RemoveAt(fromIdx);
            track.commands.Insert(toIdx, cmd);
            EditorUtility.SetDirty(_timeline);
            Repaint();
        }

        private void AddTrack()
        {
            Undo.RecordObject(_timeline, "Add Track");
            _timeline.tracks.Add(new VNTrack
            {
                trackName  = $"Track {_timeline.tracks.Count}",
                trackColor = Color.HSVToRGB(
                    (float)_timeline.tracks.Count / 8f % 1f, 0.5f, 0.8f)
            });
            EditorUtility.SetDirty(_timeline);
            Repaint();
        }

        private void RemoveTrack(int trackIdx)
        {
            if (!EditorUtility.DisplayDialog("Remove Track",
                    $"Remove track '{_timeline.tracks[trackIdx].trackName}' and all its commands?",
                    "Remove", "Cancel")) return;

            Undo.RecordObject(_timeline, "Remove Track");

            var track = _timeline.tracks[trackIdx];
            foreach (var cmd in track.commands)
            {
                if (cmd == null) continue;
                if (_selectedCommand == cmd) SelectCommand(null);
                AssetDatabase.RemoveObjectFromAsset(cmd);
                DestroyImmediate(cmd, true);
            }

            _timeline.tracks.RemoveAt(trackIdx);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(_timeline);
            Repaint();
        }

        private void SetAllCollapsed(bool collapsed)
        {
            for (int i = 0; i < _timeline.tracks.Count; i++)
                _trackCollapsed[i] = collapsed;
            Repaint();
        }

        private void CreateNewTimeline()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create VN Timeline", "Timeline_NewScene", "asset",
                "Choose a save location for the new VNTimeline asset.");

            if (string.IsNullOrEmpty(path)) return;

            var tl = ScriptableObject.CreateInstance<VNTimeline>();
            tl.sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            tl.InitDefaultTracks();

            AssetDatabase.CreateAsset(tl, path);
            AssetDatabase.SaveAssets();

            LoadTimeline(tl);
            Selection.activeObject = tl;
            Repaint();
        }
    }
}
#endif
