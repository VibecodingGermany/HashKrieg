using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// The executable half of the sprint-09 section-2.1 barracks diagnosis
    /// (the owner report: "the production bar runs but no unit appears"). The
    /// isolated spawn path is already pinned
    /// (<see cref="ProductionSystemTests"/>), so this suite reproduces the
    /// defect report under the REAL match configuration instead: the full
    /// canonical host (economy, construction, production, pathfinding,
    /// movement, FoW, combat, victory in the G1 order — the exact systems
    /// MatchRunner registers, minus only the AI sidecar), the canonical
    /// 128x128 map with the manifest's 1024-entity store, factions bound, the
    /// D-077 opening position, and the infantry queue order travelling the
    /// REAL sealed command path (ingress -&gt; sealed batch -&gt; kernel
    /// intake -&gt; executor), stepped by the same loop MatchRunner runs per
    /// fixed tick.
    /// <para>
    /// VERDICT THE TEST DELIVERS: if the entity count rises here — with every
    /// real-match factor present that the isolated fixture lacks (fog
    /// recompute, combat, the 1024 store, the command delay) — then the
    /// simulation half of the diagnosis is CLOSED and the defect lives in the
    /// presentation layer (the PlayMode lane localizes it:
    /// BarracksSpawnDiagnosisTests). Both documented silent pause paths of
    /// ProductionSystem.ExecuteTick (entity store full, no free spawn cell in
    /// eight rings) stay pinned by their own suites — this host runs nowhere
    /// near either capacity, exactly like the reported session.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class BarracksSpawnMatchConfigTests
    {
        private const ulong CanonicalSeed = 0xDE7E000000010271UL;
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int EntityCapacity = 1024;
        private const long FieldReserveAE = 2000000L;

        private const ushort DefHqAlliance = 3;
        private const ushort DefBarracksAlliance = 7;
        private const ushort DefInfantryAlliance = 12;

        /// <summary>The full canonical host with the references the assertions need kept on the fixture.</summary>
        private sealed class MatchHost
        {
            public SimulationKernel Kernel;
            public MatchSession Session;
            public CommandIngress Ingress;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public ProductionSystem Production;
            public FogOfWarSystem FogOfWar;

            /// <summary>One lockstep iteration, mirroring MatchRunner.StepFixedTick (AI-less: no peer clock).</summary>
            public void Step()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Kernel.SubmitBatch(batch);
                }
                Kernel.StepTick();
                Session.AdvanceTick();
            }

            public int CountRole(byte slot, UnitRole role)
            {
                int count = 0;
                UnitState[] units = Entities.RawUnits;
                for (int i = 0; i < Entities.Capacity; i++)
                {
                    ref readonly UnitState u = ref units[i];
                    if (u.IsActive && u.PlayerId == slot && u.Role == role) count++;
                }
                return count;
            }
        }

        private static MatchHost BuildMatchHost()
        {
            var kernel = new SimulationKernel(new SimRandom(CanonicalSeed));

            var entities = new EntityManager(EntityCapacity);
            var pathfinding = new PathfindingSystem(MapWidth, MapHeight);
            var movement = new MovementSystem(entities, pathfinding);
            var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
            var construction = new ConstructionSystem(entities, economy);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, MapWidth, MapHeight);
            var combat = new Nova.Simulation.Combat.CombatSystem(entities, fogOfWar, economy);
            var victory = new Nova.Simulation.Victory.VictorySystem(entities, construction);

            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(fogOfWar);
            kernel.RegisterSystem(combat);
            kernel.RegisterSystem(victory);

            var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            kernel.BindCommands(
                new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

            economy.SetSlotFaction(0, FactionId.Alliance);
            economy.SetSlotFaction(1, FactionId.Legion);
            kernel.Start();

            // The D-077 opening position for slot 0 (MatchBootstrap's local
            // layout): field 1 at (7,7), the completed Alliance HQ at origin
            // (4,4), one Builder at (13,7).
            Assert.That(economy.TryAddField(1, new GridPos2D(7, 7), FieldReserveAE), Is.True);
            Assert.That(construction.PlaceCompletedBuilding(0, DefHqAlliance, 4, 4).IsValid, Is.True);

            return new MatchHost
            {
                Kernel = kernel,
                Session = session,
                Ingress = ingress,
                Entities = entities,
                Economy = economy,
                Construction = construction,
                Production = production,
                FogOfWar = fogOfWar,
            };
        }

        /// <summary>
        /// The report's exact situation: a completed Alliance barracks, one
        /// infantry queued through the sealed command path. The bar's data
        /// source must show the progressing entry (the owner's "bar runs"),
        /// and after BuildTicks the entity must stand at the barracks'
        /// footprint ring with a standing order to the default rally cell
        /// (16.2, #46: spawns at the building, walks to the rally point) —
        /// if it does, the simulation side of the defect is closed.
        /// </summary>
        [Test]
        public void MatchConfig_BarracksQueueViaCommandPath_SpawnsInfantryAtDefaultRally()
        {
            MatchHost host = BuildMatchHost();

            // A completed barracks, placed programmatically like the sim
            // fixtures do (the construction walk itself is Sprint-10 proven):
            // origin (10,10) -> centre (11,11) -> default rally cell (13,11).
            EntityId barracks = host.Construction.PlaceCompletedBuilding(0, DefBarracksAlliance, 10, 10);
            Assert.That(barracks.IsValid, Is.True);
            uint rawBarracks = UnitCommandStateView.ToRawEntityId(barracks);

            Assert.That(
                host.Ingress.TrySubmitIntent(
                    CommandIntent.Create(new QueueUnitPayload(rawBarracks, DefInfantryAlliance, 1)), out _),
                Is.EqualTo(CommandIngressResult.Accepted),
                "the queue intent must enter the sealed stream like the card button's");

            // The one-tick input delay applies the command at T+1; then the
            // bar's data source (TryGetQueueEntry progress) must advance —
            // the exact state the owner watched running.
            host.Step();
            host.Step();
            Assert.That(host.Production.TryGetProducer(rawBarracks, out int entryCount, out _, out _), Is.True);
            Assert.That(entryCount, Is.EqualTo(1));
            Assert.That(host.Production.TryGetQueueEntry(rawBarracks, 0, out _, out _, out int progress), Is.True);
            Assert.That(progress, Is.GreaterThan(0), "the production bar's data source advances");
            Assert.That(host.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(0), "too early: 100 build ticks are 10 s");

            // 100 build ticks at full power (HQ 30 provided, barracks 15
            // required), plus the ticks already spent — 110 is ample.
            for (int i = 0; i < 110; i++)
            {
                host.Step();
            }

            Assert.That(host.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(1),
                "SIM VERDICT: in the real match configuration the infantry MUST spawn — if this fails, " +
                "one of the two silent ProductionSystem pause paths is reachable in an ordinary base");

            // 16.2 (#46): the spawned infantryman stands at the barracks'
            // footprint ring (no longer AT the default rally cell) and walks
            // there under a standing order. The spawn tick plus the few
            // remaining loop ticks carry him at most ~1.5 cells, so the
            // asserts pin the order and the footprint neighbourhood, not an
            // exact en-route position. He is part of the viewer team's
            // committed fog view (the feed UnitViewManager renders — own
            // entities always).
            UnitState[] units = host.Entities.RawUnits;
            EntityId infantry = EntityId.Invalid;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.PlayerId == 0 && u.Role == UnitRole.BasicInfantry)
                {
                    infantry = u.Id;
                    int gx = System.Math.Max(0, SimFixed.WorldToGrid(u.Transform.PositionX));
                    int gy = System.Math.Max(0, SimFixed.WorldToGrid(u.Transform.PositionY));
                    Assert.That(gx == 13 && gy == 11, Is.False,
                        "no longer teleports to the default rally cell (13,11)");
                    Assert.That(System.Math.Max(System.Math.Abs(gx - 11), System.Math.Abs(gy - 11)), Is.LessThanOrEqualTo(3),
                        "spawns at the footprint ring of the (11,11) barracks and walks");
                    Assert.That(u.GoalGridPos.X, Is.EqualTo(13));
                    Assert.That(u.GoalGridPos.Y, Is.EqualTo(11));
                    Assert.That(u.IsMoving, Is.True, "ordered at the default rally cell (13,11)");
                }
            }
            Assert.That(infantry.IsValid, Is.True);

            var visible = new System.Collections.Generic.List<EntityId>();
            host.FogOfWar.GetVisibleEntities(0, visible);
            Assert.That(visible.Contains(infantry), Is.True,
                "the spawned infantry is in team 0's committed view — the presentation feed has it");
        }

        /// <summary>
        /// Companion verdict for the second silent pause path: with the
        /// manifest's 1024-entity store the only way production can hang in an
        /// ordinary match is a fully blocked spawn search — proven here by
        /// walling the entire eight-ring search area around the FOOTPRINT
        /// CENTRE (16.2: the search anchors there, not at the rally cell)
        /// with placements, which must pause the finished unit at the
        /// threshold (progress clamped, nothing spawned, nothing lost).
        /// </summary>
        [Test]
        public void MatchConfig_NoFreeSpawnCell_PausesAtThreshold_Silently()
        {
            MatchHost host = BuildMatchHost();

            // Barracks at origin (21,21) -> centre (22,22); the eight-ring
            // spawn search anchored at the centre covers (14..30, 14..30).
            EntityId barracks = host.Construction.PlaceCompletedBuilding(0, DefBarracksAlliance, 21, 21);
            Assert.That(barracks.IsValid, Is.True);
            uint rawBarracks = UnitCommandStateView.ToRawEntityId(barracks);

            // Wall the ENTIRE search area with exactly tiling 3x3 footprints:
            // origins every three cells covering (12..32, 12..32) — the
            // barracks itself fills the (21,21) slot, so every placement is
            // free and no cell of the search square stays uncovered.
            for (int y = 12; y <= 30; y += 3)
            {
                for (int x = 12; x <= 30; x += 3)
                {
                    if (x == 21 && y == 21) continue; // the barracks slot itself
                    Assert.That(
                        host.Construction.PlaceCompletedBuilding(0, DefHqAlliance, x, y).IsValid, Is.True,
                        $"wall footprint ({x},{y}) must be placeable — the tiling leaves no overlaps");
                }
            }

            Assert.That(
                host.Ingress.TrySubmitIntent(
                    CommandIntent.Create(new QueueUnitPayload(rawBarracks, DefInfantryAlliance, 1)), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            for (int i = 0; i < 130; i++)
            {
                host.Step();
            }

            Assert.That(host.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(0),
                "a fully blocked spawn search pauses production (documented silent pause)");
            Assert.That(host.Production.TryGetQueueEntry(rawBarracks, 0, out _, out ushort remaining, out int progress), Is.True);
            Assert.That(remaining, Is.EqualTo((ushort)1), "the finished unit waits — nothing is lost");
            Assert.That(progress, Is.EqualTo(100 << 16), "progress clamps at the completion threshold — the bar reads full and holds");
        }
    }
}
