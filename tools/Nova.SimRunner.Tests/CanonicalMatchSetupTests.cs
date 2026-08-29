using System;
using System.Collections.Generic;
using System.Reflection;
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
    /// Cross-host comparability suite (.NET lane). Two properties make the
    /// Unity player and this headless harness the SAME match, and until this
    /// slice nothing checked either of them:
    /// <list type="number">
    /// <item>the G1 systems are registered in one canonical tick order
    /// (SimulationCore.md section 2) — a reordering silently changes every
    /// state hash while every individual system test stays green;</item>
    /// <item>the opening position they start from is bit-identical, so the
    /// tick-0 state hash matches.</item>
    /// </list>
    /// <para>
    /// THE TWO LANES MEET IN THE MIDDLE. Neither lane can see both hosts: this
    /// assembly cannot reference Nova.Gameplay (Unity assembly), and the Unity
    /// EditMode lane cannot see tools/Nova.SimRunner. So both lanes assert
    /// against the same hand-mirrored reference — <see cref="CanonicalTickOrder"/>
    /// and <see cref="BuildReferenceHost"/> below — which chains to:
    /// MatchBootstrap == reference (EditMode lane) == Determinism10000Scenario
    /// (this lane). Any edit to the reference must be applied to BOTH copies.
    /// </para>
    /// Mirror of the EditMode lane CanonicalMatchSetupTests.
    /// </summary>
    [TestFixture]
    public sealed class CanonicalMatchSetupTests
    {
        // ----------------------------------------------------------------
        // The canonical reference (hand-mirrored between the two lanes)
        // ----------------------------------------------------------------

        /// <summary>
        /// Canonical G1 tick order: economy (phases 2/3), construction and
        /// production (phases 4/5) BEFORE pathfinding/movement (phase 6), then
        /// the FoW recompute, then combat, then the D-056 victory evaluation
        /// LAST (it must judge post-combat state). Runtime type full names, so a
        /// wrapper subclass (e.g. the perf harness's TimedPathfindingSystem)
        /// is rejected rather than silently accepted.
        /// </summary>
        private static readonly string[] CanonicalTickOrder =
        {
            "Nova.Simulation.Economy.EconomySystem",
            "Nova.Simulation.Construction.ConstructionSystem",
            "Nova.Simulation.Production.ProductionSystem",
            "Nova.Simulation.Pathfinding.PathfindingSystem",
            "Nova.Simulation.Movement.MovementSystem",
            "Nova.Simulation.Vision.FogOfWarSystem",
            "Nova.Simulation.Combat.CombatSystem",
            "Nova.Simulation.Victory.VictorySystem",
        };

        /// <summary>Canonical match configuration (DeterminismOptions defaults / MS-1 manifest capacity).</summary>
        private const ulong CanonicalSeed = 0xDE7E000000010271UL;
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int EntityCapacity = 1024;
        private struct FieldLayout
        {
            public ushort Id;
            public int X, Y;
            public long ReserveAE;
        }

        private static readonly FieldLayout[] FieldLayouts =
        {
            new FieldLayout { Id = 1,  X = 7,   Y = 7,   ReserveAE = 9000L },
            new FieldLayout { Id = 2,  X = 117, Y = 117, ReserveAE = 9000L },
            new FieldLayout { Id = 3,  X = 24,  Y = 40,  ReserveAE = 9000L },
            new FieldLayout { Id = 4,  X = 100, Y = 84,  ReserveAE = 9000L },
            new FieldLayout { Id = 5,  X = 62,  Y = 62,  ReserveAE = 8000L },
            new FieldLayout { Id = 6,  X = 54,  Y = 54,  ReserveAE = 8000L },
            new FieldLayout { Id = 7,  X = 70,  Y = 70,  ReserveAE = 8000L },
            new FieldLayout { Id = 8,  X = 54,  Y = 70,  ReserveAE = 8000L },
            new FieldLayout { Id = 9,  X = 70,  Y = 54,  ReserveAE = 8000L },
            new FieldLayout { Id = 10, X = 44,  Y = 24,  ReserveAE = 9000L },
            new FieldLayout { Id = 11, X = 80,  Y = 100, ReserveAE = 9000L },
            new FieldLayout { Id = 12, X = 44,  Y = 80,  ReserveAE = 12000L },
            new FieldLayout { Id = 13, X = 80,  Y = 44,  ReserveAE = 12000L },
            new FieldLayout { Id = 14, X = 24,  Y = 96,  ReserveAE = 12000L },
            new FieldLayout { Id = 15, X = 100, Y = 28,  ReserveAE = 12000L },
        };
        // Faction-resolved opening placement ids (SimDefinitions id rule):
        // slot 0 Alliance (role value), slot 1 Legion (role value + 17).
        private const ushort DefHQAlliance = 3;
        private const ushort DefHQLegion = 20;
        private const ushort DefRefineryAlliance = 4;
        private const ushort DefRefineryLegion = 21;

        private sealed class ReferenceHost
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public CommandIngress Ingress;
        }

        /// <summary>
        /// Byte-exact mirror of Determinism10000Scenario.BuildHost: identical
        /// construction order, identical registration order, identical session
        /// (slot 0 local, slots {0,1} active, input delay 1), started before the
        /// opening position is applied.
        /// </summary>
        private static ReferenceHost BuildReferenceHost(ulong seed)
        {
            var kernel = new SimulationKernel(new SimRandom(seed));

            var entities = new EntityManager(EntityCapacity);
            var pathfinding = new PathfindingSystem(MapWidth, MapHeight);
            var movement = new MovementSystem(entities, pathfinding);
            // The D-077 start balance: the same constant the scenario's
            // BuildHost and MatchRunner use — all three hosts must hash the
            // identical initial state.
            var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
            var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, construction, economy, teamCount: 2, MapWidth, MapHeight);
            var combat = new Nova.Simulation.Combat.CombatSystem(entities, fogOfWar, economy, construction);
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

            // Mirror of BuildHost's terrain application (21.7, #94): the
            // canonical Glutrinne ring is written into the cost field BEFORE
            // Kernel.Start(), so the reference, the scenario and the Unity
            // bootstrap carry the identical terrain into the tick-0 snapshot.
            // The terrain content pin lives in GlutrinneTerrainTests.
            CanonicalTerrainMirror.Apply(pathfinding.CostField);

            // Mirror of BuildHost's faction assignment (economy block v2):
            // slot 0 Alliance, slot 1 Legion, set BEFORE Kernel.Start() —
            // the SetSlotFaction guard forbids it once the kernel runs.
            economy.SetSlotFaction(0, FactionId.Alliance);
            economy.SetSlotFaction(1, FactionId.Legion);

            kernel.Start();
            return new ReferenceHost
            {
                Kernel = kernel,
                Entities = entities,
                Economy = economy,
                Construction = construction,
                Ingress = ingress,
            };
        }

        /// <summary>Fixed opening layout of one slot, in grid cells.</summary>
        private sealed class SlotLayout
        {
            public int HqOriginX, HqOriginY;
            public int BuilderX, BuilderY;
        }

        private static readonly SlotLayout Slot0Layout = new SlotLayout
        {
            HqOriginX = 4, HqOriginY = 4,
            BuilderX = 13, BuilderY = 7,
        };

        private static readonly SlotLayout Slot1Layout = new SlotLayout
        {
            HqOriginX = 118, HqOriginY = 118,
            BuilderX = 111, BuilderY = 117,
        };

        /// <summary>
        /// Byte-exact mirror of Determinism10000Scenario.SetupMatch (D-077):
        /// fifteen finite Aetherium fields (21.6/21.7, #93/#94) in canonical
        /// id order, then per slot a
        /// completed HQ and ONE Builder — nothing else. Entity spawn order is
        /// load-bearing: EntityManager hands out ids from a deterministic free
        /// list, so any reordering shifts every id and therefore every hash.
        /// Units spawn through SpawnUnit's defaults (maxHealth 100 for all),
        /// exactly like the scenario — NOT through SimDefinitions.
        /// </summary>
        private static void ApplyOpeningPosition(ReferenceHost host)
        {
            // The slot factions are already bound: BuildReferenceHost mirrors
            // BuildHost, which assigns them before Kernel.Start() (the
            // SetSlotFaction guard requires it).
            for (int i = 0; i < FieldLayouts.Length; i++)
            {
                FieldLayout field = FieldLayouts[i];
                Assert.That(host.Economy.TryAddField(
                    field.Id, new GridPos2D(field.X, field.Y), field.ReserveAE),
                    Is.True, "reference field registration");
            }

            for (byte slot = 0; slot < 2; slot++)
            {
                SlotLayout c = slot == 0 ? Slot0Layout : Slot1Layout;
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, slot == 0 ? DefHQAlliance : DefHQLegion, c.HqOriginX, c.HqOriginY).IsValid,
                    Is.True, "reference HQ placement");

                host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.BuilderX), SimFixed.FromInt(c.BuilderY)),
                    SimFixed.FromInt(3), role: UnitRole.Builder);
            }
        }

        // ----------------------------------------------------------------
        // Reflection into the scenario (BuildHost/SetupMatch are private)
        // ----------------------------------------------------------------

        private static object InvokeScenarioBuildHost(ulong seed)
        {
            MethodInfo buildHost = typeof(Determinism10000Scenario).GetMethod(
                "BuildHost", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(buildHost, Is.Not.Null,
                "Determinism10000Scenario.BuildHost was renamed — this guard test must follow it.");
            return buildHost.Invoke(null, new object[] { seed, null });
        }

        private static void InvokeScenarioSetupMatch(object host)
        {
            MethodInfo setupMatch = typeof(Determinism10000Scenario).GetMethod(
                "SetupMatch", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(setupMatch, Is.Not.Null,
                "Determinism10000Scenario.SetupMatch was renamed — this guard test must follow it.");
            setupMatch.Invoke(null, new[] { host });
        }

        private static SimulationKernel KernelOf(object host)
        {
            FieldInfo kernelField = host.GetType().GetField("Kernel");
            Assert.That(kernelField, Is.Not.Null, "Determinism10000Scenario.Host.Kernel was renamed.");
            return (SimulationKernel)kernelField.GetValue(host);
        }

        private static string[] SystemTypeNames(SimulationKernel kernel)
        {
            var names = new List<string>();
            for (int i = 0; i < kernel.Systems.Count; i++)
            {
                names.Add(kernel.Systems[i].GetType().FullName);
            }
            return names.ToArray();
        }

        // ----------------------------------------------------------------
        // (a) SYSTEM ORDER PIN
        // ----------------------------------------------------------------

        [Test]
        public void Determinism10000Scenario_RegistersTheCanonicalSystemsInCanonicalOrder()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);

            Assert.That(SystemTypeNames(KernelOf(host)), Is.EqualTo(CanonicalTickOrder),
                "the headless harness must register exactly the canonical G1 tick order; " +
                "the EditMode lane pins MatchRunner against the same list.");
        }

        [Test]
        public void ReferenceHost_RegistersTheCanonicalSystemsInCanonicalOrder()
        {
            // Guards the mirror itself: if someone edits BuildReferenceHost the
            // hash test below would keep passing against a wrong order.
            ReferenceHost host = BuildReferenceHost(CanonicalSeed);

            Assert.That(SystemTypeNames(host.Kernel), Is.EqualTo(CanonicalTickOrder));
        }

        // ----------------------------------------------------------------
        // (b) INITIAL STATE EQUIVALENCE
        // ----------------------------------------------------------------

        [Test]
        public void ReferenceOpeningPosition_MatchesDeterminism10000ScenarioSetupMatch()
        {
            object scenarioHost = InvokeScenarioBuildHost(CanonicalSeed);
            InvokeScenarioSetupMatch(scenarioHost);
            ulong scenarioHash = KernelOf(scenarioHost).CalculateStateHash();

            ReferenceHost referenceHost = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(referenceHost);
            ulong referenceHash = referenceHost.Kernel.CalculateStateHash();

            Assert.That(referenceHash, Is.EqualTo(scenarioHash),
                $"the mirrored reference opening position drifted from SetupMatch " +
                $"(reference 0x{referenceHash:X16}, scenario 0x{scenarioHash:X16}). " +
                "The EditMode lane asserts MatchBootstrap against this same reference, so a " +
                "drift here means the Unity host and the headless harness are no longer the same match.");
        }

        [Test]
        public void ReferenceOpeningPosition_RegistersFifteenFiniteFields()
        {
            ReferenceHost host = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(host);

            Assert.That(host.Economy.FieldCount, Is.EqualTo(FieldLayouts.Length));
            for (int i = 0; i < FieldLayouts.Length; i++)
            {
                FieldLayout expected = FieldLayouts[i];
                Assert.That(host.Economy.TryGetField(expected.Id, out AetheriumField actual), Is.True);
                Assert.That(actual.GridPos.X, Is.EqualTo(expected.X));
                Assert.That(actual.GridPos.Y, Is.EqualTo(expected.Y));
                Assert.That(actual.RemainingAE, Is.EqualTo(expected.ReserveAE));
            }
        }

        [Test]
        public void CanonicalOpeningGeometry_UsesD107PointAndFootprintMirrors()
        {
            Assert.That(Slot0Layout.HqOriginX + Slot1Layout.HqOriginX, Is.EqualTo(122),
                "3x3 footprint origins mirror as o -> 122-o");
            Assert.That(Slot0Layout.HqOriginY + Slot1Layout.HqOriginY, Is.EqualTo(122));
            Assert.That(Slot0Layout.BuilderX + Slot1Layout.BuilderX, Is.EqualTo(124),
                "point coordinates mirror as p -> 124-p");
            Assert.That(Slot0Layout.BuilderY + Slot1Layout.BuilderY, Is.EqualTo(124));
            // The fifteen-field layout (21.6/21.7, #93/#94): seven mirrored
            // pairs plus the self-mirroring middle field of the centre zone.
            // Every pair sums to 124 per axis.
            AssertMirrorPair(0, 1, "start fields");
            AssertMirrorPair(2, 3, "natural expansions");
            AssertMirrorPair(5, 6, "centre-zone diagonal pair");
            AssertMirrorPair(7, 8, "centre-zone anti-diagonal pair");
            AssertMirrorPair(9, 10, "second expansions");
            AssertMirrorPair(11, 12, "near contested flanks");
            AssertMirrorPair(13, 14, "far contested flanks");
            Assert.That(FieldLayouts[4].X, Is.EqualTo(62));
            Assert.That(FieldLayouts[4].Y, Is.EqualTo(62));
            // Mirrored fields carry mirrored reserves, or the mirror is only
            // geometric and the balance statement is false.
            AssertMirroredReserves(0, 1);
            AssertMirroredReserves(2, 3);
            AssertMirroredReserves(5, 6);
            AssertMirroredReserves(7, 8);
            AssertMirroredReserves(9, 10);
            AssertMirroredReserves(11, 12);
            AssertMirroredReserves(13, 14);
        }

        private static void AssertMirrorPair(int first, int second, string label)
        {
            Assert.That(FieldLayouts[first].X + FieldLayouts[second].X, Is.EqualTo(124),
                $"D-107: {label} must mirror as (x, y) -> (124 - x, 124 - y)");
            Assert.That(FieldLayouts[first].Y + FieldLayouts[second].Y, Is.EqualTo(124),
                $"D-107: {label} must mirror as (x, y) -> (124 - x, 124 - y)");
        }

        private static void AssertMirroredReserves(int first, int second)
        {
            Assert.That(FieldLayouts[first].ReserveAE, Is.EqualTo(FieldLayouts[second].ReserveAE),
                "D-107 symmetry covers the reserve, not only the cell");
        }

        [Test]
        public void CanonicalOpeningGeometry_OffersEqualMirroredRefineryPlacements()
        {
            ReferenceHost host = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(host);
            host.Kernel.StepTick(); // commit the two completed HQ power balances
            int mismatches = 0;
            int allianceApplied = 0;
            int legionApplied = 0;

            for (int y = 0; y <= 122; y++)
            {
                for (int x = 0; x <= 122; x++)
                {
                    CommandResultCode alliance = host.Construction.ValidatePlacement(
                        0, DefRefineryAlliance, x, y);
                    CommandResultCode legion = host.Construction.ValidatePlacement(
                        1, DefRefineryLegion, 122 - x, 122 - y);
                    if (alliance != legion) mismatches++;
                    if (alliance == CommandResultCode.Applied) allianceApplied++;
                    if (legion == CommandResultCode.Applied) legionApplied++;
                }
            }

            Assert.That(mismatches, Is.EqualTo(0),
                "D-107 requires every legal or rejected refinery origin to mirror exactly");
            Assert.That(allianceApplied, Is.EqualTo(45),
                "the corrected Alliance opening has 45 legal follow-up refinery origins");
            Assert.That(legionApplied, Is.EqualTo(45),
                "the corrected Legion opening has the same 45 legal mirrored origins");
        }

        [Test]
        public void ReferenceOpeningPosition_IsSeedSensitive()
        {
            // Sanity: the state hash actually covers the PRNG words, so the
            // equality above is not trivially true for any seed.
            ReferenceHost canonical = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(canonical);

            ReferenceHost other = BuildReferenceHost(CanonicalSeed ^ 1UL);
            ApplyOpeningPosition(other);

            Assert.That(other.Kernel.CalculateStateHash(),
                Is.Not.EqualTo(canonical.Kernel.CalculateStateHash()));
        }
    }
}
