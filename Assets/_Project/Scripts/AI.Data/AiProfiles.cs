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
            infantryQueueBatch: 2,
            // Target scoring (E7). These four are the first numbers here that
            // are NOT a copy of a shipped constant — there was no scoring
            // before, only "HQ, else the first visible building, else the
            // first visible unit". They come from the plan's sketch
            // (AiSimulationEnvironment.md section 4.6) and the four
            // determinism baselines go red because of them; that is the
            // change, not a defect.
            targetDamageWeight: 10,
            targetThreatWeight: 6,
            targetFinishWeight: 3,
            targetDistanceWeight: 4,
            // Waves. 12 IS the army cap above, and that is the rule in one
            // number: attack at full strength, never reinforce piecemeal. A
            // reinforcement waits at the staging cell until the wave is full
            // again — which, with the cap at 12, means until the previous wave
            // is gone. Units already out are never called back.
            //
            // Measured one-sided against waveSize 1 over five sizes (4, 6, 8,
            // 10, 12) and both faction seatings; every column improves
            // monotonically with the size, which is why the value sits at the
            // cap and not somewhere in the middle. waveSize 1 turns the rule
            // off and reproduces the previous behaviour exactly — the lab
            // keeps that as the candidate `wave-off`, because a behaviour
            // without an off setting cannot be measured one-sided (M001).
            waveSize: 12,
            stagingDistanceCells: 12,
            stagingToleranceCells: 4);

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
            infantryQueueBatch: 2,
            // Same weights as the shipped profile: there is no "legacy" value
            // to preserve here, target scoring did not exist before E7.
            targetDamageWeight: 10,
            targetThreatWeight: 6,
            targetFinishWeight: 3,
            targetDistanceWeight: 4,
            // Same reasoning as above: there is no "legacy" wave value to
            // preserve, waves did not exist.
            waveSize: 1,
            stagingDistanceCells: 12,
            stagingToleranceCells: 4);
    }
}
