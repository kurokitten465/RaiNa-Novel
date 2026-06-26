using System;
using UnityEngine;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// Separates the VNPlayer (orchestration) from the VN View (rendering).
    ///
    /// VNPlayer calls this interface; the concrete implementation lives in
    /// Phase 3 (VNView MonoBehaviour).
    ///
    /// This seam lets us:
    ///   • Unit-test the player with a mock view.
    ///   • Swap the entire UI skin without touching the player.
    ///   • Keep commands completely ignorant of Unity UI types.
    /// </summary>
    public interface IVNView
    {
        // ── Visuals ────────────────────────────────────────────────────────────
        void SetBackground(Sprite sprite);

        void ShowCharacter(string characterId, Sprite sprite, float position, int layerOrder);
        void HideCharacter(string characterId);

        // ── Dialogue ───────────────────────────────────────────────────────────

        /// <summary>
        /// Display the dialogue box.
        /// The implementation MUST call <paramref name="onTap"/> exactly once
        /// when the player confirms (tap, click, key press).
        /// </summary>
        void ShowDialogue(string speakerName, string dialogueText, Action onTap);
        void HideDialogue();

        // ── Choice ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Display the choice panel.
        /// The implementation MUST call <paramref name="onSelected"/> exactly once
        /// with the index of the chosen option.
        /// </summary>
        void ShowChoices(string[] options, Action<int> onSelected);
        void HideChoices();

        // ── Audio ──────────────────────────────────────────────────────────────
        void PlayBGM(AudioClip clip, float volume, bool loop);
        void StopBGM();
    }
}
