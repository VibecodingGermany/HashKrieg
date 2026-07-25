using System;
using System.Collections.Generic;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Authoritative sequence, dedupe and conflict state of the ingress
    /// (docs/tech/Commands.md section 3). Tracked per player slot over the full
    /// reserved slot range; serialized into snapshots so a restored session
    /// keeps identical acceptance behaviour (SimulationCore.md section 3).
    /// <para>
    /// Per slot the state holds: the next local sequence to assign (starts at
    /// 1; 0 and uint32 overflow are structural errors and the session never
    /// reuses a sequence), the watermark of the highest sealed sequence, and
    /// the pending (accepted, not yet sealed) records keyed by sequence for
    /// byte-exact dedupe and deterministic conflict detection.
    /// </para>
    /// </summary>
    public sealed class CommandDedupeState
    {
        /// <summary>Serialization version of this state block.</summary>
        public const byte StateVersion = 1;

        private readonly uint[] _nextLocalSequence;
        private readonly uint[] _sealedWatermark;
        private readonly SortedDictionary<uint, CommandRecord>[] _pending;

        public CommandDedupeState()
        {
            _nextLocalSequence = new uint[CommandLimits.ReservedPlayerSlots];
            _sealedWatermark = new uint[CommandLimits.ReservedPlayerSlots];
            _pending = new SortedDictionary<uint, CommandRecord>[CommandLimits.ReservedPlayerSlots];
            for (int i = 0; i < CommandLimits.ReservedPlayerSlots; i++)
            {
                _nextLocalSequence[i] = 1;
                _sealedWatermark[i] = 0;
                _pending[i] = new SortedDictionary<uint, CommandRecord>();
            }
        }

        /// <summary>Total number of pending records across all slots.</summary>
        public int PendingCount
        {
            get
            {
                int total = 0;
                for (int i = 0; i < CommandLimits.ReservedPlayerSlots; i++)
                {
                    total += _pending[i].Count;
                }
                return total;
            }
        }

        /// <summary>Next sequence the local slot will assign; 1 before the first command.</summary>
        public uint NextLocalSequence(byte playerSlot) => _nextLocalSequence[playerSlot];

        /// <summary>Highest sealed sequence of a slot; 0 before the first sealed record.</summary>
        public uint SealedWatermark(byte playerSlot) => _sealedWatermark[playerSlot];

        /// <summary>
        /// Assigns the next local sequence. Returns false on uint32 overflow:
        /// the session is then not continued with a reused sequence
        /// (Commands.md section 1).
        /// </summary>
        public bool TryAssignLocalSequence(byte playerSlot, out uint sequence)
        {
            sequence = _nextLocalSequence[playerSlot];
            if (sequence == 0) return false; // overflowed earlier; never wrap into reuse
            if (sequence == uint.MaxValue)
            {
                _nextLocalSequence[playerSlot] = 0; // mark overflow for the next call
            }
            else
            {
                _nextLocalSequence[playerSlot] = sequence + 1;
            }
            return true;
        }

        /// <summary>True when the sequence is already accepted and still pending.</summary>
        public bool IsPending(byte playerSlot, uint sequence)
        {
            return _pending[playerSlot].ContainsKey(sequence);
        }

        /// <summary>True when the sequence is at or below the sealed watermark.</summary>
        public bool IsCompleted(byte playerSlot, uint sequence)
        {
            return sequence <= _sealedWatermark[playerSlot];
        }

        /// <summary>The pending record for a sequence; default when absent.</summary>
        public bool TryGetPending(byte playerSlot, uint sequence, out CommandRecord record)
        {
            return _pending[playerSlot].TryGetValue(sequence, out record);
        }

        /// <summary>Adds an accepted record to the pending set.</summary>
        public void AddPending(in CommandRecord record)
        {
            _pending[record.PlayerSlot].Add(record.Sequence, record);
        }

        /// <summary>Number of pending records targeting the given tick (backpressure).</summary>
        public int PendingCountForTick(uint targetTick)
        {
            int total = 0;
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                foreach (KeyValuePair<uint, CommandRecord> entry in _pending[slot])
                {
                    if (entry.Value.TargetTick == targetTick) total++;
                }
            }
            return total;
        }

        /// <summary>
        /// Removes and returns all pending records with the given target tick,
        /// advancing each slot's sealed watermark to the highest sealed
        /// sequence. The transport delivers sequences reliably and in order per
        /// player, so a sequence below the watermark never legitimately
        /// re-enters as new; completed sequences cannot bypass dedupe.
        /// </summary>
        public List<CommandRecord> DrainPendingForTick(uint targetTick)
        {
            var drained = new List<CommandRecord>();
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                var pending = _pending[slot];
                if (pending.Count == 0) continue;
                var toRemove = new List<uint>();
                uint highestSealed = _sealedWatermark[slot];
                foreach (KeyValuePair<uint, CommandRecord> entry in pending)
                {
                    if (entry.Value.TargetTick == targetTick)
                    {
                        drained.Add(entry.Value);
                        toRemove.Add(entry.Key);
                        if (entry.Key > highestSealed) highestSealed = entry.Key;
                    }
                }
                for (int i = 0; i < toRemove.Count; i++)
                {
                    pending.Remove(toRemove[i]);
                }
                _sealedWatermark[slot] = highestSealed;
            }
            return drained;
        }

        /// <summary>
        /// Serializes the complete state deterministically: version byte, then
        /// per reserved slot (ascending) next-sequence, watermark and the
        /// pending records in ascending sequence order as length-prefixed
        /// canonical record bytes.
        /// </summary>
        public byte[] Serialize()
        {
            // The state can exceed a single payload's byte cap, so it is
            // written into a plain growing buffer in the same canonical
            // little-endian encoding instead of the payload-capped writer.
            var bytes = new List<byte>(256);
            bytes.Add(StateVersion);
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                WriteUInt32(bytes, _nextLocalSequence[slot]);
                WriteUInt32(bytes, _sealedWatermark[slot]);
                var pending = _pending[slot];
                WriteUInt16(bytes, unchecked((ushort)pending.Count));
                foreach (KeyValuePair<uint, CommandRecord> entry in pending)
                {
                    byte[] recordBytes = entry.Value.Serialize();
                    WriteUInt16(bytes, unchecked((ushort)recordBytes.Length));
                    bytes.AddRange(recordBytes);
                }
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// Restores a state previously produced by <see cref="Serialize"/>.
        /// Every length is checked before allocation and every pending record
        /// is revalidated against the structural base rules of
        /// docs/tech/Commands.md section 4 (full parse, slot/block consistency,
        /// stream kind — never a session action, sequence ≠ 0, canonical
        /// payload, unique pending key). Malformed or manipulated input returns
        /// false without mutating anything; it never throws.
        /// </summary>
        public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out CommandDedupeState state)
        {
            state = null;
            var reader = new CommandPayloadReader(bytes);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;

            var restored = new CommandDedupeState();
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                if (!reader.TryReadUInt32(out uint nextSequence)) return false;
                if (!reader.TryReadUInt32(out uint watermark)) return false;
                if (!reader.TryReadUInt16(out ushort pendingCount)) return false;
                restored._nextLocalSequence[slot] = nextSequence;
                restored._sealedWatermark[slot] = watermark;
                for (int i = 0; i < pendingCount; i++)
                {
                    if (!reader.TryReadUInt16(out ushort recordLength)) return false;
                    if (recordLength > CommandLimits.MaxRecordBytes) return false;
                    if (!reader.TryReadBytes(recordLength, out ReadOnlySpan<byte> recordBytes)) return false;
                    if (!CommandRecord.TryDeserialize(recordBytes, out CommandRecord record, out int consumed)) return false;
                    if (consumed != recordLength) return false;

                    // Structural revalidation of snapshot content: a pending
                    // record must belong to this slot's block, carry a usable
                    // sequence and pass the same section-4 payload rules a live
                    // record would.
                    if (record.PlayerSlot != (byte)slot) return false;
                    if (record.Sequence == 0) return false;
                    if (!CommandPayloadValidation.TryValidateStreamPayload(
                            record.Kind, record.PayloadVersion, record.Payload.Span, out _)) return false;
                    if (restored._pending[slot].ContainsKey(record.Sequence)) return false;
                    restored._pending[slot].Add(record.Sequence, record);
                }
            }
            if (reader.Remaining != 0) return false;
            state = restored;
            return true;
        }

        /// <summary>
        /// True when every pending record's player slot satisfies
        /// <paramref name="isActiveSlot"/>. Used by the ingress on snapshot
        /// restore: slot activity is session state and cannot be checked here.
        /// </summary>
        internal bool AllPendingSlotsAre(System.Func<byte, bool> isActiveSlot)
        {
            for (int slot = 0; slot < CommandLimits.ReservedPlayerSlots; slot++)
            {
                foreach (KeyValuePair<uint, CommandRecord> entry in _pending[slot])
                {
                    if (!isActiveSlot(entry.Value.PlayerSlot)) return false;
                }
            }
            return true;
        }

        private static void WriteUInt16(List<byte> bytes, ushort value)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
        }

        private static void WriteUInt32(List<byte> bytes, uint value)
        {
            bytes.Add((byte)value);
            bytes.Add((byte)(value >> 8));
            bytes.Add((byte)(value >> 16));
            bytes.Add((byte)(value >> 24));
        }
    }
}
