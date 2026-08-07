// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using UnityEngine;
using Nova.Gameplay.Match;
using Nova.Simulation.Victory;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The in-game music (sprint 09 §6.1, license exception D-086): a
    /// three-track playlist that fades in when the match starts and fades out
    /// when the result screen appears — before this, the game went silent the
    /// second "Neues Spiel" landed, because only the menu had music.
    /// <para>
    /// STATE MODEL (mirrors the round itself): the playlist plays while the
    /// match is running and undecided; a latched <see cref="VictorySystem"/>
    /// outcome fades it out; the pause overlay keeps it playing (only the
    /// simulation stops, never the music); the main menu stops it — the menu
    /// track owns the speaker there (<see cref="MenuMusicPlayer"/>). No clip
    /// repeats back-to-back: a three-track round robin advances every track
    /// change, which satisfies "no title twice in a row" by construction.
    /// </para>
    /// <para>
    /// The fade mechanics follow <see cref="MenuMusicPlayer"/>'s precedent
    /// exactly (Update-driven, unscaled time, settings volume applied live
    /// via <see cref="GameSettingsStore.Applied"/>, fade wins over a settings
    /// write landing mid-fade) — deliberately restated here rather than
    /// shared, because the two components diverge in what a completed fade
    /// means (menu: stop the looping track; here: stop the playlist) and a
    /// shared abstraction would couple two components that otherwise change
    /// independently.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicDirector : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private AudioSource _source;
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private MatchBootstrap _bootstrap;
        [SerializeField] private MainMenuController _menu;
        [Tooltip("MUS_Ingame_Hashkrieg_01..03.ogg — the three Suno themes, longest cut each (D-086).")]
        [SerializeField] private AudioClip[] _playlist;

        [Header("Presentation")]
        [SerializeField] private float _fadeSeconds = 1.25f;
        [Tooltip("Seconds of silence between two tracks.")]
        [SerializeField] private float _trackGapSeconds = 2f;

        private int _trackIndex = -1;
        private float _gapUntil = -1f;
        private bool _playlistActive;

        // < 0 means "not fading". Same Update-driven convention as
        // MenuMusicPlayer: survives a paused simulation, no coroutine, no
        // allocation.
        private float _fadeDuration = -1f;
        private float _fadeElapsed;
        private float _fadeStartVolume;
        private float _fadeTargetVolume;
        private bool _stopAfterFade;

        private void Awake()
        {
            if (_source == null) _source = GetComponent<AudioSource>();
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_bootstrap == null) _bootstrap = FindAnyObjectByType<MatchBootstrap>();
            if (_menu == null) _menu = FindAnyObjectByType<MainMenuController>();

            _source.loop = false; // the playlist advances, not the clip
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            GameSettingsStore.Applied += OnSettingsApplied;
        }

        private void OnDisable()
        {
            GameSettingsStore.Applied -= OnSettingsApplied;
        }

        private void Update()
        {
            UpdateFade();
            UpdatePlaylistState();
        }

        /// <summary>
        /// The round's state transitions, polled: menu visible or no match →
        /// stopped; outcome latched → fade out; running and undecided →
        /// playlist lives (fade in if it was silent).
        /// </summary>
        private void UpdatePlaylistState()
        {
            bool menuVisible = _menu == null || _menu.IsMenuVisible;
            bool matchReady = _bootstrap != null && _bootstrap.IsMatchReady;
            bool decided = matchReady && _runner.Victory != null && _runner.Victory.IsDecided;

            if (menuVisible || !matchReady)
            {
                if (_playlistActive || _source.isPlaying)
                {
                    StopPlaylist();
                }
                return;
            }

            if (decided)
            {
                if (_playlistActive)
                {
                    BeginFade(0f, stopAfter: true);
                    _playlistActive = false;
                }
                return;
            }

            if (!_playlistActive)
            {
                _playlistActive = true;
                BeginFade(GameSettingsStore.Current != null ? GameSettingsStore.Current.EffectiveMusicVolume : 1f, stopAfter: false);
                StartNextTrack();
                return;
            }

            if (_source.isPlaying || IsFading) return;

            // A track that just ended leaves a short silence gap before the
            // round robin advances (the gap starts when the silence is first
            // noticed, so it begins exactly at the track's end).
            if (_gapUntil < 0f)
            {
                _gapUntil = Time.unscaledTime + _trackGapSeconds;
            }
            if (Time.unscaledTime >= _gapUntil)
            {
                StartNextTrack();
            }
        }

        /// <summary>Advances the round robin (never the same track twice in a row) and starts the clip.</summary>
        private void StartNextTrack()
        {
            if (_playlist == null || _playlist.Length == 0) return;

            _trackIndex = (_trackIndex + 1) % _playlist.Length;
            AudioClip clip = _playlist[_trackIndex];
            if (clip == null) return;

            _source.clip = clip;
            _source.Play();
            _gapUntil = -1f;
            if (!IsFading)
            {
                ApplyVolume(GameSettingsStore.Current);
            }
        }

        private void StopPlaylist()
        {
            _playlistActive = false;
            _trackIndex = -1;
            _gapUntil = -1f;
            _fadeDuration = -1f;
            if (_source.isPlaying)
            {
                _source.Stop();
            }
        }

        private void BeginFade(float targetVolume, bool stopAfter)
        {
            _fadeDuration = Mathf.Max(0.01f, _fadeSeconds);
            _fadeElapsed = 0f;
            _fadeStartVolume = _source.volume;
            _fadeTargetVolume = targetVolume;
            _stopAfterFade = stopAfter;
        }

        private bool IsFading => _fadeDuration >= 0f;

        private void UpdateFade()
        {
            if (!IsFading) return;

            _fadeElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(_fadeElapsed / _fadeDuration);
            _source.volume = Mathf.Lerp(_fadeStartVolume, _fadeTargetVolume, progress);
            if (progress < 1f) return;

            _fadeDuration = -1f;
            if (_stopAfterFade)
            {
                _source.Stop();
                _gapUntil = -1f;
            }
        }

        /// <summary>
        /// Live volume from the settings store, pushed onto the source
        /// immediately unless a fade owns the volume this frame (same
        /// precedence rule as MenuMusicPlayer). A track change within the
        /// playlist leaves a short silence gap.
        /// </summary>
        public void ApplyVolume(GameSettings settings)
        {
            if (_source == null || settings == null || IsFading) return;
            _source.volume = settings.EffectiveMusicVolume;
        }

        private void OnSettingsApplied(GameSettings settings) => ApplyVolume(settings);
    }
}
