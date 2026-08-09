using NUnit.Framework;
using Nova.AI.Data;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Pins the OUTCOME of the canonical AI match: the tick it is decided on
    /// and the end-state hash. Owned by the maintainer strand (D-101).
    /// <para>
    /// Why this is not in <see cref="SkirmishAiTests"/> any more: these two
    /// numbers move on ANY change to the simulation the AI plays in — economy,
    /// construction, production, vision — not only on a change to the AI
    /// itself. They sat in the AI suite until 2026-08-09, where every package
    /// of Sprint 16 tripped them and the failure message sent the maintainer
    /// strand to the AI behaviour journal, which is the wrong book. The
    /// identifier pin — "which AI is this" — stayed where it belongs, in
    /// <see cref="SkirmishAiTests.AiBehaviorId_TracksWhichAiThisIs"/>.
    /// </para>
    /// <para>
    /// The split does not lose the diagnosis, because this test reads the
    /// identifier too. Read the two results together:
    /// </para>
    /// <list type="table">
    /// <item><term>outcome moved, identifier moved</term><description>the AI
    /// changed. The AI strand bumps <c>AiBehaviorId.Revision</c>, writes the
    /// journal entry in <c>tools/Nova.AiLab/reports/behavior-log.md</c>, and
    /// updates the numbers below in the same commit.</description></item>
    /// <item><term>outcome moved, identifier unchanged</term><description>the
    /// simulation under the AI changed. The strand that changed it updates the
    /// numbers below and says so in its PR. No journal entry, no revision
    /// bump — nothing about the AI moved.</description></item>
    /// <item><term>outcome UNDECIDED (tick 0)</term><description>not a moved
    /// pin. The AI no longer finishes the match inside the budget, which means
    /// something broke its loop. Fix the cause; do not update the
    /// numbers.</description></item>
    /// </list>
    /// <para>
    /// This is NOT one of the four determinism baselines: those live in their
    /// own files and force a behaviour PR and a baseline PR apart. This pin
    /// belongs WITH the change that moved it and is updated in the same commit.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class CanonicalAiOutcomeTests
    {
        /// <summary>Decided tick of the canonical AI match, last moved by: Sprint 15 (r5) — unchanged by 16.1.</summary>
        private const uint PinnedDecidedTick = 2548u;

        /// <summary>
        /// End-state hash of the canonical AI match, last moved by: Sprint 16 package
        /// 16.1 (#43) — the founding Harvester is born with a standing harvest
        /// order, so the AI match ends with a different economy. The AI itself is
        /// unchanged: AiBehaviorId stayed r5.779A1B5B.
        /// Previous value: 0x14472B2B943ED2BB (Sprint 15, r5).
        /// </summary>
        private const string PinnedEndState = "0x8C0B54F31F2986B7";

        [Test]
        public void CanonicalAiMatch_DecidesOnThePinnedTick_WithThePinnedEndState()
        {
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);
            uint decided = host.RunUntilDecided(SkirmishAiTests.EndToEndBudgetTicks);
            ulong endState = host.Kernel.CalculateStateHash();

            // Undecided is a defect, not a moved pin — it is asserted first and
            // on its own so the failure says which of the two happened.
            Assert.That(decided, Is.Not.Zero,
                $"the AI did not decide the canonical match within {SkirmishAiTests.EndToEndBudgetTicks} ticks — "
                + "its loop is broken, not merely moved. Fix the cause instead of updating the pin.");

            Assert.Multiple(() =>
            {
                Assert.That(decided, Is.EqualTo(PinnedDecidedTick),
                    "the canonical AI match is decided on a different tick than the pinned one — "
                    + $"AI identifier is {AiBehaviorId.Value}; if that string is unchanged, the simulation moved, not the AI");
                Assert.That($"0x{endState:X16}", Is.EqualTo(PinnedEndState),
                    "the canonical AI match ends in a different state than the pinned one — "
                    + $"AI identifier is {AiBehaviorId.Value}; if that string is unchanged, the simulation moved, not the AI");
            });
        }
    }
}
