using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Gameplay.Match;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Visible proof that the graybox actually RUNS — not just compiles. The
    /// test loads the generated Bootstrap scene, lets the canonical match tick
    /// in real time, asserts the live signals (match ready, kernel running,
    /// session ticks advancing, visible views, growing Aetherium stockpile)
    /// and captures screenshots into output/demo/ so a human can SEE the
    /// Glutrinne blockout without opening the editor.
    /// <para>
    /// Run headless-with-graphics (NO -nographics, screenshots need a render
    /// device) and NEVER with -quit (quality/scripts/run_gate_check.py:462 —
    /// -quit silently skips the whole run):
    ///   Unity -batchmode -projectPath &lt;repo&gt; -runTests -testPlatform PlayMode \
    ///     -testResults &lt;abs&gt;/output/playmode-results.xml -logFile output/playmode-tests.log
    /// </para>
    /// </summary>
    public sealed class GrayboxDemoProofTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";

        [UnityTest]
        public IEnumerator SceneViews_RenderOverviewAndBothBases()
        {
            Directory.CreateDirectory(ShotDir);

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap);
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!bootstrap.IsMatchReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.IsTrue(bootstrap.IsMatchReady);

            // Let shaders and first frames settle before capturing.
            yield return new WaitForSeconds(2f);

            CaptureFrom($"{ShotDir}/demo_03_overview.png", new Vector3(64f, 110f, 24f), new Vector3(64f, 0f, 64f));
            CaptureFrom($"{ShotDir}/demo_04_base_alliance.png", new Vector3(5f, 30f, -12f), new Vector3(6f, 0f, 6f));
            CaptureFrom($"{ShotDir}/demo_05_base_legion.png", new Vector3(121f, 30f, 138f), new Vector3(120f, 0f, 120f));

            foreach (string shot in new[]
            {
                $"{ShotDir}/demo_03_overview.png", $"{ShotDir}/demo_04_base_alliance.png", $"{ShotDir}/demo_05_base_legion.png",
            })
            {
                Assert.IsTrue(File.Exists(shot), $"screenshot missing: {shot}");
                Assert.Greater(new FileInfo(shot).Length, 10 * 1024, $"screenshot suspiciously small: {shot}");
            }
        }

        /// <summary>Renders one frame from a throwaway camera posed at <paramref name="position"/> looking at <paramref name="target"/>. The RTS camera is left untouched.</summary>
        private static void CaptureFrom(string path, Vector3 position, Vector3 target)
        {
            var go = new GameObject("ProofCamera");
            try
            {
                var camera = go.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.13f, 0.16f, 0.22f, 1f);
                camera.transform.position = position;
                camera.transform.LookAt(target);
                CaptureFrame(camera, path);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [UnityTest]
        public IEnumerator BootstrapMatch_RunsRendersAndHarvests()
        {
            Directory.CreateDirectory(ShotDir);

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");

            // AutoStart fires in Start(), one frame after the scene loads.
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!bootstrap.IsMatchReady && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.IsTrue(bootstrap.IsMatchReady, "Match did not become ready within 15 s of scene load");

            MatchRunner runner = bootstrap.Runner;
            Assert.NotNull(runner, "MatchBootstrap has no runner bound");
            Assert.IsTrue(runner.IsRunning, "SimulationKernel is not running");

            var views = Object.FindAnyObjectByType<UnitViewManager>();
            Assert.NotNull(views, "Bootstrap scene contains no UnitViewManager");

            // --- Baseline after ~3 s of real time ---------------------------
            yield return new WaitForSeconds(3f);

            uint tickBaseline = runner.Session.CurrentTick;
            long creditsBaseline = runner.Economy.GetPlayerEconomy(0).AetheriumCredits;
            Assert.Greater(tickBaseline, 0u, "Session tick did not advance");

            Camera camera = Camera.main;
            Assert.NotNull(camera, "no main camera in the Bootstrap scene");
            CaptureFrame(camera, $"{ShotDir}/demo_01_start.png");

            // --- Let the match run: economy must grow on its own ------------
            yield return new WaitForSeconds(17f);

            uint tickLater = runner.Session.CurrentTick;
            long creditsLater = runner.Economy.GetPlayerEconomy(0).AetheriumCredits;

            Assert.Greater(tickLater, tickBaseline, "Ticks stalled over 17 s of real time");
            Assert.Greater(views.VisibleViewCount, 0, "No entity views are visible in the local team's committed view");
            Assert.Greater(creditsLater, creditsBaseline,
                $"Aetherium stockpile did not grow ({creditsBaseline} -> {creditsLater}): the harvest cycle is broken");

            CaptureFrame(camera, $"{ShotDir}/demo_02_economy.png");
            yield return null;

            Debug.Log($"[GrayboxDemoProof] tick {tickBaseline}->{tickLater}, " +
                      $"credits {creditsBaseline}->{creditsLater} AE, visible views {views.VisibleViewCount}");

            // A written, non-trivial PNG proves the render device actually
            // produced an image (an empty batchmode capture is 0-2 KB).
            foreach (string shot in new[] { $"{ShotDir}/demo_01_start.png", $"{ShotDir}/demo_02_economy.png" })
            {
                Assert.IsTrue(File.Exists(shot), $"screenshot missing: {shot}");
                Assert.Greater(new FileInfo(shot).Length, 10 * 1024, $"screenshot suspiciously small: {shot}");
            }
        }

        /// <summary>
        /// Renders one frame through the given camera into a PNG at a
        /// project-relative path. ScreenCapture.CaptureScreenshot is a silent
        /// no-op in -batchmode (no frame is ever presented), so the proof
        /// renders through a RenderTexture instead — the only path that works
        /// headless-with-graphics. IMGUI overlays (the debug HUD) do not draw
        /// into a RenderTexture; the capture is world-only.
        /// </summary>
        private static void CaptureFrame(Camera camera, string path, int width = 1600, int height = 900)
        {
            var rt = new RenderTexture(width, height, 24);
            try
            {
                camera.targetTexture = rt;
                camera.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                try
                {
                    tex.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes(path, tex.EncodeToPNG());
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                    RenderTexture.active = null;
                }
            }
            finally
            {
                camera.targetTexture = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
