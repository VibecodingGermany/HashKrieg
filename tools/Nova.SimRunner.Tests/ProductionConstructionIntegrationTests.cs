using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 production/construction integration suite (.NET lane): the
    /// canonical placement/queue/cancel/rally commands driven through the
    /// sealed command intake on the full kernel wiring (economy, then
    /// construction and production — SimulationCore.md section 2, phases
    /// 2/3 before 4/5 before 6), the MS-1 start-state manifest fixture,
    /// two-kernel determinism over 400 ticks, hash sensitivity, snapshot
    /// continuation and replay playback.
    /// Mirror of the EditMode lane ProductionConstructionIntegrationTests.
    /// </summary>
    [TestFixture]
    public sealed class ProductionConstructionIntegrationTests
    {
        private const ulong Seed = 0x5EED43UL;

        /// <summary>Full canonical host including construction and production.</summary>
        private sealed class ProdHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public ConstructionSystem Construction { get; }
            public ProductionSystem Production { get; }
            public MatchSession Session { get; }
            public CommandIngress Ingress { get; }

            private ProdHost(
                SimulationKernel kernel, EntityManager entities, EconomySystem economy,
                ConstructionSystem construction, ProductionSystem production,
                MatchSession session, CommandIngress ingress)
            {
                Kernel = kernel;
                Entities = entities;
                Economy = economy;
                Construction = construction;
                Production = production;
                Session = session;
                Ingress = ingress;
            }

            public static ProdHost Create(ulong seed, int capacity = 256, long startingCredits = 1000)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(128, 128);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities, startingCredits);
                var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
                var production = new ProductionSystem(entities, economy, construction);
                var fogOfWar = new FogOfWarSystem(entities, construction, teamCount: 2, 128, 128);
                var combat = new CombatSystem(entities, fogOfWar, economy, construction);

                var kernel = new SimulationKernel(new SimRandom(seed));
                // Canonical tick order (SimulationCore.md section 2): economy
                // (phases 2/3), construction and production (phases 4/5)
                // BEFORE pathfinding/movement (phase 6), then FoW, then combat.
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
                return new ProdHost(kernel, entities, economy, construction, production, session, ingress);
            }

            /// <summary>One host lockstep iteration: seal the due batch, submit it, step, advance the session.</summary>
            public CommandBatch StepTick()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.That(Kernel.SubmitBatch(batch), Is.True, "a sealed batch must be accepted");
                }
                Kernel.StepTick();
                Session.AdvanceTick();
                return batch;
            }

            /// <summary>Re-aligns the session tick after a kernel snapshot restore.</summary>
            public void RestoreSessionTick()
            {
                while (Session.CurrentTick < Kernel.CurrentTick.Value)
                {
                    Session.AdvanceTick();
                }
            }

            /// <summary>
            /// A synthetic BASE FIXTURE for the command-flow tests below: a
            /// completed HQ, a completed Refinery (placed completed —
            /// <see cref="ConstructionSystem.PlaceCompletedBuilding"/>
            /// bypasses the power rule its 20 draw would trigger), one
            /// Builder and two Harvesters at the 1.000 AE library default.
            /// This is deliberately NOT the match start state — since D-077
            /// that is HQ + one Builder + 3.000 AE, pinned by
            /// <see cref="StartState_MatchesTheManifestFixture"/> below. The
            /// fixture keeps its Refinery so the power-grid arithmetic of
            /// the command tests (30 provided, 20 required) stays put.
            /// </summary>
            public EntityId SpawnBaseFixture(byte slot, int baseX, int baseY)
            {
                Assert.That(Construction.PlaceCompletedBuilding(slot, 3, baseX, baseY).IsValid, Is.True, "HQ");
                Assert.That(Construction.PlaceCompletedBuilding(slot, 4, baseX + 4, baseY).IsValid, Is.True,
                    "base Refinery — placed completed, bypassing the power rule (no prerequisite since D-077)");
                EntityId builder = Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(baseX + 8), SimFixed.FromInt(baseY + 1)),
                    SimFixed.FromInt(3),
                    role: UnitRole.Builder);
                Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(baseX + 9), SimFixed.FromInt(baseY + 1)),
                    SimFixed.FromRaw(163840),
                    role: UnitRole.Harvester);
                Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(baseX + 10), SimFixed.FromInt(baseY + 1)),
                    SimFixed.FromRaw(163840),
                    role: UnitRole.Harvester);
                return builder;
            }

            public void Submit<TPayload>(in TPayload payload) where TPayload : struct, ICommandPayload
            {
                Assert.That(
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out _),
                    Is.EqualTo(CommandIngressResult.Accepted));
            }

            public int CountRole(byte slot, UnitRole role)
            {
                int count = 0;
                UnitState[] units = Entities.RawUnits;
                for (int i = 0; i < Entities.Capacity; i++)
                {
                    if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role) count++;
                }
                return count;
            }
        }

        [Test]
        public void StartState_MatchesTheManifestFixture()
        {
            // D-077 (quality/content/mvp-v1.json startStatePerPlayer): per
            // slot ONLY a completed HQ, one Builder and 3.000 AE — no
            // pre-placed Refinery, no starting Harvesters.
            var host = ProdHost.Create(Seed, startingCredits: EconomySystem.CanonicalMatchStartingCreditsAE);
            for (byte slot = 0; slot < 2; slot++)
            {
                int baseX = slot == 0 ? 4 : 110;
                int baseY = slot == 0 ? 4 : 110;
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, 3, baseX, baseY).IsValid, Is.True, "HQ");
                host.Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(baseX + 8), SimFixed.FromInt(baseY + 1)),
                    SimFixed.FromInt(3),
                    role: UnitRole.Builder);
            }
            host.StepTick(); // one economy recompute

            for (byte slot = 0; slot < 2; slot++)
            {
                Assert.That(host.Construction.HasFinishedBuilding(slot, UnitRole.HQ), Is.True, "completed HQ");
                Assert.That(host.CountRole(slot, UnitRole.Builder), Is.EqualTo(1));
                Assert.That(host.CountRole(slot, UnitRole.Harvester), Is.EqualTo(0),
                    "no starting Harvesters — the Refinery produces them (D-077)");
                Assert.That(host.Construction.HasFinishedBuilding(slot, UnitRole.Refinery), Is.False,
                    "no pre-placed Refinery (D-077)");
                Assert.That(host.Economy.GetPlayerEconomy(slot).AetheriumCredits, Is.EqualTo(3000L),
                    "the D-077 start balance (EconomySystem.CanonicalMatchStartingCreditsAE)");
                Assert.That(host.Economy.GetPlayerEconomy(slot).PowerProvided, Is.EqualTo(30), "HQ provides 30 (provisional)");
                Assert.That(host.Economy.GetPlayerEconomy(slot).PowerRequired, Is.EqualTo(0), "nothing draws yet");
                Assert.That(host.Economy.GetPlayerEconomy(slot).IsLowPower, Is.False);
            }
        }

        [Test]
        public void PlaceBuilding_ThroughSealedCommands_AppliesAndRejectsDeterministically()
        {
            var host = ProdHost.Create(Seed);
            EntityId builder = host.SpawnBaseFixture(0, 4, 4);
            host.StepTick(); // commit the start balance (30 provided / 20 required)

            // Legal: Storage (def 6, 300 AE) at (20,20) — the start grid
            // (30 provided, 20 required) powers its 5, not the Barracks' 15:
            // the Alliance must build a Power plant before its Barracks
            // (Buildings.md power figures).
            host.Submit(new PlaceBuildingPayload(6, 20, 20));
            // Insufficient funds: HQ (def 3, 2500 AE) at (30,20).
            host.Submit(new PlaceBuildingPayload(3, 30, 20));
            host.StepTick();

            Assert.That(host.Kernel.LastTickResults.Count, Is.EqualTo(2));
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(host.Kernel.LastTickResults[1].Code, Is.EqualTo(CommandResultCode.RejectedInsufficientResources));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(700L),
                "exactly one placement was charged");
            Assert.That(host.Construction.SiteCount, Is.EqualTo(1));

            // Occupied: the HQ footprint at (4,4).
            host.Submit(new PlaceBuildingPayload(6, 4, 4));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget));

            // Prerequisite: the DefensePlatform (def 11, 400 AE) needs a
            // completed Power plant — cheap enough that the generic cost
            // check passes and the domain check decides.
            host.Submit(new PlaceBuildingPayload(11, 30, 30));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(700L),
                "every rejection charged nothing");
            Assert.That(host.Entities.IsValid(builder), Is.True);
        }

        [Test]
        public void QueueUnit_ThroughSealedCommands_T2GatingAndProducerRules()
        {
            var host = ProdHost.Create(Seed);
            host.SpawnBaseFixture(0, 4, 4);
            EntityId barracks = host.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            uint barracksRaw = UnitCommandStateView.ToRawEntityId(barracks);

            // T2 gated: AntiArmorInfantry (def 13) before the ResearchLab.
            host.Submit(new QueueUnitPayload(barracksRaw, 13, 1));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedPrerequisitesNotMet));
            Assert.That(host.Production.TotalQueuedUnits, Is.EqualTo(0));

            // T1 works: BasicInfantry (def 12).
            host.Submit(new QueueUnitPayload(barracksRaw, 12, 2));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(760L));
            Assert.That(host.Production.TotalQueuedUnits, Is.EqualTo(2));

            // After the ResearchLab the same T2 command applies.
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 9, 30, 30).IsValid, Is.True);
            host.Submit(new QueueUnitPayload(barracksRaw, 13, 1));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(510L));
        }

        [Test]
        public void FullLoop_BuildBarracks_QueueInfantry_SpawnsAtFootprint_OrderedToRally()
        {
            var host = ProdHost.Create(Seed);
            EntityId builder = host.SpawnBaseFixture(0, 4, 4);
            // The start grid (30 provided, 20 required) cannot power the
            // Barracks' 15 — the Alliance builds its Power plant first
            // (Buildings.md); placed completed here, the test is about the
            // build/queue/spawn loop, not the power rule.
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True);
            host.StepTick(); // commit the start balance

            // The auto-assigned fixture builder walks nowhere in this test —
            // teleport it into reach of the site (movement is not under test).
            host.Submit(new PlaceBuildingPayload(7, 20, 20));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            host.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));

            for (int i = 0; i < 250; i++) host.StepTick();
            Assert.That(host.CountRole(0, UnitRole.Barracks), Is.EqualTo(1),
                "the Barracks completes (180 full-power ticks, Buildings.md) and becomes a role entity");

            uint barracksRaw = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.Barracks)
                {
                    barracksRaw = UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }

            // Rally point east of the building, then one infantry.
            host.Submit(new SetRallyPointPayload(barracksRaw, SimFixed.FromInt(30), SimFixed.FromInt(30)));
            host.Submit(new QueueUnitPayload(barracksRaw, 12, 1));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[1].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(380L), "1000 - 500 - 120");

            for (int i = 0; i < 100; i++) host.StepTick();
            Assert.That(host.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(1));
            EntityId infantry = FindRole(host, 0, UnitRole.BasicInfantry);
            ref readonly UnitState spawned = ref host.Entities.GetUnitRef(infantry);
            // 16.2 (#46): the infantry spawns at the Barracks' footprint ring
            // (center (21,21); first free ring-2 cell (19,19)) and walks to
            // the rally point. The spawn happened inside this loop's last
            // tick, so movement has carried it at most a fraction of a cell —
            // assert the footprint neighbourhood and the standing order.
            int gx = System.Math.Max(0, SimFixed.WorldToGrid(spawned.Transform.PositionX));
            int gy = System.Math.Max(0, SimFixed.WorldToGrid(spawned.Transform.PositionY));
            Assert.That(System.Math.Max(System.Math.Abs(gx - 21), System.Math.Abs(gy - 21)), Is.LessThanOrEqualTo(2),
                "spawns at the footprint ring, no longer teleports to the rally cell");
            Assert.That(spawned.GoalGridPos.X, Is.EqualTo(30));
            Assert.That(spawned.GoalGridPos.Y, Is.EqualTo(30));
            Assert.That(spawned.IsMoving, Is.True, "ordered at the rally cell (30,30)");
        }

        [Test]
        public void TwoKernels_ScriptedConstructionAndProduction_400Ticks_IdenticalHashes()
        {
            var hostA = ProdHost.Create(Seed);
            var hostB = ProdHost.Create(Seed);
            EntityId builderA = hostA.SpawnBaseFixture(0, 4, 4);
            EntityId builderB = hostB.SpawnBaseFixture(0, 4, 4);
            Assert.That(hostA.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True);
            Assert.That(hostB.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True);
            hostA.StepTick(); // commit the start balance on both kernels
            hostB.StepTick();

            for (int tick = 1; tick <= 400; tick++)
            {
                if (tick == 1)
                {
                    hostA.Submit(new PlaceBuildingPayload(7, 20, 20));
                    hostB.Submit(new PlaceBuildingPayload(7, 20, 20));
                    // Identical direct setup mutation on both hosts: the
                    // builder stands in reach of the site from tick 1 on.
                    hostA.Entities.GetUnitRef(builderA).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
                    hostB.Entities.GetUnitRef(builderB).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
                }
                if (tick == 260)
                {
                    uint rawA = BarracksRaw(hostA);
                    uint rawB = BarracksRaw(hostB);
                    Assert.That(rawA, Is.EqualTo(rawB), "identical entity assignment on both kernels");
                    hostA.Submit(new QueueUnitPayload(rawA, 12, 2));
                    hostB.Submit(new QueueUnitPayload(rawB, 12, 2));
                }
                if (tick == 262)
                {
                    uint rawA = BarracksRaw(hostA);
                    uint rawB = BarracksRaw(hostB);
                    hostA.Submit(new SetRallyPointPayload(rawA, SimFixed.FromInt(30), SimFixed.FromInt(30)));
                    hostB.Submit(new SetRallyPointPayload(rawB, SimFixed.FromInt(30), SimFixed.FromInt(30)));
                }
                if (tick == 380)
                {
                    uint rawA = BarracksRaw(hostA);
                    uint rawB = BarracksRaw(hostB);
                    hostA.Submit(new CancelProductionPayload(rawA, 0));
                    hostB.Submit(new CancelProductionPayload(rawB, 0));
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.That(
                    hostB.Kernel.CalculateStateHash(),
                    Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at tick {tick}");
            }
            Assert.That(hostA.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(hostB.CountRole(0, UnitRole.BasicInfantry)));
        }

        [Test]
        public void StateHash_IsSensitiveToConstructionAndProductionState()
        {
            var hostA = ProdHost.Create(Seed);
            var hostB = ProdHost.Create(Seed);
            hostA.SpawnBaseFixture(0, 4, 4);
            hostB.SpawnBaseFixture(0, 4, 4);
            hostA.StepTick();
            hostB.StepTick();
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()));

            // A new placement moves the hash (block 105 is hash-covered).
            Assert.That(hostB.Construction.PlaceCompletedBuilding(0, 5, 20, 20).IsValid, Is.True);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.Not.EqualTo(hostA.Kernel.CalculateStateHash()));

            hostA.Construction.PlaceCompletedBuilding(0, 5, 20, 20);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()));

            // A queued unit moves the hash (block 106 is hash-covered).
            EntityId barracksA = hostA.Construction.PlaceCompletedBuilding(0, 7, 30, 30);
            EntityId barracksB = hostB.Construction.PlaceCompletedBuilding(0, 7, 30, 30);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()));
            Assert.That(hostB.Production.TryQueueUnit(0, UnitCommandStateView.ToRawEntityId(barracksB), 12, 1), Is.True);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.Not.EqualTo(hostA.Kernel.CalculateStateHash()));
        }

        [Test]
        public void Snapshot_RestoredHost_ContinuesConstructionAndProductionIdentically()
        {
            var hostA = ProdHost.Create(Seed);
            EntityId builder = hostA.SpawnBaseFixture(0, 4, 4);
            Assert.That(hostA.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True,
                "Power first — the start grid cannot power the Barracks (Buildings.md)");
            hostA.StepTick(); // commit the start balance
            hostA.Submit(new PlaceBuildingPayload(7, 20, 20));
            hostA.StepTick();
            hostA.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            for (int i = 0; i < 100; i++) hostA.StepTick(); // site mid-progress

            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            var hostB = ProdHost.Create(Seed);
            Assert.That(hostB.Kernel.TryRestoreSnapshot(snapshotBytes), Is.True);
            hostB.RestoreSessionTick();

            // Roundtrip: restore -> serialize reproduces the exact bytes.
            Assert.That(hostB.Kernel.SaveSnapshot(), Is.EqualTo(snapshotBytes),
                "snapshot roundtrip must be byte-identical");

            // Continuation: the site completes on both hosts, a queue command
            // afterwards exercises the restored state identically.
            for (int tick = 0; tick < 300; tick++)
            {
                if (tick == 160)
                {
                    uint rawA = BarracksRaw(hostA);
                    uint rawB = BarracksRaw(hostB);
                    Assert.That(rawA, Is.Not.EqualTo(0u), "the restored site must complete like the live one");
                    Assert.That(rawB, Is.EqualTo(rawA));
                    hostA.Submit(new QueueUnitPayload(rawA, 12, 1));
                    hostB.Submit(new QueueUnitPayload(rawB, 12, 1));
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.That(
                    hostB.Kernel.CalculateStateHash(),
                    Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at continuation tick {tick + 1}");
            }
            Assert.That(hostB.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(hostA.CountRole(0, UnitRole.BasicInfantry)));
        }

        [Test]
        public void Replay_ConstructionAndProductionIntents_PlaybackReproducesEndHash()
        {
            var host = ProdHost.Create(Seed);
            EntityId builder = host.SpawnBaseFixture(0, 4, 4);
            // Direct setup mutation BEFORE recording: the initial snapshot
            // (and therefore the playback) starts with the builder already
            // in reach of the future site — replay only replays commands.
            host.Entities.GetUnitRef(builder).Transform = new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(20));
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True,
                "Power first — the start grid cannot power the Barracks (Buildings.md)");
            host.StepTick(); // commit the start balance before recording

            var slots = new byte[CommandLimits.ReservedPlayerSlots];
            slots[0] = (byte)PlayerSlotOccupancy.Human;
            slots[1] = (byte)PlayerSlotOccupancy.AI;
            MatchFingerprint fingerprint = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                slots,
                new byte[CommandLimits.ReservedPlayerSlots],
                Seed,
                host.Kernel.CalculateStateHash(),
                host.Session.InputDelayTicks);
            var recorder = new ReplayRecorder(fingerprint, host.Kernel.SaveSnapshot());

            for (int tick = 1; tick <= 300; tick++)
            {
                if (tick == 1)
                {
                    host.Submit(new PlaceBuildingPayload(7, 20, 20));
                }
                if (tick == 260)
                {
                    uint raw = BarracksRaw(host);
                    host.Submit(new QueueUnitPayload(raw, 12, 1));
                }
                if (tick == 262)
                {
                    uint raw = BarracksRaw(host);
                    host.Submit(new SetRallyPointPayload(raw, SimFixed.FromInt(30), SimFixed.FromInt(30)));
                }
                CommandBatch batch = host.StepTick();
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);
            }
            ulong endHash = host.Kernel.CalculateStateHash();
            Assert.That(host.CountRole(0, UnitRole.Barracks), Is.EqualTo(1), "the recorded run built the Barracks");

            byte[] replayBytes = recorder.Finalize(endHash);
            Assert.That(ReplayFile.TryParse(replayBytes, out _, out ReplayReadError readError),
                Is.True, () => $"parse failed: {readError}");

            var playback = ProdHost.Create(Seed);
            Assert.That(
                ReplayPlayer.TryPlay(replayBytes, fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, () => $"playback failed: {error} ({detail})");
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(endHash),
                "playback of the recorded construction/production intents must reproduce the end state hash");
        }

        [Test]
        public void SetRallyPoint_OffMapCommand_IsRejected_ProductionContinuesNormally()
        {
            var host = ProdHost.Create(Seed);
            host.SpawnBaseFixture(0, 4, 4);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 5, 40, 40).IsValid, Is.True,
                "Power first — a low-power grid would double the production time under test");
            host.StepTick(); // commit the start balance
            EntityId barracks = host.Construction.PlaceCompletedBuilding(0, 7, 20, 20);
            uint barracksRaw = UnitCommandStateView.ToRawEntityId(barracks);

            // Off-map rally through the sealed intake: rejected
            // state-dependently, mutates nothing.
            host.Submit(new SetRallyPointPayload(barracksRaw, SimFixed.FromInt(200), SimFixed.FromInt(30)));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget));
            Assert.That(host.Production.TryGetProducer(barracksRaw, out _, out _, out _), Is.False,
                "no producer row was created by the rejected command");

            // Production continues normally: queue applies and the unit
            // spawns at the footprint ring, ordered at the DEFAULT rally
            // (two cells east of the center).
            host.Submit(new QueueUnitPayload(barracksRaw, 12, 1));
            host.StepTick();
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            for (int i = 0; i < 100; i++) host.StepTick();
            Assert.That(host.CountRole(0, UnitRole.BasicInfantry), Is.EqualTo(1));
            EntityId infantry = FindRole(host, 0, UnitRole.BasicInfantry);
            ref readonly UnitState spawned = ref host.Entities.GetUnitRef(infantry);
            // 16.2 (#46): spawn at the footprint ring of center (21,21) —
            // the rejected off-map rally changed nothing, the standing order
            // targets the default rally cell (23,21).
            int gx = System.Math.Max(0, SimFixed.WorldToGrid(spawned.Transform.PositionX));
            int gy = System.Math.Max(0, SimFixed.WorldToGrid(spawned.Transform.PositionY));
            Assert.That(System.Math.Max(System.Math.Abs(gx - 21), System.Math.Abs(gy - 21)), Is.LessThanOrEqualTo(2),
                "spawns at the footprint ring, no longer teleports to the rally cell");
            Assert.That(spawned.GoalGridPos.X, Is.EqualTo(23));
            Assert.That(spawned.GoalGridPos.Y, Is.EqualTo(21));
        }

        private static uint BarracksRaw(ProdHost host)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].Role == UnitRole.Barracks)
                {
                    return UnitCommandStateView.ToRawEntityId(units[i].Id);
                }
            }
            return 0;
        }

        private static EntityId FindRole(ProdHost host, byte slot, UnitRole role)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                if (units[i].IsActive && units[i].PlayerId == slot && units[i].Role == role) return units[i].Id;
            }
            throw new System.InvalidOperationException($"no entity with role {role} found");
        }
    }
}
