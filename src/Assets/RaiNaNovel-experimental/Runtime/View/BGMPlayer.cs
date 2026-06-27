using UnityEngine;

namespace VNFramework.Runtime.View
{
    /// <summary>
    /// Thin wrapper around an <see cref="AudioSource"/> dedicated to BGM.
    ///
    /// MVP: immediate clip swap (no crossfade).
    /// Post-MVP: add a coroutine-based crossfade here without touching VNView.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class BGMPlayer : MonoBehaviour
    {
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop        = true;
        }

        /// <summary>Play a clip at the given volume. Replaces any currently playing BGM.</summary>
        public void Play(AudioClip clip, float volume, bool loop)
        {
            if (clip == null)
            {
                Stop();
                return;
            }

            _source.clip   = clip;
            _source.volume = Mathf.Clamp01(volume);
            _source.loop   = loop;
            _source.Play();
        }

        /// <summary>Stop the current BGM immediately.</summary>
        public void Stop()
        {
            _source.Stop();
            _source.clip = null;
        }

        /// <summary>True if a clip is currently playing.</summary>
        public bool IsPlaying => _source != null && _source.isPlaying;
    }
}
