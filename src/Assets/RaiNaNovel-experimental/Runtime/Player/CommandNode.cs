using VNFramework.Runtime.Commands;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// Runtime execution node wrapping a single <see cref="BaseCommand"/>.
    ///
    /// The <see cref="VNPlayableGraph"/> builds one CommandNode per command
    /// and links them in a singly-linked list sorted by startTime.
    ///
    /// Why not use Unity's PlayableGraph API directly?
    /// ────────────────────────────────────────────────
    /// Unity's PlayableGraph is audio/animation-oriented. Our "graph" is a
    /// logical execution graph over ScriptableObject commands — we get the same
    /// conceptual benefits (play/pause/seek, partial rebuild) without fighting
    /// the Playables API to do things it was not designed for.
    ///
    /// The node is intentionally a plain class — no MonoBehaviour, no reflection.
    /// </summary>
    public sealed class CommandNode
    {
        // ── Identity ───────────────────────────────────────────────────────────
        public readonly BaseCommand Command;

        // ── Linked list ────────────────────────────────────────────────────────
        /// <summary>Next node in execution order (null if last).</summary>
        public CommandNode Next;

        // ── Per-run state (reset on graph rebuild or seek) ─────────────────────
        public NodeState State = NodeState.Pending;

        public CommandNode(BaseCommand command)
        {
            Command = command;
        }

        public void Reset() => State = NodeState.Pending;
    }

    public enum NodeState
    {
        Pending,    // not yet reached
        Running,    // Await command is executing
        Completed   // done
    }
}
