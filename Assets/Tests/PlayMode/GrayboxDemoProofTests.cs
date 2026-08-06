using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Visible proof that the graybox actually RUNS — not just compiles. The
    /// test loads the generated Bootstrap scene, lets the canonical match tick
    /// in real time, asserts the live signals (match ready, kernel running,
    /// session ticks advancing, visible views, the D-077 opening state and a
    /// player-driven Refinery placement) and captures screenshots into
    /// output/demo/ so a human can SEE the Glutrinne blockout without opening
    /// the editor.
    /// <para>
    /// SINCE THE MAIN-MENU SPRINT the scene no longer starts a match by
    /// itself: MatchBootstrap.AutoStart is off and the menu overlay owns the
    /// start, so both tests below open the match explicitly (see
    /// StartMatchTheWayTheMenuDoes). What the menu adds on top of that — the
    /// overlay, its buttons and the settings — is proven by MainMenuTests;
    /// this file stays about the simulation and the render.
    /// </para>
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
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            StartMatchTheWayTheMenuDoes(bootstrap);

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

        /// <summary>
        /// Opens the match exactly where "Neues Spiel" opens it. The Bootstrap
        /// scene loads into an idle host since the main-menu sprint
        /// (MatchBootstrap.AutoStart is off, written by BootstrapSceneGenerator),
        /// so waiting for IsMatchReady would now simply run into the old 15 s
        /// timeout.
        /// <para>
        /// The button itself is not pressed here, deliberately:
        /// MainMenuController sits in Nova.Presentation.UI, which no test
        /// assembly may reference (quality/scripts/run_gate_check.py:183-188),
        /// and its click handler is private. MainMenuTests presses the real
        /// button through the panel and proves that path; this file proves the
        /// match behind it, and calls the same public entry point the button
        /// calls.
        /// </para>
        /// <para>
        /// StartGrayboxMatch() is synchronous and idempotent: it builds the
        /// D-077 opening position before it returns, and it is a no-op if the
        /// scene did start the match on its own — so this also still works
        /// against a Bootstrap.unity generated before the menu existed.
        /// </para>
        /// </summary>
        private static void StartMatchTheWayTheMenuDoes(MatchBootstrap bootstrap)
        {
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady,
                "StartGrayboxMatch() left the host un-ready — the canonical opening position was " +
                "not built, so there is nothing to render and nothing to prove");
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
        public IEnumerator BootstrapMatch_RunsRendersAndStartsTheLoop()
        {
            Directory.CreateDirectory(ShotDir);

            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");

            StartMatchTheWayTheMenuDoes(bootstrap);

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
            Assert.AreEqual(3000L, creditsBaseline,
                "the D-077 opening starts with 3.000 AE and NO income of its own — " +
                "no pre-placed refinery, no starting harvesters");

            Camera camera = Camera.main;
            Assert.NotNull(camera, "no main camera in the Bootstrap scene");
            CaptureFrame(camera, $"{ShotDir}/demo_01_start.png");

            // --- Drive the loop start like a player: place the Refinery ----
            // D-077: the economy grows only once the player builds. The
            // classic first move — the Alliance Refinery (definition id 4)
            // beside the field — enters through the sealed command intake,
            // exactly as device input would submit it.
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(
                    CommandIntent.Create(new PlaceBuildingPayload(4, 8, 4)), out _),
                "the refinery placement intent must enter the sealed stream");

            yield return new WaitForSeconds(2f);

            uint tickLater = runner.Session.CurrentTick;
            long creditsLater = runner.Economy.GetPlayerEconomy(0).AetheriumCredits;

            Assert.Greater(tickLater, tickBaseline, "Ticks stalled while the placement applied");
            Assert.Greater(views.VisibleViewCount, 0, "No entity views are visible in the local team's committed view");
            Assert.AreEqual(1, CountSites(runner, MatchBootstrap.LocalSlot),
                "the player's placed refinery exists as a construction site (counted slot-scoped: " +
                "the skirmish AI builds its own refinery on slot 1 in the same match, so the " +
                "global Construction.SiteCount is no longer a player-only signal)");
            Assert.AreEqual(2300L, creditsLater,
                $"3000 - 700 refinery cost — the D-077 loop start is player-driven (was {creditsBaseline})");

            CaptureFrame(camera, $"{ShotDir}/demo_02_economy.png");
            yield return null;

            Debug.Log($"[GrayboxDemoProof] tick {tickBaseline}->{tickLater}, " +
                      $"credits {creditsBaseline}->{creditsLater} AE (refinery placed), visible views {views.VisibleViewCount}");

            // A written, non-trivial PNG proves the render device actually
            // produced an image (an empty batchmode capture is 0-2 KB).
            foreach (string shot in new[] { $"{ShotDir}/demo_01_start.png", $"{ShotDir}/demo_02_economy.png" })
            {
                Assert.IsTrue(File.Exists(shot), $"screenshot missing: {shot}");
                Assert.Greater(new FileInfo(shot).Length, 10 * 1024, $"screenshot suspiciously small: {shot}");
            }
        }

        /// <summary>Active construction sites owned by one slot (ascending entity-index scan).</summary>
        private static int CountSites(MatchRunner runner, byte slot)
        {
            int count = 0;
            Nova.Simulation.State.UnitState[] units = runner.Entities.RawUnits;
            for (int i = 0; i < runner.Entities.Capacity; i++)
            {
                ref readonly Nova.Simulation.State.UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || u.Role != Nova.Simulation.State.UnitRole.Unit) continue;
                uint raw = Nova.Simulation.State.UnitCommandStateView.ToRawEntityId(u.Id);
                if (raw != 0 && runner.Construction.TryGetSite(raw, out _, out _, out _))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Renders one frame through the given camera into a PNG at a
        /// project-relative path. ScreenCapture.CaptureScreenshot is a silent
        /// no-op in -batchmode (no frame is ever presented), so the proof
        /// renders through a RenderTexture instead — the only path that works
        /// headless-with-graphics. IMGUI overlays (the debug HUD) do not draw
        /// into a RenderTexture, and neither does the main menu: its UIDocument
        /// renders through a screen-space-overlay PanelSettings, which is
        /// composited after all cameras and never into a camera target texture
        /// (MenuAssetSetup.LoadOrCreatePanelSettings). The capture stays
        /// world-only, exactly as before the menu existed.
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
