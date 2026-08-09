using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;

namespace Nova.Simulation.Victory
{
    /// <summary>
    /// Canonical MS-1 victory system (docs/gamedesign/VictoryConditions.md,
    /// binding section "MS-1-Override (D-056)"). Until this system existed a
    /// match could not end at all; this is the only place a
    /// <see cref="MatchOutcome"/> is produced.
    /// <para>
    /// The D-056 contract, clause by clause:
    /// <list type="number">
    /// <item>"Auswertung nach Combat am Tickende" — this system is registered
    /// LAST in the canonical tick order (SimulationCore.md section 2), after
    /// <c>CombatSystem</c>, so it judges the state AFTER this tick's damage
    /// and deaths landed. A victory system registered before combat would
    /// decide on last tick's corpses.</item>
    /// <item>"Niederlage bei null lebenden Einheiten UND null lebenden
    /// Gebäuden einschließlich Baustellen" — a side is eliminated when it
    /// owns no living entity of any kind. Construction sites count on the
    /// building side (see <see cref="IsBuilding"/>), so an unfinished site
    /// keeps a side alive.</item>
    /// <item>"Eliminierung einer Seite: Victory.Elimination" — exactly one
    /// engaged side survives.</item>
    /// <item>"Eliminierung beider Seiten im selben Tick:
    /// Draw.MutualAnnihilation" — no engaged side survives.</item>
    /// <item>"Tick 27.000 ohne Eliminierung: Draw.TimeLimit"
    /// (<see cref="TimeLimitTick"/> = 45 min on the canonical 10 Hz clock).
    /// Elimination is evaluated FIRST, so an elimination landing exactly on
    /// tick 27,000 is a victory, not a draw — that is what "ohne
    /// Eliminierung" means.</item>
    /// <item>"bei null Gebäuden und höchstens drei Einheiten werden diese
    /// nach 600 ununterbrochenen Ticks sichtbar und zielbar; endet die
    /// Bedingung, wird der Zähler zurückgesetzt" — the last-unit reveal hold,
    /// tracked per slot in <see cref="RevealHoldTicksOf"/>. NOTE: this system
    /// owns the deterministic COUNTER and the resulting
    /// <see cref="IsRevealed"/> flag only. Making the units actually visible
    /// and targetable is a Fog-of-War/Combat concern and is NOT wired in this
    /// slice (ScopeLedger item; see the class remarks below).</item>
    /// <item>"keine automatische KI-Aufgabe und kein Spieler-Surrender-Command"
    /// — deliberately absent. No surrender command kind exists and none is
    /// added here.</item>
    /// </list>
    /// </para>
    /// <para>
    /// D-077 SECOND DEFEAT TRIGGER — HQ LOSS. Since D-077 every slot starts
    /// with a completed HQ (the match loop rework: HQ + 1 Builder + 3.000 AE,
    /// harvesters come from the Refinery), and destroying a side's HQ ends
    /// the game for that side. An ENGAGED slot is therefore defeated when
    /// EITHER it owns no living entity at all (the D-056 rule of clause 2,
    /// unchanged) OR it has lost its last living HQ building after owning at
    /// least one — the per-slot latch <see cref="HadHq"/> — even if it still
    /// owns other buildings or units. Both triggers feed the same defeated
    /// count, so <see cref="MatchOutcome.VictoryElimination"/> now has two
    /// triggers (no new outcome code: the wire format and the HUD texts stay
    /// stable) and <see cref="MatchOutcome.DrawMutualAnnihilation"/> covers
    /// any same-tick mix of them. A slot that never owned an HQ — fixtures
    /// that spawn only units — is unaffected by the HQ trigger and is judged
    /// by the D-056 rule alone. Only COMPLETED buildings carry
    /// <see cref="UnitRole.HQ"/>; a construction site carries
    /// <see cref="UnitRole.Unit"/> and never counts, so an unfinished HQ
    /// rebuild does not postpone the defeat.
    /// </para>
    /// <para>
    /// ONCE DECIDED, FINAL. The first tick that produces a decided
    /// <see cref="Outcome"/> latches it together with
    /// <see cref="WinnerSlot"/> and <see cref="DecidedTick"/>; every later
    /// tick returns immediately. A winner that is subsequently wiped out (for
    /// example by an in-flight AI order still queued behind the decision)
    /// cannot flip the result, and the latch survives snapshot save/restore
    /// because it lives in the serialized block.
    /// </para>
    /// <para>
    /// ENGAGEMENT LATCH. A side may only be eliminated once it has actually
    /// been on the map: <see cref="IsEngaged"/> latches on the first tick a
    /// slot owns at least one living entity, and only engaged slots take part
    /// in the elimination decision. Without it every fresh host — including
    /// one whose opening position has not been applied yet — would report
    /// <see cref="MatchOutcome.DrawMutualAnnihilation"/> on tick 1 because
    /// nobody owns anything. D-056 does not spell this out; it is the
    /// documented minimal reading that keeps "Eliminierung" meaning "lost
    /// what it had". A decision additionally requires at least two engaged
    /// sides, because "Eliminierung beider Seiten" presupposes two sides.
    /// </para>
    /// <para>
    /// DETERMINISM. Pure integer counting over the entity store in ascending
    /// entity-index order, ascending slot order, no wall clock, no floating
    /// point, no allocation per tick. The match outcome, the engagement
    /// latches, the had-HQ latches and the reveal holds are authoritative
    /// state and serialize into snapshot block
    /// <see cref="SnapshotBlockIds.Victory"/>, so they are part of the
    /// canonical state hash (SimulationCore.md sections 5 and 7).
    /// </para>
    /// <para>
    /// DERIVED, NOT STORED: the per-slot living unit/building/HQ counts are
    /// recomputed from the entity store on every read
    /// (<see cref="CountLiving"/>). They are intentionally NOT serialized —
    /// storing a second copy of a value the entity store already owns would
    /// let the two drift and would put a redundant derivation into the state
    /// hash.
    /// </para>
    /// <para>
    /// POST-MVP, NOT IMPLEMENTED HERE (VictoryConditions.md declares these
    /// explicitly out of the MS-1 contract): optional time limits and the
    /// <c>VictoryProfile</c> ScriptableObject, wall exemptions, manual and AI
    /// surrender, "practical defeat" hints, team/FFA/Survival/King-of-the-Hill
    /// modes, the slow-motion result flow and the <c>MatchResult</c>
    /// statistics block.
    /// </para>
    /// </summary>
    public sealed class VictorySystem : IStatefulSimSystem
    {
        /// <summary>
        /// Serialization version of the victory snapshot block. v2 (D-077)
        /// adds the per-slot had-HQ latch byte. v1 blocks are REJECTED, not
        /// migrated — a deliberate clean break under the pre-release
        /// format-reset precedent: no shipped snapshot exists that must keep
        /// loading, and a single canonical layout keeps the parser small.
        /// </summary>
        public const byte StateVersion = 2;

        /// <summary>
        /// Format capacity for player slots — the same reservation the
        /// command wire format and the economy use
        /// (<see cref="CommandLimits.ReservedPlayerSlots"/> = 8). MS-1
        /// activates two of them (D-056); the block always carries all eight
        /// so activating more slots later is not a format change.
        /// </summary>
        public const int MaxSlots = CommandLimits.ReservedPlayerSlots;

        /// <summary>
        /// D-056 time limit: tick 27,000 — exactly 45 minutes on the
        /// canonical 10 Hz clock (<see cref="SimClock.TicksPerSecond"/>).
        /// Reaching it without an elimination is
        /// <see cref="MatchOutcome.DrawTimeLimit"/>.
        /// </summary>
        public const uint TimeLimitTick = 27000;

        /// <summary>
        /// D-056 last-unit reveal hold: 600 uninterrupted ticks — 60 s at
        /// 10 Hz. The per-slot counter saturates here instead of growing
        /// without bound, which keeps the serialized state finite and makes
        /// the block's range check exact.
        /// </summary>
        public const int RevealHoldTicks = 600;

        /// <summary>D-056 reveal trigger: at most three living units while owning no building.</summary>
        public const int RevealMaxUnits = 3;

        /// <summary>Winner slot sentinel for every outcome that has no winner.</summary>
        public const byte NoWinnerSlot = 0xFF;

        private readonly EntityManager _entityManager;
        private readonly ConstructionSystem _construction;

        /// <summary>Latched: the slot has owned at least one living entity at some point.</summary>
        private readonly bool[] _engaged = new bool[MaxSlots];

        /// <summary>
        /// Latched: the slot has owned at least one living COMPLETED HQ at
        /// some point (D-077). Only a latched slot can be defeated by the
        /// HQ-loss trigger; implies the engagement latch.
        /// </summary>
        private readonly bool[] _hadHq = new bool[MaxSlots];

        /// <summary>Consecutive ticks the D-056 reveal condition held, saturating at <see cref="RevealHoldTicks"/>.</summary>
        private readonly int[] _revealHold = new int[MaxSlots];

        /// <summary>Per-tick scratch for the single counting pass; derived, never serialized.</summary>
        private readonly int[] _scratchUnits = new int[MaxSlots];
        private readonly int[] _scratchBuildings = new int[MaxSlots];
        private readonly int[] _scratchHq = new int[MaxSlots];

        private INovaLogger _logger = NullNovaLogger.Instance;

        public string Name => "VictorySystem";

        /// <summary>The canonical match result; <see cref="MatchOutcome.Undecided"/> while the match runs.</summary>
        public MatchOutcome Outcome { get; private set; } = MatchOutcome.Undecided;

        /// <summary>
        /// The winning player slot for <see cref="MatchOutcome.VictoryElimination"/>;
        /// <see cref="NoWinnerSlot"/> for every other outcome (both draws and
        /// the undecided match).
        /// </summary>
        public byte WinnerSlot { get; private set; } = NoWinnerSlot;

        /// <summary>The tick the outcome was latched; 0 while undecided.</summary>
        public uint DecidedTick { get; private set; }

        /// <summary>True once the match has a final result. Read-only poll surface for the HUD.</summary>
        public bool IsDecided => Outcome != MatchOutcome.Undecided;

        public VictorySystem(EntityManager entityManager, ConstructionSystem construction)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _construction = construction ?? throw new ArgumentNullException(nameof(construction));
        }

        public void Initialize(SimulationKernel kernel)
        {
            if (kernel != null)
            {
                _logger = kernel.Logger;
            }
            _logger.LogInfo(
                $"[{Name}] Initialized MS-1 victory contract (D-056: elimination, mutual annihilation, " +
                $"time limit at tick {TimeLimitTick}; D-077: losing the last HQ also defeats).");
        }

        public void Shutdown()
        {
        }

        // ------------------------------------------------------------------
        // Read-only poll surface (HUD, AI, result screen)
        // ------------------------------------------------------------------

        /// <summary>True once the slot has owned at least one living entity (latched, serialized).</summary>
        public bool IsEngaged(byte slot) => slot < MaxSlots && _engaged[slot];

        /// <summary>
        /// True once the slot has owned at least one living COMPLETED HQ
        /// (latched, serialized). D-077: only a latched slot can be defeated
        /// by losing its last HQ; a slot that never had one is judged by the
        /// D-056 total-annihilation rule alone.
        /// </summary>
        public bool HadHq(byte slot) => slot < MaxSlots && _hadHq[slot];

        /// <summary>
        /// Consecutive ticks the D-056 reveal condition ("no buildings and at
        /// most <see cref="RevealMaxUnits"/> units") has held for the slot,
        /// saturating at <see cref="RevealHoldTicks"/>. Breaking the condition
        /// resets it to zero on the same tick.
        /// </summary>
        public int RevealHoldTicksOf(byte slot) => slot < MaxSlots ? _revealHold[slot] : 0;

        /// <summary>
        /// D-056 last-unit reveal: the slot's remaining units have satisfied
        /// the reveal condition for <see cref="RevealHoldTicks"/> uninterrupted
        /// ticks. Live state, not a latch — D-056 resets the counter when the
        /// condition ends, which ends the reveal with it (this overrides the
        /// "dauerhaft aufgedeckt" wording of the pre-D-056 v0.1 table).
        /// <para>
        /// This flag is currently INFORMATIONAL: no consumer enforces it yet.
        /// Making the revealed units actually visible and targetable requires
        /// the Fog-of-War mask/Combat targeting gate to consult it, which is
        /// outside this slice.
        /// </para>
        /// </summary>
        public bool IsRevealed(byte slot) => RevealHoldTicksOf(slot) >= RevealHoldTicks;

        /// <summary>
        /// Living entities the slot owns right now, split into the two D-056
        /// categories. Recomputed from the entity store on every call (see
        /// the class remarks on derived state), so the values are never stale
        /// — including immediately after a snapshot restore.
        /// </summary>
        public void CountLiving(byte slot, out int units, out int buildings)
        {
            Recount();
            if (slot >= MaxSlots)
            {
                units = 0;
                buildings = 0;
                return;
            }
            units = _scratchUnits[slot];
            buildings = _scratchBuildings[slot];
        }

        // ------------------------------------------------------------------
        // Tick
        // ------------------------------------------------------------------

        /// <summary>
        /// Evaluates the D-056/D-077 contract for this tick. Runs last in the
        /// canonical order, so the state it reads is post-combat. A decided
        /// match short-circuits: the outcome is final and no later tick may
        /// touch it, the engagement or had-HQ latches or the reveal holds.
        /// </summary>
        public void ExecuteTick(Tick tick)
        {
            if (Outcome != MatchOutcome.Undecided) return;

            Recount();

            int livingSides = 0;
            int eliminatedSides = 0;
            byte lastLivingSlot = NoWinnerSlot;

            for (int slot = 0; slot < MaxSlots; slot++)
            {
                int units = _scratchUnits[slot];
                int buildings = _scratchBuildings[slot];

                if (units + buildings > 0)
                {
                    _engaged[slot] = true;
                }
                if (_scratchHq[slot] > 0)
                {
                    _hadHq[slot] = true;
                }
                if (!_engaged[slot])
                {
                    _revealHold[slot] = 0;
                    continue;
                }

                if (units + buildings == 0)
                {
                    // D-056 defeat: no units AND no buildings (sites included).
                    eliminatedSides++;
                    _revealHold[slot] = 0;
                    continue;
                }

                if (_hadHq[slot] && _scratchHq[slot] == 0)
                {
                    // D-077 defeat: the slot owned a completed HQ and has
                    // lost the last one — remaining units and buildings do
                    // not save it.
                    eliminatedSides++;
                    _revealHold[slot] = 0;
                    continue;
                }

                livingSides++;
                lastLivingSlot = (byte)slot;

                // D-056 last-unit reveal hold; broken conditions reset it.
                if (buildings == 0 && units <= RevealMaxUnits)
                {
                    if (_revealHold[slot] < RevealHoldTicks)
                    {
                        _revealHold[slot]++;
                    }
                }
                else
                {
                    _revealHold[slot] = 0;
                }
            }

            // "Eliminierung beider Seiten" presupposes two sides; a single
            // engaged slot can never decide a match on its own.
            if (livingSides + eliminatedSides >= 2)
            {
                if (livingSides == 0)
                {
                    Decide(MatchOutcome.DrawMutualAnnihilation, NoWinnerSlot, tick);
                }
                else if (livingSides == 1 && eliminatedSides >= 1)
                {
                    Decide(MatchOutcome.VictoryElimination, lastLivingSlot, tick);
                }
            }

            // Elimination wins a tie on the limit tick itself ("ohne Eliminierung").
            if (Outcome == MatchOutcome.Undecided && tick.Value >= TimeLimitTick)
            {
                Decide(MatchOutcome.DrawTimeLimit, NoWinnerSlot, tick);
            }
        }

        private void Decide(MatchOutcome outcome, byte winnerSlot, Tick tick)
        {
            Outcome = outcome;
            WinnerSlot = winnerSlot;
            DecidedTick = tick.Value;
            _logger.LogInfo($"[{Name}] Match decided at tick {tick.Value}: {outcome} (winner slot {winnerSlot}).");
        }

        /// <summary>
        /// One deterministic pass over the entity store filling the per-slot
        /// scratch counters. Ascending entity index, ascending slot index;
        /// entities on a slot outside the format capacity are ignored rather
        /// than folded into slot 0.
        /// </summary>
        private void Recount()
        {
            Array.Clear(_scratchUnits, 0, MaxSlots);
            Array.Clear(_scratchBuildings, 0, MaxSlots);
            Array.Clear(_scratchHq, 0, MaxSlots);

            UnitState[] units = _entityManager.RawUnits;
            int capacity = _entityManager.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState u = ref units[i];
                if (!u.IsActive || u.PlayerId >= MaxSlots) continue;

                // Only COMPLETED buildings count as HQs: a site carries its
                // definition role since 16.3 (#44), so the bare role check
                // alone would promote a half-built HQ to a headquarters —
                // the site's own register is excluded here exactly like the
                // generic role excluded it before.
                if (u.Role == UnitRole.HQ && !_construction.IsActiveSite(u.Id))
                {
                    _scratchHq[u.PlayerId]++;
                }

                if (IsBuilding(in u))
                {
                    _scratchBuildings[u.PlayerId]++;
                }
                else
                {
                    _scratchUnits[u.PlayerId]++;
                }
            }
        }

        /// <summary>
        /// D-056 building classification: one of the nine MS-1 building roles,
        /// or an active construction site ("einschließlich Baustellen"). A
        /// site carries its definition role since 16.3 (#44), so the building
        /// role check catches it directly; the site-table consultation below
        /// remains as the defensive answer for the generic role, which no
        /// canonical path assigns to a site any more.
        /// <para>
        /// Known limitation, inherited from the command wire format: entity
        /// indices above 1023 have no packed raw id
        /// (<see cref="UnitCommandStateView.ToRawEntityId"/> returns 0), so a
        /// site living in such a slot would be counted as a unit. The MS-1
        /// entity store capacity is 1024 (mvp-v1.json capacity.entityStoreCap),
        /// so no reachable configuration hits this; it disappears when the
        /// packed-id layout of SimulationCore.md section 1 lands (Q-040).
        /// </para>
        /// <para>
        /// Walls are exempt from the destruction condition per
        /// VictoryConditions.md — no wall role exists in the MS-1 role set, so
        /// the exemption is vacuously satisfied here and needs revisiting when
        /// walls arrive.
        /// </para>
        /// </summary>
        private bool IsBuilding(in UnitState unit)
        {
            if (SimDefinitions.IsBuildingRole(unit.Role)) return true;
            if (unit.Role != UnitRole.Unit) return false;

            uint rawId = UnitCommandStateView.ToRawEntityId(unit.Id);
            if (rawId == 0u) return false;
            return _construction.TryGetSite(rawId, out _, out _, out _);
        }

        // ------------------------------------------------------------------
        // Snapshot state (IStatefulSimSystem)
        // ------------------------------------------------------------------

        /// <summary>Snapshot block id of the victory state (registry: <see cref="SnapshotBlockIds"/>; assignment provisional per Q-040).</summary>
        public ushort StateBlockId => SnapshotBlockIds.Victory;

        /// <summary>
        /// Block content v2 (D-077): version, slot capacity, the latched
        /// outcome with its winner slot and decision tick, then per slot in
        /// ascending order the engagement latch, the had-HQ latch and the
        /// reveal hold. Fixed 56 bytes — no derived counters, so a single
        /// changed latch or hold moves the block bytes and therefore the
        /// canonical state hash.
        /// </summary>
        public void WriteState(SnapshotBlockWriter writer)
        {
            writer.WriteUInt8(StateVersion);
            writer.WriteUInt8(MaxSlots);
            writer.WriteUInt8((byte)Outcome);
            writer.WriteUInt8(WinnerSlot);
            writer.WriteUInt32(DecidedTick);
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                writer.WriteUInt8(_engaged[slot] ? (byte)1 : (byte)0);
                writer.WriteUInt8(_hadHq[slot] ? (byte)1 : (byte)0);
                writer.WriteUInt32(unchecked((uint)_revealHold[slot]));
            }
        }

        /// <summary>Fully validates a victory block without mutating this system.</summary>
        public bool TryValidateState(ReadOnlySpan<byte> blockContent)
        {
            return TryParseState(blockContent, out _, out _, out _, out _, out _, out _);
        }

        /// <summary>
        /// Restores the latched outcome, the engagement and had-HQ latches
        /// and the reveal holds; malformed input returns false and leaves
        /// this system untouched (two-phase contract of
        /// <see cref="IStatefulSimSystem"/>). A decided match restores
        /// decided — the D-056 "final" property survives save/restore.
        /// </summary>
        public bool TryRestoreState(ReadOnlySpan<byte> blockContent)
        {
            if (!TryParseState(blockContent, out MatchOutcome outcome, out byte winnerSlot,
                    out uint decidedTick, out bool[] engaged, out bool[] hadHq, out int[] revealHold))
            {
                return false;
            }

            Outcome = outcome;
            WinnerSlot = winnerSlot;
            DecidedTick = decidedTick;
            Array.Copy(engaged, _engaged, MaxSlots);
            Array.Copy(hadHq, _hadHq, MaxSlots);
            Array.Copy(revealHold, _revealHold, MaxSlots);
            return true;
        }

        /// <summary>
        /// Parses and fully validates block content into commit-ready locals:
        /// exact length, known outcome code, the winner-slot/decision-tick
        /// invariants of each outcome, legal latch flags and reveal holds
        /// inside the saturation bound. Never mutates this system.
        /// </summary>
        private bool TryParseState(
            ReadOnlySpan<byte> blockContent,
            out MatchOutcome outcome, out byte winnerSlot, out uint decidedTick,
            out bool[] engaged, out bool[] hadHq, out int[] revealHold)
        {
            outcome = MatchOutcome.Undecided;
            winnerSlot = NoWinnerSlot;
            decidedTick = 0;
            engaged = null;
            hadHq = null;
            revealHold = null;

            var reader = new SnapshotBlockReader(blockContent);
            // Clean break (see StateVersion): v1 blocks fail right here.
            if (!reader.TryReadUInt8(out byte version) || version != StateVersion) return false;
            if (!reader.TryReadUInt8(out byte slotCount) || slotCount != MaxSlots) return false;
            if (!reader.TryReadUInt8(out byte outcomeRaw)) return false;
            if (outcomeRaw > (byte)MatchOutcome.DrawTimeLimit) return false; // unknown result code
            if (!reader.TryReadUInt8(out byte winnerRaw)) return false;
            if (!reader.TryReadUInt32(out uint tick)) return false;

            var parsedOutcome = (MatchOutcome)outcomeRaw;

            // Only an elimination victory names a winner; every other outcome
            // must carry the sentinel, and an undecided match has no tick.
            if (parsedOutcome == MatchOutcome.VictoryElimination)
            {
                if (winnerRaw >= MaxSlots) return false;
            }
            else if (winnerRaw != NoWinnerSlot)
            {
                return false;
            }
            if (parsedOutcome == MatchOutcome.Undecided && tick != 0) return false;

            var parsedEngaged = new bool[MaxSlots];
            var parsedHadHq = new bool[MaxSlots];
            var parsedRevealHold = new int[MaxSlots];
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                if (!reader.TryReadUInt8(out byte engagedRaw) || engagedRaw > 1) return false;
                if (!reader.TryReadUInt8(out byte hadHqRaw) || hadHqRaw > 1) return false;
                if (!reader.TryReadUInt32(out uint hold)) return false;
                // The counter saturates, so anything above the bound is a
                // tampered or foreign block.
                if (hold > RevealHoldTicks) return false;
                // A slot that was never on the map cannot have held the
                // reveal condition — and cannot ever have owned an HQ
                // (owning one would have latched the engagement, D-077).
                if (engagedRaw == 0 && (hold != 0 || hadHqRaw != 0)) return false;
                parsedEngaged[slot] = engagedRaw == 1;
                parsedHadHq[slot] = hadHqRaw == 1;
                parsedRevealHold[slot] = (int)hold;
            }
            if (reader.Remaining != 0) return false;

            // The winner must be a side that was actually on the map.
            if (parsedOutcome == MatchOutcome.VictoryElimination && !parsedEngaged[winnerRaw]) return false;

            outcome = parsedOutcome;
            winnerSlot = winnerRaw;
            decidedTick = tick;
            engaged = parsedEngaged;
            hadHq = parsedHadHq;
            revealHold = parsedRevealHold;
            return true;
        }
    }
}
