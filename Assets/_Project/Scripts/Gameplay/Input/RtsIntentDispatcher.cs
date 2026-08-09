using System;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Definitions;
using Nova.Simulation.State;

// Namespace is deliberately Nova.Gameplay (flat), not Nova.Gameplay.Input,
// matching the Gameplay/UI folder precedent (SelectionManager,
// CommandCardPresenter). A Nova.Gameplay.Input namespace would shadow
// UnityEngine.Input for every other file in a Nova.Gameplay.* namespace.
namespace Nova.Gameplay
{
    /// <summary>
    /// Outcome of one <see cref="RtsIntentDispatcher"/> call. A single call can
    /// produce several commands (a selection larger than
    /// <see cref="CommandLimits.MaxEntityIdsPerCommand"/> is chunked), so the
    /// result reports how much of the request actually entered the ingress and,
    /// on failure, the canonical structural reason — the dispatcher never
    /// swallows a rejection.
    /// </summary>
    public readonly struct IntentDispatchResult
    {
        public IntentDispatchResult(
            CommandIngressResult result, CommandRejectReason rejectReason,
            int commandCount, int entityIdCount)
        {
            Result = result;
            RejectReason = rejectReason;
            CommandCount = commandCount;
            EntityIdCount = entityIdCount;
        }

        /// <summary>Ingress verdict; <see cref="CommandIngressResult.Rejected"/> stops a chunked run.</summary>
        public CommandIngressResult Result { get; }

        /// <summary>Structural reason of a rejection; <see cref="CommandRejectReason.None"/> otherwise.</summary>
        public CommandRejectReason RejectReason { get; }

        /// <summary>Commands that entered the ingress (chunks accepted before any failure).</summary>
        public int CommandCount { get; }

        /// <summary>Entity ids covered by <see cref="CommandCount"/> commands; 0 for id-less kinds.</summary>
        public int EntityIdCount { get; }

        /// <summary>True when every command of this call was accepted.</summary>
        public bool Accepted => Result == CommandIngressResult.Accepted;

        public override string ToString()
        {
            return $"IntentDispatchResult({Result}, {RejectReason}, commands={CommandCount}, ids={EntityIdCount})";
        }
    }

    /// <summary>
    /// The single command-submission layer between input/HUD code and the
    /// simulation. Builds canonical schema-v1 payloads from selection handles,
    /// hands them to <see cref="CommandIngress.TrySubmitIntent"/> and returns
    /// the verdict; it never touches simulation state itself
    /// (docs/tech/Commands.md section 1 — UI and AI may only create intents).
    /// <para>
    /// Plain testable class on purpose: no MonoBehaviour, no UnityEngine.Input,
    /// no UnityEngine types at all. A device-reading MonoBehaviour lives one
    /// layer above and only calls into this, so swapping the input backend
    /// (legacy Input -> Input System) leaves this class untouched.
    /// </para>
    /// <para>
    /// Selection normalization is mandatory, not cosmetic: an entity list is
    /// canonical only when it is non-empty, strictly ascending, duplicate-free
    /// and at most <see cref="CommandLimits.MaxEntityIdsPerCommand"/> long
    /// (<see cref="CommandIds.IsCanonicalEntityList"/>). Selections above that
    /// cap are split into several commands instead of being rejected whole.
    /// </para>
    /// </summary>
    public sealed class RtsIntentDispatcher
    {
        /// <summary>Entity ids one command may carry (canonical contract value: 100).</summary>
        public const int MaxEntityIdsPerCommand = CommandLimits.MaxEntityIdsPerCommand;

        private readonly CommandIngress _ingress;
        private readonly UnitCommandStateView _stateView;

        /// <summary>
        /// Creates a dispatcher bound to a match's ingress.
        /// <paramref name="stateView"/> is optional; when supplied, acting
        /// entity ids that do not exist or are not owned by the local slot are
        /// dropped before chunking, because <see cref="CommandExecutor"/>
        /// rejects the WHOLE command on the first such id — a single stale
        /// handle in the selection would otherwise cancel the entire order.
        /// The filter is a local convenience only; the authoritative check
        /// still runs at the target tick.
        /// </summary>
        public RtsIntentDispatcher(CommandIngress ingress, UnitCommandStateView stateView = null)
        {
            _ingress = ingress ?? throw new ArgumentNullException(nameof(ingress));
            _stateView = stateView;
        }

        /// <summary>The session-bound local player slot commands are sealed for.</summary>
        public byte LocalSlot => _ingress.Session.LocalSlot;

        /// <summary>
        /// Resolves one serialized canonical Alliance definition id to the
        /// same role for the local faction. This is presentation/input
        /// mapping only; the stable simulation definition table is unchanged.
        /// </summary>
        public static bool TryResolveCanonicalDefinitionId(
            ushort canonicalAllianceDefinitionId, FactionId localFaction,
            out ushort localDefinitionId)
        {
            localDefinitionId = 0;
            if (localFaction != FactionId.Alliance && localFaction != FactionId.Legion)
            {
                return false;
            }

            UnitRole role;
            if (SimDefinitions.TryGetBuilding(
                    canonicalAllianceDefinitionId,
                    out SimBuildingDefinition building)
                && building.Faction == FactionId.Alliance)
            {
                role = building.Role;
            }
            else if (SimDefinitions.TryGetUnit(
                         canonicalAllianceDefinitionId,
                         out SimUnitDefinition unit)
                     && unit.Faction == FactionId.Alliance)
            {
                role = unit.Role;
            }
            else
            {
                return false;
            }

            localDefinitionId = SimDefinitions.ToDefinitionId(localFaction, role);
            return localDefinitionId != 0;
        }

        // ----------------------------------------------------------------
        // Entity-list commands (chunked at MaxEntityIdsPerCommand)
        // ----------------------------------------------------------------

        /// <summary>Move the selection to a simulation-space target.</summary>
        public IntentDispatchResult MoveTo(ReadOnlySpan<EntityId> selection, SimFixed targetX, SimFixed targetY)
        {
            return SubmitEntityListCommand(
                selection, ids => CommandIntent.Create(new MovePayload(ids, targetX, targetY)));
        }

        /// <summary>
        /// Move the selection to a world-space target. This overload is the
        /// single float -> fixed-point boundary conversion (raycast hit point
        /// to Q16.16); the resulting bytes are what every peer replays.
        /// </summary>
        public IntentDispatchResult MoveTo(ReadOnlySpan<EntityId> selection, float worldX, float worldY)
        {
            return MoveTo(selection, SimFixed.FromFloat(worldX), SimFixed.FromFloat(worldY));
        }

        /// <summary>
        /// Attack a concrete target entity (<see cref="CommandKind.AttackTarget"/>).
        /// Schema v1 has no attack-move register entry, so an A-move UI gesture
        /// must be expressed as <see cref="MoveTo"/> plus the combat system's
        /// own target acquisition — do not fake one here.
        /// </summary>
        public IntentDispatchResult Attack(ReadOnlySpan<EntityId> selection, EntityId target)
        {
            uint rawTarget = UnitCommandStateView.ToRawEntityId(target);
            return SubmitEntityListCommand(
                selection, ids => CommandIntent.Create(new AttackTargetPayload(ids, rawTarget)));
        }

        /// <summary>Cancel every standing order of the selection.</summary>
        public IntentDispatchResult Stop(ReadOnlySpan<EntityId> selection)
        {
            return SubmitEntityListCommand(selection, ids => CommandIntent.Create(new StopPayload(ids)));
        }

        /// <summary>Send harvesters to a registered Aetherium field (stable numeric field id, 0 invalid).</summary>
        public IntentDispatchResult Harvest(ReadOnlySpan<EntityId> selection, ushort fieldId)
        {
            return SubmitEntityListCommand(
                selection, ids => CommandIntent.Create(new HarvestPayload(ids, fieldId)));
        }

        /// <summary>Send the selection back to unload cargo.</summary>
        public IntentDispatchResult ReturnCargo(ReadOnlySpan<EntityId> selection)
        {
            return SubmitEntityListCommand(selection, ids => CommandIntent.Create(new ReturnCargoPayload(ids)));
        }

        /// <summary>Order the selection to repair a target entity.</summary>
        public IntentDispatchResult Repair(ReadOnlySpan<EntityId> selection, EntityId target)
        {
            uint rawTarget = UnitCommandStateView.ToRawEntityId(target);
            return SubmitEntityListCommand(
                selection, ids => CommandIntent.Create(new RepairPayload(ids, rawTarget)));
        }

        // ----------------------------------------------------------------
        // Single-record commands (no entity list, never chunked)
        // ----------------------------------------------------------------

        /// <summary>Place a building at a grid cell (definition id 0 is structurally invalid).</summary>
        public IntentDispatchResult PlaceBuilding(ushort buildingDefId, ushort gridX, ushort gridY)
        {
            return Submit(new PlaceBuildingPayload(buildingDefId, gridX, gridY));
        }

        /// <summary>Queue units in a producing building (count 0 is structurally invalid).</summary>
        public IntentDispatchResult QueueUnit(EntityId building, ushort unitDefId, ushort count)
        {
            return Submit(new QueueUnitPayload(
                UnitCommandStateView.ToRawEntityId(building), unitDefId, count));
        }

        /// <summary>Cancel one queue entry of a producing building.</summary>
        public IntentDispatchResult CancelProduction(EntityId building, ushort queueIndex)
        {
            return Submit(new CancelProductionPayload(
                UnitCommandStateView.ToRawEntityId(building), queueIndex));
        }

        /// <summary>Set a building's rally point in simulation space.</summary>
        public IntentDispatchResult SetRallyPoint(EntityId building, SimFixed targetX, SimFixed targetY)
        {
            return Submit(new SetRallyPointPayload(
                UnitCommandStateView.ToRawEntityId(building), targetX, targetY));
        }

        /// <summary>Set a building's rally point from a world-space point (float -> Q16.16 boundary).</summary>
        public IntentDispatchResult SetRallyPoint(EntityId building, float worldX, float worldY)
        {
            return SetRallyPoint(building, SimFixed.FromFloat(worldX), SimFixed.FromFloat(worldY));
        }

        /// <summary>Cancel a construction site.</summary>
        public IntentDispatchResult CancelConstruction(EntityId site)
        {
            return Submit(new CancelConstructionPayload(UnitCommandStateView.ToRawEntityId(site)));
        }

        /// <summary>Sell a finished building.</summary>
        public IntentDispatchResult Sell(EntityId building)
        {
            return Submit(new SellPayload(UnitCommandStateView.ToRawEntityId(building)));
        }

        // InstallDefenseModule deliberately has no method: the schema-v1 payload
        // exists, but UnitCommandStateView rejects the kind with
        // RejectedPrerequisitesNotMet in this slice (defense modules are G2/G4
        // content). An API that can only ever fail would be a lie.

        // ----------------------------------------------------------------
        // Internals
        // ----------------------------------------------------------------

        /// <summary>
        /// Normalizes the selection into a canonical id list, splits it at
        /// <see cref="MaxEntityIdsPerCommand"/> and submits one intent per
        /// chunk. Submission stops at the first rejection and surfaces its
        /// reason together with the chunks that already went through.
        /// </summary>
        private IntentDispatchResult SubmitEntityListCommand(
            ReadOnlySpan<EntityId> selection, Func<uint[], CommandIntent> intentFactory)
        {
            uint[] ids = NormalizeSelection(selection);
            if (ids.Length == 0)
            {
                // Reported with the canonical reason without burning a sequence:
                // an empty list is exactly what the ingress would reject.
                return new IntentDispatchResult(
                    CommandIngressResult.Rejected, CommandRejectReason.EmptyEntityList, 0, 0);
            }

            var aggregate = CommandIngressResult.Accepted;
            int commandCount = 0;
            int covered = 0;
            for (int offset = 0; offset < ids.Length; offset += MaxEntityIdsPerCommand)
            {
                int length = Math.Min(MaxEntityIdsPerCommand, ids.Length - offset);
                var chunk = new uint[length];
                Array.Copy(ids, offset, chunk, 0, length);

                CommandIngressResult result = _ingress.TrySubmitIntent(
                    intentFactory(chunk), out CommandRejectReason reason);
                if (result == CommandIngressResult.Rejected)
                {
                    return new IntentDispatchResult(result, reason, commandCount, covered);
                }
                if (result != CommandIngressResult.Accepted)
                {
                    aggregate = result;
                }
                commandCount++;
                covered += length;
            }
            return new IntentDispatchResult(aggregate, CommandRejectReason.None, commandCount, covered);
        }

        private IntentDispatchResult Submit<TPayload>(in TPayload payload)
            where TPayload : struct, ICommandPayload
        {
            CommandIngressResult result = _ingress.TrySubmitIntent(
                CommandIntent.Create(payload), out CommandRejectReason reason);
            int commandCount = result == CommandIngressResult.Rejected ? 0 : 1;
            return new IntentDispatchResult(result, reason, commandCount, 0);
        }

        /// <summary>
        /// Selection handles -> canonical raw id list: packs each handle, drops
        /// non-packable (stale) handles, applies the optional state-view filter,
        /// then sorts ascending and removes duplicates. Duplicates are a
        /// structural error at the ingress, not something it silently repairs.
        /// </summary>
        private uint[] NormalizeSelection(ReadOnlySpan<EntityId> selection)
        {
            if (selection.Length == 0) return Array.Empty<uint>();

            var raw = new uint[selection.Length];
            int count = 0;
            for (int i = 0; i < selection.Length; i++)
            {
                uint id = UnitCommandStateView.ToRawEntityId(selection[i]);
                if (id == 0) continue;
                if (_stateView != null
                    && (!_stateView.EntityExists(id) || !_stateView.IsOwnedBy(LocalSlot, id)))
                {
                    continue;
                }
                raw[count++] = id;
            }
            if (count == 0) return Array.Empty<uint>();

            Array.Sort(raw, 0, count);
            int unique = 1;
            for (int i = 1; i < count; i++)
            {
                if (raw[i] != raw[unique - 1])
                {
                    raw[unique++] = raw[i];
                }
            }

            var ids = new uint[unique];
            Array.Copy(raw, ids, unique);
            return ids;
        }
    }
}
