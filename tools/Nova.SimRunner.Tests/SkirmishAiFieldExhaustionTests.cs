using NUnit.Framework;
using Nova.Core;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.State;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// What the AI does when the field it is mining runs out (issue #85, found
    /// in the beta test of 2026-08-10).
    /// <para>
    /// THE DEFECT WAS A LIVELOCK, not a missing strategy. The economy clears
    /// <c>HarvestFieldId</c> the moment a field is empty; that clearing is
    /// exactly what puts the harvester back into the AI's idle list; and the
    /// idle list was sent straight back to the same empty field. Income zero,
    /// every decision tick, for the rest of the match — with other registered
    /// fields standing open.
    /// </para>
    /// <para>
    /// THE SETUP MINES THE FIELD OUT INSTEAD OF DECLARING IT EMPTY, because a
    /// field cannot be registered as exhausted (<c>TryAddField</c> refuses a
    /// reserve of 0). That is not a workaround, it is the better test: it walks
    /// the exact sequence the beta test walked — mine, run dry, and then either
    /// carry on somewhere else or spin.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class SkirmishAiFieldExhaustionTests
    {
        private const byte AiSlot = 1;

        /// <summary>The field the opening gives the AI: (117,117), effectively endless.</summary>
        private const ushort HomeFieldId = 2;

        /// <summary>A small field placed NEARER the AI's HQ than its home field, so the AI picks it first.</summary>
        private const ushort NearFieldId = 3;

        /// <summary>
        /// Enough to be picked, worked and delivered from — and little enough to
        /// run out inside the budget. At <c>HarvestRateAE</c> per tick and two
        /// harvesters this is a few dozen ticks of actual mining.
        /// </summary>
        private const long NearFieldReserveAE = 200L;

        private const int BudgetTicks = 4000;

        [Test]
        public void WhenTheNearFieldRunsOut_TheAiMinesAnotherRegisteredField()
        {
            SkirmishAiTests.AiHost host = SkirmishAiTests.BuildMatch(SkirmishAiTests.Seed);

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True, "the AI has no HQ to sit at");
            Assert.That(host.Economy.TryAddField(NearFieldId, new GridPos2D(hqX + 1, hqY + 1), NearFieldReserveAE),
                Is.True, "the small near field could not be registered");

            // It has to be the NEAREST, or the AI never picks it and the test
            // measures nothing at all.
            Assert.That(host.Economy.TryGetField(HomeFieldId, out AetheriumField home), Is.True);
            Assert.That(DistanceSquared(hqX, hqY, hqX + 1, hqY + 1),
                Is.LessThan(DistanceSquared(hqX, hqY, home.GridPos.X, home.GridPos.Y)),
                "the small field is not the nearest one, so the AI would never have chosen it");

            bool minedTheNearField = false;
            bool exhausted = false;
            bool minedElsewhereAfterwards = false;
            long creditsAtExhaustion = 0;

            for (int tick = 0; tick < BudgetTicks && !host.Victory.IsDecided; tick++)
            {
                host.Step();

                Assert.That(host.Economy.TryGetField(NearFieldId, out AetheriumField near), Is.True);
                if (!exhausted)
                {
                    if (AnyHarvesterGatheringAt(host, AiSlot, NearFieldId)) minedTheNearField = true;
                    if (near.IsExhausted)
                    {
                        exhausted = true;
                        creditsAtExhaustion = host.Economy.GetPlayerEconomy(AiSlot).AetheriumCredits;
                    }
                    continue;
                }

                // AFTER exhaustion the empty field must never be handed out
                // again. This is the assertion the defect fails on: before the
                // fix every single decision tick re-issued exactly this.
                Assert.That(AnyHarvesterGatheringAt(host, AiSlot, NearFieldId), Is.False,
                    $"tick {host.Kernel.CurrentTick.Value}: a harvester was sent back to the exhausted field");

                if (AnyHarvesterGatheringAt(host, AiSlot, HomeFieldId)) minedElsewhereAfterwards = true;
            }

            Assert.Multiple(() =>
            {
                Assert.That(minedTheNearField, Is.True,
                    "the AI never mined the near field, so nothing was exhausted and nothing is under test");
                Assert.That(exhausted, Is.True,
                    $"the near field still holds reserve after {BudgetTicks} ticks — raise the budget or lower it");
                Assert.That(minedElsewhereAfterwards, Is.True,
                    "the AI stopped mining altogether once its near field ran out — that is the defect of #85");
                Assert.That(host.Economy.GetPlayerEconomy(AiSlot).AetheriumCredits,
                    Is.GreaterThan(creditsAtExhaustion),
                    "no income arrived after the near field ran out, so the economy did not actually resume");
            });
        }

        // NO TEST FOR THE PLACEMENT ANCHOR, and that is deliberate rather than
        // an omission. The anchor keeps looking at the nearest field whether it
        // is exhausted or not, because it answers "where is my base" — but on
        // the canonical opening the nearest field WITH reserve sits two cells
        // from the nearest field without one, so filtering the anchor would
        // move nothing and a test could not tell the two rules apart. It would
        // pass for the shape of the map, not for the rule, and advertise a
        // guarantee it does not hold. The reasoning lives where it belongs, in
        // the remarks on TryGetOwnFieldCell.

        // ----------------------------------------------------------------

        private static long DistanceSquared(int ax, int ay, int bx, int by)
        {
            long dx = ax - bx, dy = ay - by;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// A harvester of <paramref name="slot"/> that is GATHERING at
        /// <paramref name="fieldId"/> — deliberately not one that is on its way
        /// home.
        /// <para>
        /// THE DISTINCTION IS LOAD-BEARING and it cost this test a false
        /// failure waiting to happen. A harvester that filled up keeps the field
        /// id while it delivers, and the economy keeps it ON PURPOSE even when
        /// the field is empty, so the last load is not stranded
        /// (<c>EconomySystem</c>). Counting those would mean the assertion
        /// "nobody was sent back to the empty field" fires at a harvester nobody
        /// sent anywhere — on some seeds, and not on this one, which is the
        /// worst kind of test. What the AI can be held to is who it assigns, and
        /// it only ever assigns idle gatherers.
        /// </para>
        /// </summary>
        private static bool AnyHarvesterGatheringAt(SkirmishAiTests.AiHost host, byte slot, ushort fieldId)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role != UnitRole.Harvester) continue;
                if (u.IsReturningCargo) continue;
                if (u.HarvestFieldId == fieldId) return true;
            }
            return false;
        }

        private static bool TryHqCell(SkirmishAiTests.AiHost host, byte slot, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < units.Length; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || u.Role != UnitRole.HQ) continue;
                if (host.Construction.IsActiveSite(u.Id)) continue;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            cellX = -1;
            cellY = -1;
            return false;
        }
    }
}
