using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Canonical little-endian payload writer (docs/tech/Commands.md section 2).
    /// Grows internally but never beyond <see cref="CommandLimits.MaxPayloadBytes"/>;
    /// exceeding the cap throws <see cref="InvalidOperationException"/> because a
    /// payload that large is a structural error and must never be produced.
    /// </summary>
    public sealed class CommandPayloadWriter
    {
        private byte[] _buffer;
        private int _length;

        public CommandPayloadWriter(int initialCapacity = 64)
        {
            _buffer = new byte[Math.Max(16, initialCapacity)];
            _length = 0;
        }

        /// <summary>Bytes written so far.</summary>
        public int Length => _length;

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

        /// <summary>Q16.16 raw bits as int32 little-endian.</summary>
        public void WriteSimFixed(Core.SimFixed value) => WriteInt32(value.RawValue);

        /// <summary>
        /// Entity id list as uint16 count followed by each raw uint32 id.
        /// The caller must hand in a strictly ascending, duplicate-free list;
        /// canonical ordering is validated at the ingress, not repaired here.
        /// </summary>
        public void WriteEntityIds(uint[] entityIds)
        {
            if (entityIds == null) throw new ArgumentNullException(nameof(entityIds));
            WriteUInt16(unchecked((ushort)entityIds.Length));
            for (int i = 0; i < entityIds.Length; i++)
            {
                WriteUInt32(entityIds[i]);
            }
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
            if (_length + count > CommandLimits.MaxPayloadBytes)
            {
                throw new InvalidOperationException(
                    "Command payload exceeds MaxPayloadBytes; this is a structural error.");
            }
            if (_length + count > _buffer.Length)
            {
                int newCapacity = Math.Max(_buffer.Length * 2, _length + count);
                Array.Resize(ref _buffer, newCapacity);
            }
        }
    }
}
