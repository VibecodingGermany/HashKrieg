using System.Collections.Generic;
using Nova.Core;

namespace Nova.Simulation.Replays
{
    /// <summary>
    /// Canonical replay container format, version 1 (docs/tech/SimulationCore.md
    /// sections 6 and 8). A replay binds the match fingerprint, the embedded
    /// initial snapshot, every accepted human/AI command record with its
    /// deterministic <c>CommandResult</c> and a running hash chain.
    /// <para>
    /// Byte layout (all multi-byte integers little-endian, no padding):
    /// <code>
    /// offset  size  field
    /// 0       8     Magic: ASCII "NOVAPLAY"
    /// 8       2     FormatVersion u16 (= 1)
    /// 10      4     FingerprintLength u32
    /// 14      ..    Fingerprint bytes (canonical MatchFingerprint serialization)
    /// ..      4     InitialSnapshotLength u32
    /// ..      ..    Initial snapshot bytes (canonical snapshot container v1,
    ///               embedded; a hash-only reference is the documented
    ///               alternative, see below)
    /// ..      4     TickCount u32
    /// ..      ..    TickCount tick frames (see below), ticks strictly
    ///               consecutive and starting at snapshot tick + 1
    /// ..      8     FinalStateHash u64 (NOVA_STATE_V1 hash after the last tick)
    /// ..      8     FinalChainHash u64 (chain step 2, see below)
    /// </code>
    /// Tick frame layout:
    /// <code>
    /// 0       4     Tick u32
    /// 4       2     RecordCount u16 (&lt;= MaxBatchRecordsPerTick = 256)
    /// per record, in canonical (TargetTick, PlayerSlot, Sequence) order:
    ///         2     RecordLength u16 (equals the inner record RecordLength)
    ///         ..    canonical record bytes (Commands.md section 2)
    ///         2     ResultCode u16 (CommandResultCode of the recorded tick)
    /// ..      8     ChainHash u64 (chain after this tick)
    /// </code>
    /// Empty ticks are recorded as frames with RecordCount 0, so the recorded
    /// tick range is gapless and the final state hash is reproducible.
    /// </para>
    /// <para>
    /// Hash chain (NOVA_REPLAY_CHAIN_V1 domain, XXH64 seed 0, SimHashWriter
    /// field order). chain_0 binds the fingerprint; every tick step binds the
    /// previous chain value, the tick, and per record its canonical bytes and
    /// its recorded result code; the final step binds the end state hash:
    /// <code>
    /// chain_0   = H( tag(0) | u32 FingerprintLength | fingerprint bytes )
    /// chain_n   = H( tag(1) | u64 chain_{n-1} | u32 tick | u16 recordCount |
    ///                per record: u32 RecordLength | record bytes | u16 resultCode )
    /// finalHash = H( tag(2) | u64 chain_last | u64 finalStateHash )
    /// </code>
    /// where H(x) is SimHashWriter.ForReplayChain() over x and tag(t) is
    /// WriteFieldTag(t). Record identity (slot, sequence, target tick, kind)
    /// is already bound by the record bytes, so the result contributes exactly
    /// its code. The chain value after each tick is stored in that tick's
    /// frame; the parser re-verifies incrementally, so the first tampered
    /// tick is the first chain mismatch.
    /// </para>
    /// <para>
    /// Documented alternative not implemented in v1: the initial snapshot may
    /// be carried as a hash-only reference (fingerprint InitialStateHash plus
    /// an externally resolved snapshot) instead of the embedded bytes. v1
    /// always embeds; a reference variant requires a new format version.
    /// The 64 MiB hard cap mirrors the snapshot container cap
    /// (SimulationCore.md section 7 fixes no replay cap; Q-040 candidate).
    /// </para>
    /// </summary>
    public static class ReplayFormat
    {
        /// <summary>ASCII magic bytes "NOVAPLAY" at offset 0.</summary>
        public static readonly byte[] Magic = { 0x4E, 0x4F, 0x56, 0x41, 0x50, 0x4C, 0x41, 0x59 };

        /// <summary>The only format version this implementation reads and writes.</summary>
        public const ushort FormatVersion = 1;

        /// <summary>Fixed header size through the fingerprint length field.</summary>
        public const int HeaderFixedBytes = 14;

        /// <summary>Trailer size: final state hash + final chain hash.</summary>
        public const int TrailerBytes = 16;

        /// <summary>Size of an empty tick frame (tick + record count + chain hash).</summary>
        public const int FrameFixedBytes = 14;

        /// <summary>Per-record frame overhead (record length u16 + result code u16).</summary>
        public const int RecordFrameFixedBytes = 4;

        /// <summary>
        /// Parser hard cap, checked before any content-driven allocation.
        /// Mirrors the snapshot cap; the spec fixes no replay cap (Q-040
        /// candidate, documented on the type).
        /// </summary>
        public const int MaxFileBytes = 64 * 1024 * 1024;

        /// <summary>Parser bound for the serialized fingerprint; checked before allocation.</summary>
        public const int MaxFingerprintBytes = 1024;

        /// <summary>Chain step tag: genesis (binds the fingerprint).</summary>
        public const uint ChainTagGenesis = 0;

        /// <summary>Chain step tag: one tick of records and results.</summary>
        public const uint ChainTagTick = 1;

        /// <summary>Chain step tag: final step (binds the end state hash).</summary>
        public const uint ChainTagFinal = 2;

        /// <summary>
        /// chain_0: binds the canonical fingerprint bytes into the chain
        /// (construction documented on <see cref="ReplayFormat"/>).
        /// </summary>
        public static ulong ComputeGenesisChainHash(byte[] fingerprintBytes)
        {
            var hash = SimHashWriter.ForReplayChain();
            hash.WriteFieldTag(ChainTagGenesis);
            hash.WriteLengthPrefixed(fingerprintBytes);
            return hash.Digest();
        }

        /// <summary>
        /// chain_n: one tick step over the previous chain value, the tick and
        /// per record its canonical bytes plus its recorded result code.
        /// </summary>
        public static ulong ComputeTickChainHash(
            ulong previousChainHash, uint tick,
            IReadOnlyList<byte[]> recordBytes, IReadOnlyList<ushort> resultCodes)
        {
            var hash = SimHashWriter.ForReplayChain();
            hash.WriteFieldTag(ChainTagTick);
            hash.WriteUInt64(previousChainHash);
            hash.WriteUInt32(tick);
            hash.WriteUInt16(unchecked((ushort)recordBytes.Count));
            for (int i = 0; i < recordBytes.Count; i++)
            {
                hash.WriteLengthPrefixed(recordBytes[i]);
                hash.WriteUInt16(resultCodes[i]);
            }
            return hash.Digest();
        }

        /// <summary>
        /// Final chain step: binds the recorded end state hash. Stored as the
        /// trailer FinalChainHash; equals chain_last when no ticks exist and
        /// the final state hash matches the snapshot state.
        /// </summary>
        public static ulong ComputeFinalChainHash(ulong lastTickChainHash, ulong finalStateHash)
        {
            var hash = SimHashWriter.ForReplayChain();
            hash.WriteFieldTag(ChainTagFinal);
            hash.WriteUInt64(lastTickChainHash);
            hash.WriteUInt64(finalStateHash);
            return hash.Digest();
        }
    }
}
