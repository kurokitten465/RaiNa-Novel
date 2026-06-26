using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Displays a dialogue line and waits for the player to tap/click before continuing.
    /// ExecutionMode: Await — playhead is held until the tap callback fires.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_ShowDialogue",
        menuName = "VN Framework/Commands/Show Dialogue")]
    public class ShowDialogueCommand : BaseCommand
    {
        [Header("Show Dialogue")]
        [Tooltip("Character name shown in the name plate. Leave empty to hide the nameplate.")]
        public string speakerName = string.Empty;

        [Tooltip("The dialogue text displayed in the text box.")]
        [TextArea(3, 6)]
        public string dialogueText = string.Empty;

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Await;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            // The context implementation must call onComplete when the player taps.
            context.ShowDialogue(speakerName, dialogueText, () =>
            {
                context.HideDialogue();
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// During seek the dialogue is not shown — the scene state is reconstructed
        /// from Instant commands only. Await commands are skipped during scrub.
        /// </summary>
        public override void ApplyInstantState(ICommandContext context)
        {
            // Intentionally left blank — dialogue does not apply during seek.
        }

#if UNITY_EDITOR
        public override string EditorSummary
        {
            get
            {
                var name = string.IsNullOrEmpty(speakerName) ? "—" : speakerName;
                var preview = dialogueText?.Length > 40
                    ? dialogueText[..40] + "…"
                    : dialogueText;
                return $"[{name}]: {preview}";
            }
        }
#endif
    }
}
