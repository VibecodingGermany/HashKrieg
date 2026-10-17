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
    /// Evidence shots of the whole art set as the player sees it: every
    /// building and unit of both factions, placed in the running match at
    /// its real in-game scale, photographed through the main camera (and so
    /// with the project's post-processing) from three RTS camera heights.
    /// It exists because close-up previews (ArtPreviewCapture) say nothing
    /// about readability at 30-40 m camera height, which is where an art
    /// pass is won or lost.
    /// <para>
    /// The assertions only guard that every placed entity owns a view; the
    /// value is in output/demo/showcase_*.png. Headless with graphics, never
    /// with -quit (see GrayboxDemoProofTests).
    /// </para>
    /// </summary>
    public sealed class ArtShowcaseTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";

        private static readonly UnitRole[] Buildings =
        {
            UnitRole.HQ, UnitRole.Power, UnitRole.Refinery, UnitRole.Storage, UnitRole.Barracks,
            UnitRole.VehicleFactory, UnitRole.ResearchLab, UnitRole.Radar, UnitRole.DefensePlatform,
        };

        private static readonly UnitRole[] Units =
        {
            UnitRole.Builder, UnitRole.Harvester, UnitRole.BasicInfantry, UnitRole.AntiArmorInfantry,
            UnitRole.ScoutVehicle, UnitRole.LightTank, UnitRole.BattleTank, UnitRole.Artillery,
        };

        [UnityTest]
        public IEnumerator Showcase_BothFactionsAtRtsDistance()
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

            // Legion sits in the fog of the local Alliance player; the editor
            // reveal is the same switch the lab uses to watch the AI play.
            if (!FogRevealDebug.RevealAll) FogRevealDebug.Toggle();
            try
            {
                var placed = new List<EntityId>();
                Vector3 allianceCentre = Lay(runner, 0, 22, 6, +1, placed);
                Vector3 legionCentre = Lay(runner, 1, 102, 118, -1, placed);

                // Let views spawn, rigs settle and the glow pulse run.
                yield return new WaitForSeconds(2.5f);

                foreach (EntityId id in placed)
                {
                    Assert.IsTrue(views.TryGetView(id, out GameObject view) && view != null,
                        $"entity {id} owns no view — prefab resolution or fog feed broke");
                }

                foreach ((string name, Vector3 centre) in new[] { ("alliance", allianceCentre), ("legion", legionCentre) })
                {
                    foreach (float height in new[] { 18f, 30f, 42f })
                    {
                        CaptureRts($"{ShotDir}/showcase_{name}_{height:0}m.png", centre, height);
                    }
                }
            }
            finally
            {
                if (FogRevealDebug.RevealAll) FogRevealDebug.Toggle();
            }
        }

        /// <summary>
        /// Places all nine buildings (completed) in a 5-wide row grid and the
        /// eight units in a row in front of them, walking from the start cell
        /// in direction <paramref name="dir"/> (+1 east/north, -1 west/south).
        /// Returns the world centre of the layout.
        /// </summary>
        private static Vector3 Lay(MatchRunner runner, byte slot, int x0, int y0, int dir, List<EntityId> placed)
        {
            FactionId faction = runner.Economy.GetSlotFaction(slot);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < Buildings.Length; i++)
            {
                Assert.IsTrue(SimDefinitions.TryGetBuilding(faction, Buildings[i], out SimBuildingDefinition def),
                    $"setup: no {faction} {Buildings[i]} definition");
                int ox = x0 + dir * (i % 5) * 5;
                int oy = y0 + dir * (i / 5) * 5;
                EntityId id = PlaceNear(runner, slot, def.DefinitionId, ox, oy);
                Assert.IsTrue(id.IsValid, $"setup: {faction} {Buildings[i]} could not be placed near ({ox},{oy})");
                placed.Add(id);
                Grow(ref minX, ref maxX, ref minY, ref maxY, ox + 1.5f, oy + 1.5f);
            }

            int uy = y0 + dir * 11;
            for (int i = 0; i < Units.Length; i++)
            {
                Assert.IsTrue(SimDefinitions.TryGetUnit(faction, Units[i], out SimUnitDefinition def),
                    $"setup: no {faction} {Units[i]} definition");
                int ux = x0 + dir * i * 3;
                EntityId id = runner.Entities.SpawnUnit(slot,
                    new Transform2D(SimFixed.FromInt(ux), SimFixed.FromInt(uy)), def.MoveSpeed, role: def.Role);
                Assert.IsTrue(id.IsValid, $"setup: spawn of {faction} {Units[i]} failed");
                placed.Add(id);
                Grow(ref minX, ref maxX, ref minY, ref maxY, ux, uy);
            }

            return new Vector3((minX + maxX) * 0.5f, 0f, (minY + maxY) * 0.5f);
        }

        private static EntityId PlaceNear(MatchRunner runner, byte slot, ushort defId, int x, int y)
        {
            for (int r = 0; r < 4; r++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                        EntityId id = runner.Construction.PlaceCompletedBuilding(slot, defId, x + dx, y + dy);
                        if (id.IsValid) return id;
                    }
                }
            }
            return EntityId.Invalid;
        }

        private static void Grow(ref float minX, ref float maxX, ref float minY, ref float maxY, float x, float y)
        {
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }

        /// <summary>
        /// The RTS camera's 55-degree pitch at a given height, rendered through
        /// the scene's main camera (moved, then put back) so post-processing
        /// is included.
        /// </summary>
        private static void CaptureRts(string path, Vector3 focus, float height)
        {
            Camera camera = Camera.main;
            Assert.NotNull(camera, "no main camera in the Bootstrap scene");
            Transform t = camera.transform;
            Vector3 oldPos = t.position;
            Quaternion oldRot = t.rotation;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            try
            {
                Quaternion rot = Quaternion.Euler(55f, 0f, 0f);
                float distance = height / Mathf.Sin(55f * Mathf.Deg2Rad);
                t.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
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
