using Nova.Core;

namespace Nova.AI.Data
{
    /// <summary>
    /// One short identifier for "which AI is this". It changes whenever the
    /// skirmish AI behaves differently, and it is meant to be read off a
    /// screenshot: the debug HUD shows it, every lab report carries it, and the
    /// behaviour journal keys its entries to it.
    /// <para>
    /// WHY TWO PARTS. AI behaviour changes in two ways that no single mechanism
    /// catches:
    /// </para>
    /// <list type="number">
    /// <item><b>Numbers</b> — a value in <see cref="AiProfiles.Ms1Canonical"/>
    /// moves. Caught automatically: <see cref="ProfileHash"/> is a hash over
    /// every field of the shipped profile, so an edit cannot be silent.</item>
    /// <item><b>Code</b> — the same numbers, a different rule (score targeting
    /// replacing list order, say). NOTHING can derive that from data, so
    /// <see cref="Revision"/> is bumped BY HAND. The safety net is a test that
    /// pins the identifier together with the canonical match's end-state hash:
    /// change behaviour without bumping, and it goes red.</item>
    /// </list>
    /// <para>
    /// The identifier is NOT part of the match fingerprint and deliberately so.
    /// Whether an AI profile is fingerprint-relevant is an owner decision
    /// (Simulation/Replays), and nothing here may quietly answer it.
    /// </para>
    /// </summary>
    public static class AiBehaviorId
    {
        /// <summary>
        /// Bumped BY HAND whenever the AI's behaviour code changes — a new
        /// rule, a changed formula, a different order of decisions. Not for
        /// comments, renames or refactors that leave the end-state hash alone.
        /// <para>
        /// History, so the number means something:
        /// </para>
        /// <list type="bullet">
        /// <item><b>1</b> — MS-1 skirmish AI as shipped: build order, economy,
        /// army, attack at the squad threshold, targets chosen as "HQ, else the
        /// first visible building, else the first visible unit".</item>
        /// <item><b>2</b> — target choice by integer score (counter table,
        /// threat, missing health, distance) instead of the order of the
        /// visibility list. The enemy HQ stays a short-circuit.</item>
        /// <item><b>3</b> — waves. The army marches at full strength and
        /// reinforcements wait at a staging cell between the own base and the
        /// enemy start area instead of walking to the front one at a time.
        /// Units already out are never called back. Off setting:
        /// <c>waveSize</c> 1.</item>
        /// <item><b>4</b> — retreat. A unit under a health percentage with an
        /// armed enemy nearby walks back to the staging cell instead of
        /// fighting to the last point, and rejoins with the next wave. No
        /// health hysteresis: MS-1 units never heal, so an exit percentage
        /// would never be reached. Off setting:
        /// <c>retreatHealthPercent</c> 0.</item>
        /// </list>
        /// <para>
        /// NOT bumped for the posture/assignment refactor of the army step:
        /// it reproduces the canonical match tick for tick, which is what
        /// "leaves the end-state hash alone" means. Aiming below the squad
        /// threshold WOULD have bumped it and was measured back out again —
        /// behaviour journal V003.
        /// </para>
        /// <para>
        /// r5 fixes two defects found in the review of r3/r4, both of which
        /// change decisions and therefore the end state: the wave now waits for
        /// what production can still deliver instead of a fixed cap (a single
        /// survivor outside the ring used to stall it for the rest of the
        /// match), and a retreating unit is pointed at its pursuer instead of
        /// carrying a march target it can no longer reach.
        /// </para>
        /// <para>
        /// r6 gives the wave a unit of measure. It marched on a COUNT of
        /// gathered units, and a count cannot tell an Alliance rifleman (100
        /// points of damage times health per firing interval) from a Legion
        /// recruit (44): twelve of each were both "a full wave", one of them at
        /// 44 % of the attack strength of the other. The gate now sums
        /// <c>CombatStrength</c> over the units inside the staging ring and
        /// compares it against <c>waveStrengthPoints</c>. The r5 rule survives
        /// translated into points — the threshold is still capped at what
        /// production can actually still deliver, so a survivor standing
        /// outside the ring cannot stall the next wave. Off setting:
        /// <c>waveStrengthPoints</c> 0, which restores the count.
        /// </para>
        /// <para>
        /// IT SHIPS DORMANT, and that is the point rather than an oversight.
        /// The threshold is capped at what production can still deliver, so the
        /// point clause can only decide anything while at least one head of the
        /// army cap is free — at the shipped cap of 12 that means eleven
        /// riflemen, 1.100 points, against a threshold of 1.200. The gate
        /// therefore decides exactly like the count it replaces and the
        /// canonical match is byte-identical. What the revision buys is the
        /// DECOUPLING: the wave's threshold is no longer a head count paired to
        /// the production cap, which is the precondition for moving that cap.
        /// Measured one-sided, moving it WITHOUT the gate makes the Legion
        /// worse (own losses 51 to 64); with the gate and a cap of 30 the same
        /// seat decides faster than today with 23 own losses. The cap itself is
        /// one of four values <c>MatchRunner</c> overrides with literals of its
        /// own, so it is not this strand's to move.
        /// <para>
        /// THE MARGIN IS NINE POINTS PER RIFLEMAN, and weapon numbers are this
        /// strand's own work: one more damage on the Alliance rifleman wakes
        /// the gate without anyone touching the cap.
        /// <c>AiProfileTests.TheStrengthGateIsDormantAtTheShippedArmyCap</c>
        /// computes that margin from <c>CombatStrength</c> rather than trusting
        /// a copied number, and goes red either way.
        /// </para>
        /// </para>
        /// <para>
        /// r7 keeps the D-077 strategic opening but makes its prerequisite
        /// handoff explicit: after the Refinery, the AI completes the Power
        /// plant required by D-103 before attempting its Barracks. The old
        /// margin-only rule happened to do that for Alliance, but Legion's
        /// 15-point margin covered the Barracks' 10-point draw and therefore
        /// retried an illegal placement forever once the all-of gate shipped.
        /// </para>
        /// </summary>
        public const int Revision = 7;

        /// <summary>
        /// Hash over every value of the shipped profile. Domain-separated like
        /// the rest of the project's hashes, so it can never collide with a
        /// state or definitions hash that happens to hold the same bytes.
        /// </summary>
        public static readonly ulong ProfileHash = ComputeProfileHash(AiProfiles.Ms1Canonical);

        /// <summary>
        /// What the HUD shows and what a report prints: <c>r2.4F1C08A9</c>.
        /// Short enough for a status line, unambiguous enough to grep for.
        /// </summary>
        public static readonly string Value = $"r{Revision}.{(uint)(ProfileHash >> 32):X8}";

        /// <summary>
        /// The same identifier with the full profile hash, for artefacts that
        /// have room and want to be exact rather than short.
        /// </summary>
        public static string Full => $"r{Revision}.0x{ProfileHash:X16}";

        /// <summary>
        /// Hash of an arbitrary profile — the lab tunes profiles that never
        /// ship, and a report that says which one played needs this.
        /// </summary>
        public static ulong ComputeProfileHash(AiProfile profile)
        {
            // Field order IS the hash. Appending a new field at the end keeps
            // old hashes comparable in the sense that matters here: they were
            // computed from a different set, and the value says so by changing.
            SimHashWriter writer = SimHashWriter.ForDefinitions();
            writer.WriteLengthPrefixedString(profile.ProfileId ?? string.Empty);
            writer.WriteInt32(AiProfile.SchemaVersion);
            writer.WriteUInt16(profile.DecisionTickInterval);
            writer.WriteInt32(profile.PlacementSearchRadius);
            writer.WriteInt32(profile.PowerReserve);
            writer.WriteInt32(profile.TargetHarvesters);
            writer.WriteInt32(profile.HarvesterQueueBatch);
            writer.WriteInt32(profile.TargetArmySize);
            writer.WriteInt32(profile.AttackSquadThreshold);
            writer.WriteInt32(profile.InfantryQueueBatch);
            writer.WriteInt32(profile.TargetDamageWeight);
            writer.WriteInt32(profile.TargetThreatWeight);
            writer.WriteInt32(profile.TargetFinishWeight);
            writer.WriteInt32(profile.TargetDistanceWeight);
            writer.WriteInt32(profile.WaveSize);
            writer.WriteInt32(profile.StagingDistanceCells);
            writer.WriteInt32(profile.StagingToleranceCells);
            writer.WriteInt32(profile.RetreatHealthPercent);
            writer.WriteInt32(profile.RetreatDangerCells);
            writer.WriteInt32(profile.WaveStrengthPoints);
            return writer.Digest();
        }
    }
}
