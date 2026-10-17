using System;
using System.Collections.Generic;
using System.Text;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
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
    /// Stub selector for deterministic stand-in content hashes. Rules now use
    /// <see cref="MatchFingerprint.ComputeCurrentRulesHash64"/> in current
    /// fingerprints; the Rules selector remains only to identify/refuse the
    /// legacy empty stub and for compatibility tests.
    /// </summary>
    public enum MatchContentStub : uint
    {
        /// <summary>Legacy empty stub for RulesHash64; not used by current hosts.</summary>
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
    /// reserved slots with their occupancies AND their factions — the faction
    /// assignment selects the definition row behind every faction-specific
    /// value, so two matches with different slot factions are different
    /// content and must never share a fingerprint), the input delay
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
    /// SlotFaction 8 x u8 (0=Alliance, 1=Legion) |
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

        /// <summary>
        /// First non-stub deterministic rules revision; binds the D-106
        /// storage-cap behavior.
        /// </summary>
        public const ushort RulesRevisionV1 = 1;

        /// <summary>
        /// Sprint-16.6 C4 low-power radar and repair behavior layered onto
        /// revision 1. Historical: frozen as a literal row (#138, D-120).
        /// </summary>
        public const ushort RulesRevisionV2 = 2;

        /// <summary>
        /// D-104 deterministic placement geometry and paid, single-winner
        /// repair behavior layered onto revision 2.
        /// </summary>
        public const ushort RulesRevisionV3 = 3;

        /// <summary>
        /// The rules revision emitted by current hosts. Revisions are append
        /// only (#138, D-120): changing a hashed rules constant means
        /// appending the next revision with its own frozen literal row, never
        /// editing an existing row or re-pinning its golden hash.
        /// </summary>
        public const ushort CurrentRulesRevision = RulesRevisionV3;

        /// <summary>Parser bound for one identifier string; checked before allocation.</summary>
        public const int MaxIdentifierBytes = 64;

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
        private readonly byte[] _slotFactions;

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
            byte[] slotFactions,
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
            if (slotFactions == null) throw new ArgumentNullException(nameof(slotFactions));
            if (slotFactions.Length != CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentException(
                    $"Exactly {CommandLimits.ReservedPlayerSlots} slot factions are required.",
                    nameof(slotFactions));
            }
            for (int i = 0; i < slotFactions.Length; i++)
            {
                if (slotFactions[i] > (byte)State.FactionId.Legion)
                {
                    throw new ArgumentException(
                        $"Slot {i} faction {slotFactions[i]} is not a defined value.",
                        nameof(slotFactions));
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
            _slotFactions = (byte[])slotFactions.Clone();
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
            byte[] slotFactions,
            ulong startSeed,
            ulong initialStateHash,
            uint inputDelayTicks)
        {
            return new MatchFingerprint(
                StateSchemaVersionV1, CommandSchemaVersionV1, PayloadSchemaVersionV1,
                SnapshotSchemaVersionV1, SidecarSchemaVersionV1,
                NumericModelIdV1, TicksPerSecondV1, PrngIdV1,
                rulesHash64, definitionsHash64, mapHash64,
                slotOccupancy, slotFactions, startSeed, initialStateHash, inputDelayTicks);
        }

        /// <summary>
        /// Deterministic empty stand-in content hash for legacy/test inputs and
        /// content domains without a canonical source yet: XXH64 seed 0
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

        /// <summary>
        /// Canonical rules identity of one supported simulation revision, read
        /// exclusively from that revision's frozen literal row (#138, D-120).
        /// A row is written once, when its revision is appended, and never
        /// edited, so a later change to a live rules constant cannot move a
        /// historical identity: replay and relay tests represent prior rules
        /// exactly. Hosts must use <see cref="ComputeCurrentRulesHash64"/>.
        /// Unknown revisions are rejected rather than guessed.
        /// <para>
        /// Known, accepted gap (D-120): #131 raised HqBaseCapacityAE from
        /// 2,000 to 3,000 without a revision bump, so builds from 2026-08-10
        /// to 2026-08-31 emitted revision 3 as 0x05CCA8475789AD4A. The frozen
        /// revision-3 row carries the post-#131 values (0xD1B683837D12FCEF);
        /// the earlier revision-3 identity is not representable.
        /// </para>
        /// </summary>
        public static ulong ComputeRulesHash64(ushort rulesRevision)
        {
            return HashRulesRow(rulesRevision, GetFrozenRulesRow(rulesRevision));
        }

        /// <summary>
        /// Canonical rules identity for the current simulation: the layout of
        /// <see cref="CurrentRulesRevision"/>, written from the LIVE rules
        /// constants. Revision 3 retains D-106 and Sprint-16.6 C4 from
        /// revisions 1/2, then binds the D-104 footprint, influence,
        /// field-spacing, repair-price and single-winner behavior that can
        /// diverge without changing snapshot bytes or definition rows. Old/new
        /// peers and replays therefore fail the exact-fingerprint gate before
        /// executing tick 1. This is the only rules-hash path that reads live
        /// constants (#138, D-120); <see cref="DescribeCurrentRulesDrift"/>
        /// is the tripwire that keeps it equal to the frozen row of the
        /// current revision.
        /// </summary>
        public static ulong ComputeCurrentRulesHash64()
        {
            return HashRulesRow(CurrentRulesRevision, BuildLiveCurrentRulesRow());
        }

        /// <summary>
        /// Tripwire for the append-only revision rule (#138, D-120): compares
        /// the live rules constants field by field (tag, width, value) with
        /// the frozen literal row of <see cref="CurrentRulesRevision"/> and
        /// returns one line per difference; an empty array means they agree.
        /// A non-empty result means a hashed rules constant changed under an
        /// existing revision number. The remedy is to append the next rules
        /// revision with its own literal row, never to edit a frozen row or
        /// re-pin a golden hash.
        /// </summary>
        public static string[] DescribeCurrentRulesDrift()
        {
            RulesField[] frozen = GetFrozenRulesRow(CurrentRulesRevision);
            RulesField[] live = BuildLiveCurrentRulesRow();
            var drift = new List<string>();
            if (frozen.Length != live.Length)
            {
                drift.Add($"revision {CurrentRulesRevision}: the frozen row has {frozen.Length} fields, "
                    + $"the live layout has {live.Length}");
            }

            int shared = Math.Min(frozen.Length, live.Length);
            for (int i = 0; i < shared; i++)
            {
                RulesField frozenField = frozen[i];
                RulesField liveField = live[i];
                if (frozenField.Tag != liveField.Tag
                    || frozenField.IsInt64 != liveField.IsInt64
                    || frozenField.Value != liveField.Value)
                {
                    drift.Add($"revision {CurrentRulesRevision} field {frozenField.Tag} ({liveField.Name}): "
                        + $"frozen {frozenField.Describe()}, live {liveField.Describe()}");
                }
            }
            return drift.ToArray();
        }

        /// <summary>
        /// Writes one rules-identity stream. The byte layout is unchanged from
        /// the pre-#138 writer: stub tag 1, the u32 field count (field 1 plus
        /// the row), field 1 = the revision as u16, then every row field as
        /// its tag followed by an i64 or i32 value, in row order.
        /// </summary>
        private static ulong HashRulesRow(ushort rulesRevision, RulesField[] row)
        {
            var hash = SimHashWriter.ForDefinitions();
            hash.WriteFieldTag((uint)MatchContentStub.Rules);
            hash.WriteUInt32((uint)row.Length + 1u); // field 1 (the revision) plus the ordered rule fields
            hash.WriteFieldTag(1);
            hash.WriteUInt16(rulesRevision);
            for (int i = 0; i < row.Length; i++)
            {
                hash.WriteFieldTag(row[i].Tag);
                if (row[i].IsInt64)
                {
                    hash.WriteInt64(row[i].Value);
                }
                else
                {
                    hash.WriteInt32((int)row[i].Value);
                }
            }
            return hash.Digest();
        }

        /// <summary>
        /// The layout of <see cref="CurrentRulesRevision"/> read from the live
        /// rules constants — the only place the rules hash touches them
        /// (#138, D-120). Appending a revision extends this row with the live
        /// reads of the new fields in the same order as the new frozen row.
        /// </summary>
        private static RulesField[] BuildLiveCurrentRulesRow()
        {
            return new[]
            {
                RulesField.Int64(2, nameof(EconomySystem.HqBaseCapacityAE), EconomySystem.HqBaseCapacityAE),
                RulesField.Int64(3, nameof(EconomySystem.StorageCapacityBonusAE), EconomySystem.StorageCapacityBonusAE),
                RulesField.Int32(4, nameof(EconomySystem.ExcessDecayPercent), EconomySystem.ExcessDecayPercent),
                RulesField.Int32(5, nameof(EconomySystem.ExcessDecayIntervalTicks), EconomySystem.ExcessDecayIntervalTicks),
                RulesField.Int32(6, nameof(ConstructionSystem.RepairRateHpPerTick), ConstructionSystem.RepairRateHpPerTick),
                RulesField.Int32(7, nameof(ConstructionSystem.LowPowerRepairRateHpPerTick), ConstructionSystem.LowPowerRepairRateHpPerTick),
                RulesField.Int32(8, nameof(SimDefinitions.BuildingFootprintCells), SimDefinitions.BuildingFootprintCells),
                RulesField.Int32(9, nameof(ConstructionSystem.RepairCostPercent), ConstructionSystem.RepairCostPercent),
                RulesField.Int32(10, nameof(ConstructionSystem.BuildInfluenceRadiusCells), ConstructionSystem.BuildInfluenceRadiusCells),
                RulesField.Int32(11, nameof(ConstructionSystem.MinimumBuildingDistanceCells), ConstructionSystem.MinimumBuildingDistanceCells),
                RulesField.Int32(12, nameof(ConstructionSystem.RefineryMinimumFieldDistanceCells), ConstructionSystem.RefineryMinimumFieldDistanceCells),
                RulesField.Int32(13, nameof(ConstructionSystem.RefineryMaximumFieldDistanceCells), ConstructionSystem.RefineryMaximumFieldDistanceCells),
                RulesField.Int32(14, nameof(ConstructionSystem.MinimumNonRefineryFieldDistanceCells), ConstructionSystem.MinimumNonRefineryFieldDistanceCells),
            };
        }

        // <frozen-rules-rows>
        // Frozen literal rows, one per rules revision (#138, D-120). APPEND
        // ONLY: a row is written once, when its revision is introduced, and is
        // never edited afterwards. Every value is a literal, never a reference
        // to a live constant; the FrozenRevisions_ReadNoLiveConstants tests
        // check this region as source text. Field 1 (the revision number) is
        // written by HashRulesRow and is not part of a row.
        //
        // To append revision N: declare RulesRevisionVN; add FrozenRulesRowVN
        // = the previous row with today's values as literals plus the new
        // fields; add its case to GetFrozenRulesRow; extend
        // BuildLiveCurrentRulesRow with the live reads of the new fields; move
        // CurrentRulesRevision; pin the new golden hash next to the old ones.

        /// <summary>Revision 1 (e1970a9, D-106): HQ/storage capacity and excess decay.</summary>
        private static readonly RulesField[] FrozenRulesRowV1 =
        {
            RulesField.Int64(2, "HqBaseCapacityAE", 2000L),
            RulesField.Int64(3, "StorageCapacityBonusAE", 2000L),
            RulesField.Int32(4, "ExcessDecayPercent", 25),
            RulesField.Int32(5, "ExcessDecayIntervalTicks", 10),
        };

        /// <summary>Revision 2 (8512e9f, Sprint-16.6 C4): revision 1 plus repair rates.</summary>
        private static readonly RulesField[] FrozenRulesRowV2 =
        {
            RulesField.Int64(2, "HqBaseCapacityAE", 2000L),
            RulesField.Int64(3, "StorageCapacityBonusAE", 2000L),
            RulesField.Int32(4, "ExcessDecayPercent", 25),
            RulesField.Int32(5, "ExcessDecayIntervalTicks", 10),
            RulesField.Int32(6, "RepairRateHpPerTick", 10),
            RulesField.Int32(7, "LowPowerRepairRateHpPerTick", 5),
        };

        /// <summary>
        /// Revision 3 (4cdcf07, D-104), values as of e4faa75 (#131: HQ 3,000).
        /// The pre-#131 identity of revision 3 is the accepted D-120 gap.
        /// </summary>
        private static readonly RulesField[] FrozenRulesRowV3 =
        {
            RulesField.Int64(2, "HqBaseCapacityAE", 3000L),
            RulesField.Int64(3, "StorageCapacityBonusAE", 2000L),
            RulesField.Int32(4, "ExcessDecayPercent", 25),
            RulesField.Int32(5, "ExcessDecayIntervalTicks", 10),
            RulesField.Int32(6, "RepairRateHpPerTick", 10),
            RulesField.Int32(7, "LowPowerRepairRateHpPerTick", 5),
            RulesField.Int32(8, "BuildingFootprintCells", 3),
            RulesField.Int32(9, "RepairCostPercent", 30),
            RulesField.Int32(10, "BuildInfluenceRadiusCells", 8),
            RulesField.Int32(11, "MinimumBuildingDistanceCells", 2),
            RulesField.Int32(12, "RefineryMinimumFieldDistanceCells", 1),
            RulesField.Int32(13, "RefineryMaximumFieldDistanceCells", 3),
            RulesField.Int32(14, "MinimumNonRefineryFieldDistanceCells", 2),
        };

        private static RulesField[] GetFrozenRulesRow(ushort rulesRevision)
        {
            switch (rulesRevision)
            {
                case RulesRevisionV1: return FrozenRulesRowV1;
                case RulesRevisionV2: return FrozenRulesRowV2;
                case RulesRevisionV3: return FrozenRulesRowV3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rulesRevision), rulesRevision, "Unknown rules revision.");
            }
        }
        // </frozen-rules-rows>

        /// <summary>
        /// One tagged, typed value of a rules-identity row (#138, D-120). The
        /// name is diagnostic only and never hashed or compared, so renaming
        /// a constant does not trip the drift check.
        /// </summary>
        private readonly struct RulesField
        {
            private RulesField(uint tag, string name, bool isInt64, long value)
            {
                Tag = tag;
                Name = name;
                IsInt64 = isInt64;
                Value = value;
            }

            public uint Tag { get; }
            public string Name { get; }
            public bool IsInt64 { get; }
            public long Value { get; }

            public static RulesField Int64(uint tag, string name, long value) => new RulesField(tag, name, true, value);

            public static RulesField Int32(uint tag, string name, int value) => new RulesField(tag, name, false, value);

            public string Describe() => $"{Value} ({(IsInt64 ? "i64" : "i32")}, tag {Tag})";
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

        /// <summary>Faction of one reserved slot (index 0..7).</summary>
        public State.FactionId GetSlotFaction(int slot)
        {
            if (slot < 0 || slot >= CommandLimits.ReservedPlayerSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }
            return (State.FactionId)_slotFactions[slot];
        }

        /// <summary>A defensive copy of the eight slot factions.</summary>
        public byte[] GetSlotFactionCopy() => (byte[])_slotFactions.Clone();

        /// <summary>Serializes the canonical little-endian fingerprint bytes.</summary>
        public byte[] Serialize()
        {
            var writer = new SnapshotBlockWriter(136);
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
            writer.WriteBytes(_slotFactions);
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
            if (!reader.TryReadBytes(CommandLimits.ReservedPlayerSlots, out ReadOnlySpan<byte> factions)) return false;
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
                    slots.ToArray(), factions.ToArray(), startSeed, initialStateHash, inputDelayTicks);
            }
            catch (ArgumentException)
            {
                return false; // undefined slot occupancy or faction value
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
            hash.WriteBytes(_slotFactions);
            hash.WriteFieldTag(14);
            hash.WriteUInt64(StartSeed);
            hash.WriteFieldTag(15);
            hash.WriteUInt64(InitialStateHash);
            hash.WriteFieldTag(16);
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
                && _slotFactions.AsSpan().SequenceEqual(other._slotFactions)
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
                for (int i = 0; i < _slotFactions.Length; i++)
                {
                    hash = (hash * 31) ^ _slotFactions[i];
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
            for (int i = 0; i < _slotFactions.Length; i++)
            {
                if (_slotFactions[i] != other._slotFactions[i]) return $"SlotFaction[{i}]";
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
