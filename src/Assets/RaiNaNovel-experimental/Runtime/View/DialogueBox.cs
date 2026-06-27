using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VNFramework.Runtime.View
{
    /// <summary>
    /// Manages the dialogue box UI: nameplate, text body, and tap-to-advance.
    ///
    /// Design contract
    /// ───────────────
    /// • <see cref="Show"/> registers exactly one tap callback.
    /// • Tapping (click / touch / Space / Enter) fires the callback once, then clears it.
    /// • <see cref="Hide"/> clears the callback and hides the panel without firing.
    ///
    /// Text rendering: TextMeshProUGUI for crisp quality.
    /// Falls back to legacy Text if TMP is not available (stripped via #if).
    ///
    /// Wire-up: assign all serialized fields in the Inspector or via
    /// VNView's auto-find helpers.
    /// </summary>
    public sealed class DialogueBox : MonoBehaviour
    {
        // ── Inspector refs ─────────────────────────────────────────────────────

        [Header("Panels")]
        [Tooltip("Root panel — shown/hidden as a unit.")]
        [SerializeField] private GameObject _panel;

        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _speakerNameText;
        [SerializeField] private TextMeshProUGUI _dialogueText;

        [Header("Nameplate")]
        [Tooltip("Optional nameplate background — hidden when speaker name is empty.")]
        [SerializeField] private GameObject _nameplate;

        [Header("Advance indicator")]
        [Tooltip("Optional blinking arrow / icon shown when waiting for tap.")]
        [SerializeField] private GameObject _advanceIndicator;

        // ── State ──────────────────────────────────────────────────────────────
        private Action _onTap;
        private bool   _waitingForTap;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void Update()
        {
            if (!_waitingForTap) return;

            // Accept tap / click / keyboard confirm.
            if (Input.GetMouseButtonDown(0) ||
                Input.GetKeyDown(KeyCode.Space) ||
                Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Confirm();
            }
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Display the dialogue box with the given content and register the tap callback.
        /// Calling Show while already visible replaces the content and callback.
        /// </summary>
        public void Show(string speakerName, string text, Action onTap)
        {
            _onTap         = onTap;
            _waitingForTap = true;

            // Speaker name + nameplate
            bool hasSpeaker = !string.IsNullOrEmpty(speakerName);
            if (_speakerNameText != null) _speakerNameText.text = speakerName;
            if (_nameplate       != null) _nameplate.SetActive(hasSpeaker);

            // Dialogue text
            if (_dialogueText != null) _dialogueText.text = text;

            // Advance indicator starts hidden; shown after text is "printed"
            // (in MVP we show it immediately — typewriter effect is post-MVP).
            if (_advanceIndicator != null) _advanceIndicator.SetActive(true);

            if (_panel != null) _panel.SetActive(true);
        }

        /// <summary>Hide the dialogue box without firing the tap callback.</summary>
        public void Hide()
        {
            _waitingForTap = false;
            _onTap         = null;
            if (_panel            != null) _panel.SetActive(false);
            if (_advanceIndicator != null) _advanceIndicator.SetActive(false);
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void Confirm()
        {
            if (!_waitingForTap) return;

            _waitingForTap = false;
            if (_advanceIndicator != null) _advanceIndicator.SetActive(false);

            var cb = _onTap;
            _onTap = null;
            cb?.Invoke();   // fire exactly once
        }
    }
}
