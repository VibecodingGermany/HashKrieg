using System.Collections.Generic;
using NUnit.Framework;
using Nova.AI;
using Nova.AI.Data;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Victory;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// MS-1 skirmish AI suite (.NET lane, docs/tech/AIArchitecture.md): the
    /// stub-to-opponent slice. Proves the AI plays the full D-077 loop over
    /// the canonical command path — build order, harvest economy, army
    /// production and explicit attacks — and the end-to-end contract: the AI
    /// on slot 1 defeats a PASSIVE slot 0 as <see cref="MatchOutcome.VictoryElimination"/>
    /// inside a fixed tick budget.
    /// <para>
    /// The <see cref="AiHost"/> below is a byte-exact wiring mirror of
    /// <c>MatchRunner.InitializeMatch</c> with the skirmish AI enabled: same
    /// construction order, same canonical registration order (the AI between
    /// combat and victory), the same human/AI session pair with the
    /// forwarding <see cref="AiPeerCommandTransport"/>, and
    /// <see cref="AiHost.Step"/> mirrors <c>MatchRunner.StepFixedTick</c>
    /// including the pre-step advance of the AI peer clock. Any edit to the
    /// MatchRunner wiring must be applied here too.
    /// </para>
    /// Mirror of the EditMode lane SkirmishAiTests.
    /// </summary>
    [TestFixture]
    public sealed class SkirmishAiTests
    {
        /// <summary>Seed of the canonical AI match. Internal since D-101: CanonicalAiOutcomeTests pins its outcome.</summary>
        internal const ulong Seed = 0xA17E57DE57UL;
        private const byte HumanSlot = 0;
        private const byte AiSlot = 1;
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int EntityCapacity = 1024;
        private const long FieldReserveAE = 2000000L;

        /// <summary>
        /// End-to-end tick budget: this suite's deterministic match decides
        /// at tick 2705, so 6.000 ticks is a ~2.2x margin — comfortably sane,
        /// and exact because the whole loop is deterministic.
        /// </summary>
        internal const int EndToEndBudgetTicks = 6000;

        // ----------------------------------------------------------------
        // The AI host (mirror of MatchRunner's skirmish wiring)
        // ----------------------------------------------------------------

        internal sealed class AiHost
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public ProductionSystem Production;
            public FogOfWarSystem FogOfWar;
            public VictorySystem Victory;
            public MatchSession Session;
            public CommandIngress Ingress;
            public MatchSession AiSession;
            public CommandIngress AiIngress;
            public SkirmishAiSystem Ai;

            /// <summary>
            /// Mirror of <c>MatchRunner.StepFixedTick</c>: seal the batch due
            /// at the next tick through the (host) ingress — the AI peer's
            /// records were forwarded into the same ingress and are drained
            /// together with the human's — submit it, advance the AI peer
            /// clock to the tick about to execute (its intents then target
            /// T+1), step, and advance the human session.
            /// </summary>
            public void Step()
            {
                uint nextTick = Kernel.CurrentTick.Value + 1;
                CommandBatch batch = Ingress.SealTickBatch(nextTick);
                if (batch.Count > 0)
                {
                    Assert.That(Kernel.SubmitBatch(batch), Is.True,
                        $"kernel refused the sealed batch of tick {nextTick}");
                }
                AiSession.AdvanceTick();
                Kernel.StepTick();
                Session.AdvanceTick();
            }

            public void Run(int ticks)
            {
                for (int i = 0; i < ticks; i++)
                {
                    Step();
                }
            }

            /// <summary>Runs until the match is decided or the budget is exhausted; returns the decided tick (0 = undecided).</summary>
            public uint RunUntilDecided(int budgetTicks)
            {
                for (int i = 0; i < budgetTicks && !Victory.IsDecided; i++)
                {
                    Step();
                }
                return Victory.DecidedTick;
            }
        }

        /// <summary>
        /// The shipped profile with waves switched off (<c>waveSize</c> 1).
        /// <para>
        /// A test that wants to observe TARGET CHOICE has to switch the wave
        /// gate off, or it observes the gate: since behaviour revision 3 a
        /// unit waiting inside the staging ring gets no explicit AttackTarget
        /// at all. That the off setting exists is not a convenience here — it
        /// is the same property that lets the lab measure the rule one-sided
        /// (behaviour journal M001), used a second time.
        /// </para>
        /// </summary>
        private static AiProfile WavesOff()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            return new AiProfile(
                profileId: "test-waves-off",
                decisionTickInterval: shipped.DecisionTickInterval,
                placementSearchRadius: shipped.PlacementSearchRadius,
                powerReserve: 0,
                targetHarvesters: 2,
                harvesterQueueBatch: shipped.HarvesterQueueBatch,
                targetArmySize: 12,
                attackSquadThreshold: 6,
                infantryQueueBatch: shipped.InfantryQueueBatch,
                targetDamageWeight: shipped.TargetDamageWeight,
                targetThreatWeight: shipped.TargetThreatWeight,
                targetFinishWeight: shipped.TargetFinishWeight,
                targetDistanceWeight: shipped.TargetDistanceWeight,
                waveSize: 1,
                stagingDistanceCells: shipped.StagingDistanceCells,
                stagingToleranceCells: shipped.StagingToleranceCells,
                retreatHealthPercent: shipped.RetreatHealthPercent,
                retreatDangerCells: shipped.RetreatDangerCells,
                // Waves off means waves off: the strength gate is the same
                // rule and switches off with them.
                waveStrengthPoints: 0);
        }

        private static AiHost BuildAiHost(ulong seed, AiProfile? profile = null)
        {
            // Mirror of MatchRunner.InitializeMatch(seed, ..., enableSkirmishAi: true).
            var kernel = new SimulationKernel(new SimRandom(seed));

            var entities = new EntityManager(EntityCapacity);
            var pathfinding = new PathfindingSystem(MapWidth, MapHeight);
            var movement = new MovementSystem(entities, pathfinding);
            var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
            var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
            var production = new ProductionSystem(entities, economy, construction);
            var fogOfWar = new FogOfWarSystem(entities, construction, economy, teamCount: 2, MapWidth, MapHeight);
            var combat = new CombatSystem(entities, fogOfWar, economy, construction);
            var victory = new VictorySystem(entities, construction);

            var session = new MatchSession(HumanSlot, activeSlots: new byte[] { HumanSlot, AiSlot }, inputDelayTicks: 1);
            var ingress = new CommandIngress(session);
            _ = new LocalLoopbackTransport(ingress);

            var aiSession = new MatchSession(AiSlot, activeSlots: new byte[] { HumanSlot, AiSlot }, inputDelayTicks: 1);
            var aiIngress = new CommandIngress(aiSession);
            _ = new AiPeerCommandTransport(aiIngress, ingress);
            var ai = new SkirmishAiSystem(
                AiSlot,
                profile.HasValue
                    ? new AiFactionProfile("Legion", profile.Value)
                    : new AiFactionProfile("Legion",
                        targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2),
                aiIngress, entities, economy, construction, production, fogOfWar, victory);

            kernel.RegisterSystem(economy);
            kernel.RegisterSystem(construction);
            kernel.RegisterSystem(production);
            kernel.RegisterSystem(pathfinding);
            kernel.RegisterSystem(movement);
            kernel.RegisterSystem(fogOfWar);
            kernel.RegisterSystem(combat);
            kernel.RegisterSystem(ai);
            kernel.RegisterSystem(victory);
            kernel.BindCommands(
                new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

            // Faction assignment BEFORE Kernel.Start() (the SetSlotFaction
            // guard): slot 0 Alliance, slot 1 Legion — the canonical match.
            economy.SetSlotFaction(HumanSlot, FactionId.Alliance);
            economy.SetSlotFaction(AiSlot, FactionId.Legion);

            kernel.Start();
            return new AiHost
            {
                Kernel = kernel,
                Entities = entities,
                Economy = economy,
                Construction = construction,
                Production = production,
                FogOfWar = fogOfWar,
                Victory = victory,
                Session = session,
                Ingress = ingress,
                AiSession = aiSession,
                AiIngress = aiIngress,
                Ai = ai,
            };
        }

        /// <summary>
        /// The canonical D-077 opening for both slots (mirror of
        /// MatchBootstrap.StartGrayboxMatch with definition stats): per slot
        /// one Aetherium field, a completed HQ and ONE Builder — spawn order
        /// field, HQ, Builder, slot 0 first. Slot 0 receives NO commands for
        /// the whole match: it is the passive opponent of this suite.
        /// </summary>
        private static void ApplyOpeningPosition(AiHost host)
        {
            for (byte slot = 0; slot < 2; slot++)
            {
                ushort fieldId = (ushort)(slot + 1);
                int fieldCell = slot == HumanSlot ? 7 : 119;
                int hqOrigin = slot == HumanSlot ? 4 : 120;
                int builderX = slot == HumanSlot ? 13 : 113;
                int builderY = slot == HumanSlot ? 7 : 119;

                Assert.That(host.Economy.TryAddField(fieldId, new GridPos2D(fieldCell, fieldCell), FieldReserveAE),
                    Is.True, $"field {fieldId} could not be registered");

                FactionId faction = host.Economy.GetSlotFaction(slot);
                ushort hqDefId = SimDefinitions.ToDefinitionId(faction, UnitRole.HQ);
                Assert.That(host.Construction.PlaceCompletedBuilding(slot, hqDefId, hqOrigin, hqOrigin).IsValid,
                    Is.True, $"HQ placement failed for slot {slot}");

                Assert.That(SimDefinitions.TryGetUnit(faction, UnitRole.Builder, out SimUnitDefinition builderDef), Is.True);
                host.Entities.SpawnUnit(
                    slot,
                    new Transform2D(SimFixed.FromInt(builderX), SimFixed.FromInt(builderY)),
                    builderDef.MoveSpeed,
                    maxHealth: builderDef.MaxHealth,
                    role: UnitRole.Builder);
            }
        }

        internal static AiHost BuildMatch(ulong seed, AiProfile? profile = null)
        {
            AiHost host = BuildAiHost(seed, profile);
            ApplyOpeningPosition(host);
            return host;
        }

        // ----------------------------------------------------------------
        // Deterministic read helpers (ascending entity index)
        // ----------------------------------------------------------------

        private static int CountUnits(AiHost host, byte slot, UnitRole role)
        {
            int count = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.PlayerId == slot && u.Role == role) count++;
            }
            return count;
        }

        private static bool AnyHarvesterWorking(AiHost host, byte slot)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.PlayerId == slot && u.Role == UnitRole.Harvester
                    && (u.HarvestFieldId != 0 || u.IsReturningCargo))
                {
                    return true;
                }
            }
            return false;
        }

        private static int MinCombatCellX(AiHost host, byte slot)
        {
            int min = int.MaxValue;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                int x = SimFixed.WorldToGrid(u.Transform.PositionX);
                if (x < min) min = x;
            }
            return min;
        }

        // ----------------------------------------------------------------
        // (a) Build order through the canonical command path
        // ----------------------------------------------------------------

        [Test]
        public void SkirmishAi_PlacesRefineryPowerThenBarracks_ThroughTheSealedCommandPath()
        {
            AiHost host = BuildMatch(Seed);

            uint refineryTick = 0;
            uint powerTick = 0;
            uint barracksTick = 0;
            for (int i = 0; i < 1000 && barracksTick == 0; i++)
            {
                host.Step();
                uint tick = host.Kernel.CurrentTick.Value;
                if (refineryTick == 0 && host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Refinery)) refineryTick = tick;
                if (powerTick == 0 && host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Power)) powerTick = tick;
                if (barracksTick == 0 && host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Barracks)) barracksTick = tick;
            }

            Assert.Multiple(() =>
            {
                Assert.That(refineryTick, Is.GreaterThan(0u),
                    "the AI must place and complete its Refinery (D-077: no prerequisite) through PlaceBuilding intents");
                Assert.That(powerTick, Is.GreaterThan(refineryTick),
                    "D-103 requires the AI to complete a Power plant after the Refinery and before its Barracks");
                Assert.That(barracksTick, Is.GreaterThan(powerTick),
                    "the AI must complete the Barracks only after its required Power plant stands");
            });
            Assert.That(host.Construction.HasFinishedBuilding(HumanSlot, UnitRole.Refinery), Is.False,
                "slot 0 is the passive fixture: nobody issues orders for it");

            // The AIArchitecture proof: the AI's orders travelled the SAME
            // sealed command stream a human's would (Commands.md section 1) —
            // the host ingress sealed slot-1 records.
            Assert.That(host.Ingress.DedupeState.SealedWatermark(AiSlot), Is.GreaterThan(0u),
                "AI orders must enter through the canonical session/ingress intent path, not direct system calls");
        }

        [Test]
        public void SkirmishAi_DefinitionRoleSite_DoesNotCountAsCompletedOrAdvanceBuildOrder()
        {
            AiHost host = BuildMatch(Seed);

            // The tick-20 decision submits the Refinery, tick 21 creates its
            // site, and tick 40 is the first decision that must classify that
            // definition-role entity through the site register. A bare role
            // check queues a second (Power) site for tick 41 under D-103.
            host.Run(41);

            Assert.That(host.Construction.SiteCount, Is.EqualTo(1),
                "an unfinished Refinery is the active build, not a completed producer that unlocks Barracks");
            Assert.That(host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Refinery), Is.False);
            Assert.That(host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Power), Is.False);
            Assert.That(host.Construction.HasFinishedBuilding(AiSlot, UnitRole.Barracks), Is.False);

            UnitState[] units = host.Entities.RawUnits;
            int definitionRoleSites = 0;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != AiSlot || !host.Construction.IsActiveSite(unit.Id)) continue;
                definitionRoleSites++;
                Assert.That(unit.Role, Is.EqualTo(UnitRole.Refinery),
                    "the sole site carries the Refinery role without becoming a finished Refinery");
            }
            Assert.That(definitionRoleSites, Is.EqualTo(1));
        }

        // ----------------------------------------------------------------
        // (b) Economy: harvesters work the field, credits recover
        // ----------------------------------------------------------------

        [Test]
        public void SkirmishAi_QueuesHarvesters_AndHarvestIncomeRefillsTheTreasury()
        {
            AiHost host = BuildMatch(Seed);

            host.Run(800);
            long creditsAfterOpening = host.Economy.GetPlayerEconomy(AiSlot).AetheriumCredits;

            host.Run(1200); // tick 2000

            long creditsLater = host.Economy.GetPlayerEconomy(AiSlot).AetheriumCredits;
            Assert.That(creditsLater, Is.GreaterThan(creditsAfterOpening),
                $"after the opening spend the harvest cycle must refill the treasury " +
                $"({creditsAfterOpening} AE at tick 800 -> {creditsLater} AE at tick 2000)");
            Assert.That(CountUnits(host, AiSlot, UnitRole.Harvester), Is.GreaterThanOrEqualTo(1),
                "the AI queues harvesters at the completed Refinery (D-077 producer assignment)");
            Assert.That(AnyHarvesterWorking(host, AiSlot), Is.True,
                "idle harvesters must receive explicit Harvest intents on the own field");
        }

        // ----------------------------------------------------------------
        // (c) Army: infantry production up to the threshold, then the march
        // ----------------------------------------------------------------

        [Test]
        public void SkirmishAi_ProducesInfantry_AndMarchesPastMidMap()
        {
            AiHost host = BuildMatch(Seed);

            host.Run(2500);

            Assert.That(CountUnits(host, AiSlot, UnitRole.BasicInfantry), Is.GreaterThanOrEqualTo(6),
                "the Barracks keeps infantry queued — the attack threshold of the profile must be reachable");
            Assert.That(MinCombatCellX(host, AiSlot), Is.LessThan(64),
                "at the squad threshold the army marches toward the enemy start area (slot 1 starts at x ~ 113-122)");
        }

        // ----------------------------------------------------------------
        // (d) END-TO-END: the AI defeats a passive slot 0
        // ----------------------------------------------------------------

        [Test]
        public void SkirmishAi_EndToEnd_EliminatesPassiveOpponentWithinBudget()
        {
            AiHost host = BuildMatch(Seed);

            uint decidedTick = host.RunUntilDecided(EndToEndBudgetTicks);

            Assert.That(host.Victory.IsDecided, Is.True,
                $"the AI must finish the match within {EndToEndBudgetTicks} ticks (undecided after the budget)");
            Assert.That(host.Victory.Outcome, Is.EqualTo(MatchOutcome.VictoryElimination),
                "a lone surviving side is the D-056 elimination victory");
            Assert.That(host.Victory.WinnerSlot, Is.EqualTo((byte)AiSlot),
                "the AI (slot 1) must be the winner against the passive fixture");
            Assert.That(decidedTick, Is.GreaterThan(0u),
                "a decided match carries the deciding tick");
            TestContext.Out.WriteLine($"[SkirmishAiTests] end-to-end decided at tick {decidedTick} " +
                                      $"(budget {EndToEndBudgetTicks})");
        }

        // ----------------------------------------------------------------
        // (e) Determinism: identical runs decide identically
        // ----------------------------------------------------------------

        [Test]
        public void SkirmishAi_EndToEnd_IsByteDeterministicAcrossRuns()
        {
            AiHost first = BuildMatch(Seed);
            AiHost second = BuildMatch(Seed);

            uint firstDecided = first.RunUntilDecided(EndToEndBudgetTicks);
            uint secondDecided = second.RunUntilDecided(EndToEndBudgetTicks);

            Assert.That(firstDecided, Is.EqualTo(secondDecided),
                "two identical AI matches must decide on the same tick");
            Assert.That(first.Victory.Outcome, Is.EqualTo(second.Victory.Outcome));
            Assert.That(first.Kernel.CalculateStateHash(), Is.EqualTo(second.Kernel.CalculateStateHash()),
                "the full AI loop (intents through the sealed stream) must reproduce the identical end state");
        }

        // ----------------------------------------------------------------
        // (e) The behaviour identifier — the guard against a silent change
        // ----------------------------------------------------------------

        /// <summary>
        /// Pins <see cref="AiBehaviorId"/> — the identifier that says WHICH AI
        /// this is. It carries the profile hash and the revision, so it catches
        /// changed numbers, and the revision catches a changed rule that the
        /// author declared.
        /// <para>
        /// WHEN THIS GOES RED: the AI's profile numbers or its revision moved.
        /// Bump <c>AiBehaviorId.Revision</c>, add its line to the history in
        /// that file, write the journal entry in
        /// <c>tools/Nova.AiLab/reports/behavior-log.md</c> — measured values,
        /// better AND worse — and only then update the string below.
        /// </para>
        /// <para>
        /// THE OTHER HALF LIVES ELSEWHERE (D-101, 2026-08-09). This pin used to
        /// also assert the decided tick and the end-state hash of the canonical
        /// AI match. Those two numbers move on ANY simulation change, not only
        /// on an AI change — every package of Sprint 16 tripped them — and the
        /// procedure above then pointed the wrong strand at the wrong journal.
        /// They now live in <c>CanonicalAiOutcomeTests</c>, which the maintainer
        /// strand owns. The diagnosis stays intact because that test reads this
        /// identifier: outcome moved AND identifier moved means the AI changed;
        /// outcome moved and identifier unchanged means the simulation under it
        /// changed.
        /// </para>
        /// <para>
        /// This is NOT one of the four determinism baselines and must not be
        /// treated as one: those live in their own files and separate a
        /// behaviour PR from a baseline PR. This pin belongs to the behaviour
        /// change and is updated in the same commit as the revision bump.
        /// </para>
        /// </summary>
        [Test]
        public void AiBehaviorId_TracksWhichAiThisIs()
        {
            // r6 MOVED THE IDENTIFIER AND NOT THE MATCH, and that is worth one
            // sentence rather than a shrug. The strength gate replaces the head
            // count, but its threshold is capped at what production can still
            // deliver — and with the shipped army cap at twelve that ceiling
            // binds first on both seats (twelve Alliance riflemen ARE 1.200
            // points; twelve Legion recruits are 528 and the cap allows no
            // thirteenth). So the gate ships decoupled and dormant, and the
            // number it unblocks is a later PR's business.
            //
            // That this is visible at all is what D-101 bought: the match
            // numbers live in CanonicalAiOutcomeTests now. Identifier moved and
            // outcome unmoved is exactly the "declared change, no effect yet"
            // case — under the old coupled pin it was indistinguishable from a
            // simulation change.
            Assert.That(AiBehaviorId.Value, Is.EqualTo("r7.E34435F9"),
                "the AI identifier changed — bump the revision and write the journal entry");
        }

        // ----------------------------------------------------------------
        // (f) Target choice is a score, not the order of the visible list
        // ----------------------------------------------------------------

        /// <summary>
        /// Two enemies appear next to the army at the same moment and at
        /// practically the same distance: an unarmed Harvester spawned FIRST,
        /// and a BattleTank spawned second. The army has to shoot the tank.
        /// <para>
        /// This test is written so that the PREVIOUS rule would fail it. That
        /// rule took the first non-building entity out of the visibility list,
        /// which is an ascending entity scan — so the harvester, spawned
        /// first, would have won on nothing but its lower index.
        /// </para>
        /// <para>
        /// The margin is deliberately not marginal. Legion infantry deals 8
        /// kinetic; against the tank's Heavy armor that resolves to 2 and
        /// against the harvester's Light armor to 6, so the damage term even
        /// favours the harvester (60 against 20). The threat term decides it:
        /// the tank hits back for 60, the harvester for nothing, which at
        /// weight 6 is 360 against 0. A wrong target here is a wrong ORDER of
        /// terms, not a rounding difference — and that is what makes the
        /// assertion worth having.
        /// </para>
        /// <para>
        /// Why this test exists at all: the end-to-end test above kept passing
        /// while target selection changed and the match decided 4.260 ticks
        /// earlier. It asserts outcome and winner, never the choice.
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_ShootsTheDangerousTarget_NotTheFirstOneInTheVisibleList()
        {
            AiHost host = BuildMatch(Seed, WavesOff());

            // Waves are OFF for this one — see WavesOff(). What is under test
            // is the SCORE, and the wave gate of revision 3 would hide it: a
            // unit waiting inside the staging ring carries no explicit
            // AttackTarget on purpose. Measuring the gate here instead of the
            // score would be the quiet kind of wrong test, the one that stays
            // green for the wrong reason.
            const int SquadThreshold = 6;
            int budget = EndToEndBudgetTicks;
            while (budget-- > 0 && CountUnits(host, AiSlot, UnitRole.BasicInfantry) < SquadThreshold)
            {
                host.Step();
            }
            Assert.That(CountUnits(host, AiSlot, UnitRole.BasicInfantry), Is.GreaterThanOrEqualTo(SquadThreshold),
                "the AI never reached its attack squad, so it never chose a target");

            Assert.That(TryFirstCombatCell(host, AiSlot, out int armyX, out int armyY), Is.True);

            // The army as it stands NOW. Infantry keeps rolling out of the
            // Barracks, and a unit born after this point auto-acquires a
            // target of its own (D-087) before the next decision reaches it —
            // that is correct behaviour and not what this test is about.
            List<EntityId> army = CombatUnitIds(host, AiSlot);

            // Order matters: the harvester takes the LOWER entity index, which
            // is exactly the advantage the old rule handed out.
            EntityId harvester = SpawnEnemyUnit(host, UnitRole.Harvester, armyX + 3, armyY);
            EntityId tank = SpawnEnemyUnit(host, UnitRole.BattleTank, armyX + 3, armyY + 1);
            Assert.That(harvester.Index, Is.LessThan(tank.Index),
                "the test only discriminates while the harmless target is seen first");

            RunToDecisionWithSquad(host, SquadThreshold);

            Assert.That(ArmyAttackTarget(host, army), Is.EqualTo(tank),
                "the army must shoot what actually threatens it, not what it happened to see first");
        }

        [Test]
        public void SkirmishAi_IgnoresDefinitionRoleHqSite_AndTargetsLegalEnemy()
        {
            AiHost host = BuildMatch(Seed, WavesOff());
            const int SquadThreshold = 6;
            int budget = EndToEndBudgetTicks;
            while (budget-- > 0 && CountUnits(host, AiSlot, UnitRole.BasicInfantry) < SquadThreshold)
            {
                host.Step();
            }
            Assert.That(TryFirstCombatCell(host, AiSlot, out int armyX, out int armyY), Is.True);
            List<EntityId> army = CombatUnitIds(host, AiSlot);

            // 16.4: the passive slot has decayed to its 2.000-AE HQ ceiling,
            // below the 2.500-AE HQ-site cost. A completed Storage raises the
            // derived cap and the capped deposit funds this focused fixture.
            ushort storageDefId = SimDefinitions.ToDefinitionId(
                host.Economy.GetSlotFaction(HumanSlot), UnitRole.Storage);
            Assert.That(host.Construction.PlaceCompletedBuilding(HumanSlot, storageDefId, 60, 60).IsValid, Is.True);
            Assert.That(host.Economy.DepositCapped(HumanSlot, 1000), Is.EqualTo(1000));

            EntityId hqSite = PlaceEnemySiteNear(host, UnitRole.HQ, armyX + 3, armyY);
            EntityId legalTarget = SpawnEnemyUnit(host, UnitRole.BattleTank, armyX + 3, armyY + 1);
            RunToDecisionWithSquad(host, SquadThreshold);

            Assert.That(host.Construction.IsActiveSite(hqSite), Is.True,
                "the HQ-role entity is still only a site");
            Assert.That(ArmyAttackTarget(host, army), Is.EqualTo(legalTarget),
                "the HQ short-circuit must not select a site that Combat rejects");
        }

        // ----------------------------------------------------------------
        // (g) Waves: reinforcements wait, the army marches at full strength
        // ----------------------------------------------------------------

        /// <summary>
        /// The wave rule of behaviour revision 3, stated as the two halves a
        /// player would describe: <b>nobody leaves alone</b>, and <b>at full
        /// strength everybody leaves</b>.
        /// <para>
        /// The first half is what makes the test worth having. With waves off
        /// the army marches at the squad threshold of six, so units DO leave
        /// the staging ring long before twelve exist — the assertion would
        /// fail on the previous behaviour, which is the only way a test of a
        /// new rule proves anything.
        /// </para>
        /// <para>
        /// Both halves are read off the committed state (positions), never off
        /// the intents: what matters is where the units end up, not what was
        /// submitted. An intent that is rejected or overwritten would still
        /// look right in a submission count.
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_KeepsReinforcementsHomeUntilTheWaveIsFull()
        {
            AiHost host = BuildMatch(Seed);
            AiProfile shipped = AiProfiles.Ms1Canonical;
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True,
                "without the AI's HQ there is no staging ring to measure against");

            int ticksWithAnArmyBelowTheWave = 0;
            for (int i = 0; i < EndToEndBudgetTicks && CountCombatUnits(host, AiSlot) < shipped.WaveSize; i++)
            {
                host.Step();
                if (CountCombatUnits(host, AiSlot) == 0) continue;
                ticksWithAnArmyBelowTheWave++;
                Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.LessThanOrEqualTo(ring),
                    $"a unit left the staging ring at tick {host.Kernel.CurrentTick.Value} while the wave was " +
                    $"still short of {shipped.WaveSize} — that is the trickle the rule exists to stop");
            }

            Assert.That(ticksWithAnArmyBelowTheWave, Is.GreaterThan(0),
                "the AI never held an incomplete army, so the waiting half was never observed");
            Assert.That(CountCombatUnits(host, AiSlot), Is.GreaterThanOrEqualTo(shipped.WaveSize),
                "the AI never assembled a full wave inside the budget");

            bool marched = false;
            for (int i = 0; i < EndToEndBudgetTicks && !marched; i++)
            {
                host.Step();
                marched = FarthestCombatDistance(host, AiSlot, hqX, hqY) > ring;
            }
            Assert.That(marched, Is.True,
                "the wave was full and the army still did not leave — waiting without marching is a deadlock");
        }

        // ----------------------------------------------------------------
        // (h) Retreat: a wounded unit turns back instead of dying in place
        // ----------------------------------------------------------------

        /// <summary>
        /// A battle tank appears beside the AI's army and starts shooting.
        /// Every unit it wounds below the retreat threshold has to be walking
        /// TOWARD its own base — not still walking at the tank.
        /// <para>
        /// The assertion is on the committed positions and the standing move
        /// order, never on submitted intents: what a player sees is where the
        /// units go. And it is deliberately phrased as "closer to the own HQ
        /// than the unit itself stands", not as the exact staging cell —
        /// the cell is an implementation detail, turning back is the
        /// behaviour.
        /// </para>
        /// <para>
        /// This test exists because <see cref="AiBehaviorId_TracksWhatTheAiActuallyDoes"/>
        /// CANNOT see this rule: its opponent slot is passive and owns no
        /// armed unit, so no threat is ever visible and no unit ever retreats.
        /// The pinned end state stayed byte-identical across the change, which
        /// is a pin doing its job and an argument for a second test, not
        /// against one.
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_PullsWoundedUnitsBackTowardTheirOwnBase()
        {
            AiHost host = BuildMatch(Seed);
            AiProfile shipped = AiProfiles.Ms1Canonical;
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);

            // The wave has to be OUT for this to be observable at all: a unit
            // wounded while it still waits at home is already where a retreat
            // would send it, and "turned back" has no meaning there.
            int budget = EndToEndBudgetTicks;
            while (budget-- > 0 && FarthestCombatDistance(host, AiSlot, hqX, hqY) <= ring)
            {
                host.Step();
            }
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.GreaterThan(ring),
                "the army never marched, so nothing could turn back");

            Assert.That(TryFirstCombatUnit(host, AiSlot, out EntityId woundedId, out int armyX, out int armyY),
                Is.True);
            Assert.That(host.Entities.TryGetUnit(woundedId, out UnitState marching), Is.True);
            Assert.That(marching.TargetGridPos.IsValid, Is.True, "the subject has to be marching somewhere");

            // An armed enemy inside the danger radius — the DANGER half of
            // the rule. A harvester here would (correctly) trigger nothing.
            //
            // Placed AHEAD of the unit, in its direction of travel, and two
            // failures paid for that detail. Behind it, the unit outruns the
            // radius before the next decision and the rule correctly does
            // nothing. Beside it as a tank, a 55-hitpoint infantryman does not
            // survive a 60-damage shell and there is nobody left to decide
            // about. Ahead at exactly retreatDangerCells the enemy is inside
            // the AI's danger radius (8) and outside its own rifle's reach
            // (7 tiles) at the moment of spawning.
            int aheadX = armyX + System.Math.Sign(marching.TargetGridPos.X - armyX) * shipped.RetreatDangerCells;
            int aheadY = armyY + System.Math.Sign(marching.TargetGridPos.Y - armyY) * shipped.RetreatDangerCells;
            SpawnEnemyUnit(host, UnitRole.BasicInfantry, aheadX, aheadY);

            // And the WOUND, written straight into the state instead of shot
            // in. That is deliberate: this test asks what the AI decides about
            // a wounded unit, not whether a tank can hit one. Letting the tank
            // do it made the test depend on cooldowns, armour classes and how
            // long the army stays in range — three things it is not about, and
            // the first version failed on exactly that without saying so.
            ref UnitState target = ref host.Entities.GetUnitRef(woundedId);
            target.CurrentHealth = target.MaxHealth * (shipped.RetreatHealthPercent - 20) / 100;
            int startedAt = ChebyshevTo(
                SimFixed.WorldToGrid(target.Transform.PositionX),
                SimFixed.WorldToGrid(target.Transform.PositionY), hqX, hqY);

            // To the next decision and two ticks further, so the sealed
            // intent has landed. Not further: the unit keeps walking, and a
            // long window would let it leave the danger radius on its own and
            // turn a real answer into a coin toss.
            RunToNextDecision(host);

            Assert.That(host.Entities.TryGetUnit(woundedId, out UnitState after), Is.True,
                "the wounded unit vanished, so there is nothing to read");
            Assert.That(after.TargetGridPos.IsValid, Is.True,
                "the wounded unit carries no move order at all — it was neither sent home nor sent on");

            int goingTo = ChebyshevTo(after.TargetGridPos.X, after.TargetGridPos.Y, hqX, hqY);
            Assert.That(goingTo, Is.LessThan(startedAt),
                "a unit under the retreat threshold with an armed enemy beside it is still walking " +
                "away from its own base — that is the behaviour this rule exists to end");
        }

        /// <summary>
        /// The wave has to launch with what production can still deliver.
        /// <para>
        /// The probe profile is the whole test: army cap 4, wave 3. The first
        /// three gather and march, and they never come back inside the ring —
        /// the cap counts them, so the barracks refills to exactly one unit at
        /// home. A wave size of three can never be reached again.
        /// </para>
        /// <para>
        /// NEGATIVE CONTROL BY CONSTRUCTION: on the previous rule
        /// (<c>gathered &gt;= waveSize</c>) the fourth unit stands at the
        /// staging cell until the time limit and the count outside the ring
        /// stays at three forever, so this assertion fails. The shipped profile
        /// hides the defect because <c>waveSize == targetArmySize == 12</c>
        /// only stalls once a wave has taken losses — which the passive
        /// opponent of the pinned end-to-end run never inflicts.
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_LaunchesTheNextWave_WhenTheArmyCapCannotRefillIt()
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            var probe = new AiProfile(
                profileId: "wave-refill-probe",
                decisionTickInterval: shipped.DecisionTickInterval,
                placementSearchRadius: shipped.PlacementSearchRadius,
                powerReserve: shipped.PowerReserve,
                targetHarvesters: shipped.TargetHarvesters,
                harvesterQueueBatch: shipped.HarvesterQueueBatch,
                targetArmySize: 4,
                attackSquadThreshold: 1,
                infantryQueueBatch: shipped.InfantryQueueBatch,
                targetDamageWeight: shipped.TargetDamageWeight,
                targetThreatWeight: shipped.TargetThreatWeight,
                targetFinishWeight: shipped.TargetFinishWeight,
                targetDistanceWeight: shipped.TargetDistanceWeight,
                waveSize: 3,
                stagingDistanceCells: shipped.StagingDistanceCells,
                stagingToleranceCells: shipped.StagingToleranceCells,
                retreatHealthPercent: 0,
                retreatDangerCells: shipped.RetreatDangerCells,
                // The COUNT path on purpose — this test pins the r5 rule, and
                // the strength path has its own test.
                waveStrengthPoints: 0);

            AiHost host = BuildMatch(Seed, probe);
            int ring = probe.StagingDistanceCells + probe.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True,
                "without the AI's HQ there is no staging ring to measure against");

            int outsideAtBest = 0;
            for (int i = 0; i < EndToEndBudgetTicks && !host.Victory.IsDecided; i++)
            {
                host.Step();
                int outside = CountCombatUnitsOutsideRing(host, AiSlot, hqX, hqY, ring);
                if (outside > outsideAtBest) outsideAtBest = outside;
                if (outsideAtBest > probe.WaveSize) break;
            }

            Assert.That(outsideAtBest, Is.GreaterThan(probe.WaveSize),
                $"never more than {probe.WaveSize} units left the staging ring. The wave that already " +
                "marched can never come back, so a wave threshold fixed at the wave size can never be " +
                "met again — the reinforcement stands at the staging cell until the time limit while " +
                "the units that did march hold the front alone");
        }

        /// <summary>
        /// The wave marches on STRENGTH, not on a head count — and the AI seat
        /// of this fixture is the Legion, which is exactly where the difference
        /// lives.
        /// <para>
        /// Twelve Legion recruits weigh 528 combat points; twelve Alliance
        /// riflemen weigh 1.200. The head count calls both "a full wave of
        /// twelve", so the Legion attacks at 44 % of the strength the same rule
        /// hands the Alliance. With <c>waveStrengthPoints</c> at 1.200 the
        /// Legion has to gather more recruits before it may march.
        /// </para>
        /// <para>
        /// THE ARMY CAP IS RAISED TO 36 IN THE PROBE, and both the raise and
        /// its size are load-bearing. The threshold is capped at what
        /// production can still deliver, so at the shipped cap of twelve the
        /// ceiling binds first and the two paths decide identically — which is
        /// precisely why the shipped profile's end state is unchanged in
        /// <see cref="AiBehaviorId_TracksWhatTheAiActuallyDoes"/>. A probe at
        /// the shipped cap would pass on r5 as well and prove nothing.
        /// <para>
        /// 36 rather than 24 because 1.200 points are 28 recruits: a cap of 24
        /// can hold at most 1.056 points, so the ceiling would STILL bind and
        /// the test would measure "the wave waits for a full army cap" while
        /// claiming to measure the threshold. That is what the second assertion
        /// below exists to keep out.
        /// </para>
        /// </para>
        /// <para>
        /// NEGATIVE CONTROL: the two runs differ in ONE profile value and
        /// nothing else. On the count path (<c>waveStrengthPoints</c> 0, which
        /// is r5's only behaviour) both runs launch at the same twelve units,
        /// the two numbers below are equal and the assertion fails. That the
        /// off value exists is what makes this comparison possible at all —
        /// the same property the lab uses to measure one-sided (M001).
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_GathersMoreOfTheWeakerUnit_WhenTheWaveIsMeasuredInStrength()
        {
            int atCountGate = CombatUnitsAtFirstLaunch(StrengthGateProbe(waveStrengthPoints: 0));
            int atStrengthGate = CombatUnitsAtFirstLaunch(StrengthGateProbe(waveStrengthPoints: 1200));

            Assert.Multiple(() =>
            {
                Assert.That(atCountGate, Is.GreaterThan(0),
                    "the count path never launched a wave at all — the probe profile is broken, " +
                    "not the rule under test");
                Assert.That(atCountGate, Is.LessThanOrEqualTo(StrengthGateProbe(0).WaveSize + 2),
                    "the count path is supposed to march at the wave size (plus at most one queued " +
                    "batch that popped in the same decision)");
                Assert.That(atStrengthGate, Is.GreaterThan(atCountGate),
                    "the Legion marched with the same number of recruits whether the wave was measured " +
                    "in heads or in combat points — twelve recruits are 528 points against the 1.200 " +
                    "the threshold asks for, so the strength path has to keep gathering");

                // AND IT WAS THE THRESHOLD THAT RELEASED IT, not an exhausted
                // army cap. Without this line the test passes just as happily
                // when waveStrengthPoints is ignored altogether and the wave
                // simply waits for the cap to fill — which is what an earlier
                // version of this probe measured without saying so, because its
                // cap of 24 could not hold 1.200 points of recruits in the
                // first place. 1.200 points are 28 recruits; the cap is 36.
                Assert.That(atStrengthGate, Is.LessThan(StrengthGateProbe(0).TargetArmySize),
                    "the wave only left once the army cap was full, so the point threshold decided " +
                    "nothing — the profile value is not being read as a threshold at all");
            });
        }

        /// <summary>
        /// The probe for the strength gate: the shipped profile with the army
        /// cap raised to 24 (see the test's remarks for why), retreat off so
        /// nothing pulls a unit back over the ring while we are counting, and
        /// the one value under test passed in.
        /// </summary>
        private static AiProfile StrengthGateProbe(int waveStrengthPoints)
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            return new AiProfile(
                profileId: waveStrengthPoints > 0 ? "strength-gate-probe" : "count-gate-probe",
                decisionTickInterval: shipped.DecisionTickInterval,
                placementSearchRadius: shipped.PlacementSearchRadius,
                powerReserve: shipped.PowerReserve,
                targetHarvesters: shipped.TargetHarvesters,
                harvesterQueueBatch: shipped.HarvesterQueueBatch,
                targetArmySize: 36,
                attackSquadThreshold: shipped.AttackSquadThreshold,
                infantryQueueBatch: shipped.InfantryQueueBatch,
                targetDamageWeight: shipped.TargetDamageWeight,
                targetThreatWeight: shipped.TargetThreatWeight,
                targetFinishWeight: shipped.TargetFinishWeight,
                targetDistanceWeight: shipped.TargetDistanceWeight,
                waveSize: shipped.WaveSize,
                stagingDistanceCells: shipped.StagingDistanceCells,
                stagingToleranceCells: shipped.StagingToleranceCells,
                retreatHealthPercent: 0,
                retreatDangerCells: shipped.RetreatDangerCells,
                waveStrengthPoints: waveStrengthPoints);
        }

        /// <summary>
        /// Living combat units the AI owns at the moment the FIRST of them
        /// leaves the staging ring; 0 if no wave ever launched.
        /// <para>
        /// The count is taken at the launch tick and not at the decision that
        /// ordered it, because the decision is not observable from outside the
        /// system — a unit crossing the ring is. Nothing has been lost by then:
        /// the first wave of the match walks into an empty map.
        /// </para>
        /// </summary>
        private static int CombatUnitsAtFirstLaunch(AiProfile probe)
        {
            AiHost host = BuildMatch(Seed, probe);
            int ring = probe.StagingDistanceCells + probe.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True,
                "without the AI's HQ there is no staging ring to measure against");

            for (int i = 0; i < EndToEndBudgetTicks && !host.Victory.IsDecided; i++)
            {
                host.Step();
                if (CountCombatUnitsOutsideRing(host, AiSlot, hqX, hqY, ring) > 0)
                {
                    return CountCombatUnits(host, AiSlot);
                }
            }
            return 0;
        }

        /// <summary>
        /// A retreating unit has to carry a target it can actually act on.
        /// <para>
        /// Same scene as <see cref="SkirmishAi_PullsWoundedUnitsBackTowardTheirOwnBase"/>,
        /// a different question: not WHERE the wounded unit walks but WHAT it
        /// is aimed at while it walks. It marched with the army's target — a
        /// building on the far side of the map — and the retreat branch used to
        /// submit no attack intent at all. Submitting nothing does not release
        /// a standing order: <c>UnitState.Stop()</c> leaves <c>AttackTarget</c>
        /// alone, <c>ApplyMove</c> only calls <c>SetTarget</c>, and
        /// <c>CombatSystem</c> releases a target only when it dies. The unit
        /// therefore walked home still aimed at something unreachable, and the
        /// D-087 auto-acquisition skipped it for holding a valid target.
        /// </para>
        /// <para>
        /// NEGATIVE CONTROL: on the previous rule the assertion below reads the
        /// army's march target, not the tank standing next to the unit.
        /// </para>
        /// </summary>
        [Test]
        public void SkirmishAi_AimsARetreatingUnitAtItsPursuer_NotAtWhatItWalkedAwayFrom()
        {
            AiHost host = BuildMatch(Seed);
            AiProfile shipped = AiProfiles.Ms1Canonical;
            int ring = shipped.StagingDistanceCells + shipped.StagingToleranceCells;

            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);

            int budget = EndToEndBudgetTicks;
            while (budget-- > 0 && FarthestCombatDistance(host, AiSlot, hqX, hqY) <= ring)
            {
                host.Step();
            }
            Assert.That(FarthestCombatDistance(host, AiSlot, hqX, hqY), Is.GreaterThan(ring),
                "the army never marched, so nothing could turn back");

            ushort powerDefId = SimDefinitions.ToDefinitionId(
                host.Economy.GetSlotFaction(HumanSlot), UnitRole.Power);
            Assert.That(host.Construction.PlaceCompletedBuilding(HumanSlot, powerDefId, 60, 60).IsValid, Is.True);
            host.Step(); // commit the new completed Power plant to the energy balance

            Assert.That(TryFirstCombatUnit(host, AiSlot, out EntityId woundedId, out int armyX, out int armyY),
                Is.True);
            Assert.That(host.Entities.TryGetUnit(woundedId, out UnitState marching), Is.True);
            Assert.That(marching.TargetGridPos.IsValid, Is.True, "the subject has to be marching somewhere");

            int aheadX = armyX + System.Math.Sign(marching.TargetGridPos.X - armyX) * shipped.RetreatDangerCells;
            int aheadY = armyY + System.Math.Sign(marching.TargetGridPos.Y - armyY) * shipped.RetreatDangerCells;
            EntityId inertSite = PlaceEnemySiteNear(host, UnitRole.DefensePlatform, aheadX, aheadY);
            Assert.That(host.Entities.TryGetUnit(inertSite, out UnitState siteState), Is.True);
            int siteX = SimFixed.WorldToGrid(siteState.Transform.PositionX);
            int siteY = SimFixed.WorldToGrid(siteState.Transform.PositionY);
            EntityId pursuer = SpawnEnemyUnit(host, UnitRole.BasicInfantry, siteX, siteY);

            ref UnitState target = ref host.Entities.GetUnitRef(woundedId);
            target.CurrentHealth = target.MaxHealth * (shipped.RetreatHealthPercent - 20) / 100;

            RunToNextDecision(host);

            Assert.That(host.Entities.TryGetUnit(woundedId, out UnitState after), Is.True,
                "the wounded unit vanished, so there is nothing to read");
            Assert.That(after.AttackTarget, Is.EqualTo(pursuer),
                "a retreating unit is still aimed at the target it marched away from. It cannot reach it, " +
                "and holding a valid target is exactly what makes the D-087 auto-acquisition skip the unit — " +
                "so it fires at nothing the whole way home and defends nothing once there");
            Assert.That(after.AttackTarget, Is.Not.EqualTo(inertSite),
                "an armed DefensePlatform role is not a threat while its entity remains a site");
        }

        /// <summary>The slot's combat units standing outside the staging ring around the given cell.</summary>
        private static int CountCombatUnitsOutsideRing(AiHost host, byte slot, int cellX, int cellY, int ring)
        {
            int count = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                int distance = ChebyshevTo(
                    SimFixed.WorldToGrid(u.Transform.PositionX),
                    SimFixed.WorldToGrid(u.Transform.PositionY), cellX, cellY);
                if (distance > ring) count++;
            }
            return count;
        }

        /// <summary>Steps to just past the next decision tick, so the sealed intent has been applied.</summary>
        private static void RunToNextDecision(AiHost host)
        {
            ushort cadence = host.Ai.DecisionTickInterval;
            for (int i = 0; i < cadence; i++)
            {
                host.Step();
                if ((host.Kernel.CurrentTick.Value % cadence) != 0) continue;
                host.Run(2);
                return;
            }
            Assert.Fail("no decision tick inside one cadence — the cadence is not what it says it is");
        }

        /// <summary>The slot's first combat unit (ascending index) with its id and cell.</summary>
        private static bool TryFirstCombatUnit(AiHost host, byte slot, out EntityId id, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                id = u.Id;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            id = EntityId.Invalid;
            cellX = 0;
            cellY = 0;
            return false;
        }

        private static int ChebyshevTo(int fromX, int fromY, int toX, int toY)
        {
            int dx = System.Math.Abs(fromX - toX);
            int dy = System.Math.Abs(fromY - toY);
            return dx > dy ? dx : dy;
        }

        /// <summary>Chebyshev distance of the combat unit standing farthest from <paramref name="cellX"/>/<paramref name="cellY"/>; -1 without any.</summary>
        private static int FarthestCombatDistance(AiHost host, byte slot, int cellX, int cellY)
        {
            int farthest = -1;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                int dx = System.Math.Abs(SimFixed.WorldToGrid(u.Transform.PositionX) - cellX);
                int dy = System.Math.Abs(SimFixed.WorldToGrid(u.Transform.PositionY) - cellY);
                int distance = dx > dy ? dx : dy;
                if (distance > farthest) farthest = distance;
            }
            return farthest;
        }

        /// <summary>The slot's HQ cell (ascending scan, first match).</summary>
        private static bool TryHqCell(AiHost host, byte slot, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot || u.Role != UnitRole.HQ) continue;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        /// <summary>
        /// Steps to just past a decision tick at which the AI actually holds
        /// its attack squad.
        /// <para>
        /// Waiting a fixed number of ticks is not enough, and the reason is a
        /// finding in its own right: while the army is BELOW its marching gate
        /// the AI issues no AttackTarget at all, so what its units carry is
        /// D-087 auto-acquisition — the NEAREST visible hostile, harmless or
        /// not. Measured in this scenario: with five units, four shoot the
        /// harvester while a battle tank stands one cell away. Since revision 3
        /// that gate is the wave size, not the squad threshold.
        /// </para>
        /// </summary>
        private static void RunToDecisionWithSquad(AiHost host, int squadThreshold)
        {
            ushort cadence = host.Ai.DecisionTickInterval;
            const int Budget = 2000;
            for (int i = 0; i < Budget; i++)
            {
                host.Step();
                if (CountCombatUnits(host, AiSlot) >= squadThreshold
                    && (host.Kernel.CurrentTick.Value % cadence) == 0)
                {
                    host.Run(2); // the sealed intent lands on the following tick
                    return;
                }
            }
            Assert.Fail($"the AI never held {squadThreshold} combat units on a decision tick within {Budget} ticks");
        }

        private static int CountCombatUnits(AiHost host, byte slot)
        {
            int count = 0;
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                count++;
            }
            return count;
        }

        private static List<EntityId> CombatUnitIds(AiHost host, byte slot)
        {
            var ids = new List<EntityId>();
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                ids.Add(u.Id);
            }
            return ids;
        }

        /// <summary>Cell of the lowest-indexed living combat unit of a slot.</summary>
        private static bool TryFirstCombatCell(AiHost host, byte slot, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != slot) continue;
                if (u.Role < UnitRole.BasicInfantry || u.Role > UnitRole.Artillery) continue;
                cellX = SimFixed.WorldToGrid(u.Transform.PositionX);
                cellY = SimFixed.WorldToGrid(u.Transform.PositionY);
                return true;
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        private static EntityId SpawnEnemyUnit(AiHost host, UnitRole role, int cellX, int cellY)
        {
            FactionId faction = host.Economy.GetSlotFaction(HumanSlot);
            Assert.That(SimDefinitions.TryGetUnit(faction, role, out SimUnitDefinition def), Is.True,
                $"{faction} has no {role}");
            return host.Entities.SpawnUnit(
                HumanSlot,
                new Transform2D(SimFixed.FromInt(cellX), SimFixed.FromInt(cellY)),
                def.MoveSpeed,
                maxHealth: def.MaxHealth,
                role: role);
        }

        private static EntityId PlaceEnemySiteNear(AiHost host, UnitRole role, int centreX, int centreY)
        {
            FactionId faction = host.Economy.GetSlotFaction(HumanSlot);
            ushort definitionId = SimDefinitions.ToDefinitionId(faction, role);

            for (int radius = 0; radius <= 6; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (radius > 0 && System.Math.Abs(dx) != radius && System.Math.Abs(dy) != radius) continue;
                        int originX = centreX + dx - 1;
                        int originY = centreY + dy - 1;
                        if (host.Construction.ValidatePlacement(HumanSlot, definitionId, originX, originY)
                            != CommandResultCode.Applied) continue;
                        Assert.That(host.Construction.TryPlaceBuilding(HumanSlot, definitionId, originX, originY), Is.True);

                        UnitState[] units = host.Entities.RawUnits;
                        for (int i = 0; i < host.Entities.Capacity; i++)
                        {
                            ref readonly UnitState unit = ref units[i];
                            if (unit.IsActive && unit.PlayerId == HumanSlot && unit.Role == role
                                && host.Construction.IsActiveSite(unit.Id))
                            {
                                return unit.Id;
                            }
                        }
                    }
                }
            }

            Assert.Fail($"no legal {role} site near ({centreX},{centreY})");
            return EntityId.Invalid;
        }

        /// <summary>
        /// The target the army agrees on. The AI hands ONE target to every
        /// combat unit, so a split would itself be the failure — the assertion
        /// says so rather than silently reading the first unit.
        /// </summary>
        private static EntityId ArmyAttackTarget(AiHost host, List<EntityId> army)
        {
            EntityId agreed = EntityId.Invalid;
            int checkedUnits = 0;
            for (int i = 0; i < army.Count; i++)
            {
                if (!host.Entities.TryGetUnit(army[i], out UnitState u) || !u.IsActive) continue;
                if (!u.AttackTarget.IsValid) continue;
                checkedUnits++;
                if (!agreed.IsValid)
                {
                    agreed = u.AttackTarget;
                    continue;
                }
                Assert.That(u.AttackTarget, Is.EqualTo(agreed),
                    "the army was handed ONE target; a split means the choice did not reach everyone");
            }
            Assert.That(checkedUnits, Is.GreaterThan(0), "no unit of the army carries an attack order at all");
            return agreed;
        }
    }
}
