using UnityEngine;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The menu track. Its legacy <see cref="AudioSource"/> is routed to the
    /// D-090 Music mixer group by the generated scene while this component
    /// retains the already-proven loop and live-volume lifecycle. Moving both
    /// music controllers behind IAudioService remains a documented follow-up.
    /// <para>
    /// LIVE VOLUME is the point of this component: it subscribes to
    /// <see cref="GameSettingsStore.Applied"/> and the settings panel
    /// additionally pushes the volume in on every slider frame, so dragging
    /// the slider is audible while it is being dragged, not after the panel
    /// closes.
    /// </para>
    /// <para>
    /// The clip is a seamless loop already (the last six seconds are blended
    /// over the head of the track), so <c>AudioSource.loop</c> is all the
    /// looping this needs — no crossfade in code.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuMusicPlayer : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private AudioSource _source;
        [Tooltip("Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg — seamlessly looped in the file itself.")]
        [SerializeField] private AudioClip _clip;

        [Header("Presentation")]
        [Tooltip("Seconds the track takes to fade out when the match starts. 0 = cut.")]
        [SerializeField] private float _fadeOutSeconds = 1.25f;

        // < 0 means "not fading". The fade is driven from Update instead of a
        // coroutine so it survives a paused simulation and needs no allocation.
        private float _fadeDuration = -1f;
        private float _fadeElapsed;
        private float _fadeStartVolume;

        /// <summary>True while the track is on its way to silence; the volume is owned by the fade until it lands.</summary>
        public bool IsFading => _fadeDuration >= 0f;

        private void Awake()
        {
            if (_source == null) _source = GetComponent<AudioSource>();

            if (_clip == null)
            {
                Debug.LogError(
                    "[MenuMusicPlayer] No AudioClip wired — the main menu stays silent. Expected " +
                    "Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg (wired by BootstrapSceneGenerator).");
            }

            // The component configures its own source: the scene generator
            // wires references, it does not tune (BootstrapSceneGenerator's
            // own house rule). 2D because a menu track has no position in the
            // world, and playOnAwake stays OFF — see OnEnable.
            _source.clip = _clip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            GameSettingsStore.Applied += OnSettingsApplied;

            // Volume first, Play() second, deliberately. With playOnAwake the
            // engine starts the clip before any stored setting has been
            // applied, so a player who muted the music would hear a burst of
            // it at the 0.4 default on every single start.
            ApplyVolume(GameSettingsStore.Current);
            if (_clip != null && !_source.isPlaying)
            {
                _source.Play();
            }
        }

        private void OnDisable()
        {
            GameSettingsStore.Applied -= OnSettingsApplied;
        }

        /// <summary>
        /// Pushes the stored music volume onto the source. Called both from
        /// the <see cref="GameSettingsStore.Applied"/> event and directly by
        /// the settings panel while a slider is dragged. A running fade wins:
        /// it is already on its way to zero and must not be overwritten by a
        /// settings write that lands in the same frame.
        /// </summary>
        public void ApplyVolume(GameSettings settings)
        {
            if (_source == null || settings == null || IsFading) return;
            _source.volume = settings.EffectiveMusicVolume;
        }

        /// <summary>
        /// Fades the track out and stops it. Called once, when the match
        /// starts — the menu track plays exactly once per session, so the
        /// component switches itself off when the fade lands.
        /// </summary>
        public void FadeOutAndStop()
        {
            if (_source == null) return;

            if (!_source.isPlaying || _fadeOutSeconds <= 0f)
            {
                _source.Stop();
                _fadeDuration = -1f;
                enabled = false;
                return;
            }

            _fadeDuration = _fadeOutSeconds;
            _fadeElapsed = 0f;
            _fadeStartVolume = _source.volume;
        }

        private void Update()
        {
            if (_fadeDuration < 0f) return;

            // Unscaled: nothing in this project touches Time.timeScale today,
            // but a fade that silently stalls under a future pause would be a
            // nasty thing to debug.
            _fadeElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            _source.volume = Mathf.Lerp(_fadeStartVolume, 0f, progress);
            if (progress < 1f) return;

            _source.Stop();
            _fadeDuration = -1f;
            enabled = false;
        }

        private void OnSettingsApplied(GameSettings settings) => ApplyVolume(settings);
    }
}
