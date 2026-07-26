// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System;
using System.Text;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Combat;
using Nova.Simulation.Economy;
using Nova.Simulation.State;
using Nova.Simulation.Victory;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Read-only OnGUI overlay over a running <see cref="MatchRunner"/>: tick,
    /// match outcome, economy, force census, selection combat profile,
    /// Aetherium reserves and the control legend. No Canvas, no uGUI, no
    /// prefabs, no materials.
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
        [SerializeField] private bool _visible = true;
        [Tooltip("Whole GUI is scaled by this factor so it stays legible on a Retina display.")]
        [SerializeField] private float _uiScale = 2f;
        [SerializeField] private int _fontSize = 13;

        private readonly StringBuilder _builder = new StringBuilder(256);

        /// <summary>
        /// Per-role combat profile text, sized to the weapon table itself so
        /// the two cannot drift. The profiles are immutable static content, so
        /// each role is formatted at most once per component instance.
        /// </summary>
        private readonly string[] _profileCache = new string[WeaponProfiles.RoleCount];

        private GUIStyle _labelStyle;
        private GUIStyle _outcomeStyle;

        // Force census, recomputed at most once per frame (OnGUI runs twice:
        // Layout + Repaint) so the O(capacity) sweep is not paid twice.
        private string _censusText;
        private int _censusFrame = -1;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        private void OnGUI()
        {
            if (!_visible) return;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = _fontSize, richText = false, wordWrap = true };
            }

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            float width = Mathf.Min(640f, Screen.width / scale - 16f);
            GUILayout.BeginArea(new Rect(8f, 8f, width, Screen.height / scale - 16f));
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandHeight(false));

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

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        private void DrawMatchLine()
        {
            uint tick = _runner.Kernel != null ? _runner.Kernel.CurrentTick.Value : 0u;
            string state = _runner.IsRunning ? "running" : _runner.Kernel == null ? "not initialized" : "stopped";
            GUILayout.Label($"Nova graybox HUD — tick {tick} ({state}, 10 Hz lockstep)", _labelStyle);
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
                $"Slot {slot}: {economy.AetheriumCredits} AE | power {economy.PowerProvided}/{economy.PowerRequired} ({power})",
                _labelStyle);
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

            _builder.Clear();
            _builder.Append("  lead: ").Append(lead.Role.ToString());
            if (selected.Length > 1) _builder.Append(" (+").Append(selected.Length - 1).Append(" more selected)");
            _builder.Append("   hp ").Append(lead.CurrentHealth).Append('/').Append(lead.MaxHealth);
            if (lead.WeaponCooldownTicks > 0) _builder.Append("   reloading ").Append(lead.WeaponCooldownTicks).Append('t');
            GUILayout.Label(_builder.ToString(), _labelStyle);

            GUILayout.Label(ProfileTextFor(lead.Role), _labelStyle);
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
        /// Combat profile lines of a role. The profiles are immutable static
        /// content, so each role is formatted at most once for the lifetime of
        /// the component instead of once per OnGUI pass. A role outside the
        /// weapon table is simply uncached — <see cref="BuildProfileText"/>
        /// still answers it, without indexing past the array.
        /// </summary>
        private string ProfileTextFor(UnitRole role)
        {
            int index = (int)role;
            bool cacheable = index >= 0 && index < _profileCache.Length;
            if (cacheable && _profileCache[index] != null) return _profileCache[index];

            string text = BuildProfileText(role);
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
        private static string BuildProfileText(UnitRole role)
        {
            // WeaponProfiles.Get throws on an unknown role by design; a HUD
            // must never be the thing that throws out of OnGUI.
            int index = (int)role;
            if (index < 0 || index >= WeaponProfiles.RoleCount)
            {
                return "  profile: role outside the weapon table";
            }

            WeaponProfile profile = WeaponProfiles.Get(role);

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
