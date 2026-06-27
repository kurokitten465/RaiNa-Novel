using System;
using System.Collections.Generic;
using UnityEngine;

namespace VNFramework.Runtime.Core
{
    /// <summary>
    /// A named track (lane) on the timeline.
    ///
    /// Tracks are a logical grouping used by the TimelineEditor — they do NOT
    /// change execution order, which is always determined by <see cref="BaseCommand.startTime"/>.
    ///
    /// Design convention (not enforced at runtime):
    ///   Track 0  — Dialogue / Choice
    ///   Track 1  — Characters
    ///   Track 2  — Background
    ///   Track 3  — Audio
    ///   Track n  — custom groupings added by designers
    /// </summary>
    [Serializable]
    public class VNTrack
    {
        [Tooltip("Human-readable name shown as the track header in the TimelineEditor.")]
        public string trackName = "Track";

        [Tooltip("Track color used for command blocks in the TimelineEditor.")]
        public Color trackColor = Color.white;

        [Tooltip("Hide this track's blocks in the TimelineEditor (does NOT skip execution).")]
        public bool isVisible = true;

        /// <summary>
        /// Ordered list of commands that belong to this track.
        /// Order here is for editor display only; runtime sorts by startTime.
        /// </summary>
        public List<Commands.BaseCommand> commands = new();
    }
}
