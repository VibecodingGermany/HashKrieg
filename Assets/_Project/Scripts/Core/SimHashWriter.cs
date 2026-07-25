using System;
using System.Text;

namespace Nova.Core
{
    /// <summary>
    /// Canonical domain hash writer per docs/tech/SimulationCore.md section 5.
    /// All canonical 64-bit hashes are XXH64 with seed 0 over an
    /// ASCII domain prefix followed by byte 0x00, then field tags, lengths and
    /// values in canonical little-endian order.
    /// <para>
    /// The writer deliberately exposes only explicitly ordered write calls:
    /// there is no dictionary/reflection/container enumeration anywhere in
    /// this API. Callers must pass values in a fixed, deterministic order and
    /// must pre-sort collections by a canonical key themselves — sort order is
    /// caller responsibility and is never derived from runtime iteration order.
    /// </para>
    /// <para>
    /// No float/double paths exist here; authoritative values use
    /// <see cref="SimFixed"/>, <see cref="Tick"/> and <see cref="EntityId"/>.
    /// Allocation-frugal: one reusable 8-byte scratch buffer per writer.
    /// </para>
    /// </summary>
    public sealed class SimHashWriter
    {
        /// <summary>ASCII domain prefix for simulation state hashes.</summary>
        public static readonly byte[] StateDomain = Ascii("NOVA_STATE_V1");

        /// <summary>ASCII domain prefix for definition hashes.</summary>
        public static readonly byte[] DefinitionsDomain = Ascii("NOVA_DEFINITIONS_V1");

        /// <summary>ASCII domain prefix for file/block hashes.</summary>
        public static readonly byte[] FileDomain = Ascii("NOVA_FILE_V1");

        /// <summary>ASCII domain prefix for replay chain hashes.</summary>
        public static readonly byte[] ReplayChainDomain = Ascii("NOVA_REPLAY_CHAIN_V1");

        private readonly XxHash64State _state = new XxHash64State(XxHash64.CanonicalSeed);
        private readonly byte[] _scratch = new byte[8];

        /// <summary>
        /// Starts a canonical hash for <paramref name="asciiDomain"/>: the
        /// prefix bytes are written verbatim, followed by one 0x00 terminator
        /// byte (SimulationCore.md section 5). Use the static domain constants.
        /// </summary>
        public SimHashWriter(ReadOnlySpan<byte> asciiDomain)
        {
            _state.Update(asciiDomain);
            _state.Update(new byte[] { 0x00 });
        }

        /// <summary>Canonical writer for the simulation state domain.</summary>
        public static SimHashWriter ForState() => new SimHashWriter(StateDomain);

        /// <summary>Canonical writer for the definitions domain.</summary>
        public static SimHashWriter ForDefinitions() => new SimHashWriter(DefinitionsDomain);

        /// <summary>Canonical writer for the file/block domain.</summary>
        public static SimHashWriter ForFile() => new SimHashWriter(FileDomain);

        /// <summary>Canonical writer for the replay chain domain.</summary>
        public static SimHashWriter ForReplayChain() => new SimHashWriter(ReplayChainDomain);

        /// <summary>Field identifier; encoded as one uint32 little-endian.</summary>
        public void WriteFieldTag(uint tag) => WriteUInt32(tag);

        public void WriteUInt8(byte value)
        {
            _scratch[0] = value;
            _state.Update(new ReadOnlySpan<byte>(_scratch, 0, 1));
        }

        public void WriteUInt16(ushort value)
        {
            _scratch[0] = (byte)value;
            _scratch[1] = (byte)(value >> 8);
            _state.Update(new ReadOnlySpan<byte>(_scratch, 0, 2));
        }

        public void WriteUInt32(uint value)
        {
            _scratch[0] = (byte)value;
            _scratch[1] = (byte)(value >> 8);
            _scratch[2] = (byte)(value >> 16);
            _scratch[3] = (byte)(value >> 24);
            _state.Update(new ReadOnlySpan<byte>(_scratch, 0, 4));
        }

        /// <summary>Signed values hash over their two's-complement little-endian bytes.</summary>
        public void WriteInt32(int value) => WriteUInt32(unchecked((uint)value));

        public void WriteUInt64(ulong value)
        {
            _scratch[0] = (byte)value;
            _scratch[1] = (byte)(value >> 8);
            _scratch[2] = (byte)(value >> 16);
            _scratch[3] = (byte)(value >> 24);
            _scratch[4] = (byte)(value >> 32);
            _scratch[5] = (byte)(value >> 40);
            _scratch[6] = (byte)(value >> 48);
            _scratch[7] = (byte)(value >> 56);
            _state.Update(new ReadOnlySpan<byte>(_scratch, 0, 8));
        }

        /// <summary>Signed values hash over their two's-complement little-endian bytes.</summary>
        public void WriteInt64(long value) => WriteUInt64(unchecked((ulong)value));

        /// <summary>Q16.16 raw bits as int32 little-endian.</summary>
        public void WriteSimFixed(SimFixed value) => WriteInt32(value.RawValue);

        /// <summary>Tick counter as uint32 little-endian.</summary>
        public void WriteTick(Tick tick) => WriteUInt32(tick.Value);

        /// <summary>
        /// Entity handle as int32 index then uint16 version, both little-endian.
        /// The current <see cref="EntityId"/> type stores index/version as
        /// separate fields; the packed uint32 bit layout of SimulationCore.md
        /// section 1 is not yet implemented (open spec gap, see Q-040).
        /// </summary>
        public void WriteEntityId(EntityId id)
        {
            WriteInt32(id.Index);
            WriteUInt16(id.Version);
        }

        /// <summary>
        /// Raw bytes in caller-defined order. Hashes are never built over
        /// runtime object layouts or container iteration order; callers must
        /// hand in explicitly ordered sequences (sorting is caller duty).
        /// </summary>
        public void WriteBytes(ReadOnlySpan<byte> data) => _state.Update(data);

        /// <summary>
        /// Variable-length data as uint32 little-endian length followed by the
        /// raw bytes. The length prefix prevents prefix collisions between
        /// concatenated variable fields.
        /// </summary>
        public void WriteLengthPrefixed(ReadOnlySpan<byte> data)
        {
            WriteUInt32(unchecked((uint)data.Length));
            _state.Update(data);
        }

        /// <summary>UTF-8 text with uint32 little-endian byte-length prefix.</summary>
        public void WriteLengthPrefixedString(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            WriteLengthPrefixed(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>Finalizes and returns the canonical XXH64 seed-0 domain hash.</summary>
        public ulong Digest() => _state.Digest();

        private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
    }
}
