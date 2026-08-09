namespace Nova.AI
{
    /// <summary>
    /// The arithmetic of "may the wave march?", as a pure function.
    /// <para>
    /// IT LIVES IN ITS OWN TYPE BECAUSE IT HAS TO BE TESTABLE DIRECTLY. As a
    /// private method inside <see cref="SkirmishAiSystem"/> it could only be
    /// reached through a whole match, and a match cannot produce the states
    /// this has to be right about — more units alive than the army cap allows,
    /// a Barracks that just died, a threshold that binds instead of the
    /// ceiling. Mutation testing proved the point: with the arithmetic buried,
    /// deleting the clamp and ignoring the threshold entirely both left the
    /// whole suite green.
    /// </para>
    /// <para>
    /// No state, no fields, integer only — the same rules the simulation runs
    /// under, because the answer feeds a decision two machines have to reach
    /// identically.
    /// </para>
    /// </summary>
    public static class WaveStrengthGate
    {
        /// <summary>
        /// The strength the staging ring has to hold before the wave marches.
        /// <para>
        /// TWO CLAUSES, and the second one is the whole reason this is not just
        /// <paramref name="wavePoints"/>. The threshold is capped at what the
        /// ring can still GROW to — what stands there now, plus one full-health
        /// produced unit for every head the cap still has free. Without that
        /// cap the wave waits for strength that can never arrive: every
        /// survivor of an earlier wave standing outside the ring is a unit the
        /// next wave will never get, because the army cap counts it and the
        /// Barracks refills only to <paramref name="armyCap"/> MINUS the
        /// survivors. Measured consequence of exactly that defect in r4: eleven
        /// units stood at the staging cell until the time limit while a single
        /// unit held the front alone.
        /// </para>
        /// <para>
        /// A FREE HEAD IS ONLY FREE WHILE SOMETHING CAN BUILD INTO IT, which is
        /// what <paramref name="canProduce"/> says. Without it the ceiling
        /// counts units nobody can make, and a wave whose Barracks has been
        /// destroyed waits for reinforcements that will never come while its
        /// base is taken apart — the r4 stall again, one level up. The count
        /// rule this replaces never needed it because it marched at
        /// <c>waveSize</c> regardless; a threshold in points has no such second
        /// bound and therefore has to model the producer.
        /// </para>
        /// <para>
        /// CREDITS ARE DELIBERATELY NOT PART OF IT. Being broke is temporary,
        /// having no Barracks is not, and a gate that flickered with the
        /// treasury would re-order the whole army every cadence — that is the
        /// intent churn that sank <c>DefendBase</c> (behaviour journal V002).
        /// </para>
        /// <para>
        /// THERE IS NO FLOOR UNDER THE RESULT, and the first version had one — a
        /// full produced unit — which was a second rule wearing a guard's
        /// clothes: it made a lone WOUNDED reinforcement wait for a unit that
        /// could never be built. The lab caught it, the canonical match ran
        /// 1.650 ticks longer. It is therefore NOT literally r5's
        /// <c>if (reachable &lt; 1) reachable = 1</c>: that floor is one HEAD,
        /// this one is zero POINTS, and they differ in exactly one state —
        /// everything committed, nothing gathered. Nothing reads the difference,
        /// because with no gathered unit there is nobody the flag could send.
        /// </para>
        /// </summary>
        /// <param name="wavePoints">Profile threshold in combat points; the caller only reaches here while it is positive.</param>
        /// <param name="gatheredStrength">Summed strength of the units waiting inside the staging ring.</param>
        /// <param name="gathered">How many those are.</param>
        /// <param name="committed">Living combat units already outside the ring.</param>
        /// <param name="producedStrength">Full-health strength of the unit the Barracks builds.</param>
        /// <param name="armyCap">The profile's army cap — alive plus queued.</param>
        /// <param name="canProduce">False when nothing can build the next unit (no completed Barracks).</param>
        public static long Threshold(
            int wavePoints,
            long gatheredStrength,
            int gathered,
            int committed,
            int producedStrength,
            int armyCap,
            bool canProduce)
        {
            int freeHeads = canProduce ? armyCap - committed - gathered : 0;
            if (freeHeads < 0) freeHeads = 0;

            long attainable = gatheredStrength + (long)freeHeads * producedStrength;
            return wavePoints < attainable ? wavePoints : attainable;
        }

        /// <summary>
        /// Whether the wave marches this decision: the ring holds at least
        /// <see cref="Threshold"/>. Stated as its own method so the call site
        /// reads as one question and the comparison operator is pinned in one
        /// place.
        /// </summary>
        public static bool IsReady(
            int wavePoints,
            long gatheredStrength,
            int gathered,
            int committed,
            int producedStrength,
            int armyCap,
            bool canProduce)
        {
            return gatheredStrength >= Threshold(
                wavePoints, gatheredStrength, gathered, committed, producedStrength, armyCap, canProduce);
        }
    }
}
