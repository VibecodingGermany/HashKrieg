namespace Nova.Gameplay
{
    /// <summary>
    /// The crystal-stage rule of an Aetherium field marker (21.2, #86): how
    /// many of a cluster's shards stay visible at a given remaining reserve.
    /// Pure presentation math over read-only economy values — no simulation
    /// contact, no UnityEngine dependency, so EditMode tests cover the whole
    /// staging.
    /// <para>
    /// STAGING RULE: the visible shard count is the reserve fraction mapped
    /// onto the cluster, rounded UP (ceiling). A full field shows every
    /// shard; any reserve above zero keeps at least one shard, so a field
    /// being worked never reads as empty; exactly 0 AE
    /// (<c>AetheriumField.IsExhausted</c>) shows none. The exhausted LOOK —
    /// one flattened, darkened stump instead of bare ground — is the view's
    /// own dressing on top of stage 0 and not this function's concern. The
    /// result is clamped into [0, shardCount], so an over-reserve reading
    /// (or an unknown initial reserve) can never light more shards than the
    /// cluster has.
    /// </para>
    /// </summary>
    public static class FieldCrystalStages
    {
        /// <summary>
        /// Shards lit at <paramref name="remainingAE"/> out of
        /// <paramref name="initialReserveAE"/> for a cluster of
        /// <paramref name="shardCount"/>: ceiling of the reserve fraction
        /// times the shard count (see the class remarks for the exact
        /// staging rule).
        /// </summary>
        public static int VisibleShards(long remainingAE, long initialReserveAE, int shardCount)
        {
            if (shardCount <= 0) return 0;
            if (remainingAE <= 0 || initialReserveAE <= 0) return 0;
            if (remainingAE >= initialReserveAE) return shardCount;

            long visible = (remainingAE * shardCount + initialReserveAE - 1) / initialReserveAE;
            if (visible < 1) return 1;
            return visible > shardCount ? shardCount : (int)visible;
        }
    }
}
