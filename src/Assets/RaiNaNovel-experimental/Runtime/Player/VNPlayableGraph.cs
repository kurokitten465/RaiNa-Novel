using System.Collections.Generic;
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// Builds and manages a logical execution graph from a <see cref="VNTimeline"/>.
    ///
    /// Responsibilities
    /// ────────────────
    /// • Build  — create a linked list of CommandNodes from the timeline's sorted commands.
    /// • Seek   — reposition the playhead by replaying only Instant commands up to a time.
    /// • Rebuild — patch in new commands without tearing down nodes that have not changed.
    ///
    /// The graph does NOT execute commands — that is the job of <see cref="VNPlayer"/>.
    /// The graph exposes the current node cursor and navigation helpers only.
    ///
    /// Partial rebuild rule
    /// ────────────────────
    /// When the designer edits a timeline in the editor (adds/removes/reorders commands),
    /// <see cref="RebuildFrom"/> only reconstructs nodes whose commands changed.
    /// Nodes before the edit point are left intact, preserving their Completed state.
    /// </summary>
    public sealed class VNPlayableGraph
    {
        // ── Graph state ────────────────────────────────────────────────────────
        private CommandNode _head;          // first node
        private CommandNode _current;       // playhead cursor
        private VNTimeline  _sourceTimeline;

        // Flat list kept in sync for O(1) index access (seek, editor queries).
        private readonly List<CommandNode> _nodes = new();

        // ── Public surface ─────────────────────────────────────────────────────

        /// <summary>The node the player should execute next. Null if finished.</summary>
        public CommandNode Current => _current;

        /// <summary>True when there are no more nodes to execute.</summary>
        public bool IsFinished => _current == null;

        /// <summary>How many nodes are in the graph.</summary>
        public int NodeCount => _nodes.Count;

        /// <summary>The timeline this graph was built from.</summary>
        public VNTimeline SourceTimeline => _sourceTimeline;

        // ── Build ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Full build from a timeline. Replaces any existing graph.
        /// Complexity: O(n log n) due to sort inside GetCommandsSortedByTime().
        /// </summary>
        public void Build(VNTimeline timeline)
        {
            _sourceTimeline = timeline;
            _nodes.Clear();
            _head = null;
            _current = null;

            if (timeline == null) return;

            var sorted = timeline.GetCommandsSortedByTime();
            if (sorted.Count == 0) return;

            CommandNode prev = null;
            foreach (var cmd in sorted)
            {
                var node = new CommandNode(cmd);
                _nodes.Add(node);

                if (prev == null)
                    _head = node;
                else
                    prev.Next = node;

                prev = node;
            }

            _current = _head;
        }

        // ── Seek ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Reposition the playhead to <paramref name="targetTime"/> without executing
        /// Await commands (dialogue, choices). Used by the TimelineEditor scrubber
        /// and by the player when jumping to a saved position.
        ///
        /// All Instant commands whose startTime ≤ targetTime are applied via
        /// <see cref="BaseCommand.ApplyInstantState"/> to reconstruct visual state.
        ///
        /// After seeking, <see cref="Current"/> points to the first node
        /// at or after <paramref name="targetTime"/>.
        /// </summary>
        public void Seek(float targetTime, ICommandContext context)
        {
            // Reset all nodes.
            foreach (var n in _nodes) n.Reset();

            _current = _head;

            foreach (var node in _nodes)
            {
                if (node.Command.startTime > targetTime)
                    break;

                // Only Instant commands contribute to reconstructed visual state.
                if (node.Command.ExecutionMode == CommandExecutionMode.Instant)
                {
                    node.Command.ApplyInstantState(context);
                    node.State = NodeState.Completed;
                    _current = node.Next; // advance cursor past this node
                }
                // Await commands at or before targetTime are skipped during seek.
                else
                {
                    // Cursor stops just before this Await command.
                    _current = node;
                    break;
                }
            }
        }

        // ── Partial rebuild ────────────────────────────────────────────────────

        /// <summary>
        /// Rebuild only the portion of the graph starting at
        /// <paramref name="firstChangedIndex"/> (0-based index in sorted command list).
        ///
        /// Nodes before the change point keep their current state.
        /// Nodes at and after are reconstructed from the updated timeline data.
        ///
        /// This avoids resetting already-completed portions of a long timeline
        /// when the designer edits only the end.
        /// </summary>
        public void RebuildFrom(int firstChangedIndex, VNTimeline timeline)
        {
            if (firstChangedIndex <= 0)
            {
                // Change is at the very start — full rebuild is simpler and correct.
                Build(timeline);
                return;
            }

            _sourceTimeline = timeline;

            var sorted = timeline.GetCommandsSortedByTime();

            // Truncate the node list at the change boundary.
            if (firstChangedIndex < _nodes.Count)
                _nodes.RemoveRange(firstChangedIndex, _nodes.Count - firstChangedIndex);

            // Detach old tail.
            if (_nodes.Count > 0)
                _nodes[_nodes.Count - 1].Next = null;

            // Append new nodes from the change point onward.
            CommandNode prev = _nodes.Count > 0 ? _nodes[_nodes.Count - 1] : null;

            for (int i = firstChangedIndex; i < sorted.Count; i++)
            {
                var node = new CommandNode(sorted[i]);
                _nodes.Add(node);

                if (prev == null)
                    _head = node;
                else
                    prev.Next = node;

                prev = node;
            }

            // If the current playhead was in the rebuilt section, reset it to the
            // first new node (safe fallback — editor will seek to correct position).
            bool cursorInvalidated = _current != null &&
                                     _nodes.IndexOf(_current) < 0;
            if (cursorInvalidated)
                _current = _nodes.Count > firstChangedIndex
                    ? _nodes[firstChangedIndex]
                    : null;
        }

        // ── Cursor navigation (called by VNPlayer) ─────────────────────────────

        /// <summary>Advance the cursor to the next node.</summary>
        public void Advance() => _current = _current?.Next;

        /// <summary>
        /// Return the node at <paramref name="index"/>, or null if out of range.
        /// Used by the TimelineEditor to highlight the active command.
        /// </summary>
        public CommandNode NodeAt(int index) =>
            index >= 0 && index < _nodes.Count ? _nodes[index] : null;

        /// <summary>
        /// Index of the current node in the flat list, or -1 if finished.
        /// </summary>
        public int CurrentIndex => _current == null ? -1 : _nodes.IndexOf(_current);

        // ── Debug ──────────────────────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>Read-only view of all nodes (editor inspectors, gizmos).</summary>
        public IReadOnlyList<CommandNode> AllNodes => _nodes;
#endif
    }
}
