using System.Collections.Generic;
using Nova.Simulation.CommandsV1;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Commands.md section 6, case 8: state-dependent rejection at the target
    /// tick mutates nothing, produces a deterministic CommandResult and the
    /// record stays in the (replayable) stream. Uses a minimal deterministic
    /// test double for ICommandStateView.
    /// </summary>
    [TestFixture]
    public sealed class CommandExecutionTests
    {
        /// <summary>Minimal deterministic fake state; counts mutations to prove absence.</summary>
        private sealed class FakeStateView : ICommandStateView
        {
            private readonly HashSet<uint> _existing = new HashSet<uint>();
            private readonly HashSet<uint> _ownedByLocal = new HashSet<uint>();
            private readonly byte _localSlot;
            public bool Affordable = true;
            public int ApplyCallCount { get; private set; }

            public FakeStateView(byte localSlot)
            {
                _localSlot = localSlot;
            }

            public void AddEntity(uint rawId, bool ownedByLocal)
            {
                _existing.Add(rawId);
                if (ownedByLocal) _ownedByLocal.Add(rawId);
            }

            public bool EntityExists(uint rawEntityId) => _existing.Contains(rawEntityId);

            public bool IsOwnedBy(byte playerSlot, uint rawEntityId)
            {
                return playerSlot == _localSlot && _ownedByLocal.Contains(rawEntityId);
            }

            public bool CanAfford(byte playerSlot, CommandKind kind, ushort definitionId) => Affordable;

            public void Apply(in CommandRecord record)
            {
                ApplyCallCount++;
            }
        }

        private static CommandRecord SealSingleRecord<TPayload>(in TPayload payload, out CommandBatch batch)
            where TPayload : struct, ICommandPayload
        {
            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(
                ingress.TrySubmitIntent(CommandIntent.Create(payload), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            batch = ingress.SealTickBatch(1);
            Assert.That(batch.Count, Is.EqualTo(1));
            return batch.Records[0];
        }

        [Test]
        public void Rejection_NotOwned_MutatesNothing_AndResultIsDeterministic()
        {
            uint actor = CommandTestUtil.EntityId(1, 1);
            CommandRecord record = SealSingleRecord(new StopPayload(new[] { actor }), out CommandBatch batch);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: false);

            CommandResult first = CommandExecutor.Execute(record, state);
            CommandResult second = CommandExecutor.Execute(record, state);

            Assert.That(first.Code, Is.EqualTo(CommandResultCode.RejectedNotOwned));
            Assert.That(first.Applied, Is.False);
            Assert.That(state.ApplyCallCount, Is.EqualTo(0), "rejection must not mutate state");
            Assert.That(second, Is.EqualTo(first), "same record + same state => same result");

            // The rejected record stays in the sealed stream (replay property).
            Assert.That(batch.Count, Is.EqualTo(1));
            Assert.That(batch.Records[0], Is.EqualTo(record));
        }

        [Test]
        public void Rejection_InvalidTarget_MutatesNothing()
        {
            uint actor = CommandTestUtil.EntityId(1, 1);
            uint missingTarget = CommandTestUtil.EntityId(9, 1);
            CommandRecord record = SealSingleRecord(
                new AttackTargetPayload(new[] { actor }, missingTarget), out _);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.That(result.Code, Is.EqualTo(CommandResultCode.RejectedInvalidTarget));
            Assert.That(state.ApplyCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Rejection_InsufficientResources_MutatesNothing()
        {
            uint building = CommandTestUtil.EntityId(4, 1);
            CommandRecord record = SealSingleRecord(new QueueUnitPayload(building, 3, 2), out _);

            var state = new FakeStateView(localSlot: 0) { Affordable = false };
            state.AddEntity(building, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.That(result.Code, Is.EqualTo(CommandResultCode.RejectedInsufficientResources));
            Assert.That(state.ApplyCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Success_AppliesExactlyOnce_AndResultCarriesStreamIdentity()
        {
            uint actor = CommandTestUtil.EntityId(1, 1);
            CommandRecord record = SealSingleRecord(new StopPayload(new[] { actor }), out _);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.That(result.Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(state.ApplyCallCount, Is.EqualTo(1));

            // Result identity = command stream identity + code (replay contract).
            Assert.That(result.PlayerSlot, Is.EqualTo(record.PlayerSlot));
            Assert.That(result.Sequence, Is.EqualTo(record.Sequence));
            Assert.That(result.TargetTick, Is.EqualTo(record.TargetTick));
            Assert.That(result.Kind, Is.EqualTo(record.Kind));
        }

        [Test]
        public void BatchExecution_ReturnsOneDeterministicResultPerRecord()
        {
            uint owned = CommandTestUtil.EntityId(1, 1);
            uint foreign = CommandTestUtil.EntityId(2, 1);

            var ingress = CommandTestUtil.CreateIngress();
            Assert.That(
                ingress.TrySubmitIntent(CommandIntent.Create(new StopPayload(new[] { owned })), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            Assert.That(
                ingress.TrySubmitIntent(CommandIntent.Create(new StopPayload(new[] { foreign })), out _),
                Is.EqualTo(CommandIngressResult.Accepted));
            CommandBatch batch = ingress.SealTickBatch(1);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(owned, ownedByLocal: true);
            state.AddEntity(foreign, ownedByLocal: false);

            CommandResult[] results = CommandExecutor.ExecuteBatch(batch, state);
            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0].Code, Is.EqualTo(CommandResultCode.Applied));
            Assert.That(results[1].Code, Is.EqualTo(CommandResultCode.RejectedNotOwned));
            Assert.That(state.ApplyCallCount, Is.EqualTo(1));
        }
    }
}
