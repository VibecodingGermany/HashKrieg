// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System;
using System.Text;
using UnityEngine;
using Nova.AI.Data;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Combat;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Replays;
using Nova.Simulation.State;
using Nova.Simulation.Victory;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Read-only OnGUI overlay over a running <see cref="MatchRunner"/>: an
    /// always-on one-line status bar (credits, power, decided outcome) plus
    /// a full diagnostic panel (tick, census, selection combat profile,
    /// Aetherium reserves, control legend) that is hidden by default and
    /// toggled with F3. No Canvas, no uGUI, no prefabs, no materials.
    /// <para>
    /// STRICTLY READ-ONLY: this component never mutates simulation state and
    /// never submits a command. Every value is copied out of the sim per frame
    /// at the presentation boundary.
    /// </para>
    /// <para>
    /// Combat readability (this sprint): the flat 15-damage constant is gone,
    /// so "which unit hits what, how hard" became a real decision — and an
    /// invisible one. The HUD makes it inspectable by reading the SAME
    /// <see cref="WeaponProfiles"/> table and <see cref="DamageMatrix"/>
    /// resolver that <c>CombatSystem</c> fires through, so the readout cannot
    /// drift from the damage actually applied. The lead selected unit shows its
    /// armor class, damage type, damage, range and cooldown, plus what its
    /// weapon lands against each armor class carried by the MS-1 roster.
    /// </para>
    /// <para>
    /// Match outcome (this sprint): a match previously could not end at all.
    /// The outcome line polls <see cref="VictorySystem"/> for all four
    /// <see cref="MatchOutcome"/> codes — both draws included, which is why it
    /// never reports a drawn match as somebody's defeat — and the force census
    /// reads the very counts the D-056 contract is judged on.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugHud : MonoBehaviour
    {
        /// <summary>
        /// Player slots shown in the force census. MS-1 seeds exactly two
        /// (<c>MatchRunner</c> activeSlots {0, 1}).
        /// </summary>
        private const int CensusSlots = 2;

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [Tooltip("Full diagnostic panel. Hidden by default — the compact status bar above it is always on. F3 toggles this panel.")]
        [SerializeField] private bool _visible = false;
        [Tooltip("Whole GUI is scaled by this factor so it stays legible on a Retina display.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private int _fontSize = 13;

        private readonly StringBuilder _builder = new StringBuilder(256);

        /// <summary>
        /// The simulation's identity, computed ONCE. Both halves are constant
        /// for the lifetime of the process — the definition table is static
        /// data and the schema versions are consts — and
        /// <see cref="SimDefinitions.ComputeDefinitionsHash64"/> walks both
        /// tables, which is not a per-frame cost worth paying for a label.
        /// </summary>
        private static readonly string SimulationId =
            $"sim {SimDefinitions.ComputeDefinitionsHash64():X16}  schema " +
            $"s{MatchFingerprint.StateSchemaVersionV1}/" +
            $"c{MatchFingerprint.CommandSchemaVersionV1}/" +
            $"p{MatchFingerprint.PayloadSchemaVersionV1}/" +
            $"n{MatchFingerprint.SnapshotSchemaVersionV1}/" +
            $"x{MatchFingerprint.SidecarSchemaVersionV1}";

        /// <summary>
        /// Per-role combat profile text, sized to the weapon table itself so
        /// the two cannot drift. The profiles are immutable static content, so
        /// each role is formatted at most once per component instance.
        /// </summary>
        private readonly string[] _profileCache = new string[WeaponProfiles.FactionCount * WeaponProfiles.RoleCount];

        private GUIStyle _labelStyle;
        private GUIStyle _outcomeStyle;
        private GUIStyle _statusStyle;

        // F3 panel scroll position: the panel's height is bounded by its
        // HudLayout zone, so surplus content scrolls instead of running out.
        private Vector2 _scrollPos;

        // Force census, recomputed at most once per frame (OnGUI runs twice:
        // Layout + Repaint) so the O(capacity) sweep is not paid twice.
        private string _censusText;
        private int _censusFrame = -1;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void Update()
        {
            // F3 toggles the full diagnostic panel. The compact status bar
            // (credits, power, outcome) is drawn regardless — it is the
            // minimal match readout, not a debug view.
            if (Input.GetKeyDown(KeyCode.F3))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, richText = false, wordWrap = true };
            }
            if (_statusStyle == null)
            {
                // The always-on status bar is cockpit chrome, not debug text:
                // the shared HudChrome frame. The F3 panel below paints the
                // opaque twin (HudChrome.OpaquePanelStyle) — translucent
                // chrome under dense debug text was what made it unreadable.
                _statusStyle = new GUIStyle(_labelStyle) { wordWrap = false };
                HudChrome.ApplyPanelChrome(_statusStyle);
            }

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawStatusBar(scale);

            if (!_visible)
            {
                GUI.matrix = previousMatrix;
                return;
            }

            // The F3 panel lives in the HudLayout free-field zone: below the
            // status strip, bottom-bounded by the minimap's top edge, never
            // entering the command card's column. Degenerate zone (tiny
            // window) = draw nothing instead of overlapping.
            Rect strip = HudLayout.StatusStrip(scale, _fontSize + 6f, 8f);
            Rect minimap = HudLayout.CanonicalMinimapZone(scale);
            Rect zone = HudLayout.DebugPanelZone(scale, 640f, 8f, strip.yMax + 4f, minimap.yMin);
            if (zone.height <= 0f || zone.width <= 0f)
            {
                GUI.matrix = previousMatrix;
                return;
            }

            GUILayout.BeginArea(zone);
            GUILayout.BeginVertical(HudChrome.OpaquePanelStyle);
            _scrollPos = GUILayout.BeginScrollView(
                _scrollPos,
                GUILayout.Height(Mathf.Max(0f, zone.height - HudChrome.OpaquePanelStyle.padding.vertical)));

            if (_runner == null)
            {
                GUILayout.Label("Nova graybox HUD — no MatchRunner in the scene.", _labelStyle);
            }
            else
            {
                DrawMatchLine();
                DrawOutcomeLine();
                DrawEconomyLine();
                DrawCensusLine();
                DrawSelectionLine();
                DrawFieldLine();
                if (_input != null) GUILayout.Label($"Last command: {_input.LastCommandStatus}", _labelStyle);
                string legend = _input != null ? _input.ControlLegend : null;
                GUILayout.Label(string.IsNullOrEmpty(legend) ? "Controls: no RtsDeviceInput in the scene." : legend, _labelStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        /// <summary>
        /// The always-on minimal match readout: one compact line with the
        /// local slot's credits and power state, the decided outcome (the one
        /// thing that must never be hidden) and the F3 hint. This is the
        /// player-facing substitute until the real HUD (Nova.UI) lands; the
        /// full diagnostic panel below it stays opt-in via F3.
        /// </summary>
        private void DrawStatusBar(float scale)
        {
            _builder.Clear();

            if (_runner == null)
            {
                _builder.Append("Nova — no MatchRunner in the scene.");
            }
            else
            {
                byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;

                if (_runner.Economy != null)
                {
                    PlayerEconomyState economy = _runner.Economy.GetPlayerEconomy(slot);
                    _builder.Append(economy.AetheriumCredits).Append(" AE")
                        .Append("   |   Power ").Append(economy.PowerProvided).Append('/').Append(economy.PowerRequired);
                    if (economy.IsLowPower) _builder.Append(" (LOW POWER)");
                }

                VictorySystem victory = _runner.Victory;
                if (victory != null && victory.IsDecided)
                {
                    _builder.Append("   |   ");
                    switch (victory.Outcome)
                    {
                        case MatchOutcome.VictoryElimination:
                            _builder.Append(victory.WinnerSlot == slot ? "VICTORY" : "DEFEAT");
                            break;
                        case MatchOutcome.DrawMutualAnnihilation:
                            _builder.Append("DRAW — mutual annihilation");
                            break;
                        case MatchOutcome.DrawTimeLimit:
                            _builder.Append("DRAW — time limit");
                            break;
                        default:
                            _builder.Append(victory.Outcome.ToString());
                            break;
                    }
                }

                _builder.Append("   |   F3: debug panel");
            }

            GUI.Label(HudLayout.StatusStrip(scale, _fontSize + 6f, 8f), _builder.ToString(), _statusStyle);
        }

        private void DrawMatchLine()
        {
            uint tick = _runner.Kernel != null ? _runner.Kernel.CurrentTick.Value : 0u;
            string state = _runner.IsRunning ? "running" : _runner.Kernel == null ? "not initialized" : "stopped";
            GUILayout.Label($"Nova graybox HUD — tick {tick} ({state}, 10 Hz lockstep)", _labelStyle);

            // Which AI is this? The identifier changes whenever the skirmish AI
            // behaves differently — profile numbers automatically, behaviour
            // code through a hand-bumped revision that a test pins against the
            // canonical match's end state. It exists so a screenshot, a lab
            // report and an entry in the behaviour journal can be tied to each
            // other without asking anybody what was built that day.
            GUILayout.Label($"AI behaviour {AiBehaviorId.Value}", _labelStyle);

            // WHICH SIMULATION is this? The definition table's hash plus the
            // five schema versions — the same values the match fingerprint
            // seals, and the reason a simulation-changing merge invalidates
            // every distributed test build: unequal builds are separated by
            // exactly these numbers, not by a version string somebody
            // remembered to bump. Two testers comparing screenshots can tell
            // in one glance whether they are playing the same game.
            GUILayout.Label(SimulationId, _labelStyle);
        }

        /// <summary>
        /// The D-056 match result, polled from <see cref="VictorySystem"/>'s
        /// read-only surface. All four <see cref="MatchOutcome"/> codes are
        /// spelled out: the two draws carry no winner
        /// (<see cref="VictorySystem.NoWinnerSlot"/>) and must never be
        /// reported as somebody's defeat. While the match runs, the remaining
        /// distance to <see cref="VictorySystem.TimeLimitTick"/> is shown —
        /// a time-limit draw is otherwise invisible until it fires.
        /// </summary>
        private void DrawOutcomeLine()
        {
            VictorySystem victory = _runner.Victory;
            if (victory == null)
            {
                GUILayout.Label("Outcome: unavailable (no victory system on the runner)", _labelStyle);
                return;
            }

            byte localSlot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;

            if (!victory.IsDecided)
            {
                uint tick = _runner.Kernel != null ? _runner.Kernel.CurrentTick.Value : 0u;
                uint remaining = tick >= VictorySystem.TimeLimitTick ? 0u : VictorySystem.TimeLimitTick - tick;
                float seconds = remaining * MatchRunner.TickDeltaTime;
                GUILayout.Label($"Outcome: undecided — time limit in {remaining} ticks ({seconds:0} s)", _labelStyle);
                return;
            }

            string headline;
            switch (victory.Outcome)
            {
                case MatchOutcome.VictoryElimination:
                    headline = victory.WinnerSlot == localSlot
                        ? $"VICTORY — slot {victory.WinnerSlot} eliminated the opposition"
                        : $"DEFEAT — slot {victory.WinnerSlot} eliminated the opposition";
                    break;
                case MatchOutcome.DrawMutualAnnihilation:
                    headline = "DRAW — mutual annihilation";
                    break;
                case MatchOutcome.DrawTimeLimit:
                    headline = "DRAW — time limit reached";
                    break;
                default:
                    headline = victory.Outcome.ToString();
                    break;
            }

            // A finished match is the one thing the HUD must not whisper.
            GUILayout.Label($"{headline}   (decided tick {victory.DecidedTick}, local slot {localSlot})", OutcomeStyle());
        }

        /// <summary>Larger, bold variant of the label style for a decided match.</summary>
        private GUIStyle OutcomeStyle()
        {
            if (_outcomeStyle == null)
            {
                _outcomeStyle = new GUIStyle(_labelStyle)
                {
                    fontSize = _fontSize + 6,
                    fontStyle = FontStyle.Bold
                };
            }
            return _outcomeStyle;
        }

        private void DrawEconomyLine()
        {
            if (_runner.Economy == null)
            {
                GUILayout.Label("Economy: not initialized.", _labelStyle);
                return;
            }

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            // Copies the ref return into a local: the HUD reads, never writes.
            PlayerEconomyState economy = _runner.Economy.GetPlayerEconomy(slot);
            string power = economy.IsLowPower ? "LOW POWER" : "ok";
            GUILayout.Label(
                $"Slot {slot} ({economy.Faction}): {economy.AetheriumCredits} AE | power {economy.PowerProvided}/{economy.PowerRequired} ({power})",
                _labelStyle);

            // Both slot factions at a glance: with the faction axis live the
            // same role plays differently per side, so the assignment must be
            // visible without selecting anything first.
            _builder.Clear();
            _builder.Append("Factions:");
            for (byte s = 0; s < CensusSlots; s++)
            {
                _builder.Append("  slot ").Append(s).Append(' ').Append(_runner.Economy.GetSlotFaction(s).ToString());
            }
            GUILayout.Label(_builder.ToString(), _labelStyle);
        }

        /// <summary>
        /// Live unit/building census per slot. An elimination outcome is only
        /// judgeable if you can watch the two force pools drain, so this is the
        /// counterpart of <see cref="DrawOutcomeLine"/> — and unlike that line
        /// it needs no API beyond the entity store.
        /// <para>
        /// DELIBERATELY OMNISCIENT: this reads the raw entity store and so
        /// ignores Fog of War, unlike <see cref="UnitViewManager"/>, which is
        /// fed exclusively from the committed team view. That is a debug
        /// affordance for judging the victory condition, NOT player-legal
        /// information — it is labelled as such on screen and it must not be
        /// carried into the real HUD that replaces this graybox overlay.
        /// </para>
        /// </summary>
        private void DrawCensusLine()
        {
            VictorySystem victory = _runner.Victory;
            if (victory == null) return;

            // CountLiving re-sweeps the entity store on every call, so the
            // whole line is built once per frame — OnGUI runs twice (Layout +
            // Repaint) and would otherwise pay for it double.
            if (_censusFrame != Time.frameCount)
            {
                _censusFrame = Time.frameCount;
                _censusText = BuildCensusText(victory);
            }

            GUILayout.Label(_censusText, _labelStyle);
        }

        /// <summary>
        /// Reads the very counts the D-056 contract is evaluated against, so
        /// what the HUD shows and what decides the match cannot drift apart.
        /// "not engaged" is spelled out because an empty slot that never owned
        /// an entity does NOT end the match, which is otherwise a baffling
        /// thing to watch.
        /// </summary>
        private string BuildCensusText(VictorySystem victory)
        {
            _builder.Clear();
            _builder.Append("Forces [debug, ignores fog]:");

            for (byte slot = 0; slot < CensusSlots; slot++)
            {
                victory.CountLiving(slot, out int units, out int buildings);
                if (slot > 0) _builder.Append("   |");
                _builder.Append("   slot ").Append(slot).Append(' ')
                    .Append(units).Append('u').Append('/').Append(buildings).Append('b');
                if (!victory.IsEngaged(slot)) _builder.Append(" (not engaged)");
            }

            return _builder.ToString();
        }

        private void DrawSelectionLine()
        {
            if (_input == null)
            {
                GUILayout.Label("Selection: unavailable (no RtsDeviceInput).", _labelStyle);
                return;
            }

            string wiring = _input.IsWired ? string.Empty : "  [input not wired — orders are dropped]";
            GUILayout.Label($"Selection: {_input.SelectionCount} unit(s){wiring}", _labelStyle);

            ReadOnlySpan<EntityId> selected = _input.Selection.SelectedEntities;
            if (selected.Length == 0) return;

            EntityManager entities = _runner.Entities;
            if (entities == null || !entities.TryGetUnit(selected[0], out UnitState lead))
            {
                GUILayout.Label("  lead unit: stale handle (died or left the store)", _labelStyle);
                return;
            }

            FactionId leadFaction = _runner.Economy != null
                ? _runner.Economy.GetSlotFaction(lead.PlayerId)
                : FactionId.Alliance;

            _builder.Clear();
            _builder.Append("  lead: ").Append(lead.Role.ToString())
                .Append(" (").Append(leadFaction.ToString()).Append(')');
            if (selected.Length > 1) _builder.Append(" (+").Append(selected.Length - 1).Append(" more selected)");
            _builder.Append("   hp ").Append(lead.CurrentHealth).Append('/').Append(lead.MaxHealth);
            if (lead.WeaponCooldownTicks > 0) _builder.Append("   reloading ").Append(lead.WeaponCooldownTicks).Append('t');
            GUILayout.Label(_builder.ToString(), _labelStyle);

            GUILayout.Label(ProfileTextFor(leadFaction, lead.Role), _labelStyle);
        }

        /// <summary>
        /// Aetherium reserves. EconomySystem exposes only FieldCount and a
        /// by-id lookup, so the ids are probed ascending and the scan stops
        /// once FieldCount fields are found (canonical setup uses ids 1 and 2).
        /// </summary>
        private void DrawFieldLine()
        {
            if (_runner.Economy == null) return;

            _builder.Clear();
            _builder.Append("Aetherium: ");
            int found = 0;
            for (ushort fieldId = 1; fieldId <= EconomySystem.MaxFields && found < _runner.Economy.FieldCount; fieldId++)
            {
                if (!_runner.Economy.TryGetField(fieldId, out AetheriumField field)) continue;
                if (found > 0) _builder.Append("  |  ");
                _builder.Append('#').Append(field.FieldId)
                    .Append(" (").Append(field.GridPos.X).Append(',').Append(field.GridPos.Y).Append(") ")
                    .Append(field.RemainingAE).Append(" AE");
                if (field.IsExhausted) _builder.Append(" EXHAUSTED");
                found++;
            }
            if (found == 0) _builder.Append("no fields registered");
            GUILayout.Label(_builder.ToString(), _labelStyle);
        }

        // ----------------------------------------------------------------
        // Combat profile
        // ----------------------------------------------------------------

        /// <summary>
        /// Combat profile lines of one faction's role. The profiles are
        /// immutable static content, so each (faction, role) pair is formatted
        /// at most once for the lifetime of the component instead of once per
        /// OnGUI pass. A role outside the weapon table is simply uncached —
        /// <see cref="BuildProfileText"/> still answers it, without indexing
        /// past the array.
        /// </summary>
        private string ProfileTextFor(FactionId faction, UnitRole role)
        {
            int index = (int)faction * WeaponProfiles.RoleCount + (int)role;
            bool cacheable = (int)faction >= 0 && (int)faction < WeaponProfiles.FactionCount
                && index >= 0 && index < _profileCache.Length;
            if (cacheable && _profileCache[index] != null) return _profileCache[index];

            string text = BuildProfileText(faction, role);
            if (cacheable) _profileCache[index] = text;
            return text;
        }

        /// <summary>
        /// Armor classes the census line resolves damage against. Only the four
        /// MS-1 carriers are listed: <see cref="ArmorClass.Heavy"/> (Heavy Tank)
        /// and <see cref="ArmorClass.Air"/> have no entity in the shipped
        /// roster, so printing them would be four columns of noise.
        /// </summary>
        private static readonly ArmorClass[] MatchupArmorClasses =
        {
            ArmorClass.Infantry, ArmorClass.Light, ArmorClass.Medium, ArmorClass.Building
        };

        /// <summary>
        /// Two lines: the role's own combat attributes, and what its weapon
        /// actually lands against each armor class in the MS-1 roster.
        /// <para>
        /// The numbers come from <see cref="WeaponProfiles"/> and
        /// <see cref="DamageMatrix"/> — the very table and resolver
        /// <c>CombatSystem</c> uses — so the readout cannot disagree with the
        /// damage the simulation applies. The matchup line is the whole point
        /// of the exercise: a flat constant made every unit identical, and the
        /// only way to judge the counter triangle is to see that a BattleTank's
        /// 60 kinetic lands as 60 on infantry and 18 on a building.
        /// </para>
        /// </summary>
        private string BuildProfileText(FactionId faction, UnitRole role)
        {
            // WeaponProfiles.Get throws on an unknown faction or role by
            // design; a HUD must never be the thing that throws out of OnGUI.
            int factionIndex = (int)faction;
            int index = (int)role;
            if (factionIndex < 0 || factionIndex >= WeaponProfiles.FactionCount
                || index < 0 || index >= WeaponProfiles.RoleCount)
            {
                return "  profile: role outside the weapon table";
            }

            WeaponProfile profile = WeaponProfiles.Get(faction, role);

            // The two-space indent is baked into the cached string so the
            // per-frame path concatenates nothing.
            if (!profile.IsArmed)
            {
                return $"  armor {profile.ArmorClass}   |   unarmed";
            }

            // Presentation boundary: SimFixed -> float happens here, never upstream.
            float range = profile.AttackRange.ToFloat();
            // Cooldown is authoritative in ticks; the seconds figure is the
            // presentation-side convenience at the canonical 10 Hz rate.
            float cooldownSeconds = profile.AttackCooldownTicks * MatchRunner.TickDeltaTime;

            var text = new StringBuilder(192);
            text.Append("  armor ").Append(profile.ArmorClass.ToString())
                .Append("   |   ").Append(profile.DamageType.ToString())
                .Append(' ').Append(profile.AttackDamage).Append(" dmg")
                .Append("   |   range ").Append(range.ToString("0.#"))
                .Append("   |   cooldown ").Append(profile.AttackCooldownTicks)
                .Append("t (").Append(cooldownSeconds.ToString("0.0")).Append("s)")
                .Append("\n  lands as:");

            for (int i = 0; i < MatchupArmorClasses.Length; i++)
            {
                ArmorClass armor = MatchupArmorClasses[i];
                int resolved = DamageMatrix.Resolve(profile.AttackDamage, profile.DamageType, armor);
                if (i > 0) text.Append("  |");
                text.Append("  ").Append(armor.ToString()).Append(' ').Append(resolved);
            }

            return text.ToString();
        }
    }
}
