#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using VNFramework.Runtime.Commands;

namespace VNFramework.Editor.TimelineEditor
{
    /// <summary>
    /// The right-hand Inspector panel of the Timeline Editor.
    ///
    /// Shows a focused view of the selected command's timing fields
    /// (startTime, duration) with direct numeric inputs, then delegates
    /// to the command's own SerializedObject for its custom fields.
    ///
    /// Deliberately thin: the bulk of per-command UI lives in the
    /// <see cref="BaseCommandEditor"/> hierarchy built in Phase 4.
    /// Here we only add the timing strip that is timeline-specific.
    /// </summary>
    internal static class TimelineInspectorPanel
    {
        private static SerializedObject _activeSO;
        private static BaseCommand      _activeCmd;

        /// <summary>
        /// Draw the inspector panel inside <paramref name="panelRect"/>.
        /// </summary>
        public static void Draw(Rect panelRect, TimelineEditorState s)
        {
            GUILayout.BeginArea(panelRect);

            if (s.SelectedCommand == null)
            {
                DrawEmpty();
                GUILayout.EndArea();
                return;
            }

            EnsureSO(s.SelectedCommand);

            DrawTimingStrip(s);
            ScriptEditorStyles.DrawHorizontalLine();
            DrawCommandFields();

            GUILayout.EndArea();
        }

        // ── Empty state ────────────────────────────────────────────────────────

        private static void DrawEmpty()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label("Select a command block\nto inspect its properties.",
                            new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                            { wordWrap = true });
            GUILayout.FlexibleSpace();
        }

        // ── Timing strip ───────────────────────────────────────────────────────

        private static void DrawTimingStrip(TimelineEditorState s)
        {
            var cmd = s.SelectedCommand;

            GUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Type badge
                var typeName = cmd.GetType().Name.Replace("Command", "");
                bool isInstant = cmd.ExecutionMode == CommandExecutionMode.Instant;

                GUI.color = isInstant
                    ? new Color(0.3f, 0.95f, 0.4f)
                    : new Color(1f, 0.65f, 0.2f);
                GUILayout.Label(typeName, EditorStyles.boldLabel);
                GUI.color = Color.white;

                GUILayout.FlexibleSpace();

                GUI.color = new Color(0.6f, 0.6f, 0.6f);
                GUILayout.Label(isInstant ? "INSTANT" : "AWAIT",
                                EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            GUILayout.Space(4f);
            ScriptEditorStyles.DrawHorizontalLine(0.2f);
            GUILayout.Space(4f);

            // Numeric timing fields — directly edit startTime and duration
            _activeSO.Update();

            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            float newStart = EditorGUILayout.FloatField("Start Time (s)",
                                                         cmd.startTime);
            float newDur   = EditorGUILayout.FloatField("Duration (s)",
                                                         cmd.duration);
            newStart = Mathf.Max(0f, newStart);
            newDur   = Mathf.Max(0.1f, newDur);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(cmd, "Edit Command Timing");
                cmd.startTime = newStart;
                cmd.duration  = newDur;
                EditorUtility.SetDirty(cmd);
            }

            // End-time read-only display
            GUI.enabled = false;
            EditorGUILayout.FloatField("End Time (s)", cmd.startTime + cmd.duration);
            GUI.enabled = true;

            GUILayout.Space(8f);
        }

        // ── Custom command fields ──────────────────────────────────────────────

        private static void DrawCommandFields()
        {
            if (_activeSO == null) return;

            _activeSO.Update();

            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            ScriptEditorStyles.DrawHorizontalLine(0.15f);
            GUILayout.Space(4f);

            // Draw all fields except the timeline metadata ones
            // (commandLabel, trackIndex, startTime, duration — shown in timing strip)
            var skip = new System.Collections.Generic.HashSet<string>
            {
                "m_Script", "commandLabel", "trackIndex", "startTime", "duration"
            };

            var iter = _activeSO.GetIterator();
            iter.NextVisible(true);

            while (iter.NextVisible(false))
            {
                if (skip.Contains(iter.name)) continue;
                EditorGUILayout.PropertyField(iter, true);
            }

            // commandLabel at the bottom for convenience
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("Label (Script Editor)", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(
                _activeSO.FindProperty("commandLabel"),
                GUIContent.none);

            _activeSO.ApplyModifiedProperties();
        }

        // ── SO cache ───────────────────────────────────────────────────────────

        private static void EnsureSO(BaseCommand cmd)
        {
            if (_activeCmd != cmd || _activeSO == null)
            {
                _activeCmd = cmd;
                _activeSO  = new SerializedObject(cmd);
            }
        }
    }
}
#endif
