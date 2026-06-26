using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Execution mode for a command.
    /// Instant  → fires and immediately advances the playhead.
    /// Await    → holds the playhead until an external condition resolves it.
    /// </summary>
    public enum CommandExecutionMode
    {
        Instant,
        Await
    }

    /// <summary>
    /// Abstract base for every VN command.
    /// Lives as a ScriptableObject so designers create commands
    /// as Unity assets with no code.
    ///
    /// Rules
    /// ─────
    /// • No Reflection at runtime — editor tools may use it, runtime must not.
    /// • Each subclass declares its own ExecutionMode.
    /// • Duration is a timeline hint only; Await commands ignore it at runtime.
    /// </summary>
    public abstract class BaseCommand : ScriptableObject
    {
        [Header("Timeline Metadata")]
        [Tooltip("Human-readable label shown in the Script Editor.")]
        public string commandLabel = string.Empty;

        [Tooltip("Track index this command lives on (used by TimelineEditor).")]
        public int trackIndex = 0;

        [Tooltip("Start time in seconds on the timeline (set by TimelineEditor).")]
        public float startTime = 0f;

        [Tooltip("Visual duration hint in seconds (Await commands extend until resolved).")]
        public float duration = 1f;

        // ── Execution ──────────────────────────────────────────────────────────

        /// <summary>Declared by each subclass — never changed at runtime.</summary>
        public abstract CommandExecutionMode ExecutionMode { get; }

        /// <summary>
        /// Called by the runtime player to execute this command.
        /// Instant commands complete synchronously.
        /// Await commands begin an async operation and call <paramref name="onComplete"/>
        /// when the player resolves the wait (tap, choice selected, etc.).
        /// </summary>
        /// <param name="context">Runtime services (UI refs, audio source, etc.).</param>
        /// <param name="onComplete">Invoke this when the command is done.</param>
        public abstract void Execute(ICommandContext context, System.Action onComplete);

        /// <summary>
        /// Optional: called when the playhead seeks over this command
        /// without executing it (TimelineEditor scrubbing).
        /// Default implementation is a no-op.
        /// </summary>
        public virtual void ApplyInstantState(ICommandContext context) { }

        // ── Editor helpers (editor-only; stripped from runtime builds) ─────────
#if UNITY_EDITOR
        /// <summary>
        /// Short description shown in the Script Editor list.
        /// Override to provide something more informative than the default.
        /// </summary>
        public virtual string EditorSummary =>
            string.IsNullOrEmpty(commandLabel) ? GetType().Name : commandLabel;
#endif
    }
}
