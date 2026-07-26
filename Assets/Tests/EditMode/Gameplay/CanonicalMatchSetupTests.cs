using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Nova.Core;
using Nova.Gameplay.Match;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Vision;
// Unity 6 ships UnityEngine.EntityId, so the bare name is ambiguous here (CS0104).
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Cross-host comparability suite (EditMode lane). Two properties make the
    /// Unity player and the headless harness the SAME match, and until this
    /// slice nothing checked either of them:
    /// <list type="number">
    /// <item>the G1 systems are registered in one canonical tick order
    /// (SimulationCore.md section 2) — a reordering silently changes every
    /// state hash while every individual system test stays green. Note the
    /// snapshot writer sorts blocks by BlockId, so the state hash alone does
    /// NOT detect a reordering; only this test does;</item>
    /// <item>the opening position they start from is bit-identical, so the
    /// tick-0 state hash matches.</item>
    /// </list>
    /// <para>
    /// THE TWO LANES MEET IN THE MIDDLE. Neither lane can see both hosts: this
    /// assembly cannot reference tools/Nova.SimRunner, and the .NET lane cannot
    /// reference Nova.Gameplay. So both lanes assert against the same
    /// hand-mirrored reference — <see cref="CanonicalTickOrder"/> and
    /// <see cref="BuildReferenceHost"/> below — which chains to:
    /// MatchBootstrap == reference (this lane) == Determinism10000Scenario
    /// (.NET lane). Any edit to the reference must be applied to BOTH copies.
    /// </para>
    /// Mirror of the .NET lane CanonicalMatchSetupTests.
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
        private const long FieldReserveAE = 2000000L;
        // Faction-resolved opening placement ids (SimDefinitions id rule):
        // slot 0 Alliance (role value), slot 1 Legion (role value + 17).
        private const ushort DefHQAlliance = 3;
        private const ushort DefHQLegion = 20;
        private const ushort DefRefineryAlliance = 4;
        private const ushort DefRefineryLegion = 21;

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
            // Mirror of SetupMatch's faction assignment (economy block v2):
            // slot 0 Alliance, slot 1 Legion, set BEFORE the opening position
            // so the faction bytes land in the hashed initial state.
            host.Economy.SetSlotFaction(0, FactionId.Alliance);
            host.Economy.SetSlotFaction(1, FactionId.Legion);

            for (byte slot = 0; slot < 2; slot++)
            {
                SlotLayout c = slot == 0 ? Slot0Layout : Slot1Layout;

                Assert.That(host.Economy.TryAddField(c.FieldId, new GridPos2D(c.FieldX, c.FieldY), FieldReserveAE),
                    Is.True, "reference field registration");
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, slot == 0 ? DefHQAlliance : DefHQLegion, c.HqOriginX, c.HqOriginY).IsValid,
                    Is.True, "reference HQ placement");
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, slot == 0 ? DefRefineryAlliance : DefRefineryLegion, c.RefineryOriginX, c.RefineryOriginY).IsValid,
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
        /// part of the tick-0 state hash and this lane has to compare against a
        /// reference that carries them.
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
        // Unity host fixtures
        // ----------------------------------------------------------------

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// A "Match" GameObject exactly as BootstrapSceneGenerator builds it:
        /// MatchRunner first (it is [DisallowMultipleComponent] and
        /// MatchBootstrap requires it), then MatchBootstrap.
        /// </summary>
        private MatchBootstrap NewMatchObject(bool useDefinitionStats)
        {
            var go = new GameObject("TestMatch");
            _spawned.Add(go);
            go.AddComponent<MatchRunner>();
            MatchBootstrap bootstrap = go.AddComponent<MatchBootstrap>();
            bootstrap.AutoStart = false;
            bootstrap.UseDefinitionStats = useDefinitionStats;
            return bootstrap;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Object.DestroyImmediate(_spawned[i]);
            }
            _spawned.Clear();
        }

        // ----------------------------------------------------------------
        // (a) SYSTEM ORDER PIN
        // ----------------------------------------------------------------

        [Test]
        public void MatchRunner_RegistersTheCanonicalSystemsInCanonicalOrder()
        {
            var go = new GameObject("TestOrderRunner");
            _spawned.Add(go);
            MatchRunner runner = go.AddComponent<MatchRunner>();

            runner.InitializeMatch(CanonicalSeed, MapWidth, MapHeight, EntityCapacity);

            Assert.That(SystemTypeNames(runner.Kernel), Is.EqualTo(CanonicalTickOrder),
                "the Unity host must register exactly the canonical G1 tick order; " +
                "the .NET lane pins Determinism10000Scenario.BuildHost against the same list. " +
                "Block ids are sorted before hashing, so a reordering is INVISIBLE to every " +
                "state-hash test — this assertion is the only thing that catches it.");
        }

        [Test]
        public void ReferenceHost_RegistersTheCanonicalSystemsInCanonicalOrder()
        {
            // Guards the mirror itself: if someone edits BuildReferenceHost the
            // hash tests below would keep passing against a wrong order.
            ReferenceHost host = BuildReferenceHost(CanonicalSeed);

            Assert.That(SystemTypeNames(host.Kernel), Is.EqualTo(CanonicalTickOrder));
        }

        // ----------------------------------------------------------------
        // (b) INITIAL STATE EQUIVALENCE
        // ----------------------------------------------------------------

        [Test]
        public void MatchBootstrap_ProducesTheCanonicalOpeningPositionStateHash()
        {
            MatchBootstrap bootstrap = NewMatchObject(useDefinitionStats: false);
            bootstrap.StartGrayboxMatch();

            Assert.That(bootstrap.IsMatchReady, Is.True);
            Assert.That(bootstrap.Seed, Is.EqualTo(CanonicalSeed),
                "the bootstrap must default to the scenario seed — the PRNG words are hashed");

            ReferenceHost reference = BuildReferenceHost(CanonicalSeed);
            ApplyOpeningPosition(reference);
            SubmitOpeningHarvestOrders(reference);

            ulong bootstrapHash = bootstrap.Runner.Kernel.CalculateStateHash();
            ulong referenceHash = reference.Kernel.CalculateStateHash();

            Assert.That(bootstrapHash, Is.EqualTo(referenceHash),
                $"MatchBootstrap (UseDefinitionStats off) drifted from the canonical opening " +
                $"position (bootstrap 0x{bootstrapHash:X16}, reference 0x{referenceHash:X16}). " +
                "The .NET lane asserts the same reference against Determinism10000Scenario.SetupMatch, " +
                "so a drift here means the Unity host and the headless harness are no longer the same match.");
        }

        [Test]
        public void MatchBootstrap_WithDefinitionStats_DiffersOnlyInHarvesterMaxHealth()
        {
            // The one documented, deliberate divergence: SetupMatch spawns every
            // unit with SpawnUnit's maxHealth default of 100, while the graybox
            // bootstrap defaults to the real SimDefinitions stats (Alliance
            // Harvester 800, Vehicles.md "Demeter"). maxHealth is in the hashed
            // entity-store block, so the two
            // modes cannot both be hash-identical to the harness. This test
            // documents the cost of the default so nobody "fixes" the parity
            // test by flipping it silently.
            MatchBootstrap parity = NewMatchObject(useDefinitionStats: false);
            parity.StartGrayboxMatch();

            MatchBootstrap live = NewMatchObject(useDefinitionStats: true);
            live.StartGrayboxMatch();

            Assert.That(parity.Runner.Entities.TryGetUnit(parity.LocalHarvesterA, out UnitState parityHarvester), Is.True);
            Assert.That(live.Runner.Entities.TryGetUnit(live.LocalHarvesterA, out UnitState liveHarvester), Is.True);

            Assert.That(parityHarvester.MaxHealth, Is.EqualTo(100), "SpawnUnit's default");
            Assert.That(liveHarvester.MaxHealth, Is.EqualTo(800), "SimDefinitions Harvester stat");
            Assert.That(parityHarvester.Role, Is.EqualTo(liveHarvester.Role));
            Assert.That(parityHarvester.MoveSpeed, Is.EqualTo(liveHarvester.MoveSpeed),
                "move speed is identical in both paths — only maxHealth diverges");

            Assert.That(live.Runner.Kernel.CalculateStateHash(),
                Is.Not.EqualTo(parity.Runner.Kernel.CalculateStateHash()),
                "definition stats change the entity-store block, hence the parity switch");
        }

        [Test]
        public void MatchBootstrap_PlacesTheCanonicalOpeningGeometry()
        {
            // Readable diagnosis for the hash test above: when the hash breaks,
            // this narrows down whether the geometry moved or something subtler did.
            MatchBootstrap bootstrap = NewMatchObject(useDefinitionStats: false);
            bootstrap.StartGrayboxMatch();

            Assert.That(bootstrap.LocalFieldCell, Is.EqualTo(new Vector2Int(7, 7)));
            Assert.That(bootstrap.EnemyFieldCell, Is.EqualTo(new Vector2Int(119, 119)));
            Assert.That(bootstrap.LocalHqOrigin, Is.EqualTo(new Vector2Int(4, 4)));
            Assert.That(bootstrap.EnemyHqOrigin, Is.EqualTo(new Vector2Int(120, 120)));
            Assert.That(bootstrap.LocalRefineryOrigin, Is.EqualTo(new Vector2Int(8, 4)));
            Assert.That(bootstrap.EnemyRefineryOrigin, Is.EqualTo(new Vector2Int(116, 120)));
            Assert.That(bootstrap.MapSize, Is.EqualTo(new Vector2Int(MapWidth, MapHeight)));

            Assert.That(bootstrap.Runner.Session.LocalSlot, Is.EqualTo((byte)MatchBootstrap.LocalSlot),
                "the human player must own the units it is given orders for, or every " +
                "command comes back RejectedNotOwned");

            Assert.That(bootstrap.Runner.Entities.TryGetUnit(bootstrap.LocalHq, out UnitState hq), Is.True);
            Assert.That(hq.PlayerId, Is.EqualTo((byte)MatchBootstrap.LocalSlot));
            Assert.That(hq.Role, Is.EqualTo(UnitRole.HQ));
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
