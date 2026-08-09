namespace Nova.AI.Data
{
    /// <summary>
    /// The profiles the game ships with.
    /// <para>
    /// A CODE TABLE, not a JSON file loaded at runtime — the same shape
    /// <c>SimDefinitions</c> already uses for the definition table, and for the
    /// same reasons: <c>static readonly</c> data is thread-safe (so N matches
    /// run on N cores without locks), it cannot fail to parse mid-match, and it
    /// raises no question about what a missing file would mean for the match
    /// fingerprint. The plan sketches profiles as JSON; that shape is the right
    /// one for LAB profiles, which are read by the lab and handed in as values.
    /// A shipped profile is part of the build.
    /// </para>
    /// </summary>
    public static class AiProfiles
    {
        /// <summary>
        /// What MatchRunner ships today, value for value.
        /// <para>
        /// These eight numbers are the whole point of E6 being
        /// behaviour-neutral: four came from <c>MatchRunner</c>'s
        /// <c>AiFactionProfile</c> call (power margin 0, army 12, squad
        /// threshold 6, harvesters 2) and four from <c>const</c> fields inside
        /// <c>SkirmishAiSystem</c> (cadence 20, placement radius 8, both queue
        /// batches 2). Nothing was rounded, retuned or "improved" on the way
        /// across. If any of them changes, the four baseline files go red — and
        /// that is the check, not an accident.
        /// </para>
        /// </summary>
        public static readonly AiProfile Ms1Canonical = new AiProfile(
            profileId: "ms1-canonical",
            decisionTickInterval: 20,
            placementSearchRadius: 8,
            powerReserve: 0,
            targetHarvesters: 2,
            harvesterQueueBatch: 2,
            targetArmySize: 12,
            attackSquadThreshold: 6,
            infantryQueueBatch: 2);

        /// <summary>
        /// The old <c>AiFactionProfile</c> constructor defaults (power margin
        /// 30, army 15, squad threshold 8, harvesters 2), kept so the
        /// parameterless path behaves as it did. Nothing in the game selects
        /// it: every caller passes its numbers explicitly, which is exactly why
        /// the defaults could drift unnoticed from what actually ships.
        /// </summary>
        public static readonly AiProfile LegacyDefaults = new AiProfile(
            profileId: "legacy-defaults",
            decisionTickInterval: 20,
            placementSearchRadius: 8,
            powerReserve: 30,
            targetHarvesters: 2,
            harvesterQueueBatch: 2,
            targetArmySize: 15,
            attackSquadThreshold: 8,
            infantryQueueBatch: 2);
    }
}
