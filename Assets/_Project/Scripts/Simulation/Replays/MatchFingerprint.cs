using System;
using System.Text;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation.Replays
{
    /// <summary>Occupancy of one of the eight reserved player slots (SimulationCore.md section 6).</summary>
    public enum PlayerSlotOccupancy : byte
    {
        /// <summary>Reserved but unused.</summary>
        Free = 0,

        /// <summary>Actively played by a human.</summary>
        Human = 1,

        /// <summary>Actively played by the AI (its accepted commands are recorded in the replay).</summary>
        AI = 2,
    }

    /// <summary>
    /// Stub selector for the deterministic stand-in content hashes used until
    /// canonical rules/definitions/map sources exist (Q-040 candidate).
    /// </summary>
    public enum MatchContentStub : uint
    {
        /// <summary>Stub for RulesHash64.</summary>
        Rules = 1,

        /// <summary>Stub for DefinitionsHash64.</summary>
        Definitions = 2,

        /// <summary>Stub for MapHash64.</summary>
        Map = 3,
    }

    /// <summary>
    /// The match fingerprint of docs/tech/SimulationCore.md section 6: the
    /// single identity object that snapshot, replay and command stream bind.
    /// It covers the State/Command/Payload/Snapshot/Sidecar schema versions,
    /// the numeric model id, the tick rate, the PRNG id, the
    /// rules/definitions/map content hashes, the match configuration (eight
    /// reserved slots with their occupancies), the input delay
    /// (Commands.md section 1: part of the fingerprint, MS-1 value exactly 1),
    /// the start seed and the hash of the initial state. Any missing or
    /// diverging component refuses replay start.
    /// <para>
    /// Canonical serialization (all integers little-endian, fixed field
    /// order, no padding):
    /// <code>
    /// StateSchemaVersion u16 | CommandSchemaVersion u16 |
    /// PayloadSchemaVersion u16 | SnapshotSchemaVersion u16 |
    /// SidecarSchemaVersion u16 |
    /// NumericModelId u32 length + ASCII bytes |
    /// TicksPerSecond u16 |
    /// PrngId u32 length + ASCII bytes |
    /// RulesHash64 u64 | DefinitionsHash64 u64 | MapHash64 u64 |
    /// SlotOccupancy 8 x u8 (0=Free, 1=Human, 2=AI) |
    /// StartSeed u64 | InitialStateHash u64 | InputDelayTicks u32
    /// </code>
    /// </para>
    /// <para>
    /// <see cref="ComputeHash"/> uses the NOVA_DEFINITIONS_V1 domain.
    /// SimulationCore.md section 5 defines exactly four hash domains (state,
    /// definitions, file, replay chain); the fingerprint binds schema and
    /// content/definition identity, not simulation state, not a file
    /// container and not a replay chain step, so the definitions domain is
    /// the only fitting choice. The absence of a dedicated fingerprint
    /// domain is a Q-040 candidate to be ratified before the schema freeze.
    /// </para>
    /// </summary>
    public sealed class MatchFingerprint : IEquatable<MatchFingerprint>
    {
        /// <summary>Current State schema version (schema family starts at 1 after the G1 compatibility reset).</summary>
        public const ushort StateSchemaVersionV1 = 1;

        /// <summary>Current Command schema version.</summary>
        public const ushort CommandSchemaVersionV1 = 1;

        /// <summary>Current Payload schema version.</summary>
        public const ushort PayloadSchemaVersionV1 = 1;

        /// <summary>Current Snapshot schema version.</summary>
        public const ushort SnapshotSchemaVersionV1 = 1;

        /// <summary>Current Sidecar (AI) schema version.</summary>
        public const ushort SidecarSchemaVersionV1 = 1;

        /// <summary>The only numeric model id of schema v1 (SimulationCore.md section 1).</summary>
        public const string NumericModelIdV1 = "Q16_16_V1";

        /// <summary>The canonical tick rate (SimulationCore.md section 2).</summary>
        public const ushort TicksPerSecondV1 = 10;

        /// <summary>The only PRNG id of schema v1 (SimulationCore.md section 1).</summary>
        public const string PrngIdV1 = "XorShift128PlusV1";

        /// <summary>Parser bound for one identifier string; checked before allocation.</summary>
        public const int MaxIdentifierBytes = 64;

        /// <summary>Serialization version of this fingerprint layout.</summary>
        public const byte FormatVersion = 1;

        public ushort StateSchemaVersion { get; }
        public ushort CommandSchemaVersion { get; }
        public ushort PayloadSchemaVersion { get; }
        public ushort SnapshotSchemaVersion { get; }
        public ushort SidecarSchemaVersion { get; }
        public string NumericModelId { get; }
        public ushort TicksPerSecond { get; }
        public string PrngId { get; }
        public ulong RulesHash64 { get; }
        public ulong DefinitionsHash64 { get; }
        public ulong MapHash64 { get; }
        public ulong StartSeed { get; }
        public ulong InitialStateHash { get; }
        public uint InputDelayTicks { get; }

        private readonly byte[] _slotOccupancy;

        /// <summary>
        /// Creates a fingerprint with explicit field values. Callers
        /// constructing a current-schema fingerprint use
        /// <see cref="CreateCurrent"/>; the explicit constructor exists so
        /// foreign (e.g. future or tampered) fingerprints can be represented
        /// for equality comparison — a divergence must be representable to be
        /// refusable.
        /// </summary>
        public MatchFingerprint(
            ushort stateSchemaVersion,
            ushort commandSchemaVersion,
            ushort payloadSchemaVersion,
            ushort snapshotSchemaVersion,
            ushort sidecarSchemaVersion,
            string numericModelId,
            ushort ticksPerSecond,
            string prngId,
            ulong rulesHash64,
            ulong definitionsHash64,
            ulong mapHash64,
            byte[] slotOccupancy,
            ulong startSeed,
            ulong initialStateHash,
            uint inputDelayTicks)
        {
            ValidateIdentifier(numericModelId, nameof(numericModelId));
            ValidateIdentifier(prngId, nameof(prngId));
            if (slotOccupancy == null) throw new ArgumentNullException(nameof(slotOccupancy));
            if (slotOccupancy.Length != CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentException(
                    $"Exactly {CommandLimits.ReservedPlayerSlots} slot occupancies are required.",
                    nameof(slotOccupancy));
            }
            for (int i = 0; i < slotOccupancy.Length; i++)
            {
                if (slotOccupancy[i] > (byte)PlayerSlotOccupancy.AI)
                {
                    throw new ArgumentException(
                        $"Slot {i} occupancy {slotOccupancy[i]} is not a defined value.",
                        nameof(slotOccupancy));
                }
            }

            StateSchemaVersion = stateSchemaVersion;
            CommandSchemaVersion = commandSchemaVersion;
            PayloadSchemaVersion = payloadSchemaVersion;
            SnapshotSchemaVersion = snapshotSchemaVersion;
            SidecarSchemaVersion = sidecarSchemaVersion;
            NumericModelId = numericModelId;
            TicksPerSecond = ticksPerSecond;
            PrngId = prngId;
            RulesHash64 = rulesHash64;
            DefinitionsHash64 = definitionsHash64;
            MapHash64 = mapHash64;
            _slotOccupancy = (byte[])slotOccupancy.Clone();
            StartSeed = startSeed;
            InitialStateHash = initialStateHash;
            InputDelayTicks = inputDelayTicks;
        }

        /// <summary>
        /// Creates a fingerprint pinned to the current schema v1 constants
        /// (versions, numeric model, 10 Hz, PRNG id).
        /// </summary>
        public static MatchFingerprint CreateCurrent(
            ulong rulesHash64,
            ulong definitionsHash64,
            ulong mapHash64,
            byte[] slotOccupancy,
            ulong startSeed,
            ulong initialStateHash,
            uint inputDelayTicks)
        {
            return new MatchFingerprint(
                StateSchemaVersionV1, CommandSchemaVersionV1, PayloadSchemaVersionV1,
                SnapshotSchemaVersionV1, SidecarSchemaVersionV1,
                NumericModelIdV1, TicksPerSecondV1, PrngIdV1,
                rulesHash64, definitionsHash64, mapHash64,
                slotOccupancy, startSeed, initialStateHash, inputDelayTicks);
        }

        /// <summary>
        /// Deterministic stand-in content hash until canonical
        /// rules/definitions/map sources exist (Q-040 candidate): XXH64 seed 0
        /// in the NOVA_DEFINITIONS_V1 domain over a stub field tag and an
        /// empty item list (u32 count = 0). Distinct tags keep the three
        /// stubs distinct; every host computes the identical value.
        /// </summary>
        public static ulong ComputeEmptyContentStubHash(MatchContentStub stub)
        {
            var hash = SimHashWriter.ForDefinitions();
            hash.WriteFieldTag((uint)stub);
            hash.WriteUInt32(0); // empty canonical item list
            return hash.Digest();
        }

        /// <summary>Occupancy of one reserved slot (index 0..7).</summary>
        public PlayerSlotOccupancy GetSlotOccupancy(int slot)
        {
            if (slot < 0 || slot >= CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }
            return (PlayerSlotOccupancy)_slotOccupancy[slot];
        }

        /// <summary>A defensive copy of the eight slot occupancies.</summary>
        public byte[] GetSlotOccupancyCopy() => (byte[])_slotOccupancy.Clone();

        /// <summary>Serializes the canonical little-endian fingerprint bytes.</summary>
        public byte[] Serialize()
        {
            var writer = new SnapshotBlockWriter(128);
            writer.WriteUInt16(StateSchemaVersion);
            writer.WriteUInt16(CommandSchemaVersion);
            writer.WriteUInt16(PayloadSchemaVersion);
            writer.WriteUInt16(SnapshotSchemaVersion);
            writer.WriteUInt16(SidecarSchemaVersion);
            writer.WriteLengthPrefixed(Encoding.ASCII.GetBytes(NumericModelId));
            writer.WriteUInt16(TicksPerSecond);
            writer.WriteLengthPrefixed(Encoding.ASCII.GetBytes(PrngId));
            writer.WriteUInt64(RulesHash64);
            writer.WriteUInt64(DefinitionsHash64);
            writer.WriteUInt64(MapHash64);
            writer.WriteBytes(_slotOccupancy);
            writer.WriteUInt64(StartSeed);
            writer.WriteUInt64(InitialStateHash);
            writer.WriteUInt32(InputDelayTicks);
            return writer.ToArray();
        }

        /// <summary>
        /// Parses one fingerprint from <paramref name="bytes"/>. Every length
        /// is validated before any allocation; identifier strings must be
        /// printable ASCII; trailing bytes are rejected. Malformed input
        /// returns false and never throws.
        /// </summary>
        public static bool TryParse(ReadOnlySpan<byte> bytes, out MatchFingerprint fingerprint)
        {
            fingerprint = null;
            var reader = new SnapshotBlockReader(bytes);

            if (!reader.TryReadUInt16(out ushort stateVersion)) return false;
            if (!reader.TryReadUInt16(out ushort commandVersion)) return false;
            if (!reader.TryReadUInt16(out ushort payloadVersion)) return false;
            if (!reader.TryReadUInt16(out ushort snapshotVersion)) return false;
            if (!reader.TryReadUInt16(out ushort sidecarVersion)) return false;
            if (!TryReadIdentifier(ref reader, out string numericModelId)) return false;
            if (!reader.TryReadUInt16(out ushort ticksPerSecond)) return false;
            if (!TryReadIdentifier(ref reader, out string prngId)) return false;
            if (!reader.TryReadUInt64(out ulong rulesHash)) return false;
            if (!reader.TryReadUInt64(out ulong definitionsHash)) return false;
            if (!reader.TryReadUInt64(out ulong mapHash)) return false;
            if (!reader.TryReadBytes(CommandLimits.ReservedPlayerSlots, out ReadOnlySpan<byte> slots)) return false;
            if (!reader.TryReadUInt64(out ulong startSeed)) return false;
            if (!reader.TryReadUInt64(out ulong initialStateHash)) return false;
            if (!reader.TryReadUInt32(out uint inputDelayTicks)) return false;
            if (reader.Remaining != 0) return false;

            try
            {
                fingerprint = new MatchFingerprint(
                    stateVersion, commandVersion, payloadVersion, snapshotVersion, sidecarVersion,
                    numericModelId, ticksPerSecond, prngId,
                    rulesHash, definitionsHash, mapHash,
                    slots.ToArray(), startSeed, initialStateHash, inputDelayTicks);
            }
            catch (ArgumentException)
            {
                return false; // undefined slot occupancy value
            }
            return true;
        }

        /// <summary>
        /// Canonical fingerprint hash: XXH64 seed 0 in the
        /// NOVA_DEFINITIONS_V1 domain (domain choice documented on the type)
        /// over tagged fields in the exact serialization order.
        /// </summary>
        public ulong ComputeHash()
        {
            var hash = SimHashWriter.ForDefinitions();
            hash.WriteFieldTag(1);
            hash.WriteUInt16(StateSchemaVersion);
            hash.WriteFieldTag(2);
            hash.WriteUInt16(CommandSchemaVersion);
            hash.WriteFieldTag(3);
            hash.WriteUInt16(PayloadSchemaVersion);
            hash.WriteFieldTag(4);
            hash.WriteUInt16(SnapshotSchemaVersion);
            hash.WriteFieldTag(5);
            hash.WriteUInt16(SidecarSchemaVersion);
            hash.WriteFieldTag(6);
            hash.WriteLengthPrefixed(Encoding.ASCII.GetBytes(NumericModelId));
            hash.WriteFieldTag(7);
            hash.WriteUInt16(TicksPerSecond);
            hash.WriteFieldTag(8);
            hash.WriteLengthPrefixed(Encoding.ASCII.GetBytes(PrngId));
            hash.WriteFieldTag(9);
            hash.WriteUInt64(RulesHash64);
            hash.WriteFieldTag(10);
            hash.WriteUInt64(DefinitionsHash64);
            hash.WriteFieldTag(11);
            hash.WriteUInt64(MapHash64);
            hash.WriteFieldTag(12);
            hash.WriteBytes(_slotOccupancy);
            hash.WriteFieldTag(13);
            hash.WriteUInt64(StartSeed);
            hash.WriteFieldTag(14);
            hash.WriteUInt64(InitialStateHash);
            hash.WriteFieldTag(15);
            hash.WriteUInt32(InputDelayTicks);
            return hash.Digest();
        }

        /// <summary>Value equality over every bound field (SimulationCore.md section 6).</summary>
        public bool Equals(MatchFingerprint other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return StateSchemaVersion == other.StateSchemaVersion
                && CommandSchemaVersion == other.CommandSchemaVersion
                && PayloadSchemaVersion == other.PayloadSchemaVersion
                && SnapshotSchemaVersion == other.SnapshotSchemaVersion
                && SidecarSchemaVersion == other.SidecarSchemaVersion
                && NumericModelId == other.NumericModelId
                && TicksPerSecond == other.TicksPerSecond
                && PrngId == other.PrngId
                && RulesHash64 == other.RulesHash64
                && DefinitionsHash64 == other.DefinitionsHash64
                && MapHash64 == other.MapHash64
                && _slotOccupancy.AsSpan().SequenceEqual(other._slotOccupancy)
                && StartSeed == other.StartSeed
                && InitialStateHash == other.InitialStateHash
                && InputDelayTicks == other.InputDelayTicks;
        }

        public override bool Equals(object obj) => Equals(obj as MatchFingerprint);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = StateSchemaVersion;
                hash = (hash * 397) ^ CommandSchemaVersion;
                hash = (hash * 397) ^ PayloadSchemaVersion;
                hash = (hash * 397) ^ SnapshotSchemaVersion;
                hash = (hash * 397) ^ SidecarSchemaVersion;
                hash = (hash * 397) ^ TicksPerSecond;
                hash = (hash * 397) ^ (int)RulesHash64;
                hash = (hash * 397) ^ (int)DefinitionsHash64;
                hash = (hash * 397) ^ (int)MapHash64;
                hash = (hash * 397) ^ (int)StartSeed;
                hash = (hash * 397) ^ (int)InitialStateHash;
                hash = (hash * 397) ^ (int)InputDelayTicks;
                for (int i = 0; i < _slotOccupancy.Length; i++)
                {
                    hash = (hash * 31) ^ _slotOccupancy[i];
                }
                return hash;
            }
        }

        /// <summary>
        /// Names the first field that differs from <paramref name="other"/>,
        /// or null when the fingerprints are equal. Diagnostic for replay
        /// start refusals (SimulationCore.md section 6: any divergence
        /// refuses the start); not part of the canonical contract.
        /// </summary>
        public string FindFirstDifference(MatchFingerprint other)
        {
            if (other == null) return "other fingerprint is null";
            if (StateSchemaVersion != other.StateSchemaVersion) return "StateSchemaVersion";
            if (CommandSchemaVersion != other.CommandSchemaVersion) return "CommandSchemaVersion";
            if (PayloadSchemaVersion != other.PayloadSchemaVersion) return "PayloadSchemaVersion";
            if (SnapshotSchemaVersion != other.SnapshotSchemaVersion) return "SnapshotSchemaVersion";
            if (SidecarSchemaVersion != other.SidecarSchemaVersion) return "SidecarSchemaVersion";
            if (NumericModelId != other.NumericModelId) return "NumericModelId";
            if (TicksPerSecond != other.TicksPerSecond) return "TicksPerSecond";
            if (PrngId != other.PrngId) return "PrngId";
            if (RulesHash64 != other.RulesHash64) return "RulesHash64";
            if (DefinitionsHash64 != other.DefinitionsHash64) return "DefinitionsHash64";
            if (MapHash64 != other.MapHash64) return "MapHash64";
            for (int i = 0; i < _slotOccupancy.Length; i++)
            {
                if (_slotOccupancy[i] != other._slotOccupancy[i]) return $"SlotOccupancy[{i}]";
            }
            if (StartSeed != other.StartSeed) return "StartSeed";
            if (InitialStateHash != other.InitialStateHash) return "InitialStateHash";
            if (InputDelayTicks != other.InputDelayTicks) return "InputDelayTicks";
            return null;
        }

        private static bool TryReadIdentifier(ref SnapshotBlockReader reader, out string value)
        {
            value = null;
            if (!reader.TryReadUInt32(out uint length)) return false;
            if (length == 0 || length > MaxIdentifierBytes) return false;
            if (!reader.TryReadBytes((int)length, out ReadOnlySpan<byte> raw)) return false;
            for (int i = 0; i < raw.Length; i++)
            {
                if (raw[i] < 0x20 || raw[i] > 0x7E) return false; // printable ASCII only
            }
            value = Encoding.ASCII.GetString(raw);
            return true;
        }

        private static void ValidateIdentifier(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Identifier must be non-empty.", paramName);
            if (Encoding.ASCII.GetByteCount(value) != value.Length || value.Length > MaxIdentifierBytes)
            {
                throw new ArgumentException("Identifier must be printable ASCII within the length bound.", paramName);
            }
        }
    }
}
