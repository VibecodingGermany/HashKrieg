using System;

namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Entity and definition references extracted from a payload, used by the
    /// state-dependent executor. Zero means "not referenced by this kind".
    /// Only valid for payloads that passed structural validation.
    /// </summary>
    public struct CommandPayloadRefs
    {
        /// <summary>Sorted entity list the command acts with (may be null).</summary>
        public uint[] ActingEntities;

        /// <summary>Target entity (attack/repair), 0 when unused.</summary>
        public uint TargetEntityId;

        /// <summary>Single acting or owning entity (sell, cancel, production, rally, module), 0 when unused.</summary>
        public uint SingleEntityId;

        /// <summary>Definition id carrying a potential cost (building/unit/module), 0 when unused.</summary>
        public ushort DefinitionId;
    }

    /// <summary>
    /// Structural payload validation (docs/tech/Commands.md section 4): known
    /// kind/version combination, exact payload length, canonical ids, sorted
    /// entity lists and value domains. Anything failing here is rejected and
    /// never recorded; ownership, vision, range and cost are deliberately NOT
    /// checked here — they are state-dependent and evaluated at the target tick.
    /// </summary>
    public static class CommandPayloadValidation
    {
        /// <summary>
        /// Validates the kind/version combination and the canonical payload
        /// content of a stream record. Session actions are rejected here: they
        /// never enter the simulation record stream (Commands.md section 5).
        /// </summary>
        public static bool TryValidateStreamPayload(
            CommandKind kind,
            byte payloadVersion,
            ReadOnlySpan<byte> payload,
            out CommandRejectReason reason)
        {
            reason = CommandRejectReason.None;

            if (!CommandKindInfo.IsKnown(kind))
            {
                reason = CommandRejectReason.UnknownKind;
                return false;
            }
            if (CommandKindInfo.IsSessionAction(kind))
            {
                reason = CommandRejectReason.SessionActionInStream;
                return false;
            }
            if (payloadVersion != CommandLimits.PayloadVersionV1)
            {
                reason = CommandRejectReason.UnknownPayloadVersion;
                return false;
            }

            // Entity-list kinds: the count prefix is validated before the full
            // parse so oversized lists get their precise structural reason
            // (the reader itself refuses oversized counts as a parser limit,
            // which would otherwise surface as a generic PayloadMalformed).
            if (KindCarriesEntityList(kind))
            {
                if (payload.Length < 2)
                {
                    reason = CommandRejectReason.PayloadMalformed;
                    return false;
                }
                int listCount = payload[0] | (payload[1] << 8);
                if (listCount == 0)
                {
                    reason = CommandRejectReason.EmptyEntityList;
                    return false;
                }
                if (listCount > CommandLimits.MaxEntityIdsPerCommand)
                {
                    reason = CommandRejectReason.TooManyEntityIds;
                    return false;
                }
            }

            if (!TryParseRefs(kind, payload, out CommandPayloadRefs refs))
            {
                reason = CommandRejectReason.PayloadMalformed;
                return false;
            }

            if (refs.ActingEntities != null)
            {
                if (refs.ActingEntities.Length == 0)
                {
                    reason = CommandRejectReason.EmptyEntityList;
                    return false;
                }
                if (refs.ActingEntities.Length > CommandLimits.MaxEntityIdsPerCommand)
                {
                    reason = CommandRejectReason.TooManyEntityIds;
                    return false;
                }
                if (!CommandIds.IsCanonicalEntityList(refs.ActingEntities))
                {
                    // Distinguish the two canonicality failures deterministically:
                    // scan in order, report the first violation kind.
                    uint previous = 0;
                    for (int i = 0; i < refs.ActingEntities.Length; i++)
                    {
                        if (!CommandIds.IsCanonicalEntityId(refs.ActingEntities[i]))
                        {
                            reason = CommandRejectReason.InvalidEntityId;
                            return false;
                        }
                        if (i > 0 && refs.ActingEntities[i] <= previous)
                        {
                            reason = CommandRejectReason.UnsortedEntityList;
                            return false;
                        }
                        previous = refs.ActingEntities[i];
                    }
                    reason = CommandRejectReason.InvalidEntityId;
                    return false;
                }
            }

            if (refs.TargetEntityId != 0 && !CommandIds.IsCanonicalEntityId(refs.TargetEntityId))
            {
                reason = CommandRejectReason.InvalidEntityId;
                return false;
            }
            if (refs.SingleEntityId != 0 && !CommandIds.IsCanonicalEntityId(refs.SingleEntityId))
            {
                reason = CommandRejectReason.InvalidEntityId;
                return false;
            }

            // Mandatory per-kind references: a raw 0 on the wire is an invalid
            // id, not an absent one (the kinds below always carry the field).
            switch (kind)
            {
                case CommandKind.AttackTarget:
                case CommandKind.Repair:
                    if (refs.TargetEntityId == 0)
                    {
                        reason = CommandRejectReason.InvalidEntityId;
                        return false;
                    }
                    break;
                case CommandKind.CancelConstruction:
                case CommandKind.Sell:
                case CommandKind.QueueUnit:
                case CommandKind.CancelProduction:
                case CommandKind.SetRallyPoint:
                case CommandKind.InstallDefenseModule:
                    if (refs.SingleEntityId == 0)
                    {
                        reason = CommandRejectReason.InvalidEntityId;
                        return false;
                    }
                    break;
            }

            if (refs.DefinitionId != 0 && !CommandIds.IsValidDefinitionId(refs.DefinitionId))
            {
                reason = CommandRejectReason.InvalidDefinitionId;
                return false;
            }
            if (kind == CommandKind.QueueUnit)
            {
                var reader = new CommandPayloadReader(payload);
                reader.TryReadUInt32(out _);
                reader.TryReadUInt16(out _);
                reader.TryReadUInt16(out ushort count);
                if (count == 0)
                {
                    reason = CommandRejectReason.InvalidCount;
                    return false;
                }
            }
            if (kind == CommandKind.PlaceBuilding || kind == CommandKind.Harvest
                || kind == CommandKind.InstallDefenseModule)
            {
                // Definition-id-bearing kinds: raw 0 is structurally invalid.
                if (refs.DefinitionId == 0)
                {
                    reason = CommandRejectReason.InvalidDefinitionId;
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Extracts the entity/definition references of an already validated
        /// payload for the state-dependent executor. Returns false when the
        /// payload does not parse (a programming error after validation).
        /// </summary>
        public static bool TryExtractRefs(
            CommandKind kind, ReadOnlySpan<byte> payload, out CommandPayloadRefs refs)
        {
            return TryParseRefs(kind, payload, out refs);
        }

        private static bool KindCarriesEntityList(CommandKind kind)
        {
            switch (kind)
            {
                case CommandKind.Move:
                case CommandKind.Stop:
                case CommandKind.AttackTarget:
                case CommandKind.Harvest:
                case CommandKind.ReturnCargo:
                case CommandKind.Repair:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseRefs(
            CommandKind kind, ReadOnlySpan<byte> payload, out CommandPayloadRefs refs)
        {
            refs = default;
            var reader = new CommandPayloadReader(payload);
            bool ok;
            switch (kind)
            {
                case CommandKind.Move:
                    ok = MovePayload.TryParse(ref reader, out MovePayload move);
                    if (ok) refs.ActingEntities = move.EntityIds;
                    break;
                case CommandKind.Stop:
                    ok = StopPayload.TryParse(ref reader, out StopPayload stop);
                    if (ok) refs.ActingEntities = stop.EntityIds;
                    break;
                case CommandKind.AttackTarget:
                    ok = AttackTargetPayload.TryParse(ref reader, out AttackTargetPayload attack);
                    if (ok)
                    {
                        refs.ActingEntities = attack.EntityIds;
                        refs.TargetEntityId = attack.TargetEntityId;
                    }
                    break;
                case CommandKind.Harvest:
                    ok = HarvestPayload.TryParse(ref reader, out HarvestPayload harvest);
                    if (ok)
                    {
                        refs.ActingEntities = harvest.EntityIds;
                        refs.DefinitionId = harvest.FieldId;
                    }
                    break;
                case CommandKind.ReturnCargo:
                    ok = ReturnCargoPayload.TryParse(ref reader, out ReturnCargoPayload ret);
                    if (ok) refs.ActingEntities = ret.EntityIds;
                    break;
                case CommandKind.PlaceBuilding:
                    ok = PlaceBuildingPayload.TryParse(ref reader, out PlaceBuildingPayload place);
                    if (ok) refs.DefinitionId = place.BuildingDefId;
                    break;
                case CommandKind.CancelConstruction:
                    ok = CancelConstructionPayload.TryParse(ref reader, out CancelConstructionPayload cancel);
                    if (ok) refs.SingleEntityId = cancel.EntityId;
                    break;
                case CommandKind.Repair:
                    ok = RepairPayload.TryParse(ref reader, out RepairPayload repair);
                    if (ok)
                    {
                        refs.ActingEntities = repair.EntityIds;
                        refs.TargetEntityId = repair.TargetEntityId;
                    }
                    break;
                case CommandKind.Sell:
                    ok = SellPayload.TryParse(ref reader, out SellPayload sell);
                    if (ok) refs.SingleEntityId = sell.EntityId;
                    break;
                case CommandKind.QueueUnit:
                    ok = QueueUnitPayload.TryParse(ref reader, out QueueUnitPayload queue);
                    if (ok)
                    {
                        refs.SingleEntityId = queue.BuildingEntityId;
                        refs.DefinitionId = queue.UnitDefId;
                    }
                    break;
                case CommandKind.CancelProduction:
                    ok = CancelProductionPayload.TryParse(ref reader, out CancelProductionPayload cancelProd);
                    if (ok) refs.SingleEntityId = cancelProd.BuildingEntityId;
                    break;
                case CommandKind.SetRallyPoint:
                    ok = SetRallyPointPayload.TryParse(ref reader, out SetRallyPointPayload rally);
                    if (ok) refs.SingleEntityId = rally.BuildingEntityId;
                    break;
                case CommandKind.InstallDefenseModule:
                    ok = InstallDefenseModulePayload.TryParse(ref reader, out InstallDefenseModulePayload module);
                    if (ok)
                    {
                        refs.SingleEntityId = module.BuildingEntityId;
                        refs.DefinitionId = module.ModuleId;
                    }
                    break;
                default:
                    return false;
            }
            // A canonical payload is consumed exactly; trailing or missing bytes
            // are a structural length error (Commands.md section 2).
            return ok && reader.Remaining == 0;
        }
    }
}
