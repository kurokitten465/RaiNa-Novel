using UnityEngine;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Presents the player with branching choices.
    /// Each option maps to a target <see cref="VNTimeline"/> asset.
    /// ExecutionMode: Await — holds playhead until the player selects an option.
    ///
    /// After selection the runtime player loads the target timeline.
    /// The current timeline does NOT continue after a Choice.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_Choice",
        menuName = "VN Framework/Commands/Choice")]
    public class ChoiceCommand : BaseCommand
    {
        [System.Serializable]
        public struct ChoiceOption
        {
            [Tooltip("Text shown on the choice button.")]
            public string label;

            [Tooltip("Timeline to load when this option is selected. Null = end story.")]
            public VNTimeline targetTimeline;
        }

        [Header("Choice")]
        [Tooltip("The options displayed to the player.")]
        public ChoiceOption[] options = System.Array.Empty<ChoiceOption>();

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Await;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            if (options == null || options.Length == 0)
            {
                // Nothing to choose — treat as end of branch.
                onComplete?.Invoke();
                return;
            }

            var labels = new string[options.Length];
            for (int i = 0; i < options.Length; i++)
                labels[i] = options[i].label;

            context.ShowChoices(labels, selectedIndex =>
            {
                context.HideChoices();

                var chosen = options[selectedIndex];
                if (chosen.targetTimeline != null)
                {
                    // Branch — runtime player takes over; onComplete is not called
                    // because the current timeline is abandoned.
                    context.LoadTimeline(chosen.targetTimeline);
                }
                else
                {
                    // Null target = dead end, just finish.
                    onComplete?.Invoke();
                }
            });
        }

#if UNITY_EDITOR
        public override string EditorSummary
        {
            get
            {
                if (options == null || options.Length == 0)
                    return "Choice (no options)";

                var parts = new string[options.Length];
                for (int i = 0; i < options.Length; i++)
                    parts[i] = options[i].label;

                return "Choice: " + string.Join(" / ", parts);
            }
        }
#endif
    }
}
