using System;
using System.Collections.Generic;
using Nova.AI.Data;
using Nova.Core;
using Nova.Simulation;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Combat;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using Nova.Simulation.Victory;
using Nova.Simulation.Vision;

namespace Nova.AI
{
    /// <summary>
    /// Deterministic MS-1 skirmish opponent (docs/tech/AIArchitecture.md).
    /// Plays one slot of the canonical two-slot match — build order, economy,
    /// army and attacks — over the SAME command path a human uses: every
    /// action is a schema-v1 <see cref="CommandIntent"/> handed to the AI
    /// peer's own slot-bound <see cref="CommandIngress"/> (session authority
    /// assigns slot, sequence and target tick; <see cref="AiPeerCommandTransport"/>
    /// forwards the sealed records into the host intake). No direct calls into
    /// construction/production/economy mutation APIs — the executor's
    /// state-dependent validation judges every AI order exactly like a UI
    /// order (Commands.md section 4).
    /// <para>
    /// READ BOUNDARY (AIArchitecture.md sections 1 and 6): enemy entities are
    /// observed ONLY through <see cref="FogOfWarSystem.GetVisibleEntities"/>
    /// — the team's committed view, the single legal sight for targeting.
    /// Own-slot data (credits, power, production queues, placements, own unit
    /// orders) is read from the canonical systems; a human player has the same
    /// own-slot information, so no hidden state leaks into a decision. Static
    /// map geometry (registered Aetherium fields, and with them the enemy
    /// start area — the demo map seats every base beside its field) is known
    /// map knowledge, never fog-hidden entity data. The strict
    /// AIArchitecture.md split (a TeamWorldView type and a versioned
    /// AiSidecar) does not exist in this slice yet; reading the committed own
    /// state directly is the documented G1 simplification, and the system is
    /// STATELESS (every decision is a pure function of the tick and the
    /// committed state — no timers, no memory), so there is no AI state to
    /// serialize and plain <see cref="ISimSystem"/> satisfies the kernel
    /// registration checklist. Save/restore therefore reproduces the same
    /// later intents without any sidecar block.
    /// </para>
    /// <para>
    /// DECISION LOOP (fixed cadence <see cref="DecisionTickInterval"/> = 20
    /// ticks = 2.0 s, ascending-index scans only, no PRNG): (1) build order —
    /// Refinery first (no prerequisite since D-077), then Barracks, one site
    /// at a time, a Power plant first whenever the committed margin would
    /// drop below the profile reserve, the spot picked by a deterministic
    /// search validated through <see cref="ConstructionSystem.ValidatePlacement"/>
    /// — the identical rules the command executor applies; (2) the Builder is
    /// moved next to an unfinished site when it is out of the documented
    /// Chebyshev reach &lt;= 1 (ConstructionSystem remarks), and a replacement
    /// Builder is queued at the HQ when none is alive; (3) once the Refinery
    /// stands, harvesters are queued up to
    /// <see cref="AiFactionProfile.TargetHarvesterCount"/>, every idle own
    /// harvester receives a Harvest intent on the own field, and harvesters
    /// held out of reach are WALKED into the economy's reach rule with
    /// explicit Move intents (gather leg toward a field-and-footprint
    /// dual-reach cell, return leg toward the footprint — this slice does not
    /// use the Refinery's rally point at all, it micro-manages like a human;
    /// a rally point WOULD be accepted, see the note at the economy step);
    /// (4) once the
    /// Barracks stands, infantry is queued up to
    /// <see cref="AiFactionProfile.TargetArmySize"/> as funds allow;
    /// (5) the army resolves a POSTURE (does it act at all, which target,
    /// which destination), then ONE ASSIGNMENT PER UNIT, then submission that
    /// groups units sharing an order into a single intent: at
    /// <see cref="AiFactionProfile.AttackSquadThreshold"/> living combat units
    /// the army is sent toward the enemy start area, and the best scored
    /// visible enemy receives an EXPLICIT AttackTarget intent from every unit.
    /// There is no attack-move (GB-002), but D-087 DID add auto-acquisition to
    /// <see cref="CombatSystem"/>: an idle armed unit picks the nearest
    /// visible hostile in range by itself. Explicit orders are never
    /// retargeted, so an AI order always wins over the automatic pick — and
    /// therefore has to be at least as good as it. The enemy HQ is preferred
    /// once visible (D-077: its loss defeats the slot).
    /// </para>
    /// <para>
    /// Rejection tolerance: affordability, the power rule and placement
    /// legality are pre-checked against the same rules the executor uses, and
    /// redundant re-issues are suppressed by comparing the standing order
    /// (move target, attack target, harvest field) before
    /// submitting. Anything still rejected (e.g. backpressure) is simply
    /// retried on the next cadence — the stateless loop never spams.
    /// Determinism: intents submitted while the kernel executes tick T are
    /// sealed into the batch of T+1 (the host advances the AI peer clock
    /// before stepping); the one-tick input delay is the canonical one every
    /// command pays (MatchSession.InputDelayTicks = 1, part of the match
    /// fingerprint).
    /// </para>
    /// <para>
    /// Zero engine dependencies (no UnityEngine types).
    /// </para>
    /// </summary>
    public sealed class SkirmishAiSystem : ISimSystem
    {
        // THE NUMBERS LIVE IN Nova.AI.Data. What used to be four const fields
        // here are profile values now — behaviour in C#, numbers in one place
        // (AIArchitecture.md section 3). The shipped profile carries exactly
        // the constants that stood here, so this move changes nothing;
        // that the baselines stay green is the proof it was clean.

        /// <summary>Decision cadence in ticks: 20 ticks = 2.0 s on the canonical 10 Hz clock.</summary>
        public ushort DecisionTickInterval => _profile.Profile.DecisionTickInterval;

        /// <summary>Largest Chebyshev ring around the placement anchor the spot search tries (documented AI choice, not a rule).</summary>
        private int PlacementSearchRadius => _profile.Profile.PlacementSearchRadius;

        /// <summary>Infantry queued per decision tick while below the army cap (smooths spending over the cadence).</summary>
        private int InfantryQueueBatch => _profile.Profile.InfantryQueueBatch;

        /// <summary>Harvesters queued per decision tick while below the harvester target.</summary>
        private int HarvesterQueueBatch => _profile.Profile.HarvesterQueueBatch;

        /// <summary>One own construction site seen in the ascending scan (the site entity sits at the footprint center cell).</summary>
        private struct SiteInfo
        {
            public int CellX;
            public int CellY;
            public uint AssignedBuilderRaw;
        }

        private readonly byte _aiPlayerId;
        private readonly AiFactionProfile _profile;
        private readonly CommandIngress _ingress;
        private readonly EntityManager _entityManager;
        private readonly EconomySystem _economy;
        private readonly ConstructionSystem _construction;
        private readonly ProductionSystem _production;
        private readonly FogOfWarSystem _fogOfWar;
        private readonly VictorySystem _victory;

        public string Name => $"SkirmishAi_{_profile.FactionName}_P{_aiPlayerId}";
        public byte AiPlayerId => _aiPlayerId;

        /// <summary>
        /// <paramref name="ingress"/> is the AI peer's OWN slot-bound ingress
        /// (its session's local slot is <paramref name="aiPlayerId"/>), never
        /// the human host ingress — that is what keeps the AI on the canonical
        /// intent path with an authority-assigned slot, sequence and target
        /// tick.
        /// </summary>
        public SkirmishAiSystem(
            byte aiPlayerId,
            AiFactionProfile profile,
            CommandIngress ingress,
            EntityManager entityManager,
            EconomySystem economy,
            ConstructionSystem construction,
            ProductionSystem production,
            FogOfWarSystem fogOfWar,
            VictorySystem victory)
        {
            _aiPlayerId = aiPlayerId;
            _profile = profile;
            _ingress = ingress ?? throw new ArgumentNullException(nameof(ingress));
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
            _production = production ?? throw new ArgumentNullException(nameof(production));
            _fogOfWar = fogOfWar ?? throw new ArgumentNullException(nameof(fogOfWar));
            _victory = victory ?? throw new ArgumentNullException(nameof(victory));
        }

        public void Initialize(SimulationKernel kernel)
        {
            kernel?.Logger.LogInfo(
                $"[{Name}] Initialized MS-1 skirmish AI for slot {_aiPlayerId} " +
                $"(intent path via the peer ingress, cadence {DecisionTickInterval} ticks).");
        }

        /// <summary>
        /// The fixed-cadence decision loop. Registered after Combat and before
        /// Victory, so decisions read the post-combat state of the executing
        /// tick; a decided match ends every further order.
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            if (tick.Value % DecisionTickInterval != 0) return;
            if (_victory.IsDecided) return;
            Decide();
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------------
        // The decision loop (pure function of the committed state)
        // ------------------------------------------------------------------

        private void Decide()
        {
            FactionId faction = _economy.GetSlotFaction(_aiPlayerId);
            ref readonly PlayerEconomyState eco = ref _economy.GetPlayerEconomy(_aiPlayerId);
            long credits = eco.AetheriumCredits;
            int powerMargin = eco.PowerProvided - eco.PowerRequired;

            // One ascending-index scan of the entity store collecting every
            // fact the decisions below read (deterministic iteration order).
            uint hqRaw = 0, refineryRaw = 0, barracksRaw = 0;
            int hqCellX = -1, hqCellY = -1, refineryCellX = -1, refineryCellY = -1;
            bool powerCompleted = false;
            int builders = 0, harvesters = 0, combatCount = 0;
            var combatRaws = new List<uint>();
            var combatUnits = new List<UnitState>();
            var idleHarvesterRaws = new List<uint>();
            var harvesterRaws = new List<uint>();
            var harvesterUnits = new List<UnitState>();
            var sites = new List<SiteInfo>();

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId != _aiPlayerId) continue;
                uint raw = UnitCommandStateView.ToRawEntityId(u.Id);
                if (raw == 0) continue;

                if (SimDefinitions.IsBuildingRole(u.Role))
                {
                    switch (u.Role)
                    {
                        case UnitRole.HQ when hqRaw == 0:
                            hqRaw = raw;
                            hqCellX = GridCellOf(u.Transform.PositionX);
                            hqCellY = GridCellOf(u.Transform.PositionY);
                            break;
                        case UnitRole.Refinery when refineryRaw == 0:
                            refineryRaw = raw;
                            refineryCellX = GridCellOf(u.Transform.PositionX);
                            refineryCellY = GridCellOf(u.Transform.PositionY);
                            break;
                        case UnitRole.Barracks when barracksRaw == 0:
                            barracksRaw = raw;
                            break;
                        case UnitRole.Power:
                            powerCompleted = true;
                            break;
                    }
                    continue;
                }

                switch (u.Role)
                {
                    case UnitRole.Builder:
                        builders++;
                        break;
                    case UnitRole.Harvester:
                        harvesters++;
                        harvesterRaws.Add(raw);
                        harvesterUnits.Add(u);
                        if (u.HarvestFieldId == 0 && !u.IsReturningCargo)
                        {
                            idleHarvesterRaws.Add(raw);
                        }
                        break;
                    case UnitRole.Unit:
                        // A construction site carries the generic role; the
                        // site table tells it from a plain unit.
                        if (_construction.TryGetSite(raw, out _, out _, out uint assignedBuilder))
                        {
                            sites.Add(new SiteInfo
                            {
                                CellX = GridCellOf(u.Transform.PositionX),
                                CellY = GridCellOf(u.Transform.PositionY),
                                AssignedBuilderRaw = assignedBuilder,
                            });
                        }
                        break;
                    default:
                        if (IsCombatRole(u.Role))
                        {
                            combatCount++;
                            combatRaws.Add(raw);
                            combatUnits.Add(u);
                        }
                        break;
                }
            }

            // A slot without an HQ cannot run the D-077 opening loop (and a
            // slot that owns nothing is defeated anyway): stay idle.
            if (hqRaw == 0) return;

            // ---- (1) Build order: Refinery, then Barracks, one site at a
            // time (a single Builder cannot progress two sites). A Power
            // plant preempts whenever the committed margin would drop below
            // the profile reserve — "when the margin would go negative" with
            // the demo profile's reserve of 0. ----
            if (sites.Count == 0)
            {
                UnitRole next = refineryRaw == 0
                    ? UnitRole.Refinery
                    : (barracksRaw == 0 ? UnitRole.Barracks : UnitRole.Unit);
                if (next != UnitRole.Unit
                    && SimDefinitions.TryGetBuilding(faction, next, out SimBuildingDefinition nextDef))
                {
                    if (nextDef.PowerRequired > 0 && !powerCompleted
                        && powerMargin < nextDef.PowerRequired + _profile.TargetPowerMargin)
                    {
                        next = UnitRole.Power;
                    }
                    TryPlaceBuilding(faction, next, credits, hqCellX, hqCellY);
                }
            }

            // ---- (2) Construction support: the assigned Builder must stand
            // in Chebyshev reach <= 1 of the site footprint or the site
            // pauses (ConstructionSystem remarks) — walk it there. ----
            for (int s = 0; s < sites.Count; s++)
            {
                SiteInfo site = sites[s];
                if (site.AssignedBuilderRaw == 0) continue;
                EntityId builderId = UnitCommandStateView.ToEntityId(site.AssignedBuilderRaw);
                if (!_entityManager.TryGetUnit(builderId, out UnitState builder)) continue;

                int originX = site.CellX - 1;
                int originY = site.CellY - 1;
                if (IsInReachOfFootprint(
                        GridCellOf(builder.Transform.PositionX), GridCellOf(builder.Transform.PositionY),
                        originX, originY))
                {
                    continue;
                }

                // Deterministic adjacent cell: the first footprint-free
                // cell in Chebyshev reach 1 of the site rectangle — a fixed
                // side can lie inside a neighbour building, which is
                // impassable since the Truppenführung sprint and would
                // stall the site forever.
                if (!TryFindFootprintAdjacentCell(originX, originY, out int targetX, out int targetY))
                {
                    continue;
                }
                if (builder.IsMoving && builder.TargetGridPos.IsValid
                    && builder.TargetGridPos.X == targetX && builder.TargetGridPos.Y == targetY)
                {
                    continue; // already walking there
                }
                Submit(new MovePayload(
                    new[] { site.AssignedBuilderRaw }, SimFixed.FromInt(targetX), SimFixed.FromInt(targetY)));
            }

            // ---- (3) Replacement Builder at the HQ when none is alive. ----
            if (builders == 0
                && SimDefinitions.TryGetUnit(faction, UnitRole.Builder, out SimUnitDefinition builderDef)
                && CountQueuedAt(hqRaw, builderDef.DefinitionId) == 0
                && credits >= builderDef.CostAE)
            {
                Submit(new QueueUnitPayload(hqRaw, builderDef.DefinitionId, 1));
            }

            // ---- (4) Economy: keep harvesters queued at the Refinery (the
            // D-077 producer), send every idle own harvester to the own field
            // and WALK harvesters into reach with explicit Move intents.
            // This slice never submits SetRallyPoint; it does what a human
            // player does and micros the harvesters into the economy's reach
            // rule. That is a behavior choice, NOT a validator limit: the
            // rally point would be accepted. ProductionSystem.IsProducerRole
            // reads UnitRole.Refinery out of SimDefinitions.AllUnits (both
            // factions' Harvester carries producerRole: Refinery since D-077)
            // instead of a hardcoded list, precisely so the producer move
            // could not strand it. Using the rally point here would change
            // behavior and belongs in its own PR. ----
            if (refineryRaw != 0
                && TryGetOwnFieldCell(hqCellX, hqCellY, out ushort ownFieldId, out int fieldX, out int fieldY))
            {
                if (SimDefinitions.TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition harvesterDef))
                {
                    int have = harvesters + CountQueuedAt(refineryRaw, harvesterDef.DefinitionId);
                    int batch = Math.Min(HarvesterQueueBatch, _profile.TargetHarvesterCount - have);
                    if (batch > 0 && credits >= (long)harvesterDef.CostAE * batch)
                    {
                        Submit(new QueueUnitPayload(refineryRaw, harvesterDef.DefinitionId, (ushort)batch));
                    }
                }

                if (idleHarvesterRaws.Count > 0)
                {
                    idleHarvesterRaws.Sort();
                    SubmitEntityList(idleHarvesterRaws,
                        ids => CommandIntent.Create(new HarvestPayload(ids, ownFieldId)));
                }

                // Escort targets: the gather leg wants a cell in harvest reach
                // of the field (Chebyshev 1 of the field cell) AND in deposit
                // reach of the Refinery footprint, so the full auto-cycle
                // (gather -> return -> gather, EconomySystem remarks) closes
                // in one spot; the return leg wants any cell adjacent to the
                // footprint. Deterministic ascending picks.
                int refineryOriginX = refineryCellX - 1;
                int refineryOriginY = refineryCellY - 1;
                bool haveGatherSpot = TryFindDualReachCell(fieldX, fieldY, refineryOriginX, refineryOriginY,
                    out int gatherX, out int gatherY);
                if (!haveGatherSpot)
                {
                    // The field cell itself always satisfies harvest reach.
                    gatherX = fieldX;
                    gatherY = fieldY;
                }
                int returnX, returnY;
                bool haveReturnSpot = TryFindFootprintAdjacentCell(refineryOriginX, refineryOriginY,
                    out returnX, out returnY);

                var gatherEscort = new List<uint>();
                var returnEscort = new List<uint>();
                for (int i = 0; i < harvesterUnits.Count; i++)
                {
                    UnitState harvester = harvesterUnits[i];
                    int cellX = GridCellOf(harvester.Transform.PositionX);
                    int cellY = GridCellOf(harvester.Transform.PositionY);

                    if (harvester.IsReturningCargo)
                    {
                        // The return leg resolves only in deposit reach of an
                        // own Refinery (the economy's documented footprint
                        // reach rule); walk there when held out of reach.
                        if (!haveReturnSpot || harvester.CargoAE <= 0) continue;
                        if (IsInDepositReach(cellX, cellY, refineryCellX, refineryCellY)) continue;
                        if (AlreadyHeadingTo(in harvester, returnX, returnY)) continue;
                        returnEscort.Add(harvesterRaws[i]);
                    }
                    else
                    {
                        // Out-of-reach harvest orders are HELD, never dropped
                        // (EconomySystem) — closing the distance is the AI's
                        // job, exactly like a human's move click.
                        if (IsInFieldReach(cellX, cellY, fieldX, fieldY)) continue;
                        if (AlreadyHeadingTo(in harvester, gatherX, gatherY)) continue;
                        gatherEscort.Add(harvesterRaws[i]);
                    }
                }
                if (gatherEscort.Count > 0)
                {
                    gatherEscort.Sort();
                    SubmitEntityList(gatherEscort,
                        ids => CommandIntent.Create(new MovePayload(ids, SimFixed.FromInt(gatherX), SimFixed.FromInt(gatherY))));
                }
                if (returnEscort.Count > 0)
                {
                    returnEscort.Sort();
                    SubmitEntityList(returnEscort,
                        ids => CommandIntent.Create(new MovePayload(ids, SimFixed.FromInt(returnX), SimFixed.FromInt(returnY))));
                }
            }

            // ---- (5) Army: keep infantry queued up to the cap as funds allow. ----
            if (barracksRaw != 0
                && SimDefinitions.TryGetUnit(faction, UnitRole.BasicInfantry, out SimUnitDefinition infantryDef))
            {
                int have = combatCount + CountQueuedAt(barracksRaw, infantryDef.DefinitionId);
                int batch = Math.Min(InfantryQueueBatch, _profile.TargetArmySize - have);
                if (batch > 0 && credits >= (long)infantryDef.CostAE * batch)
                {
                    Submit(new QueueUnitPayload(barracksRaw, infantryDef.DefinitionId, (ushort)batch));
                }
            }

            // ---- (6) Army: resolve one posture for the army, one assignment
            // per unit, then submit the assignments grouped. See the three
            // steps below; the rules are exactly the ones the previous
            // whole-army block applied. ----
            ArmyPosture posture = ResolveArmyPosture(combatCount, combatUnits, hqCellX, hqCellY);
            if (posture.Engages)
            {
                var assignments = new List<UnitAssignment>(combatUnits.Count);
                for (int i = 0; i < combatUnits.Count; i++)
                {
                    assignments.Add(ResolveUnitAssignment(combatRaws[i], in posture));
                }
                SubmitAssignments(assignments, combatUnits);
            }
        }

        // ------------------------------------------------------------------
        // Army: posture -> per-unit assignment -> grouped submission
        //
        // THREE STEPS, because one whole-army order cannot express what the
        // army has to do. The previous shape computed a single target and a
        // single destination for every living combat unit, which is why
        // "this one wounded unit turns back", "reinforcements wait at a
        // staging cell" and "aim before the squad threshold is reached"
        // could not be written down at all — and why a defence branch that
        // switched the WHOLE army's destination every cadence produced 23 %
        // more intents and a worse match (behaviour journal V002).
        //
        // The split is: what the army does (posture, derived from the
        // committed state, never stored), what each unit does (assignment),
        // and how that reaches the ingress (grouping, so N units sharing an
        // order still cost ONE intent). The rules themselves are unchanged
        // here: this shape reproduces the canonical match tick for tick.
        // ------------------------------------------------------------------

        /// <summary>
        /// What the army as a whole is doing this decision. Derived fresh
        /// every cadence from the committed state — the system stays
        /// stateless, so there is nothing here to serialize.
        /// </summary>
        private struct ArmyPosture
        {
            /// <summary>
            /// False when the army does not act at all: below the squad
            /// threshold, or the own slot has no committed team view (only
            /// slots below <see cref="FogOfWarSystem.TeamCount"/> do).
            /// </summary>
            public bool Engages;

            /// <summary>The scored target the army shoots at; 0 when nothing enemy is visible.</summary>
            public uint TargetRaw;

            /// <summary>Where the army walks: the target's cell, else the enemy start area; -1 while the army does not act.</summary>
            public int MoveCellX;

            /// <summary>See <see cref="MoveCellX"/>.</summary>
            public int MoveCellY;
        }

        /// <summary>
        /// One unit's orders for this decision. The two slots are
        /// INDEPENDENT on purpose — a unit can be told to shoot at one thing
        /// and to stand somewhere else, and either half can be "no explicit
        /// order, leave the standing one alone" (<see cref="AttackTargetRaw"/>
        /// 0 hands the pick to the D-087 auto-acquisition,
        /// <see cref="MoveCellX"/> &lt; 0 leaves the unit where it walks).
        /// </summary>
        private struct UnitAssignment
        {
            public uint EntityRaw;
            public uint AttackTargetRaw;
            public int MoveCellX;
            public int MoveCellY;
        }

        /// <summary>
        /// The army's posture: at
        /// <see cref="AiFactionProfile.AttackSquadThreshold"/> living combat
        /// units the army marches on the enemy start area, and the best
        /// visible enemy (integer score, committed view only) becomes the
        /// shared target and the destination. No attack-move exists (GB-002),
        /// but auto-acquisition does since D-087 — an explicit order simply
        /// outranks it and is never retargeted.
        /// </summary>
        private ArmyPosture ResolveArmyPosture(int combatCount, List<UnitState> combatUnits, int hqCellX, int hqCellY)
        {
            var posture = new ArmyPosture
            {
                Engages = combatCount >= _profile.AttackSquadThreshold && _aiPlayerId < _fogOfWar.TeamCount,
                MoveCellX = -1,
                MoveCellY = -1,
            };
            if (!posture.Engages) return posture;

            posture.TargetRaw = FindBestVisibleEnemyByScore(combatUnits, out int targetCellX, out int targetCellY);
            if (posture.TargetRaw != 0)
            {
                posture.MoveCellX = targetCellX;
                posture.MoveCellY = targetCellY;
            }
            else
            {
                GetEnemyStartAreaCell(hqCellX, hqCellY, out posture.MoveCellX, out posture.MoveCellY);
            }
            return posture;
        }

        /// <summary>
        /// One unit's orders under the given posture. Today every combat unit
        /// gets the same two — that IS the current behaviour, and this is the
        /// one place a later rule (retreat below a health threshold, waiting
        /// at a staging cell) has to change to break the uniformity.
        /// <para>
        /// Aiming BELOW the squad threshold was built here and measured back
        /// out again — behaviour journal V003 carries the four variants and
        /// the reason: an explicit order cannot be handed back to the D-087
        /// auto-acquisition, because <c>AttackTarget</c> is released only by
        /// the target's death (<c>UnitState.Stop()</c> leaves it untouched).
        /// A standing unit that stops closing the distance therefore holds a
        /// stale order, and holding beats aiming only while the unit walks
        /// toward what it aims at.
        /// </para>
        /// </summary>
        private UnitAssignment ResolveUnitAssignment(uint entityRaw, in ArmyPosture posture)
        {
            return new UnitAssignment
            {
                EntityRaw = entityRaw,
                AttackTargetRaw = posture.TargetRaw,
                MoveCellX = posture.MoveCellX,
                MoveCellY = posture.MoveCellY,
            };
        }

        /// <summary>
        /// Turns assignments into intents: units sharing an order are
        /// collected into ONE entity-list command, and an assignment that
        /// merely repeats what the unit is already doing is dropped (the same
        /// re-issue suppression the economy steps use — comparing the
        /// standing order is the AI's only memory).
        /// <para>
        /// Attack orders go out before move orders, and groups are walked in
        /// ascending key order (target id, then destination cell), so the
        /// sealed batch never depends on the entity scan or a dictionary.
        /// </para>
        /// </summary>
        private void SubmitAssignments(List<UnitAssignment> assignments, List<UnitState> units)
        {
            var keys = new List<long>();
            var members = new List<uint>();

            // ---- attack orders, grouped by target ----
            CollectKeys(assignments, keys, a => a.AttackTargetRaw != 0 ? a.AttackTargetRaw : -1L);
            for (int k = 0; k < keys.Count; k++)
            {
                uint targetRaw = (uint)keys[k];
                EntityId targetId = UnitCommandStateView.ToEntityId(targetRaw);
                members.Clear();
                for (int i = 0; i < assignments.Count; i++)
                {
                    if (assignments[i].AttackTargetRaw != targetRaw) continue;
                    if (units[i].AttackTarget == targetId) continue; // already ordered
                    members.Add(assignments[i].EntityRaw);
                }
                if (members.Count == 0) continue;
                members.Sort();
                uint target = targetRaw;
                SubmitEntityList(members, ids => CommandIntent.Create(new AttackTargetPayload(ids, target)));
            }

            // ---- move orders, grouped by destination cell ----
            CollectKeys(assignments, keys, a => a.MoveCellX >= 0 && a.MoveCellY >= 0
                ? ((long)a.MoveCellY << 32) | (uint)a.MoveCellX
                : -1L);
            for (int k = 0; k < keys.Count; k++)
            {
                int cellX = (int)(uint)keys[k];
                int cellY = (int)(keys[k] >> 32);
                members.Clear();
                for (int i = 0; i < assignments.Count; i++)
                {
                    if (assignments[i].MoveCellX != cellX || assignments[i].MoveCellY != cellY) continue;
                    GridPos2D standing = units[i].TargetGridPos;
                    if (standing.IsValid && standing.X == cellX && standing.Y == cellY) continue; // already walking there
                    members.Add(assignments[i].EntityRaw);
                }
                if (members.Count == 0) continue;
                members.Sort();
                int x = cellX, y = cellY;
                SubmitEntityList(members,
                    ids => CommandIntent.Create(new MovePayload(ids, SimFixed.FromInt(x), SimFixed.FromInt(y))));
            }
        }

        /// <summary>
        /// The distinct group keys of an assignment list in ascending order;
        /// a key of -1 means "this assignment carries no order of that kind"
        /// and forms no group. Linear scans over a handful of keys — no hash
        /// container, because iteration order of one would leak into the
        /// sealed batch.
        /// </summary>
        private static void CollectKeys(List<UnitAssignment> assignments, List<long> keys, Func<UnitAssignment, long> keyOf)
        {
            keys.Clear();
            for (int i = 0; i < assignments.Count; i++)
            {
                long key = keyOf(assignments[i]);
                if (key < 0 || keys.Contains(key)) continue;
                keys.Add(key);
            }
            keys.Sort();
        }

        // ------------------------------------------------------------------
        // Decisions helpers (all deterministic; ascending scans everywhere)
        // ------------------------------------------------------------------

        /// <summary>
        /// Places <paramref name="role"/> on the first spot of the
        /// deterministic search near the own field (falling back to the own
        /// HQ cell when no field is registered). The AI checks affordability
        /// itself — an unaffordable building is retried on the next cadence
        /// instead of burning a rejected record.
        /// </summary>
        private void TryPlaceBuilding(FactionId faction, UnitRole role, long credits, int hqCellX, int hqCellY)
        {
            if (!SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition def)) return;
            if (credits < def.CostAE) return;
            int anchorX = hqCellX, anchorY = hqCellY;
            if (TryGetOwnFieldCell(hqCellX, hqCellY, out _, out int fieldX, out int fieldY))
            {
                anchorX = fieldX;
                anchorY = fieldY;
            }
            if (TryFindPlacementSpot(def.DefinitionId, anchorX, anchorY, out int spotX, out int spotY))
            {
                Submit(new PlaceBuildingPayload(def.DefinitionId, (ushort)spotX, (ushort)spotY));
            }
        }

        /// <summary>
        /// Deterministic placement search: expanding Chebyshev rings 1..<see cref="PlacementSearchRadius"/>
        /// around the anchor cell, ascending (y, x) inside each ring, and the
        /// first origin that passes <see cref="ConstructionSystem.ValidatePlacement"/>
        /// — the identical state-dependent rules the command executor applies
        /// at the target tick — wins. Ring 1 first means the Refinery lands
        /// beside the field (harvest AND deposit reach in one cell).
        /// </summary>
        private bool TryFindPlacementSpot(ushort buildingDefId, int anchorX, int anchorY, out int spotX, out int spotY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int d = 1; d <= PlacementSearchRadius; d++)
            {
                for (int y = anchorY - d - (f - 1); y <= anchorY + d; y++)
                {
                    for (int x = anchorX - d - (f - 1); x <= anchorX + d; x++)
                    {
                        if (ChebyshevToFootprint(anchorX, anchorY, x, y) != d) continue;
                        if (_construction.ValidatePlacement(_aiPlayerId, buildingDefId, x, y)
                            == CommandResultCode.Applied)
                        {
                            spotX = x;
                            spotY = y;
                            return true;
                        }
                    }
                }
            }
            spotX = 0;
            spotY = 0;
            return false;
        }

        /// <summary>
        /// The cell that closes the harvest loop without further walking: a
        /// free ring-1 neighbour of the field cell (ascending (y, x)) that
        /// also stands in Chebyshev reach &lt;= 1 of the Refinery footprint —
        /// the reach rule the economy uses for gathering (field side) and the
        /// footprint-adjacent equivalent for depositing.
        /// </summary>
        private bool TryFindDualReachCell(int fieldX, int fieldY, int refineryOriginX, int refineryOriginY,
            out int cellX, out int cellY)
        {
            for (int y = fieldY - 1; y <= fieldY + 1; y++)
            {
                for (int x = fieldX - 1; x <= fieldX + 1; x++)
                {
                    if (x == fieldX && y == fieldY) continue;
                    if (!_construction.IsCellFree(x, y)) continue; // prefer a footprint-free stand cell
                    if (ChebyshevToFootprint(x, y, refineryOriginX, refineryOriginY) > 1) continue;
                    cellX = x;
                    cellY = y;
                    return true;
                }
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        /// <summary>
        /// The first cell (ascending (y, x)) standing in Chebyshev reach
        /// &lt;= 1 of a footprint rectangle and OUTSIDE every placement
        /// footprint — since the Truppenführung sprint footprints are
        /// impassable terrain, an unchecked adjacent cell can lie inside a
        /// neighbour building; a unit ordered there would be redirected by
        /// the formation distribution to a cell possibly OUT of reach and
        /// the construction/deposit would stall forever.
        /// </summary>
        private bool TryFindFootprintAdjacentCell(int originX, int originY, out int cellX, out int cellY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            for (int y = originY - 1; y <= originY + f; y++)
            {
                for (int x = originX - 1; x <= originX + f; x++)
                {
                    if (x < 0 || y < 0 || x >= ConstructionSystem.GridSize || y >= ConstructionSystem.GridSize) continue;
                    if (ChebyshevToFootprint(x, y, originX, originY) != 1) continue;
                    if (!_construction.IsCellFree(x, y)) continue;
                    cellX = x;
                    cellY = y;
                    return true;
                }
            }
            cellX = 0;
            cellY = 0;
            return false;
        }

        /// <summary>
        /// The registered Aetherium field nearest to the own HQ (tie: lowest
        /// id) — the demo map seats every base beside its field. Field ids are
        /// host-assigned and nonzero; the registry is probed over its format
        /// capacity in ascending id order.
        /// </summary>
        private bool TryGetOwnFieldCell(int hqCellX, int hqCellY, out ushort fieldId, out int cellX, out int cellY)
        {
            fieldId = 0;
            cellX = 0;
            cellY = 0;
            long best = long.MaxValue;
            for (ushort id = 1; id <= EconomySystem.MaxFields; id++)
            {
                if (!_economy.TryGetField(id, out AetheriumField field)) continue;
                long dx = field.GridPos.X - hqCellX;
                long dy = field.GridPos.Y - hqCellY;
                long distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < best)
                {
                    best = distanceSquared;
                    fieldId = id;
                    cellX = field.GridPos.X;
                    cellY = field.GridPos.Y;
                }
            }
            return fieldId != 0;
        }

        /// <summary>
        /// The enemy start area as static map knowledge: the registered field
        /// FARTHEST from the own HQ (tie: lowest id), because the demo map
        /// seats every base beside its field. Used as a MOVE destination only
        /// — targeting still requires a committed-view entity. No fields
        /// registered: the map center, an equally neutral destination.
        /// </summary>
        private void GetEnemyStartAreaCell(int hqCellX, int hqCellY, out int cellX, out int cellY)
        {
            cellX = ConstructionSystem.GridSize / 2;
            cellY = ConstructionSystem.GridSize / 2;
            long best = -1;
            for (ushort id = 1; id <= EconomySystem.MaxFields; id++)
            {
                if (!_economy.TryGetField(id, out AetheriumField field)) continue;
                long dx = field.GridPos.X - hqCellX;
                long dy = field.GridPos.Y - hqCellY;
                long distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > best)
                {
                    best = distanceSquared;
                    cellX = field.GridPos.X;
                    cellY = field.GridPos.Y;
                }
            }
        }

        /// <summary>
        /// The preferred target inside the team's COMMITTED view
        /// (<see cref="FogOfWarSystem.GetVisibleEntities"/>, ascending entity
        /// index): the first visible enemy HQ (D-077: its loss decides the
        /// match), else the first visible enemy building, else the first
        /// visible enemy unit. 0 when nothing enemy is visible — radar pings
        /// deliberately grant no targeting right (FogOfWar.md section 3).
        /// </summary>
        /// <summary>
        /// Picks the visible enemy the army shoots at, by integer score.
        /// <para>
        /// Replaces the previous rule "HQ, else the FIRST visible building,
        /// else the FIRST visible unit". That rule read the order of the
        /// visibility list as if it were a preference, so the army walked past
        /// a tank to shell a warehouse — kinetic damage lands on Medium armor
        /// at 50 % and on Building at 30 %, which the old rule could not see.
        /// </para>
        /// <para>
        /// The enemy HQ still short-circuits, and that is NOT a weight the
        /// score could outvote: destroying it decides the match (D-077). A
        /// win condition is not a preference.
        /// </para>
        /// <para>
        /// The score is per ARMY, not per unit, because one shared target is
        /// what this call returns. For the homogeneous army the AI builds
        /// today the ordering is identical to the per-attacker formula: the
        /// mean over n identical attackers is the single attacker's value.
        /// Mixed armies average, and per-unit targeting is a separate step.
        /// </para>
        /// <para>
        /// Integers throughout — <c>DamageMatrix</c> already speaks integer
        /// percent (100 == 1.00) — and ties break on the lower raw entity id,
        /// so the result never depends on the visibility list's order.
        /// </para>
        /// </summary>
        private uint FindBestVisibleEnemyByScore(List<UnitState> army, out int cellX, out int cellY)
        {
            cellX = 0;
            cellY = 0;
            var visible = new List<EntityId>();
            _fogOfWar.GetVisibleEntities(_aiPlayerId, visible);

            uint bestRaw = 0;
            long bestScore = 0;
            int bestX = 0, bestY = 0;

            for (int i = 0; i < visible.Count; i++)
            {
                if (!_entityManager.TryGetUnit(visible[i], out UnitState u)) continue;

                // Own units are skipped HERE and nowhere else: ValidateDomain
                // has no AttackTarget case and the fire phase checks range and
                // visibility but never the owner, so an explicit order at an
                // own unit would actually fire. The auto-acquisition filters
                // hostile strictly; the command path does not.
                if (u.PlayerId == _aiPlayerId) continue;

                uint raw = UnitCommandStateView.ToRawEntityId(u.Id);
                if (raw == 0) continue;
                int x = GridCellOf(u.Transform.PositionX);
                int y = GridCellOf(u.Transform.PositionY);

                if (u.Role == UnitRole.HQ)
                {
                    cellX = x;
                    cellY = y;
                    return raw; // win condition (D-077), not a preference
                }

                long score = ScoreTarget(army, in u, x, y);
                if (bestRaw == 0 || score > bestScore || (score == bestScore && raw < bestRaw))
                {
                    bestScore = score;
                    bestRaw = raw;
                    bestX = x;
                    bestY = y;
                }
            }

            cellX = bestX;
            cellY = bestY;
            return bestRaw;
        }

        /// <summary>
        /// One target's score for the whole army. All four terms are integers
        /// and all four weights come from the profile (Nova.AI.Data), so
        /// tuning never touches this file.
        /// </summary>
        private long ScoreTarget(List<UnitState> army, in UnitState target, int targetCellX, int targetCellY)
        {
            FactionId targetFaction = _economy.GetSlotFaction(target.PlayerId);
            ArmorClass armor = WeaponProfiles.GetArmorClass(targetFaction, target.Role);

            long damageSum = 0;
            long distanceSum = 0;
            for (int i = 0; i < army.Count; i++)
            {
                UnitState attacker = army[i];
                WeaponProfile weapon = WeaponProfiles.Get(
                    _economy.GetSlotFaction(attacker.PlayerId), attacker.Role);
                damageSum += DamageMatrix.Resolve(weapon.AttackDamage, weapon.DamageType, armor);

                int ax = GridCellOf(attacker.Transform.PositionX);
                int ay = GridCellOf(attacker.Transform.PositionY);
                int dx = ax > targetCellX ? ax - targetCellX : targetCellX - ax;
                int dy = ay > targetCellY ? ay - targetCellY : targetCellY - ay;
                distanceSum += dx > dy ? dx : dy;
            }

            // Integer mean, so the weights keep their meaning independent of
            // army size. Truncation is deterministic and identical on both
            // machines — that is the only property that matters here.
            int divisor = army.Count > 0 ? army.Count : 1;
            long effectiveDamage = damageSum / divisor;
            long distance = distanceSum / divisor;

            // What the target does back. An unarmed harvester at the fence
            // scores 0 threat, correctly.
            long threat = WeaponProfiles.Get(targetFaction, target.Role).AttackDamage;

            long missingHealthPercent = target.MaxHealth > 0
                ? 100 - ((long)target.CurrentHealth * 100 / target.MaxHealth)
                : 0;

            AiProfile p = _profile.Profile;
            return (p.TargetDamageWeight * effectiveDamage)
                 + (p.TargetThreatWeight * threat)
                 + (p.TargetFinishWeight * missingHealthPercent)
                 - (p.TargetDistanceWeight * distance);
        }

        /// <summary>Units of <paramref name="unitDefId"/> still queued (not yet spawned) at one producer.</summary>
        private int CountQueuedAt(uint buildingRaw, ushort unitDefId)
        {
            int total = 0;
            if (_production.TryGetProducer(buildingRaw, out int entryCount, out _, out _))
            {
                for (int e = 0; e < entryCount; e++)
                {
                    if (_production.TryGetQueueEntry(buildingRaw, e, out ushort defId, out ushort remainingCount, out _)
                        && defId == unitDefId)
                    {
                        total += remainingCount;
                    }
                }
            }
            return total;
        }

        // ------------------------------------------------------------------
        // Intent submission (the ONLY way the AI acts)
        // ------------------------------------------------------------------

        /// <summary>
        /// Submits one single-record payload as a canonical intent to the AI
        /// peer's ingress. The intake verdict is deliberately not inspected:
        /// every decision is re-derived on the next cadence, so a rejection
        /// (backpressure, a stale assumption) is retried, never spammed.
        /// </summary>
        private void Submit<TPayload>(in TPayload payload)
            where TPayload : struct, ICommandPayload
        {
            _ingress.TrySubmitIntent(CommandIntent.Create(payload), out _);
        }

        /// <summary>
        /// Submits an entity-list command, chunked at
        /// <see cref="CommandLimits.MaxEntityIdsPerCommand"/> like the UI
        /// dispatcher. Callers pass ascending-sorted raw ids — the canonical
        /// entity list is a structural requirement of the ingress.
        /// </summary>
        private void SubmitEntityList(List<uint> sortedRaws, Func<uint[], CommandIntent> intentFactory)
        {
            for (int offset = 0; offset < sortedRaws.Count; offset += CommandLimits.MaxEntityIdsPerCommand)
            {
                int length = Math.Min(CommandLimits.MaxEntityIdsPerCommand, sortedRaws.Count - offset);
                var chunk = new uint[length];
                sortedRaws.CopyTo(offset, chunk, 0, length);
                _ingress.TrySubmitIntent(intentFactory(chunk), out _);
            }
        }

        // ------------------------------------------------------------------
        // Small deterministic predicates
        // ------------------------------------------------------------------

        /// <summary>True for the six MS-1 combat unit roles (12..17); Builder, Harvester and sites are unarmed.</summary>
        private static bool IsCombatRole(UnitRole role)
        {
            return role >= UnitRole.BasicInfantry && role <= UnitRole.Artillery;
        }

        /// <summary>The canonical world-to-grid mapping (floor), clamped into the construction grid.</summary>
        private static int GridCellOf(SimFixed position)
        {
            return Math.Max(0, Math.Min(ConstructionSystem.GridSize - 1, SimFixed.WorldToGrid(position)));
        }

        /// <summary>Chebyshev distance of a cell to a building footprint rectangle (0 = inside).</summary>
        private static int ChebyshevToFootprint(int cellX, int cellY, int originX, int originY)
        {
            int f = SimDefinitions.BuildingFootprintCells;
            int dx = Math.Max(0, Math.Max(originX - cellX, cellX - (originX + f - 1)));
            int dy = Math.Max(0, Math.Max(originY - cellY, cellY - (originY + f - 1)));
            return Math.Max(dx, dy);
        }

        /// <summary>The documented construction/economy reach rule: the cell is the footprint rectangle or Chebyshev-adjacent (&lt;= 1).</summary>
        private static bool IsInReachOfFootprint(int cellX, int cellY, int originX, int originY)
        {
            return ChebyshevToFootprint(cellX, cellY, originX, originY) <= 1;
        }

        /// <summary>The economy's harvest reach rule: the unit's cell is the field cell or Chebyshev-adjacent (&lt;= 1 per axis).</summary>
        private static bool IsInFieldReach(int cellX, int cellY, int fieldX, int fieldY)
        {
            return Math.Abs(cellX - fieldX) <= 1 && Math.Abs(cellY - fieldY) <= 1;
        }

        /// <summary>
        /// The economy's deposit reach rule (EconomySystem.HasOwnRefineryInReach):
        /// footprint-adjacent expressed around the footprint CENTER cell —
        /// Chebyshev distance &lt;= 1 + (footprint - 1) / 2 = 2 for the pinned
        /// 3x3 footprint.
        /// </summary>
        private static bool IsInDepositReach(int cellX, int cellY, int refineryCenterX, int refineryCenterY)
        {
            int reach = 1 + (SimDefinitions.BuildingFootprintCells - 1) / 2;
            return Math.Abs(cellX - refineryCenterX) <= reach && Math.Abs(cellY - refineryCenterY) <= reach;
        }

        /// <summary>True when the unit already walks toward exactly this cell (re-issue suppression).</summary>
        private static bool AlreadyHeadingTo(in UnitState unit, int cellX, int cellY)
        {
            return unit.IsMoving && unit.TargetGridPos.IsValid
                && unit.TargetGridPos.X == cellX && unit.TargetGridPos.Y == cellY;
        }
    }
}
