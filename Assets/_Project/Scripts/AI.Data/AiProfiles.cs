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
        /// across, and <c>AiProfileTests</c> asserts every one of them value
        /// for value.
        /// </para>
        /// <para>
        /// WHAT DOES NOT CHECK THIS: the four determinism baselines. Not one
        /// of them runs the skirmish AI — <c>Determinism10000Scenario</c>
        /// registers Economy, Construction, Production, Pathfinding, Movement,
        /// FogOfWar, Combat and Victory, and the other three pin snapshot
        /// bytes, command serialisation and the RNG. Every number here can
        /// move without turning any of them red. The guard against that is the
        /// end-state pin in <c>SkirmishAiTests</c>.
        /// </para>
        /// </summary>
        public static readonly AiProfile Ms1Canonical = new AiProfile(
            profileId: "ms1-canonical",
            decisionTickInterval: 20,
            placementSearchRadius: 8,
            powerReserve: 0,
            targetHarvesters: 2,
            harvesterQueueBatch: 2,
            // THE ARMY CAP IS THE NUMBER THAT SHOULD MOVE NEXT, and moving it
            // here alone would not move the game. MatchRunner takes fifteen of
            // this profile's nineteen values through the historical
            // AiFactionProfile constructor and OVERRIDES four with literals of
            // its own — this one among them. Change it here and the lab plays
            // differently while the game does not, and the promise above
            // ("what MatchRunner ships today, value for value") quietly stops
            // being true. AiProfileTests.MatchRunnerPassesTheSameFourNumbers...
            // is what keeps the four honest; it reads MatchRunner's source.
            //
            // Why it matters for r6: waveStrengthPoints 1200 are 28 Legion
            // recruits at 44 points each, and the point clause can only decide
            // anything while a head is still free — so it first binds at a cap
            // of 29. Below that the gate does not "fall back to a head count":
            // it degenerates into "gather the entire army cap", which is why
            // caps 22, 24 and 28 were measured as grinds (78, 178 and 154 own
            // losses on the Legion seat). Measured one-sided, 30 is where it
            // works: the Legion decides FASTER than today (5.005 against 5.773
            // ticks) with 23 own losses instead of 51 and an exchange ratio of
            // 139 against 45. Raising the cap WITHOUT the gate goes the other
            // way (own losses 51 -> 64), which is why the gate ships first.
            //
            // The request is written up in the PR; the one line is
            // MatchRunner's, and MatchRunner belongs to the network strand.
            targetArmySize: 12,
            attackSquadThreshold: 6,
            infantryQueueBatch: 2,
            // Target scoring (E7). These four are the first numbers here that
            // are NOT a copy of a shipped constant — there was no scoring
            // before, only "HQ, else the first visible building, else the
            // first visible unit". They come from the plan's sketch
            // (AiSimulationEnvironment.md section 4.6). They change how the AI
            // plays and therefore move the end-state pin in SkirmishAiTests;
            // the four determinism baselines do not see this system and stay
            // green either way.
            targetDamageWeight: 10,
            targetThreatWeight: 6,
            targetFinishWeight: 3,
            targetDistanceWeight: 4,
            // Waves. SINCE r6 THIS NUMBER NO LONGER SETS THE WAVE — the
            // threshold is waveStrengthPoints below, and this value is read
            // for exactly two things: 1 still means "waves off" (and takes the
            // strength gate off with them), and anything above 1 is the
            // threshold of the COUNT path, which now only runs while
            // waveStrengthPoints is 0. It stays at 12 because that is the value
            // the count path was measured with; it is no longer paired to the
            // army cap, and it must not be re-paired to it.
            //
            // What it meant while it did set the wave: attack at full strength,
            // never reinforce piecemeal. A reinforcement waits at the staging
            // cell until the wave is full again. Units already out are never
            // called back — that part is unchanged.
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
            stagingToleranceCells: 4,
            // Retreat. A unit under 60 % health with an armed enemy within
            // eight cells walks back to the staging cell instead of dying
            // where it stands; once home it is an ordinary waiting unit and
            // leaves with the next wave. There is no exit percentage and
            // there cannot be one — MS-1 units never heal, see AiProfile.
            //
            // 60 is not a middle value, it is the top of a measured curve.
            // One-sided against retreat off, exchange ratio as Alliance:
            // 25 -> 138, 40 -> 184, 60 -> 252, 75 -> 290, 90 -> 209. 75 buys
            // its higher ratio with a match that runs twice as long and twice
            // the own losses; 90 turns the curve down on both seatings. 0
            // switches the rule off, which is how it was measured (M001).
            retreatHealthPercent: 60,
            retreatDangerCells: 8,
            // Wave strength (r6). 1.200 points is what twelve Alliance
            // riflemen at full health weigh, so on the Alliance seat the
            // threshold reads as "the same wave as before" — and on the Legion
            // seat it reads as "no, twelve recruits are not a full wave", which
            // is the whole correction. The value is one number for both
            // factions on purpose; see AiProfile.WaveStrengthPoints.
            //
            // IT ONLY BITES WHILE THE ARMY CAP ALLOWS MORE STRENGTH TO GATHER
            // THAN IT ASKS FOR — the threshold is capped at what production can
            // still deliver (see ResolveArmyPosture). At the old cap of 12 that
            // ceiling bound first on both seats and the gate decided exactly
            // like the head count, byte for byte. Which is why this value and
            // the cap above ship together and why the cap is derived from this
            // value, not chosen next to it.
            //
            // 0 switches the rule off and restores the head count exactly
            // (finding M001: a behaviour without an off setting cannot be
            // measured one-sided).
            waveStrengthPoints: 1200);

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
            stagingToleranceCells: 4,
            retreatHealthPercent: 0,
            retreatDangerCells: 8,
            // Same reasoning again: there is no "legacy" strength value, the
            // wave counted heads. 0 is the off setting.
            waveStrengthPoints: 0);
    }
}
