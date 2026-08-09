using System.Collections.Generic;
using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Canonical Fog of War suite (.NET lane, docs/tech/FogOfWar.md, D-058):
    /// state transitions, the 5 Hz commit cadence, exact radius rasterization,
    /// team separation, radar pings without targeting right, the
    /// Hidden-World metamorphic of Testing.md section 5, snapshot
    /// roundtrip/continuation and two-kernel determinism.
    /// Mirror of the .NET lane FogOfWarSystemTests.
    /// </summary>
    [TestFixture]
    public class FogOfWarSystemTests
    {
        private const ulong Seed = 0xF06UL;
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>
        /// Canonical host in the SimulationCore.md section 2 order:
        /// pathfinding, movement, then the FoW recompute.
        /// </summary>
        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public ConstructionSystem Construction { get; }
            public FogOfWarSystem Fog { get; }

            private TestHost(SimulationKernel kernel, EntityManager entities, EconomySystem economy, ConstructionSystem construction, FogOfWarSystem fog)
            {
                Kernel = kernel;
                Entities = entities;
                Economy = economy;
                Construction = construction;
                Fog = fog;
            }

            public static TestHost Create(ulong seed, int capacity = 64, ushort width = 64, ushort height = 64)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(width, height);
                var movement = new MovementSystem(entities, pathfinding);
                // 16.5/16.6: the FoW radar read requires the placement register
                // and the power balance — an unregistered economy/construction
                // pair answers both queries without ever ticking.
                var economy = new EconomySystem(entities);
                var construction = new ConstructionSystem(entities, economy);
                var fog = new FogOfWarSystem(entities, construction, economy, teamCount: 2, width, height);

                var kernel = new SimulationKernel(new SimRandom(seed));
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fog);
                kernel.Start();
                return new TestHost(kernel, entities, economy, construction, fog);
            }

            public void Step() => Kernel.StepTick();

            public void Step(int count)
            {
                for (int i = 0; i < count; i++) Kernel.StepTick();
            }
        }

        private static EntityId SpawnAt(TestHost host, byte team, int cellX, int cellY, int sightRadius)
        {
            return host.Entities.SpawnUnit(
                team,
                new Transform2D(SimFixed.FromInt(cellX) + HalfCell, SimFixed.FromInt(cellY) + HalfCell),
                SimFixed.FromInt(5),
                sightRadius: SimFixed.FromInt(sightRadius));
        }

        private static void Teleport(TestHost host, EntityId id, int cellX, int cellY)
        {
            ref UnitState u = ref host.Entities.GetUnitRef(id);
            u.Transform = new Transform2D(
                SimFixed.FromInt(cellX) + HalfCell,
                SimFixed.FromInt(cellY) + HalfCell,
                u.Transform.Rotation);
        }

        /// <summary>Serialized FoW block: 11-byte header, then team masks in ascending team order.</summary>
        private const int FogBlockHeaderBytes = 11;

        private static byte[] FogBlock(TestHost host)
        {
            var writer = new SnapshotBlockWriter();
            host.Fog.WriteState(writer);
            return writer.ToArray();
        }

        private static byte[] TeamMask(TestHost host, int team, int cellCount)
        {
            byte[] block = FogBlock(host);
            var mask = new byte[cellCount];
            System.Array.Copy(block, FogBlockHeaderBytes + team * cellCount, mask, 0, cellCount);
            return mask;
        }

        [Test]
        public void Transitions_UnexploredToVisibleToExplored_NeverBackwards()
        {
            var host = TestHost.Create(Seed);
            EntityId unit = SpawnAt(host, 0, 10, 10, sightRadius: 5);
            TeamView view = host.Fog.GetTeamView(0);

            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Unexplored));

            host.Step(2); // first recompute at tick 2
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Visible));

            Teleport(host, unit, 40, 40);
            host.Step(2); // recompute at tick 4
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Explored),
                "a cell that lost live sight must demote to Explored");
            Assert.That(view.GetCellState(40, 40), Is.EqualTo(VisionState.Visible));

            host.Step(4); // further recomputes without sight on (10,10)
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Explored),
                "Explored must never fall back to Unexplored");
        }

        [Test]
        public void Unexplored_IsOnlyLeftViaVisible()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 10, 10, sightRadius: 5);
            TeamView view = host.Fog.GetTeamView(0);

            host.Step(10); // five recomputes

            // Far outside every sight circle: never Visible, never Explored.
            Assert.That(view.GetCellState(30, 30), Is.EqualTo(VisionState.Unexplored));
            Assert.That(view.GetCellState(10, 30), Is.EqualTo(VisionState.Unexplored));
            // Inside the standing circle: live sight.
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Visible));
        }

        [Test]
        public void Recompute_OnlyOnEvenTicks_ViewStaysCommittedBetweenRecomputes()
        {
            var host = TestHost.Create(Seed);
            EntityId unit = SpawnAt(host, 0, 10, 10, sightRadius: 5);
            TeamView view = host.Fog.GetTeamView(0);

            host.Step(); // tick 1 (odd): no recompute
            Assert.That(host.Fog.HasCommittedView, Is.False);
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Unexplored),
                "a cell entered before the first even tick is not yet visible");

            host.Step(); // tick 2 (even): first recompute
            Assert.That(host.Fog.HasCommittedView, Is.True);
            Assert.That(host.Fog.LastRecomputeTick, Is.EqualTo(2u));
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Visible));

            Teleport(host, unit, 20, 20);
            host.Step(); // tick 3 (odd): the committed mask still governs
            Assert.That(view.GetCellState(20, 20), Is.EqualTo(VisionState.Unexplored),
                "between recomputes no fresh sight may appear");
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Visible),
                "between recomputes the old committed mask stays in force");
            Assert.That(host.Fog.LastRecomputeTick, Is.EqualTo(2u));

            host.Step(); // tick 4 (even): the change becomes visible exactly at the commit
            Assert.That(host.Fog.LastRecomputeTick, Is.EqualTo(4u));
            Assert.That(view.GetCellState(20, 20), Is.EqualTo(VisionState.Visible));
            Assert.That(view.GetCellState(10, 10), Is.EqualTo(VisionState.Explored));
        }

        [Test]
        public void SightCircle_RasterizesExactCellCenterRule()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 32, 32, sightRadius: 5);
            TeamView view = host.Fog.GetTeamView(0);

            host.Step(2);

            // Cell-center rule: dx^2 + dy^2 <= r^2, exact, boundary inclusive.
            Assert.That(view.IsVisible(32, 32), Is.True, "center");
            Assert.That(view.IsVisible(37, 32), Is.True, "exactly radius on the axis (25 <= 25)");
            Assert.That(view.IsVisible(38, 32), Is.False, "radius + 1 on the axis (36 > 25)");
            Assert.That(view.IsVisible(36, 35), Is.True, "diagonal 4/3 (16 + 9 = 25 <= 25)");
            Assert.That(view.IsVisible(36, 36), Is.False, "diagonal 4/4 (16 + 16 = 32 > 25)");
            Assert.That(view.IsVisible(35, 35), Is.True, "diagonal 3/3 (9 + 9 = 18 <= 25)");
        }

        [Test]
        public void Teams_HaveFullySeparateMasks()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 10, 10, sightRadius: 6);
            SpawnAt(host, 1, 50, 50, sightRadius: 6);

            host.Step(2);

            TeamView view0 = host.Fog.GetTeamView(0);
            TeamView view1 = host.Fog.GetTeamView(1);
            Assert.That(view0.IsVisible(10, 10), Is.True);
            Assert.That(view0.GetCellState(50, 50), Is.EqualTo(VisionState.Unexplored),
                "team 0 must not share team 1's sight");
            Assert.That(view1.IsVisible(50, 50), Is.True);
            Assert.That(view1.GetCellState(10, 10), Is.EqualTo(VisionState.Unexplored),
                "team 1 must not share team 0's sight");
        }

        [Test]
        public void GetVisibleEntities_OwnAlways_ForeignOnlyInVisibleCells()
        {
            var host = TestHost.Create(Seed);
            EntityId observer = SpawnAt(host, 0, 10, 10, sightRadius: 8);
            EntityId nearEnemy = SpawnAt(host, 1, 14, 10, sightRadius: 2); // inside the circle
            EntityId farEnemy = SpawnAt(host, 1, 30, 10, sightRadius: 2); // outside

            host.Step(2);

            var visible = new List<EntityId>();
            host.Fog.GetVisibleEntities(0, visible);
            Assert.That(visible.Contains(observer), Is.True, "own units are always visible");
            Assert.That(visible.Contains(nearEnemy), Is.True, "a foreign unit in a Visible cell is listed");
            Assert.That(visible.Contains(farEnemy), Is.False, "a foreign unit outside sight is hidden");

            // Team 1's units have sight 2: neither reaches the observer at
            // (10,10), so team 1 sees exactly its own two units.
            var enemyView = new List<EntityId>();
            host.Fog.GetVisibleEntities(1, enemyView);
            Assert.That(enemyView.Contains(observer), Is.False, "team 1 does not see team 0's unit");
            Assert.That(enemyView.Count, Is.EqualTo(2), "team 1 sees exactly its own two units");
        }

        [Test]
        public void RadarSignature_PingsWithoutTargetingRight()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 10, 10, sightRadius: 5); // sight observer, NOT a radar source
            // 16.5 (#54): only a COMPLETED Radar building radiates — centre
            // (10,15), coverage = its sight radius 10 x 2 = 20. NOTE: the
            // building also contributes plain SIGHT (radius 10), so a ping
            // target must hide from both radii.
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 10, 9, 14).IsValid, Is.True,
                "Alliance Radar (def 10) as a completed placement");
            SpawnAt(host, 1, 24, 10, sightRadius: 5); // inside radar coverage (dx 14 <= 20), outside both sights (14 > 10, 14 > 5)
            SpawnAt(host, 1, 40, 10, sightRadius: 5); // outside radar (dx 30 > 20)
            SpawnAt(host, 1, 13, 10, sightRadius: 5); // inside sight: a target, not a ping

            host.Step(2);

            var pings = new List<RadarSignature>();
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(1), "exactly the radar-covered hidden enemy pings");
            Assert.That(pings[0].GridX, Is.EqualTo(24));
            Assert.That(pings[0].GridY, Is.EqualTo(10));

            // Contract: the ping grants no targeting permission — the cell is
            // not Visible, so Combat must not address the pinged object. The
            // signature struct carries no EntityId by design.
            TeamView view = host.Fog.GetTeamView(0);
            Assert.That(view.IsVisible(24, 10), Is.False, "a pinged cell stays non-targetable");
            Assert.That(view.IsVisible(13, 10), Is.True, "the in-sight enemy is a target instead");
        }

        [Test]
        public void RadarSignature_WithoutCompletedRadar_NoCoverage()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 10, 10, sightRadius: 5); // a plain unit radiates nothing since 16.5
            SpawnAt(host, 1, 16, 10, sightRadius: 5); // hidden (6 > 5) — the old every-unit x2 rule WOULD have pinged this

            host.Step(2);

            var pings = new List<RadarSignature>();
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(0), "no finished Radar building, no coverage at all");
        }

        [Test]
        public void RadarSignatures_StopAtPowerDeficit_AndResumeWhenBalanceRecovers()
        {
            // 16.6 (C4, D-030): at a power deficit the radar is the FIRST
            // system to fall — no coverage, no pings. The deficit here is set
            // directly on the balance (the rig's economy never recomputes).
            var host = TestHost.Create(Seed);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 10, 9, 14).IsValid, Is.True);
            SpawnAt(host, 1, 24, 10, sightRadius: 5); // radar-covered, hidden
            host.Step(2);

            var pings = new List<RadarSignature>();
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(1), "coverage with a finished Radar and a balanced grid");

            host.Economy.GetPlayerEconomy(0).PowerRequired = 1; // deficit: 1 required > 0 provided
            pings.Clear();
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(0), "LOW POWER takes the radar offline");

            host.Economy.GetPlayerEconomy(0).PowerRequired = 0;
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(1), "the radar comes back with the balance");
        }

        [Test]
        public void HiddenWorldMetamorphic_HiddenEnemyVariation_LeavesTeamZeroViewIdentical()
        {
            // Testing.md section 5: two worlds differ ONLY in enemy state that
            // is hidden from team 0 (outside sight AND radar coverage). Team
            // 0's committed mask, its visible-entity set and its radar pings
            // must be identical; the worlds themselves must really differ.
            const int cells = 64 * 64;

            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);
            SpawnAt(hostA, 0, 10, 10, sightRadius: 5);
            SpawnAt(hostB, 0, 10, 10, sightRadius: 5);
            // 16.5: both hosts get an identical completed Radar, so the ping
            // comparison exercises the radar path rather than two empty lists.
            Assert.That(hostA.Construction.PlaceCompletedBuilding(0, 10, 9, 14).IsValid, Is.True);
            Assert.That(hostB.Construction.PlaceCompletedBuilding(0, 10, 9, 14).IsValid, Is.True);
            EntityId hiddenA = SpawnAt(hostA, 1, 50, 50, sightRadius: 5);
            EntityId hiddenB = SpawnAt(hostB, 1, 55, 52, sightRadius: 5); // hidden variation
            Assert.That(hiddenA, Is.EqualTo(hiddenB), "same spawn sequence must yield the same id");
            // Identical radar-covered enemy in BOTH hosts (inside coverage
            // 20, outside both sight radii 10 and 5): one ping each.
            SpawnAt(hostA, 1, 24, 10, sightRadius: 5);
            SpawnAt(hostB, 1, 24, 10, sightRadius: 5);

            for (int i = 0; i < 10; i++)
            {
                hostA.Step();
                hostB.Step();
            }

            // The variation is real: the full authoritative state differs.
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.Not.EqualTo(hostA.Kernel.CalculateStateHash()),
                "the enemy position variation must exist in the authoritative state");

            // Team 0's committed mask is bit-identical.
            Assert.That(TeamMask(hostB, 0, cells), Is.EqualTo(TeamMask(hostA, 0, cells)),
                "hidden enemy state must not change team 0's committed mask");

            // Team 0's combat-relevant view (targetable entities) is identical.
            var visibleA = new List<EntityId>();
            var visibleB = new List<EntityId>();
            hostA.Fog.GetVisibleEntities(0, visibleA);
            hostB.Fog.GetVisibleEntities(0, visibleB);
            Assert.That(visibleB, Is.EqualTo(visibleA), "hidden enemy state must not change team 0's targets");

            // Team 0's radar picture leaks nothing either.
            var pingsA = new List<RadarSignature>();
            var pingsB = new List<RadarSignature>();
            hostA.Fog.GetRadarSignatures(0, pingsA);
            hostB.Fog.GetRadarSignatures(0, pingsB);
            Assert.That(pingsB.Count, Is.EqualTo(pingsA.Count));
        }

        [Test]
        public void HiddenWorldMetamorphic_HiddenMovementBeforeCommit_DoesNotLeakEarly()
        {
            // FogOfWar.md section 6.3: a change that becomes visible may only
            // take effect at the committed recompute tick. Team 0's observer
            // moves so that a static enemy enters its sight circle on an odd
            // tick: until the next even tick commits, the enemy's cell stays
            // Unexplored and the enemy stays untargetable — no consumer may
            // act on a freshly computed (provisional) sight.
            var host = TestHost.Create(Seed);
            EntityId observer = SpawnAt(host, 0, 10, 10, sightRadius: 6);
            EntityId enemy = SpawnAt(host, 1, 17, 10, sightRadius: 5); // 7 cells out: hidden
            TeamView view = host.Fog.GetTeamView(0);
            var visible = new List<EntityId>();

            host.Step(2); // commit with the enemy hidden
            Assert.That(view.GetCellState(17, 10), Is.EqualTo(VisionState.Unexplored));
            host.Fog.GetVisibleEntities(0, visible);
            Assert.That(visible.Contains(enemy), Is.False);

            Teleport(host, observer, 13, 10); // enemy now 4 cells inside the circle
            host.Step(); // odd tick: no recompute, the committed mask governs
            Assert.That(view.GetCellState(17, 10), Is.EqualTo(VisionState.Unexplored),
                "no fresh sight before the commit tick");
            visible.Clear();
            host.Fog.GetVisibleEntities(0, visible);
            Assert.That(visible.Contains(enemy), Is.False, "no targeting before the commit tick");

            host.Step(); // even tick: the change takes effect exactly at the commit
            Assert.That(view.IsVisible(17, 10), Is.True);
            visible.Clear();
            host.Fog.GetVisibleEntities(0, visible);
            Assert.That(visible.Contains(enemy), Is.True);
        }

        [Test]
        public void SingleCellChange_ChangesFogBlockAndStateHash()
        {
            var host = TestHost.Create(Seed);
            EntityId unit = SpawnAt(host, 0, 10, 10, sightRadius: 5);
            host.Step(2);

            byte[] maskBefore = TeamMask(host, 0, 64 * 64);
            ulong hashBefore = host.Kernel.CalculateStateHash();

            Teleport(host, unit, 11, 10); // shifts the circle by one cell
            host.Step(2);

            Assert.That(TeamMask(host, 0, 64 * 64), Is.Not.EqualTo(maskBefore),
                "a moved sight circle must change the committed mask bytes");
            Assert.That(host.Kernel.CalculateStateHash(), Is.Not.EqualTo(hashBefore),
                "a cell change must move the canonical state hash");
        }

        [Test]
        public void Snapshot_Roundtrip_ContinuesDeterministicallyAcrossRecomputes()
        {
            var hostA = TestHost.Create(Seed);
            EntityId a1 = SpawnAt(hostA, 0, 10, 10, sightRadius: 6);
            SpawnAt(hostA, 1, 40, 40, sightRadius: 6);
            hostA.Entities.GetUnitRef(a1).SetTarget(new GridPos2D(30, 30));

            hostA.Step(9); // stop on an odd tick: the next recompute is pending
            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            var hostB = TestHost.Create(Seed);
            Assert.That(hostB.Kernel.TryRestoreSnapshot(snapshotBytes), Is.True);
            Assert.That(hostB.Fog.HasCommittedView, Is.True);
            Assert.That(hostB.Fog.LastRecomputeTick, Is.EqualTo(hostA.Fog.LastRecomputeTick));

            // Roundtrip: restore -> serialize reproduces the exact bytes.
            Assert.That(hostB.Kernel.SaveSnapshot(), Is.EqualTo(snapshotBytes),
                "snapshot roundtrip must be byte-identical");

            // Continuation: 100 ticks across many recomputes, identical hashes.
            for (int tick = 0; tick < 100; tick++)
            {
                hostA.Step();
                hostB.Step();
                Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"continuation diverged at tick {tick + 1}");
            }
        }

        [Test]
        public void TwoKernels_MovingUnits_200Ticks_ProduceIdenticalHashes()
        {
            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);

            for (int i = 0; i < 8; i++)
            {
                byte team = (byte)(i % 2);
                EntityId a = SpawnAt(hostA, team, 8 + i * 4, 8 + i * 4, sightRadius: 7);
                EntityId b = SpawnAt(hostB, team, 8 + i * 4, 8 + i * 4, sightRadius: 7);
                var target = new GridPos2D((ushort)(50 - i * 3), (ushort)(50 - i * 3));
                hostA.Entities.GetUnitRef(a).SetTarget(target);
                hostB.Entities.GetUnitRef(b).SetTarget(target);
            }

            for (int tick = 0; tick < 200; tick++)
            {
                hostA.Step();
                hostB.Step();
                Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at tick {tick + 1}");
            }
        }

        [Test]
        public void FogBlock_RejectsMalformedContent_WithoutMutatingMasks()
        {
            var host = TestHost.Create(Seed);
            SpawnAt(host, 0, 10, 10, sightRadius: 5);
            host.Step(2);
            byte[] valid = FogBlock(host);

            // Truncated.
            var truncated = new byte[valid.Length - 1];
            System.Array.Copy(valid, truncated, truncated.Length);
            Assert.That(host.Fog.TryValidateState(truncated), Is.False);
            Assert.That(host.Fog.TryRestoreState(truncated), Is.False);

            // Illegal cell value (only 0..2 exist).
            var illegalCell = (byte[])valid.Clone();
            illegalCell[FogBlockHeaderBytes] = 3;
            Assert.That(host.Fog.TryValidateState(illegalCell), Is.False);
            Assert.That(host.Fog.TryRestoreState(illegalCell), Is.False);

            // Foreign dimensions.
            var foreignDims = (byte[])valid.Clone();
            foreignDims[1] ^= 0xFF; // width low byte
            Assert.That(host.Fog.TryValidateState(foreignDims), Is.False);

            // The rejected attempts left the committed masks untouched.
            Assert.That(FogBlock(host), Is.EqualTo(valid));
        }
    }
}
