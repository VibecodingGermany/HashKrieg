using NUnit.Framework;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Canonical combat suite (.NET lane, docs/tech/SimulationCore.md
    /// section 2 order step 8; docs/tech/FogOfWar.md sections 2 and 3):
    /// FoW-gated hitscan fire (Visible only — never Explored, never radar
    /// pings), the committed-view rule between recomputes, exact cooldown
    /// cadence, damage/death/order resolution, two-kernel determinism, hash
    /// sensitivity, replay playback and mid-combat snapshot continuation.
    /// Mirror of the EditMode lane CombatSystemTests.
    /// </summary>
    [TestFixture]
    public sealed class CombatSystemTests
    {
        private const ulong Seed = 0xC0BA7UL;
        private static readonly SimFixed HalfCell = SimFixed.FromRaw(SimFixed.OneRaw / 2);

        /// <summary>
        /// Canonical host in the SimulationCore.md section 2 order:
        /// pathfinding, movement, the 5 Hz FoW recompute, then combat.
        /// </summary>
        private sealed class TestHost
        {
            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public FogOfWarSystem Fog { get; }
            public CombatSystem Combat { get; }

            private TestHost(SimulationKernel kernel, EntityManager entities,
                Nova.Simulation.Construction.ConstructionSystem construction, FogOfWarSystem fog, CombatSystem combat)
            {
                Kernel = kernel;
                Entities = entities;
                Construction = construction;
                Fog = fog;
                Combat = combat;
            }

            public Nova.Simulation.Construction.ConstructionSystem Construction { get; }

            public static TestHost Create(ulong seed, int capacity = 64, ushort width = 64, ushort height = 64)
            {
                var entities = new EntityManager(capacity);
                var pathfinding = new PathfindingSystem(width, height);
                var movement = new MovementSystem(entities, pathfinding);
                // Faction source for the combat weapon table: an unregistered
                // economy state (all slots default to Alliance). 16.5: the
                // FoW radar read also requires the placement register.
                var factions = new EconomySystem(entities);
                var construction = new Nova.Simulation.Construction.ConstructionSystem(entities, factions);
                var fog = new FogOfWarSystem(entities, construction, factions, teamCount: 2, width, height);
                var combat = new CombatSystem(entities, fog, factions);

                var kernel = new SimulationKernel(new SimRandom(seed));
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fog);
                kernel.RegisterSystem(combat);
                kernel.Start();
                return new TestHost(kernel, entities, construction, fog, combat);
            }

            public void Step() => Kernel.StepTick();

            public void Step(int count)
            {
                for (int i = 0; i < count; i++) Kernel.StepTick();
            }
        }

        private static EntityId SpawnAt(
            TestHost host, byte team, int cellX, int cellY,
            int sightRadius = 10, int maxHealth = 100)
        {
            return host.Entities.SpawnUnit(
                team,
                new Transform2D(SimFixed.FromInt(cellX) + HalfCell, SimFixed.FromInt(cellY) + HalfCell),
                SimFixed.FromInt(5),
                sightRadius: SimFixed.FromInt(sightRadius),
                maxHealth: maxHealth);
        }

        private static void Teleport(TestHost host, EntityId id, int cellX, int cellY)
        {
            ref UnitState u = ref host.Entities.GetUnitRef(id);
            u.Transform = new Transform2D(
                SimFixed.FromInt(cellX) + HalfCell,
                SimFixed.FromInt(cellY) + HalfCell,
                u.Transform.Rotation);
        }

        private static int HealthOf(TestHost host, EntityId id)
        {
            Assert.That(host.Entities.TryGetUnit(id, out UnitState u), Is.True, "unit must be alive");
            return u.CurrentHealth;
        }

        [Test]
        public void Fires_WhenTargetAliveInRangeAndVisible_OnlyAfterFirstCommit()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 100);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(); // tick 1 (odd): no committed view yet — no legal shot
            Assert.That(HealthOf(host, target), Is.EqualTo(100),
                "before the first committed view nothing is Visible, so no shot is legal");

            host.Step(); // tick 2 (even): the commit makes the target Visible — first shot
            Assert.That(HealthOf(host, target), Is.EqualTo(100 - CombatSystem.DefaultWeaponDamage));
            Assert.That(host.Entities.GetUnitRef(attacker).WeaponCooldownTicks,
                Is.EqualTo(CombatSystem.DefaultCooldownTicks), "a shot restarts the cooldown");
        }

        // ------------------------------------------------------------------
        // D-087 auto-acquisition
        // ------------------------------------------------------------------

        [Test]
        public void AutoAcquire_IdleArmedUnit_FiresWithoutAnyOrder()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 100);
            // Deliberately NO explicit order: before D-087 this unit idled forever.

            host.Step(); // tick 1 (odd): no committed view yet — nothing to acquire
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget.IsValid, Is.False,
                "without a committed view nothing is Visible, so nothing is acquired");

            host.Step(); // tick 2 (even): commit shows the target — acquire AND fire in one tick
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(target),
                "the idle unit must pick the visible in-range hostile by itself");
            Assert.That(HealthOf(host, target), Is.EqualTo(100 - CombatSystem.DefaultWeaponDamage),
                "acquisition in the same tick precedes the engagement phase — the shot lands");
        }

        [Test]
        public void AutoAcquire_HiddenTarget_IsNotAcquired()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 1); // live sight covers only its own cell
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 100);

            host.Step(6);
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget.IsValid, Is.False,
                "Explored/unseen cells grant no targeting right — auto-acquire obeys the same FoW gate as shots");
            Assert.That(HealthOf(host, target), Is.EqualTo(100));
        }

        [Test]
        public void AutoAcquire_NearestWins_LowestIndexWinsTies()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10);
            EntityId farFirst = SpawnAt(host, 1, 14, 10, maxHealth: 1000); // dist 4, lower index
            EntityId nearSecond = SpawnAt(host, 1, 12, 10, maxHealth: 1000); // dist 2, higher index

            host.Step(2);
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(nearSecond),
                "the NEAREST legal target wins, regardless of spawn order");

            // Two equidistant targets: the lower entity index must win — two
            // hosts then make the same choice (lockstep determinism).
            var host2 = TestHost.Create(Seed);
            EntityId attacker2 = SpawnAt(host2, 0, 10, 10);
            EntityId eastFirst = SpawnAt(host2, 1, 13, 10, maxHealth: 1000);
            SpawnAt(host2, 1, 7, 10, maxHealth: 1000); // west, same distance, higher index
            host2.Step(2);
            Assert.That(host2.Entities.GetUnitRef(attacker2).AttackTarget, Is.EqualTo(eastFirst),
                "equal distance: the lowest entity index wins the tie");
        }

        [Test]
        public void AutoAcquire_ExplicitHeldOrder_IsNeverRetargeted()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 20);
            EntityId orderedFar = SpawnAt(host, 1, 17, 10, maxHealth: 1000);
            host.Entities.GetUnitRef(attacker).AttackTarget = orderedFar;
            SpawnAt(host, 1, 12, 10, maxHealth: 1000); // a closer hostile appears

            host.Step(4);
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(orderedFar),
                "an explicit order is HELD (out of range here) and never silently retargeted");
        }

        [Test]
        public void AutoAcquire_DefensePlatform_FiresOnItsOwn()
        {
            var host = TestHost.Create(Seed);
            EntityId platform = SpawnAt(host, 0, 10, 10, maxHealth: 600);
            host.Entities.GetUnitRef(platform).Role = UnitRole.DefensePlatform; // armed: 20 dmg, range 10, cd 10
            EntityId attacker = SpawnAt(host, 1, 13, 10, maxHealth: 200);

            host.Step(2);
            Assert.That(host.Entities.GetUnitRef(platform).AttackTarget, Is.EqualTo(attacker),
                "the only armed building must acquire by itself — it can never receive an explicit order");
            Assert.That(HealthOf(host, attacker), Is.LessThan(200), "the platform actually fires");
        }

        [Test]
        public void AutoAcquire_UnarmedRoles_NeverAcquire()
        {
            var host = TestHost.Create(Seed);
            EntityId builder = SpawnAt(host, 0, 10, 10);
            host.Entities.GetUnitRef(builder).Role = UnitRole.Builder; // unarmed by definition
            SpawnAt(host, 1, 12, 10, maxHealth: 100);

            host.Step(4);
            Assert.That(host.Entities.GetUnitRef(builder).AttackTarget.IsValid, Is.False,
                "damage 0 means unarmed: a Builder never acquires a target");
        }

        [Test]
        public void Cooldown_ExactlyFiveTicksBetweenShots()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 1000);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            // Shots land at ticks 2, 7, 12, 17 — exactly DefaultCooldownTicks
            // (5 ticks = 0.5 s at 10 Hz) apart, and on no other tick.
            int expectedHealth = 1000;
            for (int tick = 1; tick <= 18; tick++)
            {
                host.Step();
                if (tick >= 2 && (tick - 2) % CombatSystem.DefaultCooldownTicks == 0)
                {
                    expectedHealth -= CombatSystem.DefaultWeaponDamage;
                }
                Assert.That(HealthOf(host, target), Is.EqualTo(expectedHealth),
                    $"unexpected damage timing at tick {tick}");
            }
        }

        [Test]
        public void RadarPingOnly_GrantsNoTargetingPermission()
        {
            var host = TestHost.Create(Seed);
            // 16.5: only a COMPLETED Radar building radiates — centre (10,15),
            // coverage sight 10 x 2 = 20. The building also contributes plain
            // SIGHT (radius 10), so the target hides from both radii: 16 > 10
            // and 16 > 5 away, inside the 20 coverage, inside weapon range.
            EntityId attacker = SpawnAt(host, 0, 20, 10, sightRadius: 5);
            Assert.That(host.Construction.PlaceCompletedBuilding(0, 10, 9, 14).IsValid, Is.True,
                "Alliance Radar (def 10) as a completed placement");
            EntityId target = SpawnAt(host, 1, 26, 10, maxHealth: 100);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(2); // first commit
            var pings = new System.Collections.Generic.List<RadarSignature>();
            host.Fog.GetRadarSignatures(0, pings);
            Assert.That(pings.Count, Is.EqualTo(1), "the hidden in-range enemy pings on radar");
            Assert.That(host.Fog.GetTeamView(0).IsVisible(26, 10), Is.False);

            host.Step(18); // many shot opportunities pass
            Assert.That(HealthOf(host, target), Is.EqualTo(100),
                "a radar ping must never unlock a shot (FogOfWar.md section 3)");
            Assert.That(host.Entities.GetUnitRef(attacker).WeaponCooldownTicks, Is.EqualTo(0),
                "no shot was fired, so no cooldown was ever started");
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(target),
                "a hidden but living target is held, not dropped");
        }

        [Test]
        public void ExploredButNotVisible_NoFire()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 1000);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(2); // tick 2: visible, first shot lands
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - CombatSystem.DefaultWeaponDamage));

            // Live sight collapses (radius 1): at the tick-4 commit the
            // target cell demotes to Explored — in range, but not Visible.
            host.Entities.GetUnitRef(attacker).SightRadius = SimFixed.FromInt(1);
            host.Step(2);
            Assert.That(host.Fog.GetTeamView(0).GetCellState(13, 10), Is.EqualTo(VisionState.Explored));

            host.Step(10); // the shots due at ticks 7 and 12 must not happen
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - CombatSystem.DefaultWeaponDamage),
                "Explored is not a targeting permission");
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(target),
                "the target is held while merely Explored");
        }

        [Test]
        public void SightLostBetweenRecomputes_FiresUntilTheNextCommit()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 5);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 1000);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(6); // ticks 1..6: only the tick-2 shot so far; commits at 2/4/6 with live sight intact
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - CombatSystem.DefaultWeaponDamage),
                "the next shot is due at tick 7, so exactly one shot has landed");

            // Live sight is lost on the odd tick: the attacker steps back to
            // (7,10) — still in weapon range (6 <= 8 + 0.5), but its live
            // sight circle (cells 2..12) no longer covers the target cell.
            // The committed mask of tick 6 still shows (13,10) Visible.
            Teleport(host, attacker, 7, 10);

            host.Step(); // tick 7 (odd): no recompute — the committed view governs and the due shot fires
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - 2 * CombatSystem.DefaultWeaponDamage),
                "between recomputes the committed sight stays in force (FogOfWar.md section 2)");

            host.Step(); // tick 8 (even): the commit demotes (13,10) to Explored
            Assert.That(host.Fog.GetTeamView(0).GetCellState(13, 10), Is.EqualTo(VisionState.Explored));

            host.Step(5); // the shot due at tick 12 must NOT happen anymore
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - 2 * CombatSystem.DefaultWeaponDamage),
                "after the commit the lost sight stops the fire");
        }

        [Test]
        public void OutOfRange_TargetHeld_NoFire()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 20);
            EntityId target = SpawnAt(host, 1, 22, 10, maxHealth: 100); // 12 > 8 + 0.5
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(10);
            Assert.That(HealthOf(host, target), Is.EqualTo(100), "out of range means no shots");
            Assert.That(host.Entities.GetUnitRef(attacker).AttackTarget, Is.EqualTo(target),
                "an out-of-range target is held — closing the distance is Movement's concern");
        }

        [Test]
        public void RangeBoundary_CenterDistanceLeqRangePlusTargetRadius_Inclusive()
        {
            // Exact Q16.16 boundary: center distance 8.5 == range 8 + radius 0.5.
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10, sightRadius: 12);
            EntityId target = host.Entities.SpawnUnit(
                1,
                new Transform2D(SimFixed.FromInt(19), SimFixed.FromInt(10) + HalfCell), // exactly 8.5 m
                SimFixed.FromInt(5),
                maxHealth: 100);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(2);
            Assert.That(HealthOf(host, target), Is.EqualTo(100 - CombatSystem.DefaultWeaponDamage),
                "the boundary is inclusive: distance == range + target radius fires");

            // One raw Q16.16 unit beyond the boundary: no shot.
            var host2 = TestHost.Create(Seed);
            EntityId attacker2 = SpawnAt(host2, 0, 10, 10, sightRadius: 12);
            EntityId target2 = host2.Entities.SpawnUnit(
                1,
                new Transform2D(SimFixed.FromRaw(SimFixed.FromInt(19).RawValue + 1), SimFixed.FromInt(10) + HalfCell),
                SimFixed.FromInt(5),
                maxHealth: 100);
            host2.Entities.GetUnitRef(attacker2).AttackTarget = target2;

            host2.Step(2);
            Assert.That(HealthOf(host2, target2), Is.EqualTo(100),
                "one raw unit beyond the boundary must not fire");
        }

        [Test]
        public void Damage_KillsAtZeroHealth_DespawnsAndClearsAllAttackOrders()
        {
            var host = TestHost.Create(Seed);
            EntityId attackerA = SpawnAt(host, 0, 10, 10);
            EntityId attackerB = SpawnAt(host, 0, 11, 11);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: CombatSystem.DefaultWeaponDamage);
            host.Entities.GetUnitRef(attackerA).AttackTarget = target;
            host.Entities.GetUnitRef(attackerB).AttackTarget = target;

            host.Step(2); // one shot is exactly lethal

            Assert.That(host.Entities.IsValid(target), Is.False, "health <= 0 despawns deterministically");
            Assert.That(host.Entities.GetUnitRef(attackerA).AttackTarget, Is.EqualTo(EntityId.Invalid),
                "the killer's order resolves");
            Assert.That(host.Entities.GetUnitRef(attackerB).AttackTarget, Is.EqualTo(EntityId.Invalid),
                "third-party orders on the dead id resolve in the same tick");
        }

        [Test]
        public void DeadAttacker_NeverFiresAgain()
        {
            var host = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(host, 0, 10, 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 1000);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(2); // first shot lands, cooldown 5 starts
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - CombatSystem.DefaultWeaponDamage));

            host.Entities.DespawnUnit(attacker); // dies carrying its cooldown
            host.Step(10); // past the next due shot at tick 7
            Assert.That(HealthOf(host, target), Is.EqualTo(1000 - CombatSystem.DefaultWeaponDamage),
                "a dead attacker never fires again");
        }

        [Test]
        public void UnitWithoutCommittedTeamView_CannotFire()
        {
            var host = TestHost.Create(Seed);
            // Slot 3 has no team mask (MS-1: two teams, index == slot).
            EntityId attacker = SpawnAt(host, 3, 10, 10);
            EntityId target = SpawnAt(host, 1, 13, 10, maxHealth: 100);
            host.Entities.GetUnitRef(attacker).AttackTarget = target;

            host.Step(10);
            Assert.That(HealthOf(host, target), Is.EqualTo(100),
                "a slot without a committed team view has no legal shots");
        }

        [Test]
        public void TwoKernels_Combat300Ticks_ProduceIdenticalHashes()
        {
            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);

            var idsA = new EntityId[6];
            var idsB = new EntityId[6];
            for (int i = 0; i < 6; i++)
            {
                byte team = (byte)(i % 2);
                int health = i == 5 ? 30 : 500; // one early death exercises the kill sweep
                idsA[i] = SpawnAt(hostA, team, 10 + i * 2, 10, sightRadius: 12, maxHealth: health);
                idsB[i] = SpawnAt(hostB, team, 10 + i * 2, 10, sightRadius: 12, maxHealth: health);
                Assert.That(idsB[i], Is.EqualTo(idsA[i]), "same spawn sequence must yield the same id");
            }

            // Mutual attack orders across the teams (adjacent pairs).
            for (int i = 0; i < 6; i++)
            {
                int enemyIndex = i % 2 == 0 ? i + 1 : i - 1;
                hostA.Entities.GetUnitRef(idsA[i]).AttackTarget = idsA[enemyIndex];
                hostB.Entities.GetUnitRef(idsB[i]).AttackTarget = idsB[enemyIndex];
            }

            for (int tick = 0; tick < 300; tick++)
            {
                hostA.Step();
                hostB.Step();
                Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"hash mismatch at tick {tick + 1}");
            }
        }

        [Test]
        public void HealthChange_ChangesStateHash()
        {
            var hostA = TestHost.Create(Seed);
            var hostB = TestHost.Create(Seed);
            EntityId a = SpawnAt(hostA, 1, 30, 30, maxHealth: 100);
            EntityId b = SpawnAt(hostB, 1, 30, 30, maxHealth: 100);
            hostA.Step(2);
            hostB.Step(2);
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()));

            hostB.Entities.GetUnitRef(b).CurrentHealth -= 1; // combat-relevant state lives in the entity store
            Assert.That(hostB.Kernel.CalculateStateHash(), Is.Not.EqualTo(hostA.Kernel.CalculateStateHash()),
                "a single health change must move the canonical state hash");
        }

        [Test]
        public void Snapshot_MidCombatRestore_ContinuesIdentically()
        {
            var hostA = TestHost.Create(Seed);
            EntityId attacker = SpawnAt(hostA, 0, 10, 10);
            EntityId target = SpawnAt(hostA, 1, 13, 10, maxHealth: 1000);
            hostA.Entities.GetUnitRef(attacker).AttackTarget = target;

            hostA.Step(9); // mid-combat: health reduced, cooldown running
            byte[] snapshotBytes = hostA.Kernel.SaveSnapshot();

            var hostB = TestHost.Create(Seed);
            Assert.That(hostB.Kernel.TryRestoreSnapshot(snapshotBytes), Is.True);
            Assert.That(HealthOf(hostB, target), Is.EqualTo(HealthOf(hostA, target)));
            Assert.That(hostB.Kernel.SaveSnapshot(), Is.EqualTo(snapshotBytes),
                "snapshot roundtrip must be byte-identical");

            for (int tick = 0; tick < 100; tick++)
            {
                hostA.Step();
                hostB.Step();
                Assert.That(hostB.Kernel.CalculateStateHash(), Is.EqualTo(hostA.Kernel.CalculateStateHash()),
                    $"mid-combat continuation diverged at tick {tick + 1}");
            }
        }

        [Test]
        public void Replay_CombatPlayback_ReproducesRecordedEndHash()
        {
            // Live match: slot 0 orders its unit to attack the slot-1 unit at
            // tick 1; the exchange runs 20 ticks and is recorded.
            const int ticks = 20;
            var host = ReplayHost.Create(Seed);
            EntityId attacker = host.Entities.SpawnUnit(
                0, new Transform2D(SimFixed.FromInt(10) + HalfCell, SimFixed.FromInt(10) + HalfCell), SimFixed.FromInt(5));
            EntityId target = host.Entities.SpawnUnit(
                1, new Transform2D(SimFixed.FromInt(13) + HalfCell, SimFixed.FromInt(10) + HalfCell), SimFixed.FromInt(5), maxHealth: 1000);

            MatchFingerprint fingerprint = host.CreateFingerprint();
            var recorder = new ReplayRecorder(fingerprint, host.Kernel.SaveSnapshot());

            var attack = new AttackTargetPayload(
                new[] { UnitCommandStateView.ToRawEntityId(attacker) },
                UnitCommandStateView.ToRawEntityId(target));
            Assert.That(
                host.Ingress.TrySubmitIntent(CommandIntent.Create(attack), out _),
                Is.EqualTo(CommandIngressResult.Accepted));

            for (int tick = 1; tick <= ticks; tick++)
            {
                CommandBatch batch = host.StepTick();
                recorder.RecordTick(host.Kernel.CurrentTick.Value, batch, host.Kernel.LastTickResults);
            }
            ulong endHash = host.Kernel.CalculateStateHash();
            Assert.That(host.Entities.TryGetUnit(target, out UnitState liveTarget), Is.True);
            Assert.That(liveTarget.CurrentHealth, Is.LessThan(1000), "the live match really saw combat");
            byte[] replayBytes = recorder.Finalize(endHash);

            // Playback on a fresh host reproduces the recorded end hash.
            var playback = ReplayHost.Create(Seed);
            Assert.That(
                ReplayPlayer.TryPlay(replayBytes, fingerprint, playback.Kernel, playback.Ingress,
                    out ReplayPlaybackError error, out string detail),
                Is.True, $"combat replay playback must verify: {error} ({detail})");
            Assert.That(playback.Kernel.CalculateStateHash(), Is.EqualTo(endHash));
        }

        /// <summary>
        /// Canonical host with the full command pipeline (session/ingress) for
        /// the replay test — same wiring as the kernel integration tests,
        /// plus FoW and combat in the SimulationCore.md section 2 order.
        /// </summary>
        private sealed class ReplayHost
        {
            private const byte HumanSlot = 0;
            private const byte AiSlot = 1;

            public SimulationKernel Kernel { get; }
            public EntityManager Entities { get; }
            public MatchSession Session { get; }
            public CommandIngress Ingress { get; }

            private ReplayHost(SimulationKernel kernel, EntityManager entities, MatchSession session, CommandIngress ingress)
            {
                Kernel = kernel;
                Entities = entities;
                Session = session;
                Ingress = ingress;
            }

            public static ReplayHost Create(ulong seed)
            {
                var entities = new EntityManager(64);
                var pathfinding = new PathfindingSystem(64, 64);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities);
                // 16.5: the FoW radar read requires the placement register.
                var construction = new Nova.Simulation.Construction.ConstructionSystem(entities, economy);
                var fog = new FogOfWarSystem(entities, construction, economy, teamCount: 2, 64, 64);
                var combat = new CombatSystem(entities, fog, economy);

                var kernel = new SimulationKernel(new SimRandom(seed));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fog);
                kernel.RegisterSystem(combat);

                var session = new MatchSession(
                    localSlot: HumanSlot, activeSlots: new byte[] { HumanSlot, AiSlot }, inputDelayTicks: 1);
                var ingress = new CommandIngress(session);
                _ = new LocalLoopbackTransport(ingress);
                kernel.BindCommands(new UnitCommandStateView(entities, pathfinding, economy), ingress);

                kernel.Start();
                return new ReplayHost(kernel, entities, session, ingress);
            }

            public MatchFingerprint CreateFingerprint()
            {
                var slots = new byte[CommandLimits.ReservedPlayerSlots];
                slots[HumanSlot] = (byte)PlayerSlotOccupancy.Human;
                slots[AiSlot] = (byte)PlayerSlotOccupancy.AI;
                return MatchFingerprint.CreateCurrent(
                    MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                    MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Definitions),
                    MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                    slots,
                    new byte[CommandLimits.ReservedPlayerSlots],
                    Seed,
                    Kernel.CalculateStateHash(),
                    Session.InputDelayTicks);
            }

            public CommandBatch StepTick()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.That(Kernel.SubmitBatch(batch), Is.True, "a sealed batch must be accepted");
                }
                Kernel.StepTick();
                Session.AdvanceTick();
                return batch;
            }
        }
    }
}
