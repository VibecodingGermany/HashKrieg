using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// G1 economy integration suite (.NET lane): the canonical harvest cycle
    /// driven through the sealed command intake on the full kernel wiring
    /// (economy registered BEFORE movement — SimulationCore.md section 2,
    /// phases 2/3 before 6), two-kernel determinism, hash sensitivity,
    /// snapshot continuation and replay playback with economy orders, plus
    /// the low-power transition through a combat kill.
    /// Mirror of the EditMode lane EconomyIntegrationTests.
    /// </summary>
    [TestFixture]
    public sealed class EconomyIntegrationTests
    {
        private const ulong Seed = 0x5EED42UL;

        /// <summary>
        /// Full canonical host including the economy and combat: kernel,
        /// entity store, economy (phases 2/3, registered first), pathfinding,
        /// movement, Fog of War, combat and the session/ingress pipeline.
        /// </summary>
        private sealed class EcoHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public EconomySystem Economy { get; }
            public MatchSession Session { get; }
            public CommandIngress Ingress { get; }

            private EcoHost(
                SimulationKernel kernel, EntityManager entities, EconomySystem economy,
                MatchSession session, CommandIngress ingress)
            {
                Kernel = kernel;
                Entities = entities;
                Economy = economy;
                Session = session;
                Ingress = ingress;
            }

            public static EcoHost Create(ulong seed, int capacity = 256, ushort width = 64, ushort height = 64)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(width, height);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities);
                var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, width, height);
                var combat = new CombatSystem(entities, fogOfWar);

                var kernel = new SimulationKernel(new SimRandom(seed));
                // Canonical tick order (SimulationCore.md section 2): economy
                // (phases 2/3) BEFORE pathfinding/movement (phase 6), then the
                // 5 Hz FoW recompute, then combat.
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fogOfWar);
                kernel.RegisterSystem(combat);

                var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
                var ingress = new CommandIngress(session);
                _ = new LocalLoopbackTransport(ingress);
                kernel.BindCommands(new UnitCommandStateView(entities, pathfinding, economy), ingress);

                kernel.Start();
                return new EcoHost(kernel, entities, economy, session, ingress);
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

            /// <summary>Spawns the MS-1-style economy fixture: a field, one harvester on it, an own refinery adjacent.</summary>
            public (uint harvesterRaw, EntityId harvester) SpawnHarvestFixture(byte owner, ushort fieldId, long reserve)
            {
                Assert.That(Economy.TryAddField(fieldId, new GridPos2D(10, 10), reserve), Is.True);
                EntityId harvester = Entities.SpawnUnit(
                    owner,
                    new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)),
                    SimFixed.FromInt(4),
                    role: UnitRole.Harvester);
                Entities.SpawnUnit(
                    owner,
                    new Transform2D(SimFixed.FromInt(11), SimFixed.FromInt(10)),
                    SimFixed.Zero,
                    role: UnitRole.Refinery);
                return (UnitCommandStateView.ToRawEntityId(harvester), harvester);
            }

            public void SubmitHarvest(uint[] rawIds, ushort fieldId)
            {
                var payload = new HarvestPayload(rawIds, fieldId);
                Assert.That(
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out _),
                    Is.EqualTo(CommandIngressResult.Accepted));
            }

            public void SubmitReturnCargo(uint[] rawIds)
            {
                var payload = new ReturnCargoPayload(rawIds);
                Assert.That(
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out _),
                    Is.EqualTo(CommandIngressResult.Accepted));
            }
        }

        [Test]
        public void HarvestThenReturn_ThroughSealedCommands_RaisesCreditsExactly()
        {
            var host = EcoHost.Create(Seed);
            (uint rawHarvester, EntityId harvester) = host.SpawnHarvestFixture(0, 1, 9000);

            host.SubmitHarvest(new[] { rawHarvester }, 1);
            for (int i = 0; i < 10; i++)
            {
                host.StepTick();
            }

            Assert.That(host.Entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(20),
                "the sealed Harvest order gathers 2 AE/tick from its target tick on");
            Assert.That(host.Economy.TryGetField(1, out AetheriumField field), Is.True);
            Assert.That(field.RemainingAE, Is.EqualTo(8980L));

            host.SubmitReturnCargo(new[] { rawHarvester });
            host.StepTick();

            Assert.That(host.Kernel.LastTickResults.Count, Is.EqualTo(1));
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(host.Entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(0));
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1020L),
                "credits rise by exactly the delivered cargo");
        }

        [Test]
        public void TwoKernels_HarvestAndReturnCommands_300Ticks_ProduceIdenticalHashes()
        {
            var hostA = EcoHost.Create(Seed);
            var hostB = EcoHost.Create(Seed);
            (uint rawA, _) = hostA.SpawnHarvestFixture(0, 1, 9000);
            hostB.SpawnHarvestFixture(0, 1, 9000);

            for (int tick = 0; tick < 300; tick++)
            {
                if (tick == 0)
                {
                    hostA.SubmitHarvest(new[] { rawA }, 1);
                    hostB.SubmitHarvest(new[] { rawA }, 1);
                }
                if (tick == 150)
                {
                    hostA.SubmitReturnCargo(new[] { rawA });
                    hostB.SubmitReturnCargo(new[] { rawA });
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.That(
                    hostB.Kernel.CalculateStateHash(),
                    Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at tick {tick + 1}");
            }

            Assert.That(hostA.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1300L),
                "150 harvest ticks at 2 AE/tick = 300 AE delivered once");
        }

        [Test]
        public void StateHash_IsSensitiveToCredits()
        {
            var hostA = EcoHost.Create(Seed);
            var hostB = EcoHost.Create(Seed);
            hostA.StepTick();
            hostB.StepTick();
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()));

            hostB.Economy.GetPlayerEconomy(0).AddCredits(1);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.Not.EqualTo(hostA.Kernel.CalculateStateHash()),
                "one AE of credits must change the canonical state hash (block 104 is hash-covered)");
        }

        [Test]
        public void Snapshot_RestoredHost_ContinuesHarvestIdentically()
        {
            var hostA = EcoHost.Create(Seed);
            (uint rawHarvester, EntityId harvester) = hostA.SpawnHarvestFixture(0, 1, 9000);
            hostA.SubmitHarvest(new[] { rawHarvester }, 1);
            for (int i = 0; i < 20; i++)
            {
                hostA.StepTick();
            }

            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            var hostB = EcoHost.Create(Seed);
            Assert.That(hostB.Kernel.TryRestoreSnapshot(snapshotBytes), Is.True);
            hostB.RestoreSessionTick();

            // Roundtrip: restore -> serialize reproduces the exact bytes.
            Assert.That(hostB.Kernel.SaveSnapshot(), Is.EqualTo(snapshotBytes),
                "snapshot roundtrip must be byte-identical");

            // Continuation: the restored standing harvest order keeps
            // gathering bit-identically; a return command on both hosts
            // exercises the restored ingress state.
            for (int tick = 0; tick < 300; tick++)
            {
                if (tick == 100)
                {
                    hostA.SubmitReturnCargo(new[] { rawHarvester });
                    hostB.SubmitReturnCargo(new[] { rawHarvester });
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.That(
                    hostB.Kernel.CalculateStateHash(),
                    Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at continuation tick {tick + 1}");
            }
            Assert.That(hostB.Entities.GetUnitRef(harvester).CargoAE,
                Is.EqualTo(hostA.Entities.GetUnitRef(harvester).CargoAE));
        }

        [Test]
        public void Replay_HarvestAndReturnIntents_PlaybackReproducesEndHash()
        {
            // Live match with economy orders on the record path.
            var host = EcoHost.Create(Seed);
            (uint rawHarvester, _) = host.SpawnHarvestFixture(0, 1, 9000);

            var slots = new byte[CommandLimits.ReservedPlayerSlots];
            slots[0] = (byte)PlayerSlotOccupancy.Human;
            slots[1] = (byte)PlayerSlotOccupancy.AI;
            MatchFingerprint fingerprint = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                slots,
                Seed,
                host.Kernel.CalculateStateHash(),
                host.Session.InputDelayTicks);
            var recorder = new ReplayRecorder(fingerprint, host.Kernel.SaveSnapshot());

            for (int tick = 1; tick <= 40; tick++)
            {
                if (tick == 1)
                {
                    host.SubmitHarvest(new[] { rawHarvester }, 1);
                }
                if (tick == 25)
                {
                    host.SubmitReturnCargo(new[] { rawHarvester });
                }
                CommandBatch batch = host.StepTick();
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);
            }
            ulong endHash = host.Kernel.CalculateStateHash();
            Assert.That(host.Economy.GetPlayerEconomy(0).AetheriumCredits, Is.EqualTo(1048L),
                "24 harvest ticks at 2 AE/tick = 48 AE delivered at tick 25");

            byte[] replayBytes = recorder.Finalize(endHash);
            Assert.That(ReplayFile.TryParse(replayBytes, out _, out ReplayReadError readError),
                Is.True, () => $"parse failed: {readError}");

            var playback = EcoHost.Create(Seed);
            Assert.That(
                ReplayPlayer.TryPlay(replayBytes, fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, () => $"playback failed: {error} ({detail})");
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(endHash),
                "playback of the recorded harvest/return intents must reproduce the end state hash");
        }

        [Test]
        public void CombatKill_OnPowerPlant_DropsProvidedIntoLowPower()
        {
            var host = EcoHost.Create(Seed);

            // Slot 0 grid: power plant (provides 100) + refinery (requires 20).
            EntityId plant = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(20), SimFixed.FromInt(20)), SimFixed.Zero,
                maxHealth: 100, role: UnitRole.Power);
            host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(23), SimFixed.FromInt(20)), SimFixed.Zero,
                role: UnitRole.Refinery);

            // Slot 1 attacker parked next to the plant; its own sight covers
            // the plant's cell, so the FoW commit grants targeting.
            EntityId attacker = host.Entities.SpawnUnit(
                1, new Transform2D(SimFixed.FromInt(21), SimFixed.FromInt(20)), SimFixed.FromInt(4));
            host.Entities.GetUnitRef(attacker).AttackTarget = plant;

            // Let the FoW commit (5 Hz) and combat engage until the plant dies
            // (100 health, 15 damage per 5-tick cooldown cycle).
            bool died = false;
            for (int i = 0; i < 100 && !died; i++)
            {
                host.StepTick();
                died = !host.Entities.IsValid(plant);
            }
            Assert.That(died, Is.True, "combat must despawn the power plant");

            // Economy runs BEFORE combat inside a tick, so the loss shows in
            // the power balance from the tick after the kill at the latest.
            host.StepTick();
            Assert.That(host.Economy.GetPlayerEconomy(0).PowerProvided, Is.EqualTo(0));
            Assert.That(host.Economy.GetPlayerEconomy(0).PowerRequired, Is.EqualTo(20));
            Assert.That(host.Economy.GetPlayerEconomy(0).IsLowPower, Is.True,
                "losing the only power plant drops the grid into low power");
            Assert.That(host.Economy.GetPlayerEconomy(0).ProductionSpeedMultiplierQ16.RawValue,
                Is.EqualTo(32768), "the low-power factor is exact Q16.16 0.5");
        }

        [Test]
        public void HarvestOrder_ResolvesThroughStopCommand_AndKeepsCargo()
        {
            var host = EcoHost.Create(Seed);
            (uint rawHarvester, EntityId harvester) = host.SpawnHarvestFixture(0, 1, 9000);

            host.SubmitHarvest(new[] { rawHarvester }, 1);
            for (int i = 0; i < 5; i++)
            {
                host.StepTick();
            }
            Assert.That(host.Entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(10));

            var stop = new StopPayload(new[] { rawHarvester });
            Assert.That(
                host.Ingress.TrySubmitIntent(CommandIntent.Create(stop), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            host.StepTick();

            Assert.That(host.Entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)0),
                "Stop clears the standing harvest order");
            Assert.That(host.Entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(10),
                "the gathered cargo stays on the unit");

            host.StepTick();
            Assert.That(host.Entities.GetUnitRef(harvester).CargoAE, Is.EqualTo(10),
                "no gathering after the order was cleared");
        }

        [Test]
        public void Harvest_OnNonHarvester_IsRejectedInvalidTarget_AndAssignsNoOrder()
        {
            var host = EcoHost.Create(Seed);
            Assert.That(host.Economy.TryAddField(1, new GridPos2D(10, 10), 9000), Is.True);
            EntityId soldier = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10), SimFixed.FromInt(10)), SimFixed.FromInt(4));
            uint rawSoldier = UnitCommandStateView.ToRawEntityId(soldier);

            host.SubmitHarvest(new[] { rawSoldier }, 1);
            host.StepTick();

            Assert.That(host.Kernel.LastTickResults.Count, Is.EqualTo(1));
            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "P2-2: a non-harvester actor is rejected state-dependently");
            Assert.That(host.Entities.GetUnitRef(soldier).HarvestFieldId, Is.EqualTo((ushort)0),
                "a rejected Harvest assigns no order");
            Assert.That(host.Entities.GetUnitRef(soldier).CargoAE, Is.EqualTo(0));
        }

        [Test]
        public void Harvest_OnUnknownField_IsRejectedInvalidTarget_AndAssignsNoOrder()
        {
            var host = EcoHost.Create(Seed);
            (uint rawHarvester, EntityId harvester) = host.SpawnHarvestFixture(0, 1, 9000);

            host.SubmitHarvest(new[] { rawHarvester }, 7); // field 7 is not registered
            host.StepTick();

            Assert.That(host.Kernel.LastTickResults[0].Code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "P2-1: an unknown field id is rejected state-dependently");
            Assert.That(host.Entities.GetUnitRef(harvester).HarvestFieldId, Is.EqualTo((ushort)0),
                "a rejected Harvest assigns no order");
        }

        [Test]
        public void Replay_WithHarvestRejections_PlaybackReproducesResultsAndEndHash()
        {
            // The two rejection cases stay in the recorded stream with their
            // deterministic RejectedInvalidTarget results; playback
            // re-executes them to the identical results and end state hash.
            var host = EcoHost.Create(Seed);
            (uint rawHarvester, _) = host.SpawnHarvestFixture(0, 1, 9000);
            EntityId soldier = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(20), SimFixed.FromInt(20)), SimFixed.FromInt(4));
            uint rawSoldier = UnitCommandStateView.ToRawEntityId(soldier);

            var slots = new byte[CommandLimits.ReservedPlayerSlots];
            slots[0] = (byte)PlayerSlotOccupancy.Human;
            slots[1] = (byte)PlayerSlotOccupancy.AI;
            MatchFingerprint fingerprint = MatchFingerprint.CreateCurrent(
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                slots,
                Seed,
                host.Kernel.CalculateStateHash(),
                host.Session.InputDelayTicks);
            var recorder = new ReplayRecorder(fingerprint, host.Kernel.SaveSnapshot());

            for (int tick = 1; tick <= 30; tick++)
            {
                if (tick == 1)
                {
                    host.SubmitHarvest(new[] { rawHarvester }, 7); // unknown field -> rejected
                }
                if (tick == 5)
                {
                    host.SubmitHarvest(new[] { rawSoldier }, 1); // non-harvester -> rejected
                }
                if (tick == 10)
                {
                    host.SubmitHarvest(new[] { rawHarvester }, 1); // valid -> applied
                }
                if (tick == 25)
                {
                    host.SubmitReturnCargo(new[] { rawHarvester });
                }
                CommandBatch batch = host.StepTick();
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);
            }
            ulong endHash = host.Kernel.CalculateStateHash();
            byte[] replayBytes = recorder.Finalize(endHash);

            Assert.That(ReplayFile.TryParse(replayBytes, out ReplayFile replay, out ReplayReadError readError),
                Is.True, () => $"parse failed: {readError}");
            Assert.That(replay.Frames[0].ResultCodes[0], Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the unknown-field rejection stays in the stream");
            Assert.That(replay.Frames[4].ResultCodes[0], Is.EqualTo(CommandResultCode.RejectedInvalidTarget),
                "the non-harvester rejection stays in the stream");
            Assert.That(replay.Frames[9].ResultCodes[0], Is.EqualTo(CommandResultCode.Applied),
                "the valid harvest applies");

            var playback = EcoHost.Create(Seed);
            Assert.That(
                ReplayPlayer.TryPlay(replayBytes, fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, () => $"playback failed: {error} ({detail})");
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(endHash),
                "playback re-executes the rejections to the identical results and end state hash");
        }
    }
}
