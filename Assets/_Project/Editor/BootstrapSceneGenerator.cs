using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
            GameObject ground = CreateGroundPlane();
            MatchRunner runner = CreateMatchObject();
            CreateMapObject(runner, ground);
            CreateUiObject(runner, camera);
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
                      "in EditorBuildSettings (camera rig, ground, Match, Map, UI).");
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
            return camera;
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
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
            matchObject.AddComponent<MatchBootstrap>();

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

        /// <summary>Input sampling and the read-only debug overlay.</summary>
        private static void CreateUiObject(MatchRunner runner, Camera camera)
        {
            var uiObject = new GameObject("UI");

            RtsDeviceInput input = uiObject.AddComponent<RtsDeviceInput>();
            WireReference(input, "_runner", runner);
            WireReference(input, "_camera", camera);

            DebugHud hud = uiObject.AddComponent<DebugHud>();
            WireReference(hud, "_runner", runner);
            WireReference(hud, "_input", input);
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
