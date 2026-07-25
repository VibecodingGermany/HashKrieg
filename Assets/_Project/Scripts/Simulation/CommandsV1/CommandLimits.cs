namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Binding capacity limits of the canonical command contract v1
    /// (docs/tech/Commands.md section 2, docs/tech/SimulationCore.md section 10).
    /// All limits are structural: violations reject a command before it enters
    /// the canonical stream and are never recorded.
    /// </summary>
    public static class CommandLimits
    {
        /// <summary>Fixed record header size in bytes (Commands.md section 2).</summary>
        public const int HeaderBytes = 20;

        /// <summary>Maximum bytes of one whole record, header included.</summary>
        public const int MaxRecordBytes = 4096;

        /// <summary>Maximum payload bytes of one record (MaxRecordBytes - HeaderBytes).</summary>
        public const int MaxPayloadBytes = 4076;

        /// <summary>Maximum command records sealed into one tick batch.</summary>
        public const int MaxBatchRecordsPerTick = 256;

        /// <summary>Maximum pending (accepted, not yet sealed) records in the ingress queue.</summary>
        public const int MaxPendingRecords = 1024;

        /// <summary>Maximum entity ids carried by a single command payload.</summary>
        public const int MaxEntityIdsPerCommand = 100;

        /// <summary>Reserved player slots per match (SimulationCore.md section 10).</summary>
        public const int ReservedPlayerSlots = 8;

        /// <summary>Only payload version of schema v1.</summary>
        public const byte PayloadVersionV1 = 1;
    }
}
