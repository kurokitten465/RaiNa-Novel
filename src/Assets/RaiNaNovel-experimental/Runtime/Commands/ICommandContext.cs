using UnityEngine;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Commands
{
    /// <summary>
    /// Provides commands with access to runtime services.
    ///
    /// Commands depend on this interface only — never on concrete MonoBehaviours.
    /// This keeps commands testable and decoupled from scene structure.
    /// </summary>
    public interface ICommandContext
    {
        // ── Visuals ────────────────────────────────────────────────────────────

        /// <summary>Set the full-screen background image.</summary>
        void SetBackground(Sprite sprite);

        /// <summary>
        /// Show a character sprite at a named anchor position.
        /// </summary>
        /// <param name="characterId">Unique id matching a CharacterSlot in the view.</param>
        /// <param name="sprite">Sprite to display.</param>
        /// <param name="position">Normalized horizontal position (0 = left, 1 = right).</param>
        /// <param name="layerOrder">Sorting order among characters.</param>
        void ShowCharacter(string characterId, Sprite sprite, float position, int layerOrder);

        /// <summary>Hide a previously shown character.</summary>
        void HideCharacter(string characterId);

        // ── Dialogue ───────────────────────────────────────────────────────────

        /// <summary>
        /// Display a dialogue line.
        /// The implementation must call <paramref name="onTap"/> when the player taps/clicks.
        /// </summary>
        void ShowDialogue(string speakerName, string dialogueText, System.Action onTap);

        /// <summary>Hide the dialogue box.</summary>
        void HideDialogue();

        // ── Choice ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Present branching choices to the player.
        /// <paramref name="onSelected"/> receives the index of the chosen option.
        /// </summary>
        void ShowChoices(string[] options, System.Action<int> onSelected);

        /// <summary>Hide the choice panel.</summary>
        void HideChoices();

        // ── Audio ──────────────────────────────────────────────────────────────

        /// <summary>Play or cross-fade background music.</summary>
        void PlayBGM(AudioClip clip, float volume, bool loop);

        /// <summary>Stop background music.</summary>
        void StopBGM();

        // ── Timeline navigation ────────────────────────────────────────────────

        /// <summary>
        /// Called by Choice command to branch to another Timeline asset.
        /// </summary>
        void LoadTimeline(VNTimeline timeline);
    }
}
