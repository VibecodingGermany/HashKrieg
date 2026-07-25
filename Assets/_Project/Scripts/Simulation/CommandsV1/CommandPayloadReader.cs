using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Bounds-checked little-endian reader over a command payload. Every read
    /// validates the remaining length before touching memory; a truncated or
    /// overlong payload is reported as a parse failure, never as an exception
    /// or an out-of-bounds access (docs/tech/Commands.md sections 2 and 4).
    /// </summary>
    public ref struct CommandPayloadReader
    {
        private readonly ReadOnlySpan<byte> _span;
        private int _offset;

        public CommandPayloadReader(ReadOnlySpan<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        /// <summary>Unconsumed bytes; a canonical payload must end at exactly zero.</summary>
        public int Remaining => _span.Length - _offset;

        public bool TryReadUInt8(out byte value)
        {
            value = 0;
            if (Remaining < 1) return false;
            value = _span[_offset];
            _offset += 1;
            return true;
        }

        public bool TryReadUInt16(out ushort value)
        {
            value = 0;
            if (Remaining < 2) return false;
            value = (ushort)(_span[_offset] | (_span[_offset + 1] << 8));
            _offset += 2;
            return true;
        }

        public bool TryReadUInt32(out uint value)
        {
            value = 0;
            if (Remaining < 4) return false;
            value = (uint)_span[_offset]
                | ((uint)_span[_offset + 1] << 8)
                | ((uint)_span[_offset + 2] << 16)
                | ((uint)_span[_offset + 3] << 24);
            _offset += 4;
            return true;
        }

        public bool TryReadInt32(out int value)
        {
            bool ok = TryReadUInt32(out uint raw);
            value = unchecked((int)raw);
            return ok;
        }

        public bool TryReadSimFixed(out Core.SimFixed value)
        {
            bool ok = TryReadInt32(out int raw);
            value = Core.SimFixed.FromRaw(raw);
            return ok;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> raw bytes as a span over the
        /// original buffer. The count is validated against the remaining length
        /// before the span is formed; no allocation happens.
        /// </summary>
        public bool TryReadBytes(int count, out ReadOnlySpan<byte> bytes)
        {
            bytes = default;
            if (count < 0 || Remaining < count) return false;
            bytes = _span.Slice(_offset, count);
            _offset += count;
            return true;
        }

        /// <summary>
        /// Reads a uint16 count and then that many raw uint32 entity ids into a
        /// freshly allocated array. The count is validated against
        /// <see cref="CommandLimits.MaxEntityIdsPerCommand"/> and the remaining
        /// byte length before any allocation happens.
        /// </summary>
        public bool TryReadEntityIds(out uint[] entityIds)
        {
            entityIds = null;
            if (!TryReadUInt16(out ushort count)) return false;
            if (count > CommandLimits.MaxEntityIdsPerCommand) return false;
            if (Remaining < count * 4) return false;
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
            {
                if (!TryReadUInt32(out ids[i])) return false;
            }
            entityIds = ids;
            return true;
        }
    }
}
