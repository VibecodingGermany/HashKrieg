using System;
using UnityEngine;

namespace Nova.Gameplay.Audio
{
    /// <summary>Stable Tier-0 sound keys. Names match the authored SND_*.asset files.</summary>
    public enum SoundEventId : byte
    {
        UI_Click,
        UI_Select,
        UI_Ack,
        UI_Deny,
        WPN_Kinetic_Light,
        WPN_Kinetic_Heavy,
        WPN_Explosive,
        IMP_Kinetic,
        IMP_Explosive,
        DTH_Unit,
        DTH_Building,
        PRD_UnitReady,
    }

    public enum AudioCategory : byte
    {
        Ui,
        Weapon,
        Impact,
        Unit,
        Production,
        /// <summary>Resolve routing from the authored SoundEventSO.</summary>
        EventDefault = byte.MaxValue,
    }

    public enum AudioBus : byte
    {
        Master,
        Music,
        Sfx,
        Voice,
        Ambience,
    }

    public enum VoicePriority : byte
    {
        Low,
        Normal,
        High,
        Critical,
        /// <summary>Resolve stealing priority from the authored SoundEventSO.</summary>
        EventDefault = byte.MaxValue,
    }

    /// <summary>Opaque playback token; zero is invalid and safe to stop.</summary>
    public readonly struct AudioHandle : IEquatable<AudioHandle>
    {
        public static readonly AudioHandle Invalid = default;

        internal int Value { get; }
        public bool IsValid => Value > 0;

        internal AudioHandle(int value) => Value = value;

        public bool Equals(AudioHandle other) => Value == other.Value;
        public override bool Equals(object obj) => obj is AudioHandle other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(AudioHandle left, AudioHandle right) => left.Equals(right);
        public static bool operator !=(AudioHandle left, AudioHandle right) => !left.Equals(right);
    }

    /// <summary>
    /// Value object at the Unity boundary. Keeping the position in the audio
    /// contract makes call sites explicit without exposing AudioSource.
    /// </summary>
    public readonly struct AudioPosition
    {
        public Vector3 World { get; }
        public AudioPosition(Vector3 world) => World = world;
    }

    /// <summary>
    /// Backend-neutral sound surface decided by D-039. Gameplay and UI can ask
    /// for sound; only the backend may own sources or touch a mixer.
    /// </summary>
    public interface IAudioService
    {
        AudioHandle Play2D(SoundEventId id, AudioCategory category, VoicePriority priority);
        AudioHandle Play3D(SoundEventId id, AudioPosition position, AudioCategory category, VoicePriority priority);
        void Stop(AudioHandle handle);
        void SetBusVolume(AudioBus bus, float linear01);
    }

    /// <summary>
    /// Scene-independent access point for presentation callers. A missing
    /// backend is deliberately a silent no-op so audio can never break input
    /// or deterministic gameplay.
    /// </summary>
    public static class AudioServiceLocator
    {
        public static IAudioService Current { get; internal set; }

        public static AudioHandle Play2D(
            SoundEventId id,
            AudioCategory category = AudioCategory.EventDefault,
            VoicePriority priority = VoicePriority.EventDefault)
        {
            return Current?.Play2D(id, category, priority) ?? AudioHandle.Invalid;
        }

        public static AudioHandle Play3D(
            SoundEventId id,
            Vector3 world,
            AudioCategory category = AudioCategory.EventDefault,
            VoicePriority priority = VoicePriority.EventDefault)
        {
            return Current?.Play3D(id, new AudioPosition(world), category, priority)
                   ?? AudioHandle.Invalid;
        }
    }
}
