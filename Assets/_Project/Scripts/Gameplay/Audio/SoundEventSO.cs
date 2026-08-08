using System;
using UnityEngine;

namespace Nova.Gameplay.Audio
{
    /// <summary>
    /// One randomly selected take of a sound event. Layers start as one atomic
    /// playback instance; DTH_Building uses this to pair its low explosion and
    /// metal impact without special-case caller code.
    /// </summary>
    [Serializable]
    public sealed class SoundVariation
    {
        [Tooltip("Clips that start together. Null entries are ignored; at least one clip is required.")]
        public AudioClip[] Layers = Array.Empty<AudioClip>();

        [Range(0f, 2f)] public float Gain = 1f;
    }

    /// <summary>Data-driven Tier-0 event definition consumed by UnityAudioService.</summary>
    [CreateAssetMenu(fileName = "SND_Event", menuName = "Nova/Audio/Sound Event")]
    public sealed class SoundEventSO : ScriptableObject
    {
        public SoundEventId EventId;
        public AudioCategory Category = AudioCategory.Unit;
        public VoicePriority DefaultPriority = VoicePriority.Normal;
        public SoundVariation[] Variations = Array.Empty<SoundVariation>();

        [Range(1, 4)] public int MaxConcurrent = 4;
        [Min(0f)] public float CooldownSeconds;
        public bool Spatialized = true;
        [Range(0f, 2f)] public float Gain = 1f;
        [Min(0.01f)] public float MinDistance = 15f;
        [Min(0.02f)] public float MaxDistance = 120f;
    }
}
