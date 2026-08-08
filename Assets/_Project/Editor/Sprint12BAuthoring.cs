using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nova.Gameplay.Audio;
using Nova.Gameplay.CombatFeedback;
using Nova.Gameplay.Match;
using Nova.Presentation;
using Nova.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Nova.Editor
{
    /// <summary>
    /// Deterministic Unity authoring pass for Sprint 12B. The repository pins
    /// Unity 6000.5.4f1, so the small reflected mixer surface below is treated
    /// like a versioned build tool: every expected type/member is checked and
    /// a changed editor API fails loudly instead of emitting incomplete YAML.
    /// </summary>
    public static class Sprint12BAuthoring
    {
        public const string MixerPath = "Assets/_Project/Audio/Mixer/MIX_Master.mixer";
        public const string EventsFolder = "Assets/_Project/Audio/Events";
        public const string SfxRoot = "Assets/_Project/Audio/Sfx/Kenney";

        private const string SciFiRoot = SfxRoot + "/SciFi";
        private const string ImpactRoot = SfxRoot + "/Impact";
        private const string InterfaceRoot = SfxRoot + "/Interface";

        private static readonly string[] ExpectedSfxPaths =
        {
            SciFiRoot + "/explosionCrunch_000.ogg",
            SciFiRoot + "/explosionCrunch_001.ogg",
            SciFiRoot + "/explosionCrunch_002.ogg",
            SciFiRoot + "/laserLarge_000.ogg",
            SciFiRoot + "/laserLarge_001.ogg",
            SciFiRoot + "/laserLarge_002.ogg",
            SciFiRoot + "/laserSmall_000.ogg",
            SciFiRoot + "/laserSmall_001.ogg",
            SciFiRoot + "/laserSmall_002.ogg",
            SciFiRoot + "/lowFrequency_explosion_000.ogg",
            SciFiRoot + "/lowFrequency_explosion_001.ogg",

            ImpactRoot + "/impactMetal_heavy_000.ogg",
            ImpactRoot + "/impactMetal_heavy_001.ogg",
            ImpactRoot + "/impactMetal_heavy_002.ogg",
            ImpactRoot + "/impactMetal_light_000.ogg",
            ImpactRoot + "/impactMetal_light_001.ogg",
            ImpactRoot + "/impactMetal_light_002.ogg",
            ImpactRoot + "/impactMetal_medium_000.ogg",
            ImpactRoot + "/impactMetal_medium_001.ogg",
            ImpactRoot + "/impactMetal_medium_002.ogg",
            ImpactRoot + "/impactPlate_heavy_000.ogg",
            ImpactRoot + "/impactPlate_heavy_001.ogg",

            InterfaceRoot + "/click_001.ogg",
            InterfaceRoot + "/click_002.ogg",
            InterfaceRoot + "/click_003.ogg",
            InterfaceRoot + "/confirmation_001.ogg",
            InterfaceRoot + "/confirmation_002.ogg",
            InterfaceRoot + "/confirmation_003.ogg",
            InterfaceRoot + "/confirmation_004.ogg",
            InterfaceRoot + "/error_001.ogg",
            InterfaceRoot + "/error_002.ogg",
            InterfaceRoot + "/error_003.ogg",
            InterfaceRoot + "/select_001.ogg",
            InterfaceRoot + "/select_002.ogg",
            InterfaceRoot + "/select_003.ogg",
        };

        private static readonly SoundEventId[] EventOrder =
        {
            SoundEventId.UI_Click,
            SoundEventId.UI_Select,
            SoundEventId.UI_Ack,
            SoundEventId.UI_Deny,
            SoundEventId.WPN_Kinetic_Light,
            SoundEventId.WPN_Kinetic_Heavy,
            SoundEventId.WPN_Explosive,
            SoundEventId.IMP_Kinetic,
            SoundEventId.IMP_Explosive,
            SoundEventId.DTH_Unit,
            SoundEventId.DTH_Building,
            SoundEventId.PRD_UnitReady,
        };

        [MenuItem("Tools/Project Nova/Author Sprint 12B Assets")]
        public static void GenerateAll()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureSfxImporters();
            AudioMixer mixer = EnsureMixer();
            EnsureSoundEvents();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // Bootstrap.unity is generated output. Calling its generator is the
            // only supported way to persist the new backend and mixer wiring.
            BootstrapSceneGenerator.CreateBootstrapScene();
            ValidateAuthoredAssets(mixer);
            AssetDatabase.SaveAssets();

            Debug.Log("[Sprint12BAuthoring] Authored and validated 35 SFX clips, " +
                      "MIX_Master, 12 sound events and Bootstrap scene wiring.");
        }

        /// <summary>Loads the event catalog in stable enum order for scene generation.</summary>
        internal static SoundEventSO[] LoadSoundEvents()
        {
            var result = new SoundEventSO[EventOrder.Length];
            for (int i = 0; i < EventOrder.Length; i++)
            {
                string path = EventPath(EventOrder[i]);
                result[i] = AssetDatabase.LoadAssetAtPath<SoundEventSO>(path);
                if (result[i] == null)
                {
                    Debug.LogError($"[Sprint12BAuthoring] Missing SoundEventSO at {path}.");
                }
            }
            return result;
        }

        private static void ConfigureSfxImporters()
        {
            string[] discovered = Directory.GetFiles(SfxRoot, "*.ogg", SearchOption.AllDirectories)
                .Select(ToProjectPath)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] expected = ExpectedSfxPaths.OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (!discovered.SequenceEqual(expected, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Sprint 12B SFX set must contain exactly the 35 reviewed OGG files; " +
                    $"found {discovered.Length}.\nExpected:\n{string.Join("\n", expected)}\n" +
                    $"Found:\n{string.Join("\n", discovered)}");
            }

            for (int i = 0; i < ExpectedSfxPaths.Length; i++)
            {
                string path = ExpectedSfxPaths[i];
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"{path} did not import through AudioImporter.");
                }

                bool ui = path.StartsWith(InterfaceRoot + "/", StringComparison.Ordinal);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.70f;
                settings.preloadAudioData = true;
                settings.sampleRateSetting = ui
                    ? AudioSampleRateSetting.OverrideSampleRate
                    : AudioSampleRateSetting.PreserveSampleRate;
                settings.sampleRateOverride = ui ? 22050u : 0u;

                importer.defaultSampleSettings = settings;
                importer.forceToMono = !ui;
                importer.loadInBackground = false;
                importer.ambisonic = false;
                importer.SaveAndReimport();
            }
        }

        private static AudioMixer EnsureMixer()
        {
            EnsureFolder(Path.GetDirectoryName(MixerPath)?.Replace('\\', '/'));

            MixerReflection api = MixerReflection.Load();
            Object controller = AssetDatabase.LoadAssetAtPath(MixerPath, api.ControllerType);
            if (controller == null)
            {
                controller = api.CreateMixer(MixerPath);
                if (controller == null)
                {
                    throw new InvalidOperationException("CreateMixerControllerAtPath returned null.");
                }
            }

            AudioMixer mixer = controller as AudioMixer;
            if (mixer == null)
            {
                throw new InvalidOperationException($"{MixerPath} is not an AudioMixer.");
            }

            object master = api.MasterGroup(controller);
            object music = api.EnsureChild(controller, master, "Music");
            object sfx = api.EnsureChild(controller, master, "SFX");
            object voice = api.EnsureChild(controller, master, "Voice");
            object ambience = api.EnsureChild(controller, master, "Ambience");
            api.EnsureChild(controller, sfx, "SFX_Weapons");
            api.EnsureChild(controller, sfx, "SFX_Units");
            api.EnsureChild(controller, sfx, "UI");
            api.EnsureChild(controller, voice, "Voice_Commander");
            api.EnsureChild(controller, voice, "Voice_Barks");

            api.EnsureExposedVolume(controller, master, UnityAudioService.MasterVolumeParameter);
            api.EnsureExposedVolume(controller, music, UnityAudioService.MusicVolumeParameter);
            api.EnsureExposedVolume(controller, sfx, UnityAudioService.SfxVolumeParameter);
            api.EnsureExposedVolume(controller, voice, UnityAudioService.VoiceVolumeParameter);
            api.EnsureExposedVolume(controller, ambience, UnityAudioService.AmbienceVolumeParameter);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return mixer;
        }

        private static void EnsureSoundEvents()
        {
            EnsureFolder(EventsFolder);
            foreach (EventSpec spec in BuildEventSpecs())
            {
                string path = EventPath(spec.Id);
                Object existingMain = AssetDatabase.LoadMainAssetAtPath(path);
                if (existingMain != null && !(existingMain is SoundEventSO))
                {
                    throw new InvalidOperationException(
                        $"Refusing to replace non-SoundEvent asset at {path} ({existingMain.GetType().Name}).");
                }

                SoundEventSO asset = existingMain as SoundEventSO;
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<SoundEventSO>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.EventId = spec.Id;
                asset.Category = spec.Category;
                asset.DefaultPriority = spec.Priority;
                asset.MaxConcurrent = spec.MaxConcurrent;
                asset.CooldownSeconds = spec.Cooldown;
                asset.Spatialized = spec.Spatialized;
                asset.Gain = spec.Gain;
                asset.MinDistance = 15f;
                asset.MaxDistance = 120f;
                asset.Variations = new SoundVariation[spec.LayerPaths.Length];

                for (int variationIndex = 0; variationIndex < spec.LayerPaths.Length; variationIndex++)
                {
                    string[] layerPaths = spec.LayerPaths[variationIndex];
                    var variation = new SoundVariation
                    {
                        Gain = 1f,
                        Layers = new AudioClip[layerPaths.Length],
                    };
                    for (int layerIndex = 0; layerIndex < layerPaths.Length; layerIndex++)
                    {
                        variation.Layers[layerIndex] = LoadRequiredClip(layerPaths[layerIndex]);
                    }
                    asset.Variations[variationIndex] = variation;
                }

                EditorUtility.SetDirty(asset);
            }
        }

        private static IEnumerable<EventSpec> BuildEventSpecs()
        {
            yield return SingleLayer(SoundEventId.UI_Click, AudioCategory.Ui, VoicePriority.Normal,
                false, 4, 0.03f, 0.75f, InterfaceRoot, "click", 1, 2, 3);
            yield return SingleLayer(SoundEventId.UI_Select, AudioCategory.Ui, VoicePriority.Normal,
                false, 3, 0.08f, 0.80f, InterfaceRoot, "select", 1, 2, 3);
            yield return SingleLayer(SoundEventId.UI_Ack, AudioCategory.Ui, VoicePriority.Normal,
                false, 3, 0.12f, 0.85f, InterfaceRoot, "confirmation", 1, 2);
            yield return SingleLayer(SoundEventId.UI_Deny, AudioCategory.Ui, VoicePriority.High,
                false, 3, 0.15f, 0.85f, InterfaceRoot, "error", 1, 2, 3);
            yield return SingleLayer(SoundEventId.WPN_Kinetic_Light, AudioCategory.Weapon, VoicePriority.Low,
                true, 4, 0.02f, 0.80f, SciFiRoot, "laserSmall", 0, 1, 2);
            yield return SingleLayer(SoundEventId.WPN_Kinetic_Heavy, AudioCategory.Weapon, VoicePriority.Normal,
                true, 4, 0.03f, 0.85f, SciFiRoot, "laserLarge", 0, 1, 2);
            yield return SingleLayer(SoundEventId.WPN_Explosive, AudioCategory.Weapon, VoicePriority.Normal,
                true, 3, 0.06f, 0.90f, SciFiRoot, "explosionCrunch", 0, 1, 2);
            yield return SingleLayer(SoundEventId.IMP_Kinetic, AudioCategory.Impact, VoicePriority.Low,
                true, 4, 0.02f, 0.75f, ImpactRoot, "impactMetal_light", 0, 1, 2);
            yield return SingleLayer(SoundEventId.IMP_Explosive, AudioCategory.Impact, VoicePriority.Normal,
                true, 4, 0.04f, 0.85f, ImpactRoot, "impactMetal_heavy", 0, 1, 2);
            yield return SingleLayer(SoundEventId.DTH_Unit, AudioCategory.Unit, VoicePriority.High,
                true, 3, 0.10f, 0.90f, ImpactRoot, "impactMetal_medium", 0, 1, 2);

            yield return new EventSpec(
                SoundEventId.DTH_Building,
                AudioCategory.Unit,
                VoicePriority.High,
                spatialized: true,
                maxConcurrent: 3,
                cooldown: 0.20f,
                gain: 0.95f,
                new[]
                {
                    new[]
                    {
                        SciFiRoot + "/lowFrequency_explosion_000.ogg",
                        ImpactRoot + "/impactPlate_heavy_000.ogg",
                    },
                    new[]
                    {
                        SciFiRoot + "/lowFrequency_explosion_001.ogg",
                        ImpactRoot + "/impactPlate_heavy_001.ogg",
                    },
                });

            yield return SingleLayer(SoundEventId.PRD_UnitReady, AudioCategory.Production, VoicePriority.High,
                false, 3, 0.20f, 0.85f, InterfaceRoot, "confirmation", 3, 4);
        }

        private static EventSpec SingleLayer(
            SoundEventId id,
            AudioCategory category,
            VoicePriority priority,
            bool spatialized,
            int maxConcurrent,
            float cooldown,
            float gain,
            string root,
            string stem,
            params int[] indices)
        {
            string[][] layers = indices
                .Select(index => new[] { $"{root}/{stem}_{index:000}.ogg" })
                .ToArray();
            return new EventSpec(id, category, priority, spatialized, maxConcurrent, cooldown, gain, layers);
        }

        private static AudioClip LoadRequiredClip(string path)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new InvalidOperationException($"Missing imported AudioClip at {path}.");
            return clip;
        }

        private static string EventPath(SoundEventId id) => $"{EventsFolder}/SND_{id}.asset";

        private static void ValidateAuthoredAssets(AudioMixer mixer)
        {
            MixerReflection api = MixerReflection.Load();
            Object controller = AssetDatabase.LoadAssetAtPath(MixerPath, api.ControllerType);
            if (controller == null || mixer == null) throw new InvalidOperationException("Authored mixer is missing.");
            api.ValidateTopologyAndExposedParameters(controller);
            ValidateImporters();
            ValidateSoundEvents();

            Scene scene = EditorSceneManager.OpenScene(BootstrapSceneGenerator.ScenePath, OpenSceneMode.Single);
            Component[] all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Component>(includeInactive: true))
                .ToArray();

            AudioListener[] listeners = all.OfType<AudioListener>().ToArray();
            UnityAudioService[] services = all.OfType<UnityAudioService>().ToArray();
            SfxSettingsBridge[] bridges = all.OfType<SfxSettingsBridge>().ToArray();
            CombatEffectController[] effects = all.OfType<CombatEffectController>().ToArray();
            if (listeners.Length != 1 || services.Length != 1 || bridges.Length != 1 || effects.Length != 1)
            {
                throw new InvalidOperationException(
                    "Bootstrap scene must contain exactly one AudioListener, UnityAudioService, " +
                    $"SfxSettingsBridge and CombatEffectController; found {listeners.Length}/" +
                    $"{services.Length}/{bridges.Length}/{effects.Length}.");
            }

            ValidateServiceWiring(services[0], mixer);
            UnitViewManager views = all.OfType<UnitViewManager>().Single();
            SerializedProperty combatEffects = new SerializedObject(views).FindProperty("_combatEffects");
            if (combatEffects?.objectReferenceValue != effects[0])
            {
                throw new InvalidOperationException("UnitViewManager is not wired to the scene CombatEffectController.");
            }

            AudioMixerGroup musicGroup = ExactGroup(mixer, "Music");
            AudioSource menuMusic = all.OfType<MenuMusicPlayer>().Single().GetComponent<AudioSource>();
            AudioSource ingameMusic = all.OfType<MusicDirector>().Single().GetComponent<AudioSource>();
            if (menuMusic.outputAudioMixerGroup != musicGroup || ingameMusic.outputAudioMixerGroup != musicGroup)
            {
                throw new InvalidOperationException("Menu and in-game music are not routed to the Music mixer group.");
            }
        }

        private static void ValidateImporters()
        {
            for (int i = 0; i < ExpectedSfxPaths.Length; i++)
            {
                string path = ExpectedSfxPaths[i];
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) throw new InvalidOperationException($"Missing AudioImporter for {path}.");
                bool ui = path.StartsWith(InterfaceRoot + "/", StringComparison.Ordinal);
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                bool valid = importer.forceToMono == !ui
                             && !importer.loadInBackground
                             && !importer.ambisonic
                             && settings.loadType == AudioClipLoadType.DecompressOnLoad
                             && settings.compressionFormat == AudioCompressionFormat.Vorbis
                             && settings.preloadAudioData
                             && Mathf.Abs(settings.quality - 0.70f) < 0.001f
                             && settings.sampleRateSetting == (ui
                                 ? AudioSampleRateSetting.OverrideSampleRate
                                 : AudioSampleRateSetting.PreserveSampleRate)
                             && (!ui || settings.sampleRateOverride == 22050u);
                if (!valid) throw new InvalidOperationException($"Unexpected Sprint 12B import settings at {path}.");
            }
        }

        private static void ValidateSoundEvents()
        {
            var ids = new HashSet<SoundEventId>();
            foreach (EventSpec spec in BuildEventSpecs())
            {
                SoundEventSO asset = AssetDatabase.LoadAssetAtPath<SoundEventSO>(EventPath(spec.Id));
                if (asset == null || asset.EventId != spec.Id || !ids.Add(asset.EventId))
                {
                    throw new InvalidOperationException($"Missing, mismatched or duplicate event asset for {spec.Id}.");
                }
                if (asset.Category != spec.Category
                    || asset.DefaultPriority != spec.Priority
                    || asset.Spatialized != spec.Spatialized
                    || asset.MaxConcurrent != spec.MaxConcurrent
                    || Mathf.Abs(asset.CooldownSeconds - spec.Cooldown) > 0.0001f
                    || Mathf.Abs(asset.Gain - spec.Gain) > 0.0001f
                    || Mathf.Abs(asset.MinDistance - 15f) > 0.0001f
                    || Mathf.Abs(asset.MaxDistance - 120f) > 0.0001f
                    || asset.Variations == null
                    || asset.Variations.Length != spec.LayerPaths.Length)
                {
                    throw new InvalidOperationException($"Invalid authored contract in {EventPath(spec.Id)}.");
                }
                for (int i = 0; i < asset.Variations.Length; i++)
                {
                    SoundVariation variation = asset.Variations[i];
                    string[] expectedLayers = spec.LayerPaths[i];
                    if (variation?.Layers == null
                        || variation.Layers.Length != expectedLayers.Length
                        || Mathf.Abs(variation.Gain - 1f) > 0.0001f)
                    {
                        throw new InvalidOperationException($"Invalid variation {i} in {EventPath(spec.Id)}.");
                    }
                    for (int layer = 0; layer < variation.Layers.Length; layer++)
                    {
                        string actualPath = AssetDatabase.GetAssetPath(variation.Layers[layer]);
                        if (!string.Equals(actualPath, expectedLayers[layer], StringComparison.Ordinal))
                        {
                            throw new InvalidOperationException(
                                $"Unexpected layer {i}/{layer} in {EventPath(spec.Id)}: " +
                                $"expected {expectedLayers[layer]}, found {actualPath}.");
                        }
                    }
                }
            }
            if (ids.Count != EventOrder.Length) throw new InvalidOperationException("Sound-event catalog is incomplete.");
        }

        private static void ValidateServiceWiring(UnityAudioService service, AudioMixer mixer)
        {
            var serialized = new SerializedObject(service);
            SerializedProperty events = serialized.FindProperty("_events");
            if (events == null || events.arraySize != EventOrder.Length)
            {
                throw new InvalidOperationException("UnityAudioService catalog wiring is incomplete.");
            }
            for (int i = 0; i < events.arraySize; i++)
            {
                Object expected = AssetDatabase.LoadAssetAtPath<SoundEventSO>(EventPath(EventOrder[i]));
                Object actual = events.GetArrayElementAtIndex(i).objectReferenceValue;
                if (actual != expected)
                {
                    throw new InvalidOperationException(
                        $"UnityAudioService event slot {i} must be {EventOrder[i]}; " +
                        $"found {(actual == null ? "null" : actual.name)}.");
                }
            }

            RequireReference(serialized, "_mixer", mixer);
            RequireReference(serialized, "_sfxGroup", ExactGroup(mixer, "SFX"));
            RequireReference(serialized, "_weaponsGroup", ExactGroup(mixer, "SFX_Weapons"));
            RequireReference(serialized, "_unitsGroup", ExactGroup(mixer, "SFX_Units"));
            RequireReference(serialized, "_uiGroup", ExactGroup(mixer, "UI"));
        }

        private static void RequireReference(SerializedObject serialized, string name, Object expected)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property?.objectReferenceValue != expected)
            {
                throw new InvalidOperationException($"{serialized.targetObject.name}.{name} is not wired correctly.");
            }
        }

        internal static AudioMixerGroup ExactGroup(AudioMixer mixer, string name)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] matches = mixer.FindMatchingGroups(name)
                .Where(group => group != null && group.name == name)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Mixer group '{name}' must resolve exactly once; found {matches.Length}.");
            }
            return matches[0];
        }

        private static string ToProjectPath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Cannot create Unity asset folder '{path}'.");
            }
            AssetDatabase.CreateFolder(parent, name);
        }

        private sealed class EventSpec
        {
            public SoundEventId Id { get; }
            public AudioCategory Category { get; }
            public VoicePriority Priority { get; }
            public bool Spatialized { get; }
            public int MaxConcurrent { get; }
            public float Cooldown { get; }
            public float Gain { get; }
            public string[][] LayerPaths { get; }

            public EventSpec(
                SoundEventId id,
                AudioCategory category,
                VoicePriority priority,
                bool spatialized,
                int maxConcurrent,
                float cooldown,
                float gain,
                string[][] layerPaths)
            {
                Id = id;
                Category = category;
                Priority = priority;
                Spatialized = spatialized;
                MaxConcurrent = maxConcurrent;
                Cooldown = cooldown;
                Gain = gain;
                LayerPaths = layerPaths;
            }
        }

        /// <summary>
        /// Narrow adapter for Unity's editor-only mixer controller. Public
        /// runtime APIs can route and set parameters but cannot author mixer
        /// groups, so this version-pinned reflection is preferable to editing
        /// opaque .mixer YAML by hand.
        /// </summary>
        private sealed class MixerReflection
        {
            private const BindingFlags InstanceFlags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private const BindingFlags StaticFlags =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            public Type ControllerType { get; private set; }
            private Type GroupType { get; set; }
            private Type GroupParameterPathType { get; set; }
            private Type GuidType { get; set; }
            private MethodInfo CreateMixerMethod { get; set; }
            private MethodInfo CreateGroupMethod { get; set; }
            private MethodInfo AddChildMethod { get; set; }
            private MethodInfo AddExposedMethod { get; set; }
            private MethodInfo ContainsExposedMethod { get; set; }
            private MethodInfo GetVolumeGuidMethod { get; set; }
            private PropertyInfo MasterGroupProperty { get; set; }
            private PropertyInfo ChildrenProperty { get; set; }
            private PropertyInfo ExposedParametersProperty { get; set; }
            private ConstructorInfo GroupParameterPathConstructor { get; set; }
            private FieldInfo ExposedGuidField { get; set; }
            private FieldInfo ExposedNameField { get; set; }

            public static MixerReflection Load()
            {
                Assembly editorAssembly = typeof(AudioImporter).Assembly;
                var api = new MixerReflection
                {
                    ControllerType = RequireType(editorAssembly, "UnityEditor.Audio.AudioMixerController"),
                    GroupType = RequireType(editorAssembly, "UnityEditor.Audio.AudioMixerGroupController"),
                    GroupParameterPathType = RequireType(editorAssembly, "UnityEditor.Audio.AudioGroupParameterPath"),
                };
                api.GuidType = typeof(UnityEngine.GUID);
                Type exposedType = RequireType(editorAssembly, "UnityEditor.Audio.ExposedAudioParameter");
                Type parameterPathBase = RequireType(editorAssembly, "UnityEditor.Audio.AudioParameterPath");

                api.CreateMixerMethod = RequireMethod(
                    api.ControllerType, "CreateMixerControllerAtPath", StaticFlags, typeof(string));
                api.CreateGroupMethod = RequireMethod(
                    api.ControllerType, "CreateNewGroup", InstanceFlags, typeof(string), typeof(bool));
                api.AddChildMethod = RequireMethod(
                    api.ControllerType, "AddChildToParent", InstanceFlags, api.GroupType, api.GroupType);
                api.AddExposedMethod = RequireMethod(
                    api.ControllerType, "AddExposedParameter", InstanceFlags, parameterPathBase);
                api.ContainsExposedMethod = RequireMethod(
                    api.ControllerType, "ContainsExposedParameter", InstanceFlags, api.GuidType);
                api.GetVolumeGuidMethod = RequireMethod(
                    api.GroupType, "GetGUIDForVolume", InstanceFlags);
                api.MasterGroupProperty = RequireProperty(api.ControllerType, "masterGroup");
                api.ChildrenProperty = RequireProperty(api.GroupType, "children");
                api.ExposedParametersProperty = RequireProperty(api.ControllerType, "exposedParameters");
                api.GroupParameterPathConstructor = api.GroupParameterPathType.GetConstructor(
                    InstanceFlags, null, new[] { api.GroupType, api.GuidType }, null)
                    ?? throw new MissingMethodException(
                        api.GroupParameterPathType.FullName,
                        ".ctor(AudioMixerGroupController, GUID)");
                api.ExposedGuidField = exposedType.GetField("guid", InstanceFlags)
                    ?? throw new MissingFieldException(exposedType.FullName, "guid");
                api.ExposedNameField = exposedType.GetField("name", InstanceFlags)
                    ?? throw new MissingFieldException(exposedType.FullName, "name");
                return api;
            }

            public Object CreateMixer(string path)
            {
                return Invoke(CreateMixerMethod, null, path) as Object;
            }

            public object MasterGroup(Object controller)
            {
                return MasterGroupProperty.GetValue(controller)
                       ?? throw new InvalidOperationException("Audio mixer has no master group.");
            }

            public object EnsureChild(Object controller, object parent, string name)
            {
                object[] children = Children(parent);
                object[] exact = children.Where(child => ((Object)child).name == name).ToArray();
                if (exact.Length > 1)
                {
                    throw new InvalidOperationException($"Mixer parent '{((Object)parent).name}' has duplicate child '{name}'.");
                }
                if (exact.Length == 1) return exact[0];

                // Refuse a same-named group elsewhere. A duplicate name makes
                // runtime FindMatchingGroups ambiguous and should be repaired
                // intentionally, never silently reparented by this pass.
                AudioMixer mixer = controller as AudioMixer;
                if (mixer != null && mixer.FindMatchingGroups(name).Any(group => group.name == name))
                {
                    throw new InvalidOperationException(
                        $"Mixer group '{name}' exists under the wrong parent; refusing implicit reparenting.");
                }

                object child = Invoke(CreateGroupMethod, controller, name, false)
                               ?? throw new InvalidOperationException($"CreateNewGroup('{name}') returned null.");
                Invoke(AddChildMethod, controller, child, parent);
                EditorUtility.SetDirty((Object)child);
                EditorUtility.SetDirty((Object)parent);
                return child;
            }

            public void EnsureExposedVolume(Object controller, object group, string desiredName)
            {
                object guid = Invoke(GetVolumeGuidMethod, group)
                              ?? throw new InvalidOperationException(
                                  $"Group '{((Object)group).name}' has no Volume GUID.");
                Array exposed = (Array)ExposedParametersProperty.GetValue(controller);
                EnsureNameIsAvailable(exposed, guid, desiredName);

                bool contains = (bool)Invoke(ContainsExposedMethod, controller, guid);
                if (!contains)
                {
                    object path = GroupParameterPathConstructor.Invoke(new[] { group, guid });
                    Invoke(AddExposedMethod, controller, path);
                    exposed = (Array)ExposedParametersProperty.GetValue(controller);
                }

                bool renamed = false;
                for (int i = 0; i < exposed.Length; i++)
                {
                    object boxed = exposed.GetValue(i);
                    if (!Equals(ExposedGuidField.GetValue(boxed), guid)) continue;
                    ExposedNameField.SetValue(boxed, desiredName);
                    exposed.SetValue(boxed, i);
                    renamed = true;
                    break;
                }
                if (!renamed)
                {
                    throw new InvalidOperationException(
                        $"Exposed Volume GUID for '{((Object)group).name}' was not found after authoring.");
                }
                ExposedParametersProperty.SetValue(controller, exposed);
            }

            public void ValidateTopologyAndExposedParameters(Object controller)
            {
                object master = MasterGroup(controller);
                object music = RequireChild(master, "Music");
                object sfx = RequireChild(master, "SFX");
                object voice = RequireChild(master, "Voice");
                object ambience = RequireChild(master, "Ambience");
                RequireChild(sfx, "SFX_Weapons");
                RequireChild(sfx, "SFX_Units");
                RequireChild(sfx, "UI");
                RequireChild(voice, "Voice_Commander");
                RequireChild(voice, "Voice_Barks");

                ValidateExposed(controller, master, UnityAudioService.MasterVolumeParameter);
                ValidateExposed(controller, music, UnityAudioService.MusicVolumeParameter);
                ValidateExposed(controller, sfx, UnityAudioService.SfxVolumeParameter);
                ValidateExposed(controller, voice, UnityAudioService.VoiceVolumeParameter);
                ValidateExposed(controller, ambience, UnityAudioService.AmbienceVolumeParameter);
            }

            private object RequireChild(object parent, string name)
            {
                object[] exact = Children(parent).Where(child => ((Object)child).name == name).ToArray();
                if (exact.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Mixer parent '{((Object)parent).name}' must have exactly one '{name}' child.");
                }
                return exact[0];
            }

            private void ValidateExposed(Object controller, object group, string expectedName)
            {
                object guid = Invoke(GetVolumeGuidMethod, group);
                Array exposed = (Array)ExposedParametersProperty.GetValue(controller);
                int matches = 0;
                for (int i = 0; i < exposed.Length; i++)
                {
                    object boxed = exposed.GetValue(i);
                    if (Equals(ExposedGuidField.GetValue(boxed), guid)
                        && Equals(ExposedNameField.GetValue(boxed), expectedName))
                    {
                        matches++;
                    }
                }
                if (matches != 1)
                {
                    throw new InvalidOperationException(
                        $"Mixer parameter '{expectedName}' must map exactly once to '{((Object)group).name}/Volume'.");
                }
            }

            private void EnsureNameIsAvailable(Array exposed, object guid, string desiredName)
            {
                for (int i = 0; i < exposed.Length; i++)
                {
                    object boxed = exposed.GetValue(i);
                    if (!Equals(ExposedNameField.GetValue(boxed), desiredName)) continue;
                    if (!Equals(ExposedGuidField.GetValue(boxed), guid))
                    {
                        throw new InvalidOperationException(
                            $"Exposed mixer name '{desiredName}' already belongs to another parameter.");
                    }
                }
            }

            private object[] Children(object group)
            {
                Array array = (Array)ChildrenProperty.GetValue(group);
                if (array == null) return Array.Empty<object>();
                var result = new object[array.Length];
                for (int i = 0; i < array.Length; i++) result[i] = array.GetValue(i);
                return result;
            }

            private static Type RequireType(Assembly assembly, string name)
            {
                return assembly.GetType(name, throwOnError: false)
                       ?? throw new TypeLoadException($"Unity 6000.5 mixer authoring type '{name}' was not found.");
            }

            private static MethodInfo RequireMethod(
                Type type, string name, BindingFlags flags, params Type[] parameterTypes)
            {
                return type.GetMethod(name, flags, null, parameterTypes, null)
                       ?? throw new MissingMethodException(
                           type.FullName,
                           $"{name}({string.Join(", ", parameterTypes.Select(item => item.Name))})");
            }

            private static PropertyInfo RequireProperty(Type type, string name)
            {
                return type.GetProperty(name, InstanceFlags)
                       ?? throw new MissingMemberException(type.FullName, name);
            }

            private static object Invoke(MethodInfo method, object target, params object[] args)
            {
                try
                {
                    return method.Invoke(target, args);
                }
                catch (TargetInvocationException exception) when (exception.InnerException != null)
                {
                    throw new InvalidOperationException(
                        $"Unity mixer authoring call {method.DeclaringType?.FullName}.{method.Name} failed.",
                        exception.InnerException);
                }
            }
        }
    }
}
