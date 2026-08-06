using System.Text;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.State;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The build bar: a permanent row of the nine MS-1 building roles of the
    /// local faction along the bottom screen edge — the discoverable entry
    /// point into construction that the graybox hotkeys never were. Each
    /// entry shows the German display name (the runbook and GDDs are German),
    /// its hotkey, cost in AE and build time in seconds at the canonical
    /// 10 Hz. Entries are clickable when buildable and greyed out WITH the
    /// reason otherwise ("benötigt X" for an unmet prerequisite, "nicht genug
    /// Aetherium") — a greyed button without a reason is a dead end, and
    /// "what do I need for this?" is the central onboarding question.
    /// <para>
    /// DATA SOURCE: <see cref="SimDefinitions"/> — the authoritative static
    /// table the simulation itself validates placement against. The natural
    /// alternative BuildingRegistrySO has no asset instances in the project
    /// and is not wired at runtime, so SimDefinitions is the honest source:
    /// the bar cannot drift from the executor. Availability mirrors the
    /// executor's own rule precisely — the prerequisite check is the sim's
    /// <see cref="ConstructionSystem.HasFinishedBuilding"/> and the credit
    /// check is the balance the executor charges at placement.
    /// </para>
    /// <para>
    /// Clicking an available entry calls
    /// <see cref="RtsDeviceInput.EnterPlacementMode"/> — placement itself
    /// (ghost, LMB/RMB/ESC) lives in the input component. The onboarding
    /// hint above the bar names the critical path of the D-077 opening
    /// (HQ + Builder + 3000 AE at start; the Refinery produces the
    /// Harvester) and dismisses itself once the local player owns a
    /// completed refinery or a harvester, or on a key press after a few
    /// seconds, or after a long timeout.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildMenuHud : MonoBehaviour
    {
        /// <summary>The nine MS-1 building roles in role-value order (HQ .. DefensePlatform).</summary>
        private static readonly UnitRole[] BuildableRoles =
        {
            UnitRole.HQ, UnitRole.Refinery, UnitRole.Power, UnitRole.Storage,
            UnitRole.Barracks, UnitRole.VehicleFactory, UnitRole.ResearchLab,
            UnitRole.Radar, UnitRole.DefensePlatform
        };

        /// <summary>
        /// The opening-loop hint. German, like the runbook: build a Refinery
        /// (Y), produce a Harvester (Q) at it, then harvest (H).
        /// </summary>
        private const string HintText =
            "1. Raffinerie bauen (Y)    2. Harvester produzieren (Q)    3. Aetherium ernten (H)";

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [Tooltip("Whole bar is scaled by this factor, matching the DebugHud convention for Retina displays.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _buttonWidth = 108f;
        [SerializeField] private float _buttonHeight = 52f;
        [SerializeField] private float _barMargin = 8f;

        [Header("Onboarding hint")]
        [Tooltip("Seconds the hint stays up before a key press may dismiss it.")]
        [SerializeField] private float _hintMinSeconds = 6f;
        [Tooltip("Seconds after which the hint dismisses itself unconditionally.")]
        [SerializeField] private float _hintMaxSeconds = 90f;

        private readonly StringBuilder _builder = new StringBuilder(64);
        private GUIStyle _buttonStyle;
        private GUIStyle _hintStyle;
        private bool _hintDismissed;
        private float _hintShownAt = -1f;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        /// <summary>
        /// GUI-space height the bar occupies above the bottom screen edge
        /// (button height plus its bottom margin). The command card docks
        /// directly above this reserve, so the two permanent HUD elements
        /// can never overlap.
        /// </summary>
        public float OccupiedHeight => _buttonHeight + _barMargin;

        /// <summary>
        /// Screen-space hit test used by RtsDeviceInput: a pointer over the
        /// bar belongs to the HUD, so world selection drags and placement
        /// clicks are suppressed there. Takes the RAW mouse position
        /// (bottom-left origin) and converts it into the bar's scaled
        /// top-left GUI space — the layout math lives in
        /// <see cref="ComputeBarRect"/>, shared with OnGUI, so hit test and
        /// drawing cannot drift apart.
        /// </summary>
        public bool IsPointerOverBar(Vector2 mousePosition)
        {
            float scale = Mathf.Max(1f, _uiScale);
            var gui = new Vector2(mousePosition.x / scale, (Screen.height - mousePosition.y) / scale);
            Rect bar = ComputeBarRect();
            bar.yMin -= 6f; // small forgiveness band so a near-miss still counts as HUD
            return bar.Contains(gui);
        }

        private Rect ComputeBarRect()
        {
            float scale = Mathf.Max(1f, _uiScale);
            float width = BuildableRoles.Length * EffectiveButtonWidth() + (BuildableRoles.Length - 1) * 4f;
            float x = (Screen.width / scale - width) * 0.5f;
            float y = Screen.height / scale - _buttonHeight - _barMargin;
            return new Rect(x, y, width, _buttonHeight);
        }

        /// <summary>
        /// Button width clamped so the whole bar fits the screen even at
        /// small window sizes (nine entries plus spacing must never run off
        /// the right edge).
        /// </summary>
        private float EffectiveButtonWidth()
        {
            float scale = Mathf.Max(1f, _uiScale);
            float available = Screen.width / scale - 2f * _barMargin - (BuildableRoles.Length - 1) * 4f;
            return Mathf.Min(_buttonWidth, Mathf.Max(64f, available / BuildableRoles.Length));
        }

        private void Update()
        {
            if (_hintDismissed || _runner == null || !_runner.IsRunning) return;
            if (_hintShownAt < 0f) _hintShownAt = Time.time;

            float elapsed = Time.time - _hintShownAt;
            if (elapsed >= _hintMaxSeconds
                || (elapsed >= _hintMinSeconds && Input.anyKeyDown)
                || LocalPlayerBrokeIntoEconomyLoop())
            {
                _hintDismissed = true;
            }
        }

        /// <summary>
        /// The hint's job is done the moment the local player owns a
        /// COMPLETED refinery (the sim's own completion record) or a
        /// harvester, whichever comes first — the economy loop the hint
        /// teaches is running by then.
        /// </summary>
        private bool LocalPlayerBrokeIntoEconomyLoop()
        {
            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            if (_runner.Construction != null
                && _runner.Construction.HasFinishedBuilding(slot, UnitRole.Refinery))
            {
                return true;
            }

            EntityManager entities = _runner.Entities;
            if (entities == null) return false;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (unit.IsActive && unit.PlayerId == slot && unit.Role == UnitRole.Harvester)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnGUI()
        {
            if (_runner == null || _input == null) return;
            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            DrawHint();
            DrawBar();

            GUI.matrix = previousMatrix;
        }

        private void DrawHint()
        {
            if (_hintDismissed || !_runner.IsRunning) return;

            Rect bar = ComputeBarRect();
            var rect = new Rect(bar.center.x - 320f, bar.yMin - 36f, 640f, 26f);
            GUI.Box(rect, GUIContent.none, HudChrome.PanelStyle);
            GUI.Label(rect, HintText, _hintStyle);
        }

        private void DrawBar()
        {
            EconomySystem economy = _runner.Economy;
            ConstructionSystem construction = _runner.Construction;
            if (economy == null || construction == null) return; // match not initialized yet

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            long credits = economy.GetPlayerEconomy(slot).AetheriumCredits;
            FactionId faction = economy.GetSlotFaction(slot);

            // Shared cockpit chrome behind the buttons, slightly larger than
            // the bar so the border ring is not clipped by the outermost
            // buttons (HudChrome: one generated panel texture for all HUD
            // panels).
            Rect bar = ComputeBarRect();
            GUI.Box(
                new Rect(bar.x - 4f, bar.y - 4f, bar.width + 8f, bar.height + 8f),
                GUIContent.none, HudChrome.PanelStyle);

            GUILayout.BeginArea(bar);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < BuildableRoles.Length; i++)
            {
                UnitRole role = BuildableRoles[i];
                if (!SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition def)) continue;

                bool prerequisiteMet = !def.HasPrerequisite
                    || construction.HasFinishedBuilding(slot, def.PrerequisiteRole);
                bool affordable = credits >= def.CostAE;
                bool available = prerequisiteMet && affordable;

                bool wasEnabled = GUI.enabled;
                GUI.enabled = available;
                if (GUILayout.Button(
                        ButtonLabel(role, in def, prerequisiteMet, affordable),
                        _buttonStyle, GUILayout.Width(EffectiveButtonWidth()), GUILayout.Height(_buttonHeight)))
                {
                    _input.EnterPlacementMode(def.DefinitionId);
                }
                GUI.enabled = wasEnabled;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        /// <summary>
        /// "Raffinerie (Y)\n700 AE · 20 s", plus a third line naming the
        /// blocker while the entry is unavailable. Build ticks are whole
        /// seconds at 10 Hz (every MS-1 value is a multiple of 10). The
        /// German building names are shared with the command card through
        /// <see cref="CommandCardPresenter.BuildingDisplayName"/>.
        /// </summary>
        private string ButtonLabel(UnitRole role, in SimBuildingDefinition def, bool prerequisiteMet, bool affordable)
        {
            _builder.Clear();
            _builder.Append(CommandCardPresenter.BuildingDisplayName(role));
            string hotkey = HotkeyHint(role);
            if (hotkey.Length > 0) _builder.Append(" (").Append(hotkey).Append(')');
            _builder.Append('\n')
                .Append(def.CostAE).Append(" AE · ").Append(def.BuildTicks / 10).Append(" s");
            if (!prerequisiteMet) _builder.Append('\n').Append("benötigt ").Append(CommandCardPresenter.BuildingDisplayName(def.PrerequisiteRole));
            else if (!affordable) _builder.Append('\n').Append("nicht genug Aetherium");
            return _builder.ToString();
        }

        /// <summary>
        /// The hotkey bound to a role in RtsDeviceInput, or empty (HQ is
        /// deliberately unbound — MS-1 builds it only at match start).
        /// </summary>
        private static string HotkeyHint(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Power: return "B";
                case UnitRole.Barracks: return "Shift+B";
                case UnitRole.Storage: return "C";
                case UnitRole.VehicleFactory: return "V";
                case UnitRole.ResearchLab: return "T";
                case UnitRole.Radar: return "G";
                case UnitRole.DefensePlatform: return "F";
                case UnitRole.Refinery: return "Y";
                default: return string.Empty;
            }
        }

        private void EnsureStyles()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, wordWrap = true };
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            }
        }
    }
}
