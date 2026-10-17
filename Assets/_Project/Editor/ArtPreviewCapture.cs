using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Nova.Core;
using Nova.Gameplay.Match;

namespace Nova.Editor
{
    /// <summary>
    /// Headless photo booth for the art prefabs: renders every <c>PF_*</c>
    /// under the art root through the real URP pipeline, under the in-game
    /// sun and ambient (BootstrapSceneGenerator), and writes PNGs. It exists
    /// so an asset pass can be judged as the game draws it — not as Blender
    /// draws it — without anybody opening the editor.
    /// <code>
    /// Unity -batchmode -projectPath . -executeMethod Nova.Editor.ArtPreviewCapture.CaptureFromCommandLine \
    ///       -captureOut /abs/dir [-captureFilter BattleTank] [-captureHeading 0] [-captureTime 0.0] [-captureDump 0.7]
    ///       [-captureBuildPrefabs] [-captureWreck] [-captureLineup] -logFile log -quit
    /// </code>
    /// Per prefab it writes <c>&lt;name&gt;_top.png</c> (orthographic, from
    /// above, with a red bar pointing along the simulated move direction) and
    /// <c>&lt;name&gt;_rts.png</c> (the RTS camera's 55° pitch, framed close).
    /// The prefab is rotated by <see cref="ViewHeading"/> for a unit moving
    /// at <c>-captureHeading</c> sim degrees (0 = sim +X = world east), so
    /// the top shot proves the whole chain: FBX axes, import, and the
    /// sim-to-yaw mapping — the nose must sit on the red bar.
    /// </summary>
    public static class ArtPreviewCapture
    {
        private const int Size = 512;

        public static void CaptureFromCommandLine()
        {
            string outDir = Arg("-captureOut", Path.Combine(Directory.GetCurrentDirectory(), "output", "art-preview"));
            string filter = Arg("-captureFilter", "");
            float heading = float.Parse(Arg("-captureHeading", "0"), System.Globalization.CultureInfo.InvariantCulture);
            float time = float.Parse(Arg("-captureTime", "0"), System.Globalization.CultureInfo.InvariantCulture);
            PreviewDumpSeconds = float.Parse(Arg("-captureDump", "-1"), System.Globalization.CultureInfo.InvariantCulture);
            int written = 0;
            try
            {
                if (HasFlag("-captureBuildPrefabs"))
                {
                    // Fresh FBX/texture drops need their materials and prefabs
                    // rebuilt before they can be photographed.
                    ArtAssetPrefabBuilder.Build();
                    ArtAssetAutoSync.SyncRegistry();
                }
                written = HasFlag("-captureLineup")
                    ? CaptureLineup(outDir)
                    : Capture(outDir, filter, heading, time);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log($"[ArtPreviewCapture] {written} prefab(s) captured into {outDir}");
        }

        public static int Capture(string outDir, string filter, float simHeadingDegrees, float animationTime)
        {
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpStage();

            string[] guids = AssetDatabase.FindAssets("t:Prefab PF_", new[] { "Assets/_Project/Art" });
            var paths = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith("PF_", StringComparison.Ordinal)) continue;
                if (filter.Length > 0 && path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                paths.Add(path);
            }
            paths.Sort(StringComparer.Ordinal);

            SimAngle heading = SimAngle.FromDegrees(SimFixed.FromFloat(simHeadingDegrees));
            Quaternion rotation = ViewHeading.Rotation(heading);
            Vector3 moveDir = rotation * Vector3.forward;

            foreach (string path in paths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                GameObject instance = UnityEngine.Object.Instantiate(prefab);
                try
                {
                    instance.transform.SetPositionAndRotation(Vector3.zero, rotation);
                    if (HasFlag("-captureDump")) Dump(instance.transform, "");
                    ApplyFactionTint(instance, path);
                    PoseAnimation(instance, path, animationTime);
                    if (HasFlag("-captureWreck")) ShowWreck(instance);

                    Bounds b = WorldBounds(instance);
                    float extent = Mathf.Max(b.extents.x, b.extents.z, b.extents.y * 0.7f, 0.5f);

                    GameObject marker = MoveMarker(b, moveDir, extent);
                    string stem = Path.GetFileNameWithoutExtension(path);
                    Shoot(TopCamera(b, extent), Path.Combine(outDir, stem + "_top.png"));
                    UnityEngine.Object.DestroyImmediate(marker);
                    Shoot(RtsCamera(b, extent), Path.Combine(outDir, stem + "_rts.png"));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }
            }
            return paths.Count;
        }

        /// <summary>
        /// Size line-up per faction: every prefab at exactly the scale the
        /// game gives it (<see cref="ArtViewScale"/>), buildings in a back
        /// row, units in front, a 1-cell grid on the ground. Writes
        /// <c>lineup_&lt;Faction&gt;_rts.png</c> (RTS pitch) and
        /// <c>_side.png</c> (eye level), and logs each model's view size in
        /// cells — the table proportion decisions are made from.
        /// </summary>
        public static int CaptureLineup(string outDir)
        {
            Directory.CreateDirectory(outDir);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpStage();
            GridLines(40);

            int count = 0;
            foreach (string faction in new[] { "Alliance", "Legion" })
            {
                var placed = new List<GameObject>();
                float bx = -13.5f, ux = -12f;
                foreach (string path in PrefabPaths($"_{faction}_"))
                {
                    string stem = Path.GetFileNameWithoutExtension(path);
                    string roleName = stem.Substring(stem.LastIndexOf('_') + 1);
                    bool building = stem.StartsWith("PF_BLDG_", StringComparison.Ordinal);

                    GameObject go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    Bounds raw = WorldBounds(go);
                    if (!ArtViewScale.TryFactor(roleName, raw.size, out float f))
                    {
                        UnityEngine.Object.DestroyImmediate(go);
                        continue;
                    }
                    go.transform.localScale = Vector3.one * f;
                    ApplyFactionTint(go, path);
                    Bounds b = WorldBounds(go);

                    float width = building ? 3.4f : Mathf.Max(b.size.x, 0.6f) + 0.5f;
                    float x = building ? bx + width * 0.5f : ux + width * 0.5f;
                    go.transform.position += new Vector3(x - b.center.x, -b.min.y, (building ? 3f : -1.2f) - b.center.z);
                    if (building) bx += width; else ux += width;
                    placed.Add(go);
                    count++;
                    Debug.Log($"[ArtPreviewCapture:lineup] {faction,-8} {roleName,-18} factor={f:F3}  width={b.size.x:F2} depth={b.size.z:F2} height={b.size.y:F2} cells");
                }

                Bounds all = new Bounds(Vector3.zero, Vector3.one);
                foreach (GameObject go in placed) all.Encapsulate(WorldBounds(go));
                const int W = 1600, H = 560;
                Camera rts = NewCamera();
                rts.fieldOfView = 24f;
                rts.aspect = (float)W / H;
                Quaternion rot = Quaternion.Euler(55f, 0f, 0f);
                rts.transform.SetPositionAndRotation(all.center - rot * Vector3.forward * (all.extents.x * 2.0f), rot);
                Shoot(rts, Path.Combine(outDir, $"lineup_{faction}_rts.png"), W, H);

                Camera side = NewCamera();
                side.orthographic = true;
                side.aspect = (float)W / H;
                side.orthographicSize = all.extents.x * 1.04f * H / W;
                side.transform.SetPositionAndRotation(new Vector3(all.center.x, side.orthographicSize * 0.92f, -30f), Quaternion.identity);
                Shoot(side, Path.Combine(outDir, $"lineup_{faction}_side.png"), W, H);

                foreach (GameObject go in placed) UnityEngine.Object.DestroyImmediate(go);
            }
            return count;
        }

        private static List<string> PrefabPaths(string contains)
        {
            var paths = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab PF_", new[] { "Assets/_Project/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path).StartsWith("PF_", StringComparison.Ordinal) && path.Contains(contains)) paths.Add(path);
            }
            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        /// <summary>Thin dark lines every cell, so sizes read off the picture.</summary>
        private static void GridLines(int half)
        {
            var dark = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            dark.SetColor("_BaseColor", new Color(0.45f, 0.37f, 0.27f));
            for (int i = -half; i <= half; i++)
            {
                foreach (bool alongX in new[] { true, false })
                {
                    GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    UnityEngine.Object.DestroyImmediate(line.GetComponent<Collider>());
                    line.GetComponent<Renderer>().sharedMaterial = dark;
                    line.transform.localScale = alongX ? new Vector3(half * 2f, 0.01f, 0.03f) : new Vector3(0.03f, 0.01f, half * 2f);
                    line.transform.position = alongX ? new Vector3(0f, 0.005f, i) : new Vector3(i, 0.005f, 0f);
                }
            }
        }

        /// <summary>Same sun, ambient and sand ground as the generated Bootstrap scene.</summary>
        private static void SetUpStage()
        {
            var lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(BootstrapSceneGenerator.SunPitch, BootstrapSceneGenerator.SunYaw, 0f);
            light.color = new Color(1f, 0.87f, 0.70f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.52f, 0.50f, 0.46f);
            RenderSettings.ambientEquatorColor = new Color(0.76f, 0.66f, 0.50f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.26f, 0.21f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            var sand = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sand.SetColor("_BaseColor", new Color(0.66f, 0.56f, 0.42f));
            sand.SetFloat("_Smoothness", 0.1f);
            ground.GetComponent<Renderer>().sharedMaterial = sand;
        }

        private static void ApplyFactionTint(GameObject instance, string path)
        {
            Color tint = path.Contains("_Legion_") ? FactionTint.LegionPrefabTint : FactionTint.AlliancePrefabTint;
            var block = new MaterialPropertyBlock();
            FactionTint.ApplyToPropertyBlock(block, tint);
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                r.SetPropertyBlock(block);
            }
        }

        /// <summary>
        /// Puts the rig into a mid-drive, mid-fight pose after a fixed time
        /// (turret swung toward a target front-right, gun recoiling), exactly
        /// as UnitViewManager would attach it at runtime.
        /// </summary>
        /// <summary>-captureDump: seconds into a harvester unload to show; negative = no unload.</summary>
        private static float PreviewDumpSeconds = -1f;

        private static void PoseAnimation(GameObject instance, string path, float seconds)
        {
            if (seconds <= 0f) return;
            UnitRigAnimator rig = instance.GetComponent<UnitRigAnimator>();
            if (rig == null) rig = instance.AddComponent<UnitRigAnimator>();
            rig.InfantryGait = path.Contains("Infantry");
            rig.PreviewPose(seconds, PreviewDumpSeconds);
        }

        /// <summary>Switches a view to its destroyed state, exactly as ViewWrecks does on death.</summary>
        private static void ShowWreck(GameObject instance)
        {
            Transform wreck = ViewWrecks.FindWreck(instance);
            if (wreck == null) return;
            foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.transform.IsChildOf(wreck)) r.enabled = false;
            }
            wreck.gameObject.SetActive(true);
        }

        private static GameObject MoveMarker(Bounds b, Vector3 dir, float extent)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var red = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            red.SetColor("_BaseColor", new Color(1f, 0.1f, 0.1f));
            bar.GetComponent<Renderer>().sharedMaterial = red;
            float len = extent * 0.9f;
            bar.transform.localScale = new Vector3(extent * 0.12f, 0.05f, len);
            bar.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            bar.transform.position = new Vector3(b.center.x, 0.03f, b.center.z) + dir * (extent * 1.05f + len * 0.5f);
            return bar;
        }

        private static Camera TopCamera(Bounds b, float extent)
        {
            Camera cam = NewCamera();
            cam.orthographic = true;
            cam.orthographicSize = extent * 2.3f;
            cam.transform.SetPositionAndRotation(new Vector3(b.center.x, b.max.y + 50f, b.center.z),
                Quaternion.Euler(90f, 0f, 0f));
            return cam;
        }

        private static Camera RtsCamera(Bounds b, float extent)
        {
            Camera cam = NewCamera();
            cam.fieldOfView = 30f;
            Quaternion rot = Quaternion.Euler(55f, 0f, 0f);
            float dist = extent * 4.6f;
            cam.transform.SetPositionAndRotation(b.center - rot * Vector3.forward * dist, rot);
            return cam;
        }

        private static Camera NewCamera()
        {
            var go = new GameObject("PreviewCamera");
            Camera cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.allowHDR = true;
            UrpPostProcessingSetup.EnablePostProcessing(cam);
            return cam;
        }

        private static void Shoot(Camera cam, string file, int width = Size, int height = Size)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request))
                {
                    RenderPipeline.SubmitRenderRequest(cam, request);
                }
                else
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;
                }

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                RenderTexture.active = previous;
                File.WriteAllBytes(file, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
        }

        private static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static void Dump(Transform t, string indent)
        {
            var r = t.GetComponent<Renderer>();
            Debug.Log($"[ArtPreviewCapture:dump] {indent}{t.name} pos={t.localPosition:F3} rot={t.localEulerAngles:F1} scl={t.localScale:F3}"
                      + (r != null ? $" bounds={r.bounds.center:F2}/{r.bounds.size:F2}" : ""));
            foreach (Transform c in t) Dump(c, indent + "  ");
        }

        private static bool HasFlag(string name)
        {
            return Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
        }

        private static string Arg(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return fallback;
        }
    }
}
