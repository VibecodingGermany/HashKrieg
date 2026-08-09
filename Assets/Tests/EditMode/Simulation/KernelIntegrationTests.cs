using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// G1 kernel integration suite (EditMode lane): the rebuilt canonical
    /// kernel against the audit findings F-001 (accepted commands vanished),
    /// F-005 (state hash mutated the PRNG / used FNV-1a) and F-006 (20 Hz
    /// instead of 10 Hz), plus the snapshot continuation proof of
    /// docs/tech/SimulationCore.md section 7.2.
    /// Mirror of the .NET lane KernelIntegrationTests with Unity Test
    /// Framework asserts.
    /// </summary>
    [TestFixture]
    public class KernelIntegrationTests
    {
        private const ulong Seed = 0x5EED42UL;

        /// <summary>
        /// A complete canonical host: kernel, entity store, pathfinding,
        /// movement and the session/ingress command pipeline — the same
        /// wiring MatchRunner and SimRunner use.
        /// </summary>
        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public PathfindingSystem Pathfinding { get; }
            public MatchSession Session { get; }
            public CommandIngress Ingress { get; }

            private TestHost(
                SimulationKernel kernel, EntityManager entities,
                PathfindingSystem pathfinding, MatchSession session, CommandIngress ingress)
            {
                Kernel = kernel;
                Entities = entities;
                Pathfinding = pathfinding;
                Session = session;
                Ingress = ingress;
            }

            public static TestHost Create(
                ulong seed, int capacity = 256, ushort width = 64, ushort height = 64,
                bool reverseOrder = false)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(width, height);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities);

                var kernel = new SimulationKernel(new SimRandom(seed));
                if (reverseOrder)
                {
                    kernel.RegisterSystem(economy);
                    kernel.RegisterSystem(movement);
                    kernel.RegisterSystem(pathfinding);
                }
                else
                {
                    kernel.RegisterSystem(economy);
                    kernel.RegisterSystem(pathfinding);
                    kernel.RegisterSystem(movement);
                }

                var session = new MatchSession(localSlot: 0, activeSlots: new byte[] { 0, 1 }, inputDelayTicks: 1);
                var ingress = new CommandIngress(session);
                _ = new LocalLoopbackTransport(ingress);
                kernel.BindCommands(new UnitCommandStateView(entities, pathfinding, economy), ingress);

                kernel.Start();
                return new TestHost(kernel, entities, pathfinding, session, ingress);
            }

            /// <summary>One host lockstep iteration: seal the due batch, submit it, step, advance the session.</summary>
            public void StepTick()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.IsTrue(Kernel.SubmitBatch(batch), "a sealed batch must be accepted");
                }
                Kernel.StepTick();
                Session.AdvanceTick();
            }

            /// <summary>Re-aligns the session tick after a kernel snapshot restore.</summary>
            public void RestoreSessionTick()
            {
                while (Session.CurrentTick < Kernel.CurrentTick.Value)
                {
                    Session.AdvanceTick();
                }
            }

            /// <summary>Submits a Move intent and asserts acceptance.</summary>
            public void SubmitMove(uint[] rawIds, int targetX, int targetY)
            {
                var payload = new MovePayload(rawIds, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY));
                Assert.AreEqual(
                    CommandIngressResult.Accepted,
                    Ingress.TrySubmitIntent(CommandIntent.Create(payload), out _));
            }
        }

        [Test]
        public void SealedMoveCommand_ChangesUnitStateAtTargetTick()
        {
            // F-001 regression: a Move command sealed through the ingress and
            // submitted as a batch verifiably changes unit state at its
            // target tick. The prototype kernel accepted commands into a
            // buffer that was never handed to any system.
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            uint rawUnit = UnitCommandStateView.ToRawEntityId(unit);

            // Control host: same seed, same spawn, no command.
            var control = TestHost.Create(Seed);
            control.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));

            ulong hashBefore = host.Kernel.CalculateStateHash();

            host.SubmitMove(new[] { rawUnit }, 30, 30);
            host.StepTick(); // tick 1 = target tick (InputDelayTicks = 1)
            control.StepTick();

            Assert.AreEqual(1, host.Kernel.LastTickResults.Count);
            Assert.AreEqual(CommandResultCode.Applied, host.Kernel.LastTickResults[0].Code);

            Assert.IsTrue(host.Entities.GetUnitRef(unit).IsMoving);
            Assert.AreEqual(30, host.Entities.GetUnitRef(unit).TargetGridPos.X);
            Assert.AreEqual(30, host.Entities.GetUnitRef(unit).TargetGridPos.Y);

            ulong hashAfter = host.Kernel.CalculateStateHash();
            Assert.AreNotEqual(hashBefore, hashAfter, "applying a command must change the state hash");
            Assert.AreNotEqual(control.Kernel.CalculateStateHash(), hashAfter,
                "identical ticks with and without the command must differ");

            SimFixed xBefore = host.Entities.GetUnitRef(unit).Transform.PositionX;
            for (int i = 0; i < 5; i++)
            {
                host.StepTick();
            }
            Assert.IsTrue(host.Entities.GetUnitRef(unit).Transform.PositionX > xBefore,
                "the ordered unit must actually move after the target tick");
        }

        [Test]
        public void SealedStopCommand_ClearsMovementAndAttackTarget_WithoutCombatSystem()
        {
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            EntityId target = host.Entities.SpawnUnit(
                1, new Transform2D(SimFixed.FromFloat(20.5f), SimFixed.FromFloat(20.5f)), SimFixed.FromInt(5));

            ref UnitState state = ref host.Entities.GetUnitRef(unit);
            state.SetTarget(new GridPos2D(30, 30));
            state.AttackTarget = target;

            var stop = new StopPayload(new[] { UnitCommandStateView.ToRawEntityId(unit) });
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                host.Ingress.TrySubmitIntent(CommandIntent.Create(stop), out _));

            host.StepTick();

            Assert.AreEqual(1, host.Kernel.LastTickResults.Count);
            Assert.AreEqual(CommandResultCode.Applied, host.Kernel.LastTickResults[0].Code);
            ref readonly UnitState stopped = ref host.Entities.GetUnitRef(unit);
            Assert.IsFalse(stopped.IsMoving);
            Assert.IsFalse(stopped.TargetGridPos.IsValid);
            Assert.IsFalse(stopped.GoalGridPos.IsValid);
            Assert.AreEqual(EntityId.Invalid, stopped.AttackTarget);
        }

        [Test]
        public void StateHash_ReflectsStateMutation_AndStaysStableOnRepeat()
        {
            // F-005 regression: canonical NOVA_STATE_V1/XXH64 hash over the
            // full authoritative state — every mutation changes it, repeating
            // the hash never does and never consumes PRNG state.
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));

            ulong h0 = host.Kernel.CalculateStateHash();
            Assert.AreEqual(h0, host.Kernel.CalculateStateHash(), "hash repetition must be stable");

            // A movement mutation changes the hash.
            host.Entities.GetUnitRef(unit).SetTarget(new GridPos2D(30, 30));
            host.Pathfinding.RequestFlowField(new GridPos2D(30, 30));
            host.StepTick();
            ulong h1 = host.Kernel.CalculateStateHash();
            Assert.AreNotEqual(h0, h1, "a moved unit must change the hash");
            Assert.AreEqual(h1, host.Kernel.CalculateStateHash());

            // An applied command changes the hash.
            host.SubmitMove(new[] { UnitCommandStateView.ToRawEntityId(unit) }, 40, 40);
            host.StepTick();
            ulong h2 = host.Kernel.CalculateStateHash();
            Assert.AreNotEqual(h1, h2, "an applied command must change the hash");
        }

        [Test]
        public void StateHash_MatchesSnapshotHeaderHash()
        {
            // The live hash is the canonical container state hash of exactly
            // the bytes SaveSnapshot emits — the two can never drift apart.
            var host = TestHost.Create(Seed);
            host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            host.StepTick();

            byte[] snapshot = host.Kernel.SaveSnapshot();
            Assert.IsTrue(SnapshotReader.TryRead(snapshot, out SnapshotFile parsed, out _));
            Assert.AreEqual(host.Kernel.CalculateStateHash(), parsed.StateHash);
        }

        [Test]
        public void TickRate_IsCanonical10Hz_MovementCoversOneSecondInTenTicks()
        {
            // F-006 regression: one canonical tick rate (10 Hz) shared by the
            // clock constant, the host and the movement system.
            Assert.AreEqual(10, SimClock.TicksPerSecond);
            Assert.AreEqual(0.1f, SimClock.TickDeltaSeconds, 1e-7f);

            // A unit with speed 5 units/s covers exactly one second of
            // simulation time (5 units) in 10 ticks. No flow field is
            // requested, so the direct-target fallback drives it straight at
            // the target cell center (pure +x).
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            host.Entities.GetUnitRef(unit).SetTarget(new GridPos2D(20, 10));

            for (int i = 0; i < 10; i++)
            {
                host.StepTick();
            }

            SimFixed moved = host.Entities.GetUnitRef(unit).Transform.PositionX - SimFixed.FromFloat(10.5f);
            Assert.AreEqual(SimFixed.FromInt(5), moved,
                "10 ticks at 10 Hz must equal 1 second of movement, exactly in Q16.16");
        }

        [Test]
        public void DiagonalMovement_IsNormalized_NoDiagonalSpeedup()
        {
            // The combined steering vector is normalized before the speed
            // step is applied: diagonal movement covers the same per-tick
            // distance as straight movement (no sqrt(2) speed-up). No flow
            // field is requested, so the direct-target fallback drives the
            // unit diagonally at the target cell center.
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            host.Entities.GetUnitRef(unit).SetTarget(new GridPos2D(20, 20));

            for (int i = 0; i < 10; i++)
            {
                host.StepTick();
            }

            ref readonly UnitState movedUnit = ref host.Entities.GetUnitRef(unit);
            SimFixed dx = movedUnit.Transform.PositionX - SimFixed.FromFloat(10.5f);
            SimFixed dy = movedUnit.Transform.PositionY - SimFixed.FromFloat(10.5f);
            Assert.AreEqual(dx, dy, "diagonal movement must advance both axes equally");

            SimFixed distance = SimTrig.Sqrt(dx * dx + dy * dy);
            SimFixed tolerance = SimFixed.FromRaw(1000); // ~0.015, integer-normalization rounding
            Assert.IsTrue(distance > SimFixed.FromInt(5) - tolerance);
            Assert.IsTrue(distance < SimFixed.FromInt(5) + tolerance,
                "10 diagonal ticks must cover the same 5 units as straight movement");
        }

        [Test]
        public void Rotation_FollowsMovementDirection_ViaSimTrigAtan2()
        {
            // The heading is SimTrig.Atan2 of the normalized steering vector:
            // a pure diagonal move faces exactly 45 degrees (8192 angle units).
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            host.Entities.GetUnitRef(unit).SetTarget(new GridPos2D(20, 20));

            host.StepTick();

            Assert.AreEqual(8192, host.Entities.GetUnitRef(unit).Transform.Rotation.RawValue);
        }

        [Test]
        public void TwoKernels_RandomMoveCommands_500Ticks_ProduceIdenticalHashes()
        {
            // Long-horizon determinism: two identically seeded hosts receive
            // the same SimRandom-driven Move commands through the sealed
            // intake and must stay bit-identical for 500 ticks.
            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);

            var ids = new uint[16];
            for (int i = 0; i < ids.Length; i++)
            {
                EntityId a = hostA.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f + i), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
                EntityId b = hostB.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f + i), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
                ids[i] = UnitCommandStateView.ToRawEntityId(a);
            }

            var rng = new SimRandom(777UL);
            for (int tick = 0; tick < 500; tick++)
            {
                if (tick % 10 == 0)
                {
                    int targetX = rng.NextInt(5, 60);
                    int targetY = rng.NextInt(5, 60);
                    hostA.SubmitMove(ids, targetX, targetY);
                    hostB.SubmitMove(ids, targetX, targetY);
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.AreEqual(
                    hostA.Kernel.CalculateStateHash(),
                    hostB.Kernel.CalculateStateHash(),
                    $"hash mismatch at tick {tick + 1}");
            }
        }

        [Test]
        public void TwoIdenticalHosts_ProduceIdenticalStateHashes()
        {
            // Determinism check: identical seeds and identical sealed batches
            // yield identical canonical state hashes on every tick.
            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);

            var ids = new uint[8];
            for (int i = 0; i < ids.Length; i++)
            {
                EntityId a = hostA.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f + i), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
                EntityId b = hostB.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f + i), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
                Assert.AreEqual(UnitCommandStateView.ToRawEntityId(a), UnitCommandStateView.ToRawEntityId(b));
                ids[i] = UnitCommandStateView.ToRawEntityId(a);
            }

            hostA.SubmitMove(ids, 40, 40);
            hostB.SubmitMove(ids, 40, 40);

            for (int tick = 0; tick < 200; tick++)
            {
                hostA.StepTick();
                hostB.StepTick();
                Assert.AreEqual(
                    hostA.Kernel.CalculateStateHash(),
                    hostB.Kernel.CalculateStateHash(),
                    $"hash mismatch at tick {tick + 1}");
            }
        }

        [Test]
        public void Snapshot_RestoredHost_ContinuesIdentically_For1000Ticks()
        {
            // SimulationCore.md section 7.2: a fresh and a restored host run
            // at least 1,000 ticks with commands already queued before the
            // snapshot and produce identical state hashes on every tick;
            // serialize -> restore -> serialize is byte-identical (7.1).
            var hostA = TestHost.Create(Seed);
            var ids = new uint[32];
            for (int i = 0; i < ids.Length; i++)
            {
                EntityId id = hostA.Entities.SpawnUnit(
                    0, new Transform2D(SimFixed.FromFloat(10.5f + (i % 8)), SimFixed.FromFloat(10.5f + (i / 8))), SimFixed.FromFloat(4.5f));
                ids[i] = UnitCommandStateView.ToRawEntityId(id);
            }

            // Live command flow before the snapshot.
            hostA.SubmitMove(ids, 40, 40);
            for (int i = 0; i < 10; i++)
            {
                hostA.StepTick();
            }

            // Queue a command that is sealed and pending — but not yet
            // applied — exactly at snapshot time.
            hostA.SubmitMove(ids, 50, 50);
            uint nextTick = hostA.Kernel.CurrentTick.Value + 1;
            CommandBatch pending = hostA.Ingress.SealTickBatch(nextTick);
            Assert.AreEqual(1, pending.Count);
            Assert.IsTrue(hostA.Kernel.SubmitBatch(pending));

            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            // Restore into a fresh, independently constructed host.
            var hostB = TestHost.Create(Seed);
            Assert.IsTrue(hostB.Kernel.TryRestoreSnapshot(snapshotBytes));
            hostB.RestoreSessionTick();
            Assert.AreEqual(hostA.Kernel.CurrentTick, hostB.Kernel.CurrentTick);

            // Roundtrip (7.1): restore -> serialize reproduces the exact bytes.
            byte[] resaved = hostB.Kernel.SaveSnapshot();
            Assert.AreEqual(snapshotBytes, resaved, "snapshot roundtrip must be byte-identical");

            // Continuation (7.2): 1,000 ticks, identical hashes per tick;
            // the command submitted mid-run exercises the restored
            // dedupe/sequence state on both hosts.
            for (int tick = 0; tick < 1000; tick++)
            {
                if (tick == 500)
                {
                    hostA.SubmitMove(ids, 60, 60);
                    hostB.SubmitMove(ids, 60, 60);
                }
                hostA.StepTick();
                hostB.StepTick();
                Assert.AreEqual(
                    hostA.Kernel.CalculateStateHash(),
                    hostB.Kernel.CalculateStateHash(),
                    $"hash mismatch at continuation tick {tick + 1}");
            }
        }

        [Test]
        public void Restore_RejectsTamperedTruncatedAndForeignSnapshots()
        {
            var hostA = TestHost.Create(Seed);
            hostA.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            hostA.StepTick();
            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            var hostB = TestHost.Create(Seed);

            // Truncated container: rejected by the hardened reader.
            var truncated = new byte[snapshotBytes.Length - 1];
            System.Array.Copy(snapshotBytes, truncated, truncated.Length);
            Assert.IsFalse(hostB.Kernel.TryRestoreSnapshot(truncated));

            // Single-bit corruption: container hash verification rejects it.
            var corrupted = (byte[])snapshotBytes.Clone();
            corrupted[corrupted.Length - 1] ^= 0x01;
            Assert.IsFalse(hostB.Kernel.TryRestoreSnapshot(corrupted));

            // Different entity capacity: the entity store block does not fit
            // this host and is rejected.
            var foreign = TestHost.Create(Seed, capacity: 128);
            Assert.IsFalse(foreign.Kernel.TryRestoreSnapshot(snapshotBytes));

            // The rejected attempts left the host able to accept the valid file.
            Assert.IsTrue(hostB.Kernel.TryRestoreSnapshot(snapshotBytes));
        }

        [Test]
        public void SubmitBatch_EnforcesSingleBatchPerTick_AndRequiresBoundPipeline()
        {
            var host = TestHost.Create(Seed);
            EntityId unit = host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            host.SubmitMove(new[] { UnitCommandStateView.ToRawEntityId(unit) }, 30, 30);

            CommandBatch batch = host.Ingress.SealTickBatch(1);
            Assert.IsTrue(host.Kernel.SubmitBatch(batch));
            Assert.IsFalse(host.Kernel.SubmitBatch(batch), "a second batch for the same tick is rejected");

            // Without a bound command pipeline there is no state to apply
            // commands to — a host programming error, not a false return.
            var unbound = new SimulationKernel(new SimRandom(Seed));
            unbound.Start();
            Assert.Throws<System.InvalidOperationException>(() => unbound.SubmitBatch(batch));
        }

        [Test]
        public void FailedRestore_LeavesHostCompletelyUnchanged()
        {
            // Atomic restore (Serialization.md section 5): when any block
            // fails validation, the running host must stay bit-identical —
            // no franken-state from already committed earlier blocks.
            var source = TestHost.Create(Seed);
            source.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
            source.StepTick();
            byte[] snapshotBytes = source.Kernel.SaveSnapshot();

            // Case 1: a semantically invalid block behind a VALID container
            // hash (forged ActiveCount inside the entity store block). This
            // is the case a sequential commit would half-apply.
            Assert.IsTrue(SnapshotReader.TryRead(snapshotBytes, out SnapshotFile parsed, out _));
            var writer = new SnapshotWriter();
            for (int i = 0; i < parsed.Blocks.Count; i++)
            {
                SnapshotBlock block = parsed.Blocks[i];
                byte[] content = block.Content;
                if (block.BlockId == SnapshotBlockIds.EntityStore)
                {
                    content = (byte[])block.Content.Clone();
                    content[5] ^= 0xFF; // ActiveCount LSB: no longer matches the active flags
                }
                writer.AddBlock(block.BlockId, content);
            }
            byte[] forged = writer.ToArray();

            var host = TestHost.Create(Seed);
            host.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(20.5f), SimFixed.FromFloat(20.5f)), SimFixed.FromInt(5));
            host.StepTick();
            ulong hashBefore = host.Kernel.CalculateStateHash();
            byte[] stateBefore = host.Kernel.SaveSnapshot();

            Assert.IsFalse(host.Kernel.TryRestoreSnapshot(forged));
            Assert.AreEqual(hashBefore, host.Kernel.CalculateStateHash(),
                "a failed restore must not touch the state hash");
            Assert.AreEqual(stateBefore, host.Kernel.SaveSnapshot(),
                "a failed restore must leave the full state byte-identical");

            // Case 2: a valid snapshot that does not fit this host's entity
            // capacity (foreign block) is rejected just as atomically.
            var foreign = TestHost.Create(Seed, capacity: 128);
            ulong foreignHashBefore = foreign.Kernel.CalculateStateHash();
            byte[] foreignStateBefore = foreign.Kernel.SaveSnapshot();

            Assert.IsFalse(foreign.Kernel.TryRestoreSnapshot(snapshotBytes));
            Assert.AreEqual(foreignHashBefore, foreign.Kernel.CalculateStateHash());
            Assert.AreEqual(foreignStateBefore, foreign.Kernel.SaveSnapshot());
        }

        [Test]
        public void Restore_IsBlockIdBased_IndependentOfRegistrationOrder()
        {
            // Blocks are matched by their registered BlockId, not by system
            // registration order: a host with reversed registration restores
            // the identical state and continues identically (Pathfinding's
            // tick is currently empty, so the order swap is behaviorally
            // neutral here).
            var source = TestHost.Create(Seed);
            var ids = new uint[4];
            for (int i = 0; i < ids.Length; i++)
            {
                EntityId id = source.Entities.SpawnUnit(0, new Transform2D(SimFixed.FromFloat(10.5f + i), SimFixed.FromFloat(10.5f)), SimFixed.FromInt(5));
                ids[i] = UnitCommandStateView.ToRawEntityId(id);
            }
            source.SubmitMove(ids, 40, 40);
            for (int i = 0; i < 5; i++)
            {
                source.StepTick();
            }
            byte[] snapshotBytes = source.Kernel.SaveSnapshot();

            var reversed = TestHost.Create(Seed, reverseOrder: true);
            Assert.IsTrue(reversed.Kernel.TryRestoreSnapshot(snapshotBytes));
            Assert.AreEqual(source.Kernel.CalculateStateHash(), reversed.Kernel.CalculateStateHash(),
                "restore must map blocks by BlockId, not by registration order");

            for (int i = 0; i < 10; i++)
            {
                source.StepTick();
                reversed.StepTick();
                Assert.AreEqual(source.Kernel.CalculateStateHash(), reversed.Kernel.CalculateStateHash(),
                    $"continuation diverged at tick {i + 1}");
            }
        }
    }
}
