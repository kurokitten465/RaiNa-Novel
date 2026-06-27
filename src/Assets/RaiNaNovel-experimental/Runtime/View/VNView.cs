using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VNFramework.Runtime.Player;

namespace VNFramework.Runtime.View
{
    /// <summary>
    /// Concrete implementation of <see cref="IVNView"/>.
    ///
    /// Scene hierarchy expected
    /// ────────────────────────
    /// VNRoot (Canvas, Screen Space – Overlay)
    ///   ├── BackgroundLayer    (Image — full-screen)
    ///   ├── CharacterLayer     (RectTransform — full-screen, no Image)
    ///   ├── DialogueBox        (DialogueBox component)
    ///   ├── ChoicePanel        (ChoicePanel component)
    ///   └── BGMPlayer          (BGMPlayer component + AudioSource)
    ///
    /// VNView wires itself to VNPlayer in Awake via GetComponent.
    /// Add both to the same GameObject or assign via <see cref="_player"/>.
    ///
    /// Character slots are created dynamically — no pre-placed slot GameObjects needed.
    /// </summary>
    [AddComponentMenu("VN Framework/VN View")]
    public sealed class VNView : MonoBehaviour, IVNView
    {
        // ── Inspector refs ─────────────────────────────────────────────────────

        [Header("Scene Layers")]
        [Tooltip("Full-screen background Image component.")]
        [SerializeField] private Image _backgroundImage;

        [Tooltip("Empty RectTransform that contains all character slots.")]
        [SerializeField] private RectTransform _characterLayer;

        [Header("UI Components")]
        [SerializeField] private DialogueBox _dialogueBox;
        [SerializeField] private ChoicePanel _choicePanel;
        [SerializeField] private BGMPlayer   _bgmPlayer;

        [Header("Character Slot")]
        [Tooltip("Prefab with CharacterSlot + Image. Created per unique characterId.")]
        [SerializeField] private GameObject _characterSlotPrefab;

        [Header("Player (optional — auto-found on same GameObject)")]
        [SerializeField] private VNPlayer _player;

        // ── Runtime character tracking ─────────────────────────────────────────
        // key: characterId  value: the slot showing that character
        private readonly Dictionary<string, CharacterSlot> _activeSlots = new();

        // Pool of hidden slots for reuse
        private readonly List<CharacterSlot> _slotPool = new();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Auto-find VNPlayer on same GameObject if not explicitly assigned.
            if (_player == null)
                _player = GetComponent<VNPlayer>();

            if (_player != null)
                _player.SetView(this);
            else
                Debug.LogWarning("[VNView] No VNPlayer found. Set the Player field or " +
                                 "add VNPlayer to the same GameObject.");

            // Ensure panels start hidden.
            _dialogueBox?.Hide();
            _choicePanel?.Hide();
        }

        // ── IVNView — Background ───────────────────────────────────────────────

        public void SetBackground(Sprite sprite)
        {
            if (_backgroundImage == null)
            {
                Debug.LogWarning("[VNView] BackgroundImage is not assigned.");
                return;
            }

            _backgroundImage.sprite  = sprite;
            _backgroundImage.enabled = sprite != null;

            // Fit the sprite to the full screen rect.
            if (sprite != null)
                _backgroundImage.type = Image.Type.Simple;
        }

        // ── IVNView — Characters ───────────────────────────────────────────────

        public void ShowCharacter(string characterId, Sprite sprite, float position, int layerOrder)
        {
            if (string.IsNullOrEmpty(characterId))
            {
                Debug.LogWarning("[VNView] ShowCharacter called with empty characterId.");
                return;
            }

            var slot = GetOrCreateSlot(characterId);
            slot.Show(characterId, sprite, position, layerOrder);
        }

        public void HideCharacter(string characterId)
        {
            if (_activeSlots.TryGetValue(characterId, out var slot))
            {
                slot.Hide();
                _activeSlots.Remove(characterId);
                _slotPool.Add(slot);
            }
        }

        // ── IVNView — Dialogue ─────────────────────────────────────────────────

        public void ShowDialogue(string speakerName, string dialogueText, Action onTap)
        {
            if (_dialogueBox == null)
            {
                Debug.LogWarning("[VNView] DialogueBox is not assigned — auto-confirming.");
                onTap?.Invoke();
                return;
            }

            _dialogueBox.Show(speakerName, dialogueText, onTap);
        }

        public void HideDialogue() => _dialogueBox?.Hide();

        // ── IVNView — Choices ──────────────────────────────────────────────────

        public void ShowChoices(string[] options, Action<int> onSelected)
        {
            if (_choicePanel == null)
            {
                Debug.LogWarning("[VNView] ChoicePanel is not assigned — auto-selecting 0.");
                onSelected?.Invoke(0);
                return;
            }

            _choicePanel.Show(options, onSelected);
        }

        public void HideChoices() => _choicePanel?.Hide();

        // ── IVNView — Audio ────────────────────────────────────────────────────

        public void PlayBGM(AudioClip clip, float volume, bool loop)
        {
            if (_bgmPlayer == null)
            {
                Debug.LogWarning("[VNView] BGMPlayer is not assigned.");
                return;
            }
            _bgmPlayer.Play(clip, volume, loop);
        }

        public void StopBGM() => _bgmPlayer?.Stop();

        // ── Character slot pool ────────────────────────────────────────────────

        private CharacterSlot GetOrCreateSlot(string characterId)
        {
            // If already active, reuse it (e.g. expression change).
            if (_activeSlots.TryGetValue(characterId, out var existing))
                return existing;

            // Grab from pool first.
            CharacterSlot slot = null;
            for (int i = _slotPool.Count - 1; i >= 0; i--)
            {
                if (_slotPool[i] != null)
                {
                    slot = _slotPool[i];
                    _slotPool.RemoveAt(i);
                    break;
                }
            }

            // Nothing in pool — instantiate.
            if (slot == null)
            {
                if (_characterSlotPrefab == null)
                {
                    Debug.LogError("[VNView] CharacterSlotPrefab is not assigned.");
                    return null;
                }

                var parent = _characterLayer != null
                    ? _characterLayer
                    : (RectTransform)transform;

                var go = Instantiate(_characterSlotPrefab, parent);
                slot   = go.GetComponent<CharacterSlot>();

                if (slot == null)
                {
                    Debug.LogError("[VNView] CharacterSlotPrefab is missing a CharacterSlot component.");
                    Destroy(go);
                    return null;
                }
            }

            _activeSlots[characterId] = slot;
            return slot;
        }

        // ── Editor helpers ─────────────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>
        /// Validate that all required references are assigned.
        /// Called by a custom editor or OnValidate.
        /// </summary>
        private void OnValidate()
        {
            if (_backgroundImage   == null) Debug.LogWarning("[VNView] BackgroundImage not set.",   this);
            if (_characterLayer    == null) Debug.LogWarning("[VNView] CharacterLayer not set.",    this);
            if (_dialogueBox       == null) Debug.LogWarning("[VNView] DialogueBox not set.",       this);
            if (_choicePanel       == null) Debug.LogWarning("[VNView] ChoicePanel not set.",       this);
            if (_bgmPlayer         == null) Debug.LogWarning("[VNView] BGMPlayer not set.",         this);
            if (_characterSlotPrefab == null) Debug.LogWarning("[VNView] CharacterSlotPrefab not set.", this);
        }
#endif
    }
}
