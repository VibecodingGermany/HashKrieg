// Graybox throwaway. Legacy Input + OnGUI. Does NOT satisfy G2/G4 UI criteria
// (see docs/production/MVPRecoveryPlan.md). Replaced when the new Input System and the real UI land.
using System;
using UnityEngine;
using Nova.Gameplay;
using Nova.Gameplay.Match;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.State;
using EntityId = Nova.Core.EntityId;

namespace Nova.Presentation.UI
{
    /// <summary>
    /// Device input of the graybox slice: legacy mouse/keyboard in, command
    /// intents out. Runs at -200 so every enqueue lands BEFORE MatchRunner
    /// steps the tick — without a fixed order what you measure is frame
    /// ordering noise instead of the real ~100-200 ms of the 10 Hz input-delay
    /// pipeline.
    /// <para>
    /// This component never mutates simulation state. Selection is UI state
    /// (<see cref="SelectionManager"/>); every order goes through
    /// <see cref="RtsIntentDispatcher"/>, which is the only caller of
    /// <c>MatchRunner.Ingress.TrySubmitIntent</c> here.
    /// </para>
    /// <para>
    /// Orders are addressed to MOBILE entities only
    /// (<see cref="SelectionManager.CopyMobileSelection"/>): buildings are
    /// immobile, so Move/Stop/Attack/Harvest/ReturnCargo addressed to them
    /// are meaningless — and a building that received a move order was the
    /// source of the "buildings rotate when right-clicked" bug. The one
    /// exception is the right-click with an own PRODUCER building as the
    /// lead selection: that sets the building's rally point
    /// (<see cref="RtsIntentDispatcher.SetRallyPoint"/>) instead of moving.
    /// </para>
    /// <para>
    /// Placement mode: the build bar and the building hotkeys no longer
    /// place instantly — they arm a placement ghost
    /// (<see cref="EnterPlacementMode"/>) that follows the cursor, LMB
    /// dispatches <see cref="RtsIntentDispatcher.PlaceBuilding"/> at the
    /// hovered footprint origin, RMB/ESC cancels, and another building
    /// hotkey re-targets the ghost. While armed, the LMB/drag selection
    /// path and every other order are suspended: the selection must not
    /// change mid-placement. The ghost's green/red verdict
    /// (<see cref="PlacementValid"/>) evaluates the SAME
    /// <see cref="ConstructionSystem.ValidatePlacement"/> the executor runs,
    /// plus the affordability check the executor runs separately, so green
    /// means "this click will be accepted".
    /// </para>
    /// <para>
    /// ORDER TARGET-PICK MODE (command card, GB-006): the card's Move,
    /// Attack, Harvest and Repair buttons do not dispatch immediately —
    /// they ARM a pick (<see cref="RequestMoveOrder"/> and siblings) and
    /// the NEXT LMB world click resolves the order at that point through
    /// the exact resolution paths the hotkeys use
    /// (<see cref="ResolveAttackAt"/> is the A key, verbatim). RMB/ESC
    /// cancels. Pick mode and placement mode are mutually exclusive:
    /// entering one disarms the other, and while either is armed the
    /// selection must not change. Repair additionally filters the selection
    /// down to its BUILDER-role entities — the executor rejects the whole
    /// repair command when any actor is not a Builder
    /// (<see cref="ConstructionSystem.ValidateRepair"/>). Because the sim's
    /// repair order is a standing order with NO movement coupling (held,
    /// never dropped, while the Builder is out of reach), a repair pick
    /// dispatches TWO intents — Move toward the target, then Repair — so
    /// the Builder actually walks into reach.
    /// </para>
    /// <para>
    /// World mapping (identical to UnitViewManager / FlowFieldDebugView /
    /// RtsCameraController): sim X -&gt; Unity x, sim Y -&gt; Unity z, ground
    /// plane at y = <see cref="_groundPlaneY"/> (0). Cell (gx, gy) covers the
    /// world square [gx, gx+1) x [gy, gy+1).
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    public sealed class RtsDeviceInput : MonoBehaviour
    {
        [Header("Wiring (scene generator)")]
        [SerializeField] private MatchRunner _runner;
        [Tooltip("Camera used for screen->ground projection. Falls back to Camera.main.")]
        [SerializeField] private Camera _camera;
        [Tooltip("Build bar; its screen rect suppresses selection drags and placement clicks that land on the HUD.")]
        [SerializeField] private BuildMenuHud _buildMenu;
        [Tooltip("Command card; its panel rect suppresses selection drags and order-pick clicks that land on the HUD.")]
        [SerializeField] private CommandCardHud _commandCard;
        [Tooltip("Minimap; its map rect suppresses selection drags and order-pick clicks that land on the HUD (they are camera jumps).")]
        [SerializeField] private MinimapHud _minimap;

        [Header("Graybox tuning")]
        [Tooltip("Ground plane height. Picking projects onto this mathematical plane, not onto view colliders, so the per-role view heights do not matter.")]
        [SerializeField] private float _groundPlaneY = 0f;
        [Tooltip("Pixels the mouse must travel before a click becomes a box drag.")]
        [SerializeField] private float _dragThresholdPixels = 8f;
        [Tooltip("Click-select radius in world units (= cells).")]
        [SerializeField] private float _pickRadiusWorld = 1.5f;

        [Header("Definition ids (verified against SimDefinitions; local slot 0 plays Alliance)")]
        [Tooltip("B: Power — Alliance defId 5, 450 AE, prerequisite-free.")]
        [SerializeField] private ushort _buildingDefId = 5;
        [Tooltip("Shift+B: Barracks — Alliance defId 7, 500 AE, prerequisite-free.")]
        [SerializeField] private ushort _altBuildingDefId = 7;
        [Tooltip("Q: Harvester — Alliance defId 2, tier 1, produced by the HQ that exists from tick 0.")]
        [SerializeField] private ushort _unitDefId = 2;
        [Tooltip("Shift+Q: BasicInfantry — Alliance defId 12, tier 1, produced by a Barracks you build with Shift+B.")]
        [SerializeField] private ushort _altUnitDefId = 12;

        [Header("Remaining MS-1 roles (added GB-004 so the full roster is reachable)")]
        [Tooltip("C: Storage — Alliance defId 6.")]
        [SerializeField] private ushort _storageDefId = 6;
        [Tooltip("V: VehicleFactory — Alliance defId 8, produces vehicles.")]
        [SerializeField] private ushort _vehicleFactoryDefId = 8;
        [Tooltip("T: ResearchLab — Alliance defId 9, unlocks tier 2.")]
        [SerializeField] private ushort _researchLabDefId = 9;
        [Tooltip("G: Radar — Alliance defId 10.")]
        [SerializeField] private ushort _radarDefId = 10;
        [Tooltip("F: DefensePlatform — Alliance defId 11, the only armed building.")]
        [SerializeField] private ushort _defensePlatformDefId = 11;
        [Tooltip("Y: Refinery — Alliance defId 4, for expansion fields. HQ is deliberately unbound: MS-1 builds it only at match start.")]
        [SerializeField] private ushort _refineryDefId = 4;
        [Tooltip("U: Builder — Alliance defId 1, produced by the HQ.")]
        [SerializeField] private ushort _builderDefId = 1;
        [Tooltip("N: AntiArmorInfantry — Alliance defId 13, tier 2, from a Barracks.")]
        [SerializeField] private ushort _antiArmorDefId = 13;
        [Tooltip("E: ScoutVehicle — Alliance defId 14, from a VehicleFactory.")]
        [SerializeField] private ushort _scoutDefId = 14;
        [Tooltip("Shift+E: LightTank — Alliance defId 15.")]
        [SerializeField] private ushort _lightTankDefId = 15;
        [Tooltip("D: BattleTank — Alliance defId 16, tier 2.")]
        [SerializeField] private ushort _battleTankDefId = 16;
        [Tooltip("Shift+D: Artillery — Alliance defId 17, tier 2.")]
        [SerializeField] private ushort _artilleryDefId = 17;

        /// <summary>The order a command-card button armed; its target is the next LMB world click.</summary>
        private enum PendingOrder
        {
            None,
            Move,
            Attack,
            Harvest,
            Repair,
        }

        private readonly SelectionManager _selection = new SelectionManager();
        private RtsIntentDispatcher _dispatcher;
        private CommandIngress _boundIngress;

        private Vector2 _dragStart;
        private bool _dragActive;
        private bool _dragPastThreshold;
        private Texture2D _pixel;
        private string _legend = string.Empty;
        private string _lastCommandStatus = "no command yet";

        // Placement mode state. The hovered footprint origin and its validity
        // verdict are refreshed every frame by UpdatePlacementHover and read
        // back by PlacementGhostView — one computation, one drawer.
        private bool _placementMode;
        private ushort _placementDefId;
        private bool _placementHasCell;
        private int _placementOriginX;
        private int _placementOriginY;
        private bool _placementValid;

        // Order target-pick mode state (command card): the armed order whose
        // target the next LMB world click resolves. Mutually exclusive with
        // placement mode (one gesture owns the next click).
        private PendingOrder _pendingOrder;

        // Reusable buffer for the mobile-only view of the selection, so order
        // dispatch stays allocation-free.
        private readonly EntityId[] _mobileScratch = new EntityId[SelectionManager.MaxSelectedEntities];

        // Reusable buffer for the Builder-only view of the selection (the
        // repair command's actors; the executor rejects the whole command on
        // a non-Builder actor).
        private readonly EntityId[] _builderScratch = new EntityId[SelectionManager.MaxSelectedEntities];

        /// <summary>Selected unit count (read by <see cref="DebugHud"/>).</summary>
        public int SelectionCount => _selection.SelectedCount;

        /// <summary>
        /// The live selection, for read-only observers such as
        /// <see cref="DebugHud"/>, which resolves the lead unit's combat
        /// profile from it. Exposing the manager rather than a copy keeps the
        /// per-frame path allocation-free; callers must treat it as read-only
        /// (selection is owned by this component and mutated only here).
        /// </summary>
        public SelectionManager Selection => _selection;

        /// <summary>One-line control legend; single source of truth for the HUD.</summary>
        public string ControlLegend => _legend;

        /// <summary>Verdict of the last dispatched command, for the HUD.</summary>
        public string LastCommandStatus => _lastCommandStatus;

        /// <summary>False while no runner/ingress is bound — the HUD says so instead of failing silently.</summary>
        public bool IsWired => _runner != null && _dispatcher != null;

        /// <summary>True while a building placement ghost is armed (build bar click or building hotkey).</summary>
        public bool PlacementModeActive => _placementMode;

        /// <summary>Definition the armed placement ghost currently carries.</summary>
        public ushort PlacementDefId => _placementDefId;

        /// <summary>
        /// True when placing at the hovered cell would be accepted — the
        /// executor's own <see cref="ConstructionSystem.ValidatePlacement"/>
        /// plus the affordability check the executor runs separately. Read by
        /// <see cref="PlacementGhostView"/> for the green/red verdict.
        /// </summary>
        public bool PlacementValid => _placementValid;

        /// <summary>Programmatic wiring for bootstrap code that builds the scene at runtime.</summary>
        public void Bind(MatchRunner runner, Camera cam = null)
        {
            _runner = runner;
            if (cam != null) _camera = cam;
        }

        /// <summary>
        /// Enters (or re-targets) placement mode for a building definition.
        /// Called by the build bar buttons and by the building hotkeys — both
        /// flows share everything from here on (ghost, LMB place, RMB/ESC
        /// cancel).
        /// </summary>
        public void EnterPlacementMode(ushort defId)
        {
            _placementMode = true;
            _placementDefId = defId;
            _pendingOrder = PendingOrder.None; // placement and order pick are mutually exclusive
            _dragActive = false; // an in-flight drag must not complete into a re-select
            _lastCommandStatus = SimDefinitions.TryGetBuilding(defId, out SimBuildingDefinition def)
                ? $"Placement: {def.Role} ({def.CostAE} AE) — LMB place | RMB/ESC cancel"
                : $"Placement: definition {defId} — LMB place | RMB/ESC cancel";
        }

        /// <summary>
        /// The hovered footprint origin cell (lower-left) while placement
        /// mode is armed and the cursor projects onto the ground plane.
        /// </summary>
        public bool TryGetPlacementCell(out int originX, out int originY)
        {
            originX = _placementOriginX;
            originY = _placementOriginY;
            return _placementMode && _placementHasCell;
        }

        // ----------------------------------------------------------------
        // Command-card entry points (GB-006). The card owns no dispatcher:
        // every order a button can trigger flows through these methods, the
        // same paths the hotkeys use.
        // ----------------------------------------------------------------

        /// <summary>True while a command-card order is armed and waits for its target click.</summary>
        public bool OrderPickModeActive => _pendingOrder != PendingOrder.None;

        /// <summary>Arms the Move pick: the next LMB world click moves the selection there (RMB/ESC cancels).</summary>
        public void RequestMoveOrder()
        {
            if (!EnsureDispatcher()) return;
            ArmOrderPick(PendingOrder.Move, "Move");
        }

        /// <summary>Arms the Attack pick: the next LMB resolves exactly like the A key at that point.</summary>
        public void RequestAttackOrder()
        {
            if (!EnsureDispatcher()) return;
            ArmOrderPick(PendingOrder.Attack, "Attack");
        }

        /// <summary>Arms the Harvest pick: the next LMB resolves exactly like the H key at that point.</summary>
        public void RequestHarvestOrder()
        {
            if (!EnsureDispatcher()) return;
            ArmOrderPick(PendingOrder.Harvest, "Harvest");
        }

        /// <summary>
        /// Arms the Repair pick. Refused when the selection holds no Builder:
        /// the executor rejects the whole repair command on a non-Builder
        /// actor, so arming anyway would arm a gesture that can only fail.
        /// </summary>
        public void RequestRepairOrder()
        {
            if (!EnsureDispatcher()) return;
            BuilderSelection(out int builderCount);
            if (builderCount == 0)
            {
                _lastCommandStatus = "Repair: no Builder in the selection";
                return;
            }
            ArmOrderPick(PendingOrder.Repair, "Repair");
        }

        /// <summary>Stop for the mobile selection — the S key and the card button share this path.</summary>
        public void OrderStop()
        {
            if (!EnsureDispatcher()) return;
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0) _lastCommandStatus = "Stop: no mobile units in the selection";
            else Report("Stop", _dispatcher.Stop(mobile));
        }

        /// <summary>ReturnCargo for the mobile selection — the R key and the card button share this path.</summary>
        public void OrderReturnCargo()
        {
            if (!EnsureDispatcher()) return;
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0) _lastCommandStatus = "ReturnCargo: no mobile units in the selection";
            else Report("ReturnCargo", _dispatcher.ReturnCargo(mobile));
        }

        /// <summary>Sells a COMPLETED own building (50% refund, floor — the construction domain's rule).</summary>
        public void OrderSell(EntityId building)
        {
            if (!EnsureDispatcher()) return;
            Report("Sell", _dispatcher.Sell(building));
        }

        /// <summary>Cancels an own construction site (75% refund, floor — the construction domain's rule).</summary>
        public void OrderCancelConstruction(EntityId site)
        {
            if (!EnsureDispatcher()) return;
            Report("CancelConstruction", _dispatcher.CancelConstruction(site));
        }

        /// <summary>Queues one unit at a producer building (full cost charged at enqueue by the executor).</summary>
        public void OrderQueueUnit(EntityId building, ushort unitDefId)
        {
            if (!EnsureDispatcher()) return;
            Report($"QueueUnit {unitDefId}", _dispatcher.QueueUnit(building, unitDefId, 1));
        }

        /// <summary>Cancels one queue entry of a producer building (its remaining count is refunded in full).</summary>
        public void OrderCancelProduction(EntityId building, int queueIndex)
        {
            if (!EnsureDispatcher()) return;
            if (queueIndex < 0 || queueIndex > ushort.MaxValue) return;
            Report($"CancelProduction {queueIndex}", _dispatcher.CancelProduction(building, (ushort)queueIndex));
        }

        /// <summary>
        /// The building-side repair button: no Builder is selected, so the
        /// actor is the lowest-index living own Builder — the sim's own
        /// auto-assignment convention for sites, rebuilt in
        /// <see cref="CommandCardPresenter.TryFindRepairBuilder"/>. Dispatches
        /// Move + Repair, because the standing repair order has no movement
        /// coupling (see <see cref="DispatchMoveAndRepair"/>).
        /// </summary>
        public void OrderRepairBuilding(EntityId building)
        {
            if (!EnsureDispatcher()) return;
            if (!CommandCardPresenter.TryFindRepairBuilder(_runner.Entities, _dispatcher.LocalSlot, out EntityId builder))
            {
                _lastCommandStatus = "Repair: no own Builder available";
                return;
            }
            if (!_runner.Entities.TryGetUnit(building, out UnitState target))
            {
                _lastCommandStatus = "Repair: stale building handle";
                return;
            }

            _builderScratch[0] = builder;
            var approach = new Vector3(
                target.Transform.PositionX.ToFloat(), _groundPlaneY, target.Transform.PositionY.ToFloat());
            DispatchMoveAndRepair(_builderScratch.AsSpan(0, 1), approach, building);
        }

        /// <summary>
        /// Arms (or re-arms) an order pick. Pick mode and placement mode are
        /// mutually exclusive — exactly one gesture owns the next LMB click —
        /// so arming one disarms the other.
        /// </summary>
        private void ArmOrderPick(PendingOrder order, string label)
        {
            _placementMode = false;
            _pendingOrder = order;
            _dragActive = false; // an in-flight drag must not complete into a re-select
            _lastCommandStatus = $"{label}: pick a target — LMB confirm | RMB/ESC cancel";
        }

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_camera == null) _camera = Camera.main;
            if (_commandCard == null) _commandCard = FindAnyObjectByType<CommandCardHud>();
            if (_minimap == null) _minimap = FindAnyObjectByType<MinimapHud>();
            _legend =
                "LMB click/drag select | RMB move — with an own producer building selected: set its rally point | S stop | " +
                "A attack enemy under cursor (else plain move — no auto-acquire yet) | " +
                "H harvest nearest field | R return cargo | P pause/resume\n" +
                "Build (build bar below or hotkey — a ghost follows the cursor; LMB place | RMB/ESC cancel): " +
                $"B {_buildingDefId} | Shift+B {_altBuildingDefId} | C {_storageDefId} | V {_vehicleFactoryDefId} | " +
                $"T {_researchLabDefId} | G {_radarDefId} | F {_defensePlatformDefId} | Y {_refineryDefId}\n" +
                $"Units: Q {_unitDefId} | Shift+Q {_altUnitDefId} | U {_builderDefId} | N {_antiArmorDefId} | " +
                $"E {_scoutDefId} | Shift+E {_lightTankDefId} | D {_battleTankDefId} | Shift+D {_artilleryDefId}\n" +
                "Command card (bottom right): LMB an order button, then LMB its target in the world (RMB/ESC cancels the pick)\n" +
                "Camera: arrow keys / screen edge pan | wheel zoom | Z,X rotate | MMB drag rotate | Space reset rotation";
        }

        private void Update()
        {
            if (!EnsureDispatcher()) return;

            Vector2 mouse = Input.mousePosition;
            UpdatePlacementHover(mouse);
            HandleSelection(mouse);
            HandleOrders(mouse);
        }

        /// <summary>
        /// Binds (or rebinds) the dispatcher to the runner's ingress. The
        /// ingress only exists after MatchRunner.InitializeMatch, which the
        /// bootstrap calls in Start() — i.e. AFTER this component's Start(),
        /// because of the -200 execution order. So the binding is lazy and
        /// re-runs when a new match replaces the ingress instance.
        /// </summary>
        private bool EnsureDispatcher()
        {
            if (_runner == null) return false;

            CommandIngress ingress = _runner.Ingress;
            if (ingress == null) return false;
            if (_dispatcher != null && ReferenceEquals(ingress, _boundIngress)) return true;

            // The state view drops stale/foreign handles before chunking: the
            // executor rejects a WHOLE command on the first bad id, so one dead
            // unit in the selection would otherwise cancel the entire order.
            var stateView = new UnitCommandStateView(
                _runner.Entities, _runner.Pathfinding, _runner.Economy,
                _runner.Construction, _runner.Production);
            _dispatcher = new RtsIntentDispatcher(ingress, stateView);
            _boundIngress = ingress;
            _selection.ClearSelection();
            return true;
        }

        // ----------------------------------------------------------------
        // Placement mode
        // ----------------------------------------------------------------

        /// <summary>
        /// Refreshes the cell the placement ghost sits on and whether placing
        /// there would succeed. The footprint CENTRES on the cursor cell, so
        /// what is under the mouse is what gets covered. The verdict combines
        /// the executor's validator (footprint inside the map and free,
        /// faction, prerequisite, power, site capacity) with the credit check
        /// the executor runs before it — a green ghost means "this LMB click
        /// will be accepted", never just "the cells are free".
        /// </summary>
        private void UpdatePlacementHover(Vector2 mouse)
        {
            _placementHasCell = false;
            _placementValid = false;
            if (!_placementMode || _runner.Construction == null || _runner.Economy == null) return;
            if (!TryScreenPointToGround(mouse, out Vector3 at)) return;

            const int half = SimDefinitions.BuildingFootprintCells / 2;
            _placementOriginX = Mathf.FloorToInt(at.x) - half;
            _placementOriginY = Mathf.FloorToInt(at.z) - half;
            _placementHasCell = true;

            byte slot = _dispatcher.LocalSlot;
            bool affordable = SimDefinitions.TryGetBuilding(_placementDefId, out SimBuildingDefinition def)
                && _runner.Economy.GetPlayerEconomy(slot).AetheriumCredits >= def.CostAE;
            _placementValid = affordable
                && _runner.Construction.ValidatePlacement(
                    slot, _placementDefId, _placementOriginX, _placementOriginY) == CommandResultCode.Applied;
        }

        /// <summary>
        /// Input flow while the ghost is armed: LMB places (through the
        /// dispatcher, at the hovered footprint origin), RMB or ESC cancels,
        /// the building hotkeys RE-TARGET the ghost to another role. All
        /// other orders wait until the placement resolves.
        /// </summary>
        private void HandlePlacementModeInput(Vector2 mouse, bool shift)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                _placementMode = false;
                _lastCommandStatus = "Placement cancelled";
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // A click on the HUD belongs to its buttons (OnGUI), which
                // re-target the ghost — never place behind the HUD.
                if (IsPointerOverHud(mouse)) return;
                if (_placementHasCell)
                {
                    Report($"PlaceBuilding {_placementDefId}",
                        _dispatcher.PlaceBuilding(_placementDefId,
                            ToGridCoordinate(_placementOriginX), ToGridCoordinate(_placementOriginY)));
                }
                _placementMode = false;
                return;
            }

            HandleBuildingHotkeys(shift);
        }

        // ----------------------------------------------------------------
        // Selection & orders
        // ----------------------------------------------------------------

        private void HandleSelection(Vector2 mouse)
        {
            // Placement and order-pick mode own the LMB entirely: no drag, no
            // re-select — the selection must not change while a gesture is armed.
            if (_placementMode || _pendingOrder != PendingOrder.None)
            {
                _dragActive = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                // A press that lands on the HUD (build bar or command card)
                // belongs to its buttons, not to world selection.
                if (IsPointerOverHud(mouse))
                {
                    _dragActive = false;
                    return;
                }
                _dragStart = mouse;
                _dragActive = true;
                _dragPastThreshold = false;
            }
            else if (_dragActive && !_dragPastThreshold && Input.GetMouseButton(0)
                     && (mouse - _dragStart).sqrMagnitude >= _dragThresholdPixels * _dragThresholdPixels)
            {
                _dragPastThreshold = true;
            }

            if (!_dragActive || !Input.GetMouseButtonUp(0)) return;
            _dragActive = false;
            if (_dragPastThreshold) SelectBox(_dragStart, mouse);
            else SelectSingle(mouse);
        }

        private void HandleOrders(Vector2 mouse)
        {
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (_placementMode)
            {
                HandlePlacementModeInput(mouse, shift);
                return;
            }

            if (_pendingOrder != PendingOrder.None)
            {
                HandleOrderPickInput(mouse);
                return;
            }

            if (Input.GetMouseButtonDown(1) && TryScreenPointToGround(mouse, out Vector3 moveTo))
            {
                // Right-click with an own producer building as the lead
                // selection sets that building's rally point — the building
                // cannot move, so this is the only useful reading of the
                // gesture for it.
                if (TryGetLeadProducer(out EntityId producer))
                {
                    Report("RallyPoint", _dispatcher.SetRallyPoint(producer, moveTo.x, moveTo.z));
                }
                else
                {
                    ResolveMoveAt(moveTo);
                }
            }

            if (Input.GetKeyDown(KeyCode.S)) OrderStop();

            if (Input.GetKeyDown(KeyCode.A) && TryScreenPointToGround(mouse, out Vector3 attackAt))
            {
                ResolveAttackAt(attackAt);
            }

            if (Input.GetKeyDown(KeyCode.H) && TryScreenPointToGround(mouse, out Vector3 harvestAt))
            {
                ResolveHarvestAt(harvestAt);
            }

            if (Input.GetKeyDown(KeyCode.R)) OrderReturnCargo();

            // Pause/resume toggles the kernel clock only — simulation state is
            // untouched, so this needs no command. StartMatch after PauseMatch
            // simply restarts the tick pump.
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (_runner.IsRunning)
                {
                    _runner.PauseMatch();
                    _lastCommandStatus = "Match paused (P resumes)";
                }
                else
                {
                    _runner.StartMatch();
                    _lastCommandStatus = "Match resumed";
                }
            }

            // Buildings: one key per MS-1 role ENTERS placement mode (the
            // ghost then follows the cursor); the early graybox's instant
            // placement at the cursor is gone. HQ is deliberately unbound
            // (MS-1 builds it only at match start).
            HandleBuildingHotkeys(shift);

            // Units: one key per MS-1 role; the producer resolves by the
            // definition's producer role (selected own building first).
            if (Input.GetKeyDown(KeyCode.Q)) TryQueueUnit(shift ? _altUnitDefId : _unitDefId);
            if (Input.GetKeyDown(KeyCode.U)) TryQueueUnit(_builderDefId);
            if (Input.GetKeyDown(KeyCode.N)) TryQueueUnit(_antiArmorDefId);
            if (Input.GetKeyDown(KeyCode.E)) TryQueueUnit(shift ? _lightTankDefId : _scoutDefId);
            if (Input.GetKeyDown(KeyCode.D)) TryQueueUnit(shift ? _artilleryDefId : _battleTankDefId);
        }

        /// <summary>The building hotkeys enter (or re-target) placement mode; shared by the normal and the placement input flow.</summary>
        private void HandleBuildingHotkeys(bool shift)
        {
            if (Input.GetKeyDown(KeyCode.B)) EnterPlacementMode(shift ? _altBuildingDefId : _buildingDefId);
            if (Input.GetKeyDown(KeyCode.C)) EnterPlacementMode(_storageDefId);
            if (Input.GetKeyDown(KeyCode.V)) EnterPlacementMode(_vehicleFactoryDefId);
            if (Input.GetKeyDown(KeyCode.T)) EnterPlacementMode(_researchLabDefId);
            if (Input.GetKeyDown(KeyCode.G)) EnterPlacementMode(_radarDefId);
            if (Input.GetKeyDown(KeyCode.F)) EnterPlacementMode(_defensePlatformDefId);
            if (Input.GetKeyDown(KeyCode.Y)) EnterPlacementMode(_refineryDefId);
        }

        private void TryQueueUnit(ushort defId)
        {
            if (TryResolveProducer(defId, out EntityId producer))
            {
                Report($"QueueUnit {defId}", _dispatcher.QueueUnit(producer, defId, 1));
            }
            else
            {
                _lastCommandStatus = $"QueueUnit {defId}: no own producer building for that definition";
            }
        }

        /// <summary>
        /// The selection minus building-role entities — buildings are
        /// immobile, so Move/Stop/Attack/Harvest/ReturnCargo addressed to
        /// them are meaningless — and minus stale ids. Backed by a reusable
        /// scratch buffer, so the per-order path stays allocation-free.
        /// </summary>
        private ReadOnlySpan<EntityId> MobileSelection(out int count)
        {
            count = _selection.CopyMobileSelection(_runner.Entities, _mobileScratch);
            return _mobileScratch.AsSpan(0, count);
        }

        /// <summary>
        /// The selected BUILDER-role entities — the repair command's actors.
        /// The executor rejects the WHOLE repair command when any actor is
        /// not a Builder (<see cref="ConstructionSystem.ValidateRepair"/>),
        /// so a mixed selection is filtered down to its Builders here, the
        /// same discipline as <see cref="MobileSelection"/>. Backed by a
        /// reusable scratch buffer; the result aliases it.
        /// </summary>
        private ReadOnlySpan<EntityId> BuilderSelection(out int count)
        {
            count = 0;
            EntityManager entities = _runner.Entities;
            if (entities == null) return _builderScratch.AsSpan(0, 0);

            ReadOnlySpan<EntityId> selected = _selection.SelectedEntities;
            for (int i = 0; i < selected.Length && count < _builderScratch.Length; i++)
            {
                if (!entities.TryGetUnit(selected[i], out UnitState unit)) continue;
                if (unit.Role != UnitRole.Builder || unit.PlayerId != _dispatcher.LocalSlot) continue;
                _builderScratch[count++] = selected[i];
            }
            return _builderScratch.AsSpan(0, count);
        }

        /// <summary>
        /// The lead selected entity when it is an OWN producer building (per
        /// <see cref="ProducerBuildingRoles"/>, the input layer's restatement
        /// of the sim's producer-role rule over the same definition table).
        /// </summary>
        private bool TryGetLeadProducer(out EntityId producer)
        {
            producer = EntityId.Invalid;
            if (_runner.Entities == null) return false;
            ReadOnlySpan<EntityId> selected = _selection.SelectedEntities;
            if (selected.Length == 0) return false;
            if (!_runner.Entities.TryGetUnit(selected[0], out UnitState lead)) return false;
            if (lead.PlayerId != _dispatcher.LocalSlot) return false;
            if (!ProducerBuildingRoles.IsProducerRole(lead.Role)) return false;
            producer = lead.Id;
            return true;
        }

        /// <summary>True when the raw mouse position is over the build bar — those clicks belong to the HUD, not the world.</summary>
        private bool IsPointerOverBuildMenu(Vector2 mouse)
        {
            return _buildMenu != null && _buildMenu.IsPointerOverBar(mouse);
        }

        /// <summary>True when the raw mouse position is over ANY HUD panel (build bar, command card or minimap) — those clicks belong to the HUD, not the world.</summary>
        private bool IsPointerOverHud(Vector2 mouse)
        {
            return IsPointerOverBuildMenu(mouse)
                || (_commandCard != null && _commandCard.IsPointerOverPanel(mouse))
                || (_minimap != null && _minimap.IsPointerOverMinimap(mouse));
        }

        // ----------------------------------------------------------------
        // Order target-pick mode (command card)
        // ----------------------------------------------------------------

        /// <summary>
        /// Input flow while an order pick is armed: LMB resolves the order at
        /// the clicked ground point through the SAME resolution path the
        /// matching hotkey uses, RMB or ESC cancels. A repair click on an
        /// invalid target keeps the mode armed (the status names the problem)
        /// — cancelling is what RMB/ESC is for, misclicks are free.
        /// </summary>
        private void HandleOrderPickInput(Vector2 mouse)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
            {
                _pendingOrder = PendingOrder.None;
                _lastCommandStatus = "Order pick cancelled";
                return;
            }

            if (!Input.GetMouseButtonDown(0)) return;
            // A click on the HUD belongs to its buttons (OnGUI), which re-arm
            // or dispatch — never resolve the pick behind the panel.
            if (IsPointerOverHud(mouse)) return;
            if (!TryScreenPointToGround(mouse, out Vector3 at)) return;

            switch (_pendingOrder)
            {
                case PendingOrder.Move:
                    ResolveMoveAt(at);
                    _pendingOrder = PendingOrder.None;
                    break;
                case PendingOrder.Attack:
                    ResolveAttackAt(at);
                    _pendingOrder = PendingOrder.None;
                    break;
                case PendingOrder.Harvest:
                    ResolveHarvestAt(at);
                    _pendingOrder = PendingOrder.None;
                    break;
                case PendingOrder.Repair:
                    if (TryResolveRepairAt(at)) _pendingOrder = PendingOrder.None;
                    break;
            }
        }

        /// <summary>The plain move order; shared by RMB and the card's Move pick.</summary>
        private void ResolveMoveAt(Vector3 moveTo)
        {
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0)
            {
                _lastCommandStatus = "Move: no mobile units in the selection (buildings cannot move)";
            }
            else
            {
                Report("Move", _dispatcher.MoveTo(mobile, moveTo.x, moveTo.z));
            }
        }

        /// <summary>
        /// The attack gesture; shared by the A key and the card's Attack pick.
        /// Schema v1 has no attack-move register entry (see
        /// RtsIntentDispatcher.Attack): an enemy under the cursor becomes a
        /// real AttackTarget, everything else is a plain Move — Combat does
        /// NOT acquire targets on arrival yet (known gap, GB-002), so this is
        /// deliberately not labelled attack-move.
        /// </summary>
        private void ResolveAttackAt(Vector3 attackAt)
        {
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0)
            {
                _lastCommandStatus = "Attack: no mobile units in the selection";
            }
            else if (TryPickUnit(attackAt, ownedByLocalSlot: false, out EntityId enemy))
            {
                Report("Attack", _dispatcher.Attack(mobile, enemy));
            }
            else
            {
                Report("Move (no target)", _dispatcher.MoveTo(mobile, attackAt.x, attackAt.z));
            }
        }

        /// <summary>The harvest gesture; shared by the H key and the card's Harvest pick.</summary>
        private void ResolveHarvestAt(Vector3 harvestAt)
        {
            ReadOnlySpan<EntityId> mobile = MobileSelection(out int mobileCount);
            if (mobileCount == 0)
            {
                _lastCommandStatus = "Harvest: no mobile units in the selection";
            }
            else if (TryResolveNearestField(harvestAt, out ushort fieldId))
            {
                Report($"Harvest #{fieldId}", _dispatcher.Harvest(mobile, fieldId));
            }
            else
            {
                _lastCommandStatus = "Harvest: no unexhausted Aetherium field registered";
            }
        }

        /// <summary>
        /// The repair pick target: an own COMPLETED placement that is damaged
        /// — the sim's own target rule (<see cref="ConstructionSystem.ValidateRepair"/>)
        /// restated at the pick boundary, so the dispatch is accepted instead
        /// of rejected. Returns false (and leaves the pick armed) on an
        /// invalid target, with the status naming the problem.
        /// </summary>
        private bool TryResolveRepairAt(Vector3 at)
        {
            if (!TryPickBuilding(at, out EntityId target, out UnitState building))
            {
                _lastCommandStatus = "Repair: no own building under the cursor";
                return false;
            }
            if (building.CurrentHealth >= building.MaxHealth)
            {
                _lastCommandStatus = $"Repair: {building.Role} is undamaged";
                return false;
            }

            ReadOnlySpan<EntityId> builders = BuilderSelection(out int builderCount);
            if (builderCount == 0)
            {
                _lastCommandStatus = "Repair: no Builder in the selection";
                return false;
            }

            DispatchMoveAndRepair(builders, at, target);
            return true;
        }

        /// <summary>
        /// The repair gesture as TWO schema-v1 intents: the sim's repair
        /// order is a standing order with NO movement coupling (held, never
        /// dropped, while the Builder is out of reach — ConstructionSystem
        /// remarks), so an order without a move would silently do nothing
        /// until the Builder happens to walk past. The Builders are sent
        /// toward the target first; Stop clears the standing order as
        /// documented. Both intents flow through the dispatcher.
        /// </summary>
        private void DispatchMoveAndRepair(ReadOnlySpan<EntityId> builders, Vector3 approach, EntityId target)
        {
            Report("Move (repair approach)", _dispatcher.MoveTo(builders, approach.x, approach.z));
            Report("Repair", _dispatcher.Repair(builders, target));
        }

        /// <summary>
        /// Nearest own active entity with a building role to a ground point —
        /// the repair pick's target search. A construction site (role Unit)
        /// is excluded by construction; the completed-placement check the
        /// executor runs stays authoritative at the target tick.
        /// </summary>
        private bool TryPickBuilding(Vector3 world, out EntityId picked, out UnitState building)
        {
            picked = EntityId.Invalid;
            building = default;
            EntityManager entities = _runner.Entities;
            if (entities == null) return false;

            byte slot = _dispatcher.LocalSlot;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            float bestDistanceSq = _pickRadiusWorld * _pickRadiusWorld;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != slot) continue;
                if (!SimDefinitions.IsBuildingRole(unit.Role)) continue;

                // Presentation-side boundary conversion (picking is UI, not sim).
                float dx = unit.Transform.PositionX.ToFloat() - world.x;
                float dy = unit.Transform.PositionY.ToFloat() - world.z;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq > bestDistanceSq) continue;

                bestDistanceSq = distanceSq;
                picked = unit.Id;
                building = unit;
            }

            return picked.IsValid;
        }

        // ----------------------------------------------------------------
        // Selection
        // ----------------------------------------------------------------

        /// <summary>
        /// Box select over the ground-projected AABB of all four drag corners.
        /// Four, not two: under a tilted camera the screen rectangle projects
        /// to a trapezoid, and two corners would clip the selection.
        /// </summary>
        private void SelectBox(Vector2 a, Vector2 b)
        {
            if (_runner.Entities == null) return;
            if (!TryScreenPointToGround(a, out Vector3 p0)) return;
            if (!TryScreenPointToGround(b, out Vector3 p1)) return;
            if (!TryScreenPointToGround(new Vector2(a.x, b.y), out Vector3 p2)) return;
            if (!TryScreenPointToGround(new Vector2(b.x, a.y), out Vector3 p3)) return;

            float minX = Mathf.Min(Mathf.Min(p0.x, p1.x), Mathf.Min(p2.x, p3.x));
            float maxX = Mathf.Max(Mathf.Max(p0.x, p1.x), Mathf.Max(p2.x, p3.x));
            float minY = Mathf.Min(Mathf.Min(p0.z, p1.z), Mathf.Min(p2.z, p3.z));
            float maxY = Mathf.Max(Mathf.Max(p0.z, p1.z), Mathf.Max(p2.z, p3.z));

            int count = _selection.SelectBox(_runner.Entities, _dispatcher.LocalSlot, minX, minY, maxX, maxY);
            _lastCommandStatus = $"Box select: {count} unit(s)";
        }

        /// <summary>Click select: nearest own active unit within <see cref="_pickRadiusWorld"/>, else clear.</summary>
        private void SelectSingle(Vector2 screenPoint)
        {
            if (_runner.Entities == null) return;
            if (TryScreenPointToGround(screenPoint, out Vector3 world)
                && TryPickUnit(world, ownedByLocalSlot: true, out EntityId picked))
            {
                _selection.SelectSingle(picked);
                _lastCommandStatus = $"Selected entity {picked.Index}";
                return;
            }

            _selection.ClearSelection();
            _lastCommandStatus = "Selection cleared";
        }

        /// <summary>Nearest active unit to a ground point, filtered by ownership.</summary>
        private bool TryPickUnit(Vector3 world, bool ownedByLocalSlot, out EntityId picked)
        {
            picked = EntityId.Invalid;
            EntityManager entities = _runner.Entities;
            if (entities == null) return false;

            byte slot = _dispatcher.LocalSlot;
            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            float bestDistanceSq = _pickRadiusWorld * _pickRadiusWorld;

            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive) continue;
                if ((unit.PlayerId == slot) != ownedByLocalSlot) continue;

                // Presentation-side boundary conversion (picking is UI, not sim).
                float dx = unit.Transform.PositionX.ToFloat() - world.x;
                float dy = unit.Transform.PositionY.ToFloat() - world.z;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq > bestDistanceSq) continue;

                bestDistanceSq = distanceSq;
                picked = unit.Id;
            }

            return picked.IsValid;
        }

        // ----------------------------------------------------------------
        // Target resolution
        // ----------------------------------------------------------------

        /// <summary>
        /// Nearest field with reserve left. EconomySystem exposes only
        /// FieldCount plus a by-id lookup, so ids are probed ascending and the
        /// scan stops once FieldCount fields are found (canonical setup: 1, 2).
        /// </summary>
        private bool TryResolveNearestField(Vector3 world, out ushort fieldId)
        {
            fieldId = 0;
            EconomySystem economy = _runner.Economy;
            if (economy == null) return false;

            float best = float.MaxValue;
            int found = 0;
            for (ushort id = 1; id <= EconomySystem.MaxFields && found < economy.FieldCount; id++)
            {
                if (!economy.TryGetField(id, out AetheriumField field)) continue;
                found++;
                if (field.IsExhausted) continue;

                float dx = field.GridPos.X + 0.5f - world.x;
                float dy = field.GridPos.Y + 0.5f - world.z;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq >= best) continue;

                best = distanceSq;
                fieldId = field.FieldId;
            }
            return fieldId != 0;
        }

        /// <summary>
        /// Producer for a unit definition: a selected own building of the
        /// definition's producer role wins, otherwise the first own building of
        /// that role in entity order. Construction sites carry role Unit until
        /// completion, so they are excluded by construction.
        /// </summary>
        private bool TryResolveProducer(ushort unitDefId, out EntityId building)
        {
            building = EntityId.Invalid;
            EntityManager entities = _runner.Entities;
            if (entities == null || !SimDefinitions.TryGetUnit(unitDefId, out SimUnitDefinition definition))
            {
                return false;
            }

            byte slot = _dispatcher.LocalSlot;
            ReadOnlySpan<EntityId> selected = _selection.SelectedEntities;
            for (int i = 0; i < selected.Length; i++)
            {
                if (!entities.TryGetUnit(selected[i], out UnitState candidate)) continue;
                if (candidate.PlayerId != slot || candidate.Role != definition.ProducerRole) continue;
                building = candidate.Id;
                return true;
            }

            UnitState[] units = entities.RawUnits;
            int capacity = entities.Capacity;
            for (int i = 0; i < capacity; i++)
            {
                ref readonly UnitState unit = ref units[i];
                if (!unit.IsActive || unit.PlayerId != slot || unit.Role != definition.ProducerRole) continue;
                building = unit.Id;
                return true;
            }
            return false;
        }

        // ----------------------------------------------------------------
        // Projection, reporting, drag rectangle
        // ----------------------------------------------------------------

        /// <summary>
        /// Screen point onto the ground plane. Deliberately local: this
        /// assembly (Nova.Presentation.UI, rank 4) cannot reference
        /// Nova.Presentation (also rank 4) — quality/scripts/run_gate_check.py
        /// rejects same-layer edges — so RtsCameraController.TryScreenPointToGround
        /// is unreachable from here. Same plane, same result. If the camera
        /// controller ever moves to rank &lt;= 3 this becomes a delegation.
        /// </summary>
        private bool TryScreenPointToGround(Vector2 screenPoint, out Vector3 world)
        {
            world = Vector3.zero;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Ray ray = _camera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
            if (Mathf.Abs(ray.direction.y) < 1e-6f) return false;

            float distance = (_groundPlaneY - ray.origin.y) / ray.direction.y;
            if (distance < 0f) return false;

            world = ray.origin + ray.direction * distance;
            world.y = _groundPlaneY;
            return true;
        }

        /// <summary>Footprint origin cell into the ushort payload domain (clamped; the validator rejects off-map cells anyway).</summary>
        private static ushort ToGridCoordinate(int gridCoordinate)
        {
            return (ushort)Mathf.Clamp(gridCoordinate, 0, ushort.MaxValue);
        }

        /// <summary>
        /// Records a dispatch verdict for the HUD and logs rejections once per
        /// distinct message — the dispatcher never swallows a rejection, and
        /// right-clicking with an empty selection would otherwise spam the log.
        /// </summary>
        private void Report(string label, IntentDispatchResult result)
        {
            string previous = _lastCommandStatus;
            _lastCommandStatus = result.Accepted
                ? $"{label}: accepted ({result.CommandCount} cmd, {result.EntityIdCount} ids)"
                : $"{label}: {result.Result} ({result.RejectReason})";
            if (!result.Accepted && !string.Equals(previous, _lastCommandStatus, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[RtsDeviceInput] {_lastCommandStatus}");
            }
        }

        private void OnGUI()
        {
            // No drag rectangle while the placement ghost owns the LMB.
            if (_placementMode || !_dragActive || !_dragPastThreshold) return;

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity; // the drag rectangle is raw screen pixels

            Vector2 mouse = Input.mousePosition;
            float xMin = Mathf.Min(_dragStart.x, mouse.x);
            float xMax = Mathf.Max(_dragStart.x, mouse.x);
            float yMin = Screen.height - Mathf.Max(_dragStart.y, mouse.y); // GUI origin is top-left
            float yMax = Screen.height - Mathf.Min(_dragStart.y, mouse.y);
            var rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin);

            GUI.color = new Color(0.35f, 0.9f, 0.45f, 0.15f);
            GUI.DrawTexture(rect, Pixel);
            GUI.color = new Color(0.35f, 0.95f, 0.45f, 0.9f);
            const float border = 2f;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, border), Pixel);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - border, rect.width, border), Pixel);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, border, rect.height), Pixel);
            GUI.DrawTexture(new Rect(rect.xMax - border, rect.yMin, border, rect.height), Pixel);

            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private Texture2D Pixel
        {
            get
            {
                if (_pixel == null)
                {
                    _pixel = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                    _pixel.SetPixel(0, 0, Color.white);
                    _pixel.Apply();
                }
                return _pixel;
            }
        }

        private void OnDestroy()
        {
            if (_pixel != null) Destroy(_pixel);
        }
    }
}
