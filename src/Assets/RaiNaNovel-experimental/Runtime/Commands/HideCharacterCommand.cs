using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Hides a previously shown character sprite.
    /// ExecutionMode: Instant.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_HideCharacter",
        menuName = "VN Framework/Commands/Hide Character")]
    public class HideCharacterCommand : BaseCommand
    {
        [Header("Hide Character")]
        [Tooltip("Must match the characterId used in ShowCharacterCommand.")]
        public string characterId = string.Empty;

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Instant;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            context.HideCharacter(characterId);
            onComplete?.Invoke();
        }

        public override void ApplyInstantState(ICommandContext context)
        {
            context.HideCharacter(characterId);
        }

#if UNITY_EDITOR
        public override string EditorSummary => $"Hide [{characterId}]";
#endif
    }
}
