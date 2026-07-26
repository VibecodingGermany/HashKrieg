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

        [Header("Graybox tuning")]
        [Tooltip("Ground plane height. Picking projects onto this mathematical plane, not onto view colliders, so the per-role view heights do not matter.")]
        [SerializeField] private float _groundPlaneY = 0f;
        [Tooltip("Pixels the mouse must travel before a click becomes a box drag.")]
        [SerializeField] private float _dragThresholdPixels = 8f;
        [Tooltip("Click-select radius in world units (= cells).")]
        [SerializeField] private float _pickRadiusWorld = 1.5f;

        [Header("Definition ids (verified against SimDefinitions)")]
        [Tooltip("B: Power — defId 2, 300 AE, prerequisite-free.")]
        [SerializeField] private ushort _buildingDefId = 2;
        [Tooltip("Shift+B: Barracks — defId 5, 500 AE, prerequisite-free.")]
        [SerializeField] private ushort _altBuildingDefId = 5;
        [Tooltip("Q: Harvester — defId 2, tier 1, produced by the HQ that exists from tick 0.")]
        [SerializeField] private ushort _unitDefId = 2;
        [Tooltip("Shift+Q: BasicInfantry — defId 3, tier 1, produced by a Barracks you build with Shift+B.")]
        [SerializeField] private ushort _altUnitDefId = 3;

        private readonly SelectionManager _selection = new SelectionManager();
        private RtsIntentDispatcher _dispatcher;
        private CommandIngress _boundIngress;

        private Vector2 _dragStart;
        private bool _dragActive;
        private bool _dragPastThreshold;
        private Texture2D _pixel;
        private string _legend = string.Empty;
        private string _lastCommandStatus = "no command yet";

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

        /// <summary>Programmatic wiring for bootstrap code that builds the scene at runtime.</summary>
        public void Bind(MatchRunner runner, Camera cam = null)
        {
            _runner = runner;
            if (cam != null) _camera = cam;
        }

        private void Awake()
        {
            if (_runner == null) _runner = FindAnyObjectByType<MatchRunner>();
            if (_camera == null) _camera = Camera.main;
            _legend =
                $"LMB click/drag select | RMB move | S stop | A attack (enemy under cursor, else attack-move) | " +
                $"H harvest nearest field | R return cargo | B build {_buildingDefId} (Shift {_altBuildingDefId}) | " +
                $"Q queue {_unitDefId} (Shift {_altUnitDefId})\n" +
                "Camera: arrow keys / screen edge pan | wheel zoom | Z,X rotate";
        }

        private void Update()
        {
            if (!EnsureDispatcher()) return;

            Vector2 mouse = Input.mousePosition;
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

        private void HandleSelection(Vector2 mouse)
        {
            if (Input.GetMouseButtonDown(0))
            {
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

            if (Input.GetMouseButtonDown(1) && TryScreenPointToGround(mouse, out Vector3 moveTo))
            {
                Report("Move", _dispatcher.MoveTo(_selection.SelectedEntities, moveTo.x, moveTo.z));
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                Report("Stop", _dispatcher.Stop(_selection.SelectedEntities));
            }

            if (Input.GetKeyDown(KeyCode.A) && TryScreenPointToGround(mouse, out Vector3 attackAt))
            {
                // Schema v1 has no attack-move register entry (see
                // RtsIntentDispatcher.Attack): an enemy under the cursor becomes
                // a real AttackTarget, everything else is the honest A-move
                // approximation — Move, and Combat acquires targets on arrival.
                if (TryPickUnit(attackAt, ownedByLocalSlot: false, out EntityId enemy))
                {
                    Report("Attack", _dispatcher.Attack(_selection.SelectedEntities, enemy));
                }
                else
                {
                    Report("Attack-move", _dispatcher.MoveTo(_selection.SelectedEntities, attackAt.x, attackAt.z));
                }
            }

            if (Input.GetKeyDown(KeyCode.H) && TryScreenPointToGround(mouse, out Vector3 harvestAt))
            {
                if (TryResolveNearestField(harvestAt, out ushort fieldId))
                {
                    Report($"Harvest #{fieldId}", _dispatcher.Harvest(_selection.SelectedEntities, fieldId));
                }
                else
                {
                    _lastCommandStatus = "Harvest: no unexhausted Aetherium field registered";
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Report("ReturnCargo", _dispatcher.ReturnCargo(_selection.SelectedEntities));
            }

            if (Input.GetKeyDown(KeyCode.B) && TryScreenPointToGround(mouse, out Vector3 buildAt))
            {
                ushort defId = shift ? _altBuildingDefId : _buildingDefId;
                // The payload cell is the lower-left origin of the 3x3 footprint.
                Report($"PlaceBuilding {defId}",
                    _dispatcher.PlaceBuilding(defId, ToGridCoordinate(buildAt.x), ToGridCoordinate(buildAt.z)));
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                ushort defId = shift ? _altUnitDefId : _unitDefId;
                if (TryResolveProducer(defId, out EntityId producer))
                {
                    Report($"QueueUnit {defId}", _dispatcher.QueueUnit(producer, defId, 1));
                }
                else
                {
                    _lastCommandStatus = $"QueueUnit {defId}: no own producer building for that definition";
                }
            }
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

        /// <summary>World coordinate to grid cell (floor, clamped into the ushort payload domain).</summary>
        private static ushort ToGridCoordinate(float worldCoordinate)
        {
            return (ushort)Mathf.Clamp(Mathf.FloorToInt(worldCoordinate), 0, ushort.MaxValue);
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
            if (!_dragActive || !_dragPastThreshold) return;

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
