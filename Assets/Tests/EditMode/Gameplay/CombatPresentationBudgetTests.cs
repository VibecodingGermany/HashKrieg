using System;
using System.Reflection;
using Nova.Core;
using Nova.Gameplay.Audio;
using Nova.Gameplay.CombatFeedback;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using NUnit.Framework;
using UnityEngine;
using EntityId = Nova.Core.EntityId;
using Object = UnityEngine.Object;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Hard presentation budgets from D-090. These tests exercise runtime
    /// objects but never create a simulation or mutate authoritative state.
    /// </summary>
    [TestFixture]
    public sealed class CombatPresentationBudgetTests
    {
        [Test]
        public void EffectControllerDropsTheSixtyFifthCueAndCapsMuzzleLights()
        {
            var root = new GameObject("CombatPresentationBudgetTest");
            try
            {
                CombatEffectController effects = root.AddComponent<CombatEffectController>();
                for (int i = 0; i < CombatEffectController.MaxActiveEffects + 1; i++)
                {
                    effects.Present(new CombatFeedbackEvent(
                        CombatFeedbackKind.Shot,
                        new EntityId(i, 1),
                        EntityId.Invalid,
                        new Vector3(i, 0f, 0f),
                        default,
                        UnitRole.BasicInfantry,
                        UnitRole.Unit,
                        DamageType.Kinetic,
                        hasTargetPosition: false));
                }

                Assert.That(effects.ActiveEffectCount,
                    Is.EqualTo(CombatEffectController.MaxActiveEffects));
                Assert.That(effects.ActiveMuzzleLightCount,
                    Is.EqualTo(CombatEffectController.MaxMuzzleLights));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AudioConstantsReserveMusicInsideTheThirtyTwoVoiceCeiling()
        {
            Assert.That(UnityAudioService.MaxRealVoices, Is.EqualTo(32));
            Assert.That(UnityAudioService.ReservedMusicVoices, Is.EqualTo(2));
            Assert.That(UnityAudioService.MaxOneShotVoices,
                Is.EqualTo(UnityAudioService.MaxRealVoices - UnityAudioService.ReservedMusicVoices));
            Assert.That(UnityAudioService.MaxSpatialVoices,
                Is.LessThanOrEqualTo(UnityAudioService.MaxOneShotVoices));
        }

        [Test]
        public void AudioServiceEnforcesCooldownAndPerKeyConcurrency()
        {
            AudioClip clip = CreateSilentClip();
            SoundEventSO limited = CreateEvent(
                clip,
                SoundEventId.UI_Click,
                maxConcurrent: 3,
                spatialized: false);
            SoundEventSO cooled = CreateEvent(
                clip,
                SoundEventId.UI_Deny,
                cooldownSeconds: 60f,
                spatialized: false);

            using (var fixture = new AudioServiceFixture(clip, limited, cooled))
            {
                var handles = new AudioHandle[4];
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i] = fixture.Service.Play2D(
                        SoundEventId.UI_Click,
                        AudioCategory.EventDefault,
                        VoicePriority.EventDefault);
                    Assert.That(handles[i].IsValid, Is.True);
                }

                Assert.That(fixture.Service.ActiveVoiceCount, Is.EqualTo(3));
                fixture.Service.Stop(handles[0]);
                Assert.That(fixture.Service.ActiveVoiceCount, Is.EqualTo(3),
                    "the fourth instance must have replaced the oldest one atomically");

                AudioHandle firstCooled = fixture.Service.Play2D(
                    SoundEventId.UI_Deny,
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);
                AudioHandle rejectedCooled = fixture.Service.Play2D(
                    SoundEventId.UI_Deny,
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);

                Assert.That(firstCooled.IsValid, Is.True);
                Assert.That(rejectedCooled.IsValid, Is.False,
                    "a cooled-down cue is dropped, never queued for later playback");
                Assert.That(fixture.Service.ActiveVoiceCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void AudioServiceTreatsLayeredVariationAsOneAtomicSpatialInstance()
        {
            AudioClip clip = CreateSilentClip();
            SoundEventSO layered = CreateEvent(
                clip,
                SoundEventId.DTH_Building,
                defaultPriority: VoicePriority.High,
                spatialized: true,
                layerCount: 2);

            using (var fixture = new AudioServiceFixture(clip, layered))
            {
                AudioHandle handle = fixture.Service.Play3D(
                    SoundEventId.DTH_Building,
                    new AudioPosition(Vector3.one),
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);

                Assert.That(handle.IsValid, Is.True);
                Assert.That(fixture.Service.ActiveVoiceCount, Is.EqualTo(2));
                Assert.That(fixture.Service.ActiveSpatialVoiceCount, Is.EqualTo(2));

                fixture.Service.Stop(handle);
                Assert.That(fixture.Service.ActiveVoiceCount, Is.Zero,
                    "stopping the event must release every authored layer together");
                Assert.That(fixture.Service.ActiveSpatialVoiceCount, Is.Zero);
            }
        }

        [Test]
        public void AudioServiceCapsRealVoicesAndStealsOnlyLowerPriorityInstances()
        {
            AudioClip clip = CreateSilentClip();
            SoundEventId[] ids = (SoundEventId[])Enum.GetValues(typeof(SoundEventId));
            var events = new SoundEventSO[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                events[i] = CreateEvent(
                    clip,
                    ids[i],
                    defaultPriority: i == ids.Length - 1
                        ? VoicePriority.High
                        : VoicePriority.Normal,
                    spatialized: false);
            }

            using (var fixture = new AudioServiceFixture(clip, events))
            {
                AudioHandle oldest = AudioHandle.Invalid;
                for (int i = 0; i < UnityAudioService.MaxOneShotVoices; i++)
                {
                    AudioHandle handle = fixture.Service.Play2D(
                        ids[i % 10],
                        AudioCategory.EventDefault,
                        VoicePriority.EventDefault);
                    Assert.That(handle.IsValid, Is.True);
                    if (i == 0) oldest = handle;
                }

                Assert.That(fixture.Service.ActiveVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxOneShotVoices));

                AudioHandle equalPriority = fixture.Service.Play2D(
                    ids[10],
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);
                Assert.That(equalPriority.IsValid, Is.False,
                    "an equal-priority cue must not steal an existing voice");

                AudioHandle higherPriority = fixture.Service.Play2D(
                    ids[11],
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);
                Assert.That(higherPriority.IsValid, Is.True,
                    "the authored High priority must steal the oldest Normal voice");
                Assert.That(fixture.Service.ActiveVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxOneShotVoices));

                fixture.Service.Stop(oldest);
                Assert.That(fixture.Service.ActiveVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxOneShotVoices),
                    "the oldest handle must already have been stolen");
                fixture.Service.Stop(higherPriority);
                Assert.That(fixture.Service.ActiveVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxOneShotVoices - 1));
            }
        }

        [Test]
        public void AudioServiceCapsSpatialVoicesIndependently()
        {
            AudioClip clip = CreateSilentClip();
            SoundEventId[] ids = (SoundEventId[])Enum.GetValues(typeof(SoundEventId));
            var events = new SoundEventSO[9];
            for (int i = 0; i < events.Length; i++)
            {
                events[i] = CreateEvent(
                    clip,
                    ids[i],
                    defaultPriority: i == events.Length - 1
                        ? VoicePriority.High
                        : VoicePriority.Low,
                    spatialized: true);
            }

            using (var fixture = new AudioServiceFixture(clip, events))
            {
                AudioHandle oldest = AudioHandle.Invalid;
                for (int i = 0; i < UnityAudioService.MaxSpatialVoices; i++)
                {
                    AudioHandle handle = fixture.Service.Play3D(
                        ids[i % 8],
                        new AudioPosition(new Vector3(i, 0f, 0f)),
                        AudioCategory.EventDefault,
                        VoicePriority.EventDefault);
                    Assert.That(handle.IsValid, Is.True);
                    if (i == 0) oldest = handle;
                }

                AudioHandle higherPriority = fixture.Service.Play3D(
                    ids[8],
                    new AudioPosition(Vector3.zero),
                    AudioCategory.EventDefault,
                    VoicePriority.EventDefault);

                Assert.That(higherPriority.IsValid, Is.True);
                Assert.That(fixture.Service.ActiveSpatialVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxSpatialVoices));
                Assert.That(fixture.Service.ActiveVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxSpatialVoices));

                fixture.Service.Stop(oldest);
                Assert.That(fixture.Service.ActiveSpatialVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxSpatialVoices),
                    "the oldest low-priority spatial handle must have been stolen");
                fixture.Service.Stop(higherPriority);
                Assert.That(fixture.Service.ActiveSpatialVoiceCount,
                    Is.EqualTo(UnityAudioService.MaxSpatialVoices - 1));
            }
        }

        [Test]
        public void EventDefaultSentinelsCannotCollideWithAuthoredValues()
        {
            Assert.That((byte)AudioCategory.EventDefault, Is.EqualTo(byte.MaxValue));
            Assert.That((byte)VoicePriority.EventDefault, Is.EqualTo(byte.MaxValue));
            Assert.That((byte)AudioCategory.Production, Is.LessThan((byte)AudioCategory.EventDefault));
            Assert.That((byte)VoicePriority.Critical, Is.LessThan((byte)VoicePriority.EventDefault));
        }

        [TestCase(0f, -80f)]
        [TestCase(1f, 0f)]
        public void MixerConversionPinsEndpointDecibels(float linear, float expected)
        {
            Assert.That(UnityAudioService.LinearToDecibels(linear), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void MixerConversionUsesAmplitudeDecibels()
        {
            Assert.That(UnityAudioService.LinearToDecibels(0.5f), Is.EqualTo(-6.0206f).Within(0.001f));
        }

        private static AudioClip CreateSilentClip()
        {
            return AudioClip.Create("Sprint12B_TestTone", 441, 1, 44100, stream: false);
        }

        private static SoundEventSO CreateEvent(
            AudioClip clip,
            SoundEventId id,
            VoicePriority defaultPriority = VoicePriority.Normal,
            int maxConcurrent = 4,
            float cooldownSeconds = 0f,
            bool spatialized = false,
            int layerCount = 1)
        {
            var layers = new AudioClip[layerCount];
            for (int i = 0; i < layers.Length; i++) layers[i] = clip;

            SoundEventSO definition = ScriptableObject.CreateInstance<SoundEventSO>();
            definition.name = $"SND_Test_{id}";
            definition.EventId = id;
            definition.Category = (byte)id <= (byte)SoundEventId.UI_Deny
                ? AudioCategory.Ui
                : AudioCategory.Unit;
            definition.DefaultPriority = defaultPriority;
            definition.Variations = new[]
            {
                new SoundVariation
                {
                    Layers = layers,
                    Gain = 1f,
                },
            };
            definition.MaxConcurrent = maxConcurrent;
            definition.CooldownSeconds = cooldownSeconds;
            definition.Spatialized = spatialized;
            definition.Gain = 1f;
            definition.MinDistance = 15f;
            definition.MaxDistance = 120f;
            return definition;
        }

        private sealed class AudioServiceFixture : IDisposable
        {
            private readonly GameObject _root;
            private readonly AudioClip _clip;
            private readonly SoundEventSO[] _events;

            public UnityAudioService Service { get; }

            public AudioServiceFixture(AudioClip clip, params SoundEventSO[] events)
            {
                _clip = clip;
                _events = events;
                _root = new GameObject("UnityAudioServiceTest");
                Service = _root.AddComponent<UnityAudioService>();

                FieldInfo eventField = typeof(UnityAudioService).GetField(
                    "_events",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(eventField, Is.Not.Null, "missing authored event catalog seam");
                eventField.SetValue(Service, events);
                InvokePrivate(Service, "BuildCatalog");
                InvokePrivate(Service, "BuildVoicePool");
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
                for (int i = 0; i < _events.Length; i++) Object.DestroyImmediate(_events[i]);
                Object.DestroyImmediate(_clip);
            }
        }

        private static object InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                $"missing private test seam {target.GetType().Name}.{methodName}");
            return method.Invoke(target, null);
        }
    }
}
