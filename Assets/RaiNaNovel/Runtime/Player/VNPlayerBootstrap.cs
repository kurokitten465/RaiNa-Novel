using UnityEngine;

namespace VNFramework.Runtime.Player
{
    /// <summary>
    /// Automatically injects a <see cref="NullVNView"/> into <see cref="VNPlayer"/>
    /// if no real <see cref="IVNView"/> is found on the same GameObject.
    ///
    /// Add this component alongside VNPlayer during development (Phase 1–2).
    /// Remove it in Phase 3 once VNView is wired.
    ///
    /// Lives in the Runtime assembly so it compiles in builds —
    /// but NullVNView logs are a development-time hint to add the real view.
    /// </summary>
    [AddComponentMenu("VN Framework/VN Player Bootstrap (Dev)")]
    [RequireComponent(typeof(VNPlayer))]
    public sealed class VNPlayerBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var player = GetComponent<VNPlayer>();
            var view   = GetComponent<IVNView>();

            if (view == null)
            {
                Debug.LogWarning(
                    "[VNPlayerBootstrap] No IVNView found — injecting NullVNView. " +
                    "Add VNView (Phase 3) to remove this warning.");

                player.SetView(new NullVNView(this));
            }
            // If a real IVNView exists (Phase 3), VNPlayer's Awake() will pick it up
            // via GetComponent — nothing extra needed here.
        }
    }
}
