using System;
using UnityEngine;
using VNFramework.Runtime.Commands;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// Runtime MonoBehaviour that drives a <see cref="VNPlayableGraph"/>.
    ///
    /// Responsibilities
    /// ────────────────
    /// • Load a <see cref="VNTimeline"/> and build the graph.
    /// • Step through command nodes: fire Instant commands synchronously,
    ///   pause on Await commands until their onComplete callback arrives.
    /// • Expose Play / Pause / Seek for editor and UI controls.
    /// • Implement <see cref="ICommandContext"/> to serve commands with
    ///   runtime services — forwarded to the VN View layer (Phase 3).
    ///
    /// ICommandContext
    /// ───────────────
    /// VNPlayer implements the interface but delegates every visual/audio
    /// call to a <see cref="IVNView"/> (set in Phase 3). This way commands
    /// stay decoupled and the player acts as the pure orchestrator.
    ///
    /// No Reflection at runtime.
    /// </summary>
    [AddComponentMenu("VN Framework/VN Player")]
    public sealed class VNPlayer : MonoBehaviour, ICommandContext
    {
        // ── Inspector ──────────────────────────────────────────────────────────

        [Header("Timeline")]
        [Tooltip("Timeline to play automatically on Start. Can be null for manual control.")]
        [SerializeField] private VNTimeline _startTimeline;

        [Header("Playback")]
        [Tooltip("Auto-play the start timeline on Awake.")]
        [SerializeField] private bool _autoPlay = true;

        // ── Internal graph ─────────────────────────────────────────────────────
        private readonly VNPlayableGraph _graph = new();

        // ── State ──────────────────────────────────────────────────────────────
        private PlaybackState _state = PlaybackState.Idle;
        private bool _pendingAdvance;   // set inside Execute callbacks to defer Advance()

        // ── View delegate (injected in Phase 3) ───────────────────────────────
        private IVNView _view;

        // ── Events ─────────────────────────────────────────────────────────────

        /// <summary>Fired whenever the playback state changes.</summary>
        public event Action<PlaybackState> OnStateChanged;

        /// <summary>Fired when a new timeline finishes loading.</summary>
        public event Action<VNTimeline> OnTimelineLoaded;

        /// <summary>Fired when the timeline reaches its end.</summary>
        public event Action OnFinished;

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>Current playback state.</summary>
        public PlaybackState State => _state;

        /// <summary>The timeline currently loaded in the graph.</summary>
        public VNTimeline CurrentTimeline => _graph.SourceTimeline;

        /// <summary>
        /// Inject the view implementation. Call this before Play() or from Awake
        /// on the component that owns both VNPlayer and IVNView.
        /// </summary>
        public void SetView(IVNView view) => _view = view;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // View may be on the same GameObject (Phase 3 will wire this up).
            if (_view == null)
                _view = GetComponent<IVNView>();
        }

        private void Start()
        {
            if (_autoPlay && _startTimeline != null)
                LoadAndPlay(_startTimeline);
        }

        // ── Playback controls ──────────────────────────────────────────────────

        /// <summary>Load a timeline and immediately start playback from the beginning.</summary>
        public void LoadAndPlay(VNTimeline timeline)
        {
            if (timeline == null)
            {
                Debug.LogWarning("[VNPlayer] LoadAndPlay called with null timeline.");
                SetState(PlaybackState.Finished);
                return;
            }

            _graph.Build(timeline);
            OnTimelineLoaded?.Invoke(timeline);

            SetState(PlaybackState.Playing);
            ProcessGraph();
        }

        /// <summary>Pause playback. Does nothing if already paused or idle.</summary>
        public void Pause()
        {
            if (_state == PlaybackState.Playing || _state == PlaybackState.AwaitingInput)
                SetState(PlaybackState.Paused);
        }

        /// <summary>Resume from paused state.</summary>
        public void Resume()
        {
            if (_state != PlaybackState.Paused) return;

            // If we were mid-Await when paused, return to AwaitingInput.
            // The Await callback is still registered — it will fire when the player taps.
            var curNode = _graph.Current;
            if (curNode != null && curNode.State == NodeState.Running)
                SetState(PlaybackState.AwaitingInput);
            else
            {
                SetState(PlaybackState.Playing);
                ProcessGraph();
            }
        }

        /// <summary>
        /// Seek to a specific time in the current timeline.
        /// Instant commands up to that time are applied via ApplyInstantState.
        /// Await commands are skipped.
        /// </summary>
        public void Seek(float time)
        {
            if (_graph.SourceTimeline == null) return;

            var prevState = _state;
            SetState(PlaybackState.Seeking);

            _graph.Seek(time, this);

            // After seek: if nothing is left, we're finished.
            if (_graph.IsFinished)
                SetState(PlaybackState.Finished);
            else
            {
                // Return to paused so the caller can decide to resume.
                SetState(PlaybackState.Paused);
            }
        }

        /// <summary>
        /// Called by the editor after a partial timeline edit.
        /// Rebuilds graph from the first changed command index without
        /// resetting already-completed nodes.
        /// </summary>
        public void NotifyTimelineEdited(int firstChangedIndex)
        {
            if (_graph.SourceTimeline == null) return;
            _graph.RebuildFrom(firstChangedIndex, _graph.SourceTimeline);
        }

        // ── Graph processing ───────────────────────────────────────────────────

        /// <summary>
        /// Core loop: pump Instant commands until we hit an Await or finish.
        /// Each Instant command's onComplete immediately re-enters this method.
        /// Await commands set state to AwaitingInput and exit — re-entry happens
        /// when the view calls back.
        ///
        /// Stack depth concern: deeply sequential Instant commands could overflow
        /// the stack if called recursively. We use an iterative loop for Instant
        /// commands and only recurse (via callback) for Await resolution.
        /// </summary>
        private void ProcessGraph()
        {
            if (_state == PlaybackState.Paused || _state == PlaybackState.Seeking)
                return;

            while (!_graph.IsFinished)
            {
                var node = _graph.Current;
                if (node == null) break;

                if (node.Command.ExecutionMode == CommandExecutionMode.Instant)
                {
                    node.State = NodeState.Running;
                    _graph.Advance();           // move cursor before Execute so callback sees next node
                    node.Command.Execute(this, null);   // Instant — onComplete not needed
                    node.State = NodeState.Completed;
                    // Loop continues to next node.
                }
                else // Await
                {
                    node.State = NodeState.Running;
                    SetState(PlaybackState.AwaitingInput);

                    node.Command.Execute(this, () =>
                    {
                        // This callback fires from UI events (tap, choice selection).
                        // Guard against duplicate resolution.
                        if (node.State != NodeState.Running) return;

                        node.State = NodeState.Completed;
                        _graph.Advance();

                        if (_state == PlaybackState.Paused) return; // stay paused

                        SetState(PlaybackState.Playing);
                        ProcessGraph(); // re-enter after await resolves
                    });

                    return; // exit loop — waiting for callback
                }
            }

            // If we get here, no more nodes.
            SetState(PlaybackState.Finished);
            OnFinished?.Invoke();
        }

        // ── State machine ──────────────────────────────────────────────────────

        private void SetState(PlaybackState next)
        {
            if (_state == next) return;
            _state = next;
            OnStateChanged?.Invoke(_state);
        }

        // ── ICommandContext implementation ─────────────────────────────────────
        // All calls forward to IVNView (Phase 3). Guards prevent null-ref crashes
        // during testing before the view is wired.

        void ICommandContext.SetBackground(Sprite sprite)
            => _view?.SetBackground(sprite);

        void ICommandContext.ShowCharacter(string id, Sprite sprite, float pos, int order)
            => _view?.ShowCharacter(id, sprite, pos, order);

        void ICommandContext.HideCharacter(string id)
            => _view?.HideCharacter(id);

        void ICommandContext.ShowDialogue(string speaker, string text, Action onTap)
            => _view?.ShowDialogue(speaker, text, onTap);

        void ICommandContext.HideDialogue()
            => _view?.HideDialogue();

        void ICommandContext.ShowChoices(string[] options, Action<int> onSelected)
            => _view?.ShowChoices(options, onSelected);

        void ICommandContext.HideChoices()
            => _view?.HideChoices();

        void ICommandContext.PlayBGM(AudioClip clip, float volume, bool loop)
            => _view?.PlayBGM(clip, volume, loop);

        void ICommandContext.StopBGM()
            => _view?.StopBGM();

        void ICommandContext.LoadTimeline(VNTimeline timeline)
        {
            // Branch: teardown current, load new.
            LoadAndPlay(timeline);
        }

        // ── Editor API ─────────────────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>
        /// Read-only graph reference for the TimelineEditor to highlight
        /// the current node and draw state overlays.
        /// </summary>
        public VNPlayableGraph Graph => _graph;
#endif
    }
}
