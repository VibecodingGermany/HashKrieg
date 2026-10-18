using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The unit command bar (D-117): a Command &amp; Conquer: Generals –
    /// Zero Hour style control panel in the bottom-right corner, built in UI
    /// Toolkit (D-083). Shown whenever the local player has mobile units
    /// selected; the IMGUI command card keeps buildings and sites.
    /// <list type="bullet">
    /// <item>Header: lead type and count, current formation. Tailored per
    /// unit type: buttons that make no sense for the selection (patrol for a
    /// harvester, formations without armed units) are HIDDEN, not greyed —
    /// see <see cref="CommandCardPresenter.GetBarCommands"/>.</item>
    /// <item>Harvester selections show a cargo readout (fill bar, "Ladung x / y",
    /// status line) instead.</item>
    /// <item>Selection chips per unit type — click to keep only that type.</item>
    /// <item>A 5×3 grid of square buttons with drawn icons and hotkey badges:
    /// orders (move, attack-move, stop, guard, patrol), role actions (scatter,
    /// harvest, deliver, repair, attack target) and five formations.</item>
    /// <item>A tooltip line that names the hovered button and its hotkey, or
    /// the armed pick.</item>
    /// </list>
    /// Icons are painted with <see cref="Painter2D"/> — no texture assets.
    /// </summary>
    public sealed class UnitCommandBar : MonoBehaviour
    {
        private static readonly List<UnitCommandBar> Instances = new List<UnitCommandBar>(1);

        /// <summary>True while a bar shows a unit selection (the IMGUI card then stays out of the way).</summary>
        public static bool ShowsUnits
        {
            get
            {
                for (int i = 0; i < Instances.Count; i++) if (Instances[i]._visible) return true;
                return false;
            }
        }

        /// <summary>Pointer test in raw screen coordinates (Input.mousePosition), for the world-click lock.</summary>
        public static bool IsPointerOverAny(Vector2 rawMouse)
        {
            for (int i = 0; i < Instances.Count; i++) if (Instances[i].IsPointerOver(rawMouse)) return true;
            return false;
        }

        [SerializeField] private UIDocument _document;
        [SerializeField] private RtsDeviceInput _input;
        [SerializeField] private BuildMenuHud _buildMenu;
        [SerializeField] private Font _titleFont;
        [SerializeField] private Font _bodyFont;
        [SerializeField] private float _buttonSize = 58f;
        [SerializeField] private float _margin = 12f;

        private const int Columns = 5;

        private enum Cmd
        {
            Move, AttackMove, Stop, Guard, Patrol,
            Scatter, Harvest, Deliver, Repair, AttackTarget,
            Loose, Line, Column, Wedge, Block,
        }

        private sealed class Slot
        {
            public Cmd Cmd;
            public VisualElement Root;
            public VisualElement Icon;
            public Label Hotkey;
            public string Name;
            public string Key;
            public string Help;
            public bool Enabled;
            public bool Active;
            public bool Visible = true;
        }

        private readonly CommandCardPresenter _presenter = new CommandCardPresenter();
        private readonly SelectionGroup[] _groups = new SelectionGroup[16];
        private readonly UnitRole[] _roles = new UnitRole[16];
        private readonly List<Slot> _slots = new List<Slot>(15);
        private VisualElement _bar;
        private Label _title;
        private Label _formationLabel;
        private VisualElement _chips;
        private Label _hint;
        private VisualElement _cargoPanel;
        private VisualElement _cargoFill;
        private Label _cargoText;
        private Label _cargoStatus;
        private bool _visible;
        private bool _built;
        private string _hoverText;
        private int _chipSignature = -1;
        private Color _accent = new Color(0.28f, 0.78f, 0.92f);

        private void OnEnable() => Instances.Add(this);

        private void OnDisable()
        {
            Instances.Remove(this);
            _visible = false;
        }

        private void Update()
        {
            if (!_built) Build();
            if (_bar == null) return;

            bool show = ComputeVisible(out FactionId faction, out ReadOnlySpan<EntityId> selection);
            if (show != _visible)
            {
                _visible = show;
                _bar.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (!show) return;

            _accent = faction == FactionId.Legion ? new Color(0.95f, 0.52f, 0.22f) : new Color(0.30f, 0.80f, 0.95f);
            Refresh(faction, selection);
            PlaceBar();
        }

        private bool ComputeVisible(out FactionId faction, out ReadOnlySpan<EntityId> selection)
        {
            faction = FactionId.Alliance;
            selection = default;
            if (_input == null || !_input.IsWired || ModalSurfaceLink.Open) return false;
            MatchRunner runner = FindRunner();
            if (runner == null || !runner.IsRunning || runner.Entities == null) return false;

            selection = _input.Selection.SelectedEntities;
            if (selection.Length == 0) return false;
            byte slot = runner.Session != null ? runner.Session.LocalSlot : (byte)0;
            faction = runner.Economy.GetSlotFaction(slot);
            for (int i = 0; i < selection.Length; i++)
            {
                if (runner.Entities.TryGetUnit(selection[i], out UnitState u)
                    && u.PlayerId == slot && !SimDefinitions.IsBuildingRole(u.Role) && u.Role != UnitRole.Unit)
                {
                    return true;
                }
            }
            return false;
        }

        private MatchRunner _runner;

        private MatchRunner FindRunner()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            return _runner;
        }

        // ----------------------------------------------------------------
        // Build
        // ----------------------------------------------------------------

        private void Build()
        {
            if (_document == null || _document.rootVisualElement == null) return;
            _built = true;
            VisualElement root = _document.rootVisualElement;

            _bar = new VisualElement { name = "unit-command-bar", pickingMode = PickingMode.Position };
            _bar.style.position = Position.Absolute;
            _bar.style.flexDirection = FlexDirection.Column;
            _bar.style.paddingLeft = _bar.style.paddingRight = 10;
            _bar.style.paddingTop = 8;
            _bar.style.paddingBottom = 8;
            _bar.style.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.93f);
            SetBorder(_bar, 1f, new Color(0.02f, 0.03f, 0.05f, 1f));
            _bar.style.borderTopWidth = 2f;
            _bar.style.borderTopLeftRadius = _bar.style.borderTopRightRadius = 4f;
            _bar.style.display = DisplayStyle.None;
            root.Add(_bar);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.marginBottom = 4;
            _title = MakeLabel(15, _titleFont, Color.white);
            _formationLabel = MakeLabel(12, _bodyFont, new Color(0.75f, 0.8f, 0.85f));
            header.Add(_title);
            header.Add(_formationLabel);
            _bar.Add(header);

            _chips = new VisualElement();
            _chips.style.flexDirection = FlexDirection.Row;
            _chips.style.flexWrap = Wrap.Wrap;
            _chips.style.marginBottom = 6;
            _bar.Add(_chips);

            BuildCargoPanel();

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.width = Columns * (_buttonSize + 4f);
            _bar.Add(grid);

            AddSlot(grid, Cmd.Move, "Bewegen", "RMB", "Einheiten zum Zielpunkt schicken; die gewählte Formation gilt.");
            AddSlot(grid, Cmd.AttackMove, "Angriffsbewegung", "A", "Zum Ziel fahren und unterwegs jeden sichtbaren Feind angreifen.");
            AddSlot(grid, Cmd.Stop, "Stopp", "S", "Alle Befehle abbrechen und stehen bleiben.");
            AddSlot(grid, Cmd.Guard, "Bewachen", "W", "Die Stellung halten: Feinde in der Nähe angreifen, danach zurückkehren.");
            AddSlot(grid, Cmd.Patrol, "Patrouille", "K", "Zwischen jetziger Position und Ziel pendeln und unterwegs kämpfen.");
            AddSlot(grid, Cmd.Scatter, "Verteilen", "J", "Die Gruppe auseinanderziehen — gegen Flächenschaden.");
            AddSlot(grid, Cmd.Harvest, "Ernten", "H", "Sammler zu einem Aetherium-Feld schicken.");
            AddSlot(grid, Cmd.Deliver, "Fracht abliefern", "R", "Sammler bringen ihre Ladung zur Raffinerie.");
            AddSlot(grid, Cmd.Repair, "Reparieren", "", "Bauer reparieren ein beschädigtes eigenes Gebäude.");
            AddSlot(grid, Cmd.AttackTarget, "Ziel angreifen", "", "Einen bestimmten Feind auswählen und angreifen.");
            AddSlot(grid, Cmd.Loose, "Formation: Locker", "M", "Keine feste Aufstellung — die Gruppe verteilt sich ums Ziel.");
            AddSlot(grid, Cmd.Line, "Formation: Linie", "M", "Breite Front quer zur Marschrichtung.");
            AddSlot(grid, Cmd.Column, "Formation: Kolonne", "M", "Schmale Marschkolonne, zwei Einheiten breit.");
            AddSlot(grid, Cmd.Wedge, "Formation: Keil", "M", "Speerspitze: eine Einheit vorn, die anderen gestaffelt dahinter.");
            AddSlot(grid, Cmd.Block, "Formation: Block", "M", "Kompaktes Viereck.");

            _hint = MakeLabel(12, _bodyFont, new Color(0.82f, 0.86f, 0.9f));
            _hint.style.marginTop = 6;
            _hint.style.whiteSpace = WhiteSpace.Normal;
            _hint.style.width = Columns * (_buttonSize + 4f);
            _hint.style.minHeight = 30;
            _bar.Add(_hint);
        }

        private static readonly Color AetheriumColor = new Color(0.35f, 0.9f, 1f);

        /// <summary>Harvester readout: a fill bar in Aetherium colour with the cargo text on it, status line below.</summary>
        private void BuildCargoPanel()
        {
            _cargoPanel = new VisualElement { name = "cargo-panel", pickingMode = PickingMode.Ignore };
            _cargoPanel.style.flexDirection = FlexDirection.Column;
            _cargoPanel.style.marginBottom = 8;
            _cargoPanel.style.width = Columns * (_buttonSize + 4f);
            _cargoPanel.style.display = DisplayStyle.None;

            var track = new VisualElement { pickingMode = PickingMode.Ignore };
            track.style.height = 26;
            track.style.backgroundColor = new Color(0.07f, 0.09f, 0.12f, 1f);
            SetBorder(track, 1f, new Color(0.22f, 0.29f, 0.36f));
            track.style.borderTopLeftRadius = track.style.borderTopRightRadius = 3f;
            track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = 3f;

            _cargoFill = new VisualElement { pickingMode = PickingMode.Ignore };
            _cargoFill.style.position = Position.Absolute;
            _cargoFill.style.left = 0;
            _cargoFill.style.top = 0;
            _cargoFill.style.bottom = 0;
            _cargoFill.style.width = new Length(0f, LengthUnit.Percent);
            _cargoFill.style.backgroundColor = AetheriumColor;
            track.Add(_cargoFill);

            _cargoText = MakeLabel(14, _titleFont, Color.white);
            _cargoText.name = "cargo-text";
            _cargoText.style.position = Position.Absolute;
            _cargoText.style.left = 0;
            _cargoText.style.right = 0;
            _cargoText.style.top = 0;
            _cargoText.style.bottom = 0;
            _cargoText.style.unityTextAlign = TextAnchor.MiddleCenter;
            _cargoText.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 2f, color = new Color(0f, 0f, 0f, 0.95f) };
            track.Add(_cargoText);
            _cargoPanel.Add(track);

            _cargoStatus = MakeLabel(12, _bodyFont, new Color(0.82f, 0.9f, 0.95f));
            _cargoStatus.style.marginTop = 3;
            _cargoPanel.Add(_cargoStatus);
            _bar.Add(_cargoPanel);
        }

        private void AddSlot(VisualElement grid, Cmd cmd, string name, string key, string help)
        {
            var slot = new Slot { Cmd = cmd, Name = name, Key = key, Help = help };
            var button = new VisualElement { name = $"cmd-{cmd}", pickingMode = PickingMode.Position };
            button.style.width = _buttonSize;
            button.style.height = _buttonSize;
            button.style.marginRight = 4;
            button.style.marginBottom = 4;
            button.style.borderTopLeftRadius = button.style.borderTopRightRadius = 3f;
            button.style.borderBottomLeftRadius = button.style.borderBottomRightRadius = 3f;
            SetBorder(button, 1f, new Color(0.2f, 0.27f, 0.34f));

            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.style.position = Position.Absolute;
            icon.style.left = icon.style.top = icon.style.right = icon.style.bottom = 0;
            icon.generateVisualContent += ctx => DrawIcon(ctx, slot);
            button.Add(icon);

            var hotkey = MakeLabel(10, _bodyFont, new Color(1f, 1f, 1f, 0.85f));
            hotkey.text = key;
            hotkey.pickingMode = PickingMode.Ignore;
            hotkey.style.position = Position.Absolute;
            hotkey.style.left = 3;
            hotkey.style.top = 1;
            button.Add(hotkey);

            button.RegisterCallback<PointerEnterEvent>(_ => _hoverText = Describe(slot));
            button.RegisterCallback<PointerLeaveEvent>(_ => _hoverText = null);
            button.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || !slot.Enabled) return;
                Execute(slot.Cmd);
                e.StopPropagation();
            });

            slot.Root = button;
            slot.Icon = icon;
            slot.Hotkey = hotkey;
            _slots.Add(slot);
            grid.Add(button);
        }

        private static string Describe(Slot s)
        {
            string key = string.IsNullOrEmpty(s.Key) ? string.Empty : $" ({s.Key})";
            string state = s.Enabled ? string.Empty : "  — für diese Auswahl nicht verfügbar";
            return $"{s.Name}{key}: {s.Help}{state}";
        }

        // ----------------------------------------------------------------
        // Refresh
        // ----------------------------------------------------------------

        private void Refresh(FactionId faction, ReadOnlySpan<EntityId> selection)
        {
            MatchRunner runner = FindRunner();
            int groupCount = _presenter.SummarizeSelection(selection, runner.Entities, _groups);
            int roleCount = 0;
            int mobile = 0;
            int harvesters = 0;
            UnitRole leadRole = UnitRole.Unit;
            int signature = 17;
            for (int i = 0; i < groupCount; i++)
            {
                if (SimDefinitions.IsBuildingRole(_groups[i].Role)) continue;
                if (mobile == 0) leadRole = _groups[i].Role;
                mobile += _groups[i].Count;
                if (_groups[i].Role == UnitRole.Harvester) harvesters += _groups[i].Count;
                _roles[roleCount++] = _groups[i].Role;
                signature = signature * 31 + (int)_groups[i].Role * 101 + _groups[i].Count;
            }

            BarCommand shown = _presenter.GetBarCommands(faction, _roles.AsSpan(0, roleCount));
            bool harvesterOnly = mobile > 0 && harvesters == mobile;

            if (harvesterOnly && mobile > 1) _title.text = $"{mobile} Sammler";
            else if (mobile <= 1) _title.text = CommandCardPresenter.UnitDisplayName(faction, leadRole);
            else _title.text = $"{CommandCardPresenter.UnitDisplayName(faction, leadRole)} — {mobile} Einheiten";

            bool formations = shown.HasFlag(BarCommand.Formations);
            _formationLabel.style.display = formations ? DisplayStyle.Flex : DisplayStyle.None;
            _formationLabel.text = $"Formation: {FormationPlanner.DisplayName(_input.Formation)}";

            _cargoPanel.style.display = harvesterOnly ? DisplayStyle.Flex : DisplayStyle.None;
            if (harvesterOnly) RefreshCargo(faction, selection, runner);
            _bar.style.borderTopColor = _accent;

            if (signature != _chipSignature)
            {
                _chipSignature = signature;
                RebuildChips(faction, groupCount);
            }

            StandingOrderKind standing = _input.SelectionStandingOrder;
            string armedPick = _input.ArmedOrderName;

            foreach (Slot s in _slots)
            {
                switch (s.Cmd)
                {
                    case Cmd.Move: Set(s, shown.HasFlag(BarCommand.Move), armedPick == "Bewegen"); break;
                    case Cmd.AttackMove: Set(s, shown.HasFlag(BarCommand.AttackMove), armedPick == "Angriffsbewegung" || standing == StandingOrderKind.AttackMove); break;
                    case Cmd.Stop: Set(s, shown.HasFlag(BarCommand.Stop), false); break;
                    case Cmd.Guard: Set(s, shown.HasFlag(BarCommand.Guard), standing == StandingOrderKind.Guard); break;
                    case Cmd.Patrol: Set(s, shown.HasFlag(BarCommand.Patrol), armedPick == "Patrouille" || standing == StandingOrderKind.Patrol); break;
                    case Cmd.Scatter: Set(s, shown.HasFlag(BarCommand.Scatter), false, mobile > 1); break;
                    case Cmd.Harvest: Set(s, shown.HasFlag(BarCommand.Harvest), armedPick == "Ernten"); break;
                    case Cmd.Deliver: Set(s, shown.HasFlag(BarCommand.ReturnCargo), false); break;
                    case Cmd.Repair: Set(s, shown.HasFlag(BarCommand.Repair), armedPick == "Reparieren"); break;
                    case Cmd.AttackTarget: Set(s, shown.HasFlag(BarCommand.AttackTarget), armedPick == "Angriff"); break;
                    case Cmd.Loose: Set(s, formations, _input.Formation == FormationKind.Loose); break;
                    case Cmd.Line: Set(s, formations, _input.Formation == FormationKind.Line); break;
                    case Cmd.Column: Set(s, formations, _input.Formation == FormationKind.Column); break;
                    case Cmd.Wedge: Set(s, formations, _input.Formation == FormationKind.Wedge); break;
                    case Cmd.Block: Set(s, formations, _input.Formation == FormationKind.Block); break;
                }
            }

            if (_hoverText != null) _hint.text = _hoverText;
            else if (!string.IsNullOrEmpty(armedPick)) _hint.text = $"{armedPick}: Ziel wählen — Linksklick bestätigt, Rechtsklick bricht ab";
            else _hint.text = harvesterOnly ? "Rechtsklick: bewegen · H: ernten · R: Fracht abliefern" : StandingText(standing);
        }

        private void RefreshCargo(FactionId faction, ReadOnlySpan<EntityId> selection, MatchRunner runner)
        {
            HarvesterSummary sum = _presenter.SummarizeHarvesters(faction, selection, runner.Entities);
            _cargoFill.style.width = new Length(sum.Fill * 100f, LengthUnit.Percent);
            _cargoText.text = CommandCardPresenter.FormatCargo(sum.CargoAE, sum.CapacityAE);
            _cargoStatus.text = CommandCardPresenter.FormatHarvesterStatusLine(in sum);
        }

        private static string StandingText(StandingOrderKind standing)
        {
            switch (standing)
            {
                case StandingOrderKind.AttackMove: return "Auftrag: Angriffsbewegung läuft";
                case StandingOrderKind.Patrol: return "Auftrag: Patrouille läuft";
                case StandingOrderKind.Guard: return "Auftrag: Stellung bewachen";
                default: return "Rechtsklick: bewegen · A: Angriffsbewegung · Formation mit M wechseln";
            }
        }

        /// <summary>Visible buttons are clickable (hidden ones take no grid space); <paramref name="usable"/> can still grey a shown one (scatter of a single unit).</summary>
        private void Set(Slot s, bool visible, bool active, bool usable = true)
        {
            bool enabled = visible && usable;
            if (s.Visible == visible && s.Enabled == enabled && s.Active == active && s.Root.userData != null) return;
            s.Root.userData = this;   // first refresh always paints
            s.Visible = visible;
            s.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            s.Enabled = enabled;
            s.Active = active;
            Color fill = !enabled ? new Color(0.07f, 0.08f, 0.10f, 0.9f)
                : active ? Color.Lerp(new Color(0.10f, 0.14f, 0.19f), _accent, 0.35f)
                : new Color(0.10f, 0.14f, 0.19f, 0.97f);
            s.Root.style.backgroundColor = fill;
            SetBorder(s.Root, active ? 2f : 1f, active ? _accent : new Color(0.22f, 0.29f, 0.36f));
            s.Hotkey.style.color = enabled ? new Color(1f, 1f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.25f);
            s.Icon.MarkDirtyRepaint();
        }

        private void RebuildChips(FactionId faction, int groupCount)
        {
            _chips.Clear();
            for (int i = 0; i < groupCount; i++)
            {
                SelectionGroup g = _groups[i];
                if (SimDefinitions.IsBuildingRole(g.Role)) continue;
                UnitRole role = g.Role;
                var chip = new Label($"{CommandCardPresenter.UnitDisplayName(faction, role)} ×{g.Count}");
                if (_bodyFont != null) chip.style.unityFontDefinition = FontDefinition.FromFont(_bodyFont);
                chip.style.fontSize = 12;
                chip.style.color = Color.white;
                chip.style.backgroundColor = new Color(0.12f, 0.17f, 0.23f);
                SetBorder(chip, 1f, HudChrome.RoleAccentColor(role));
                chip.style.paddingLeft = chip.style.paddingRight = 6;
                chip.style.paddingTop = chip.style.paddingBottom = 1;
                chip.style.marginRight = 4;
                chip.style.marginBottom = 3;
                chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius = 3f;
                chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 3f;
                chip.RegisterCallback<PointerEnterEvent>(_ => _hoverText = "Klick: nur diesen Typ auswählen");
                chip.RegisterCallback<PointerLeaveEvent>(_ => _hoverText = null);
                chip.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button != 0) return;
                    _input.FilterSelectionToRole(role);
                    e.StopPropagation();
                });
                _chips.Add(chip);
            }
        }

        private void Execute(Cmd cmd)
        {
            switch (cmd)
            {
                case Cmd.Move: _input.RequestMoveOrder(); break;
                case Cmd.AttackMove: _input.RequestAttackMoveOrder(); break;
                case Cmd.Stop: _input.OrderStop(); break;
                case Cmd.Guard: _input.OrderGuard(); break;
                case Cmd.Patrol: _input.RequestPatrolOrder(); break;
                case Cmd.Scatter: _input.OrderScatter(); break;
                case Cmd.Harvest: _input.RequestHarvestOrder(); break;
                case Cmd.Deliver: _input.OrderReturnCargo(); break;
                case Cmd.Repair: _input.RequestRepairOrder(); break;
                case Cmd.AttackTarget: _input.RequestAttackOrder(); break;
                case Cmd.Loose: _input.SetFormation(FormationKind.Loose); break;
                case Cmd.Line: _input.SetFormation(FormationKind.Line); break;
                case Cmd.Column: _input.SetFormation(FormationKind.Column); break;
                case Cmd.Wedge: _input.SetFormation(FormationKind.Wedge); break;
                case Cmd.Block: _input.SetFormation(FormationKind.Block); break;
            }
        }

        /// <summary>Bottom-right, directly above the build bar's reserve (screen px converted to panel px).</summary>
        private void PlaceBar()
        {
            IPanel panel = _bar.panel;
            if (panel == null) return;
            float reserveScreen = _buildMenu != null ? _buildMenu.OccupiedHeight * Mathf.Max(1f, _buildMenu.UiScale) : 0f;
            Vector2 origin = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, 0f));
            Vector2 reserve = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, reserveScreen));
            _bar.style.right = _margin;
            _bar.style.bottom = (reserve.y - origin.y) + _margin;
        }

        private bool IsPointerOver(Vector2 rawMouse)
        {
            if (!_visible || _bar == null || _bar.panel == null) return false;
            Vector2 p = RuntimePanelUtils.ScreenToPanel(_bar.panel, new Vector2(rawMouse.x, Screen.height - rawMouse.y));
            return _bar.worldBound.Contains(p);
        }

        // ----------------------------------------------------------------
        // Icons
        // ----------------------------------------------------------------

        private void DrawIcon(MeshGenerationContext ctx, Slot slot)
        {
            Rect r = slot.Icon.contentRect;
            if (r.width < 4f) return;
            Painter2D p = ctx.painter2D;
            Color ink = slot.Enabled ? (slot.Active ? Color.white : new Color(0.85f, 0.92f, 0.97f)) : new Color(0.5f, 0.55f, 0.6f, 0.4f);
            p.strokeColor = ink;
            p.fillColor = ink;
            p.lineWidth = Mathf.Max(2f, r.width * 0.055f);
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            var c = new Vector2(r.width * 0.5f, r.height * 0.56f);
            float u = r.width * 0.30f;

            switch (slot.Cmd)
            {
                case Cmd.Move: Arrow(p, c + new Vector2(-u, u), c + new Vector2(u, -u), u * 0.45f); break;
                case Cmd.AttackMove:
                    Arrow(p, c + new Vector2(-u, u), c + new Vector2(u * 0.4f, -u * 0.4f), u * 0.4f);
                    Crosshair(p, c + new Vector2(u * 0.55f, -u * 0.55f), u * 0.42f, _accent);
                    p.strokeColor = ink;
                    break;
                case Cmd.Stop: Octagon(p, c, u * 0.95f, ink); break;
                case Cmd.Guard: Shield(p, c, u); break;
                case Cmd.Patrol: Loop(p, c, u * 0.8f); break;
                case Cmd.Scatter:
                    for (int i = 0; i < 4; i++)
                    {
                        float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                        var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Arrow(p, c + d * u * 0.25f, c + d * u * 1.05f, u * 0.3f);
                    }
                    break;
                case Cmd.Harvest: Crystal(p, c, u, new Color(0.35f, 0.9f, 1f, slot.Enabled ? 1f : 0.35f)); break;
                case Cmd.Deliver:
                    Box(p, c + new Vector2(0, u * 0.35f), u * 0.75f);
                    Arrow(p, c + new Vector2(0, -u * 1.05f), c + new Vector2(0, -u * 0.05f), u * 0.35f);
                    break;
                case Cmd.Repair: Wrench(p, c, u); break;
                case Cmd.AttackTarget: Crosshair(p, c, u * 0.9f, ink); break;
                case Cmd.Loose: Dots(p, c, u, new[] { new Vector2(-0.8f, -0.5f), new Vector2(0.3f, -0.9f), new Vector2(0.9f, 0.1f), new Vector2(-0.2f, 0.4f), new Vector2(-0.9f, 0.8f), new Vector2(0.6f, 0.9f) }); break;
                case Cmd.Line: Dots(p, c, u, new[] { new Vector2(-1f, 0f), new Vector2(-0.5f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0f), new Vector2(1f, 0f) }); break;
                case Cmd.Column: Dots(p, c, u, new[] { new Vector2(-0.3f, -1f), new Vector2(0.3f, -1f), new Vector2(-0.3f, -0.33f), new Vector2(0.3f, -0.33f), new Vector2(-0.3f, 0.33f), new Vector2(0.3f, 0.33f), new Vector2(-0.3f, 1f), new Vector2(0.3f, 1f) }); break;
                case Cmd.Wedge: Dots(p, c, u, new[] { new Vector2(0f, -0.9f), new Vector2(-0.45f, -0.3f), new Vector2(0.45f, -0.3f), new Vector2(-0.9f, 0.3f), new Vector2(0.9f, 0.3f) }); break;
                case Cmd.Block: Dots(p, c, u, new[] { new Vector2(-0.7f, -0.7f), new Vector2(0f, -0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, 0f), new Vector2(0f, 0f), new Vector2(0.7f, 0f), new Vector2(-0.7f, 0.7f), new Vector2(0f, 0.7f), new Vector2(0.7f, 0.7f) }); break;
            }
        }

        private static void Arrow(Painter2D p, Vector2 from, Vector2 to, float head)
        {
            p.BeginPath(); p.MoveTo(from); p.LineTo(to); p.Stroke();
            Vector2 d = (to - from).normalized;
            var n = new Vector2(-d.y, d.x);
            p.BeginPath();
            p.MoveTo(to - d * head + n * head * 0.6f);
            p.LineTo(to);
            p.LineTo(to - d * head - n * head * 0.6f);
            p.Stroke();
        }

        private static void Crosshair(Painter2D p, Vector2 c, float r, Color color)
        {
            p.strokeColor = color;
            p.BeginPath(); p.Arc(c, r * 0.7f, 0f, 360f); p.Stroke();
            p.BeginPath(); p.MoveTo(c + new Vector2(-r, 0)); p.LineTo(c + new Vector2(-r * 0.35f, 0)); p.Stroke();
            p.BeginPath(); p.MoveTo(c + new Vector2(r, 0)); p.LineTo(c + new Vector2(r * 0.35f, 0)); p.Stroke();
            p.BeginPath(); p.MoveTo(c + new Vector2(0, -r)); p.LineTo(c + new Vector2(0, -r * 0.35f)); p.Stroke();
            p.BeginPath(); p.MoveTo(c + new Vector2(0, r)); p.LineTo(c + new Vector2(0, r * 0.35f)); p.Stroke();
        }

        private static void Octagon(Painter2D p, Vector2 c, float r, Color ink)
        {
            p.fillColor = new Color(0.85f, 0.2f, 0.18f, ink.a);
            p.BeginPath();
            for (int i = 0; i < 8; i++)
            {
                float a = Mathf.PI / 8f + i * Mathf.PI / 4f;
                var v = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (i == 0) p.MoveTo(v); else p.LineTo(v);
            }
            p.ClosePath();
            p.Fill();
            p.Stroke();
        }

        private static void Shield(Painter2D p, Vector2 c, float u)
        {
            p.BeginPath();
            p.MoveTo(c + new Vector2(0, -u));
            p.LineTo(c + new Vector2(u * 0.85f, -u * 0.6f));
            p.LineTo(c + new Vector2(u * 0.7f, u * 0.35f));
            p.LineTo(c + new Vector2(0, u));
            p.LineTo(c + new Vector2(-u * 0.7f, u * 0.35f));
            p.LineTo(c + new Vector2(-u * 0.85f, -u * 0.6f));
            p.ClosePath();
            p.Stroke();
            p.BeginPath(); p.MoveTo(c + new Vector2(0, -u * 0.55f)); p.LineTo(c + new Vector2(0, u * 0.55f)); p.Stroke();
        }

        private static void Loop(Painter2D p, Vector2 c, float r)
        {
            p.BeginPath(); p.Arc(c, r, 200f, 340f); p.Stroke();
            p.BeginPath(); p.Arc(c, r, 20f, 160f); p.Stroke();
            Vector2 e1 = c + new Vector2(Mathf.Cos(340f * Mathf.Deg2Rad), Mathf.Sin(340f * Mathf.Deg2Rad)) * r;
            Vector2 e2 = c + new Vector2(Mathf.Cos(160f * Mathf.Deg2Rad), Mathf.Sin(160f * Mathf.Deg2Rad)) * r;
            Arrow(p, e1 + new Vector2(-r * 0.3f, -r * 0.25f), e1, r * 0.4f);
            Arrow(p, e2 + new Vector2(r * 0.3f, r * 0.25f), e2, r * 0.4f);
        }

        private static void Crystal(Painter2D p, Vector2 c, float u, Color color)
        {
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(c + new Vector2(0, -u));
            p.LineTo(c + new Vector2(u * 0.55f, -u * 0.2f));
            p.LineTo(c + new Vector2(0, u));
            p.LineTo(c + new Vector2(-u * 0.55f, -u * 0.2f));
            p.ClosePath();
            p.Fill();
        }

        private static void Box(Painter2D p, Vector2 c, float h)
        {
            p.BeginPath();
            p.MoveTo(c + new Vector2(-h, -h * 0.6f));
            p.LineTo(c + new Vector2(h, -h * 0.6f));
            p.LineTo(c + new Vector2(h, h * 0.6f));
            p.LineTo(c + new Vector2(-h, h * 0.6f));
            p.ClosePath();
            p.Stroke();
        }

        private static void Wrench(Painter2D p, Vector2 c, float u)
        {
            Vector2 a = c + new Vector2(-u * 0.75f, u * 0.75f);
            Vector2 b = c + new Vector2(u * 0.35f, -u * 0.35f);
            p.BeginPath(); p.MoveTo(a); p.LineTo(b); p.Stroke();
            p.BeginPath(); p.Arc(c + new Vector2(u * 0.55f, -u * 0.55f), u * 0.38f, 160f, 470f); p.Stroke();
        }

        private static void Dots(Painter2D p, Vector2 c, float u, Vector2[] at)
        {
            float r = u * 0.14f;
            for (int i = 0; i < at.Length; i++)
            {
                p.BeginPath();
                p.Arc(c + at[i] * u * 0.9f, r, 0f, 360f);
                p.Fill();
            }
        }

        // ----------------------------------------------------------------

        private static Label MakeLabel(int size, Font font, Color color)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            l.style.fontSize = size;
            l.style.color = color;
            if (font != null) l.style.unityFontDefinition = FontDefinition.FromFont(font);
            return l;
        }

        private static void SetBorder(VisualElement e, float width, Color color)
        {
            e.style.borderLeftWidth = e.style.borderRightWidth = width;
            e.style.borderTopWidth = e.style.borderBottomWidth = width;
            e.style.borderLeftColor = e.style.borderRightColor = color;
            e.style.borderTopColor = e.style.borderBottomColor = color;
        }
    }
}
