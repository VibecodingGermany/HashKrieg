using Nova.Gameplay.Audio;
using UnityEngine;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Rank-4 settings adapter for the rank-3 audio contract. It is the only
    /// bridge needed because the backend never depends upward on UI types.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SfxSettingsBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            GameSettingsStore.Applied += Apply;
        }

        private void Start()
        {
            // Start runs after every scene Awake, including the audio backend's
            // registration in AudioServiceLocator.
            Apply(GameSettingsStore.Current);
        }

        private void OnDisable()
        {
            GameSettingsStore.Applied -= Apply;
        }

        public void Apply(GameSettings settings)
        {
            if (settings == null) return;
            settings.Sanitize();
            AudioServiceLocator.Current?.SetBusVolume(AudioBus.Sfx, settings.EffectiveSfxVolume);
        }
    }
}
