using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// A concrete, versioned command payload of schema v1. Payloads contain
    /// only fixed-size numeric fields: no floats, strings, GUIDs, dictionaries
    /// or runtime-dependent layouts (docs/tech/Commands.md section 2). Text and
    /// definition inputs are resolved to stable numeric ids before this point.
    /// </summary>
    public interface ICommandPayload
    {
        /// <summary>The register entry this payload belongs to.</summary>
        CommandKind Kind { get; }

        /// <summary>Payload layout version; schema v1 uses exactly 1.</summary>
        byte Version { get; }

        /// <summary>Writes the canonical little-endian payload bytes.</summary>
        void WriteTo(CommandPayloadWriter writer);
    }

    /// <summary>Move: sorted entity list + target x/y as <see cref="Core.SimFixed"/>.</summary>
    public readonly struct MovePayload : ICommandPayload
    {
        public uint[] EntityIds { get; }
        public Core.SimFixed TargetX { get; }
        public Core.SimFixed TargetY { get; }

        public MovePayload(uint[] entityIds, Core.SimFixed targetX, Core.SimFixed targetY)
        {
            EntityIds = Copy(entityIds);
            TargetX = targetX;
            TargetY = targetY;
        }

        public CommandKind Kind => CommandKind.Move;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteEntityIds(EntityIds);
            writer.WriteSimFixed(TargetX);
            writer.WriteSimFixed(TargetY);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out MovePayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            if (!reader.TryReadSimFixed(out Core.SimFixed x)) return false;
            if (!reader.TryReadSimFixed(out Core.SimFixed y)) return false;
            payload = new MovePayload(ids, x, y);
            return true;
        }

        internal static uint[] Copy(uint[] ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));
            var copy = new uint[ids.Length];
            Array.Copy(ids, copy, ids.Length);
            return copy;
        }
    }

    /// <summary>Stop: sorted entity list.</summary>
    public readonly struct StopPayload : ICommandPayload
    {
        public uint[] EntityIds { get; }

        public StopPayload(uint[] entityIds)
        {
            EntityIds = MovePayload.Copy(entityIds);
        }

        public CommandKind Kind => CommandKind.Stop;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer) => writer.WriteEntityIds(EntityIds);

        public static bool TryParse(ref CommandPayloadReader reader, out StopPayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            payload = new StopPayload(ids);
            return true;
        }
    }

    /// <summary>AttackTarget: sorted entity list + target entity id.</summary>
    public readonly struct AttackTargetPayload : ICommandPayload
    {
        public uint[] EntityIds { get; }
        public uint TargetEntityId { get; }

        public AttackTargetPayload(uint[] entityIds, uint targetEntityId)
        {
            EntityIds = MovePayload.Copy(entityIds);
            TargetEntityId = targetEntityId;
        }

        public CommandKind Kind => CommandKind.AttackTarget;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteEntityIds(EntityIds);
            writer.WriteUInt32(TargetEntityId);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out AttackTargetPayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            if (!reader.TryReadUInt32(out uint target)) return false;
            payload = new AttackTargetPayload(ids, target);
            return true;
        }
    }

    /// <summary>
    /// Harvest: sorted entity list + stable numeric field id (uint16, 0 invalid).
    /// The spec fixes no field identity model; a resource field is referenced
    /// by a stable numeric id resolved before the payload is built (Q-040
    /// candidate).
    /// </summary>
    public readonly struct HarvestPayload : ICommandPayload
    {
        public uint[] EntityIds { get; }
        public ushort FieldId { get; }

        public HarvestPayload(uint[] entityIds, ushort fieldId)
        {
            EntityIds = MovePayload.Copy(entityIds);
            FieldId = fieldId;
        }

        public CommandKind Kind => CommandKind.Harvest;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteEntityIds(EntityIds);
            writer.WriteUInt16(FieldId);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out HarvestPayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            if (!reader.TryReadUInt16(out ushort fieldId)) return false;
            payload = new HarvestPayload(ids, fieldId);
            return true;
        }
    }

    /// <summary>ReturnCargo: sorted entity list.</summary>
    public readonly struct ReturnCargoPayload : ICommandPayload
    {
        public uint[] EntityIds { get; }

        public ReturnCargoPayload(uint[] entityIds)
        {
            EntityIds = MovePayload.Copy(entityIds);
        }

        public CommandKind Kind => CommandKind.ReturnCargo;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer) => writer.WriteEntityIds(EntityIds);

        public static bool TryParse(ref CommandPayloadReader reader, out ReturnCargoPayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            payload = new ReturnCargoPayload(ids);
            return true;
        }
    }

    /// <summary>
    /// PlaceBuilding: building definition id (uint16, 0 invalid) + grid cell.
    /// Grid coordinates are uint16; the MS-1 grid is 128×128 (SimulationCore.md
    /// section 10). Whether a cell is inside the map and buildable is a
    /// state-dependent check at the target tick, not a structural one.
    /// </summary>
    public readonly struct PlaceBuildingPayload : ICommandPayload
    {
        public ushort BuildingDefId { get; }
        public ushort GridX { get; }
        public ushort GridY { get; }

        public PlaceBuildingPayload(ushort buildingDefId, ushort gridX, ushort gridY)
        {
            BuildingDefId = buildingDefId;
            GridX = gridX;
            GridY = gridY;
        }

        public CommandKind Kind => CommandKind.PlaceBuilding;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteUInt16(BuildingDefId);
            writer.WriteUInt16(GridX);
            writer.WriteUInt16(GridY);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out PlaceBuildingPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt16(out ushort defId)) return false;
            if (!reader.TryReadUInt16(out ushort x)) return false;
            if (!reader.TryReadUInt16(out ushort y)) return false;
            payload = new PlaceBuildingPayload(defId, x, y);
            return true;
        }
    }

    /// <summary>CancelConstruction: construction entity id.</summary>
    public readonly struct CancelConstructionPayload : ICommandPayload
    {
        public uint EntityId { get; }

        public CancelConstructionPayload(uint entityId)
        {
            EntityId = entityId;
        }

        public CommandKind Kind => CommandKind.CancelConstruction;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer) => writer.WriteUInt32(EntityId);

        public static bool TryParse(ref CommandPayloadReader reader, out CancelConstructionPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint id)) return false;
            payload = new CancelConstructionPayload(id);
            return true;
        }
    }

    /// <summary>Repair: sorted repairer entity list + target entity id.</summary>
    public readonly struct RepairPayload : ICommandPayload
    {
        public uint[] EntityIds { get; }
        public uint TargetEntityId { get; }

        public RepairPayload(uint[] entityIds, uint targetEntityId)
        {
            EntityIds = MovePayload.Copy(entityIds);
            TargetEntityId = targetEntityId;
        }

        public CommandKind Kind => CommandKind.Repair;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteEntityIds(EntityIds);
            writer.WriteUInt32(TargetEntityId);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out RepairPayload payload)
        {
            payload = default;
            if (!reader.TryReadEntityIds(out uint[] ids)) return false;
            if (!reader.TryReadUInt32(out uint target)) return false;
            payload = new RepairPayload(ids, target);
            return true;
        }
    }

    /// <summary>Sell: building entity id.</summary>
    public readonly struct SellPayload : ICommandPayload
    {
        public uint EntityId { get; }

        public SellPayload(uint entityId)
        {
            EntityId = entityId;
        }

        public CommandKind Kind => CommandKind.Sell;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer) => writer.WriteUInt32(EntityId);

        public static bool TryParse(ref CommandPayloadReader reader, out SellPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint id)) return false;
            payload = new SellPayload(id);
            return true;
        }
    }

    /// <summary>
    /// QueueUnit: producing building entity id + unit definition id (uint16,
    /// 0 invalid) + count (uint16, ≥ 1; zero is a structural error).
    /// </summary>
    public readonly struct QueueUnitPayload : ICommandPayload
    {
        public uint BuildingEntityId { get; }
        public ushort UnitDefId { get; }
        public ushort Count { get; }

        public QueueUnitPayload(uint buildingEntityId, ushort unitDefId, ushort count)
        {
            BuildingEntityId = buildingEntityId;
            UnitDefId = unitDefId;
            Count = count;
        }

        public CommandKind Kind => CommandKind.QueueUnit;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteUInt32(BuildingEntityId);
            writer.WriteUInt16(UnitDefId);
            writer.WriteUInt16(Count);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out QueueUnitPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint building)) return false;
            if (!reader.TryReadUInt16(out ushort defId)) return false;
            if (!reader.TryReadUInt16(out ushort count)) return false;
            payload = new QueueUnitPayload(building, defId, count);
            return true;
        }
    }

    /// <summary>CancelProduction: producing building entity id + queue index.</summary>
    public readonly struct CancelProductionPayload : ICommandPayload
    {
        public uint BuildingEntityId { get; }
        public ushort QueueIndex { get; }

        public CancelProductionPayload(uint buildingEntityId, ushort queueIndex)
        {
            BuildingEntityId = buildingEntityId;
            QueueIndex = queueIndex;
        }

        public CommandKind Kind => CommandKind.CancelProduction;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteUInt32(BuildingEntityId);
            writer.WriteUInt16(QueueIndex);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out CancelProductionPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint building)) return false;
            if (!reader.TryReadUInt16(out ushort index)) return false;
            payload = new CancelProductionPayload(building, index);
            return true;
        }
    }

    /// <summary>SetRallyPoint: building entity id + rally x/y as <see cref="Core.SimFixed"/>.</summary>
    public readonly struct SetRallyPointPayload : ICommandPayload
    {
        public uint BuildingEntityId { get; }
        public Core.SimFixed TargetX { get; }
        public Core.SimFixed TargetY { get; }

        public SetRallyPointPayload(uint buildingEntityId, Core.SimFixed targetX, Core.SimFixed targetY)
        {
            BuildingEntityId = buildingEntityId;
            TargetX = targetX;
            TargetY = targetY;
        }

        public CommandKind Kind => CommandKind.SetRallyPoint;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteUInt32(BuildingEntityId);
            writer.WriteSimFixed(TargetX);
            writer.WriteSimFixed(TargetY);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out SetRallyPointPayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint building)) return false;
            if (!reader.TryReadSimFixed(out Core.SimFixed x)) return false;
            if (!reader.TryReadSimFixed(out Core.SimFixed y)) return false;
            payload = new SetRallyPointPayload(building, x, y);
            return true;
        }
    }

    /// <summary>InstallDefenseModule: building entity id + module definition id (uint16, 0 invalid).</summary>
    public readonly struct InstallDefenseModulePayload : ICommandPayload
    {
        public uint BuildingEntityId { get; }
        public ushort ModuleId { get; }

        public InstallDefenseModulePayload(uint buildingEntityId, ushort moduleId)
        {
            BuildingEntityId = buildingEntityId;
            ModuleId = moduleId;
        }

        public CommandKind Kind => CommandKind.InstallDefenseModule;
        public byte Version => CommandLimits.PayloadVersionV1;

        public void WriteTo(CommandPayloadWriter writer)
        {
            writer.WriteUInt32(BuildingEntityId);
            writer.WriteUInt16(ModuleId);
        }

        public static bool TryParse(ref CommandPayloadReader reader, out InstallDefenseModulePayload payload)
        {
            payload = default;
            if (!reader.TryReadUInt32(out uint building)) return false;
            if (!reader.TryReadUInt16(out ushort moduleId)) return false;
            payload = new InstallDefenseModulePayload(building, moduleId);
            return true;
        }
    }
}
