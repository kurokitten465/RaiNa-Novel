using UnityEngine;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Displays a character sprite at a given position and layer.
    /// ExecutionMode: Instant — advances playhead immediately after showing.
    /// </summary>
    [CreateAssetMenu(
        fileName = "CMD_ShowCharacter",
        menuName = "VN Framework/Commands/Show Character")]
    public class ShowCharacterCommand : BaseCommand
    {
        [Header("Show Character")]
        [Tooltip("Unique identifier for this character slot (e.g. 'heroine', 'rival').")]
        public string characterId = string.Empty;

        [Tooltip("Sprite to display (outfit/expression variant).")]
        public Sprite characterSprite;

        [Tooltip("Horizontal position: 0 = far left, 0.5 = center, 1 = far right.")]
        [Range(0f, 1f)]
        public float horizontalPosition = 0.5f;

        [Tooltip("Sorting order — higher values render in front.")]
        public int layerOrder = 0;

        public override CommandExecutionMode ExecutionMode => CommandExecutionMode.Instant;

        public override void Execute(ICommandContext context, System.Action onComplete)
        {
            context.ShowCharacter(characterId, characterSprite, horizontalPosition, layerOrder);
            onComplete?.Invoke();
        }

        public override void ApplyInstantState(ICommandContext context)
        {
            context.ShowCharacter(characterId, characterSprite, horizontalPosition, layerOrder);
        }

#if UNITY_EDITOR
        public override string EditorSummary =>
            $"Show [{characterId}] @ {horizontalPosition:F2}  layer {layerOrder}";
#endif
    }
}
