using System.Collections.Generic;
using Nova.Core;
using Nova.Gameplay.CombatFeedback;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using NUnit.Framework;
using UnityEngine;
using EntityId = Nova.Core.EntityId;

namespace Nova.Gameplay.Tests
{
    /// <summary>
    /// Fog-safety contract for Sprint 12B's presentation differ. These tests
    /// use only caller-provided visible samples, mirroring the production rule
    /// that the differ cannot inspect the raw entity store.
    /// </summary>
    [TestFixture]
    public sealed class VisibleCombatFrameDifferTests
    {
        private readonly List<VisibleCombatSample> _samples = new List<VisibleCombatSample>();
        private readonly List<CombatFeedbackEvent> _events = new List<CombatFeedbackEvent>();
        private VisibleCombatFrameDiffer _differ;

        [SetUp]
        public void SetUp()
        {
            _differ = new VisibleCombatFrameDiffer();
            _samples.Clear();
            _events.Clear();
        }

        [Test]
        public void FirstObservationEstablishesBaselineWithoutEvents()
        {
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry));

            Observe(1);

            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void SameTickNeverDuplicatesAnEvent()
        {
            EntityId target = new EntityId(1, 1);
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry, target: target));
            _samples.Add(Sample(1, 1, 1, UnitRole.BasicInfantry));
            Observe(1);

            _samples[0] = Sample(0, 1, 0, UnitRole.BasicInfantry, cooldown: 9, target: target);
            Observe(2);
            Assert.That(Count(CombatFeedbackKind.Shot), Is.EqualTo(1));

            _events.Clear();
            Observe(2);
            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void CooldownRiseWithVisibleTargetEmitsShotAtCopiedPosition()
        {
            EntityId target = new EntityId(1, 1);
            _samples.Add(Sample(0, 1, 0, UnitRole.LightTank, target: target));
            _samples.Add(Sample(1, 1, 1, UnitRole.BasicInfantry, x: 7f));
            Observe(10);

            _samples[0] = Sample(0, 1, 0, UnitRole.LightTank, cooldown: 12, target: target);
            Observe(11);

            CombatFeedbackEvent shot = First(CombatFeedbackKind.Shot);
            Assert.That(shot.TargetId, Is.EqualTo(target));
            Assert.That(shot.HasTargetPosition, Is.True);
            Assert.That(shot.TargetPosition, Is.EqualTo(new Vector3(7f, 0f, 0f)));
        }

        [Test]
        public void InvalidCurrentAndPreviousTargetSuppressesFalseShot()
        {
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry));
            Observe(1);

            _samples[0] = Sample(0, 1, 0, UnitRole.BasicInfantry, cooldown: 9);
            Observe(2);

            Assert.That(Count(CombatFeedbackKind.Shot), Is.Zero);
        }

        [Test]
        public void LethalShotUsesPreviousTargetAndConfirmsForeignDeath()
        {
            EntityId target = new EntityId(1, 1);
            _samples.Add(Sample(0, 1, 0, UnitRole.Artillery, target: target, damage: DamageType.Explosive));
            _samples.Add(Sample(1, 1, 1, UnitRole.LightTank, x: 6f));
            Observe(20);

            _samples.RemoveAt(1);
            _samples[0] = Sample(0, 1, 0, UnitRole.Artillery, cooldown: 40, target: EntityId.Invalid,
                damage: DamageType.Explosive);
            Observe(21);

            Assert.That(Count(CombatFeedbackKind.Shot), Is.EqualTo(1));
            CombatFeedbackEvent death = First(CombatFeedbackKind.Death);
            Assert.That(death.TargetId, Is.EqualTo(target));
            Assert.That(death.TargetPosition, Is.EqualTo(new Vector3(6f, 0f, 0f)));
            Assert.That(death.DamageType, Is.EqualTo(DamageType.Explosive));
        }

        [Test]
        public void HealthDropWithoutAssociatedShotGetsGenericKineticHit()
        {
            _samples.Add(Sample(0, 1, 1, UnitRole.LightTank, health: 100));
            Observe(1);

            _samples[0] = Sample(0, 1, 1, UnitRole.LightTank, health: 75);
            Observe(2);

            CombatFeedbackEvent hit = First(CombatFeedbackKind.Hit);
            Assert.That(hit.DamageType, Is.EqualTo(DamageType.Kinetic));
            Assert.That(hit.SourceId, Is.EqualTo(EntityId.Invalid));
        }

        [Test]
        public void OwnMobileDisappearanceIsDeathWithoutRawStoreLookup()
        {
            _samples.Add(Sample(0, 1, 0, UnitRole.Builder));
            Observe(1);

            _samples.Clear();
            Observe(2);

            Assert.That(Count(CombatFeedbackKind.Death), Is.EqualTo(1));
        }

        [TestCase(UnitRole.HQ)]
        [TestCase(UnitRole.Unit)]
        public void AmbiguousOwnBuildingOrSiteDisappearanceStaysSilent(UnitRole role)
        {
            _samples.Add(Sample(0, 1, 0, role));
            Observe(1);

            _samples.Clear();
            Observe(2);

            Assert.That(Count(CombatFeedbackKind.Death), Is.Zero);
        }

        [Test]
        public void ForeignFogLossWithoutLocalShotStaysSilent()
        {
            _samples.Add(Sample(0, 1, 1, UnitRole.BasicInfantry));
            Observe(1);

            _samples.Clear();
            Observe(2);

            Assert.That(Count(CombatFeedbackKind.Death), Is.Zero);
        }

        [Test]
        public void MultiTickCatchUpDoesNotClaimCorrelatedForeignDeath()
        {
            EntityId target = new EntityId(1, 1);
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry, target: target));
            _samples.Add(Sample(1, 1, 1, UnitRole.BasicInfantry));
            Observe(1);

            _samples.RemoveAt(1);
            _samples[0] = Sample(0, 1, 0, UnitRole.BasicInfantry, cooldown: 9, target: target);
            Observe(3);

            Assert.That(Count(CombatFeedbackKind.Shot), Is.EqualTo(1));
            Assert.That(Count(CombatFeedbackKind.Death), Is.Zero);
        }

        [Test]
        public void NewOwnProducedUnitEmitsReadyButBuildingDoesNot()
        {
            Observe(1);
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry));
            _samples.Add(Sample(1, 1, 0, UnitRole.Barracks));

            Observe(2);

            Assert.That(Count(CombatFeedbackKind.UnitReady), Is.EqualTo(1));
            Assert.That(First(CombatFeedbackKind.UnitReady).SourceRole, Is.EqualTo(UnitRole.BasicInfantry));
        }

        [Test]
        public void ResetAndVersionReuseNeverMixOldSamplesIntoNewMatch()
        {
            _samples.Add(Sample(0, 1, 0, UnitRole.BasicInfantry, health: 10));
            Observe(8);

            _differ.Reset(8);
            _events.Clear();
            _samples[0] = Sample(0, 2, 0, UnitRole.BasicInfantry, health: 100);
            Observe(1);

            Assert.That(_events, Is.Empty);
        }

        [Test]
        public void EveryArmedProfileHasDetectableCooldownRise()
        {
            for (int factionValue = (int)FactionId.Alliance; factionValue <= (int)FactionId.Legion; factionValue++)
            {
                var faction = (FactionId)factionValue;
                for (int roleValue = (int)UnitRole.Unit; roleValue <= (int)UnitRole.Artillery; roleValue++)
                {
                    var role = (UnitRole)roleValue;
                    WeaponProfile profile = WeaponProfiles.Get(faction, role);
                    if (!profile.IsArmed) continue;
                    Assert.That(profile.AttackCooldownTicks, Is.GreaterThan(1), $"{faction}/{role}");
                }
            }
        }

        private void Observe(uint tick)
        {
            _events.Clear();
            _differ.Observe(tick, viewerTeam: 0, _samples, _events);
        }

        private int Count(CombatFeedbackKind kind)
        {
            int count = 0;
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Kind == kind) count++;
            }
            return count;
        }

        private CombatFeedbackEvent First(CombatFeedbackKind kind)
        {
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].Kind == kind) return _events[i];
            }
            Assert.Fail($"No {kind} event in batch.");
            return default;
        }

        private static VisibleCombatSample Sample(
            int index,
            ushort version,
            byte player,
            UnitRole role,
            int health = 100,
            int cooldown = 0,
            EntityId target = default,
            float x = 0f,
            DamageType damage = DamageType.Kinetic)
        {
            if (!target.IsValid || target.Version == 0) target = EntityId.Invalid;
            return new VisibleCombatSample(
                new EntityId(index, version),
                player,
                role,
                new Vector3(x, 0f, 0f),
                health,
                cooldown,
                target,
                damage);
        }
    }
}
