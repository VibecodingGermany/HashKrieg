using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    /// Destroyed units stay on the field (D-115): in the running match, every
    /// own mobile role is spawned and then removed from the simulation; each
    /// art view must turn into a wreck that is still there seconds later,
    /// with its live model off and its wreck mesh on. Without art (fresh
    /// clone, graybox primitives) the views keep the short fade-out, and the
    /// test only proves that no wreck appears out of nowhere.
    /// Evidence shots go to output/demo/wrecks_*.png.
    /// </summary>
    public sealed class WreckPresentationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";

        private static readonly UnitRole[] Roles =
        {
            UnitRole.Builder, UnitRole.Harvester, UnitRole.BasicInfantry, UnitRole.AntiArmorInfantry,
            UnitRole.ScoutVehicle, UnitRole.LightTank, UnitRole.BattleTank, UnitRole.Artillery,
        };

        [UnityTest]
        public IEnumerator DestroyedUnits_LeaveWrecksThatStay()
        {
            Directory.CreateDirectory(ShotDir);
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            var bootstrap = Object.FindAnyObjectByType<MatchBootstrap>();
            Assert.NotNull(bootstrap, "Bootstrap scene contains no MatchBootstrap");
            bootstrap.StartGrayboxMatch();
            Assert.IsTrue(bootstrap.IsMatchReady);
            MatchRunner runner = bootstrap.Runner;
            var views = Object.FindAnyObjectByType<UnitViewManager>();
            Assert.NotNull(views);
            FactionId faction = runner.Economy.GetSlotFaction(0);

            var ids = new List<EntityId>();
            var wreckViews = new List<GameObject>();
            for (int i = 0; i < Roles.Length; i++)
            {
                Assert.IsTrue(SimDefinitions.TryGetUnit(faction, Roles[i], out SimUnitDefinition def));
                EntityId id = runner.Entities.SpawnUnit(0,
                    new Transform2D(SimFixed.FromInt(24 + (i % 4) * 3), SimFixed.FromInt(20 + (i / 4) * 3)),
                    def.MoveSpeed, role: def.Role);
                Assert.IsTrue(id.IsValid);
                ids.Add(id);
            }
            yield return new WaitForSeconds(0.6f);

            int withWreck = 0;
            foreach (EntityId id in ids)
            {
                Assert.IsTrue(views.TryGetView(id, out GameObject view) && view != null, $"{id} owns no view");
                if (ViewWrecks.FindWreck(view) != null)
                {
                    withWreck++;
                    wreckViews.Add(view);
                }
            }

            // Kill them the way the game does: inside a simulation tick, by
            // combat. (Removing them between ticks would let the views vanish
            // before the frame differ sees a death.) One hit point each, and
            // enemy battle tanks right next to them; auto-acquire does the rest.
            foreach (EntityId id in ids) SetHealth(runner, id, 1);
            FactionId enemy = runner.Economy.GetSlotFaction(1);
            Assert.IsTrue(SimDefinitions.TryGetUnit(enemy, UnitRole.BattleTank, out SimUnitDefinition tank));
            for (int k = 0; k < 4; k++)
            {
                runner.Entities.SpawnUnit(1, new Transform2D(SimFixed.FromInt(23 + k * 4), SimFixed.FromInt(26)),
                    tank.MoveSpeed, maxHealth: tank.MaxHealth, role: tank.Role);
            }

            float deadline = Time.time + 12f;
            while (Time.time < deadline && ids.Exists(id => runner.Entities.TryGetUnit(id, out _)))
            {
                yield return null;
            }
            Assert.IsFalse(ids.Exists(id => runner.Entities.TryGetUnit(id, out _)),
                "setup: the enemy tanks did not kill the one-hit-point units in time");
            yield return new WaitForSeconds(2.5f);

            Assert.That(views.WreckCount, Is.EqualTo(withWreck),
                "every art view with a wreck child must stay as a wreck after its death");
            foreach (GameObject wreckView in wreckViews)
            {
                Assert.IsTrue(wreckView.activeInHierarchy, $"{wreckView.name}: wreck vanished");
                Assert.IsTrue(ViewWrecks.FindWreck(wreckView).gameObject.activeSelf, $"{wreckView.name}: wreck mesh not shown");
            }

            if (withWreck > 0)
            {
                CaptureRts($"{ShotDir}/wrecks_10m.png", new Vector3(28.5f, 0f, 22f), 10f);
            }
            Debug.Log($"[WreckPresentation] {withWreck}/{ids.Count} views left a wreck");
        }

        /// <summary>Test-setup write (iterators cannot hold ref locals).</summary>
        private static void SetHealth(MatchRunner runner, EntityId id, int health)
        {
            ref UnitState u = ref runner.Entities.GetUnitRef(id);
            u.CurrentHealth = health;
        }

        private static void CaptureRts(string path, Vector3 focus, float height)
        {
            Camera camera = Camera.main;
            Transform t = camera.transform;
            Vector3 oldPos = t.position;
            Quaternion oldRot = t.rotation;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                Quaternion rot = Quaternion.Euler(55f, 0f, 0f);
                t.SetPositionAndRotation(focus - rot * Vector3.forward * (height / Mathf.Sin(55f * Mathf.Deg2Rad)), rot);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0f, 0f, 1600f, 900f), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                RenderTexture.active = null;
                Object.DestroyImmediate(tex);
            }
            finally
            {
                camera.targetTexture = null;
                t.SetPositionAndRotation(oldPos, oldRot);
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }
    }
}
