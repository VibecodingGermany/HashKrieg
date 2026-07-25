using System;
using System.IO;
using Nova.Core;
using Nova.Simulation;

namespace Nova.Networking
{
    /// <summary>
    /// Binary network packet struct for transmitting lockstep CommandEnvelopes over UDP relays.
    /// Zero engine dependencies (no UnityEngine types).
    /// </summary>
    public readonly struct CommandEnvelopeNetPacket
    {
        public uint TargetTick { get; }
        public byte PlayerId { get; }
        public CommandType Type { get; }
        public int UnitIndex { get; }
        public ushort UnitVersion { get; }
        public int TargetUnitIndex { get; }
        public ushort TargetUnitVersion { get; }
        public float TargetPositionX { get; }
        public float TargetPositionY { get; }
        public ulong StateHash { get; }

        public EntityId UnitId => new EntityId(UnitIndex, UnitVersion);
        public EntityId TargetUnitId => new EntityId(TargetUnitIndex, TargetUnitVersion);

        public CommandEnvelopeNetPacket(
            uint targetTick,
            byte playerId,
            CommandType type,
            EntityId unitId,
            EntityId targetUnitId,
            float targetPositionX,
            float targetPositionY,
            ulong stateHash)
        {
            TargetTick = targetTick;
            PlayerId = playerId;
            Type = type;
            UnitIndex = unitId.Index;
            UnitVersion = unitId.Version;
            TargetUnitIndex = targetUnitId.Index;
            TargetUnitVersion = targetUnitId.Version;
            TargetPositionX = targetPositionX;
            TargetPositionY = targetPositionY;
            StateHash = stateHash;
        }

        public CommandEnvelope ToCommandEnvelope()
        {
            return new CommandEnvelope(
                type: Type,
                issuer: CommandIssuer.Human,
                playerId: PlayerId,
                sequence: 0,
                targetTick: new Tick(TargetTick),
                entityId: UnitId,
                targetEntityId: TargetUnitId,
                targetPositionX: TargetPositionX,
                targetPositionY: TargetPositionY
            );
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream(41))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(TargetTick);
                writer.Write(PlayerId);
                writer.Write((byte)Type);
                writer.Write(UnitIndex);
                writer.Write(UnitVersion);
                writer.Write(TargetUnitIndex);
                writer.Write(TargetUnitVersion);
                writer.Write(TargetPositionX);
                writer.Write(TargetPositionY);
                writer.Write(StateHash);
                return ms.ToArray();
            }
        }

        public static CommandEnvelopeNetPacket Deserialize(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 41)
                throw new ArgumentException("Invalid network packet buffer size.", nameof(buffer));

            using (var ms = new MemoryStream(buffer))
            using (var reader = new BinaryReader(ms))
            {
                uint targetTick = reader.ReadUInt32();
                byte playerId = reader.ReadByte();
                CommandType type = (CommandType)reader.ReadByte();
                int unitIdx = reader.ReadInt32();
                ushort unitVer = reader.ReadUInt16();
                int targetUnitIdx = reader.ReadInt32();
                ushort targetUnitVer = reader.ReadUInt16();
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                ulong stateHash = reader.ReadUInt64();

                return new CommandEnvelopeNetPacket(
                    targetTick,
                    playerId,
                    type,
                    new EntityId(unitIdx, unitVer),
                    new EntityId(targetUnitIdx, targetUnitVer),
                    posX,
                    posY,
                    stateHash
                );
            }
        }
    }
}
