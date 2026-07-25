using System;
using System.Collections.Generic;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Immutable, validated and sealed set of command records for one target
    /// tick — the only command object the kernel may accept
    /// (docs/tech/Commands.md sections 1 and 3). Records are sorted by
    /// (TargetTick, PlayerSlot, Sequence). The constructor is internal: only
    /// <see cref="CommandIngress"/> can seal a batch, so host code outside the
    /// simulation assembly cannot fabricate one.
    /// </summary>
    public sealed class CommandBatch
    {
        private readonly CommandRecord[] _records;

        internal CommandBatch(uint targetTick, CommandRecord[] sortedRecords)
        {
            TargetTick = targetTick;
            _records = sortedRecords;
        }

        /// <summary>The tick this batch applies to.</summary>
        public uint TargetTick { get; }

        /// <summary>Records in canonical (TargetTick, PlayerSlot, Sequence) order.</summary>
        public IReadOnlyList<CommandRecord> Records => _records;

        /// <summary>Number of records; never exceeds <see cref="CommandLimits.MaxBatchRecordsPerTick"/>.</summary>
        public int Count => _records.Length;

        /// <summary>Canonical byte stream of the batch: the record bytes in batch order.</summary>
        public byte[] Serialize()
        {
            int total = 0;
            for (int i = 0; i < _records.Length; i++)
            {
                total += _records[i].RecordBytes;
            }
            var bytes = new byte[total];
            int offset = 0;
            for (int i = 0; i < _records.Length; i++)
            {
                byte[] recordBytes = _records[i].Serialize();
                Array.Copy(recordBytes, 0, bytes, offset, recordBytes.Length);
                offset += recordBytes.Length;
            }
            return bytes;
        }

        /// <summary>
        /// Canonical (TargetTick, PlayerSlot, Sequence) comparison used for
        /// batch ordering (docs/tech/Commands.md section 3).
        /// </summary>
        internal static int CompareRecords(CommandRecord left, CommandRecord right)
        {
            int byTick = left.TargetTick.CompareTo(right.TargetTick);
            if (byTick != 0) return byTick;
            int bySlot = left.PlayerSlot.CompareTo(right.PlayerSlot);
            if (bySlot != 0) return bySlot;
            return left.Sequence.CompareTo(right.Sequence);
        }
    }
}
