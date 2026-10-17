using System;
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

namespace Nova.AI.Tests
{
    /// <summary>
    /// MS-1 skirmish AI suite (EditMode lane, docs/tech/AIArchitecture.md): the
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
    /// Mirror of the .NET lane SkirmishAiTests.
    /// </summary>
    [TestFixture]
    public sealed class SkirmishAiTests
    {
        private const ulong Seed = 0xA17E57DE57UL;
        private const byte HumanSlot = 0;
        private const byte AiSlot = 1;
        private const ushort MapWidth = 128;
        private const ushort MapHeight = 128;
        private const int EntityCapacity = 1024;
        private const long FieldReserveAE = 2000000L;

        /// <summary>
        /// End-to-end tick budget: this suite's deterministic match decides
        /// at tick 2726, so 6.000 ticks is a ~2.2x margin — comfortably sane,
        /// and exact because the whole loop is deterministic.
        /// </summary>
        private const int EndToEndBudgetTicks = 6000;

        // ----------------------------------------------------------------
        // The AI host (mirror of MatchRunner's skirmish wiring)
        // ----------------------------------------------------------------

        private sealed class AiHost
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

        private static AiHost BuildAiHost(
            ulong seed, IAiGoalObserver goalObserver = null, AiPersonality personality = default)
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
            // Applied the way MatchRunner does it: deltas into the profile, the rest by argument.
            var ai = new SkirmishAiSystem(
                AiSlot,
                new AiFactionProfile("Legion",
                    targetPowerMargin: 0, targetArmySize: 12, attackSquadThreshold: 6, targetHarvesterCount: 2)
                    .WithPersonality(personality),
                aiIngress, entities, economy, construction, production, fogOfWar, victory,
                goalObserver, null, personality);

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
                int fieldCell = slot == HumanSlot ? 7 : 117;
                int hqOrigin = slot == HumanSlot ? 4 : 118;
                int builderX = slot == HumanSlot ? 13 : 111;
                int builderY = slot == HumanSlot ? 7 : 117;

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

        private static AiHost BuildMatch(
            ulong seed, IAiGoalObserver goalObserver = null, AiPersonality personality = default)
        {
            AiHost host = BuildAiHost(seed, goalObserver, personality);
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

        private static EntityId FirstHarvester(AiHost host, byte slot)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (u.IsActive && u.PlayerId == slot && u.Role == UnitRole.Harvester) return u.Id;
            }
            return EntityId.Invalid;
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

            Assert.That(refineryTick, Is.GreaterThan(0u),
                "the AI must place and complete its Refinery (D-077: no prerequisite) through PlaceBuilding intents");
            Assert.That(powerTick, Is.GreaterThan(refineryTick),
                "D-103 requires the AI to complete a Power plant after the Refinery and before its Barracks");
            Assert.That(barracksTick, Is.GreaterThan(powerTick),
                "the AI must complete the Barracks only after its required Power plant stands");
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

        /// <summary>
        /// Regression for the hang behaviour revision 10 closes (#134, D-121).
        /// Production and the founding grant pick a harvester's field as the
        /// nearest one to the Refinery; this AI picks the nearest one to its
        /// HQ. Where the two disagree, the r9 gather escort walked the
        /// harvester to the AI's field while its order named the other one —
        /// and the economy HOLDS an out-of-reach order instead of dropping it,
        /// so the harvester stood there with frozen cargo for good. The
        /// two-field map of this suite makes both rules agree, so the test
        /// creates the disagreement directly: a third field away from the AI
        /// base, and the AI's first harvester ordered onto it the tick it
        /// appears. Artificial on purpose; the mechanism is the real one.
        /// </summary>
        [Test]
        public void SkirmishAi_RetasksAHarvesterOrderedOntoAForeignField_AndItMines()
        {
            AiHost host = BuildMatch(Seed);

            EntityId harvester = EntityId.Invalid;
            for (int i = 0; i < 2000 && !harvester.IsValid; i++)
            {
                host.Step();
                harvester = FirstHarvester(host, AiSlot);
            }
            Assert.That(harvester.IsValid, Is.True, "the AI's Refinery hands out its first harvester");

            const ushort ownFieldId = 2;     // slot 1's field at (117,117), see ApplyOpeningPosition
            const ushort foreignFieldId = 3; // far from the AI's HQ and Refinery alike
            Assert.That(host.Economy.TryAddField(foreignFieldId, new GridPos2D(95, 100), FieldReserveAE), Is.True);
            ref UnitState forced = ref host.Entities.GetUnitRef(harvester);
            forced.HarvestFieldId = foreignFieldId;
            forced.IsReturningCargo = false;
            Assert.That(forced.CargoAE, Is.EqualTo(0), "a fresh harvester carries nothing yet");

            bool retasked = false;
            bool mined = false;
            for (int i = 0; i < 1000 && !mined; i++)
            {
                host.Step();
                ref readonly UnitState h = ref host.Entities.GetUnitRef(harvester);
                if (h.HarvestFieldId == ownFieldId) retasked = true;
                if (h.CargoAE > 0 || h.IsReturningCargo) mined = true;
            }

            Assert.That(retasked, Is.True,
                "the AI re-tasks a harvester whose order names a field other than its own (r10)");
            Assert.That(mined, Is.True,
                "the re-tasked harvester mines instead of standing in reach of the wrong field with frozen cargo");
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
                "at the squad threshold the army marches toward the enemy start area (slot 1 starts at x ~ 111-120)");
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
        // (f) The flanked staging cell (r11, #101, D-127)
        //
        // Mirror of the .NET lane SkirmishGoalTests flank block: a D-114
        // flank of ten put the staging cell outside the commit ring, so no
        // unit held there and the army marched piecemeal before its wave
        // was ready.
        // ----------------------------------------------------------------

        private sealed class RecordingObserver : IAiGoalObserver
        {
            public readonly List<(uint Tick, AiArmyGoal Goal)> Army = new List<(uint, AiArmyGoal)>();
            public readonly List<(uint Tick, AiUnitGoal Goal)> Units = new List<(uint, AiUnitGoal)>();

            public void OnArmyGoal(byte slot, uint tick, in AiArmyGoal army) => Army.Add((tick, army));

            public void OnUnitGoal(byte slot, uint tick, in AiUnitGoal goal) => Units.Add((tick, goal));
        }

        [TestCase(10)]
        [TestCase(-10)]
        public void SkirmishAi_FlankedStagingCellIsInsideTheRing_AndProducesHold(int flank)
        {
            AiProfile shipped = AiProfiles.Ms1Canonical;
            var observer = new RecordingObserver();
            AiHost host = BuildMatch(Seed, observer, AiPersonality.Create(AiOpening.Balanced, flank, layoutVariant: 0));
            Assert.That(TryHqCell(host, AiSlot, out int hqX, out int hqY), Is.True);
            uint firstWave = RunToFirstWave(host, observer);
            Assert.That(firstWave, Is.Not.Zero, $"flank {flank}: the first wave never marched");

            AiArmyGoal army = FirstEngagingArmy(observer);
            int stagingDistance = Math.Max(Math.Abs(army.StagingCellX - hqX), Math.Abs(army.StagingCellY - hqY));
            Assert.That(stagingDistance, Is.GreaterThan(shipped.StagingDistanceCells + shipped.StagingToleranceCells),
                $"flank {flank}: the staging cell is inside the unflanked ring, so this scene does not reproduce #101");
            Assert.That(stagingDistance, Is.LessThanOrEqualTo(shipped.StagingDistanceCells + Math.Abs(flank)));

            int holds = 0;
            foreach ((uint tick, AiArmyGoal goal) in observer.Army)
            {
                if (tick >= firstWave || !goal.Engages) continue;
                Assert.That(goal.Committed, Is.Zero,
                    $"flank {flank}, tick {tick}: units counted as already out before any wave marched");
            }
            foreach ((uint tick, AiUnitGoal goal) in observer.Units)
            {
                if (tick < firstWave && goal.Goal == GoalKind.Hold) holds++;
            }
            Assert.That(holds, Is.GreaterThan(0),
                $"flank {flank}: not one unit was told to hold at the staging cell before the first wave");
        }

        [TestCase(10)]
        [TestCase(-10)]
        [TestCase(0)]
        public void SkirmishAi_NoAttackBeforeWaveReady_WhenFlanked(int flank)
        {
            var observer = new RecordingObserver();
            AiHost host = BuildMatch(Seed, observer, AiPersonality.Create(AiOpening.Balanced, flank, layoutVariant: 0));
            uint firstWave = RunToFirstWave(host, observer);
            Assert.That(firstWave, Is.Not.Zero, $"flank {flank}: the first wave never marched");

            int attacksAtTheWave = 0;
            foreach ((uint tick, AiUnitGoal goal) in observer.Units)
            {
                if (goal.Goal != GoalKind.Attack) continue;
                Assert.That(tick, Is.GreaterThanOrEqualTo(firstWave),
                    $"flank {flank}: unit {goal.EntityRaw} was sent to attack at tick {tick}, "
                    + $"before the first wave was ready at tick {firstWave}");
                if (tick == firstWave) attacksAtTheWave++;
            }
            Assert.That(attacksAtTheWave, Is.GreaterThan(0),
                $"flank {flank}: the wave was reported ready at tick {firstWave} and nobody marched");
        }

        private static uint RunToFirstWave(AiHost host, RecordingObserver observer)
        {
            for (int budget = 0; budget < EndToEndBudgetTicks && !host.Victory.IsDecided; budget++)
            {
                host.Step();
                for (int i = observer.Army.Count - 1; i >= 0; i--)
                {
                    if (observer.Army[i].Goal.Engages && observer.Army[i].Goal.WaveReady) return observer.Army[i].Tick;
                }
            }
            return 0;
        }

        private static AiArmyGoal FirstEngagingArmy(RecordingObserver observer)
        {
            foreach ((uint _, AiArmyGoal goal) in observer.Army)
            {
                if (goal.Engages && goal.StagingCellX >= 0) return goal;
            }
            Assert.Fail("the army never engaged with a resolved staging cell");
            return default;
        }

        private static bool TryHqCell(AiHost host, byte slot, out int cellX, out int cellY)
        {
            UnitState[] units = host.Entities.RawUnits;
            for (int i = 0; i < host.Entities.Capacity; i++)
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
