using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Core;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Proof in the running match that a harvester view shows its load: the
    /// procedural cargo pile grows with <c>UnitState.CargoAE</c> and the dump
    /// bed tips back when the cargo drops to zero (the sim empties a
    /// harvester in one tick, so the unload animation is presentation only and
    /// keyed on the cargo &gt; 0 to 0 edge). Needs the art prefabs built with
    /// the <c>Bed</c> / <c>Cargo_Anchor</c> nodes (ArtAssetPrefabBuilder).
    /// <para>Frames are awaited with <c>yield return null</c> only; never
    /// <c>WaitForEndOfFrame</c> under batchmode.</para>
    /// </summary>
    public sealed class HarvesterCargoPresentationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const byte Slot = 0;

        private static IEnumerator Settle(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until) yield return null;
        }

        [UnityTest]
        public IEnumerator HarvesterView_PileGrowsWithCargo_AndBedTipsAfterUnload()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);

            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady, "the canonical opening position was not built");

            MatchRunner runner = bootstrap.Runner;
            var views = Object.FindAnyObjectByType<UnitViewManager>();
            Assert.NotNull(views, "Bootstrap scene contains no UnitViewManager");

            FactionId faction = runner.Economy.GetSlotFaction(Slot);
            Assert.IsTrue(SimDefinitions.TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition def));
            int capacity = SimDefinitions.HarvesterCargoCapacityAE(faction);

            EntityId id = runner.Entities.SpawnUnit(Slot,
                new Transform2D(SimFixed.FromInt(26), SimFixed.FromInt(22)), def.MoveSpeed, role: def.Role);
            Assert.IsTrue(id.IsValid, "setup: harvester spawn failed");

            yield return Settle(0.6f);
            Assert.IsTrue(views.TryGetView(id, out GameObject view) && view != null, "the harvester owns no view");
            var rig = view.GetComponent<UnitRigAnimator>();
            Assert.NotNull(rig, "the art harvester view carries no rig animator (graybox fallback?)");
            Assert.IsTrue(rig.CarriesCargo,
                "the harvester prefab has neither a Bed nor a Cargo_Anchor node: rebuild it with the art pass");
            CargoPile pile = rig.Pile;
            Assert.IsFalse(pile.Visible, "an empty harvester must not show a pile");

            // --- the pile grows with the load, smoothly --------------------------------
            float lastY = 0f;
            int[] steps = { capacity / 4, capacity / 2, capacity };
            foreach (int cargo in steps)
            {
                runner.Entities.GetUnitRef(id).CargoAE = cargo;
                yield return Settle(1.2f);
                float y = pile.MoundScale.y;
                Assert.IsTrue(pile.Visible, $"a harvester carrying {cargo} AE shows no pile");
                Assert.That(pile.Fill, Is.EqualTo(cargo / (float)capacity).Within(0.03f));
                Assert.Greater(y, lastY + 1e-4f, $"the pile did not grow when the load rose to {cargo} AE");
                lastY = y;
            }

            // --- unload: the bed tips back, then returns ------------------------------
            Transform bed = rig.BedTransform;
            Quaternion rest = bed != null ? bed.localRotation : Quaternion.identity;
            runner.Entities.GetUnitRef(id).CargoAE = 0;
            float maxAngle = 0f, maxTilt = 0f;
            bool sawDump = false;
            float until = Time.time + 1.2f;
            while (Time.time < until)
            {
                yield return null;
                sawDump |= rig.Dumping;
                maxTilt = Mathf.Max(maxTilt, rig.BedTilt01);
                if (bed != null) maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(rest, bed.localRotation));
            }
            Assert.IsTrue(sawDump, "cargo > 0 to 0 did not start the dump animation");
            Assert.Greater(maxTilt, 0.9f, "the dump never reached its tipped pose");
            if (bed != null)
            {
                Assert.Greater(maxAngle, 40f, $"the bed rotated only {maxAngle:F1} degrees");
            }

            yield return Settle(HarvesterDumpCycle.TotalSeconds);
            Assert.IsFalse(rig.Dumping, "the dump animation never ended");
            Assert.IsFalse(pile.Visible, "the pile stayed behind after the dump");
            if (bed != null)
            {
                Assert.Less(Quaternion.Angle(rest, bed.localRotation), 1f, "the bed did not return");
            }

            // A load that arrives later starts the pile again from empty.
            runner.Entities.GetUnitRef(id).CargoAE = capacity / 2;
            yield return Settle(1.2f);
            Assert.IsTrue(pile.Visible, "the pile did not come back with the next load");
        }
    }
}
