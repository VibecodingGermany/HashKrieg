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
    /// <item>the seven G1 systems are registered in one canonical tick order
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
        /// the FoW recompute, then combat. Runtime type full names, so a
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
        };

        /// <summary>Canonical match configuration (DeterminismOptions defaults / MS-1 manifest capacity).</summary>
        private const ulong CanonicalSeed = 0xDE7E000000010271UL;
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int EntityCapacity = 1024;
        private const long FieldReserveAE = 2000000L;
        private const ushort DefHQ = 1;
        private const ushort DefRefinery = 3;

        /// <summary>Harvester move speed, Q16.16 raw (2.5 cells/tick).</summary>
        private const int HarvesterSpeedRaw = 163840;

        private sealed class ReferenceHost
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public CommandIngress Ingress;
            public EntityId HarvesterA;
            public EntityId HarvesterB;
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
            var economy = new EconomySystem(entities);
            var construction = new ConstructionSystem(entities, economy);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, MapWidth, MapHeight);
            var combat = new Nova.Simulation.Combat.CombatSystem(entities, fogOfWar);

            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(fogOfWar);
            kernel.RegisterSystem(combat);

            var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);
            kernel.BindCommands(
                new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

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
            public ushort FieldId;
            public int FieldX, FieldY;
            public int HqOriginX, HqOriginY;
            public int RefineryOriginX, RefineryOriginY;
            public int HarvesterAX, HarvesterAY, HarvesterBX, HarvesterBY;
            public int BuilderX, BuilderY;
            public int[] SquadX, SquadY;
        }

        private static readonly SlotLayout Slot0Layout = new SlotLayout
        {
            FieldId = 1, FieldX = 7, FieldY = 7,
            HqOriginX = 4, HqOriginY = 4,
            RefineryOriginX = 8, RefineryOriginY = 4,
            HarvesterAX = 7, HarvesterAY = 6, HarvesterBX = 7, HarvesterBY = 7,
            BuilderX = 13, BuilderY = 7,
            SquadX = new[] { 56, 57, 56, 57 },
            SquadY = new[] { 62, 62, 63, 63 },
        };

        private static readonly SlotLayout Slot1Layout = new SlotLayout
        {
            FieldId = 2, FieldX = 119, FieldY = 119,
            HqOriginX = 120, HqOriginY = 120,
            RefineryOriginX = 116, RefineryOriginY = 120,
            HarvesterAX = 119, HarvesterAY = 120, HarvesterBX = 119, HarvesterBY = 119,
            BuilderX = 113, BuilderY = 119,
            SquadX = new[] { 65, 66, 65, 66 },
            SquadY = new[] { 62, 62, 63, 63 },
        };

        /// <summary>
        /// Byte-exact mirror of Determinism10000Scenario.SetupMatch. Spawn ORDER
        /// is load-bearing: EntityManager hands out ids from a deterministic free
        /// list, so any reordering shifts every id and therefore every hash.
        /// Units spawn through SpawnUnit's defaults (maxHealth 100 for all),
        /// exactly like the scenario — NOT through SimDefinitions.
        /// </summary>
        private static void ApplyOpeningPosition(ReferenceHost host)
        {
            for (byte slot = 0; slot < 2; slot++)
            {
                SlotLayout c = slot == 0 ? Slot0Layout : Slot1Layout;

                Assert.That(host.Economy.TryAddField(c.FieldId, new GridPos2D(c.FieldX, c.FieldY), FieldReserveAE),
                    Is.True, "reference field registration");
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, DefHQ, c.HqOriginX, c.HqOriginY).IsValid,
                    Is.True, "reference HQ placement");
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, DefRefinery, c.RefineryOriginX, c.RefineryOriginY).IsValid,
                    Is.True, "reference Refinery placement");

                EntityId harvesterA = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.HarvesterAX), SimFixed.FromInt(c.HarvesterAY)),
                    SimFixed.FromRaw(HarvesterSpeedRaw), role: UnitRole.Harvester);
                EntityId harvesterB = host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.HarvesterBX), SimFixed.FromInt(c.HarvesterBY)),
                    SimFixed.FromRaw(HarvesterSpeedRaw), role: UnitRole.Harvester);
                if (slot == 0)
                {
                    host.HarvesterA = harvesterA;
                    host.HarvesterB = harvesterB;
                }

                host.Entities.SpawnUnit(
                    slot, new Transform2D(SimFixed.FromInt(c.BuilderX), SimFixed.FromInt(c.BuilderY)),
                    SimFixed.FromInt(3), role: UnitRole.Builder);

                for (int i = 0; i < 4; i++)
                {
                    host.Entities.SpawnUnit(
                        slot, new Transform2D(SimFixed.FromInt(c.SquadX[i]), SimFixed.FromInt(c.SquadY[i])),
                        SimFixed.FromInt(4), role: UnitRole.BasicInfantry);
                }
            }
        }

        /// <summary>
        /// Mirror of MatchBootstrap's opening command pair: one Harvest intent
        /// per human harvester, in spawn order, through the ingress. The kernel
        /// block hashes the ingress dedupe/sequence state INCLUDING pending
        /// records (SimulationKernel.BuildKernelBlock), so these two intents are
        /// part of the tick-0 state hash and the EditMode lane has to compare
        /// against a reference that carries them.
        /// </summary>
        private static void SubmitOpeningHarvestOrders(ReferenceHost host)
        {
            SubmitHarvest(host, host.HarvesterA, Slot0Layout.FieldId);
            SubmitHarvest(host, host.HarvesterB, Slot0Layout.FieldId);
        }

        private static void SubmitHarvest(ReferenceHost host, EntityId harvester, ushort fieldId)
        {
            uint raw = UnitCommandStateView.ToRawEntityId(harvester);
            Assert.That(raw, Is.Not.Zero, "reference harvester handle must pack to a valid raw id");

            CommandIngressResult result = host.Ingress.TrySubmitIntent(
                CommandIntent.Create(new HarvestPayload(new[] { raw }, fieldId)),
                out CommandRejectReason reason);
            Assert.That(result, Is.EqualTo(CommandIngressResult.Accepted),
                $"reference opening harvest order rejected: {result} ({reason})");
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
        public void Determinism10000Scenario_RegistersTheSevenSystemsInCanonicalOrder()
        {
            object host = InvokeScenarioBuildHost(CanonicalSeed);

            Assert.That(SystemTypeNames(KernelOf(host)), Is.EqualTo(CanonicalTickOrder),
                "the headless harness must register exactly the canonical G1 tick order; " +
                "the EditMode lane pins MatchRunner against the same list.");
        }

        [Test]
        public void ReferenceHost_RegistersTheSevenSystemsInCanonicalOrder()
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
        public void OpeningHarvestOrders_AreTheOnlyDeltaTheEditModeLaneAddsOnTop()
        {
            // The EditMode lane compares MatchBootstrap against
            // ApplyOpeningPosition + SubmitOpeningHarvestOrders. This pins that
            // the second stage is (a) accepted by the real ingress and (b)
            // actually visible in the hash — otherwise that comparison would
            // silently be a weaker test than it looks.
            ReferenceHost bare = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(bare);
            ulong bareHash = bare.Kernel.CalculateStateHash();

            ReferenceHost withOrders = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(withOrders);
            SubmitOpeningHarvestOrders(withOrders);

            Assert.That(withOrders.Ingress.PendingCount, Is.EqualTo(2),
                "both opening harvest intents are pending for tick 1");
            Assert.That(withOrders.Kernel.CalculateStateHash(), Is.Not.EqualTo(bareHash),
                "pending ingress records are part of the kernel block, so they must move the hash");
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
