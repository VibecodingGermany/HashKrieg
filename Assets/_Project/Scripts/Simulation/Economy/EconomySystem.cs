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
    /// order. Provisional values (Q-040 candidates, not ratified balancing):
    /// HQ provides 30, a Power plant provides 100, a Refinery requires 20,
    /// every other building role requires 10; mobile roles draw nothing.
    /// Buildings-as-role-entities is the documented minimal building model of
    /// this slice (no canonical construction output exists yet).
    /// </para>
    /// <para>
    /// Phase 3 (Aetherium): every harvester with a standing
    /// <see cref="UnitState.HarvestFieldId"/> order whose grid cell is the
    /// field's cell or adjacent (Chebyshev distance &lt;= 1, documented
    /// reach rule) gathers <see cref="HarvestRateAE"/> per tick, bounded by
    /// the field's remaining reserve and the free cargo space. The order
    /// resolves (unit goes idle, keeps its cargo) when the cargo is full or
    /// the field is exhausted; out-of-reach orders are HELD, never dropped —
    /// closing the distance is Movement's concern. A harvester with a
    /// standing <see cref="UnitState.IsReturningCargo"/> order deposits its
    /// full cargo at an own refinery in reach (same Chebyshev rule): credits
    /// rise by exactly the cargo amount and the order resolves. Automatic
    /// return-on-full and harvest-on-arrival behaviors are deliberately NOT
    /// part of this slice (Q-040 candidates); orders come from commands only.
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
    /// depletion warning. Faction differences are not modeled: every
    /// harvester uses <see cref="UnitState.DefaultCargoCapacityAE"/> (330;
    /// the Legion value 300 of quality/content/mvp-v1.json awaits the
    /// faction slice — Q-040 candidate).
    /// </para>
    /// <para>
    /// State (snapshot block <see cref="SnapshotBlockIds.Economy"/>, v1):
    /// per slot credits/provided/required plus every field's id, position and
    /// remaining reserve. Cargo and harvest orders live with the entities
    /// (entity store block, v4) — exactly one home per value, nothing
    /// duplicated. Hash-sensitive by construction; restore is two-phase and
    /// validates credits &gt;= 0, reserves &gt;= 0 and unique nonzero field
    /// ids (entity-side cargo bounds are validated by the entity store).
    /// </para>
    /// </summary>
    public sealed class EconomySystem : IStatefulSimSystem
    {
        /// <summary>Serialization version of the economy snapshot block.</summary>
        public const byte StateVersion = 1;

        /// <summary>Player slots (D-058 reserves eight; MS-1 activates two).</summary>
        public const int MaxPlayers = 8;

        /// <summary>Format capacity for Aetherium fields (map content: 5 fields in mvp-v1).</summary>
        public const int MaxFields = 64;

        /// <summary>Provisional harvest rate in AE per tick per harvester (Q-040 candidate).</summary>
        public const int HarvestRateAE = 2;

        /// <summary>Provisional power provided by an HQ (Q-040 candidate; covers the MS-1 start without a power plant).</summary>
        public const int HqPowerProvided = 30;

        /// <summary>Provisional power provided by a Power plant (Q-040 candidate).</summary>
        public const int PowerPlantPowerProvided = 100;

        /// <summary>Provisional power required by a Refinery (Q-040 candidate).</summary>
        public const int RefineryPowerRequired = 20;

        /// <summary>Provisional power required by any other building role (Q-040 candidate).</summary>
        public const int DefaultBuildingPowerRequired = 10;

        private readonly EntityManager _entityManager;
        private readonly PlayerEconomyState[] _players;
        private readonly AetheriumField[] _fields;
        private int _fieldCount;

        public string Name => "EconomySystem";

        public ushort StateBlockId => SnapshotBlockIds.Economy;

        /// <summary>Number of registered Aetherium fields.</summary>
        public int FieldCount => _fieldCount;

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
            kernel?.Logger.LogInfo(
                $"[{Name}] Initialized canonical economy ({MaxPlayers} slots, harvest rate {HarvestRateAE} AE/tick).");
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
        /// Phases 2 and 3 of the canonical tick (SimulationCore.md section
        /// 2): power recompute, then the harvest cycle — both in strict
        /// ascending entity-index order, before movement runs (registration
        /// order; see class remarks).
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            RecomputePower();
            ExecuteHarvest();
        }

        public void Shutdown()
        {
        }

        /// <summary>
        /// Phase 2: provided/required per slot from the living building-role
        /// entities (ascending index; provisional values, see class remarks).
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

                switch (unit.Role)
                {
                    case UnitRole.HQ:
                        _players[unit.PlayerId].PowerProvided += HqPowerProvided;
                        break;
                    case UnitRole.Power:
                        _players[unit.PlayerId].PowerProvided += PowerPlantPowerProvided;
                        break;
                    case UnitRole.Refinery:
                        _players[unit.PlayerId].PowerRequired += RefineryPowerRequired;
                        break;
                    case UnitRole.Unit:
                    case UnitRole.Builder:
                    case UnitRole.Harvester:
                        break; // mobile roles draw no power in this slice
                    default:
                        _players[unit.PlayerId].PowerRequired += DefaultBuildingPowerRequired;
                        break;
                }
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
                if (!unit.IsActive || unit.Role != UnitRole.Harvester) continue;

                if (unit.HarvestFieldId != 0)
                {
                    ExecuteHarvestOrder(ref unit);
                }
                else if (unit.IsReturningCargo)
                {
                    ExecuteReturnOrder(ref unit);
                }
            }
        }

        /// <summary>
        /// One harvest order: in reach, bounded by reserve and free cargo;
        /// resolves on full cargo or exhausted field, holds out of reach.
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

            long freeCargo = UnitState.DefaultCargoCapacityAE - unit.CargoAE;
            long gathered = Math.Min(HarvestRateAE, Math.Min(field.RemainingAE, freeCargo));
            if (gathered <= 0)
            {
                // Cargo full: the order resolves; the unit idles with its
                // load until a ReturnCargo command arrives (no auto-return
                // in this slice — Q-040 candidate).
                unit.HarvestFieldId = 0;
                return;
            }

            unit.CargoAE += (int)gathered;
            field.RemainingAE -= gathered;

            if (unit.CargoAE >= UnitState.DefaultCargoCapacityAE || field.IsExhausted)
            {
                unit.HarvestFieldId = 0;
            }
        }

        /// <summary>
        /// One return order: deposits the full cargo at an own refinery in
        /// reach (credits rise by exactly the cargo); holds out of reach.
        /// </summary>
        private void ExecuteReturnOrder(ref UnitState unit)
        {
            if (unit.CargoAE <= 0)
            {
                unit.IsReturningCargo = false;
                return;
            }

            if (!HasOwnRefineryInReach(in unit)) return; // held, not dropped

            _players[unit.PlayerId].AddCredits(unit.CargoAE);
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
        /// (Chebyshev rule; ascending entity-index scan, first hit decides).
        /// </summary>
        private bool HasOwnRefineryInReach(in UnitState unit)
        {
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
                if (Math.Abs(ux - rx) <= 1 && Math.Abs(uy - ry) <= 1)
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
        /// Block content v1: version, then per slot (ascending) credits
        /// int64, power provided/required int32, then the field count and per
        /// field (ascending registration order) id uint16, grid x/y uint16
        /// and remaining reserve int64. Hash-sensitive by construction: any
        /// credit, power or reserve change moves the block bytes and
        /// therefore the canonical state hash.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            for (int p = 0; p < MaxPlayers; p++)
            {
                writer.WriteInt64(_players[p].AetheriumCredits);
                writer.WriteInt32(_players[p].PowerProvided);
                writer.WriteInt32(_players[p].PowerRequired);
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
        /// invariants (credits &gt;= 0, power values &gt;= 0) and field
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
                players[p] = new PlayerEconomyState((byte)p, credits)
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
