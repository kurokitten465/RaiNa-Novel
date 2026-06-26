using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VNFramework.Runtime.Commands;

namespace VNFramework.Runtime.Core
{
    /// <summary>
    /// The central asset for a VN scene or branch.
    ///
    /// A designer creates one VNTimeline per scene (or per choice branch).
    /// It contains an ordered list of tracks, each holding commands.
    ///
    /// Runtime access pattern
    /// ──────────────────────
    /// Use <see cref="GetCommandsSortedByTime"/> to get all commands in
    /// execution order.  The runtime player iterates this flat list.
    ///
    /// Editor access pattern
    /// ─────────────────────
    /// The ScriptEditor and TimelineEditor work with <see cref="tracks"/>
    /// directly to preserve lane grouping.
    /// </summary>
    [CreateAssetMenu(
        fileName = "Timeline_NewScene",
        menuName = "VN Framework/VN Timeline")]
    public class VNTimeline : ScriptableObject
    {
        [Header("Scene Info")]
        [Tooltip("Human-readable scene name (shown in editors and debug logs).")]
        public string sceneName = "New Scene";

        [Tooltip("Optional description / notes for the designer.")]
        [TextArea(2, 4)]
        public string notes = string.Empty;

        [Header("Tracks")]
        [Tooltip("All tracks in this timeline. Add tracks via the ScriptEditor.")]
        public List<VNTrack> tracks = new();

        // ── Runtime API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns every command across all tracks, sorted ascending by startTime.
        /// Commands with equal startTime are sorted by track index (stable order).
        /// This is the authoritative execution order for the runtime player.
        /// </summary>
        public IReadOnlyList<BaseCommand> GetCommandsSortedByTime()
        {
            var all = new List<(BaseCommand cmd, int trackIdx)>();

            for (int t = 0; t < tracks.Count; t++)
            {
                if (tracks[t]?.commands == null) continue;
                foreach (var cmd in tracks[t].commands)
                {
                    if (cmd != null)
                        all.Add((cmd, t));
                }
            }

            return all
                .OrderBy(pair => pair.cmd.startTime)
                .ThenBy(pair => pair.trackIdx)
                .Select(pair => pair.cmd)
                .ToList();
        }

        /// <summary>
        /// Total duration of the timeline in seconds.
        /// Equals the end time of the last command (startTime + duration).
        /// Returns 0 if the timeline is empty.
        /// </summary>
        public float TotalDuration
        {
            get
            {
                float max = 0f;
                foreach (var track in tracks)
                {
                    if (track?.commands == null) continue;
                    foreach (var cmd in track.commands)
                    {
                        if (cmd == null) continue;
                        float end = cmd.startTime + cmd.duration;
                        if (end > max) max = end;
                    }
                }
                return max;
            }
        }

        // ── Editor helpers ─────────────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>
        /// Ensures default tracks exist when the asset is first created.
        /// Called from a custom editor or reset context.
        /// </summary>
        public void InitDefaultTracks()
        {
            if (tracks.Count > 0) return;

            tracks.Add(new VNTrack { trackName = "Dialogue",   trackColor = new Color(0.4f, 0.7f, 1f) });
            tracks.Add(new VNTrack { trackName = "Characters",  trackColor = new Color(1f,  0.75f, 0.4f) });
            tracks.Add(new VNTrack { trackName = "Background", trackColor = new Color(0.5f, 1f,  0.5f) });
            tracks.Add(new VNTrack { trackName = "Audio",      trackColor = new Color(1f,  0.5f,  1f) });

            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>Total command count across all tracks (editor display).</summary>
        public int TotalCommandCount
        {
            get
            {
                int count = 0;
                foreach (var track in tracks)
                    if (track?.commands != null)
                        count += track.commands.Count;
                return count;
            }
        }
#endif
    }
}
