namespace Nova.Simulation.Snapshots
{
    /// <summary>
    /// Registry of snapshot block ids used by the kernel-level snapshot
    /// integration (docs/tech/SimulationCore.md section 7). The kernel owns
    /// id 1; system state blocks (<see cref="IStatefulSimSystem"/>) are
    /// assigned from 100 upward so kernel-reserved low ids and system blocks
    /// can never collide. This assignment is provisional: the final block
    /// registry is a Q-040 item and must be ratified by D-ID before the G1
    /// schema freeze.
    /// </summary>
    public static class SnapshotBlockIds
    {
        /// <summary>Kernel block: tick, PRNG words, ingress/dedupe state, pending batches.</summary>
        public const ushort Kernel = 1;

        /// <summary>First block id available to stateful simulation systems.</summary>
        public const ushort FirstSystemBlock = 100;

        /// <summary>Entity store block (units, generations, free list), written by MovementSystem.</summary>
        public const ushort EntityStore = FirstSystemBlock;

        /// <summary>
        /// Pathfinding block: cost field epoch, recency clock and the
        /// flow-field cache directory (destination + last-used tick per
        /// entry). The field contents themselves are rebuilt on restore; the
        /// epoch is the proof that the rebuild sees the same terrain.
        /// </summary>
        public const ushort Pathfinding = FirstSystemBlock + 1;

        /// <summary>Fog of War block (committed per-team masks, last recompute tick), written by FogOfWarSystem.</summary>
        public const ushort FogOfWar = FirstSystemBlock + 2;

        /// <summary>Reserved for the G2 Aetherium domain block (D-010 loop); intentionally unassigned in this slice.</summary>
        public const ushort ReservedAetherium = FirstSystemBlock + 3;

        /// <summary>Economy block (per-slot credits/power, Aetherium fields), written by EconomySystem.</summary>
        public const ushort Economy = FirstSystemBlock + 4;

        /// <summary>Construction block (sites, finished placements, repair orders, T2 unlocks), written by ConstructionSystem.</summary>
        public const ushort Construction = FirstSystemBlock + 5;

        /// <summary>Production block (per-producer queues and rally points), written by ProductionSystem.</summary>
        public const ushort Production = FirstSystemBlock + 6;
    }
}
