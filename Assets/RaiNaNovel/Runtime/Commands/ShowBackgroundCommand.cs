using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Instantly swaps the full-screen background to a new sprite.
    /// ExecutionMode: Instant — advances the playhead immediately.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_ShowBackground",
        menuName = "VN Framework/Commands/Show Background")]
    public class ShowBackgroundCommand : BaseCommand
    {
        [Header("Show Background")]
        [Tooltip("The sprite to display as the scene background.")]
        public Sprite backgroundSprite;

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Instant;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            context.SetBackground(backgroundSprite);
            onComplete?.Invoke();
        }

        public override void ApplyInstantState(ICommandContext context)
        {
            context.SetBackground(backgroundSprite);
        }

#if UNITY_EDITOR
        public override string EditorSummary =>
            backgroundSprite != null
                ? $"Background → {backgroundSprite.name}"
                : "Background → (none)";
#endif
    }
}
