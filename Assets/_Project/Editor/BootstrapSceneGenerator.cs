using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Nova.Data;
using Nova.Gameplay.Audio;
using Nova.Gameplay.CombatFeedback;
using Nova.Gameplay.Match;
using Nova.Presentation;
using Nova.Presentation.Maps;
using Nova.Presentation.UI;
using UnityEngine.Audio;

namespace Nova.Editor
{
    /// <summary>
    /// Generator for the playable graybox Bootstrap scene. Run via the menu
    /// item, or headless:
    ///   Unity -batchmode -projectPath &lt;repo&gt; \
    ///     -executeMethod Nova.Editor.BootstrapSceneGenerator.CreateBootstrapScene -quit
    /// The scene is saved to Assets/_Project/Scenes/Bootstrap.unity and
    /// registered as the first enabled EditorBuildSettings scene.
    /// <para>
    /// THE SCENE IS MACHINE OUTPUT. Never hand-edit Bootstrap.unity — every
    /// change belongs here, and regenerating overwrites the file.
    /// </para>
    /// <para>
    /// THIS FILE WIRES, IT DOES NOT TUNE. Camera speeds, input hotkeys, player
    /// colours, interpolation, HUD scale and the match seed/size all live as
    /// [SerializeField] defaults on their components, so a freshly added
    /// component is already correctly configured. The only values written here
    /// are cross-object references plus the geometry of the ground plane, which
    /// has no component of its own to carry a default.
    /// </para>
    /// </summary>
    public static class BootstrapSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        /// <summary>
        /// Map edge in cells. Matches MatchBootstrap's _mapWidth/_mapHeight and
        /// RtsCameraController's _mapWidth/_mapHeight defaults; it exists here
        /// only to size the ground quad, which is not a Nova component.
        /// </summary>
        private const float MapCells = 128f;

        /// <summary>Unity's built-in Plane primitive spans 10x10 world units at scale 1.</summary>
        private const float PlanePrimitiveExtent = 10f;

        [MenuItem("Tools/Project Nova/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
            }

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            Camera camera = CreateCamera();
            CreateDirectionalLight();
            ConfigureAtmosphere();
            GameObject ground = CreateGroundPlane();
            AudioSceneReferences audio = CreateAudioObject();
            MatchRunner runner = CreateMatchObject();
            CreateMapObject(runner, ground);
            GameObject ui = CreateUiObject(runner, camera);
            CreateMainMenuObject(runner, camera, ui, audio.MusicGroup);
            CreateVersionBadgeObject();
            EnsureGlutrinneMapAsset();

            // The drop-in registry is rebuilt from whatever PF_* prefabs
            // currently exist under Assets/_Project/Art (usually zero — the
            // sync is a no-op until the first asset lands).
            ArtAssetAutoSync.SyncRegistry();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log($"Bootstrap scene created at {ScenePath} and registered " +
                      "in EditorBuildSettings (camera rig, ground, Match, Map, UI, MainMenu).");
        }

        /// <summary>
        /// The MainCamera plus the RTS rig. The rig's Awake overwrites position
        /// and rotation from its own serialized start focus, so the transform
        /// written here is only the pre-Play editor framing.
        /// </summary>
        private static Camera CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(64f, 60f, -20f);
            camera.transform.rotation = Quaternion.Euler(60f, 0f, 0f);

            // Far clip must clear the map diagonal at maximum zoom-out; 1000 is
            // Unity's default and is left untouched, asserted here so a future
            // edit cannot silently clip the terrain away.
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 400f);

            // [RequireComponent(typeof(Camera))] — the Camera above satisfies it.
            cameraObject.AddComponent<RtsCameraController>();

            // The scene had no AudioListener at all, so every sound in the
            // game — starting with the menu track — was silent no matter how
            // the source was configured. It rides on the main camera, which is
            // where Unity's own camera template puts it.
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        /// <summary>
        /// The Glutrinne sun (D-085): low and slanted for long shadows, warm
        /// in colour temperature, with soft shadows. These are Light
        /// properties on the generated object, so regeneration keeps them —
        /// hand-tuning the scene YAML would be overwritten here anyway.
        /// </summary>
        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(33f, -128f, 0f);
            light.color = new Color(1f, 0.87f, 0.70f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
        }

        /// <summary>
        /// Desert atmosphere (D-085): a light sand-coloured distance fog and
        /// a tri-light ambient gradient with a sandy horizon — the single
        /// most visible quick win of the map pass, and pure scene
        /// configuration. RenderSettings values are SCENE-serialized, so they
        /// are set here, in the generator: any value hand-edited into
        /// Bootstrap.unity would silently revert on the next regeneration.
        /// </summary>
        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.68f, 0.52f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 210f;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.50f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.76f, 0.66f, 0.50f); // the sandy horizon
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.26f, 0.21f);
        }

        /// <summary>
        /// Ground quad covering the whole 128x128 map, top face exactly on the
        /// y = 0 plane that RtsDeviceInput and RtsCameraController project onto.
        /// It keeps Unity's built-in default material (neutral grey) — the
        /// sprint forbids creating material assets, and the GlutrinneBlockoutView
        /// tints it at Play through a property block instead.
        /// </summary>
        private static GameObject CreateGroundPlane()
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.isStatic = true;

            float scale = MapCells / PlanePrimitiveExtent;
            ground.transform.position = new Vector3(MapCells * 0.5f, 0f, MapCells * 0.5f);
            ground.transform.localScale = new Vector3(scale, 1f, scale);
            return ground;
        }

        /// <summary>
        /// The simulation host: kernel driver, graybox match setup and the view
        /// layer, all on one GameObject. The view layer additionally receives
        /// the shared art-mapping registry, so registered PF_* prefabs replace
        /// their graybox primitives the moment they land under Assets/_Project/Art.
        /// </summary>
        private static MatchRunner CreateMatchObject()
        {
            var matchObject = new GameObject("Match");

            // Order matters: MatchRunner is [DisallowMultipleComponent] and
            // MatchBootstrap declares [RequireComponent(typeof(MatchRunner))].
            // Adding the runner first means MatchBootstrap binds to this
            // instance instead of Unity refusing an auto-added duplicate.
            MatchRunner runner = matchObject.AddComponent<MatchRunner>();
            MatchBootstrap bootstrap = matchObject.AddComponent<MatchBootstrap>();

            // The main menu owns the start now: the scene loads into an idle
            // host (no kernel, every HUD component silent) and "Neues Spiel"
            // calls the idempotent StartGrayboxMatch(). AutoStart is a public
            // field, so a plain assignment is serialized with the scene —
            // WireReference exists for private [SerializeField] object
            // references and cannot carry a bool.
            bootstrap.AutoStart = false;

            // CombatEffectController is presentation-only even though the
            // assembly boundary puts it beside the view manager. Wiring it
            // explicitly keeps the generated scene auditable; the manager's
            // lazy fallback exists only for old scenes and tests.
            CombatEffectController effects = matchObject.AddComponent<CombatEffectController>();
            UnitViewManager views = matchObject.AddComponent<UnitViewManager>();
            WireReference(views, "_matchRunner", runner);
            WireReference(views, "_assetMappings", ArtAssetAutoSync.LoadOrCreateRegistry());
            WireReference(views, "_combatEffects", effects);

            return runner;
        }

        /// <summary>
        /// The sole Tier-0 one-shot backend and the settings adapter. Sound
        /// event and mixer assets are authored by Sprint12BAuthoring before
        /// this generated scene is saved; missing assets remain loud nulls so
        /// that the authoring validation fails instead of hiding dead audio.
        /// </summary>
        private static AudioSceneReferences CreateAudioObject()
        {
            var audioObject = new GameObject("Audio");
            UnityAudioService service = audioObject.AddComponent<UnityAudioService>();
            audioObject.AddComponent<SfxSettingsBridge>();

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Sprint12BAuthoring.MixerPath);
            if (mixer == null)
            {
                Debug.LogError($"[BootstrapSceneGenerator] Missing mixer at {Sprint12BAuthoring.MixerPath}.");
            }

            AudioMixerGroup music = FindAudioGroup(mixer, "Music");
            AudioMixerGroup sfx = FindAudioGroup(mixer, "SFX");
            AudioMixerGroup weapons = FindAudioGroup(mixer, "SFX_Weapons");
            AudioMixerGroup units = FindAudioGroup(mixer, "SFX_Units");
            AudioMixerGroup ui = FindAudioGroup(mixer, "UI");

            WireObjectArray(service, "_events", Sprint12BAuthoring.LoadSoundEvents());
            WireReference(service, "_mixer", mixer);
            WireReference(service, "_sfxGroup", sfx);
            WireReference(service, "_weaponsGroup", weapons);
            WireReference(service, "_unitsGroup", units);
            WireReference(service, "_uiGroup", ui);

            return new AudioSceneReferences(music);
        }

        /// <summary>
        /// The Glutrinne blockout: desert ground tint, aetherium field markers
        /// and the map edge frame. It reads the MatchBootstrap layout at Play,
        /// so the rendered map always matches the registered match state.
        /// </summary>
        private static void CreateMapObject(MatchRunner runner, GameObject ground)
        {
            var mapObject = new GameObject("Map");

            var blockout = mapObject.AddComponent<GlutrinneBlockoutView>();
            WireReference(blockout, "_bootstrap", runner.GetComponent<MatchBootstrap>());
            WireReference(blockout, "_groundRenderer", ground.GetComponent<Renderer>());
        }

        /// <summary>
        /// The data-layer map asset. It records exactly the graybox-accurate
        /// subset of the Glutrinne manifest layout — the two spawn points and
        /// the five aetherium fields the canonical match registers since
        /// Sprint 16.7. Primary-route dressing remains G4 scope
        /// (docs/production/ScopeLedger.md) and is deliberately not invented here.
        /// </summary>
        private static void EnsureGlutrinneMapAsset()
        {
            const string mapPath = "Assets/_Project/Data/Maps/MAP_Glutrinne.asset";

            MapDefinitionSO map = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(mapPath);
            if (map == null)
            {
                ArtAssetAutoSync.EnsureFolder("Assets/_Project/Data/Maps");
                map = ScriptableObject.CreateInstance<MapDefinitionSO>();
                AssetDatabase.CreateAsset(map, mapPath);
            }

            map.Initialize(
                "Glutrinne",
                MapBiomeType.Desert,
                128,
                128,
                // D-107 HQ footprint centres: point mirror p -> 124-p.
                new[] { new Vector2(5f, 5f), new Vector2(119f, 119f) },
                // The five fields MatchBootstrap registers, in canonical id order.
                new[]
                {
                    new Vector2(7f, 7f),
                    new Vector2(117f, 117f),
                    new Vector2(24f, 40f),
                    new Vector2(100f, 84f),
                    new Vector2(62f, 62f),
                });
            EditorUtility.SetDirty(map);
        }

        /// <summary>
        /// Input sampling, the world-space HUD markers (selection, rally
        /// flags, placement ghost), the build bar, the command card, the
        /// match frame (result/network panels), the pause menu and the
        /// read-only debug overlay. The build bar and the command card are
        /// additionally wired INTO the input component: clicks landing on a
        /// HUD rect belong to the HUD and must not start a world selection
        /// drag, place a building or resolve an order pick behind it. The
        /// pause menu is wired into the input's counterpart direction: it
        /// reads the gesture-cancel stamp so ESC peels one layer at a time.
        /// Everything lands on ONE GameObject so the main menu can silence
        /// the whole cockpit with a single root switch — a component added
        /// here is covered by that switch from the day it lands.
        /// </summary>
        private static GameObject CreateUiObject(MatchRunner runner, Camera camera)
        {
            var uiObject = new GameObject("UI");

            RtsDeviceInput input = uiObject.AddComponent<RtsDeviceInput>();
            WireReference(input, "_runner", runner);
            WireReference(input, "_camera", camera);

            SelectionMarkerView markers = uiObject.AddComponent<SelectionMarkerView>();
            WireReference(markers, "_runner", runner);
            WireReference(markers, "_input", input);
            WireReference(markers, "_views", runner.GetComponent<UnitViewManager>());

            RallyFlagView rally = uiObject.AddComponent<RallyFlagView>();
            WireReference(rally, "_runner", runner);
            WireReference(rally, "_input", input);

            PlacementGhostView ghost = uiObject.AddComponent<PlacementGhostView>();
            WireReference(ghost, "_input", input);

            // #91: the build-zone overlay — asks the construction system's
            // own placement reads, so only the data source (runner) and the
            // visibility state (input) are wired here.
            BuildZoneOverlayView buildZone = uiObject.AddComponent<BuildZoneOverlayView>();
            WireReference(buildZone, "_runner", runner);
            WireReference(buildZone, "_input", input);

            ConstructionSiteMarkerView siteMarkers = uiObject.AddComponent<ConstructionSiteMarkerView>();
            WireReference(siteMarkers, "_runner", runner);

            BuildMenuHud menu = uiObject.AddComponent<BuildMenuHud>();
            WireReference(menu, "_runner", runner);
            WireReference(menu, "_input", input);

            WireReference(input, "_buildMenu", menu);

            CommandCardHud card = uiObject.AddComponent<CommandCardHud>();
            WireReference(card, "_runner", runner);
            WireReference(card, "_input", input);
            WireReference(card, "_buildMenu", menu);
            WireReference(card, "_bootstrap", runner.GetComponent<MatchBootstrap>());

            WireReference(input, "_commandCard", card);

            FogOfWarOverlayView fog = uiObject.AddComponent<FogOfWarOverlayView>();
            WireReference(fog, "_runner", runner);

            MinimapHud minimap = uiObject.AddComponent<MinimapHud>();
            WireReference(minimap, "_runner", runner);
            WireReference(minimap, "_buildMenu", menu);

            HealthBarHud healthBars = uiObject.AddComponent<HealthBarHud>();
            WireReference(healthBars, "_runner", runner);
            WireReference(healthBars, "_input", input);

            MatchFrameHud frame = uiObject.AddComponent<MatchFrameHud>();
            WireReference(frame, "_runner", runner);
            WireReference(frame, "_bootstrap", runner.GetComponent<MatchBootstrap>());
            WireReference(frame, "_views", runner.GetComponent<UnitViewManager>());
            // _menu is wired in CreateMainMenuObject — the menu object does
            // not exist yet at this point in the generation order.

            PauseMenuHud pauseMenu = uiObject.AddComponent<PauseMenuHud>();
            WireReference(pauseMenu, "_runner", runner);
            WireReference(pauseMenu, "_matchFrame", frame);
            WireReference(pauseMenu, "_input", input);
            // _menu is wired in CreateMainMenuObject — same ordering reason
            // as the match frame above.

            DebugHud hud = uiObject.AddComponent<DebugHud>();
            WireReference(hud, "_runner", runner);
            WireReference(hud, "_input", input);

            return uiObject;
        }

        /// <summary>
        /// The main menu overlay: a UI Toolkit panel and the menu track, in
        /// the same scene as the match. There is no menu scene and no
        /// SceneManager call anywhere — MatchBootstrap.AutoStart is off (see
        /// CreateMatchObject) and MainMenuController starts the match from
        /// "Neues Spiel".
        /// <para>
        /// TWO things are wired INTO the menu because they are the scene's
        /// only parts without a "no match yet" guard: the camera rig (it
        /// would edge-pan and zoom while the player moves the pointer over
        /// the menu) and the gameplay HUD ROOT — this one GameObject carries
        /// every in-match HUD component, so the menu silences the whole
        /// cockpit (including the debug HUD's always-on status bar, which
        /// draws before its own visibility check) with a single root switch
        /// that can never rot the way a component catalogue would. On the
        /// receiving side the rig is typed as Behaviour (Nova.Presentation.UI
        /// may not reference Nova.Presentation), which WireReference handles
        /// — it assigns object references, not typed fields.
        /// </para>
        /// </summary>
        private static void CreateMainMenuObject(
            MatchRunner runner,
            Camera camera,
            GameObject uiObject,
            AudioMixerGroup musicGroup)
        {
            var menuObject = new GameObject("MainMenu");

            UIDocument document = menuObject.AddComponent<UIDocument>();
            document.panelSettings = MenuAssetSetup.LoadOrCreatePanelSettings();
            // No visualTreeAsset on purpose: MainMenuController builds its
            // tree in C#, so the generated scene carries no hand-authored UXML
            // asset that this generator could not reproduce.

            // MenuMusicPlayer declares [RequireComponent(typeof(AudioSource))]
            // and configures the source itself (clip, loop, 2D, playback) —
            // this file wires, it does not tune.
            AudioSource source = menuObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = musicGroup;

            MenuMusicPlayer music = menuObject.AddComponent<MenuMusicPlayer>();
            WireReference(music, "_source", source);
            WireReference(music, "_clip",
                MenuAssetSetup.LoadRequired<AudioClip>(MenuAssetSetup.MusicClipPath));

            MainMenuController menu = menuObject.AddComponent<MainMenuController>();
            WireReference(menu, "_document", document);
            WireReference(menu, "_bootstrap", runner.GetComponent<MatchBootstrap>());
            WireReference(menu, "_music", music);
            WireReference(menu, "_cameraRig", camera.GetComponent<RtsCameraController>());
            WireReference(menu, "_gameplayHudRoot", uiObject);
            WireReference(menu, "_keyArt",
                MenuAssetSetup.LoadRequired<Texture2D>(MenuAssetSetup.KeyArtPath));
            WireReference(menu, "_titleFont",
                MenuAssetSetup.LoadRequired<Font>(MenuAssetSetup.TitleFontPath));
            WireReference(menu, "_bodyFont",
                MenuAssetSetup.LoadRequired<Font>(MenuAssetSetup.BodyFontPath));

            // The match frame's "Hauptmenü" and the pause menu's "Zum
            // Hauptmenü"/"Spiel beenden" call back into this menu.
            MatchFrameHud frame = uiObject.GetComponent<MatchFrameHud>();
            if (frame != null)
            {
                WireReference(frame, "_menu", menu);
            }
            PauseMenuHud pauseMenu = uiObject.GetComponent<PauseMenuHud>();
            if (pauseMenu != null)
            {
                WireReference(pauseMenu, "_menu", menu);
            }

            CreateIngameMusicObject(runner, menu, musicGroup);
        }

        /// <summary>
        /// The always-on version badge (issue #103): its OWN GameObject and
        /// UIDocument, so it survives both the menu's root rebuilds and the
        /// IMGUI cockpit's menu/match visibility switches (issue #102). It
        /// shares the menu's PanelSettings; the component sorts its document
        /// above the menu's full-screen key art itself. No visualTreeAsset —
        /// like the menu, the tree is built in C# so the generated scene
        /// carries nothing this generator could not reproduce.
        /// </summary>
        private static void CreateVersionBadgeObject()
        {
            var badgeObject = new GameObject("VersionBadge");

            UIDocument document = badgeObject.AddComponent<UIDocument>();
            document.panelSettings = MenuAssetSetup.LoadOrCreatePanelSettings();

            VersionBadge badge = badgeObject.AddComponent<VersionBadge>();
            WireReference(badge, "_document", document);
            WireReference(badge, "_font",
                MenuAssetSetup.LoadRequired<Font>(MenuAssetSetup.BodyFontPath));
        }

        /// <summary>
        /// The in-game playlist (D-086): own GameObject, own AudioSource —
        /// MusicDirector configures the source itself, this file only wires
        /// the three MUS_Ingame clips in playlist order.
        /// </summary>
        private static void CreateIngameMusicObject(
            MatchRunner runner,
            MainMenuController menu,
            AudioMixerGroup musicGroup)
        {
            var musicObject = new GameObject("IngameMusic");
            AudioSource source = musicObject.AddComponent<AudioSource>();
            source.outputAudioMixerGroup = musicGroup;
            var director = musicObject.AddComponent<MusicDirector>();
            WireReference(director, "_source", source);
            WireReference(director, "_runner", runner);
            WireReference(director, "_bootstrap", runner.GetComponent<MatchBootstrap>());
            WireReference(director, "_menu", menu);

            var clips = new[]
            {
                MenuAssetSetup.LoadRequired<AudioClip>("Assets/_Project/Audio/Music/MUS_Ingame_Hashkrieg_01.ogg"),
                MenuAssetSetup.LoadRequired<AudioClip>("Assets/_Project/Audio/Music/MUS_Ingame_Hashkrieg_02.ogg"),
                MenuAssetSetup.LoadRequired<AudioClip>("Assets/_Project/Audio/Music/MUS_Ingame_Hashkrieg_03.ogg"),
            };
            var serialized = new SerializedObject(director);
            SerializedProperty playlist = serialized.FindProperty("_playlist");
            if (playlist == null)
            {
                Debug.LogError("[BootstrapSceneGenerator] MusicDirector has no serialized field '_playlist'.");
                return;
            }
            playlist.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++)
            {
                playlist.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioMixerGroup FindAudioGroup(AudioMixer mixer, string name)
        {
            if (mixer == null) return null;
            AudioMixerGroup[] matches = mixer.FindMatchingGroups(name);
            AudioMixerGroup exact = null;
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i] == null || matches[i].name != name) continue;
                if (exact != null)
                {
                    Debug.LogError($"[BootstrapSceneGenerator] Mixer group '{name}' is ambiguous.");
                    return null;
                }
                exact = matches[i];
            }
            if (exact == null)
            {
                Debug.LogError($"[BootstrapSceneGenerator] Mixer group '{name}' is missing.");
            }
            return exact;
        }

        /// <summary>SerializedObject equivalent of WireReference for asset arrays.</summary>
        private static void WireObjectArray<T>(Object target, string fieldName, T[] values)
            where T : Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                Debug.LogError(
                    $"[BootstrapSceneGenerator] {target.GetType().Name} has no serialized array " +
                    $"'{fieldName}' — the Bootstrap scene will be wired incompletely.");
                return;
            }

            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Assigns a private [SerializeField] object reference and logs loudly
        /// if the field disappeared, so a rename cannot silently produce a
        /// scene with dead wiring.
        /// </summary>
        private static void WireReference(Object target, string fieldName, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError(
                    $"[BootstrapSceneGenerator] {target.GetType().Name} has no serialized field " +
                    $"'{fieldName}' — the Bootstrap scene will be wired incompletely.");
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct AudioSceneReferences
        {
            public AudioMixerGroup MusicGroup { get; }

            public AudioSceneReferences(AudioMixerGroup musicGroup)
            {
                MusicGroup = musicGroup;
            }
        }
    }
}
