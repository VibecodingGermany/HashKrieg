using System.Collections.Generic;
using NUnit.Framework;
using Nova.Simulation.CommandsV1;

namespace Nova.Simulation.Tests
{
    /// <summary>
    /// Commands.md section 6, case 8 (EditMode lane). Mirror of the .NET lane
    /// CommandExecutionTests with Unity Test Framework asserts.
    /// </summary>
    [TestFixture]
    public class CommandV1ExecutionTests
    {
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
            var ingress = CommandV1TestUtil.CreateIngress();
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(CommandIntent.Create(payload), out _));
            batch = ingress.SealTickBatch(1);
            Assert.AreEqual(1, batch.Count);
            return batch.Records[0];
        }

        [Test]
        public void Rejection_NotOwned_MutatesNothing_AndResultIsDeterministic()
        {
            uint actor = CommandV1TestUtil.EntityId(1, 1);
            CommandRecord record = SealSingleRecord(new StopPayload(new[] { actor }), out CommandBatch batch);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: false);

            CommandResult first = CommandExecutor.Execute(record, state);
            CommandResult second = CommandExecutor.Execute(record, state);

            Assert.AreEqual(CommandResultCode.RejectedNotOwned, first.Code);
            Assert.IsFalse(first.Applied);
            Assert.AreEqual(0, state.ApplyCallCount, "rejection must not mutate state");
            Assert.AreEqual(first, second, "same record + same state => same result");

            Assert.AreEqual(1, batch.Count);
            Assert.AreEqual(record, batch.Records[0]);
        }

        [Test]
        public void Rejection_InvalidTarget_MutatesNothing()
        {
            uint actor = CommandV1TestUtil.EntityId(1, 1);
            uint missingTarget = CommandV1TestUtil.EntityId(9, 1);
            CommandRecord record = SealSingleRecord(
                new AttackTargetPayload(new[] { actor }, missingTarget), out _);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.AreEqual(CommandResultCode.RejectedInvalidTarget, result.Code);
            Assert.AreEqual(0, state.ApplyCallCount);
        }

        [Test]
        public void Rejection_InsufficientResources_MutatesNothing()
        {
            uint building = CommandV1TestUtil.EntityId(4, 1);
            CommandRecord record = SealSingleRecord(new QueueUnitPayload(building, 3, 2), out _);

            var state = new FakeStateView(localSlot: 0) { Affordable = false };
            state.AddEntity(building, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.AreEqual(CommandResultCode.RejectedInsufficientResources, result.Code);
            Assert.AreEqual(0, state.ApplyCallCount);
        }

        [Test]
        public void Success_AppliesExactlyOnce_AndResultCarriesStreamIdentity()
        {
            uint actor = CommandV1TestUtil.EntityId(1, 1);
            CommandRecord record = SealSingleRecord(new StopPayload(new[] { actor }), out _);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(actor, ownedByLocal: true);

            CommandResult result = CommandExecutor.Execute(record, state);
            Assert.AreEqual(CommandResultCode.Applied, result.Code);
            Assert.AreEqual(1, state.ApplyCallCount);

            Assert.AreEqual(record.PlayerSlot, result.PlayerSlot);
            Assert.AreEqual(record.Sequence, result.Sequence);
            Assert.AreEqual(record.TargetTick, result.TargetTick);
            Assert.AreEqual(record.Kind, result.Kind);
        }

        [Test]
        public void BatchExecution_ReturnsOneDeterministicResultPerRecord()
        {
            uint owned = CommandV1TestUtil.EntityId(1, 1);
            uint foreign = CommandV1TestUtil.EntityId(2, 1);

            var ingress = CommandV1TestUtil.CreateIngress();
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(CommandIntent.Create(new StopPayload(new[] { owned })), out _));
            Assert.AreEqual(
                CommandIngressResult.Accepted,
                ingress.TrySubmitIntent(CommandIntent.Create(new StopPayload(new[] { foreign })), out _));
            CommandBatch batch = ingress.SealTickBatch(1);

            var state = new FakeStateView(localSlot: 0);
            state.AddEntity(owned, ownedByLocal: true);
            state.AddEntity(foreign, ownedByLocal: false);

            CommandResult[] results = CommandExecutor.ExecuteBatch(batch, state);
            Assert.AreEqual(2, results.Length);
            Assert.AreEqual(CommandResultCode.Applied, results[0].Code);
            Assert.AreEqual(CommandResultCode.RejectedNotOwned, results[1].Code);
            Assert.AreEqual(1, state.ApplyCallCount);
        }
    }
}
