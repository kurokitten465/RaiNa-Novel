using UnityEngine;
using UnityEngine.UI;

namespace VNFramework.Runtime.View
{
    /// <summary>
    /// Represents one character display slot in the VN scene.
    ///
    /// Each slot owns one Image component. VNView creates/manages slots
    /// dynamically by characterId — there is no fixed "left / right / center"
    /// hardcoding. Position is driven by the normalized value from
    /// ShowCharacterCommand (0 = left edge, 1 = right edge).
    ///
    /// Layer order is applied to the Image's canvas sortingOrder so characters
    /// can overlap correctly (e.g. a foreground character in front of the bg).
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class CharacterSlot : MonoBehaviour
    {
        // ── Internal refs ──────────────────────────────────────────────────────
        private Image  _image;
        private RectTransform _rect;

        /// <summary>The characterId this slot was last assigned to.</summary>
        public string CharacterId { get; private set; }

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rect  = GetComponent<RectTransform>();

            // Characters sit at their anchor; pivot centered-bottom so position
            // feels natural (characters stand on a floor line).
            _rect.anchorMin = new Vector2(0f, 0f);
            _rect.anchorMax = new Vector2(0f, 0f);
            _rect.pivot     = new Vector2(0.5f, 0f);
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Assign a sprite and position this slot.
        /// </summary>
        /// <param name="characterId">Logical id from the command.</param>
        /// <param name="sprite">Sprite to display.</param>
        /// <param name="normalizedX">0 = left, 1 = right (relative to parent rect).</param>
        /// <param name="layerOrder">Canvas sibling order — higher is in front.</param>
        public void Show(string characterId, Sprite sprite, float normalizedX, int layerOrder)
        {
            CharacterId      = characterId;
            _image.sprite    = sprite;
            _image.enabled   = true;

            // Position: place anchor point at normalizedX across parent width.
            // We use anchoredPosition with a 0-width parent anchor.
            // The parent (CharacterLayer) must be full-screen.
            var parentRect = (_rect.parent as RectTransform);
            float parentWidth = parentRect != null ? parentRect.rect.width : Screen.width;
            _rect.anchoredPosition = new Vector2(normalizedX * parentWidth, 0f);

            // Sprite native size — designers set the sprite to the intended height.
            if (sprite != null)
                _rect.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * 100f;

            // Layer order via sibling index (simple, no extra Canvas component needed).
            transform.SetSiblingIndex(layerOrder);
        }

        /// <summary>Hide this slot (sprite stays in memory for fast re-show).</summary>
        public void Hide()
        {
            _image.enabled = false;
            CharacterId    = null;
        }

        /// <summary>True when this slot is currently displaying a character.</summary>
        public bool IsActive => _image != null && _image.enabled;
    }
}
