using System;
using Nova.Core;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Economy
{
    /// <summary>
    /// Canonical economy system of the harvest slice: per-slot credits and
    /// power grids (docs/tech/SimulationCore.md section 2, phase 2) plus the
    /// finite Aetherium harvest cycle (section 2, phase 3). Deterministic,
    /// pure integer/fixed-point, zero engine dependencies.
    /// <para>
    /// Tick order: the system covers phases 2 and 3 of the canonical pipeline
    /// and must therefore be registered BEFORE pathfinding/movement (phase
    /// 6). Consequence, stated explicitly: a harvester already in reach of
    /// its field collects its per-tick rate BEFORE movement runs in the same
    /// tick, and the power balance a consumer reads inside one tick reflects
    /// the living buildings at the start of that tick — a building destroyed
    /// by combat (phase 8) lowers <see cref="PlayerEconomyState.PowerProvided"/>
    /// from the next tick on.
    /// </para>
    /// <para>
    /// Phase 2 (economy and power): provided/required are recomputed from
    /// the living building-role entities in strict ascending entity-index
    /// order. The per-role power figures come from the canonical definition
    /// table <see cref="Definitions.SimDefinitions"/>, resolved through the
    /// owner slot's faction (Buildings.md section 2: an Alliance Power plant
    /// provides 100, a Legion one 80; the HQ provides 30 for both).
    /// Buildings-as-role-entities is the documented minimal building model
    /// of this slice; construction output becomes a building-role entity on
    /// completion (construction domain, phase 4).
    /// </para>
    /// <para>
    /// Phase 3 (Aetherium): every harvester with a standing
    /// <see cref="UnitState.HarvestFieldId"/> order whose grid cell is the
    /// field's cell or adjacent (Chebyshev distance &lt;= 1, documented
    /// reach rule) gathers <see cref="HarvestRateAE"/> per tick, bounded by
    /// the field's remaining reserve and the free cargo space. A FULL cargo
    /// no longer resolves the order: the harvester flips to
    /// <see cref="UnitState.IsReturningCargo"/> and KEEPS its
    /// <see cref="UnitState.HarvestFieldId"/>, so after the deposit it
    /// resumes the very same field by itself. An EXHAUSTED field still
    /// resolves the order — the harvester goes idle and keeps whatever cargo
    /// it holds. Out-of-reach orders are HELD, never dropped —
    /// closing the distance is Movement's concern. A harvester with a
    /// standing <see cref="UnitState.IsReturningCargo"/> order deposits its
    /// full cargo at an own refinery in reach (same Chebyshev rule): credits
    /// rise by the cargo amount THAT FITS under the storage ceiling (16.4 —
    /// overflow is forfeit) and the return leg resolves.
    /// </para>
    /// <para>
    /// Storage ceiling (16.4, #53, D-024/D-096): the AE account has a derived
    /// upper bound — a completed HQ provides the 2.000 AE base (per HQ),
    /// every completed Storage adds 2.000, scanned from the living building
    /// stock on every read and NEVER stored (a stored cap would be a state
    /// field and a format break). All income and refunds route through
    /// <see cref="DepositCapped"/> and clamp at the ceiling ("Überschuss
    /// verfällt"); an EXISTING balance above it decays by 25% of the excess
    /// once per second (tick % <see cref="ExcessDecayIntervalTicks"/>, integer
    /// floor, minimum 1 AE). The decay IS the D-024 "25% loss on
    /// destruction", carried without an event: a destroyed or sold storage
    /// drops the ceiling and the decay is the loss. Keyed to the tick number
    /// — stateless and restore-safe.
    /// </para>
    /// <para>
    /// Auto-cycle (harvest -&gt; return -&gt; harvest, Q-040 resolution): the
    /// cycle is carried entirely by the RETAINED field id and adds no new
    /// state — while returning, both order fields are set at once, which is
    /// exactly why the return leg has dispatch priority in
    /// <see cref="ExecuteHarvest"/>. An EXPLICIT ReturnCargo command keeps
    /// its previous semantics: the command view clears
    /// <see cref="UnitState.HarvestFieldId"/>, so a manually ordered return
    /// ends in idle and never auto-resumes; Stop clears both fields and
    /// cancels the cycle outright. Caveat, stated explicitly: the cycle only
    /// closes when the harvester actually stands in reach of an own refinery.
    /// This system issues NO movement (phase 6 concern), so a harvester whose
    /// refinery is out of reach holds its full cargo indefinitely until
    /// something else moves it.
    /// </para>
    /// <para>
    /// Harvester contention (review finding P2-3): when several harvesters
    /// work the same field and the remaining reserve is smaller than the
    /// combined per-tick demand, the strict ascending entity-index sweep
    /// decides — the harvester with the LOWER index collects first, and a
    /// later one may find the field already exhausted inside the same tick.
    /// This is deterministic and spec-conform (SimulationCore.md section 2
    /// phase order, same precedent as the combat duel asymmetry) but
    /// economy-relevant and therefore stated explicitly.
    /// </para>
    /// <para>
    /// Scope (G2 reservation, D-010): fields are finite and never regrow,
    /// spread or take overharvest damage; there is no mother node and no
    /// depletion warning. The slot faction (<see cref="PlayerEconomyState.Faction"/>)
    /// IS modeled — it selects the definition row behind every cost, build
    /// time, power figure and weapon profile, AND the harvester cargo
    /// capacity: gathering clamps at the owner faction's
    /// <see cref="Definitions.SimUnitDefinition.CargoCapacityAE"/> (Allianz
    /// 330, Legion 300 — quality/content/mvp-v1.json
    /// factions[i].identity.harvesterCargoAE).
    /// </para>
    /// <para>
    /// State (snapshot block <see cref="SnapshotBlockIds.Economy"/>, v2):
    /// per slot credits/provided/required plus the slot faction, plus every
    /// field's id, position and remaining reserve. Cargo and harvest orders
    /// live with the entities
    /// (entity store block, v4) — exactly one home per value, nothing
    /// duplicated. Hash-sensitive by construction; restore is two-phase and
    /// validates credits &gt;= 0, reserves &gt;= 0, declared faction values
    /// and unique nonzero field
    /// ids (entity-side cargo bounds are validated by the entity store).
    /// </para>
    /// </summary>
    public sealed class EconomySystem : IStatefulSimSystem, ISlotFactionLookup
    {
        /// <summary>
        /// Serialization version of the economy snapshot block. v2 adds the
        /// per-slot <see cref="FactionId"/> byte (faction identity slice);
        /// v1 blocks are rejected, not migrated — the pre-G1 format reset is
        /// deliberate and free of evidence.
        /// </summary>
        public const byte StateVersion = 2;

        /// <summary>Player slots (D-058 reserves eight; MS-1 activates two).</summary>
        public const int MaxPlayers = 8;

        /// <summary>Format capacity for Aetherium fields (map content: 5 fields in mvp-v1).</summary>
        public const int MaxFields = 64;

        /// <summary>Provisional harvest rate in AE per tick per harvester (Q-040 candidate).</summary>
        public const int HarvestRateAE = 2;

        /// <summary>
        /// Starting Aetherium credits of the canonical match (D-077 —
        /// quality/content/mvp-v1.json startStatePerPlayer.aetheriumAE):
        /// 3.000 AE, so the opening affords the classic loop (Refinery
        /// first, then Harvesters). The single named home of the value —
        /// MatchRunner plumbs it into every Unity-hosted match and
        /// tools/Nova.SimRunner's Determinism10000Scenario passes the same
        /// constant, so both hosts hash the identical initial state.
        /// </summary>
        public const long CanonicalMatchStartingCreditsAE = 3000L;

        /// <summary>
        /// 16.4 (#53, D-024/D-096): AE capacity base per completed HQ.
        /// Deliberately below the canonical start balance (3.000 AE, D-077):
        /// the start stock stays (existing balances only decay), but fresh
        /// income forfeits until the player builds storage — the D-024 silo
        /// pressure from the first minute.
        /// </summary>
        public const long HqBaseCapacityAE = 2000L;

        /// <summary>16.4 (#53, D-024): AE capacity bonus per completed Storage.</summary>
        public const long StorageCapacityBonusAE = 2000L;

        /// <summary>16.4 (#53, D-024): excess balance decay in percent of the excess per decay tick (integer floor, minimum 1 AE).</summary>
        public const int ExcessDecayPercent = 25;

        /// <summary>16.4 (#53): excess decay cadence — once per second on the canonical 10 Hz clock, keyed to the tick number (stateless, restore-safe).</summary>
        public const int ExcessDecayIntervalTicks = 10;

        /// <summary>
        /// Faction-resolved harvester cargo capacities, indexed by raw
        /// <see cref="FactionId"/> and resolved once from
        /// <see cref="Definitions.SimDefinitions"/>: the tick path never
        /// re-scans the definition table, and a slot's faction cannot change
        /// after kernel start (the <see cref="SetSlotFaction"/> guard), so a
        /// static cache cannot go stale.
        /// </summary>
        private static readonly int[] CargoCapacityByFaction =
        {
            Definitions.SimDefinitions.HarvesterCargoCapacityAE(FactionId.Alliance),
            Definitions.SimDefinitions.HarvesterCargoCapacityAE(FactionId.Legion),
        };

        private readonly EntityManager _entityManager;
        private readonly PlayerEconomyState[] _players;
        private readonly AetheriumField[] _fields;
        private int _fieldCount;
        private SimulationKernel _kernel;

        /// <summary>
        /// Construction-site lookup bound by the ConstructionSystem
        /// constructor (16.3, #44): a site entity carries its definition role
        /// now, so the power recompute needs the site's own register to tell
        /// "unfinished" from "completed". Null in a rig without construction
        /// — every building-role entity then counts, the pre-16.3 behaviour.
        /// </summary>
        private Func<EntityId, bool> _isSiteLookup;

        public string Name => "EconomySystem";

        public ushort StateBlockId => SnapshotBlockIds.Economy;

        /// <summary>Number of registered Aetherium fields.</summary>
        public int FieldCount => _fieldCount;

        /// <summary>
        /// The parameterless-caller default stays 1.000 AE: a plain library
        /// default for tests and fixtures, NOT the match rule. The canonical
        /// match starts with <see cref="CanonicalMatchStartingCreditsAE"/>
        /// (3.000 AE, D-077), passed explicitly by the match hosts.
        /// </summary>
        public EconomySystem(EntityManager entityManager, long startingCredits = 1000)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _players = new PlayerEconomyState[MaxPlayers];
            for (byte i = 0; i < MaxPlayers; i++)
            {
                _players[i] = new PlayerEconomyState(i, startingCredits);
            }
            _fields = new AetheriumField[MaxFields];
        }

        public void Initialize(SimulationKernel kernel)
        {
            _kernel = kernel;
            kernel?.Logger.LogInfo(
                $"[{Name}] Initialized canonical economy ({MaxPlayers} slots, harvest rate {HarvestRateAE} AE/tick).");
        }

        /// <summary>
        /// Binds the construction site's own register as the "is this entity
        /// an unfinished site" lookup (16.3, #44). Called ONCE by the
        /// ConstructionSystem constructor — hosts never wire this themselves.
        /// The lookup is read-only against the site table and moves no state
        /// into the economy, so the snapshot layout is untouched.
        /// </summary>
        public void BindSiteLookup(Func<EntityId, bool> isSiteLookup)
        {
            _isSiteLookup = isSiteLookup;
        }

        /// <summary>
        /// 16.4 (#53, D-024/D-096): the slot's AE ceiling, DERIVED from the
        /// living building stock on every read — never stored (a stored cap
        /// would be a state-field and format break). A completed HQ provides
        /// the 2.000 AE base (per HQ), every completed Storage adds 2.000.
        /// Sites are excluded via the bound lookup: a half-built silo holds
        /// nothing. Without the lookup (construction-free rigs) every
        /// building-role entity counts. Integer scan in ascending entity
        /// order — deterministic, restore-safe.
        /// </summary>
        public long CapacityFor(byte playerId)
        {
            long capacity = 0;
            UnitState[] units = _entityManager.RawUnits;
            int count = _entityManager.Capacity;
            for (int i = 0; i < count; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != playerId) continue;
                if (unit.Role == UnitRole.HQ)
                {
                    if (_isSiteLookup != null && _isSiteLookup(unit.Id)) continue;
                    capacity += HqBaseCapacityAE;
                }
                else if (unit.Role == UnitRole.Storage)
                {
                    if (_isSiteLookup != null && _isSiteLookup(unit.Id)) continue;
                    capacity += StorageCapacityBonusAE;
                }
            }
            return capacity;
        }

        /// <summary>
        /// 16.4 (#53, D-024): the capped deposit — the ONLY way income and
        /// refunds should land. What does not fit under <see cref="CapacityFor"/>
        /// is forfeit ("Überschuss verfällt"); an existing balance above the
        /// ceiling is NOT touched here (it decays per second, see ExecuteTick).
        /// Returns the amount actually deposited (0 when the account is at or
        /// above the ceiling).
        /// </summary>
        public long DepositCapped(byte playerId, long amount)
        {
            if (amount <= 0 || playerId >= MaxPlayers) return 0;
            ref PlayerEconomyState eco = ref _players[playerId];
            long room = CapacityFor(playerId) - eco.AetheriumCredits;
            if (room <= 0) return 0;
            long deposited = Math.Min(amount, room);
            eco.AddCredits(deposited);
            return deposited;
        }

        /// <summary>Mutable access to one slot's economy state (slot must be in [0, MaxPlayers)).</summary>
        public ref PlayerEconomyState GetPlayerEconomy(byte playerId)
        {
            if (playerId >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), $"PlayerId must be between 0 and {MaxPlayers - 1}.");
            }
            return ref _players[playerId];
        }

        /// <summary>
        /// Registers an Aetherium field at match setup (host content wiring).
        /// Field ids are stable, nonzero and unique; the reserve must be
        /// positive. Returns false without mutating when the id collides, the
        /// position is invalid or the capacity is exhausted.
        /// </summary>
        public bool TryAddField(ushort fieldId, GridPos2D gridPos, long reserveAE)
        {
            if (fieldId == 0 || !gridPos.IsValid || reserveAE <= 0 || _fieldCount >= MaxFields)
            {
                return false;
            }
            for (int i = 0; i < _fieldCount; i++)
            {
                if (_fields[i].FieldId == fieldId)
                {
                    return false;
                }
            }
            _fields[_fieldCount++] = new AetheriumField(fieldId, gridPos, reserveAE);
            return true;
        }

        /// <summary>Read-only lookup of a registered field by id.</summary>
        public bool TryGetField(ushort fieldId, out AetheriumField field)
        {
            for (int i = 0; i < _fieldCount; i++)
            {
                if (_fields[i].FieldId == fieldId)
                {
                    field = _fields[i];
                    return true;
                }
            }
            field = default;
            return false;
        }

        /// <summary>
        /// Nearest field with reserve left to a grid cell, false when every
        /// registered field is exhausted (or none is registered). Ascending
        /// registration-index scan, deterministic: Chebyshev distance decides
        /// and a tie resolves to the LOWER index — never to a discovery
        /// order. Exhausted fields are skipped: an exhausted target would
        /// resolve a fresh harvest order on its first tick and teach the
        /// grant that issues it (16.1, #43) nothing.
        /// </summary>
        public bool TryFindNearestField(int gridX, int gridY, out ushort fieldId)
        {
            fieldId = 0;
            int bestDistance = int.MaxValue;
            for (int i = 0; i < _fieldCount; i++)
            {
                ref readonly AetheriumField field = ref _fields[i];
                if (field.IsExhausted)
                {
                    continue;
                }
                int distance = Math.Max(
                    Math.Abs(gridX - field.GridPos.X),
                    Math.Abs(gridY - field.GridPos.Y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    fieldId = field.FieldId;
                }
            }
            return fieldId != 0;
        }

        /// <summary>
        /// The faction one slot plays (economy block v2). Part of the hashed
        /// state, so every consumer of faction-differentiated content —
        /// power figures, build costs, weapon profiles — reads it here rather
        /// than carrying a second copy.
        /// </summary>
        public FactionId GetSlotFaction(byte playerId)
        {
            if (playerId >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), $"PlayerId must be between 0 and {MaxPlayers - 1}.");
            }
            return _players[playerId].Faction;
        }

        /// <summary>
        /// Assigns a slot's faction at match setup (host content wiring, same
        /// contract as <see cref="TryAddField"/>): call before the first tick,
        /// so the faction is bound into the hashed initial state and the match
        /// fingerprint. Values outside the declared <see cref="FactionId"/>
        /// range are rejected.
        /// <para>
        /// GUARD: once this system is registered with a kernel, the
        /// assignment is legal only while that kernel has never started —
        /// <c>CurrentTick == Tick.Zero &amp;&amp; !IsRunning</c>. A faction
        /// change after <c>Start()</c> would rewrite hashed state the match
        /// fingerprint was already computed from (and combat/economy have
        /// already resolved against), so it throws
        /// <see cref="InvalidOperationException"/> and leaves the state
        /// untouched. An economy that was never registered with a kernel has
        /// no tick stream to protect and is therefore unguarded (isolated
        /// test harnesses drive it directly).
        /// </para>
        /// </summary>
        public void SetSlotFaction(byte playerId, FactionId faction)
        {
            if (playerId >= MaxPlayers)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), $"PlayerId must be between 0 and {MaxPlayers - 1}.");
            }
            if (faction != FactionId.Alliance && faction != FactionId.Legion)
            {
                throw new ArgumentOutOfRangeException(nameof(faction), faction, "unknown faction");
            }
            if (_kernel != null && (_kernel.IsRunning || _kernel.CurrentTick != Tick.Zero))
            {
                throw new InvalidOperationException(
                    $"[{Name}] SetSlotFaction is legal only before Kernel.Start() " +
                    $"(CurrentTick {_kernel.CurrentTick}, IsRunning {_kernel.IsRunning}): the faction " +
                    "is part of the hashed initial state and the match fingerprint.");
            }
            _players[playerId].Faction = faction;
        }

        /// <summary>
        /// Phases 2 and 3 of the canonical tick (SimulationCore.md section
        /// 2): power recompute, then the harvest cycle — both in strict
        /// ascending entity-index order, before movement runs (registration
        /// order; see class remarks). Once per second the excess-balance
        /// decay runs (16.4, #53, D-024): a balance above the derived
        /// ceiling loses 25% of the excess per decay tick (integer floor,
        /// minimum 1 AE, so it always converges). This IS the "25% loss on
        /// destruction", carried without an event: a destroyed (or sold)
        /// storage drops the ceiling and the decay is the loss. Keyed to the
        /// tick number — stateless and restore-safe, no remembered event.
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            RecomputePower();
            ExecuteHarvest();
            if (tick.Value % ExcessDecayIntervalTicks == 0)
            {
                DecayExcessBalances();
            }
        }

        /// <summary>The per-second excess decay (16.4): every slot above its derived ceiling loses a quarter of the excess.</summary>
        private void DecayExcessBalances()
        {
            for (byte p = 0; p < MaxPlayers; p++)
            {
                ref PlayerEconomyState eco = ref _players[p];
                long excess = eco.AetheriumCredits - CapacityFor(p);
                if (excess <= 0) continue;
                long loss = Math.Max(1L, excess * ExcessDecayPercent / 100L);
                eco.AetheriumCredits -= loss;
            }
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// Phase 2: provided/required per slot from the living building-role
        /// entities (ascending index). The power figures come from the
        /// canonical definition table (<see cref="Definitions.SimDefinitions"/>)
        /// and are FACTION-RESOLVED: the entity's owner slot selects the row
        /// (a Legion Schwerer Generator feeds 80, an Alliance Fusionsreaktor
        /// 100). Mobile roles and construction sites (role
        /// <see cref="UnitRole.Unit"/>) draw nothing.
        /// </summary>
        private void RecomputePower()
        {
            for (int p = 0; p < MaxPlayers; p++)
            {
                _players[p].PowerProvided = 0;
                _players[p].PowerRequired = 0;
            }

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId >= MaxPlayers) continue;

                if (Definitions.SimDefinitions.TryGetBuilding(
                        _players[unit.PlayerId].Faction, unit.Role, out Definitions.SimBuildingDefinition building))
                {
                    _players[unit.PlayerId].PowerProvided += building.PowerProvided;
                    _players[unit.PlayerId].PowerRequired += building.PowerRequired;
                }
                // Mobile roles (Unit, Builder, Harvester and the combat unit
                // roles) draw no power in this slice.
            }
        }

        /// <summary>
        /// Phase 3: the harvest cycle in strict ascending entity-index order
        /// — standing harvest orders gather into cargo, standing return
        /// orders deposit cargo into credits (rules in the class remarks).
        /// </summary>
        private void ExecuteHarvest()
        {
            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref UnitState unit = ref units[i];
                // PlayerId bounds: the cargo ceiling and the deposit both
                // index the per-slot state, so an out-of-range owner harvests
                // nothing (same guard as RecomputePower).
                if (!unit.IsActive || unit.Role != UnitRole.Harvester || unit.PlayerId >= MaxPlayers) continue;

                // The return leg wins the dispatch: during an auto-cycle the
                // harvester carries BOTH a returning flag and its retained
                // field id, and the cargo must be delivered before the same
                // field is worked again. Command-issued orders are unaffected
                // — the command view keeps the two fields mutually exclusive.
                if (unit.IsReturningCargo)
                {
                    ExecuteReturnOrder(ref unit);
                }
                else if (unit.HarvestFieldId != 0)
                {
                    ExecuteHarvestOrder(ref unit);
                }
            }
        }

        /// <summary>
        /// One harvest order: in reach, bounded by reserve and free cargo;
        /// starts the return leg on a full cargo (field id retained), resolves
        /// to idle on an exhausted field, holds out of reach.
        /// </summary>
        private void ExecuteHarvestOrder(ref UnitState unit)
        {
            int fieldIndex = IndexOfField(unit.HarvestFieldId);
            if (fieldIndex < 0)
            {
                // The field id no longer exists (never produced by canonical
                // host setup; defensive): the order cannot proceed and
                // resolves instead of spinning forever.
                unit.HarvestFieldId = 0;
                return;
            }

            ref AetheriumField field = ref _fields[fieldIndex];
            if (field.IsExhausted)
            {
                unit.HarvestFieldId = 0;
                return;
            }

            if (!IsInReach(in unit, field.GridPos)) return; // held, not dropped

            // The cargo ceiling is the OWNER FACTION's, not a flat constant:
            // an Alliance harvester loads 330, a Legion one 300.
            int cargoCapacity = CargoCapacityByFaction[(int)_players[unit.PlayerId].Faction];
            long freeCargo = cargoCapacity - unit.CargoAE;
            long gathered = Math.Min(HarvestRateAE, Math.Min(field.RemainingAE, freeCargo));
            if (gathered <= 0)
            {
                // The field is not exhausted (checked above) and the rate is
                // positive, so the only way to gather nothing is a full
                // cargo: start the return leg, keeping the field id.
                unit.IsReturningCargo = true;
                return;
            }

            unit.CargoAE += (int)gathered;
            field.RemainingAE -= gathered;

            if (unit.CargoAE >= cargoCapacity)
            {
                // Full cargo: return first, then auto-resume THIS field. The
                // retained field id is the entire auto-cycle mechanism — it
                // deliberately takes precedence over an exhausted field so a
                // final load still gets delivered instead of being stranded.
                unit.IsReturningCargo = true;
            }
            else if (field.IsExhausted)
            {
                // Exhaustion with a partial load resolves the order as before
                // — the harvester goes idle and keeps its cargo.
                unit.HarvestFieldId = 0;
            }
        }

        /// <summary>
        /// One return order: deposits the full cargo at an own refinery in
        /// reach (credits rise by exactly the cargo); holds out of reach.
        /// Clearing the returning flag alone resumes an auto-cycle, because
        /// the retained <see cref="UnitState.HarvestFieldId"/> is picked up by
        /// the harvest branch on the next tick. A command-issued return
        /// carries no field id and therefore ends in idle.
        /// </summary>
        private void ExecuteReturnOrder(ref UnitState unit)
        {
            if (unit.CargoAE <= 0)
            {
                unit.IsReturningCargo = false;
                return;
            }

            if (!HasOwnRefineryInReach(in unit)) return; // held, not dropped

            DepositCapped(unit.PlayerId, unit.CargoAE); // 16.4: capped at the derived ceiling — overflow is forfeit
            unit.CargoAE = 0;
            unit.IsReturningCargo = false;
        }

        /// <summary>Chebyshev reach rule: the unit's grid cell is the target cell or adjacent (&lt;= 1 per axis).</summary>
        private static bool IsInReach(in UnitState unit, GridPos2D target)
        {
            int ux = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionX));
            int uy = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionY));
            return Math.Abs(ux - target.X) <= 1 && Math.Abs(uy - target.Y) <= 1;
        }

        /// <summary>
        /// True when an active own refinery stands in reach of the unit
        /// (ascending entity-index scan, first hit decides). Reach is measured
        /// against the building FOOTPRINT, not its entity cell: the entity
        /// sits at the footprint centre (ConstructionSystem.SpawnBuildingEntity
        /// spawns at origin+1), so the old centre-cell Chebyshev-1 check
        /// demanded the harvester stand INSIDE the building — the canonical
        /// opening (harvesters at (7,6)/(7,7), refinery footprint (8,4)-(10,6),
        /// centre (9,5)) could never deposit, and the full-cargo return leg
        /// held forever with credits frozen. The footprint rule widens the
        /// centre distance by the footprint radius, so "adjacent to any
        /// footprint cell" is the same Chebyshev-1 rule the field side uses.
        /// </summary>
        private bool HasOwnRefineryInReach(in UnitState unit)
        {
            // Footprint radius around the centre cell for odd footprint sizes
            // (MS-1 pins 3x3, Q-040 candidate): reach 1 + (3-1)/2 = 2.
            int reach = 1 + (Definitions.SimDefinitions.BuildingFootprintCells - 1) / 2;

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState candidate = ref units[i];
                if (!candidate.IsActive || candidate.Role != UnitRole.Refinery) continue;
                if (candidate.PlayerId != unit.PlayerId) continue;

                int rx = Math.Max(0, SimFixed.WorldToGrid(candidate.Transform.PositionX));
                int ry = Math.Max(0, SimFixed.WorldToGrid(candidate.Transform.PositionY));
                int ux = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionX));
                int uy = Math.Max(0, SimFixed.WorldToGrid(unit.Transform.PositionY));
                if (Math.Abs(ux - rx) <= reach && Math.Abs(uy - ry) <= reach)
                {
                    return true;
                }
            }
            return false;
        }

        private int IndexOfField(ushort fieldId)
        {
            for (int i = 0; i < _fieldCount; i++)
            {
                if (_fields[i].FieldId == fieldId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Block content v2: version, then per slot (ascending) credits
        /// int64, power provided/required int32 and the faction uint8, then
        /// the field count and per field (ascending registration order) id
        /// uint16, grid x/y uint16 and remaining reserve int64. Hash-sensitive
        /// by construction: any credit, power, faction or reserve change moves
        /// the block bytes and therefore the canonical state hash — the
        /// faction assignment is part of the initial state hash by exactly
        /// this mechanism.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            for (int p = 0; p < MaxPlayers; p++)
            {
                writer.WriteInt64(_players[p].AetheriumCredits);
                writer.WriteInt32(_players[p].PowerProvided);
                writer.WriteInt32(_players[p].PowerRequired);
                writer.WriteUInt8((byte)_players[p].Faction);
            }
            writer.WriteUInt16(unchecked((ushort)_fieldCount));
            for (int i = 0; i < _fieldCount; i++)
            {
                writer.WriteUInt16(_fields[i].FieldId);
                writer.WriteUInt16(_fields[i].GridPos.X);
                writer.WriteUInt16(_fields[i].GridPos.Y);
                writer.WriteInt64(_fields[i].RemainingAE);
            }
        }

        /// <summary>Fully validates an economy block without mutating the system.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _);
        }

        /// <summary>
        /// Restores slots and fields; malformed input returns false and
        /// leaves the system untouched (two-phase contract of
        /// <see cref="IStatefulSimSystem"/>).
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out ParsedEconomy parsed)) return false;

            Array.Copy(parsed.Players, _players, MaxPlayers);
            Array.Clear(_fields, 0, MaxFields);
            Array.Copy(parsed.Fields, _fields, parsed.Fields.Length);
            _fieldCount = parsed.Fields.Length;
            return true;
        }

        /// <summary>Validated intermediate of a parsed economy block.</summary>
        private sealed class ParsedEconomy
        {
            public PlayerEconomyState[] Players;
            public AetheriumField[] Fields;
        }

        /// <summary>
        /// Parses and fully validates block content — exact lengths, slot
        /// invariants (credits &gt;= 0, power values &gt;= 0, faction a
        /// declared <see cref="FactionId"/>) and field
        /// invariants (nonzero unique ids, valid positions, reserves
        /// &gt;= 0) — into a commit-ready intermediate. Never mutates this
        /// system.
        /// </summary>
        private bool TryParseState(ReadOnlySpan<byte> blockContent, out ParsedEconomy parsed)
        {
            parsed = null;
            var reader = new SnapshotBlockReader(blockContent);
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;

            var players = new PlayerEconomyState[MaxPlayers];
            for (int p = 0; p < MaxPlayers; p++)
            {
                if (!reader.TryReadInt64(out long credits) || credits < 0) return false;
                if (!reader.TryReadInt32(out int provided) || provided < 0) return false;
                if (!reader.TryReadInt32(out int required) || required < 0) return false;
                if (!reader.TryReadUInt8(out byte factionRaw) || factionRaw > (byte)FactionId.Legion) return false;
                players[p] = new PlayerEconomyState((byte)p, credits, (FactionId)factionRaw)
                {
                    PowerProvided = provided,
                    PowerRequired = required,
                };
            }

            if (!reader.TryReadUInt16(out ushort fieldCount) || fieldCount > MaxFields) return false;
            var fields = new AetheriumField[fieldCount];
            for (int i = 0; i < fieldCount; i++)
            {
                if (!reader.TryReadUInt16(out ushort fieldId) || fieldId == 0) return false;
                if (!reader.TryReadUInt16(out ushort x)) return false;
                if (!reader.TryReadUInt16(out ushort y)) return false;
                if (!reader.TryReadInt64(out long remaining) || remaining < 0) return false;

                var gridPos = new GridPos2D(x, y);
                if (!gridPos.IsValid) return false;
                for (int j = 0; j < i; j++)
                {
                    if (fields[j].FieldId == fieldId) return false; // duplicate field id
                }
                fields[i] = new AetheriumField(fieldId, gridPos, remaining);
            }
            if (reader.Remaining != 0) return false;

            parsed = new ParsedEconomy { Players = players, Fields = fields };
            return true;
        }
    }
}
