using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Nova.Core;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.PlayMode.Tests
{
    /// <summary>
    /// Proof in the running match that a unit looks where it drives. Before
    /// <see cref="ViewHeading"/>, the view used the sim heading as a raw Unity
    /// yaw: the simulation measures counter-clockwise from +X, Unity clockwise
    /// from +Z, so every unit was mirrored across the north-east diagonal (a
    /// tank driving east faced north). The EditMode suite pins the mapping;
    /// this test proves the whole chain with real prefabs, real pathfinding
    /// and the real interpolating view: every moving view's forward axis must
    /// lie along its own measured displacement.
    /// <para>
    /// It also renders evidence shots from the RTS camera's pitch into
    /// output/demo/ (headless with graphics; never run with -quit, see
    /// GrayboxDemoProofTests).
    /// </para>
    /// </summary>
    public sealed class UnitMotionPresentationTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string ShotDir = "output/demo";
        private const byte Slot = 0;

        private static readonly (UnitRole Role, int Dx, int Dy)[] Plan =
        {
            (UnitRole.LightTank, 1, 0),      // east
            (UnitRole.BattleTank, 0, 1),     // north
            (UnitRole.ScoutVehicle, -1, 0),  // west
            (UnitRole.Harvester, 0, -1),     // south
            (UnitRole.BasicInfantry, 1, 1),  // north-east
            (UnitRole.Artillery, -1, 1),     // north-west
        };

        [UnityTest]
        public IEnumerator MovingViews_FaceTheirDirectionOfTravel()
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
            CostField cost = runner.Pathfinding.CostField;
            FactionId faction = runner.Economy.GetSlotFaction(Slot);

            // Test-setup spawns (the same legal direct write MatchBootstrap
            // uses for the opening position), each on a free straight lane
            // so the path is a line and the expected heading is unambiguous.
            const int Lane = 12;
            var ids = new List<EntityId>();
            var expected = new List<Vector3>();
            int seedX = 26, seedY = 22;
            foreach ((UnitRole role, int dx, int dy) in Plan)
            {
                Assert.IsTrue(SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition def),
                    $"setup: no {faction} {role} definition");
                Assert.IsTrue(FindOpenLane(cost, seedX, seedY, dx, dy, Lane, out int sx, out int sy),
                    $"setup: no free {Lane}-cell lane for {role} near ({seedX},{seedY})");
                EntityId id = runner.Entities.SpawnUnit(Slot,
                    new Transform2D(SimFixed.FromInt(sx), SimFixed.FromInt(sy)), def.MoveSpeed, role: def.Role);
                Assert.IsTrue(id.IsValid, $"setup: spawn of {role} failed");

                uint raw = UnitCommandStateView.ToRawEntityId(id);
                Assert.AreEqual(CommandIngressResult.Accepted,
                    runner.Ingress.TrySubmitIntent(CommandIntent.Create(new MovePayload(new[] { raw },
                        SimFixed.FromInt(sx + dx * Lane), SimFixed.FromInt(sy + dy * Lane))), out _),
                    $"the move intent for {role} must enter the sealed stream");
                ids.Add(id);
                expected.Add(new Vector3(dx, 0f, dy).normalized);
                seedY += 7;
            }

            // Let the order land and the views get up to speed.
            yield return new WaitForSeconds(1.2f);

            var samples = new int[ids.Count];
            var aligned = new int[ids.Count];
            var worst = new float[ids.Count];
            var lastPos = new Vector3[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                Assert.IsTrue(views.TryGetView(ids[i], out GameObject v) && v != null,
                    $"PRESENTATION: {Plan[i].Role} owns no view — prefab resolution or fog feed broke");
                lastPos[i] = v.transform.position;
            }

            bool shot = false;
            int gaitShots = 0;
            float start = Time.time;
            float until = Time.time + 1.6f;
            while (Time.time < until)
            {
                yield return null;
                for (int i = 0; i < ids.Count; i++)
                {
                    if (!views.TryGetView(ids[i], out GameObject v) || v == null) continue;
                    Vector3 p = v.transform.position;
                    Vector3 d = p - lastPos[i];
                    d.y = 0f;
                    lastPos[i] = p;
                    if (d.magnitude < 0.3f * Time.deltaTime) continue;   // standing: nothing to check
                    samples[i]++;
                    float angle = Vector3.Angle(Flat(v.transform.forward), d);
                    if (angle < 25f) aligned[i]++;
                    worst[i] = Mathf.Max(worst[i], angle);
                }

                // A close strip of the walking soldier (legs are rigged since the
                // infantry pass): four frames 0.09 s apart.
                int infantry = System.Array.FindIndex(Plan, p => p.Role == UnitRole.BasicInfantry);
                if (gaitShots < 4 && Time.time > start + 0.2f + gaitShots * 0.09f
                    && views.TryGetView(ids[infantry], out GameObject soldier) && soldier != null)
                {
                    CaptureRts($"{ShotDir}/motion_infantry_{gaitShots}.png", soldier.transform.position, 3.2f);
                    gaitShots++;
                }

                if (!shot && Time.time > until - 0.8f)
                {
                    shot = true;
                    Vector3 c = Centroid(views, ids);
                    CaptureRts($"{ShotDir}/motion_01_rts_close.png", c, 26f);
                    CaptureRts($"{ShotDir}/motion_02_rts_default.png", c, 42f);
                }
            }

            for (int i = 0; i < ids.Count; i++)
            {
                Assert.Greater(samples[i], 5, $"{Plan[i].Role} never moved — no evidence for its heading");
                Assert.GreaterOrEqual(aligned[i], (int)(samples[i] * 0.8f),
                    $"{Plan[i].Role}: the view faced its direction of travel in only {aligned[i]}/{samples[i]} " +
                    $"moving frames (worst {worst[i]:F0} deg) — the sim-to-yaw mapping or the prefab's forward axis is off");
                Debug.Log($"[UnitMotionPresentation] {Plan[i].Role}: {aligned[i]}/{samples[i]} frames aligned, worst {worst[i]:F1} deg, expected {expected[i]}");
            }
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        /// <summary>
        /// First walkable start cell near the seed whose whole lane (three
        /// cells wide) in the move direction is walkable.
        /// </summary>
        private static bool FindOpenLane(CostField cost, int seedX, int seedY, int dx, int dy, int len,
            out int sx, out int sy)
        {
            for (int r = 0; r < 30; r++)
            {
                for (int oy = -r; oy <= r; oy++)
                {
                    for (int ox = -r; ox <= r; ox++)
                    {
                        if (Mathf.Max(Mathf.Abs(ox), Mathf.Abs(oy)) != r) continue;
                        int x = seedX + ox, y = seedY + oy;
                        if (LaneIsOpen(cost, x, y, dx, dy, len))
                        {
                            sx = x;
                            sy = y;
                            return true;
                        }
                    }
                }
            }
            sx = sy = 0;
            return false;
        }

        private static bool LaneIsOpen(CostField cost, int x, int y, int dx, int dy, int len)
        {
            for (int k = -1; k <= len + 1; k++)
            {
                for (int side = -1; side <= 1; side++)
                {
                    int cx = x + dx * k + (dy != 0 ? side : 0);
                    int cy = y + dy * k + (dx != 0 ? side : 0);
                    if (!cost.IsInBounds(cx, cy) || !cost.IsWalkable((ushort)cx, (ushort)cy)) return false;
                }
            }
            return true;
        }

        private static Vector3 Centroid(UnitViewManager views, List<EntityId> ids)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (EntityId id in ids)
            {
                if (views.TryGetView(id, out GameObject v) && v != null)
                {
                    sum += v.transform.position;
                    n++;
                }
            }
            return n > 0 ? sum / n : Vector3.zero;
        }

        /// <summary>
        /// The RTS camera's 55-degree pitch at a given height, looking at a
        /// point. Renders through the scene's MAIN camera (moved, then put
        /// back) so the shot carries what the player sees — post-processing
        /// included, which a fresh test camera would not have opted into.
        /// </summary>
        private static void CaptureRts(string path, Vector3 focus, float height)
        {
            Camera camera = Camera.main;
            Assert.NotNull(camera, "no main camera in the Bootstrap scene");
            Transform t = camera.transform;
            Vector3 oldPos = t.position;
            Quaternion oldRot = t.rotation;
            // LDR target: URP renders HDR internally and resolves bloom into it.
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
