using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Nova.Data;
using Nova.Gameplay.Match;
using Nova.Presentation;
using Nova.Presentation.Maps;
using Nova.Presentation.UI;

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
            MatchRunner runner = CreateMatchObject();
            CreateMapObject(runner, ground);
            GameObject ui = CreateUiObject(runner, camera);
            CreateMainMenuObject(runner, camera, ui);
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

            UnitViewManager views = matchObject.AddComponent<UnitViewManager>();
            WireReference(views, "_matchRunner", runner);
            WireReference(views, "_assetMappings", ArtAssetAutoSync.LoadOrCreateRegistry());

            return runner;
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
        /// the two aetherium fields the canonical match actually registers.
        /// The full five-field layout with both primary routes is G4 scope
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
                // HQ footprint centres of the canonical opening (4,4)+(3x3) and its 180° mirror.
                new[] { new Vector2(5f, 5f), new Vector2(121f, 121f) },
                // The two fields MatchBootstrap registers: local (7,7), enemy (119,119).
                new[] { new Vector2(7f, 7f), new Vector2(119f, 119f) });
            EditorUtility.SetDirty(map);
        }

        /// <summary>
        /// Input sampling, the world-space HUD markers (selection, rally
        /// flags, placement ghost), the build bar, the command card and the
        /// read-only debug overlay. The build bar and the command card are
        /// additionally wired INTO the input component: clicks landing on a
        /// HUD rect belong to the HUD and must not start a world selection
        /// drag, place a building or resolve an order pick behind it.
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
        /// The camera rig and the debug HUD are wired INTO the menu because
        /// they are the only two components in this scene without a "no match
        /// yet" guard: the rig would edge-pan and zoom while the player moves
        /// the pointer over the menu, and the HUD's always-on status bar draws
        /// before its own visibility check. The menu switches both off while
        /// it is up. On the receiving side the rig is typed as Behaviour
        /// (Nova.Presentation.UI may not reference Nova.Presentation), which
        /// WireReference handles — it assigns object references, not typed
        /// fields.
        /// </para>
        /// </summary>
        private static void CreateMainMenuObject(MatchRunner runner, Camera camera, GameObject uiObject)
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

            MenuMusicPlayer music = menuObject.AddComponent<MenuMusicPlayer>();
            WireReference(music, "_source", source);
            WireReference(music, "_clip",
                MenuAssetSetup.LoadRequired<AudioClip>(MenuAssetSetup.MusicClipPath));

            MainMenuController menu = menuObject.AddComponent<MainMenuController>();
            WireReference(menu, "_document", document);
            WireReference(menu, "_bootstrap", runner.GetComponent<MatchBootstrap>());
            WireReference(menu, "_music", music);
            WireReference(menu, "_cameraRig", camera.GetComponent<RtsCameraController>());
            WireReference(menu, "_debugHud", uiObject.GetComponent<DebugHud>());
            WireReference(menu, "_keyArt",
                MenuAssetSetup.LoadRequired<Texture2D>(MenuAssetSetup.KeyArtPath));
            WireReference(menu, "_titleFont",
                MenuAssetSetup.LoadRequired<Font>(MenuAssetSetup.TitleFontPath));
            WireReference(menu, "_bodyFont",
                MenuAssetSetup.LoadRequired<Font>(MenuAssetSetup.BodyFontPath));

            // The match frame's "Hauptmenü" button calls back into this menu.
            MatchFrameHud frame = uiObject.GetComponent<MatchFrameHud>();
            if (frame != null)
            {
                WireReference(frame, "_menu", menu);
            }

            CreateIngameMusicObject(runner, menu);
        }

        /// <summary>
        /// The in-game playlist (D-086): own GameObject, own AudioSource —
        /// MusicDirector configures the source itself, this file only wires
        /// the three MUS_Ingame clips in playlist order.
        /// </summary>
        private static void CreateIngameMusicObject(MatchRunner runner, MainMenuController menu)
        {
            var musicObject = new GameObject("IngameMusic");
            AudioSource source = musicObject.AddComponent<AudioSource>();
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
    }
}
