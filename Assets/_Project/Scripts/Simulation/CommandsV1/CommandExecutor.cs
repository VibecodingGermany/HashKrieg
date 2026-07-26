namespace Nova.Simulation.CommandsV1
{
    /// <summary>
    /// Minimal authoritative world view the state-dependent executor checks
    /// against at the target tick (docs/tech/Commands.md section 4). The real
    /// simulation state implements this in the G1 integration; tests implement
    /// it with deterministic doubles. <see cref="Apply"/> is the ONLY mutating
    /// member and is called exclusively after every check passed — a
    /// state-dependent failure therefore provably mutates nothing.
    /// </summary>
    public interface ICommandStateView
    {
        /// <summary>True when the raw entity id refers to a live entity at the target tick.</summary>
        bool EntityExists(uint rawEntityId);

        /// <summary>True when the entity belongs to the given player slot.</summary>
        bool IsOwnedBy(byte playerSlot, uint rawEntityId);

        /// <summary>True when the id names a registered Aetherium field (Harvest legality, state-dependent).</summary>
        bool AetheriumFieldExists(uint fieldId);

        /// <summary>True when the raw entity id refers to a live harvester (Harvest legality, state-dependent).</summary>
        bool IsHarvester(uint rawEntityId);

        /// <summary>True when the player can pay the cost implied by kind and definition id.</summary>
        bool CanAfford(byte playerSlot, CommandKind kind, ushort definitionId);

        /// <summary>
        /// Domain-specific state-dependent validation beyond the generic
        /// existence/ownership/cost checks (docs/tech/Commands.md section 4):
        /// placement legality, prerequisites, tier gating, queue capacity and
        /// per-kind target rules of the construction/production slice. Runs
        /// after the generic checks in the fixed executor order, mutates
        /// nothing and returns <see cref="CommandResultCode.Applied"/> when
        /// the command is legal for its domain.
        /// </summary>
        CommandResultCode ValidateDomain(in CommandRecord record, in CommandPayloadRefs refs);

        /// <summary>Applies the command to the authoritative state. Called only on success.</summary>
        void Apply(in CommandRecord record);
    }

    /// <summary>
    /// State-dependent evaluation of sealed records at their target tick
    /// (docs/tech/Commands.md section 4). Checks run in a fixed, deterministic
    /// order — acting entity existence, ownership, harvest field existence and
    /// harvester role, target existence, cost, domain validation — so the same
    /// record and the same state always yield the same <see cref="CommandResult"/>.
    /// A failed check produces the deterministic result, mutates nothing and
    /// stays in the replay.
    /// <para>
    /// Vision, range and cooldowns are additional state-dependent checks of
    /// the full contract; the schema-v1 view models existence, ownership,
    /// harvest legality, cost and the construction/production domain checks
    /// (<see cref="ICommandStateView.ValidateDomain"/>), and the result codes
    /// reserve the remaining cases for later slices.
    /// </para>
    /// </summary>
    public static class CommandExecutor
    {
        /// <summary>
        /// Evaluates one sealed record against the authoritative state. The
        /// record must already be structurally valid (sealed by the ingress);
        /// an unparseable payload after sealing is a programming error.
        /// </summary>
        public static CommandResult Execute(in CommandRecord record, ICommandStateView state)
        {
            if (!CommandPayloadValidation.TryExtractRefs(
                    record.Kind, record.Payload.Span, out CommandPayloadRefs refs))
            {
                throw new System.InvalidOperationException(
                    "Sealed record payload failed to parse; the ingress contract is broken.");
            }

            if (refs.ActingEntities != null)
            {
                for (int i = 0; i < refs.ActingEntities.Length; i++)
                {
                    if (!state.EntityExists(refs.ActingEntities[i]))
                    {
                        return new CommandResult(record, CommandResultCode.RejectedInvalidTarget);
                    }
                    if (!state.IsOwnedBy(record.PlayerSlot, refs.ActingEntities[i]))
                    {
                        return new CommandResult(record, CommandResultCode.RejectedNotOwned);
                    }
                }
            }

            if (refs.SingleEntityId != 0)
            {
                if (!state.EntityExists(refs.SingleEntityId))
                {
                    return new CommandResult(record, CommandResultCode.RejectedInvalidTarget);
                }
                if (!state.IsOwnedBy(record.PlayerSlot, refs.SingleEntityId))
                {
                    return new CommandResult(record, CommandResultCode.RejectedNotOwned);
                }
            }

            if (refs.TargetEntityId != 0 && !state.EntityExists(refs.TargetEntityId))
            {
                return new CommandResult(record, CommandResultCode.RejectedInvalidTarget);
            }

            if (record.Kind == CommandKind.Harvest)
            {
                // Harvest legality at the target tick: the named field must
                // exist (for Harvest the refs definition slot carries the
                // field id) and every acting entity must be a harvester.
                // Both failures are RejectedInvalidTarget — the order of the
                // two checks is fixed, so the result stays deterministic.
                if (!state.AetheriumFieldExists(refs.DefinitionId))
                {
                    return new CommandResult(record, CommandResultCode.RejectedInvalidTarget);
                }
                for (int i = 0; i < refs.ActingEntities.Length; i++)
                {
                    if (!state.IsHarvester(refs.ActingEntities[i]))
                    {
                        return new CommandResult(record, CommandResultCode.RejectedInvalidTarget);
                    }
                }
            }

            if (refs.DefinitionId != 0
                && (record.Kind == CommandKind.PlaceBuilding
                    || record.Kind == CommandKind.InstallDefenseModule)
                && !state.CanAfford(record.PlayerSlot, record.Kind, refs.DefinitionId))
            {
                return new CommandResult(record, CommandResultCode.RejectedInsufficientResources);
            }

            // Domain-specific state-dependent checks (placement legality,
            // prerequisites, tier gating, queue capacity and the QueueUnit
            // count-scaled cost — the count lives in the payload, so the
            // generic check above cannot cover it).
            CommandResultCode domainCode = state.ValidateDomain(in record, in refs);
            if (domainCode != CommandResultCode.Applied)
            {
                return new CommandResult(record, domainCode);
            }

            state.Apply(record);
            return new CommandResult(record, CommandResultCode.Applied);
        }

        /// <summary>
        /// Evaluates a sealed batch in canonical record order and returns one
        /// deterministic result per record.
        /// </summary>
        public static CommandResult[] ExecuteBatch(CommandBatch batch, ICommandStateView state)
        {
            var results = new CommandResult[batch.Count];
            for (int i = 0; i < batch.Count; i++)
            {
                results[i] = Execute(batch.Records[i], state);
            }
            return results;
        }
    }
}
