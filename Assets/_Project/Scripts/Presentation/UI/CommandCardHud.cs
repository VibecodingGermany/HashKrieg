using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Audio;
using Nova.Gameplay.Match;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Production;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// The command card: the context panel docked to the bottom-right corner,
    /// directly above the build bar. Shows the buttons the CURRENT selection
    /// supports and dispatches every order through <see cref="RtsDeviceInput"/>
    /// — the card owns no dispatcher, so the ingress keeps its single caller.
    /// <para>
    /// CONTENT MODEL (schema v1, MS-1): the role→buttons mapping lives in
    /// <see cref="CommandCardPresenter"/> (the testable, Unity-free brain);
    /// this component only renders it and evaluates live state. A mobile lead
    /// unit gets Move/Stop, Attack when armed, Harvest/ReturnCargo on the
    /// Harvester and Repair on the Builder. A completed own building gets
    /// Sell (50% refund) and Repair (greyed with the reason when undamaged
    /// or when no Builder exists), plus — for producers — one production
    /// button per unit the building builds (from
    /// <see cref="SimDefinitions.AllUnits"/> filtered by faction and producer
    /// role, the table the executor validates against), greyed WITH the
    /// reason (benötigt Forschungslabor / Warteschlange voll / nicht genug
    /// Aetherium — the executor's own validation order, so the reason the
    /// player reads is the one the executor would reject with). The queue is
    /// shown with the head entry's progress (Q16.16 ticks against
    /// BuildTicks&lt;&lt;16) and a per-entry cancel. A construction site gets
    /// its progress and CancelConstruction (75% refund).
    /// </para>
    /// <para>
    /// REPAIR FLOW (decided, GB-006): the sim issues repair as a BUILDER-side
    /// standing order on an own damaged completed building — there is no
    /// building-side assignment. The unit card therefore arms a target pick
    /// (<see cref="RtsDeviceInput.RequestRepairOrder"/>), and the building
    /// card assigns the lowest-index living own Builder (the sim's own site
    /// auto-assignment convention). Because the standing order has no
    /// movement coupling (held while out of reach), both flows dispatch Move
    /// + Repair so the Builder actually walks into reach.
    /// </para>
    /// <para>
    /// DELIBERATELY NOT HERE: <see cref="CommandButtonType.InstallDefenseModule"/>
    /// is rendered DISABLED with the honest reason (defense modules are
    /// G2/G4 content; the sim rejects the kind in this slice) and never
    /// dispatches. Research upgrades and tower target priorities have no
    /// schema-v1 CommandKind at all — they get NO button (open design
    /// question, to be logged; nothing is improvised around the frozen
    /// schema).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommandCardHud : MonoBehaviour
    {
        // Panel geometry (GUI space, before the UI scale): the card docks to
        // the bottom-right corner directly above the build bar.
        private const float TitleHeight = 22f;
        private const float SectionHeight = 20f;
        private const float RowHeight = 20f;
        private const float ProgressHeight = 8f;
        private const float SiteStatusHeight = 28f;
        private const float FooterHeight = 16f;

        /// <summary>Canonical panel width (the serialized default), read by HudLayout for the zones that must clear the card column.</summary>
        public const float DefaultPanelWidth = 236f;

        /// <summary>What a card button does when clicked. Payload: QueueUnit = unit defId, CancelProduction = queue index.</summary>
        private enum CardAction
        {
            None,
            MovePick,
            Stop,
            AttackPick,
            HarvestPick,
            ReturnCargo,
            RepairPick,
            Sell,
            RepairBuilding,
            QueueUnit,
            CancelProduction,
            CancelConstruction,
        }

        private struct CardButton
        {
            public string Label;
            public bool Enabled;
            public CardAction Action;
            public int Payload;
        }

        private struct QueueRow
        {
            public string Label;
            public int Index;
            /// <summary>Head-entry progress 0..1; negative = no bar (only the head entry progresses).</summary>
            public float Progress01;
        }

        /// <summary>
        /// One frame's panel content. Rebuilt at most once per frame and
        /// shared between the hit test and the draw, so the two cannot
        /// drift apart (the OnGUI Layout/Repaint pair also reuses it).
        /// </summary>
        private sealed class CardModel
        {
            public bool Visible;
            public string Title = string.Empty;
            public EntityId LeadId;
            public readonly List<CardButton> Buttons = new List<CardButton>(16);
            public string QueueHeader;
            public readonly List<QueueRow> QueueRows = new List<QueueRow>(ProductionSystem.MaxQueueEntries);
            /// <summary>Site progress 0..1; negative = no bar under the title.</summary>
            public float ProgressBar01 = -1f;
            /// <summary>The site card's live state line (kein Builder / unterwegs / im Bau …); null on unit and building cards.</summary>
            public string SiteStatusText;
            /// <summary>The producer card's stall line when the head entry sits finished but cannot spawn (capacity / no free cell); null while production flows.</summary>
            public string QueueStallText;
            public string FooterHint;

            public void Clear()
            {
                Visible = false;
                Title = string.Empty;
                LeadId = EntityId.Invalid;
                Buttons.Clear();
                QueueHeader = null;
                QueueRows.Clear();
                ProgressBar01 = -1f;
                SiteStatusText = null;
                QueueStallText = null;
                FooterHint = null;
            }
        }

        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [SerializeField] private RtsDeviceInput _input;
        [Tooltip("Build bar; the card docks directly above it.")]
        [SerializeField] private BuildMenuHud _buildMenu;

        [Header("Presentation")]
        [Tooltip("Whole panel is scaled by this factor, matching the BuildMenuHud/DebugHud convention for Retina displays.")]
        [SerializeField] private float _uiScale = 1.5f;
        [SerializeField] private float _panelWidth = DefaultPanelWidth;
        [SerializeField] private float _panelMargin = 8f;

        private readonly CommandCardPresenter _presenter = new CommandCardPresenter();
        private readonly StringBuilder _builder = new StringBuilder(96);
        private readonly CardModel _model = new CardModel();
        private readonly SimUnitDefinition[] _producibleScratch = new SimUnitDefinition[SimDefinitions.UnitsPerFaction];

        private int _modelFrame = -1;
        private Rect _lastPanelRect;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _rowStyle;
        private GUIStyle _siteStatusStyle;
        private GUIStyle _cancelStyle;
        private GUIStyle _hintStyle;

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_input == null) _input = FindAnyObjectByType<RtsDeviceInput>();
            if (_buildMenu == null) _buildMenu = FindAnyObjectByType<BuildMenuHud>();
        }

        /// <summary>
        /// Screen-space hit test used by RtsDeviceInput: a pointer over the
        /// panel belongs to the HUD, so world selection drags and order-pick
        /// clicks are suppressed there. Uses the rect the panel was LAST
        /// DRAWN with (the model is rebuilt per frame and the panel's
        /// geometry changes only with the selection, so one frame of lag is
        /// invisible). Takes the RAW mouse position (bottom-left origin) and
        /// converts it into scaled top-left GUI space, exactly as
        /// <see cref="BuildMenuHud.IsPointerOverBar"/>.
        /// </summary>
        public bool IsPointerOverPanel(Vector2 mousePosition)
        {
            if (_lastPanelRect.width <= 0f) return false;
            float scale = Mathf.Max(1f, _uiScale);
            var gui = new Vector2(mousePosition.x / scale, (Screen.height - mousePosition.y) / scale);
            Rect panel = _lastPanelRect;
            panel.yMin -= 6f; // small forgiveness band, same as the build bar
            return panel.Contains(gui);
        }

        // ----------------------------------------------------------------
        // Content model
        // ----------------------------------------------------------------

        private CardModel EnsureModel()
        {
            if (_modelFrame == Time.frameCount) return _model;
            _modelFrame = Time.frameCount;
            _model.Clear();
            BuildModel(_model);
            return _model;
        }

        private void BuildModel(CardModel model)
        {
            EntityManager entities = _runner.Entities;
            if (entities == null || _input == null) return;

            ReadOnlySpan<EntityId> selected = _input.Selection.SelectedEntities;
            if (selected.Length == 0) return;
            if (!entities.TryGetUnit(selected[0], out UnitState lead)) return; // stale handle

            byte slot = _runner.Session != null ? _runner.Session.LocalSlot : (byte)0;
            if (lead.PlayerId != slot) return; // foreign entities get no orders

            FactionId faction = _runner.Economy != null
                ? _runner.Economy.GetSlotFaction(slot)
                : FactionId.Alliance;
            uint rawLead = UnitCommandStateView.ToRawEntityId(lead.Id);

            model.LeadId = lead.Id;
            model.Visible = true;

            if (_runner.Construction != null
                && _runner.Construction.TryGetSite(rawLead, out ushort siteDefId, out int siteProgressRaw, out uint siteBuilderRaw))
            {
                BuildSiteModel(model, siteDefId, siteProgressRaw, siteBuilderRaw, slot, entities);
            }
            else if (SimDefinitions.IsBuildingRole(lead.Role))
            {
                BuildBuildingModel(model, in lead, rawLead, slot, faction, entities);
            }
            else
            {
                BuildUnitModel(model, faction, lead.Role, selected.Length);
            }

            if (_input.OrderPickModeActive)
            {
                model.FooterHint = "Ziel wählen — LMB bestätigen | RMB Abbruch";
            }
        }

        /// <summary>The unit card: the lead unit's role decides the buttons (armed? harvester? builder?).</summary>
        private void BuildUnitModel(CardModel model, FactionId faction, UnitRole leadRole, int selectedCount)
        {
            model.Title = CommandCardPresenter.UnitDisplayName(faction, leadRole);
            if (selectedCount > 1) model.Title += $" (+{selectedCount - 1} weitere)";

            CommandButtonType commands = _presenter.GetUnitCommands(faction, leadRole);
            if (commands.HasFlag(CommandButtonType.Move)) AddButton(model, "Bewegen (RMB)", true, CardAction.MovePick);
            if (commands.HasFlag(CommandButtonType.Stop)) AddButton(model, "Stopp (S)", true, CardAction.Stop);
            if (commands.HasFlag(CommandButtonType.Attack)) AddButton(model, "Angreifen (A)", true, CardAction.AttackPick);
            if (commands.HasFlag(CommandButtonType.Harvest)) AddButton(model, "Ernten (H)", true, CardAction.HarvestPick);
            if (commands.HasFlag(CommandButtonType.ReturnCargo)) AddButton(model, "Fracht abliefern (R)", true, CardAction.ReturnCargo);
            if (commands.HasFlag(CommandButtonType.Repair)) AddButton(model, "Reparieren", true, CardAction.RepairPick);
        }

        /// <summary>The building card: Sell/Repair for every building, production for producers, the disabled module slot on the DefensePlatform.</summary>
        private void BuildBuildingModel(
            CardModel model, in UnitState building, uint rawBuilding, byte slot, FactionId faction, EntityManager entities)
        {
            model.Title = CommandCardPresenter.BuildingDisplayName(building.Role);
            if (building.CurrentHealth < building.MaxHealth)
            {
                model.Title += $"   {building.CurrentHealth}/{building.MaxHealth} HP";
            }

            CommandButtonType commands = _presenter.GetBuildingCommands(building.Role);
            bool definitionKnown = SimDefinitions.TryGetBuilding(faction, building.Role, out SimBuildingDefinition def);

            if (commands.HasFlag(CommandButtonType.Sell))
            {
                string label = "Verkaufen";
                if (definitionKnown)
                {
                    long refund = (long)def.CostAE * ConstructionSystem.SellRefundPercent / 100;
                    label = $"Verkaufen (+{refund} AE)";
                }
                AddButton(model, label, true, CardAction.Sell);
            }

            if (commands.HasFlag(CommandButtonType.Repair))
            {
                bool builderAvailable = CommandCardPresenter.TryFindRepairBuilder(entities, slot, out _);
                BuildingRepairBlocker blocker = CommandCardPresenter.EvaluateBuildingRepairBlocker(
                    building.CurrentHealth < building.MaxHealth, builderAvailable);
                string label = "Reparieren";
                if (blocker == BuildingRepairBlocker.NotDamaged) label += "\nkeine Schäden";
                else if (blocker == BuildingRepairBlocker.NoBuilderAvailable) label += "\nkein Builder verfügbar";
                AddButton(model, label, blocker == BuildingRepairBlocker.None, CardAction.RepairBuilding);
            }

            if (commands.HasFlag(CommandButtonType.InstallDefenseModule))
            {
                // Presence without dispatch: the schema-v1 kind exists, but
                // the sim rejects it in this slice (G2/G4 content) — the
                // button is honest about WHY, and clicking it is impossible.
                AddButton(model, "Verteidigungsmodul installieren\nG2/G4-Inhalt — noch nicht verfügbar", false, CardAction.None);
            }

            if (ProducerBuildingRoles.IsProducerRole(building.Role))
            {
                BuildProductionModel(model, rawBuilding, building.Role, slot, faction);
            }
        }

        /// <summary>
        /// The construction-site card: progress with a live state line plus
        /// cancel (75% refund, the construction domain's rule). The state
        /// line names the sim's own evaluation in its own order — no living
        /// Builder (paused), Builder assigned but outside the footprint's
        /// reach (paused), or building with the ETA from BuildTicks,
        /// ProgressRaw and the owner's ProductionSpeedMultiplierQ16 — because
        /// a standing site must never look simply "broken" (D-085).
        /// </summary>
        private void BuildSiteModel(
            CardModel model, ushort siteDefId, int siteProgressRaw, uint siteBuilderRaw,
            byte slot, EntityManager entities)
        {
            if (!SimDefinitions.TryGetBuilding(siteDefId, out SimBuildingDefinition def))
            {
                model.Title = "Baustelle";
                return;
            }

            model.Title = $"Baustelle: {CommandCardPresenter.BuildingDisplayName(def.Role)}";
            model.ProgressBar01 = Mathf.Clamp01(ConstructionSiteStatus.Progress01(siteProgressRaw, def.BuildTicks));

            // The reach verdict mirrors the sim's private Chebyshev rule: the
            // site entity sits at the footprint's CENTER cell, so the origin
            // is center minus half the footprint — the same derivation the
            // AI's construction support makes.
            bool builderInReach = false;
            if (siteBuilderRaw != 0
                && entities.TryGetUnit(UnitCommandStateView.ToEntityId(siteBuilderRaw), out UnitState builder)
                && entities.TryGetUnit(model.LeadId, out UnitState siteUnit))
            {
                const int half = SimDefinitions.BuildingFootprintCells / 2;
                int originX = GridCellOf(siteUnit.Transform.PositionX) - half;
                int originY = GridCellOf(siteUnit.Transform.PositionY) - half;
                builderInReach = ConstructionSiteStatus.IsInReachOfFootprint(
                    GridCellOf(builder.Transform.PositionX), GridCellOf(builder.Transform.PositionY),
                    originX, originY, SimDefinitions.BuildingFootprintCells);
            }

            SiteBuildState state = ConstructionSiteStatus.Evaluate(siteBuilderRaw != 0, builderInReach);
            int speedRaw = _runner.Economy != null
                ? _runner.Economy.GetPlayerEconomy(slot).ProductionSpeedMultiplierQ16.RawValue
                : Nova.Core.SimFixed.One.RawValue;
            int remainingSeconds = ConstructionSiteStatus.RemainingSecondsCeil(
                siteProgressRaw, def.BuildTicks, speedRaw);
            model.SiteStatusText = ConstructionSiteStatus.StatusText(
                state, ConstructionSiteStatus.Progress01(siteProgressRaw, def.BuildTicks), remainingSeconds);

            long refund = (long)def.CostAE * ConstructionSystem.CancelRefundPercent / 100;
            AddButton(model, $"Abbruch (+{refund} AE)", true, CardAction.CancelConstruction);
        }

        /// <summary>Sim-space coordinate to grid cell at the presentation boundary: floor, clamped at 0, the same mapping the sim applies.</summary>
        private static int GridCellOf(Nova.Core.SimFixed simCoordinate)
        {
            return Mathf.Max(0, Mathf.FloorToInt(simCoordinate.ToFloat()));
        }

        /// <summary>
        /// The producer section: the live queue (head entry progresses,
        /// cancel per entry) and one production button per unit definition
        /// the building builds. Button availability mirrors
        /// <see cref="ProductionSystem.ValidateQueueUnit"/>'s check order —
        /// tier gate, queue capacity, affordability — via
        /// <see cref="CommandCardPresenter.EvaluateProductionBlocker"/>.
        /// </summary>
        private void BuildProductionModel(CardModel model, uint rawBuilding, UnitRole producerRole, byte slot, FactionId faction)
        {
            if (_runner.Production == null) return;

            int entryCount = 0;
            _runner.Production.TryGetProducer(rawBuilding, out entryCount, out _, out _);
            model.QueueHeader = $"Warteschlange ({entryCount}/{ProductionSystem.MaxQueueEntries})";

            for (int i = 0; i < entryCount; i++)
            {
                if (!_runner.Production.TryGetQueueEntry(rawBuilding, i, out ushort defId, out ushort remaining, out int progressRaw))
                {
                    continue;
                }

                string name = defId.ToString();
                float progress01 = -1f;
                if (SimDefinitions.TryGetUnit(defId, out SimUnitDefinition queueDef))
                {
                    name = CommandCardPresenter.UnitDisplayName(queueDef.Faction, queueDef.Role);
                    if (i == 0)
                    {
                        // Only the head entry progresses (production domain rule).
                        progress01 = Mathf.Clamp01(progressRaw / (float)(queueDef.BuildTicks << 16));
                    }
                }

                var row = new QueueRow { Label = $"{i + 1}. {name}", Index = i, Progress01 = progress01 };
                if (remaining > 1) row.Label += $" ×{remaining}";
                model.QueueRows.Add(row);
            }

            // A head entry clamped AT its build threshold is the sim's
            // documented silent pause (ProductionSystem.ExecuteTick): the
            // entity store is full, or the ring search found no free spawn
            // cell — the bar then reads "full" forever and nothing spawns.
            // Like the construction site before it (D-085), a stalled
            // production names its reason instead of looking simply broken.
            if (entryCount > 0
                && _runner.Production.TryGetQueueEntry(rawBuilding, 0, out ushort headDefId, out ushort headRemaining, out int headProgressRaw)
                && headRemaining > 0
                && SimDefinitions.TryGetUnit(headDefId, out SimUnitDefinition headDef)
                && headProgressRaw >= (headDef.BuildTicks << 16))
            {
                model.QueueStallText = _runner.Entities != null
                    && _runner.Entities.ActiveCount >= _runner.Entities.Capacity
                    ? "Produktion pausiert: Einheitenlimit erreicht"
                    : "Produktion pausiert: kein Platz zum Ausrücken";
            }

            long credits = _runner.Economy != null
                ? _runner.Economy.GetPlayerEconomy(slot).AetheriumCredits
                : 0L;
            bool t2Unlocked = _runner.Construction != null && _runner.Construction.IsT2Unlocked(slot);

            int producible = _presenter.GetProducibleUnits(faction, producerRole, _producibleScratch);
            for (int i = 0; i < producible; i++)
            {
                SimUnitDefinition def = _producibleScratch[i];
                ProductionBlocker blocker = CommandCardPresenter.EvaluateProductionBlocker(
                    in def, credits, t2Unlocked, entryCount);

                _builder.Clear();
                _builder.Append(CommandCardPresenter.UnitDisplayName(faction, def.Role));
                string hotkey = ProductionHotkeyHint(def.Role);
                if (hotkey.Length > 0) _builder.Append(" (").Append(hotkey).Append(')');
                _builder.Append('\n').Append(def.CostAE).Append(" AE · ").Append(def.BuildTicks / 10).Append(" s");
                string reason = BlockerReason(blocker);
                if (reason.Length > 0) _builder.Append('\n').Append(reason);

                AddButton(model, _builder.ToString(), blocker == ProductionBlocker.None, CardAction.QueueUnit, def.DefinitionId);
            }

            // The Phase-A gesture, surfaced: RMB with this producer selected
            // sets its rally point (RtsDeviceInput).
            model.FooterHint = "Rechtsklick: Sammelpunkt setzen";
        }

        // ----------------------------------------------------------------
        // Drawing
        // ----------------------------------------------------------------

        private void OnGUI()
        {
            if (_runner == null || _input == null) return;
            CardModel model = EnsureModel();
            if (!model.Visible)
            {
                _lastPanelRect = default;
                return;
            }
            EnsureStyles();

            float scale = Mathf.Max(1f, _uiScale);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            Rect rect = ComputePanelRect(model);
            _lastPanelRect = rect;

            GUILayout.BeginArea(rect);
            GUILayout.BeginVertical(HudChrome.PanelStyle);

            GUILayout.Label(model.Title, _titleStyle, GUILayout.Height(TitleHeight));
            if (model.ProgressBar01 >= 0f) DrawProgressBar(model.ProgressBar01);
            if (model.SiteStatusText != null)
            {
                GUILayout.Label(model.SiteStatusText, _siteStatusStyle, GUILayout.Height(SiteStatusHeight));
            }

            for (int i = 0; i < model.Buttons.Count; i++)
            {
                CardButton button = model.Buttons[i];
                bool wasEnabled = GUI.enabled;
                GUI.enabled = button.Enabled;
                if (GUILayout.Button(button.Label, _buttonStyle, GUILayout.Height(ButtonHeight(button.Label))))
                {
                    AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                    Dispatch(button, model);
                }
                GUI.enabled = wasEnabled;
            }

            if (model.QueueHeader != null)
            {
                GUILayout.Label(model.QueueHeader, _sectionStyle, GUILayout.Height(SectionHeight));
                for (int i = 0; i < model.QueueRows.Count; i++)
                {
                    QueueRow row = model.QueueRows[i];
                    GUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
                    GUILayout.Label(row.Label, _rowStyle, GUILayout.Height(RowHeight));
                    if (GUILayout.Button("X", _cancelStyle, GUILayout.Width(22f), GUILayout.Height(RowHeight)))
                    {
                        AudioServiceLocator.Play2D(SoundEventId.UI_Click);
                        _input.OrderCancelProduction(model.LeadId, row.Index);
                    }
                    GUILayout.EndHorizontal();
                    if (row.Progress01 >= 0f) DrawProgressBar(row.Progress01);
                }
            }

            if (model.QueueStallText != null)
            {
                GUILayout.Label(model.QueueStallText, _siteStatusStyle, GUILayout.Height(SiteStatusHeight));
            }

            if (model.FooterHint != null)
            {
                GUILayout.Label(model.FooterHint, _hintStyle, GUILayout.Height(FooterHeight));
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
        }

        /// <summary>
        /// Every dispatch flows through RtsDeviceInput's entry points — the
        /// dispatcher stays the only way into the ingress.
        /// <see cref="CardAction.None"/> (the disabled defense-module button)
        /// never dispatches: InstallDefenseModule is G2/G4 content and the
        /// sim rejects it in this slice.
        /// </summary>
        private void Dispatch(CardButton button, CardModel model)
        {
            switch (button.Action)
            {
                case CardAction.MovePick: _input.RequestMoveOrder(); break;
                case CardAction.Stop: _input.OrderStop(); break;
                case CardAction.AttackPick: _input.RequestAttackOrder(); break;
                case CardAction.HarvestPick: _input.RequestHarvestOrder(); break;
                case CardAction.ReturnCargo: _input.OrderReturnCargo(); break;
                case CardAction.RepairPick: _input.RequestRepairOrder(); break;
                case CardAction.Sell: _input.OrderSell(model.LeadId); break;
                case CardAction.RepairBuilding: _input.OrderRepairBuilding(model.LeadId); break;
                case CardAction.QueueUnit: _input.OrderQueueUnit(model.LeadId, (ushort)button.Payload); break;
                case CardAction.CancelProduction: _input.OrderCancelProduction(model.LeadId, button.Payload); break;
                case CardAction.CancelConstruction: _input.OrderCancelConstruction(model.LeadId); break;
            }
        }

        /// <summary>Bottom-right docking: the card's zone sits directly above the build bar's reserve, sharing the right margin (HudLayout owns the screen read).</summary>
        private Rect ComputePanelRect(CardModel model)
        {
            float scale = Mathf.Max(1f, _uiScale);
            float height = EstimateHeight(model);
            float barReserve = _buildMenu != null ? _buildMenu.OccupiedHeight : 0f;
            return HudLayout.CommandCardZone(scale, _panelWidth, height, _panelMargin, barReserve);
        }

        /// <summary>
        /// Panel height from the model's rows; must track the GUILayout
        /// consumption of OnGUI row for row (a clip would cut buttons off,
        /// a surplus is just empty box background). Every row costs its
        /// content height PLUS its style's vertical margin — GUILayout stacks
        /// margin boxes, it does not collapse them — and the panel style's
        /// own padding wraps the lot. (The earlier estimate ignored both and
        /// the card's bottom buttons fell outside the BeginArea, ~40 px short
        /// on a typical card: visible, but not clickable.)
        /// </summary>
        private float EstimateHeight(CardModel model)
        {
            float height = HudChrome.PanelStyle.padding.vertical;
            height += TitleHeight + _titleStyle.margin.vertical;
            if (model.ProgressBar01 >= 0f) height += ProgressHeight; // GUIStyle.none: no margin
            if (model.SiteStatusText != null) height += SiteStatusHeight + _siteStatusStyle.margin.vertical;
            for (int i = 0; i < model.Buttons.Count; i++)
            {
                height += ButtonHeight(model.Buttons[i].Label) + _buttonStyle.margin.vertical;
            }
            if (model.QueueHeader != null)
            {
                height += SectionHeight + _sectionStyle.margin.vertical;
                for (int i = 0; i < model.QueueRows.Count; i++)
                {
                    height += RowHeight; // the horizontal row group carries GUIStyle.none
                    if (model.QueueRows[i].Progress01 >= 0f) height += ProgressHeight;
                }
            }
            if (model.QueueStallText != null) height += SiteStatusHeight + _siteStatusStyle.margin.vertical;
            if (model.FooterHint != null) height += FooterHeight + _hintStyle.margin.vertical;
            return height;
        }

        /// <summary>Buttons wrap to their line count (name / cost / reason) — the card keeps its reasons on the button (D-084); only the build bar moved them to the status line.</summary>
        private static float ButtonHeight(string label)
        {
            int lines = 1;
            for (int i = 0; i < label.Length; i++)
            {
                if (label[i] == '\n') lines++;
            }
            return 10f + lines * 13f;
        }

        private void DrawProgressBar(float fraction01)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, ProgressHeight, GUILayout.ExpandWidth(true));
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(rect, HudChrome.Pixel);
            float fill = Mathf.Clamp01(fraction01);
            if (fill > 0f)
            {
                GUI.color = new Color(0.35f, 0.9f, 0.45f, 0.9f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fill, rect.height), HudChrome.Pixel);
            }
            GUI.color = previousColor;
        }

        private void AddButton(CardModel model, string label, bool enabled, CardAction action, int payload = 0)
        {
            model.Buttons.Add(new CardButton { Label = label, Enabled = enabled, Action = action, Payload = payload });
        }

        /// <summary>German blocker reasons, matching the build bar's wording ("benötigt X", "nicht genug Aetherium").</summary>
        private static string BlockerReason(ProductionBlocker blocker)
        {
            switch (blocker)
            {
                // The T2 unlock IS a completed ResearchLab (MS-1 technology model).
                case ProductionBlocker.TierLocked: return "benötigt Forschungslabor";
                case ProductionBlocker.QueueFull: return "Warteschlange voll";
                case ProductionBlocker.InsufficientCredits: return "nicht genug Aetherium";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// The hotkey bound to a unit role in RtsDeviceInput (the same key
        /// queues at this building while it is selected), or empty.
        /// </summary>
        private static string ProductionHotkeyHint(UnitRole role)
        {
            switch (role)
            {
                case UnitRole.Harvester: return "Q";
                case UnitRole.BasicInfantry: return "Shift+Q";
                case UnitRole.Builder: return "U";
                case UnitRole.AntiArmorInfantry: return "N";
                case UnitRole.ScoutVehicle: return "E";
                case UnitRole.LightTank: return "Shift+E";
                case UnitRole.BattleTank: return "D";
                case UnitRole.Artillery: return "Shift+D";
                default: return string.Empty;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, wordWrap = false };
            }
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 11, wordWrap = true };
            }
            if (_sectionStyle == null)
            {
                _sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, wordWrap = false };
            }
            if (_rowStyle == null)
            {
                _rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = false };
            }
            if (_siteStatusStyle == null)
            {
                // Wraps: the no-Builder warning is longer than the panel is
                // wide and must read in full, never clip (SiteStatusHeight
                // reserves the two lines it needs).
                _siteStatusStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true };
            }
            if (_cancelStyle == null)
            {
                _cancelStyle = new GUIStyle(GUI.skin.button) { fontSize = 10 };
            }
            if (_hintStyle == null)
            {
                _hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Italic, wordWrap = false };
            }
        }

        private void OnDestroy()
        {
            HudChrome.DestroyShared();
        }
    }
}
