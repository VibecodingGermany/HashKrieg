using NUnit.Framework;
using Nova.Core;
using Nova.Networking;
using Nova.Simulation;

namespace Nova.Networking.Tests
{
    [TestFixture]
    public class LockstepRelayBufferTests
    {
        [Test]
        public void CommandEnvelopeNetPacket_Serialization_PreservesValues()
        {
            var packet = new CommandEnvelopeNetPacket(
                targetTick: 100,
                playerId: 0,
                type: CommandType.Move,
                unitId: new EntityId(42, 1),
                targetUnitId: EntityId.Invalid,
                targetPositionX: 35.5f,
                targetPositionY: 45.5f,
                stateHash: 0x123456789ABCDEF0
            );

            byte[] bytes = packet.Serialize();
            Assert.AreEqual(41, bytes.Length);

            CommandEnvelopeNetPacket restored = CommandEnvelopeNetPacket.Deserialize(bytes);

            Assert.AreEqual(100, restored.TargetTick);
            Assert.AreEqual(0, restored.PlayerId);
            Assert.AreEqual(CommandType.Move, restored.Type);
            Assert.AreEqual(new EntityId(42, 1), restored.UnitId);
            Assert.AreEqual(35.5f, restored.TargetPositionX);
            Assert.AreEqual(0x123456789ABCDEF0, restored.StateHash);
        }

        [Test]
        public void LockstepRelayBuffer_VerifyDesyncHashes_DetectsDesync()
        {
            var buffer = new LockstepRelayBuffer();

            var p0 = new CommandEnvelopeNetPacket(50, 0, CommandType.Stop, EntityId.Invalid, EntityId.Invalid, 0f, 0f, stateHash: 0x999);
            var p1 = new CommandEnvelopeNetPacket(50, 1, CommandType.Stop, EntityId.Invalid, EntityId.Invalid, 0f, 0f, stateHash: 0x888); // Mismatch!

            buffer.PushPacket(p0);
            buffer.PushPacket(p1);

            Assert.IsTrue(buffer.IsTickReady(50, 2));

            bool synchronized = buffer.VerifyDesyncHashes(50, out ulong mismatchHash);
            Assert.IsFalse(synchronized);
            Assert.AreEqual(0x888, mismatchHash);
        }
    }
}
