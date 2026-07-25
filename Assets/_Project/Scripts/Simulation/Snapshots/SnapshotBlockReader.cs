using System;

namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Bounds-checked little-endian reader over the content of one snapshot
    /// block; the strict counterpart of <see cref="SnapshotBlockWriter"/>.
    /// Every read validates the remaining length before touching memory; a
    /// truncated or overlong block is reported as a parse failure, never as
    /// an exception or an out-of-bounds access (docs/tech/SimulationCore.md
    /// section 7, point 4). A canonical block must end with
    /// <see cref="Remaining"/> exactly zero.
    /// </summary>
    public ref struct SnapshotBlockReader
    {
        private readonly ReadOnlySpan<byte> _span;
        private int _offset;

        public SnapshotBlockReader(ReadOnlySpan<byte> span)
        {
            _span = span;
            _offset = 0;
        }

        /// <summary>Unconsumed bytes; a canonical block must end at exactly zero.</summary>
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

        public bool TryReadUInt64(out ulong value)
        {
            value = 0;
            if (Remaining < 8) return false;
            value = (ulong)_span[_offset]
                | ((ulong)_span[_offset + 1] << 8)
                | ((ulong)_span[_offset + 2] << 16)
                | ((ulong)_span[_offset + 3] << 24)
                | ((ulong)_span[_offset + 4] << 32)
                | ((ulong)_span[_offset + 5] << 40)
                | ((ulong)_span[_offset + 6] << 48)
                | ((ulong)_span[_offset + 7] << 56);
            _offset += 8;
            return true;
        }

        public bool TryReadInt64(out long value)
        {
            bool ok = TryReadUInt64(out ulong raw);
            value = unchecked((long)raw);
            return ok;
        }

        public bool TryReadSimFixed(out Core.SimFixed value)
        {
            bool ok = TryReadInt32(out int raw);
            value = Core.SimFixed.FromRaw(raw);
            return ok;
        }

        public bool TryReadSimAngle(out Core.SimAngle value)
        {
            bool ok = TryReadUInt16(out ushort raw);
            value = Core.SimAngle.FromRaw(raw);
            return ok;
        }

        public bool TryReadTick(out Core.Tick value)
        {
            bool ok = TryReadUInt32(out uint raw);
            value = new Core.Tick(raw);
            return ok;
        }

        /// <summary>
        /// Reads an entity handle as int32 index then uint16 version, the
        /// exact counterpart of <see cref="SnapshotBlockWriter.WriteEntityId"/>.
        /// </summary>
        public bool TryReadEntityId(out Core.EntityId value)
        {
            value = Core.EntityId.Invalid;
            if (!TryReadInt32(out int index)) return false;
            if (!TryReadUInt16(out ushort version)) return false;
            value = new Core.EntityId(index, version);
            return true;
        }

        /// <summary>
        /// Reads exactly <paramref name="count"/> raw bytes as a span over the
        /// original buffer. The count is validated against the remaining
        /// length before the span is formed; no allocation happens.
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
        /// Reads a uint32 little-endian length followed by that many raw
        /// bytes as a span over the original buffer. The declared length is
        /// validated against the remaining bytes before the span is formed;
        /// no allocation happens.
        /// </summary>
        public bool TryReadLengthPrefixed(out ReadOnlySpan<byte> bytes)
        {
            bytes = default;
            if (!TryReadUInt32(out uint count)) return false;
            if (count > int.MaxValue) return false;
            return TryReadBytes((int)count, out bytes);
        }
    }
}
