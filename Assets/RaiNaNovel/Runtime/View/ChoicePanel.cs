using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VNFramework.Runtime.View
{
    /// <summary>
    /// Renders a set of choice buttons and reports the player's selection.
    ///
    /// Design contract
    /// ───────────────
    /// • <see cref="Show"/> spawns one button per option from <see cref="_buttonPrefab"/>.
    /// • Pressing a button fires <see cref="_onSelected"/> exactly once.
    /// • <see cref="Hide"/> destroys spawned buttons and hides the panel.
    ///
    /// Button prefab requirements
    /// ──────────────────────────
    /// The prefab must have a Button component and a TextMeshProUGUI child
    /// named "Label" (or the first TMP child found will be used).
    /// </summary>
    public sealed class ChoicePanel : MonoBehaviour
    {
        // ── Inspector refs ─────────────────────────────────────────────────────

        [Header("Layout")]
        [Tooltip("Root panel shown/hidden as a unit.")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Container that holds spawned buttons (use a Vertical Layout Group).")]
        [SerializeField] private Transform _buttonContainer;

        [Tooltip("Prefab with Button + TextMeshProUGUI child.")]
        [SerializeField] private GameObject _buttonPrefab;

        // ── State ──────────────────────────────────────────────────────────────
        private Action<int>        _onSelected;
        private List<GameObject>   _spawnedButtons = new();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Show choices. Spawns one button per option label.
        /// </summary>
        public void Show(string[] options, Action<int> onSelected)
        {
            _onSelected = onSelected;
            ClearButtons();

            for (int i = 0; i < options.Length; i++)
            {
                int capturedIndex = i; // capture for closure
                var go = Instantiate(_buttonPrefab, _buttonContainer);
                _spawnedButtons.Add(go);

                // Set label text
                var label = go.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = options[i];

                // Wire button click
                var btn = go.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnChoiceSelected(capturedIndex));
            }

            if (_panel != null) _panel.SetActive(true);
        }

        /// <summary>Hide the panel and destroy all spawned buttons.</summary>
        public void Hide()
        {
            _onSelected = null;
            ClearButtons();
            if (_panel != null) _panel.SetActive(false);
        }

        // ── Internal ───────────────────────────────────────────────────────────

        private void OnChoiceSelected(int index)
        {
            var cb = _onSelected;
            _onSelected = null;     // prevent double-fire if multiple buttons somehow clicked

            Hide();
            cb?.Invoke(index);
        }

        private void ClearButtons()
        {
            foreach (var go in _spawnedButtons)
                if (go != null) Destroy(go);
            _spawnedButtons.Clear();
        }
    }
}
