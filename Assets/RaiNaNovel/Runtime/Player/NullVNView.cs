using System;
using UnityEngine;
using VNFramework.Runtime.Core;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// No-op implementation of <see cref="IVNView"/>.
    ///
    /// Used as a safe default when no real view is available —
    /// dialogue taps auto-confirm after one frame so the graph
    /// can still advance during tests and early editor runs.
    ///
    /// Replace with VNView (Phase 3) in production.
    /// </summary>
    internal sealed class NullVNView : IVNView
    {
        private readonly MonoBehaviour _runner;

        public NullVNView(MonoBehaviour runner) => _runner = runner;

        public void SetBackground(Sprite sprite)
            => Debug.Log($"[NullView] SetBackground: {sprite?.name ?? "null"}");

        public void ShowCharacter(string id, Sprite sprite, float pos, int order)
            => Debug.Log($"[NullView] ShowCharacter: {id} @ {pos:F2}  layer {order}");

        public void HideCharacter(string id)
            => Debug.Log($"[NullView] HideCharacter: {id}");

        public void ShowDialogue(string speaker, string text, Action onTap)
        {
            Debug.Log($"[NullView] Dialogue [{speaker}]: {text}");
            // Auto-advance after one frame so headless runs don't block forever.
            _runner.StartCoroutine(AutoConfirm(onTap));
        }

        public void HideDialogue()
            => Debug.Log("[NullView] HideDialogue");

        public void ShowChoices(string[] options, Action<int> onSelected)
        {
            Debug.Log($"[NullView] ShowChoices ({options.Length} options) → auto-selecting 0");
            _runner.StartCoroutine(AutoConfirm(() => onSelected?.Invoke(0)));
        }

        public void HideChoices()
            => Debug.Log("[NullView] HideChoices");

        public void PlayBGM(AudioClip clip, float volume, bool loop)
            => Debug.Log($"[NullView] PlayBGM: {clip?.name}  vol {volume}  loop {loop}");

        public void StopBGM()
            => Debug.Log("[NullView] StopBGM");

        private static System.Collections.IEnumerator AutoConfirm(Action action)
        {
            yield return null; // wait one frame
            action?.Invoke();
        }
    }
}
