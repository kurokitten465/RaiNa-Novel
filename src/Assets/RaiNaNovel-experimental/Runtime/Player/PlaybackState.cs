namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// The current state of the VN runtime player.
    /// Transitions are managed exclusively by <see cref="VNPlayer"/>.
    ///
    ///  Idle ──► Playing ──► AwaitingInput
    ///             ▲               │
    ///             └───────────────┘  (onComplete from Await command)
    ///             │
    ///           Seeking  (scrub; returns to Idle/Playing when done)
    ///             │
    ///           Paused   (from Playing or AwaitingInput)
    ///             │
    ///           Finished (timeline end or null branch target)
    /// </summary>
    public enum PlaybackState
    {
        /// <summary>No timeline loaded; player is dormant.</summary>
        Idle,

        /// <summary>Advancing through Instant commands automatically.</summary>
        Playing,

        /// <summary>An Await command is active; playhead is held.</summary>
        AwaitingInput,

        /// <summary>Player is paused mid-timeline (e.g. editor pause button).</summary>
        Paused,

        /// <summary>Playhead is being repositioned without executing commands.</summary>
        Seeking,

        /// <summary>The timeline has reached its end or a null branch target.</summary>
        Finished
    }
}
