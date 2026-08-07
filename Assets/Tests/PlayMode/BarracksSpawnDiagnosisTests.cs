using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// DIAGNOSIS FIRST (sprint 09 section 2.1, the barracks half): the owner
    /// report is "the production bar runs but no unit appears". The sim suite
    /// already proves the spawn path in isolation
    /// (ProductionSystemTests.Production_SpawnsAtDefaultRally_AfterExactBuildTicks),
    /// so this test reproduces the exact in-game situation in the generated
    /// Bootstrap scene — a COMPLETED Alliance barracks, one infantry queued
    /// through the real sealed command path — and then measures BOTH sides of
    /// the question the F3 panel would answer interactively:
    /// <list type="number">
    /// <item>SIM: does the entity count rise (a BasicInfantry entity exists
    /// after BuildTicks)?</item>
    /// <item>PRESENTATION: does the spawned entity own a live
    /// <see cref="UnitViewManager"/> view with an enabled renderer, and does a
    /// camera at the RTS rig's distance actually draw it (a RenderTexture
    /// capture, inspected by a human, next to a close-up that controls the
    /// LOD-culling hypothesis)?</item>
    /// </list>
    /// Whichever side fails localises the defect: a sim failure means one of
    /// the two documented silent pause paths of ProductionSystem.ExecuteTick
    /// fired (entity store full / no free spawn cell in eight rings); a view
    /// failure with a passing sim count means the art-prefab view path
    /// (ResolveViewPrefab, scale normalization, LOD thresholds) renders the
    /// infantry invisibly.
    /// <para>
    /// Run headless-with-graphics (NO -nographics, captures need a render
    /// device) and NEVER with -quit:
    ///   Unity -batchmode -projectPath &lt;repo&gt; -runTests -testPlatform PlayMode \
    ///     -testFilter BarracksSpawnDiagnosisTests \
    ///     -testResults &lt;abs&gt;/output/playmode-results.xml -logFile output/playmode-tests.log
    /// </para>
    /// </summary>
    public sealed class BarracksSpawnDiagnosisTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";

        /// <summary>Alliance Barracks definition id (SimDefinitions row 7) and its BasicInfantry product (row 12).</summary>
        private const ushort BarracksDefId = 7;
        private const ushort InfantryDefId = 12;

        /// <summary>Barracks footprint origin; centre (11,11) -> default rally cell (13,11), well clear of the HQ footprint (4,4)-(6,6).</summary>
        private const int BarracksOriginX = 10;
        private const int BarracksOriginY = 10;

        [UnityTest]
        public IEnumerator BarracksQueue_InfantrySpawnsInSimulation_AndGetsAVisibleView()
        {
            Directory.CreateDirectory(ShotDir);
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady, "the canonical opening position was not built");

            MatchRunner runner = bootstrap.Runner;
            var views = Object.FindAnyObjectByType<UnitViewManager>();
            Assert.NotNull(views, "Bootstrap scene contains no UnitViewManager");

            // A COMPLETED barracks, placed programmatically: the defect report
            // starts AFTER the construction phase, so the test enters there —
            // the build walk itself is Sprint-10-proven.
            EntityId barracks = runner.Construction.PlaceCompletedBuilding(
                MatchBootstrap.LocalSlot, BarracksDefId, BarracksOriginX, BarracksOriginY);
            Assert.IsTrue(barracks.IsValid, "programmatic barracks placement failed");
            uint rawBarracks = UnitCommandStateView.ToRawEntityId(barracks);

            // The queue order travels the REAL sealed command path — the same
            // record the command card's button and the Shift+Q hotkey produce.
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                runner.Ingress.TrySubmitIntent(
                    CommandIntent.Create(new QueueUnitPayload(rawBarracks, InfantryDefId, 1)), out _),
                "the infantry queue intent must enter the sealed stream");

            // Let the queue apply, then confirm the bar's data source: the
            // producer row with one progressing entry (the owner's "bar runs").
            yield return new WaitForSeconds(1.5f);
            Assert.IsTrue(runner.Production.TryGetProducer(rawBarracks, out int entryCount, out _, out _),
                "no producer row after the queue order");
            Assert.AreEqual(1, entryCount, "the queue holds no entry — the bar could not run either");

            int infantryBaseline = CountRole(runner, UnitRole.BasicInfantry);

            // Infantry BuildTicks = 100 at full power (HQ 30 provided, Barracks
            // 15 required) = 10 s; 14 s leaves margin for the input delay and
            // batchmode frame pacing.
            yield return new WaitForSeconds(14f);

            // ---- SIM VERDICT: did the entity appear in the store? ----
            int infantrySim = CountRole(runner, UnitRole.BasicInfantry);
            EntityId infantry = FindRole(runner, UnitRole.BasicInfantry);

            // ---- PRESENTATION VERDICT: does a live view render it? ----
            bool viewExists = false;
            bool anyRendererEnabled = false;
            string viewDescription = "no view";
            if (infantry.IsValid && views.TryGetView(infantry, out GameObject view) && view != null)
            {
                viewExists = true;
                Renderer[] renderers = view.GetComponentsInChildren<Renderer>(true);
                int enabledCount = 0;
                var bounds = new Bounds(view.transform.position, Vector3.zero);
                foreach (Renderer r in renderers)
                {
                    if (r != null && r.enabled && r.gameObject.activeInHierarchy) enabledCount++;
                    if (r != null) bounds.Encapsulate(r.bounds);
                }
                anyRendererEnabled = enabledCount > 0;
                viewDescription =
                    $"view '{view.name}' pos={view.transform.position} scale={view.transform.localScale} " +
                    $"renderers={renderers.Length} enabled={enabledCount} bounds={bounds.size}";
            }
            Debug.Log($"[BarracksSpawnDiagnosis] infantrySim={infantrySim} viewExists={viewExists} " +
                      $"anyRendererEnabled={anyRendererEnabled} | {viewDescription}");

            // ---- VISUAL VERDICT: what a player at the barracks would see ----
            if (infantry.IsValid && views.TryGetView(infantry, out GameObject seen) && seen != null)
            {
                Vector3 at = seen.transform.position;
                // Close-up (controls mesh/material health) and the RTS rig's
                // own distance (controls the LOD-culling hypothesis).
                CaptureFrom($"{ShotDir}/diag_10_infantry_closeup.png",
                    at + new Vector3(3.5f, 4f, -3.5f), at);
                CaptureFrom($"{ShotDir}/diag_11_infantry_rts_distance.png",
                    at + new Vector3(0f, 37f, -21f), at);
            }

            Assert.AreEqual(infantryBaseline + 1, infantrySim,
                "SIM DEFECT: the barracks queue ran but no BasicInfantry entity exists — one of the two " +
                "silent ProductionSystem pause paths fired (entity store full / no free spawn cell)");
            Assert.IsTrue(viewExists,
                "PRESENTATION DEFECT: the infantry exists in the sim but owns no view — UnitViewManager never rendered it");
            Assert.IsTrue(anyRendererEnabled,
                $"PRESENTATION DEFECT: the infantry view has no enabled renderer — {viewDescription}");
        }

        /// <summary>Active entities of one role across both slots (ascending store scan).</summary>
        private static int CountRole(MatchRunner runner, UnitRole role)
        {
            int count = 0;
            UnitState[] units = runner.Entities.RawUnits;
            for (int i = 0; i < runner.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.Role == role) count++;
            }
            return count;
        }

        private static EntityId FindRole(MatchRunner runner, UnitRole role)
        {
            UnitState[] units = runner.Entities.RawUnits;
            for (int i = 0; i < runner.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.Role == role) return u.Id;
            }
            return EntityId.Invalid;
        }

        /// <summary>Renders one frame from a throwaway camera posed at <paramref name="position"/> looking at <paramref name="target"/>.</summary>
        private static void CaptureFrom(string path, Vector3 position, Vector3 target)
        {
            var go = new GameObject("DiagnosisCamera");
            try
            {
                var camera = go.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.13f, 0.16f, 0.22f, 1f);
                camera.transform.position = position;
                camera.transform.LookAt(target);

                const int width = 1600, height = 900;
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
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
