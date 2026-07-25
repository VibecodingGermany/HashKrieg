using System;
using System.Collections.Generic;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Snapshots;

namespace Nova.Simulation
{
    /// <summary>
    /// Canonical lockstep simulation kernel (docs/tech/SimulationCore.md).
    /// Manages deterministic 10 Hz tick stepping, the single command intake,
    /// the authoritative state hash and kernel-level snapshots.
    /// Engine-decoupled (no UnityEngine dependency).
    /// <para>
    /// Command intake (Commands.md section 1): the ONLY way commands enter
    /// the simulation is <see cref="SubmitBatch"/> with a sealed
    /// <see cref="CommandBatch"/> produced by <see cref="CommandIngress"/>.
    /// Systems never receive commands; the kernel applies due batches itself
    /// in tick phase 1 via <see cref="CommandExecutor"/> against the
    /// <see cref="ICommandStateView"/> bound with <see cref="BindCommands"/>,
    /// then runs the registered systems in registration order
    /// (SimulationCore.md section 2, as far as the systems of this slice
    /// exist). A record's TargetTick is its earliest execution tick
    /// (Commands.md section 2), so a batch is due when
    /// TargetTick &lt;= CurrentTick.
    /// </para>
    /// <para>
    /// State hash (SimulationCore.md sections 5 and 7):
    /// <see cref="CalculateStateHash"/> is the canonical NOVA_STATE_V1
    /// container state hash over the same blocks <see cref="SaveSnapshot"/>
    /// emits — the kernel block (tick, PRNG words, ingress dedupe/sequence
    /// state, pending batches) plus one block per
    /// <see cref="IStatefulSimSystem"/>. Hashing only reads the PRNG words;
    /// it never advances the sequence, so repeated hashes are identical.
    /// </para>
    /// </summary>
    public sealed class SimulationKernel
    {
        /// <summary>Serialization version of the kernel snapshot block.</summary>
        public const byte KernelStateVersion = 1;

        private readonly List<ISimSystem> _systems = new List<ISimSystem>();
        private readonly List<IStatefulSimSystem> _statefulSystems = new List<IStatefulSimSystem>();
        private readonly List<CommandBatch> _pendingBatches = new List<CommandBatch>();
        private CommandResult[] _lastTickResults = Array.Empty<CommandResult>();

        private ICommandStateView _commandStateView;
        private CommandIngress _ingress;
        private bool _started;

        public Tick CurrentTick { get; private set; } = Tick.Zero;

        /// <summary>
        /// The deterministic PRNG (concrete <see cref="SimRandom"/>): the
        /// kernel reads and restores its state words for hashing and
        /// snapshots without ever advancing the sequence outside ticks.
        /// </summary>
        public SimRandom Random { get; }

        public INovaLogger Logger { get; }
        public bool IsRunning { get; private set; }

        public IReadOnlyList<ISimSystem> Systems => _systems;

        /// <summary>Sealed batches accepted and waiting for their target tick.</summary>
        public int PendingBatchCount => _pendingBatches.Count;

        /// <summary>Deterministic results of the batches applied in the most recent tick.</summary>
        public IReadOnlyList<CommandResult> LastTickResults => _lastTickResults;

        public SimulationKernel(SimRandom random, INovaLogger logger = null)
        {
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Logger = logger ?? NullNovaLogger.Instance;
        }

        /// <summary>
        /// Binds the command execution pipeline: the state view the executor
        /// applies due batches against, and optionally the ingress whose
        /// authoritative dedupe/sequence state becomes part of the kernel
        /// block in hashes and snapshots (SimulationCore.md section 3).
        /// Callable once, before <see cref="Start"/>.
        /// </summary>
        public void BindCommands(ICommandStateView stateView, CommandIngress ingress = null)
        {
            if (stateView == null) throw new ArgumentNullException(nameof(stateView));
            if (_started) throw new InvalidOperationException("Cannot bind the command pipeline after Start.");
            if (_commandStateView != null) throw new InvalidOperationException("Command pipeline already bound.");
            _commandStateView = stateView;
            _ingress = ingress;
        }

        /// <summary>
        /// Registers a system. Registration checklist: any system that holds
        /// match-relevant state MUST implement <see cref="IStatefulSimSystem"/>
        /// — otherwise its state is missing from the canonical state hash and
        /// from snapshots, and restored hosts silently desync. The prototype
        /// scaffolding systems (Combat, Economy, Production — with
        /// known 20 Hz and float relics) deliberately do NOT satisfy this yet
        /// and must be migrated in their domain slices BEFORE they may be
        /// registered here.
        /// <para>
        /// Tick order (SimulationCore.md section 2): systems execute in exact
        /// registration order, so the registration order must mirror the
        /// canonical pipeline — pathfinding/movement first, then the Fog of
        /// War system (its 5 Hz recompute sees the final positions of the
        /// tick), then combat and projectiles, then match/victory logic. A
        /// FoW system registered before movement would recompute on stale
        /// positions; a combat system registered before FoW would judge
        /// targeting on the previous recompute.
        /// </para>
        /// </summary>
        public void RegisterSystem(ISimSystem system)
        {
            if (system == null) throw new ArgumentNullException(nameof(system));
            if (_started) throw new InvalidOperationException("Cannot register systems after the kernel was started.");

            if (system is IStatefulSimSystem stateful)
            {
                if (stateful.StateBlockId == SnapshotBlockIds.Kernel)
                {
                    throw new ArgumentException(
                        $"System '{system.Name}' claims the reserved kernel snapshot block id.",
                        nameof(system));
                }
                for (int i = 0; i < _statefulSystems.Count; i++)
                {
                    if (_statefulSystems[i].StateBlockId == stateful.StateBlockId)
                    {
                        throw new ArgumentException(
                            $"Duplicate snapshot block id {stateful.StateBlockId} " +
                            $"('{_statefulSystems[i].Name}' vs '{system.Name}').",
                            nameof(system));
                    }
                }
                _statefulSystems.Add(stateful);
            }

            _systems.Add(system);
        }

        public void Start(Tick initialTick = default)
        {
            CurrentTick = initialTick;
            _started = true;
            IsRunning = true;
            _pendingBatches.Clear();
            _lastTickResults = Array.Empty<CommandResult>();

            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Initialize(this);
            }

            Logger.LogInfo($"SimulationKernel started at {CurrentTick} with {_systems.Count} systems.");
        }

        /// <summary>
        /// The single command intake (Commands.md section 1): accepts a
        /// sealed, validated batch for application at its target tick.
        /// Returns false when the kernel is not running, when a batch for the
        /// same target tick is already pending (one sealed batch per tick
        /// from the single ingress authority) or when the pending-record cap
        /// would be exceeded. Throws when no command pipeline is bound.
        /// </summary>
        public bool SubmitBatch(CommandBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (_commandStateView == null)
            {
                throw new InvalidOperationException(
                    "No command pipeline bound; call BindCommands before submitting batches.");
            }
            if (!IsRunning) return false;
            if (batch.Count == 0) return true; // empty tick batch: nothing to apply

            int pendingRecords = 0;
            for (int i = 0; i < _pendingBatches.Count; i++)
            {
                if (_pendingBatches[i].TargetTick == batch.TargetTick)
                {
                    return false;
                }
                pendingRecords += _pendingBatches[i].Count;
            }
            if (pendingRecords + batch.Count > CommandLimits.MaxPendingRecords)
            {
                return false;
            }

            _pendingBatches.Add(batch);
            return true;
        }

        /// <summary>
        /// Advances the simulation by exactly one tick (10 Hz,
        /// SimulationCore.md section 2): first validates and applies every
        /// due sealed batch through the canonical executor, then runs the
        /// registered systems in exact registration order.
        /// </summary>
        public void StepTick()
        {
            if (!IsRunning) return;

            CurrentTick++;
            _lastTickResults = ApplyDueBatches();

            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].ExecuteTick(CurrentTick);
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            for (int i = _systems.Count - 1; i >= 0; i--)
            {
                _systems[i].Shutdown();
            }

            IsRunning = false;
            Logger.LogInfo($"SimulationKernel stopped at {CurrentTick}.");
        }

        /// <summary>
        /// Canonical state hash (SimulationCore.md sections 5 and 7):
        /// NOVA_STATE_V1 over the same blocks <see cref="SaveSnapshot"/>
        /// serializes — identical to the snapshot header state hash. Purely
        /// read-only: the PRNG state words are hashed, never advanced, so
        /// repeated calls return the identical value.
        /// </summary>
        public ulong CalculateStateHash()
        {
            BuildStateBlocks(out ushort[] ids, out byte[][] contents);
            return SnapshotWriter.ComputeStateHash(ids, contents);
        }

        /// <summary>
        /// Serializes the complete authoritative state into the canonical
        /// snapshot container v1 (SimulationCore.md section 7): the kernel
        /// block (tick, PRNG words, ingress dedupe/sequence state, pending
        /// batches as canonical record bytes) plus one block per registered
        /// <see cref="IStatefulSimSystem"/>.
        /// </summary>
        public byte[] SaveSnapshot()
        {
            BuildStateBlocks(out ushort[] ids, out byte[][] contents);
            var writer = new SnapshotWriter();
            for (int i = 0; i < ids.Length; i++)
            {
                writer.AddBlock(ids[i], contents[i]);
            }
            return writer.ToArray();
        }

        /// <summary>
        /// Restores a snapshot produced by <see cref="SaveSnapshot"/>.
        /// Requires a started kernel (systems initialized) with the same
        /// stateful system set: every file block must be claimed by the
        /// kernel or a registered stateful system and every stateful system
        /// must find its block — a mismatch rejects the whole file.
        /// <para>
        /// The restore is strictly two-phase (docs/tech/Serialization.md
        /// section 5: no partial state). Phase A validates EVERYTHING without
        /// mutating anything: the hardened container parse (lengths, block
        /// hashes, state hash), the complete kernel block into locals, the
        /// ingress state via <see cref="CommandIngress.TryValidateState"/> and
        /// every system block via
        /// <see cref="IStatefulSimSystem.TryValidateState"/>. Phase B commits
        /// only after every validation passed. A failure anywhere in phase A
        /// returns false and the running host is guaranteed unchanged — no
        /// franken-state. A failure inside phase B is impossible by the
        /// <see cref="IStatefulSimSystem"/> contract (commit of already
        /// validated identical bytes) and is surfaced as an
        /// <see cref="InvalidOperationException"/> (implementation bug, never
        /// bad input).
        /// </para>
        /// </summary>
        public bool TryRestoreSnapshot(byte[] snapshotBytes)
        {
            if (snapshotBytes == null) throw new ArgumentNullException(nameof(snapshotBytes));
            if (!_started)
            {
                return false; // systems must be initialized to absorb state
            }

            // ---- Phase A: validate everything, commit nothing. ----
            if (!SnapshotReader.TryRead(snapshotBytes, out SnapshotFile snapshot, out _))
            {
                return false;
            }

            // Exact block coverage: kernel + every registered stateful system.
            if (snapshot.Blocks.Count != 1 + _statefulSystems.Count)
            {
                return false;
            }
            if (!snapshot.TryGetBlock(SnapshotBlockIds.Kernel, out byte[] kernelBlock))
            {
                return false;
            }
            for (int i = 0; i < _statefulSystems.Count; i++)
            {
                if (!snapshot.TryGetBlock(_statefulSystems[i].StateBlockId, out _))
                {
                    return false;
                }
            }

            // Parse and validate the kernel block completely into locals.
            if (!TryParseKernelBlock(kernelBlock, out Tick restoredTick,
                    out ulong prngS0, out ulong prngS1,
                    out byte[] ingressState, out List<CommandBatch> restoredBatches))
            {
                return false;
            }
            if (prngS0 == 0UL && prngS1 == 0UL)
            {
                return false; // degenerate xorshift128+ state
            }
            if (ingressState.Length > 0
                && (_ingress == null || !_ingress.TryValidateState(ingressState)))
            {
                return false;
            }
            for (int i = 0; i < _statefulSystems.Count; i++)
            {
                snapshot.TryGetBlock(_statefulSystems[i].StateBlockId, out byte[] block);
                if (!_statefulSystems[i].TryValidateState(block))
                {
                    return false;
                }
            }

            // ---- Phase B: everything validated; commit. ----
            // The commit calls below re-validate the identical bytes that
            // just passed phase A, so by contract they cannot fail; a false
            // return is an implementation bug, not bad input.
            if (ingressState.Length > 0 && !_ingress.TryRestoreState(ingressState))
            {
                throw new InvalidOperationException(
                    "Ingress state failed to commit after a successful validation; " +
                    "the CommandIngress two-phase contract is broken.");
            }

            CurrentTick = restoredTick;
            Random.SetState(prngS0, prngS1);
            _pendingBatches.Clear();
            _pendingBatches.AddRange(restoredBatches);
            _lastTickResults = Array.Empty<CommandResult>();

            for (int i = 0; i < _statefulSystems.Count; i++)
            {
                snapshot.TryGetBlock(_statefulSystems[i].StateBlockId, out byte[] block);
                if (!_statefulSystems[i].TryRestoreState(block))
                {
                    throw new InvalidOperationException(
                        $"System '{_statefulSystems[i].Name}' failed to commit a block it " +
                        "successfully validated; the IStatefulSimSystem two-phase contract is broken.");
                }
            }

            Logger.LogInfo($"SimulationKernel restored snapshot at {CurrentTick}.");
            return true;
        }

        /// <summary>
        /// Applies every pending batch with TargetTick &lt;= CurrentTick in
        /// canonical (TargetTick, PlayerSlot, Sequence) record order and
        /// returns one deterministic result per record (Commands.md section
        /// 4). Due records across batches are re-sorted into one canonical
        /// sequence, so the execution order never depends on submission
        /// order.
        /// </summary>
        private CommandResult[] ApplyDueBatches()
        {
            if (_pendingBatches.Count == 0) return Array.Empty<CommandResult>();

            List<CommandRecord> dueRecords = null;
            for (int i = _pendingBatches.Count - 1; i >= 0; i--)
            {
                if (_pendingBatches[i].TargetTick > CurrentTick.Value) continue;
                CommandBatch due = _pendingBatches[i];
                if (dueRecords == null) dueRecords = new List<CommandRecord>(due.Count);
                for (int r = 0; r < due.Count; r++)
                {
                    dueRecords.Add(due.Records[r]);
                }
                _pendingBatches.RemoveAt(i);
            }

            if (dueRecords == null) return Array.Empty<CommandResult>();

            dueRecords.Sort(CommandBatch.CompareRecords);
            var results = new CommandResult[dueRecords.Count];
            for (int i = 0; i < dueRecords.Count; i++)
            {
                results[i] = CommandExecutor.Execute(dueRecords[i], _commandStateView);
            }
            return results;
        }

        /// <summary>
        /// Builds the canonical block set (ascending BlockId) shared by
        /// <see cref="CalculateStateHash"/> and <see cref="SaveSnapshot"/> so
        /// the live hash and the snapshot header hash can never drift apart.
        /// </summary>
        private void BuildStateBlocks(out ushort[] ids, out byte[][] contents)
        {
            int count = 1 + _statefulSystems.Count;
            var rawIds = new ushort[count];
            var rawContents = new byte[count][];

            rawIds[0] = SnapshotBlockIds.Kernel;
            rawContents[0] = BuildKernelBlock();

            for (int i = 0; i < _statefulSystems.Count; i++)
            {
                var blockWriter = new SnapshotBlockWriter();
                _statefulSystems[i].WriteState(blockWriter);
                rawIds[i + 1] = _statefulSystems[i].StateBlockId;
                rawContents[i + 1] = blockWriter.ToArray();
            }

            // Canonical order: strictly ascending BlockId, independent of
            // system registration order (ids are unique — enforced at
            // registration — so the sort is fully determined).
            var order = new int[count];
            for (int i = 0; i < count; i++) order[i] = i;
            Array.Sort(order, (a, b) => rawIds[a].CompareTo(rawIds[b]));

            var sortedIds = new ushort[count];
            var sortedContents = new byte[count][];
            for (int i = 0; i < count; i++)
            {
                sortedIds[i] = rawIds[order[i]];
                sortedContents[i] = rawContents[order[i]];
            }
            ids = sortedIds;
            contents = sortedContents;
        }

        /// <summary>
        /// Kernel block content v1: block version, current tick, both PRNG
        /// state words, the bound ingress state (dedupe/sequence state with
        /// pending records; empty when unbound) and the pending sealed
        /// batches in ascending TargetTick order as canonical record bytes.
        /// </summary>
        private byte[] BuildKernelBlock()
        {
            Random.GetState(out ulong s0, out ulong s1);
            byte[] ingressState = _ingress != null ? _ingress.SerializeState() : Array.Empty<byte>();

            var writer = new SnapshotBlockWriter();
            writer.WriteUInt8(KernelStateVersion);
            writer.WriteTick(CurrentTick);
            writer.WriteUInt64(s0);
            writer.WriteUInt64(s1);
            writer.WriteLengthPrefixed(ingressState);

            // Canonical pending-batch order: ascending TargetTick (unique —
            // enforced at intake — so the order is fully determined).
            var order = new int[_pendingBatches.Count];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            Array.Sort(order, (a, b) => _pendingBatches[a].TargetTick.CompareTo(_pendingBatches[b].TargetTick));

            writer.WriteUInt16(unchecked((ushort)_pendingBatches.Count));
            for (int i = 0; i < order.Length; i++)
            {
                CommandBatch batch = _pendingBatches[order[i]];
                writer.WriteUInt32(batch.TargetTick);
                writer.WriteUInt16(unchecked((ushort)batch.Count));
                for (int r = 0; r < batch.Count; r++)
                {
                    writer.WriteLengthPrefixed(batch.Records[r].Serialize());
                }
            }
            return writer.ToArray();
        }

        /// <summary>
        /// Parses and fully validates a kernel block without mutating
        /// anything: exact lengths, a future target tick per pending batch,
        /// structurally revalidated records (Commands.md section 4),
        /// canonical per-batch record order and the stream capacity limits.
        /// </summary>
        private bool TryParseKernelBlock(
            byte[] content, out Tick restoredTick,
            out ulong prngS0, out ulong prngS1,
            out byte[] ingressState, out List<CommandBatch> restoredBatches)
        {
            restoredTick = Tick.Zero;
            prngS0 = 0UL;
            prngS1 = 0UL;
            ingressState = Array.Empty<byte>();
            restoredBatches = null;

            var reader = new SnapshotBlockReader(content);
            if (!reader.TryReadUInt8(out byte version) || version != KernelStateVersion) return false;
            if (!reader.TryReadTick(out Tick tick)) return false;
            if (!reader.TryReadUInt64(out ulong s0)) return false;
            if (!reader.TryReadUInt64(out ulong s1)) return false;
            if (!reader.TryReadLengthPrefixed(out ReadOnlySpan<byte> ingressBytes)) return false;

            if (!reader.TryReadUInt16(out ushort batchCount)) return false;
            var batches = new List<CommandBatch>(batchCount);
            uint previousTargetTick = 0;
            int totalPendingRecords = 0;
            for (int b = 0; b < batchCount; b++)
            {
                if (!reader.TryReadUInt32(out uint targetTick)) return false;
                // Pending batches must lie in the restored future and be unique per tick.
                if (targetTick <= tick.Value) return false;
                if (b > 0 && targetTick <= previousTargetTick) return false;
                previousTargetTick = targetTick;

                if (!reader.TryReadUInt16(out ushort recordCount)) return false;
                if (recordCount == 0 || recordCount > CommandLimits.MaxBatchRecordsPerTick) return false;
                totalPendingRecords += recordCount;
                if (totalPendingRecords > CommandLimits.MaxPendingRecords) return false;

                var records = new CommandRecord[recordCount];
                for (int r = 0; r < recordCount; r++)
                {
                    if (!reader.TryReadLengthPrefixed(out ReadOnlySpan<byte> recordBytes)) return false;
                    if (!CommandRecord.TryDeserialize(recordBytes, out CommandRecord record, out int consumed)) return false;
                    if (consumed != recordBytes.Length) return false;
                    if (!CommandPayloadValidation.TryValidateStreamPayload(
                            record.Kind, record.PayloadVersion, record.Payload.Span, out _)) return false;
                    if (record.Sequence == 0) return false;
                    if (record.TargetTick != targetTick) return false;
                    if (r > 0 && CommandBatch.CompareRecords(records[r - 1], record) >= 0) return false;
                    records[r] = record;
                }
                batches.Add(new CommandBatch(targetTick, records));
            }
            if (reader.Remaining != 0) return false;

            restoredTick = tick;
            prngS0 = s0;
            prngS1 = s1;
            ingressState = ingressBytes.ToArray();
            restoredBatches = batches;
            return true;
        }
    }
}
