using System;

namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Canonical little-endian writer for the content of a single snapshot
    /// block (docs/tech/Serialization.md section 1). Mirrors the encoding of
    /// <see cref="Core.SimHashWriter"/> so hash input and serialized bytes
    /// stay field-identical: same widths, same order, same two's-complement
    /// signed representation.
    /// <para>
    /// There are no float/double paths and no reflection/container
    /// enumeration; callers pass values in a fixed, explicitly ordered
    /// sequence. The buffer never grows beyond
    /// <see cref="SnapshotFormat.MaxFileBytes"/>; exceeding it throws
    /// <see cref="InvalidOperationException"/> because a block that large is
    /// a structural error and must never be produced.
    /// </para>
    /// </summary>
    public sealed class SnapshotBlockWriter
    {
        private byte[] _buffer;
        private int _length;

        public SnapshotBlockWriter(int initialCapacity = 64)
        {
            _buffer = new byte[Math.Max(16, initialCapacity)];
            _length = 0;
        }

        /// <summary>Bytes written so far.</summary>
        public int Length => _length;

        /// <summary>Field identifier; encoded as one uint32 little-endian.</summary>
        public void WriteFieldTag(uint tag) => WriteUInt32(tag);

        public void WriteUInt8(byte value)
        {
            Ensure(1);
            _buffer[_length++] = value;
        }

        public void WriteUInt16(ushort value)
        {
            Ensure(2);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
        }

        public void WriteUInt32(uint value)
        {
            Ensure(4);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 24);
        }

        /// <summary>Signed values serialize as two's-complement little-endian.</summary>
        public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));

        public void WriteUInt64(ulong value)
        {
            Ensure(8);
            _buffer[_length++] = (byte)value;
            _buffer[_length++] = (byte)(value >> 8);
            _buffer[_length++] = (byte)(value >> 16);
            _buffer[_length++] = (byte)(value >> 24);
            _buffer[_length++] = (byte)(value >> 32);
            _buffer[_length++] = (byte)(value >> 40);
            _buffer[_length++] = (byte)(value >> 48);
            _buffer[_length++] = (byte)(value >> 56);
        }

        /// <summary>Signed values serialize as two's-complement little-endian.</summary>
        public void WriteInt64(long value) => WriteUInt64(unchecked((ulong)value));

        /// <summary>Q16.16 raw bits as int32 little-endian.</summary>
        public void WriteSimFixed(Core.SimFixed value) => WriteInt32(value.RawValue);

        /// <summary>Angle units as uint16 little-endian.</summary>
        public void WriteSimAngle(Core.SimAngle value) => WriteUInt16(value.RawValue);

        /// <summary>Tick counter as uint32 little-endian.</summary>
        public void WriteTick(Core.Tick tick) => WriteUInt32(tick.Value);

        /// <summary>
        /// Entity handle as int32 index then uint16 version, both
        /// little-endian — identical to <see cref="Core.SimHashWriter.WriteEntityId"/>.
        /// The packed uint32 bit layout of SimulationCore.md section 1 is not
        /// yet implemented in <see cref="Core.EntityId"/> (open spec gap,
        /// see Q-040).
        /// </summary>
        public void WriteEntityId(Core.EntityId id)
        {
            WriteInt32(id.Index);
            WriteUInt16(id.Version);
        }

        /// <summary>Raw bytes in caller-defined order, without a length prefix.</summary>
        public void WriteBytes(ReadOnlySpan<byte> data)
        {
            Ensure(data.Length);
            data.CopyTo(new Span<byte>(_buffer, _length, data.Length));
            _length += data.Length;
        }

        /// <summary>
        /// Variable-length data as uint32 little-endian length followed by the
        /// raw bytes; the prefix prevents concatenation ambiguities.
        /// </summary>
        public void WriteLengthPrefixed(ReadOnlySpan<byte> data)
        {
            WriteUInt32(unchecked((uint)data.Length));
            WriteBytes(data);
        }

        /// <summary>Returns the written bytes as an exact-length copy.</summary>
        public byte[] ToArray()
        {
            var copy = new byte[_length];
            Array.Copy(_buffer, copy, _length);
            return copy;
        }

        private void Ensure(int count)
        {
            if ((long)_length + count > SnapshotFormat.MaxFileBytes)
            {
                throw new InvalidOperationException(
                    "Snapshot block exceeds MaxFileBytes; this is a structural error.");
            }
            if (_length + count > _buffer.Length)
            {
                long newCapacity = Math.Max((long)_buffer.Length * 2, (long)_length + count);
                Array.Resize(ref _buffer, (int)Math.Min(newCapacity, SnapshotFormat.MaxFileBytes));
            }
        }
    }
}
