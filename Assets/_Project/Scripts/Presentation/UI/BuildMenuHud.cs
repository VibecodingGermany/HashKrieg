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
    /// 10 Hz. Entries are clickable when buildable and greyed out otherwise.
    /// <para>
    /// TWO LINES, NEVER MORE (D-085): a button carries exactly its name (with
    /// hotkey) and its "cost · time" line — wordWrap is OFF and every line is
    /// hard-truncated with "…" to the button width, so no label can ever run
    /// out of its button. The blocker reason ("benötigt Raffinerie", "nicht
    /// genug Aetherium") moved OUT of the button into the status line above
    /// the bar, where it appears while the entry is hovered.
    /// </para>
    /// <para>
    /// THE STATUS LINE above the bar serves three masters in priority order:
    /// the hovered entry's blocker reason, then the D-085 builder warning
    /// ("Kein Builder — Bau pausiert…") shown as long as any own construction
    /// site has no living Builder — the visible warning instead of the silent
    /// dead end —, then the onboarding hint below.
    /// </para>
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
    /// hint names the critical path of the D-077 opening
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

        /// <summary>Serialized defaults as constants — HudLayout reads these for the zones that interlock with the bar.</summary>
        public const float DefaultButtonHeight = 62f;
        public const float DefaultBarMargin = 8f;
        public const float StatusLineHeight = 26f;
        public const float StatusLineGap = 4f;
        public const float DefaultOccupiedHeight =
            StatusLineHeight + StatusLineGap + DefaultButtonHeight + DefaultBarMargin;

        /// <summary>Horizontal gap between two buttons (explicit rects, so the gap is exactly this, not a GUILayout margin guess).</summary>
        private const float ButtonSpacing = 4f;

        /// <summary>Width of the status line above the bar (centered on the zone), same measure the onboarding hint had.</summary>
        private const float StatusLineWidth = 640f;

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;

        [Header("Presentation")]
        [Tooltip("Whole bar is scaled by this factor, matching the DebugHud convention for Retina displays.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _buttonWidth = 108f;
        [Tooltip("Two lines at font size 11 plus air — the third line (blocker reason) moved into the status line above the bar.")]
        [SerializeField] private float _buttonHeight = DefaultButtonHeight;
        [SerializeField] private float _barMargin = DefaultBarMargin;

        [Header("Onboarding hint")]
        [Tooltip("Seconds the hint stays up before a key press may dismiss it.")]
        [SerializeField] private float _hintMinSeconds = 6f;
        [Tooltip("Seconds after which the hint dismisses itself unconditionally.")]
        [SerializeField] private float _hintMaxSeconds = 90f;

        private readonly StringBuilder _builder = new StringBuilder(64);
        private GUIStyle _buttonStyle;
        private GUIStyle _statusStyle;
        private bool _hintDismissed;
        private float _hintShownAt = -1f;

        // Per-frame state, computed once in Update/OnGUI and shared between
        // the status line and the buttons.
        private bool _siteLacksBuilder;
        private int _siteLacksBuilderFrame = -1;
        private string _hoveredBlockerReason;
        private string _transientNotice;
        private float _transientNoticeUntil;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
        }

        /// <summary>
        /// GUI-space height the bar occupies above the bottom screen edge:
        /// the status line, its gap and the button row, plus the bottom
        /// margin. The command card and the minimap dock above this reserve,
        /// so the permanent HUD elements can never overlap.
        /// </summary>
        public float OccupiedHeight => StatusLineHeight + StatusLineGap + _buttonHeight + _barMargin;

        /// <summary>
        /// Screen-space hit test used by RtsDeviceInput: a pointer over the
        /// bar belongs to the HUD, so world selection drags and placement
        /// clicks are suppressed there. Takes the RAW mouse position
        /// (bottom-left origin) and converts it into the bar's scaled
        /// top-left GUI space — the layout math lives in
        /// <see cref="ComputeBarZone"/>, shared with OnGUI, so hit test and
        /// drawing cannot drift apart.
        /// </summary>
        public bool IsPointerOverBar(Vector2 mousePosition)
        {
            float scale = Mathf.Max(1f, _uiScale);
            Vector2 gui = HudLayout.RawMouseToGui(mousePosition, scale);
            Rect bar = ComputeBarZone();
            bar.yMin -= 6f; // small forgiveness band so a near-miss still counts as HUD
            return bar.Contains(gui);
        }

        /// <summary>The bar's zone: status line plus button row, bottom-center (HudLayout owns the screen read).</summary>
        private Rect ComputeBarZone()
        {
            float scale = Mathf.Max(1f, _uiScale);
            float width = BuildableRoles.Length * EffectiveButtonWidth() + (BuildableRoles.Length - 1) * ButtonSpacing;
            return HudLayout.BuildBarZone(scale, width, StatusLineHeight + StatusLineGap + _buttonHeight, _barMargin);
        }

        /// <summary>
        /// Button width clamped so the whole bar fits the screen even at
        /// small window sizes (nine entries plus spacing must never run off
        /// the right edge).
        /// </summary>
        private float EffectiveButtonWidth()
        {
            float scale = Mathf.Max(1f, _uiScale);
            float available = HudLayout.GuiWidth(scale) - 2f * _barMargin - (BuildableRoles.Length - 1) * ButtonSpacing;
            return Mathf.Min(_buttonWidth, Mathf.Max(64f, available / BuildableRoles.Length));
        }

        private void Update()
        {
            _siteLacksBuilder = ComputeSiteLacksBuilder();

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
        /// The D-085 builder warning's data source: true while any own
        /// construction site has no living Builder assigned — the state the
        /// sim pauses on (ConstructionSystem re-scans every tick, so this
        /// clears the moment a fresh Builder leaves the HQ). Cached once per
        /// frame like the hint scan.
        /// </summary>
        private bool ComputeSiteLacksBuilder()
        {
            if (_siteLacksBuilderFrame == Time.frameCount) return _siteLacksBuilder;
            _siteLacksBuilderFrame = Time.frameCount;

            _siteLacksBuilder = false;
            EntityManager entities = _runner != null ? _runner.Entities : null;
            ConstructionSystem construction = _runner != null ? _runner.Construction : null;
            if (entities == null || construction == null || _runner.Session == null) return false;

            byte slot = _runner.Session.LocalSlot;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != slot || unit.Role != UnitRole.Unit) continue;
                uint raw = UnitCommandStateView.ToRawEntityId(unit.Id);
                if (raw != 0
                    && construction.TryGetSite(raw, out _, out _, out uint assignedBuilderRaw)
                    && assignedBuilderRaw == 0)
                {
                    _siteLacksBuilder = true;
                    return true;
                }
            }
            return false;
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

            DrawBar();

            GUI.matrix = previousMatrix;
        }

        private void DrawBar()
        {
            EconomySystem economy = _runner.Economy;
            ConstructionSystem construction = _runner.Construction;
            if (economy == null || construction == null) return; // match not initialized yet

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            long credits = economy.GetPlayerEconomy(slot).AetheriumCredits;
            FactionId faction = economy.GetSlotFaction(slot);

            Rect zone = ComputeBarZone();
            var buttonsRect = new Rect(
                zone.x, zone.y + StatusLineHeight + StatusLineGap, zone.width, _buttonHeight);
            float buttonWidth = EffectiveButtonWidth();

            // Hover scan BEFORE anything draws: the status line above the
            // buttons already needs the reason. Disabled controls report no
            // hover to IMGUI, so the bar tests its own explicit rects — that
            // is exactly why they are explicit.
            float scale = Mathf.Max(1f, _uiScale);
            Vector2 guiMouse = HudLayout.RawMouseToGui(Input.mousePosition, scale);
            _hoveredBlockerReason = null;
            for (int i = 0; i < BuildableRoles.Length; i++)
            {
                UnitRole role = BuildableRoles[i];
                if (!SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition def)) continue;
                if (IsAvailable(in def, slot, credits, construction)) continue;

                var rect = new Rect(
                    buttonsRect.x + i * (buttonWidth + ButtonSpacing), buttonsRect.y, buttonWidth, _buttonHeight);
                if (rect.Contains(guiMouse))
                {
                    _hoveredBlockerReason = BlockerReason(role, in def, slot, credits, construction);
                }
            }

            // Chrome and status line paint BEFORE the buttons — IMGUI paints
            // in call order and later calls sit on top.
            DrawChromeAndStatusLine(zone, buttonsRect);

            for (int i = 0; i < BuildableRoles.Length; i++)
            {
                UnitRole role = BuildableRoles[i];
                if (!SimDefinitions.TryGetBuilding(faction, role, out SimBuildingDefinition def)) continue;

                bool available = IsAvailable(in def, slot, credits, construction);
                var rect = new Rect(
                    buttonsRect.x + i * (buttonWidth + ButtonSpacing), buttonsRect.y, buttonWidth, _buttonHeight);

                bool wasEnabled = GUI.enabled;
                GUI.enabled = available;
                if (GUI.Button(rect, ButtonLabel(role, in def, buttonWidth), _buttonStyle))
                {
                    _input.EnterPlacementMode(def.DefinitionId);
                }
                GUI.enabled = wasEnabled;
            }
        }

        /// <summary>Entry availability, the executor's own rule: prerequisite finished (if any) and enough credits.</summary>
        private static bool IsAvailable(in SimBuildingDefinition def, byte slot, long credits, ConstructionSystem construction)
        {
            bool prerequisiteMet = !def.HasPrerequisite
                || construction.HasFinishedBuilding(slot, def.PrerequisiteRole);
            return prerequisiteMet && credits >= def.CostAE;
        }

        /// <summary>
        /// The chrome frame and the status line. IMGUI paints in call order,
        /// so this runs BEFORE the buttons and the box stays behind them.
        /// </summary>
        private void DrawChromeAndStatusLine(Rect zone, Rect buttonsRect)
        {
            GUI.Box(
                new Rect(buttonsRect.x - 4f, buttonsRect.y - 4f, buttonsRect.width + 8f, buttonsRect.height + 8f),
                GUIContent.none, HudChrome.PanelStyle);

            string statusText = ResolveStatusLineText();
            if (statusText == null) return;

            var rect = new Rect(zone.center.x - StatusLineWidth * 0.5f, zone.y, StatusLineWidth, StatusLineHeight);
            GUI.Box(rect, GUIContent.none, HudChrome.PanelStyle);
            GUI.Label(rect, statusText, _statusStyle);
        }

        /// <summary>
        /// What the status line says, in priority order: the hovered entry's
        /// blocker reason (the player is interrogating that button right
        /// now), then the D-085 builder warning while any own site lacks a
        /// Builder, then the onboarding hint until it dismisses itself.
        /// Null = the line stays empty (and unpainted).
        /// </summary>
        private string ResolveStatusLineText()
        {
            if (_hoveredBlockerReason != null) return _hoveredBlockerReason;
            if (_transientNotice != null && Time.unscaledTime < _transientNoticeUntil) return _transientNotice;
            if (_siteLacksBuilder) return ConstructionSiteStatus.NoBuilderWarning;
            if (!_hintDismissed && _runner.IsRunning) return HintText;
            // Idle fallback: the camera controls, so the one undiscoverable
            // gesture (MMB rotate / Space reset) is documented WITHOUT F3.
            return "Kamera: Pfeile/Rand schwenken · Mausrad Zoom · MMB-Drag drehen · Space Reset";
        }

        /// <summary>
        /// A short-lived notice in the status line (e.g. a rejected command's
        /// reason): visible for a few seconds, then the line falls back to
        /// its normal priorities. A rejected order must never read as
        /// "kaputt" (sprint 09 §7).
        /// </summary>
        public void ShowTransientNotice(string text, float seconds = 4f)
        {
            _transientNotice = text;
            _transientNoticeUntil = Time.unscaledTime + seconds;
        }

        /// <summary>
        /// Exactly two lines: "Raffinerie (Y)" and "700 AE · 20 s", each
        /// hard-truncated with "…" to the button width — with wordWrap off a
        /// longer string would bleed out of the button instead of wrapping.
        /// Build ticks are whole seconds at 10 Hz (every MS-1 value is a
        /// multiple of 10). The German building names are shared with the
        /// command card through <see cref="CommandCardPresenter.BuildingDisplayName"/>.
        /// </summary>
        private string ButtonLabel(UnitRole role, in SimBuildingDefinition def, float buttonWidth)
        {
            float textWidth = buttonWidth - 12f; // the button style's horizontal padding

            _builder.Clear();
            _builder.Append(CommandCardPresenter.BuildingDisplayName(role));
            string hotkey = HotkeyHint(role);
            if (hotkey.Length > 0) _builder.Append(" (").Append(hotkey).Append(')');
            string nameLine = TruncateToWidth(_builder.ToString(), textWidth);

            _builder.Clear();
            _builder.Append(def.CostAE).Append(" AE · ").Append(def.BuildTicks / 10).Append(" s");
            string costLine = TruncateToWidth(_builder.ToString(), textWidth);

            return nameLine + "\n" + costLine;
        }

        /// <summary>The hovered entry's blocker, in the executor's own check order — prerequisite first, then affordability.</summary>
        private static string BlockerReason(
            UnitRole role, in SimBuildingDefinition def, byte slot, long credits, ConstructionSystem construction)
        {
            bool prerequisiteMet = !def.HasPrerequisite
                || construction.HasFinishedBuilding(slot, def.PrerequisiteRole);
            if (!prerequisiteMet)
            {
                return $"{CommandCardPresenter.BuildingDisplayName(role)}: benötigt {CommandCardPresenter.BuildingDisplayName(def.PrerequisiteRole)}";
            }
            return $"{CommandCardPresenter.BuildingDisplayName(role)}: nicht genug Aetherium";
        }

        /// <summary>
        /// Hard truncation to a pixel width against the real button style:
        /// drops characters until the text plus the ellipsis fits. IMGUI does
        /// not ellipsize by itself — without this a long German name would
        /// simply overflow the button (wordWrap is off on purpose, so it
        /// cannot wrap into a phantom third line either).
        /// </summary>
        private string TruncateToWidth(string text, float maxWidth)
        {
            var content = new GUIContent(text);
            if (_buttonStyle.CalcSize(content).x <= maxWidth) return text;

            const string ellipsis = "…";
            for (int length = text.Length - 1; length > 0; length--)
            {
                content.text = text.Substring(0, length) + ellipsis;
                if (_buttonStyle.CalcSize(content).x <= maxWidth) return content.text;
            }
            return ellipsis;
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
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, wordWrap = false };
            }
            if (_statusStyle == null)
            {
                _statusStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = false
                };
            }
        }
    }
}
