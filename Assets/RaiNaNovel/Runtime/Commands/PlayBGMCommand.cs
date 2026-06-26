using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Starts background music playback.
    /// ExecutionMode: Instant — does not block the playhead.
    /// Audio continues playing until a new PlayBGM or StopBGM command fires.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_PlayBGM",
        menuName = "VN Framework/Commands/Play BGM")]
    public class PlayBGMCommand : BaseCommand
    {
        [Header("Play BGM")]
        [Tooltip("AudioClip to play as background music.")]
        public AudioClip bgmClip;

        [Tooltip("Playback volume (0–1).")]
        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Loop the track indefinitely.")]
        public bool loop = true;

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Instant;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            if (bgmClip != null)
                context.PlayBGM(bgmClip, volume, loop);
            onComplete?.Invoke();
        }

        // BGM is not rewound on seek — intentional MVP behaviour.
        // A future version may snapshot audio state per timeline position.

#if UNITY_EDITOR
        public override string EditorSummary =>
            bgmClip != null ? $"BGM → {bgmClip.name}  vol {volume:F1}" : "BGM → (none)";
#endif
    }
}
