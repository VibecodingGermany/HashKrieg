using System;
using Nova.Core;

namespace Nova.Simulation
{
    /// <summary>
    /// Unboxed, fixed-size command container for deterministic simulation transport.
    /// Carries player actions, AI decisions, and peer commands without GC allocations.
    /// </summary>
    public readonly struct CommandEnvelope : IEquatable<CommandEnvelope>
    {
        public CommandType Type { get; }
        public CommandIssuer Issuer { get; }
        public byte PlayerId { get; }
        public uint Sequence { get; }
        public Tick TargetTick { get; }
        public EntityId EntityId { get; }
        public EntityId TargetEntityId { get; }
        public float TargetPositionX { get; }
        public float TargetPositionY { get; }
        public float TargetPositionZ { get; }
        public int DataId { get; }

        public CommandEnvelope(
            CommandType type,
            CommandIssuer issuer,
            byte playerId,
            uint sequence,
            Tick targetTick,
            EntityId entityId,
            EntityId targetEntityId = default,
            float targetPositionX = 0f,
            float targetPositionY = 0f,
            float targetPositionZ = 0f,
            int dataId = 0)
        {
            Type = type;
            Issuer = issuer;
            PlayerId = playerId;
            Sequence = sequence;
            TargetTick = targetTick;
            EntityId = entityId;
            TargetEntityId = targetEntityId;
            TargetPositionX = targetPositionX;
            TargetPositionY = targetPositionY;
            TargetPositionZ = targetPositionZ;
            DataId = dataId;
        }

        public bool Equals(CommandEnvelope other)
        {
            return Type == other.Type &&
                   Issuer == other.Issuer &&
                   PlayerId == other.PlayerId &&
                   Sequence == other.Sequence &&
                   TargetTick == other.TargetTick &&
                   EntityId == other.EntityId &&
                   TargetEntityId == other.TargetEntityId &&
                   TargetPositionX.Equals(other.TargetPositionX) &&
                   TargetPositionY.Equals(other.TargetPositionY) &&
                   TargetPositionZ.Equals(other.TargetPositionZ) &&
                   DataId == other.DataId;
        }

        public override bool Equals(object obj) => obj is CommandEnvelope other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Type;
                hash = (hash * 397) ^ (int)Issuer;
                hash = (hash * 397) ^ PlayerId;
                hash = (hash * 397) ^ (int)Sequence;
                hash = (hash * 397) ^ TargetTick.GetHashCode();
                hash = (hash * 397) ^ EntityId.GetHashCode();
                return hash;
            }
        }
    }
}
