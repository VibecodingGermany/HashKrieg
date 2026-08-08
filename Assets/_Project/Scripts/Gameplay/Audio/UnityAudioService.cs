using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Nova.Gameplay.Audio
{
    /// <summary>
    /// D-039 Tier-0 one-shot backend. It is the only production type allowed
    /// to call PlayOneShot or write exposed mixer parameters. The two existing
    /// music controllers remain an explicit D-090 transition exception and
    /// receive two reserved voices; one-shots therefore keep the project-wide
    /// 32-real/24-spatial ceiling and never queue stale cosmetic work.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnityAudioService : MonoBehaviour, IAudioService
    {
        public const int MaxRealVoices = 32;
        // MenuMusicPlayer and MusicDirector still own the two legacy music
        // sources during the D-090 transition. Reserving both slots keeps the
        // project-wide real-voice promise honest even during their handover.
        public const int ReservedMusicVoices = 2;
        public const int MaxOneShotVoices = MaxRealVoices - ReservedMusicVoices;
        public const int MaxSpatialVoices = 24;
        public const string MasterVolumeParameter = "MasterVolumeDb";
        public const string MusicVolumeParameter = "MusicVolumeDb";
        public const string SfxVolumeParameter = "SFXVolumeDb";
        public const string VoiceVolumeParameter = "VoiceVolumeDb";
        public const string AmbienceVolumeParameter = "AmbienceVolumeDb";

        [Header("Authored catalog")]
        [SerializeField] private SoundEventSO[] _events = Array.Empty<SoundEventSO>();

        [Header("Mixer routing")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _weaponsGroup;
        [SerializeField] private AudioMixerGroup _unitsGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;

        private readonly Dictionary<SoundEventId, SoundEventSO> _catalog =
            new Dictionary<SoundEventId, SoundEventSO>();
        private readonly Dictionary<SoundEventId, float> _lastStartedAt =
            new Dictionary<SoundEventId, float>();
        private readonly List<PlaybackInstance> _active = new List<PlaybackInstance>(MaxOneShotVoices);
        private readonly Stack<Voice> _freeVoices = new Stack<Voice>(MaxOneShotVoices);

        private int _nextHandle = 1;
        private float _sfxLinear = 1f;

        /// <summary>Number of AudioSource voices currently reserved by live event instances.</summary>
        public int ActiveVoiceCount { get; private set; }

        /// <summary>Number of reserved voices using 3D spatialization.</summary>
        public int ActiveSpatialVoiceCount { get; private set; }

        private void Awake()
        {
            BuildCatalog();
            BuildVoicePool();

            if (AudioServiceLocator.Current != null && !ReferenceEquals(AudioServiceLocator.Current, this))
            {
                Debug.LogWarning("[UnityAudioService] Replacing a duplicate audio backend; the newest scene instance wins.");
            }
            AudioServiceLocator.Current = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(AudioServiceLocator.Current, this))
            {
                AudioServiceLocator.Current = null;
            }
            StopAll();
        }

        private void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PlaybackInstance instance = _active[i];
                bool playing = false;
                for (int v = 0; v < instance.Voices.Count; v++)
                {
                    if (instance.Voices[v].Source.isPlaying)
                    {
                        playing = true;
                        break;
                    }
                }
                if (!playing) ReleaseAt(i, stopSources: false);
            }
        }

        public AudioHandle Play2D(SoundEventId id, AudioCategory category, VoicePriority priority)
        {
            return Play(id, default, spatial: false, category, priority);
        }

        public AudioHandle Play3D(
            SoundEventId id,
            AudioPosition position,
            AudioCategory category,
            VoicePriority priority)
        {
            return Play(id, position.World, spatial: true, category, priority);
        }

        public void Stop(AudioHandle handle)
        {
            if (!handle.IsValid) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_active[i].Handle == handle)
                {
                    ReleaseAt(i, stopSources: true);
                    return;
                }
            }
        }

        public void SetBusVolume(AudioBus bus, float linear01)
        {
            float linear = Mathf.Clamp01(linear01);
            if (bus == AudioBus.Sfx) _sfxLinear = linear;

            if (_mixer != null)
            {
                _mixer.SetFloat(ParameterFor(bus), LinearToDecibels(linear));
            }
            else if (bus == AudioBus.Sfx)
            {
                // A scene regenerated before the mixer asset is available is
                // still honest: the setting reaches every live SFX voice.
                for (int i = 0; i < _active.Count; i++)
                {
                    PlaybackInstance instance = _active[i];
                    for (int v = 0; v < instance.Voices.Count; v++)
                    {
                        Voice voice = instance.Voices[v];
                        voice.Source.volume = voice.BaseGain * _sfxLinear;
                    }
                }
            }
        }

        public static float LinearToDecibels(float linear01)
        {
            float linear = Mathf.Clamp01(linear01);
            return linear <= 0f ? -80f : Mathf.Clamp(20f * Mathf.Log10(linear), -80f, 0f);
        }

        private AudioHandle Play(
            SoundEventId id,
            Vector3 position,
            bool spatial,
            AudioCategory requestedCategory,
            VoicePriority requestedPriority)
        {
            if (!_catalog.TryGetValue(id, out SoundEventSO definition) || definition == null)
            {
                return AudioHandle.Invalid;
            }

            float now = Time.unscaledTime;
            if (_lastStartedAt.TryGetValue(id, out float last)
                && now - last < Mathf.Max(0f, definition.CooldownSeconds))
            {
                return AudioHandle.Invalid;
            }

            SoundVariation variation = PickVariation(definition);
            if (variation == null) return AudioHandle.Invalid;

            int requiredVoices = CountValidLayers(variation);
            if (requiredVoices == 0 || requiredVoices > MaxOneShotVoices) return AudioHandle.Invalid;

            // Callers may override exceptional cues (for example a rejected
            // command), while the normal path remains data-driven. Sentinels
            // are resolved before comparisons/routing, so they can never
            // accidentally become the numerically highest priority.
            VoicePriority priority = requestedPriority == VoicePriority.EventDefault
                ? definition.DefaultPriority
                : requestedPriority;
            AudioCategory category = requestedCategory == AudioCategory.EventDefault
                ? definition.Category
                : requestedCategory;
            bool useSpatial = spatial && definition.Spatialized;

            EnforcePerKeyLimit(id, Mathf.Clamp(definition.MaxConcurrent, 1, 4));
            if (!MakeRoom(requiredVoices, useSpatial, priority)) return AudioHandle.Invalid;

            int handleValue = NextHandleValue();
            var instance = new PlaybackInstance(
                new AudioHandle(handleValue), id, priority, useSpatial, now, requiredVoices);

            // Reserve and configure every layer before starting any of them.
            // A layered building death is therefore all-or-nothing.
            for (int i = 0; i < variation.Layers.Length; i++)
            {
                AudioClip clip = variation.Layers[i];
                if (clip == null) continue;

                Voice voice = _freeVoices.Pop();
                ConfigureVoice(voice, definition, category, priority, useSpatial, position,
                    Mathf.Max(0f, definition.Gain) * Mathf.Max(0f, variation.Gain));
                voice.Source.clip = null;
                instance.Voices.Add(voice);
            }

            _active.Add(instance);
            ActiveVoiceCount += instance.Voices.Count;
            if (useSpatial) ActiveSpatialVoiceCount += instance.Voices.Count;
            _lastStartedAt[id] = now;

            for (int i = 0; i < instance.Voices.Count; i++)
            {
                AudioClip clip = NextValidLayer(variation, i);
                instance.Voices[i].Source.PlayOneShot(clip, 1f);
            }
            return instance.Handle;
        }

        private void BuildCatalog()
        {
            _catalog.Clear();
            for (int i = 0; i < _events.Length; i++)
            {
                SoundEventSO definition = _events[i];
                if (definition == null) continue;
                if (_catalog.ContainsKey(definition.EventId))
                {
                    Debug.LogWarning($"[UnityAudioService] Duplicate event {definition.EventId}; last asset wins.");
                }
                _catalog[definition.EventId] = definition;
            }
        }

        private void BuildVoicePool()
        {
            if (_freeVoices.Count > 0) return;
            for (int i = 0; i < MaxOneShotVoices; i++)
            {
                var voiceObject = new GameObject($"AudioVoice_{i:00}");
                voiceObject.transform.SetParent(transform, false);
                var source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.dopplerLevel = 0f;
                _freeVoices.Push(new Voice(source));
            }
        }

        private void ConfigureVoice(
            Voice voice,
            SoundEventSO definition,
            AudioCategory category,
            VoicePriority priority,
            bool spatial,
            Vector3 position,
            float baseGain)
        {
            AudioSource source = voice.Source;
            source.Stop();
            source.transform.position = position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.priority = SourcePriority(priority);
            source.minDistance = Mathf.Max(0.01f, definition.MinDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.01f, definition.MaxDistance);
            source.outputAudioMixerGroup = GroupFor(category);
            voice.BaseGain = baseGain;
            source.volume = _mixer == null ? baseGain * _sfxLinear : baseGain;
        }

        private void EnforcePerKeyLimit(SoundEventId id, int maxConcurrent)
        {
            while (CountInstances(id) >= maxConcurrent)
            {
                int oldest = FindOldestInstance(id);
                if (oldest < 0) return;
                ReleaseAt(oldest, stopSources: true);
            }
        }

        private bool MakeRoom(int requiredVoices, bool spatial, VoicePriority priority)
        {
            while (_freeVoices.Count < requiredVoices
                   || (spatial && ActiveSpatialVoiceCount + requiredVoices > MaxSpatialVoices))
            {
                // A spatial instance must be stolen only when the spatial
                // budget itself is full. If the shared 32-voice pool is full,
                // an older lower-priority 2D voice is an equally valid victim.
                bool spatialBudgetFull = spatial
                                         && ActiveSpatialVoiceCount + requiredVoices > MaxSpatialVoices;
                int candidate = FindOldestLowerPriority(priority, spatialOnly: spatialBudgetFull);
                if (candidate < 0) return false;
                ReleaseAt(candidate, stopSources: true);
            }
            return true;
        }

        private int FindOldestLowerPriority(VoicePriority priority, bool spatialOnly)
        {
            int candidate = -1;
            float oldest = float.PositiveInfinity;
            for (int i = 0; i < _active.Count; i++)
            {
                PlaybackInstance instance = _active[i];
                if (instance.Priority >= priority) continue;
                if (spatialOnly && !instance.Spatial) continue;
                if (instance.StartedAt >= oldest) continue;
                oldest = instance.StartedAt;
                candidate = i;
            }
            return candidate;
        }

        private int FindOldestInstance(SoundEventId id)
        {
            int candidate = -1;
            float oldest = float.PositiveInfinity;
            for (int i = 0; i < _active.Count; i++)
            {
                PlaybackInstance instance = _active[i];
                if (instance.EventId != id || instance.StartedAt >= oldest) continue;
                oldest = instance.StartedAt;
                candidate = i;
            }
            return candidate;
        }

        private int CountInstances(SoundEventId id)
        {
            int count = 0;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i].EventId == id) count++;
            }
            return count;
        }

        private void ReleaseAt(int index, bool stopSources)
        {
            PlaybackInstance instance = _active[index];
            for (int i = 0; i < instance.Voices.Count; i++)
            {
                Voice voice = instance.Voices[i];
                if (stopSources) voice.Source.Stop();
                voice.Source.clip = null;
                voice.Source.outputAudioMixerGroup = null;
                voice.Source.spatialBlend = 0f;
                _freeVoices.Push(voice);
            }

            ActiveVoiceCount -= instance.Voices.Count;
            if (instance.Spatial) ActiveSpatialVoiceCount -= instance.Voices.Count;
            _active.RemoveAt(index);
        }

        private void StopAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ReleaseAt(i, stopSources: true);
            }
        }

        private AudioMixerGroup GroupFor(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ui: return _uiGroup != null ? _uiGroup : _sfxGroup;
                case AudioCategory.Weapon: return _weaponsGroup != null ? _weaponsGroup : _sfxGroup;
                case AudioCategory.Impact:
                case AudioCategory.Unit:
                case AudioCategory.Production:
                    return _unitsGroup != null ? _unitsGroup : _sfxGroup;
                default: return _sfxGroup;
            }
        }

        private static SoundVariation PickVariation(SoundEventSO definition)
        {
            if (definition.Variations == null || definition.Variations.Length == 0) return null;
            int start = UnityEngine.Random.Range(0, definition.Variations.Length);
            for (int i = 0; i < definition.Variations.Length; i++)
            {
                SoundVariation candidate = definition.Variations[(start + i) % definition.Variations.Length];
                if (candidate != null && CountValidLayers(candidate) > 0) return candidate;
            }
            return null;
        }

        private static int CountValidLayers(SoundVariation variation)
        {
            if (variation?.Layers == null) return 0;
            int count = 0;
            for (int i = 0; i < variation.Layers.Length; i++)
            {
                if (variation.Layers[i] != null) count++;
            }
            return count;
        }

        private static AudioClip NextValidLayer(SoundVariation variation, int validIndex)
        {
            int found = 0;
            for (int i = 0; i < variation.Layers.Length; i++)
            {
                if (variation.Layers[i] == null) continue;
                if (found == validIndex) return variation.Layers[i];
                found++;
            }
            return null;
        }

        private int NextHandleValue()
        {
            if (_nextHandle == int.MaxValue) _nextHandle = 1;
            return _nextHandle++;
        }

        private static string ParameterFor(AudioBus bus)
        {
            switch (bus)
            {
                case AudioBus.Master: return MasterVolumeParameter;
                case AudioBus.Music: return MusicVolumeParameter;
                case AudioBus.Sfx: return SfxVolumeParameter;
                case AudioBus.Voice: return VoiceVolumeParameter;
                case AudioBus.Ambience: return AmbienceVolumeParameter;
                default: return SfxVolumeParameter;
            }
        }

        private static int SourcePriority(VoicePriority priority)
        {
            // Unity uses 0 as highest and 256 as lowest. Match the authored
            // event priority so hardware virtualization cannot invert the
            // service's own stealing decision.
            switch (priority)
            {
                case VoicePriority.Critical: return 0;
                case VoicePriority.High: return 64;
                case VoicePriority.Normal: return 128;
                default: return 200;
            }
        }

        private sealed class Voice
        {
            public AudioSource Source { get; }
            public float BaseGain;
            public Voice(AudioSource source) => Source = source;
        }

        private sealed class PlaybackInstance
        {
            public AudioHandle Handle { get; }
            public SoundEventId EventId { get; }
            public VoicePriority Priority { get; }
            public bool Spatial { get; }
            public float StartedAt { get; }
            public List<Voice> Voices { get; }

            public PlaybackInstance(
                AudioHandle handle,
                SoundEventId eventId,
                VoicePriority priority,
                bool spatial,
                float startedAt,
                int layerCapacity)
            {
                Handle = handle;
                EventId = eventId;
                Priority = priority;
                Spatial = spatial;
                StartedAt = startedAt;
                Voices = new List<Voice>(layerCapacity);
            }
        }
    }
}
