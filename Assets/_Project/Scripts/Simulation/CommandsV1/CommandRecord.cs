using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// One sealed record of the canonical command stream v1
    /// (docs/tech/Commands.md section 2). All integers are little-endian; the
    /// wire layout is exactly:
    /// <code>
    /// RecordLength u16 | EnqueueTick u32 | TargetTick u32 | PlayerSlot u8 |
    /// Sequence u32 | CommandKind u16 | PayloadVersion u8 | PayloadLength u16 |
    /// Payload bytes
    /// </code>
    /// <para>
    /// Trust boundary (Commands.md section 1): the constructor is internal.
    /// UI and AI assemblies can build <see cref="CommandIntent"/> values but
    /// can never forge a record — only <see cref="CommandIngress"/> (same
    /// assembly) assigns PlayerSlot, Sequence and TargetTick and seals records.
    /// </para>
    /// </summary>
    public readonly struct CommandRecord : IEquatable<CommandRecord>
    {
        /// <summary>Tick at the session ingress.</summary>
        public uint EnqueueTick { get; }

        /// <summary>Earliest execution tick: EnqueueTick + InputDelayTicks.</summary>
        public uint TargetTick { get; }

        /// <summary>Session-bound active player slot.</summary>
        public byte PlayerSlot { get; }

        /// <summary>Monotonically increasing sequence per player; starts at 1.</summary>
        public uint Sequence { get; }

        public CommandKind Kind { get; }

        /// <summary>Layout version of the concrete payload.</summary>
        public byte PayloadVersion { get; }

        private readonly byte[] _payload;

        /// <summary>Canonical payload bytes (never null, may be empty).</summary>
        public ReadOnlyMemory<byte> Payload => _payload;

        /// <summary>Total serialized record bytes (header + payload).</summary>
        public int RecordBytes => CommandLimits.HeaderBytes + _payload.Length;

        internal CommandRecord(
            uint enqueueTick,
            uint targetTick,
            byte playerSlot,
            uint sequence,
            CommandKind kind,
            byte payloadVersion,
            byte[] payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (payload.Length > CommandLimits.MaxPayloadBytes)
            {
                throw new ArgumentException("Payload exceeds MaxPayloadBytes.", nameof(payload));
            }
            EnqueueTick = enqueueTick;
            TargetTick = targetTick;
            PlayerSlot = playerSlot;
            Sequence = sequence;
            Kind = kind;
            PayloadVersion = payloadVersion;
            _payload = payload;
        }

        /// <summary>Serializes the exact canonical record bytes (little-endian).</summary>
        public byte[] Serialize()
        {
            var bytes = new byte[RecordBytes];
            WriteUInt16(bytes, 0, unchecked((ushort)RecordBytes));
            WriteUInt32(bytes, 2, EnqueueTick);
            WriteUInt32(bytes, 6, TargetTick);
            bytes[10] = PlayerSlot;
            WriteUInt32(bytes, 11, Sequence);
            WriteUInt16(bytes, 15, (ushort)Kind);
            bytes[17] = PayloadVersion;
            WriteUInt16(bytes, 18, unchecked((ushort)_payload.Length));
            Array.Copy(_payload, 0, bytes, CommandLimits.HeaderBytes, _payload.Length);
            return bytes;
        }

        /// <summary>
        /// Parses one record from the front of <paramref name="buffer"/>.
        /// Every length is validated before any allocation: the declared
        /// RecordLength must fit the header bounds, the buffer must contain the
        /// whole declared record, and PayloadLength must exactly match
        /// RecordLength - HeaderBytes (docs/tech/Commands.md sections 2 and 4).
        /// Only the payload copy allocates, and only after all checks pass.
        /// </summary>
        public static bool TryDeserialize(
            ReadOnlySpan<byte> buffer, out CommandRecord record, out int recordBytes)
        {
            record = default;
            recordBytes = 0;
            if (buffer.Length < 2) return false;

            int declaredLength = buffer[0] | (buffer[1] << 8);
            if (declaredLength < CommandLimits.HeaderBytes) return false;
            if (declaredLength > CommandLimits.MaxRecordBytes) return false;
            if (buffer.Length < declaredLength) return false;

            uint enqueueTick = ReadUInt32(buffer, 2);
            uint targetTick = ReadUInt32(buffer, 6);
            byte playerSlot = buffer[10];
            uint sequence = ReadUInt32(buffer, 11);
            var kind = (CommandKind)(buffer[15] | (buffer[16] << 8));
            byte payloadVersion = buffer[17];
            int payloadLength = buffer[18] | (buffer[19] << 8);
            if (payloadLength != declaredLength - CommandLimits.HeaderBytes) return false;

            var payload = new byte[payloadLength];
            buffer.Slice(CommandLimits.HeaderBytes, payloadLength).CopyTo(payload);

            record = new CommandRecord(
                enqueueTick, targetTick, playerSlot, sequence, kind, payloadVersion, payload);
            recordBytes = declaredLength;
            return true;
        }

        /// <summary>Value equality over the exact canonical record bytes.</summary>
        public bool Equals(CommandRecord other)
        {
            return EnqueueTick == other.EnqueueTick
                && TargetTick == other.TargetTick
                && PlayerSlot == other.PlayerSlot
                && Sequence == other.Sequence
                && Kind == other.Kind
                && PayloadVersion == other.PayloadVersion
                && _payload.AsSpan().SequenceEqual(other._payload);
        }

        public override bool Equals(object obj) => obj is CommandRecord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)EnqueueTick;
                hash = (hash * 397) ^ (int)TargetTick;
                hash = (hash * 397) ^ PlayerSlot;
                hash = (hash * 397) ^ (int)Sequence;
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ PayloadVersion;
                for (int i = 0; i < _payload.Length; i++)
                {
                    hash = (hash * 31) ^ _payload[i];
                }
                return hash;
            }
        }

        public static bool operator ==(CommandRecord left, CommandRecord right) => left.Equals(right);
        public static bool operator !=(CommandRecord left, CommandRecord right) => !left.Equals(right);

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)value;
            bytes[offset + 1] = (byte)(value >> 8);
            bytes[offset + 2] = (byte)(value >> 16);
            bytes[offset + 3] = (byte)(value >> 24);
        }

        private static uint ReadUInt32(ReadOnlySpan<byte> buffer, int offset)
        {
            return (uint)buffer[offset]
                | ((uint)buffer[offset + 1] << 8)
                | ((uint)buffer[offset + 2] << 16)
                | ((uint)buffer[offset + 3] << 24);
        }
    }
}
