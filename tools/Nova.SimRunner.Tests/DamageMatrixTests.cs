using System;
using NUnit.Framework;
using Nova.Simulation.Combat;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Canonical damage-versus-armor matrix suite (.NET lane,
    /// docs/gamedesign/ArmorSystem.md "Schaden-gegen-Panzerung-Matrix").
    /// Pins all 36 multipliers, the integer-percent resolution rule and the
    /// truncation behaviour that keeps repeated shots free of rounding drift.
    /// Mirror of the EditMode lane DamageMatrixTests.
    /// </summary>
    [TestFixture]
    public sealed class DamageMatrixTests
    {
        /// <summary>
        /// The 36 documented multipliers in percent, transcribed
        /// INDEPENDENTLY from the ArmorSystem.md table (rows = damage type,
        /// columns = Infanterie, Leicht, Mittel, Schwer, Gebäude, Luft). This
        /// is deliberately a second copy: if someone edits the production
        /// table, the intent has to be re-stated here too.
        /// </summary>
        private static readonly int[,] DocumentedPercents =
        {
            //            Infantry Light Medium Heavy Building Air
            /* Kinetic   */ { 100,   75,   50,   25,    30,    75 },
            /* Energy    */ {  75,  100,  100,   75,    50,   100 },
            /* Explosive */ {  75,   75,  100,  100,    75,    50 },
            /* Fire      */ { 150,   75,   50,   25,   100,    25 },
            /* Bio       */ { 125,   75,   75,   50,    75,    50 },
            /* Radiation */ { 100,   75,   75,   75,    50,    75 },
        };

        [Test]
        public void Matrix_ReturnsExactlyTheThirtySixDocumentedValues()
        {
            Assert.That(DamageMatrix.DamageTypeCount, Is.EqualTo(6), "six damage types (ArmorSystem.md)");
            Assert.That(DamageMatrix.ArmorClassCount, Is.EqualTo(6), "six armor classes (ArmorSystem.md)");
            Assert.That(DocumentedPercents.Length, Is.EqualTo(36), "the matrix is a flat set of 36 numbers");

            for (int row = 0; row < DamageMatrix.DamageTypeCount; row++)
            {
                for (int column = 0; column < DamageMatrix.ArmorClassCount; column++)
                {
                    var damageType = (DamageType)row;
                    var armorClass = (ArmorClass)column;
                    Assert.That(
                        DamageMatrix.GetMultiplierPercent(damageType, armorClass),
                        Is.EqualTo(DocumentedPercents[row, column]),
                        $"multiplier {damageType} vs {armorClass}");
                }
            }
        }

        [Test]
        public void EnumOrder_IsTheWireContractTheMatrixIsIndexedBy()
        {
            // Renumbering either enum silently redefines every counter
            // relation, so the raw values are pinned.
            Assert.That((int)DamageType.Kinetic, Is.EqualTo(0));
            Assert.That((int)DamageType.Energy, Is.EqualTo(1));
            Assert.That((int)DamageType.Explosive, Is.EqualTo(2));
            Assert.That((int)DamageType.Fire, Is.EqualTo(3));
            Assert.That((int)DamageType.Bio, Is.EqualTo(4));
            Assert.That((int)DamageType.Radiation, Is.EqualTo(5));

            Assert.That((int)ArmorClass.Infantry, Is.EqualTo(0));
            Assert.That((int)ArmorClass.Light, Is.EqualTo(1));
            Assert.That((int)ArmorClass.Medium, Is.EqualTo(2));
            Assert.That((int)ArmorClass.Heavy, Is.EqualTo(3));
            Assert.That((int)ArmorClass.Building, Is.EqualTo(4));
            Assert.That((int)ArmorClass.Air, Is.EqualTo(5));

            Assert.That(Enum.GetUnderlyingType(typeof(DamageType)), Is.EqualTo(typeof(byte)));
            Assert.That(Enum.GetUnderlyingType(typeof(ArmorClass)), Is.EqualTo(typeof(byte)));
        }

        [Test]
        public void KineticVsHeavyAndVsInfantry_ProduceTheDocumentedDamageFromAKnownBase()
        {
            // Known base 100 makes the percent readable as the damage itself.
            Assert.That(DamageMatrix.Resolve(100, DamageType.Kinetic, ArmorClass.Infantry), Is.EqualTo(100),
                "Kinetic vs Infantry is 1.00 — the neutral reference of the table");
            Assert.That(DamageMatrix.Resolve(100, DamageType.Kinetic, ArmorClass.Heavy), Is.EqualTo(25),
                "Kinetic vs Heavy is 0.25 — guns break on heavy armor");

            // A base that is not a round 100 exercises the actual arithmetic.
            Assert.That(DamageMatrix.Resolve(60, DamageType.Kinetic, ArmorClass.Infantry), Is.EqualTo(60));
            Assert.That(DamageMatrix.Resolve(60, DamageType.Kinetic, ArmorClass.Heavy), Is.EqualTo(15));

            // The full swing the counter model exists for: a factor of four
            // between the softest and the hardest target of one damage type.
            Assert.That(
                DamageMatrix.Resolve(100, DamageType.Kinetic, ArmorClass.Infantry),
                Is.EqualTo(4 * DamageMatrix.Resolve(100, DamageType.Kinetic, ArmorClass.Heavy)));
        }

        [Test]
        public void Resolve_TruncatesTowardZero_Exactly()
        {
            // 35 * 25% = 8.75 -> 8, never 9.
            Assert.That(DamageMatrix.Resolve(35, DamageType.Kinetic, ArmorClass.Heavy), Is.EqualTo(8));
            // 10 * 75% = 7.5 -> 7.
            Assert.That(DamageMatrix.Resolve(10, DamageType.Explosive, ArmorClass.Infantry), Is.EqualTo(7));
            // 50 * 75% = 37.5 -> 37.
            Assert.That(DamageMatrix.Resolve(50, DamageType.Explosive, ArmorClass.Infantry), Is.EqualTo(37));
            // 1 * 25% = 0.25 -> 0: a shot can legally land for nothing.
            Assert.That(DamageMatrix.Resolve(1, DamageType.Kinetic, ArmorClass.Heavy), Is.EqualTo(0));
            // Exact multiples stay exact.
            Assert.That(DamageMatrix.Resolve(60, DamageType.Kinetic, ArmorClass.Medium), Is.EqualTo(30));
            Assert.That(DamageMatrix.Resolve(110, DamageType.Explosive, ArmorClass.Medium), Is.EqualTo(110));
            // The only multiplier above 1.00 rounds down too: 35 * 150% = 52.5 -> 52.
            Assert.That(DamageMatrix.Resolve(35, DamageType.Fire, ArmorClass.Infantry), Is.EqualTo(52));
        }

        [Test]
        public void Resolve_HasNoRoundingDriftOverManyApplications()
        {
            // Truncation happens ONCE per shot against the untouched base, so
            // N shots must remove exactly N * perShot health — a running
            // remainder would show up as a mismatch here.
            const int applications = 1000;

            for (int row = 0; row < DamageMatrix.DamageTypeCount; row++)
            {
                for (int column = 0; column < DamageMatrix.ArmorClassCount; column++)
                {
                    var damageType = (DamageType)row;
                    var armorClass = (ArmorClass)column;

                    for (int baseDamage = 1; baseDamage <= 120; baseDamage++)
                    {
                        int perShot = DamageMatrix.Resolve(baseDamage, damageType, armorClass);

                        long accumulated = 0;
                        for (int shot = 0; shot < applications; shot++)
                        {
                            accumulated += DamageMatrix.Resolve(baseDamage, damageType, armorClass);
                        }

                        Assert.That(accumulated, Is.EqualTo((long)perShot * applications),
                            $"drift for base {baseDamage}, {damageType} vs {armorClass}");
                        Assert.That(perShot * 100, Is.LessThanOrEqualTo(baseDamage * DamageMatrix.GetMultiplierPercent(damageType, armorClass)),
                            "truncation must never round up");
                        Assert.That((perShot + 1) * 100, Is.GreaterThan(baseDamage * DamageMatrix.GetMultiplierPercent(damageType, armorClass)),
                            "truncation must lose less than one whole point");
                    }
                }
            }
        }

        [Test]
        public void Resolve_ZeroOrNegativeBase_IsAlwaysZero()
        {
            // "Unarmed" is expressed as base damage 0 and nothing else, so it
            // has to be inert against every column of every row.
            for (int row = 0; row < DamageMatrix.DamageTypeCount; row++)
            {
                for (int column = 0; column < DamageMatrix.ArmorClassCount; column++)
                {
                    Assert.That(DamageMatrix.Resolve(0, (DamageType)row, (ArmorClass)column), Is.EqualTo(0),
                        "base damage 0 can never reduce health");
                    Assert.That(DamageMatrix.Resolve(-25, (DamageType)row, (ArmorClass)column), Is.EqualTo(0),
                        "a negative base can never heal a target through the matrix");
                }
            }
        }

        [Test]
        public void UnknownEnumValues_FailLoudly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DamageMatrix.GetMultiplierPercent((DamageType)6, ArmorClass.Infantry));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DamageMatrix.GetMultiplierPercent(DamageType.Kinetic, (ArmorClass)6));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => DamageMatrix.Resolve(10, DamageType.Kinetic, (ArmorClass)200));
        }

        [Test]
        public void Matrix_KeepsItsDocumentedShapeGuarantees()
        {
            // Radiation is deliberately flat: no value above neutral.
            for (int column = 0; column < DamageMatrix.ArmorClassCount; column++)
            {
                Assert.That(
                    DamageMatrix.GetMultiplierPercent(DamageType.Radiation, (ArmorClass)column),
                    Is.LessThanOrEqualTo(DamageMatrix.NeutralPercent),
                    "Strahlung denies zones, it never counters (ArmorSystem.md)");
            }

            // Every multiplier is a whole percent in a sane band — the percent
            // representation is only lossless while that holds.
            for (int row = 0; row < DamageMatrix.DamageTypeCount; row++)
            {
                for (int column = 0; column < DamageMatrix.ArmorClassCount; column++)
                {
                    int percent = DamageMatrix.GetMultiplierPercent((DamageType)row, (ArmorClass)column);
                    Assert.That(percent, Is.GreaterThanOrEqualTo(25),
                        "0.25 (Kinetic/Fire vs Heavy) is the documented floor; no multiplier is a full immunity");
                    Assert.That(percent, Is.LessThanOrEqualTo(150),
                        "1.50 (Fire vs Infantry) is the documented ceiling");
                }
            }
        }
    }
}
