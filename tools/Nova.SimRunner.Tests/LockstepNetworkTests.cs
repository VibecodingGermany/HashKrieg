using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using NUnit.Framework;
using Nova.Core;
using Nova.Networking;
using Nova.Simulation;
using Nova.Simulation.Combat;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Construction;
using Nova.Simulation.Definitions;
using Nova.Simulation.Economy;
using Nova.Simulation.Movement;
using Nova.Simulation.Pathfinding;
using Nova.Simulation.Production;
using Nova.Simulation.Replays;
using Nova.Simulation.Snapshots;
using Nova.Simulation.State;
using Nova.Simulation.Vision;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Lockstep network soak (.NET lane; sprint 12 strand A, proof stage 1):
    /// two complete simulation clients — each with its own kernel, session,
    /// ingress and <see cref="RelayMatchClient"/> — play one scripted match
    /// over REAL loopback TCP through an in-process <see cref="RelayServerCore"/>.
    /// The canonical state hashes of both clients must be bit-identical at
    /// every 50-tick checkpoint and through tick 10.023: this is the
    /// CI-capable proof that the lockstep barrier, the TickComplete transport
    /// frame and the relay validation hold a two-human match together.
    /// <para>
    /// The stall behaviour is proven separately: a client that goes silent
    /// visibly stalls the other (never diverges) and the match resumes
    /// bit-identically once the peer is back.
    /// </para>
    /// </summary>
    [TestFixture]
    public sealed class LockstepNetworkTests
    {
        private const ulong Token = 0xA11CE42UL;
        private const ulong Seed = 0x5EED42UL;
        private const uint Delay = 3;

        [Test]
        public void NetworkTransportContract_RequiresToken_AndExposesRoundTripTicks()
        {
            Assert.That(typeof(INetworkTransport).GetMethod(
                "Connect", new[] { typeof(string), typeof(int), typeof(ulong) }), Is.Not.Null);
            Assert.That(typeof(INetworkTransport).GetMethod(
                "Connect", new[] { typeof(string), typeof(int) }), Is.Null,
                "the interface must not retain a throw-only tokenless path");
            Assert.That(typeof(INetworkTransport).GetProperty("RoundTripTicks"), Is.Not.Null);
        }

        [Test]
        public void TcpConnect_IsPollDriven_AndDisconnectCancelsAnInflightAttempt()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var connection = new TcpRelayConnection();
            TcpClient accepted = null;
            try
            {
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Assert.That(connection.Connect("127.0.0.1", port), Is.True);
                Assert.That(connection.State, Is.EqualTo(RelayConnectionState.Connecting),
                    "Connect must only begin the socket operation; Poll owns completion");

                accepted = listener.AcceptTcpClient();
                long guard = 100_000;
                while (connection.State == RelayConnectionState.Connecting && guard-- > 0)
                {
                    connection.Poll();
                    System.Threading.Thread.Yield();
                }
                Assert.That(guard, Is.GreaterThan(0));
                Assert.That(connection.State, Is.EqualTo(RelayConnectionState.Connected));
            }
            finally
            {
                connection.Disconnect();
                accepted?.Dispose();
                listener.Stop();
            }

            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                var cancelled = new TcpRelayConnection();
                int port = ((IPEndPoint)listener.LocalEndpoint).Port;
                Assert.That(cancelled.Connect("127.0.0.1", port), Is.True);
                Assert.That(cancelled.State, Is.EqualTo(RelayConnectionState.Connecting));

                cancelled.Disconnect();
                cancelled.Poll();

                Assert.That(cancelled.State, Is.EqualTo(RelayConnectionState.Disconnected));
                Assert.That(cancelled.LastError, Is.Null);
            }
            finally
            {
                listener.Stop();
            }
        }

        [Test]
        public void BoundRelayClient_RefusesReuseWithoutOpeningASocket()
        {
            var client = new RelayMatchClient();
            var session = new MatchSession(0, new byte[] { 0, 1 }, Delay);
            client.BindIngress(new CommandIngress(session));

            client.Connect("127.0.0.1", 47777, Token);

            Assert.That(client.Phase, Is.EqualTo(RelayClientPhase.Ended));
            Assert.That(client.EndReason, Does.Contain("fresh client"));
            Assert.That(client.Lifecycle, Is.EqualTo(RelayMatchLifecycle.Ended));
            Assert.That(client.LastError, Is.EqualTo(client.EndReason));
        }

        [Test]
        public void MatchTokenAndDelayBoundaries_AreExactAndFailClosed()
        {
            Assert.That(RelayProtocol.TryParseMatchToken(
                "0123456789ABCDEF", out ulong parsed), Is.True);
            Assert.That(parsed, Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(RelayProtocol.TryParseMatchToken("0x0123456789ABCDEF", out _), Is.False);
            Assert.That(RelayProtocol.TryParseMatchToken("123456789ABCDEF", out _), Is.False);
            Assert.That(RelayProtocol.TryParseMatchToken("0123456789ABCDEFG", out _), Is.False);
            Assert.That(RelayProtocol.TryParseMatchToken(" 123456789ABCDEF", out _), Is.False);
            Assert.That(RelayProtocol.TryParseMatchToken("0000000000000000", out _), Is.False);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelayProtocol.CreateOfferPayload(0, new byte[] { 0, 1 }, Seed, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelayProtocol.CreateOfferPayload(0, new byte[] { 0, 1 }, Seed, 61, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelayProtocol.CreateOfferPayload(0, new byte[] { 0, 1 }, Seed, uint.MaxValue, 1));

            byte[] forgedOffer = RelayProtocol.CreateOfferPayload(
                0, new byte[] { 0, 1 }, Seed, RelayProtocol.MaxInputDelayTicks, 1);
            RelayProtocol.WriteUInt32(forgedOffer, 12, uint.MaxValue);
            Assert.That(RelayProtocol.TryParseOffer(
                forgedOffer, out _, out _, out _, out _, out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RelayProtocol.CreateTickCompletePayload(0, 1, CommandLimits.MaxBatchRecordsPerTick + 1));
        }

        [Test]
        public void FrameCutter_AcceptsAnExactMaximumPayloadAcrossTheGrowthBoundary()
        {
            var frame = new byte[RelayProtocol.HeaderBytes + RelayProtocol.MaxFramePayloadBytes];
            RelayProtocol.WriteUInt32(
                frame, 0, unchecked((uint)RelayProtocol.MaxFramePayloadBytes));
            frame[4] = (byte)RelayFrameType.InitialSnapshot;
            frame[RelayProtocol.HeaderBytes] = 0xA5;
            frame[frame.Length - 1] = 0x5A;
            var cutter = new RelayProtocol.FrameCutter();

            cutter.Feed(frame.AsSpan(0, RelayProtocol.MaxFramePayloadBytes));
            Assert.That(cutter.TryTakeFrame(out _, out _), Is.False);
            cutter.Feed(frame.AsSpan(
                RelayProtocol.MaxFramePayloadBytes, RelayProtocol.HeaderBytes));

            Assert.That(cutter.TryTakeFrame(
                out RelayFrameType type, out byte[] payload), Is.True);
            Assert.That(type, Is.EqualTo(RelayFrameType.InitialSnapshot));
            Assert.That(payload, Has.Length.EqualTo(RelayProtocol.MaxFramePayloadBytes));
            Assert.That(payload[0], Is.EqualTo(0xA5));
            Assert.That(payload[payload.Length - 1], Is.EqualTo(0x5A));
            Assert.That(cutter.TryTakeFrame(out _, out _), Is.False);
        }

        [Test]
        public void FrameCutter_RejectsAnOversizedDeclarationFromTheHeaderAlone()
        {
            var header = new byte[RelayProtocol.HeaderBytes];
            RelayProtocol.WriteUInt32(
                header, 0, RelayProtocol.MaxFramePayloadBytes + 1u);
            header[4] = (byte)RelayFrameType.InitialSnapshot;
            var cutter = new RelayProtocol.FrameCutter();

            cutter.Feed(header);

            Assert.Throws<RelayFrameFormatException>(() =>
                cutter.TryTakeFrame(out _, out _));
        }

        [Test]
        public void FrameCutter_DrainsAMaximumFrameBeforeCoalescedFollowingBytes()
        {
            var maximumFrame = new byte[
                RelayProtocol.HeaderBytes + RelayProtocol.MaxFramePayloadBytes];
            RelayProtocol.WriteUInt32(
                maximumFrame, 0, unchecked((uint)RelayProtocol.MaxFramePayloadBytes));
            maximumFrame[4] = (byte)RelayFrameType.InitialSnapshot;
            maximumFrame[RelayProtocol.HeaderBytes] = 0x11;
            maximumFrame[maximumFrame.Length - 1] = 0x22;
            byte[] nextFrame = RelayProtocol.CreateFrame(
                RelayFrameType.Ping, RelayProtocol.CreatePingPayload(0xA1B2C3D4));
            var available = new byte[RelayProtocol.HeaderBytes + nextFrame.Length];
            Array.Copy(
                maximumFrame, RelayProtocol.MaxFramePayloadBytes,
                available, 0, RelayProtocol.HeaderBytes);
            Array.Copy(nextFrame, 0, available, RelayProtocol.HeaderBytes, nextFrame.Length);
            var cutter = new RelayProtocol.FrameCutter();
            cutter.Feed(maximumFrame.AsSpan(0, RelayProtocol.MaxFramePayloadBytes));

            Assert.Throws<RelayFrameFormatException>(() => cutter.Feed(available),
                "bounded carry must make callers drain instead of buffering a second frame");

            var types = new List<RelayFrameType>();
            var payloads = new List<byte[]>();
            int offset = 0;
            while (offset < available.Length)
            {
                int count = Math.Min(
                    available.Length - offset, cutter.RemainingCapacity);
                Assert.That(count, Is.GreaterThan(0));
                cutter.Feed(available.AsSpan(offset, count));
                offset += count;
                while (cutter.TryTakeFrame(
                    out RelayFrameType type, out byte[] payload))
                {
                    types.Add(type);
                    payloads.Add(payload);
                }
            }

            Assert.That(types, Is.EqualTo(new[]
            {
                RelayFrameType.InitialSnapshot,
                RelayFrameType.Ping,
            }));
            Assert.That(payloads[0], Has.Length.EqualTo(RelayProtocol.MaxFramePayloadBytes));
            Assert.That(payloads[0][0], Is.EqualTo(0x11));
            Assert.That(payloads[0][payloads[0].Length - 1], Is.EqualTo(0x22));
            Assert.That(RelayProtocol.TryParsePing(
                payloads[1], out uint probe), Is.True);
            Assert.That(probe, Is.EqualTo(0xA1B2C3D4u));
        }

        [Test]
        public void Ingress_BeforeAuthoritativeStart_IsSideEffectFree_ThenStartsAtSequenceOne()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var clientA = new RelayMatchClient();
            var clientB = new RelayMatchClient();
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, clientA, clientB,
                () => clientA.HasOffer && clientB.HasOffer, "offers");
            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);

            clientA.SubmitLocalProof(
                hostA.CreateFingerprint().Serialize(), hostA.Kernel.SaveSnapshot());
            Assert.That(clientA.Phase, Is.EqualTo(RelayClientPhase.WaitingStart));

            var stop = new StopPayload(new[] { hostA.BuilderRaw });
            Assert.That(
                hostA.Ingress.TrySubmitIntent(
                    CommandIntent.Create(stop), out CommandRejectReason streamReason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(streamReason, Is.EqualTo(CommandRejectReason.TransportNotReady));
            Assert.That(
                hostA.Ingress.TrySubmitIntent(
                    CommandIntent.ForSessionAction(CommandKind.PauseRequest),
                    out CommandRejectReason actionReason),
                Is.EqualTo(CommandIngressResult.Rejected));
            Assert.That(actionReason, Is.EqualTo(CommandRejectReason.TransportNotReady));
            Assert.That(hostA.Ingress.DedupeState.NextLocalSequence(clientA.AssignedSlot), Is.EqualTo(1));
            Assert.That(hostA.Ingress.PendingCount, Is.Zero);
            Assert.That(hostA.Ingress.PendingSessionActionCount, Is.Zero);
            Assert.That(hostA.Session.CurrentTick, Is.Zero);
            server.Poll();
            Assert.That(server.PeerCount, Is.EqualTo(2),
                "a pre-Start socket frame would make the relay drop the peer");

            clientB.SubmitLocalProof(
                hostB.CreateFingerprint().Serialize(), hostB.Kernel.SaveSnapshot());
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Running
                    && clientB.Phase == RelayClientPhase.Running,
                "authoritative Start");
            Assert.That(
                hostA.Ingress.TrySubmitIntent(
                    CommandIntent.Create(stop), out CommandRejectReason acceptedReason),
                Is.EqualTo(CommandIngressResult.Accepted), acceptedReason.ToString());
            Assert.That(hostA.Ingress.DedupeState.TryGetPending(
                clientA.AssignedSlot, 1, out CommandRecord first), Is.True);
            Assert.That(first.Sequence, Is.EqualTo(1));
            Assert.That(first.EnqueueTick, Is.Zero);
            Assert.That(first.TargetTick, Is.EqualTo(Delay));

            PumpUntil(server, clientA, clientB,
                () => hostB.Ingress.DedupeState.IsPending(clientA.AssignedSlot, 1),
                "first post-Start command reaches the peer");
            Drive(server, clientA, clientB, hostA, hostB, Delay);
            DriveUntilLevel(server, clientA, clientB, hostA, hostB);
            Assert.That(hostA.Ingress.DedupeState.SealedWatermark(clientA.AssignedSlot), Is.EqualTo(1));
            Assert.That(hostB.Ingress.DedupeState.SealedWatermark(clientA.AssignedSlot), Is.EqualTo(1));
            server.Stop();
        }

        /// <summary>Full client host: the canonical system set plus the relay client engine.</summary>
        private sealed class ClientHost
        {
            public SimulationKernel Kernel;
            public EntityManager Entities;
            public EconomySystem Economy;
            public ConstructionSystem Construction;
            public ProductionSystem Production;
            public MatchSession Session;
            public CommandIngress Ingress;
            public RelayMatchClient Client;
            public uint BuilderRaw;
            public uint SoldierRaw;
            public uint HqRaw;

            public static ClientHost Create(RelayMatchClient client)
            {
                var entities = new EntityManager(256);
                var pathfinding = new PathfindingSystem(128, 128);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
                var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
                var production = new ProductionSystem(entities, economy, construction);
                var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, 128, 128);
                var combat = new CombatSystem(entities, fogOfWar, economy);

                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(construction);
                kernel.RegisterSystem(production);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fogOfWar);
                kernel.RegisterSystem(combat);

                var session = new MatchSession(client.AssignedSlot, client.ActiveSlots, client.InputDelayTicks);
                var ingress = new CommandIngress(session);
                client.BindIngress(ingress);
                kernel.BindCommands(
                    new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);

                // Identical factions on both clients, before Kernel.Start()
                // (the SetSlotFaction guard; the faction bytes hash).
                economy.SetSlotFaction(0, FactionId.Alliance);
                economy.SetSlotFaction(1, FactionId.Legion);
                kernel.Start();

                // Identical opening on both clients: field, HQ, builder and
                // one infantry per slot (mirrors the canonical graybox
                // opening, both base corners).
                var host = new ClientHost
                {
                    Kernel = kernel,
                    Entities = entities,
                    Economy = economy,
                    Construction = construction,
                    Production = production,
                    Session = session,
                    Ingress = ingress,
                    Client = client,
                };
                for (byte slot = 0; slot < 2; slot++)
                {
                    ushort fieldId = (ushort)(slot + 1);
                    int fieldCell = slot == 0 ? 7 : 119;
                    int hqOrigin = slot == 0 ? 4 : 120;
                    Assert.That(economy.TryAddField(fieldId, new GridPos2D(fieldCell, fieldCell), 9000), Is.True);
                    FactionId faction = economy.GetSlotFaction(slot);
                    EntityId hq = construction.PlaceCompletedBuilding(
                        slot, SimDefinitions.ToDefinitionId(faction, UnitRole.HQ), hqOrigin, hqOrigin);
                    Assert.That(hq.IsValid, Is.True);
                    SimDefinitions.TryGetUnit(faction, UnitRole.Builder, out SimUnitDefinition builderDef);
                    EntityId builder = entities.SpawnUnit(slot,
                        new Transform2D(SimFixed.FromInt(slot == 0 ? 13 : 113), SimFixed.FromInt(slot == 0 ? 7 : 119)),
                        builderDef.MoveSpeed, maxHealth: builderDef.MaxHealth, role: UnitRole.Builder);
                    SimDefinitions.TryGetUnit(faction, UnitRole.BasicInfantry, out SimUnitDefinition infantryDef);
                    EntityId infantry = entities.SpawnUnit(slot,
                        new Transform2D(SimFixed.FromInt(slot == 0 ? 10 : 110), SimFixed.FromInt(slot == 0 ? 10 : 110)),
                        infantryDef.MoveSpeed, maxHealth: infantryDef.MaxHealth, role: UnitRole.BasicInfantry);
                    if (slot == client.AssignedSlot)
                    {
                        host.HqRaw = UnitCommandStateView.ToRawEntityId(hq);
                        host.BuilderRaw = UnitCommandStateView.ToRawEntityId(builder);
                        host.SoldierRaw = UnitCommandStateView.ToRawEntityId(infantry);
                    }
                }
                return host;
            }

            /// <summary>Fresh engine-only host for NOVAREC2 playback; the embedded snapshot supplies all match state.</summary>
            public static ClientHost CreatePlayback()
            {
                var entities = new EntityManager(256);
                var pathfinding = new PathfindingSystem(128, 128);
                var movement = new MovementSystem(entities, pathfinding);
                var economy = new EconomySystem(entities, EconomySystem.CanonicalMatchStartingCreditsAE);
                var construction = new ConstructionSystem(entities, economy, pathfinding.CostField);
                var production = new ProductionSystem(entities, economy, construction);
                var fogOfWar = new FogOfWarSystem(entities, teamCount: 2, 128, 128);
                var combat = new CombatSystem(entities, fogOfWar, economy);
                var kernel = new SimulationKernel(new SimRandom(Seed));
                kernel.RegisterSystem(economy);
                kernel.RegisterSystem(construction);
                kernel.RegisterSystem(production);
                kernel.RegisterSystem(pathfinding);
                kernel.RegisterSystem(movement);
                kernel.RegisterSystem(fogOfWar);
                kernel.RegisterSystem(combat);
                var session = new MatchSession(0, new byte[] { 0, 1 }, Delay);
                var ingress = new CommandIngress(session);
                kernel.BindCommands(
                    new UnitCommandStateView(entities, pathfinding, economy, construction, production), ingress);
                kernel.Start();
                return new ClientHost
                {
                    Kernel = kernel,
                    Entities = entities,
                    Economy = economy,
                    Construction = construction,
                    Production = production,
                    Session = session,
                    Ingress = ingress,
                };
            }

            public MatchFingerprint CreateFingerprint()
            {
                var slots = new byte[CommandLimits.ReservedPlayerSlots];
                slots[0] = (byte)PlayerSlotOccupancy.Human;
                slots[1] = (byte)PlayerSlotOccupancy.Human;
                var factions = new byte[CommandLimits.ReservedPlayerSlots];
                factions[0] = (byte)FactionId.Alliance;
                factions[1] = (byte)FactionId.Legion;
                return MatchFingerprint.CreateCurrent(
                    MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Rules),
                    SimDefinitions.ComputeDefinitionsHash64(),
                    MatchFingerprint.ComputeEmptyContentStubHash(MatchContentStub.Map),
                    slots, factions, Seed, Kernel.CalculateStateHash(), Session.InputDelayTicks);
            }

            public void SubmitIntent<TPayload>(in TPayload payload) where TPayload : struct, ICommandPayload
            {
                Assert.That(Ingress.TrySubmitIntent(CommandIntent.Create(payload), out CommandRejectReason reason),
                    Is.EqualTo(CommandIngressResult.Accepted), $"intent rejected: {reason}");
            }

            private uint _lastScriptTick = uint.MaxValue;

            /// <summary>The deterministic per-slot script of the soak match; each event fires exactly once per tick value.</summary>
            public void RunScript()
            {
                uint tick = Kernel.CurrentTick.Value;
                if (tick == _lastScriptTick) return;
                _lastScriptTick = tick;
                int slot = Session.LocalSlot;
                FactionId faction = Economy.GetSlotFaction((byte)slot);
                ushort refineryDef = SimDefinitions.ToDefinitionId(faction, UnitRole.Refinery);

                // Builder walks to the future refinery footprint and places it.
                if (tick == 10)
                {
                    SubmitIntent(new MovePayload(new[] { BuilderRaw },
                        SimFixed.FromInt(slot == 0 ? 10 : 117), SimFixed.FromInt(slot == 0 ? 5 : 117)));
                }
                if (tick == 40)
                {
                    SubmitIntent(new PlaceBuildingPayload(refineryDef,
                        (ushort)(slot == 0 ? 7 : 118), (ushort)(slot == 0 ? 4 : 116)));
                }
                // The infantry marches at the enemy base: auto-acquisition
                // (D-087) turns this into real combat ticks on the wire.
                if (tick == 60)
                {
                    SubmitIntent(new MovePayload(new[] { SoldierRaw },
                        SimFixed.FromInt(slot == 0 ? 100 : 20), SimFixed.FromInt(slot == 0 ? 100 : 20)));
                }
                // Once the refinery stands: harvester production and a rally.
                if (tick == 400 && Construction.HasFinishedBuilding((byte)slot, UnitRole.Refinery))
                {
                    uint refineryRaw = FindOwnBuildingRaw(UnitRole.Refinery);
                    SimDefinitions.TryGetUnit(faction, UnitRole.Harvester, out SimUnitDefinition harvesterDef);
                    SubmitIntent(new QueueUnitPayload(refineryRaw, harvesterDef.DefinitionId, 2));
                }
            }

            private uint FindOwnBuildingRaw(UnitRole role)
            {
                UnitState[] units = Entities.RawUnits;
                for (int i = 0; i < Entities.Capacity; i++)
                {
                    ref readonly UnitState u = ref units[i];
                    if (u.IsActive && u.PlayerId == Session.LocalSlot && u.Role == role)
                    {
                        return UnitCommandStateView.ToRawEntityId(u.Id);
                    }
                }
                return 0;
            }
        }

        // ------------------------------------------------------------------
        // A8 stage 1: the 10.023-tick two-client soak (23-tick tail)
        // ------------------------------------------------------------------

        [Test]
        public void TwoClients_OverRealRelay_StayBitIdentical_ThroughTick10023()
        {
            string recordDir = Path.Combine(Path.GetTempPath(), "nova-relay-test-" + Guid.NewGuid().ToString("N"));
            var server = new RelayServerCore(Token, Seed, Delay, recordDir,
                message => TestContext.Progress.WriteLine($"[server] {message}"));
            server.Start(0);

            var clientA = new RelayMatchClient { DebugLog = m => TestContext.Progress.WriteLine($"[A] {m}") };
            var clientB = new RelayMatchClient { DebugLog = m => TestContext.Progress.WriteLine($"[B] {m}") };
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);

            PumpUntil(server, clientA, clientB, () => clientA.HasOffer && clientB.HasOffer,
                "both clients received their slot offer");
            Assert.That(clientA.AssignedSlot, Is.Not.EqualTo(clientB.AssignedSlot));

            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);

            // Identical fingerprints and identical tick-0 state are the
            // handshake's premise — assert them locally before the server does.
            MatchFingerprint fingerprintA = hostA.CreateFingerprint();
            MatchFingerprint fingerprintB = hostB.CreateFingerprint();
            Assert.That(fingerprintB.Serialize(), Is.EqualTo(fingerprintA.Serialize()),
                "both clients built byte-identical fingerprints");
            byte[] snapshotA = hostA.Kernel.SaveSnapshot();
            byte[] snapshotB = hostB.Kernel.SaveSnapshot();
            Assert.That(snapshotB, Is.EqualTo(snapshotA), "both clients built byte-identical initial snapshots");

            clientA.SubmitLocalProof(fingerprintA.Serialize(), snapshotA);
            clientB.SubmitLocalProof(fingerprintB.Serialize(), snapshotB);
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Running && clientB.Phase == RelayClientPhase.Running,
                "the relay accepted both proofs and started the match");

            var hashMismatches = new List<string>();
            const int targetTicks = 10_023;
            const int finalCheckpointTick = 10_000;
            long guard = targetTicks * 40L;
            while ((hostA.Kernel.CurrentTick.Value < targetTicks || hostB.Kernel.CurrentTick.Value < targetTicks)
                && guard-- > 0)
            {
                server.Poll();
                clientA.Poll();
                clientB.Poll();

                bool steppedA = false;
                bool steppedB = false;
                if (hostA.Kernel.CurrentTick.Value < targetTicks)
                {
                    hostA.RunScript();
                    steppedA = clientA.TryStepTick(hostA.Kernel);
                }
                if (hostB.Kernel.CurrentTick.Value < targetTicks)
                {
                    hostB.RunScript();
                    steppedB = clientB.TryStepTick(hostB.Kernel);
                }

                uint tickA = hostA.Kernel.CurrentTick.Value;
                uint tickB = hostB.Kernel.CurrentTick.Value;
                if (tickA == tickB && tickA % RelayMatchClient.StateHashIntervalTicks == 0 && (steppedA || steppedB))
                {
                    ulong hashA = hostA.Kernel.CalculateStateHash();
                    ulong hashB = hostB.Kernel.CalculateStateHash();
                    if (hashA != hashB)
                    {
                        hashMismatches.Add($"tick {tickA}: A 0x{hashA:X16} != B 0x{hashB:X16}");
                        break;
                    }
                }
            }

            Assert.That(hashMismatches, Is.Empty, string.Join("; ", hashMismatches));
            TestContext.Progress.WriteLine(
                $"end state: A tick={hostA.Kernel.CurrentTick.Value} phase={clientA.Phase} end='{clientA.EndReason}' stalled={clientA.IsStalled} on={clientA.StalledOnSlot} | " +
                $"B tick={hostB.Kernel.CurrentTick.Value} phase={clientB.Phase} end='{clientB.EndReason}' stalled={clientB.IsStalled} on={clientB.StalledOnSlot}");
            Assert.That(hostA.Kernel.CurrentTick.Value, Is.EqualTo((uint)targetTicks), "client A completed the soak");
            Assert.That(hostB.Kernel.CurrentTick.Value, Is.EqualTo((uint)targetTicks), "client B completed the soak");
            Assert.That(clientA.Desynced, Is.False, "the relay reported a desync for A");
            Assert.That(clientB.Desynced, Is.False, "the relay reported a desync for B");
            Assert.That(hostA.Kernel.CalculateStateHash(), Is.EqualTo(hostB.Kernel.CalculateStateHash()),
                "final state hashes must be bit-identical");

            PumpUntil(server, clientA, clientB,
                () => server.LastCheckpointTick >= finalCheckpointTick,
                "the relay persisted the tick-10000 hash checkpoint and final announcements");
            Assert.That(server.LastRecordedTick, Is.EqualTo((uint)finalCheckpointTick),
                "before ordered close, only equal-hash checkpoints are durable");
            Assert.That(server.LastCheckpointTick, Is.EqualTo((uint)finalCheckpointTick));
            ulong liveHash = hostA.Kernel.CalculateStateHash();

            // Consume the final TickComplete frames emitted by the two
            // tick-10023 steps. They may still be queued because the
            // checkpoint condition above was already true at tick 10000.
            server.Poll();

            // Stop closes and flushes the complete stream (including its
            // latest target-tick group) before the reader verifies it.
            server.Stop();
            Assert.That(server.LastTerminalTick, Is.EqualTo((uint)targetTicks));
            Assert.That(server.LastRecordedTick, Is.EqualTo((uint)targetTicks));
            string[] recordings = Directory.GetFiles(recordDir, "*.novarec");
            Assert.That(recordings, Has.Length.EqualTo(1), "one command-stream dump per match");
            Assert.That(RelayRecordStream.TryRead(
                File.ReadAllBytes(recordings[0]), out RelayRecordStreamFile recording, out string readError),
                Is.True, readError);
            Assert.That(recording.Fingerprint, Is.EqualTo(fingerprintA));
            Assert.That(recording.InitialSnapshotBytes, Is.EqualTo(snapshotA));
            Assert.That(recording.LastRecordedTick, Is.EqualTo((uint)targetTicks));
            Assert.That(recording.LastCheckpointTick, Is.EqualTo((uint)finalCheckpointTick));
            Assert.That(recording.TerminalTick, Is.EqualTo((uint)targetTicks));
            Assert.That(recording.TerminalReason, Is.EqualTo(RelayRecordTerminalReason.ServerStopped));
            Assert.That(recording.IsComplete, Is.True);
            Assert.That(recording.Frames.Count, Is.EqualTo(targetTicks),
                "NOVAREC2 contains every tick frame, including empty ticks");
            Assert.That(recording.Checkpoints.Count,
                Is.EqualTo(targetTicks / RelayMatchClient.StateHashIntervalTicks));
            Assert.That(recording.TryGetCheckpointHash(finalCheckpointTick, out _), Is.True);
            int recordedCommands = 0;
            for (int i = 0; i < recording.Frames.Count; i++)
            {
                recordedCommands += recording.Frames[i].Records.Count;
            }
            Assert.That(recordedCommands, Is.GreaterThan(0),
                "the verified stream contains the scripted match commands");

            ClientHost playback = ClientHost.CreatePlayback();
            Assert.That(RelayRecordPlayback.TryPlay(
                    recording, fingerprintA, playback.Kernel, playback.Ingress,
                    out RelayRecordPlaybackResult playbackResult,
                    out RelayRecordPlaybackError playbackError, out string playbackDetail),
                Is.True, $"{playbackError}: {playbackDetail}");
            Assert.That(playbackResult.EndTick, Is.EqualTo((uint)targetTicks));
            Assert.That(playbackResult.StateHash, Is.EqualTo(liveHash),
                "engine-free playback must reproduce the live tick-10023 hash");
        }

        // ------------------------------------------------------------------
        // A2c: stall is visible, never a divergence — and it recovers
        // ------------------------------------------------------------------

        [Test]
        public void SilentPeer_StallsTheMatchVisibly_AndResumesBitIdentically()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var (hostA, hostB) = StartMatch(server);
            RelayMatchClient clientA = hostA.Client;
            RelayMatchClient clientB = hostB.Client;

            // Run 200 synchronized ticks first (clients may pass the mark by
            // a few ticks — bounded pipeline drift is by design — but they
            // must always agree with each other).
            Drive(server, clientA, clientB, hostA, hostB, 200);
            uint tickAtSilence = hostA.Kernel.CurrentTick.Value;
            Assert.That(hostB.Kernel.CurrentTick.Value, Is.EqualTo(tickAtSilence));

            // Client B goes silent: A drains at most the already-announced
            // window (input delay - 1 ticks — every record in it is final,
            // so draining it cannot diverge) and must then stall VISIBLY,
            // waiting on slot B.
            for (int i = 0; i < 50; i++)
            {
                server.Poll();
                clientA.Poll();
                clientA.TryStepTick(hostA.Kernel);
            }
            Assert.That(clientA.IsStalled, Is.True, "a silent peer must stall the local client");
            Assert.That(clientA.StalledOnSlot, Is.EqualTo(clientB.AssignedSlot));
            Assert.That(hostA.Kernel.CurrentTick.Value,
                Is.LessThanOrEqualTo(tickAtSilence + Delay - 1),
                "a stalled client may drain only the announced window, never run past it");
            uint tickAfterStall = hostA.Kernel.CurrentTick.Value;
            for (int i = 0; i < 50; i++)
            {
                server.Poll();
                clientA.Poll();
                clientA.TryStepTick(hostA.Kernel);
            }
            Assert.That(hostA.Kernel.CurrentTick.Value, Is.EqualTo(tickAfterStall),
                "once the announced window is drained, the client freezes — stall is right, running on is the bug");

            // B returns: the match resumes. A keeps the lead it legitimately
            // drained while B was silent (asserted above), and Drive stops
            // once the SLOWER end reaches the mark — so the two do not stand
            // on the same tick, exactly the bounded drift this test declares
            // by design further up. The invariant of lockstep is not "same
            // tick at the same moment", it is "same state at the same tick":
            // level the laggard first, then compare. Comparing hashes taken
            // at different ticks would assert nothing at all.
            Drive(server, clientA, clientB, hostA, hostB, 500);
            DriveUntilLevel(server, clientA, clientB, hostA, hostB);
            Assert.That(hostA.Kernel.CurrentTick.Value, Is.EqualTo(hostB.Kernel.CurrentTick.Value));
            Assert.That(hostA.Kernel.CalculateStateHash(), Is.EqualTo(hostB.Kernel.CalculateStateHash()));
            server.Stop();
        }

        [Test]
        public void DelayOne_ClosesInputBeforeBarrierWait_AndRunsThroughCheckpoint()
        {
            var server = new RelayServerCore(Token, Seed, 1, string.Empty, _ => { });
            ClientHost hostA = null;
            ClientHost hostB = null;
            try
            {
                server.Start(0);
                (hostA, hostB) = StartMatch(server);

                Assert.That(hostA.Client.IsReadyForCommandSubmission, Is.True);
                var first = CommandIntent.Create(
                    new StopPayload(new[] { hostA.BuilderRaw }));
                Assert.That(hostA.Ingress.TrySubmitIntent(
                        first, out CommandRejectReason firstReason),
                    Is.EqualTo(CommandIngressResult.Accepted), firstReason.ToString());
                uint nextSequence = hostA.Ingress.DedupeState.NextLocalSequence(
                    hostA.Client.AssignedSlot);

                Assert.That(hostA.Client.TryStepTick(hostA.Kernel), Is.False,
                    "the first attempt closes local input but still waits for the peer");
                Assert.That(hostA.Client.IsReadyForCommandSubmission, Is.False,
                    "a completed local target tick must reject late input while stalled");
                Assert.That(hostA.Ingress.TrySubmitIntent(
                        first, out CommandRejectReason lateReason),
                    Is.EqualTo(CommandIngressResult.Rejected));
                Assert.That(lateReason, Is.EqualTo(CommandRejectReason.TransportNotReady));
                Assert.That(hostA.Ingress.DedupeState.NextLocalSequence(
                    hostA.Client.AssignedSlot), Is.EqualTo(nextSequence));

                Drive(
                    server, hostA.Client, hostB.Client,
                    hostA, hostB, RelayMatchClient.StateHashIntervalTicks + 5u);
                DriveUntilLevel(
                    server, hostA.Client, hostB.Client, hostA, hostB);
                PumpUntil(server, hostA.Client, hostB.Client,
                    () => server.LastCheckpointTick >= RelayMatchClient.StateHashIntervalTicks,
                    "delay-1 checkpoint");

                Assert.That(hostA.Client.Phase, Is.EqualTo(RelayClientPhase.Running));
                Assert.That(hostB.Client.Phase, Is.EqualTo(RelayClientPhase.Running));
                Assert.That(hostA.Kernel.CurrentTick.Value,
                    Is.GreaterThan(RelayMatchClient.StateHashIntervalTicks));
                Assert.That(hostA.Kernel.CalculateStateHash(),
                    Is.EqualTo(hostB.Kernel.CalculateStateHash()));
            }
            finally
            {
                hostA?.Client.Disconnect();
                hostB?.Client.Disconnect();
                server.Stop();
            }
        }

        [Test]
        public void ClientTiming_RemainsCorrectAcrossSignedAndUnsignedTickCountWraps()
        {
            Assert.That(RelayMatchClient.ElapsedMilliseconds(
                    0x80000020u, 0x7FFFFFF0u),
                Is.EqualTo(48u), "the signed TickCount boundary is not a timer reset");
            Assert.That(RelayMatchClient.ElapsedMilliseconds(
                    20u, uint.MaxValue - 10u),
                Is.EqualTo(31u), "the uint TickCount wrap uses modulo subtraction");

            uint now = 0x80000000u;
            var clientA = new RelayMatchClient(() => now);
            var clientB = new RelayMatchClient(() => now);
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            ClientHost hostA = null;
            ClientHost hostB = null;
            try
            {
                server.Start(0);
                (hostA, hostB) = StartMatch(server, clientA, clientB);
                long guard = 2_000;
                while ((!clientA.RoundTripMilliseconds.HasValue
                        || !clientB.RoundTripMilliseconds.HasValue)
                    && guard-- > 0)
                {
                    server.Poll();
                    clientA.Poll();
                    clientB.Poll();
                    now = unchecked(now + 10u);
                }

                Assert.That(guard, Is.GreaterThan(0),
                    "RTT probes must complete while signed Environment.TickCount would be negative");
                Assert.That(clientA.RoundTripMilliseconds, Is.Not.Null);
                Assert.That(clientB.RoundTripMilliseconds, Is.Not.Null);
                Assert.That(clientA.RoundTripMilliseconds.Value, Is.LessThan(100u));
                Assert.That(clientB.RoundTripMilliseconds.Value, Is.LessThan(100u));
            }
            finally
            {
                hostA?.Client.Disconnect();
                hostB?.Client.Disconnect();
                server.Stop();
            }
        }

        [Test]
        public void StallTimeout_FiresAcrossTheSignedTickCountBoundary()
        {
            uint now = 0x7FFFFFF0u;
            var clientA = new RelayMatchClient(() => now);
            var clientB = new RelayMatchClient(() => now);
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            ClientHost hostA = null;
            ClientHost hostB = null;
            try
            {
                server.Start(0);
                (hostA, hostB) = StartMatch(server, clientA, clientB);
                long guard = 1_000;
                while (!clientA.IsStalled && guard-- > 0)
                {
                    server.Poll();
                    clientA.Poll();
                    clientA.TryStepTick(hostA.Kernel);
                }
                Assert.That(guard, Is.GreaterThan(0));

                now = unchecked(now + 31_000u);
                clientA.Poll();

                Assert.That(clientA.Phase, Is.EqualTo(RelayClientPhase.Ended));
                Assert.That(clientA.EndReason, Does.Contain("counted as lost"));
            }
            finally
            {
                hostA?.Client.Disconnect();
                hostB?.Client.Disconnect();
                server.Stop();
            }
        }

        [TestCase("start-before-offer")]
        [TestCase("invalid-offer-roles")]
        [TestCase("empty-reject")]
        [TestCase("nonempty-start")]
        [TestCase("local-command")]
        [TestCase("duplicate-remote-command")]
        [TestCase("local-tick-complete")]
        [TestCase("malformed-tick-complete")]
        [TestCase("invalid-desync")]
        [TestCase("invalid-peer-lost")]
        [TestCase("unsolicited-pong")]
        [TestCase("unknown")]
        [TestCase("completion-overcount")]
        [TestCase("completion-undercount")]
        [TestCase("late-remote-record")]
        [TestCase("duplicate-remote-completion")]
        [TestCase("conflicting-remote-completion")]
        public void ClientFailsClosedOnInvalidServerFrames(string violation)
        {
            ScriptedRelayServer server = null;
            RelayMatchClient client = null;
            ClientHost host = null;
            try
            {
                (server, client) = ConnectScriptedClient();
                if (violation == "start-before-offer")
                {
                    server.Send(RelayFrameType.Start, Array.Empty<byte>());
                }
                else if (violation == "invalid-offer-roles")
                {
                    server.Send(RelayFrameType.Offer, RelayProtocol.CreateOfferPayload(
                        2, new byte[] { 0, 1 }, Seed, Delay,
                        SimDefinitions.ComputeDefinitionsHash64()));
                }
                else if (violation == "empty-reject")
                {
                    server.Send(RelayFrameType.Reject,
                        RelayProtocol.CreateReasonPayload(RelayFrameType.Reject, string.Empty));
                }
                else
                {
                    host = AdvanceScriptedClientToWaitingStart(server, client);
                    if (violation == "nonempty-start")
                    {
                        server.Send(RelayFrameType.Start, new byte[] { 1 });
                    }
                    else
                    {
                        StartScriptedClient(server, client);
                        byte remoteSlot = client.AssignedSlot == 0 ? (byte)1 : (byte)0;
                        switch (violation)
                        {
                            case "local-command":
                                server.Send(RelayFrameType.CommandRecord,
                                    CreateStopRecord(
                                        client.AssignedSlot, 1, 0, client.InputDelayTicks).Serialize());
                                break;
                            case "duplicate-remote-command":
                                byte[] duplicate = CreateStopRecord(
                                    remoteSlot, 1, 0, client.InputDelayTicks).Serialize();
                                server.Send(RelayFrameType.CommandRecord, duplicate);
                                server.Send(RelayFrameType.CommandRecord, duplicate);
                                break;
                            case "local-tick-complete":
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        client.AssignedSlot, 1, 0));
                                break;
                            case "malformed-tick-complete":
                                server.Send(RelayFrameType.TickComplete, Array.Empty<byte>());
                                break;
                            case "invalid-desync":
                                server.Send(RelayFrameType.Desync,
                                    RelayProtocol.CreateSlotTickPayload(
                                        RelayFrameType.Desync, byte.MaxValue,
                                        RelayMatchClient.StateHashIntervalTicks));
                                break;
                            case "invalid-peer-lost":
                                server.Send(RelayFrameType.PeerLost,
                                    RelayProtocol.CreateSlotTickPayload(
                                        RelayFrameType.PeerLost, client.AssignedSlot, 0));
                                break;
                            case "unsolicited-pong":
                                server.Send(RelayFrameType.Pong,
                                    RelayProtocol.CreatePingPayload(123));
                                break;
                            case "unknown":
                                server.Send((RelayFrameType)255, Array.Empty<byte>());
                                break;
                            case "completion-overcount":
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, 1, 1));
                                break;
                            case "completion-undercount":
                                server.Send(RelayFrameType.CommandRecord,
                                    CreateStopRecord(
                                        remoteSlot, 1, 0, client.InputDelayTicks).Serialize());
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, client.InputDelayTicks, 0));
                                break;
                            case "late-remote-record":
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, client.InputDelayTicks, 0));
                                server.Send(RelayFrameType.CommandRecord,
                                    CreateStopRecord(
                                        remoteSlot, 1, 0, client.InputDelayTicks).Serialize());
                                break;
                            case "duplicate-remote-completion":
                                byte[] duplicateComplete =
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, 1, 0);
                                server.Send(RelayFrameType.TickComplete, duplicateComplete);
                                server.Send(RelayFrameType.TickComplete, duplicateComplete);
                                break;
                            case "conflicting-remote-completion":
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, 1, 0));
                                server.Send(RelayFrameType.TickComplete,
                                    RelayProtocol.CreateTickCompletePayload(
                                        remoteSlot, 1, 1));
                                break;
                            default:
                                Assert.Fail($"unknown scripted violation {violation}");
                                break;
                        }
                    }
                }

                PollClientUntil(client,
                    () => client.Phase == RelayClientPhase.Ended,
                    $"client rejection of {violation}");
                Assert.That(client.EndReason,
                    Does.StartWith("relay protocol violation:"));
            }
            finally
            {
                client?.Disconnect();
                server?.Dispose();
            }
        }

        [Test]
        public void ClientKeepsOnePingOutstanding_AndRejectsAMismatchedPong()
        {
            ScriptedRelayServer server = null;
            RelayMatchClient client = null;
            try
            {
                (server, client) = ConnectScriptedClient();
                AdvanceScriptedClientToWaitingStart(server, client);
                StartScriptedClient(server, client);

                for (int frame = 0; frame < 900; frame++)
                {
                    client.Poll();
                    server.Pump();
                }
                Assert.That(server.ReceivedCount(RelayFrameType.Ping), Is.EqualTo(1),
                    "an outstanding ping must not be overwritten by later cadence windows");
                Assert.That(server.TryGetFirstPayload(
                    RelayFrameType.Ping, out byte[] pingPayload), Is.True);
                Assert.That(RelayProtocol.TryParsePing(
                    pingPayload, out uint probe), Is.True);

                server.Send(RelayFrameType.Pong,
                    RelayProtocol.CreatePingPayload(unchecked(probe + 1u)));
                PollClientUntil(client,
                    () => client.Phase == RelayClientPhase.Ended,
                    "mismatched Pong rejection");
                Assert.That(client.EndReason,
                    Does.StartWith("relay protocol violation:"));
            }
            finally
            {
                client?.Disconnect();
                server?.Dispose();
            }
        }

        [Test]
        public void TickCompleteSendFailure_EndsBeforeMarkingOrStepping()
        {
            ScriptedRelayServer server = null;
            RelayMatchClient client = null;
            ClientHost host = null;
            try
            {
                (server, client) = ConnectScriptedClient();
                host = AdvanceScriptedClientToWaitingStart(server, client);
                StartScriptedClient(server, client);

                FieldInfo connectionField = typeof(RelayMatchClient).GetField(
                    "_connection", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo barrierField = typeof(RelayMatchClient).GetField(
                    "_barrier", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(connectionField, Is.Not.Null);
                Assert.That(barrierField, Is.Not.Null);
                var connection = (TcpRelayConnection)connectionField.GetValue(client);
                var barrier = (LockstepBarrier)barrierField.GetValue(client);
                connection.Disconnect();

                Assert.That(client.TryStepTick(host.Kernel), Is.False);
                Assert.That(client.Phase, Is.EqualTo(RelayClientPhase.Ended));
                Assert.That(host.Kernel.CurrentTick.Value, Is.Zero);
                Assert.That(barrier.WaitingOnSlot(client.InputDelayTicks),
                    Is.EqualTo(client.AssignedSlot),
                    "failed TickComplete must not mark the local barrier");
            }
            finally
            {
                client?.Disconnect();
                server?.Dispose();
            }
        }

        [Test]
        public void CompletenessWindowAtUIntMax_EndsWithoutWrapping()
        {
            ScriptedRelayServer server = null;
            RelayMatchClient client = null;
            ClientHost host = null;
            try
            {
                (server, client) = ConnectScriptedClient();
                host = AdvanceScriptedClientToWaitingStart(server, client);
                StartScriptedClient(server, client);

                FieldInfo currentTickField = typeof(MatchSession).GetField(
                    "<CurrentTick>k__BackingField",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(currentTickField, Is.Not.Null);
                currentTickField.SetValue(
                    host.Session, uint.MaxValue - host.Session.InputDelayTicks);

                Assert.That(client.TryStepTick(host.Kernel), Is.False);
                Assert.That(client.Phase, Is.EqualTo(RelayClientPhase.Ended));
                Assert.That(client.EndReason, Does.Contain("final representable tick"));
                Assert.That(host.Kernel.CurrentTick.Value, Is.Zero);
            }
            finally
            {
                client?.Disconnect();
                server?.Dispose();
            }
        }

        // ------------------------------------------------------------------
        // A4: the fingerprint lock names the differing field
        // ------------------------------------------------------------------

        [Test]
        public void FingerprintMismatch_RefusesTheMatch_AndNamesTheField()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var clientA = new RelayMatchClient();
            var clientB = new RelayMatchClient();
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, clientA, clientB, () => clientA.HasOffer && clientB.HasOffer, "offers");

            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);

            MatchFingerprint good = hostA.CreateFingerprint();
            // Client B arrives with a DIFFERENT input delay (an old build):
            // the match must not start, and the reason must name the field.
            MatchFingerprint tampered = MatchFingerprint.CreateCurrent(
                good.RulesHash64, good.DefinitionsHash64, good.MapHash64,
                good.GetSlotOccupancyCopy(), good.GetSlotFactionCopy(),
                good.StartSeed, good.InitialStateHash, Delay + 1);

            clientA.SubmitLocalProof(good.Serialize(), hostA.Kernel.SaveSnapshot());
            clientB.SubmitLocalProof(tampered.Serialize(), hostB.Kernel.SaveSnapshot());
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Ended && clientB.Phase == RelayClientPhase.Ended,
                "the relay refused the mismatched match");

            Assert.That(clientA.Phase, Is.Not.EqualTo(RelayClientPhase.Running));
            Assert.That(clientB.Phase, Is.Not.EqualTo(RelayClientPhase.Running));
            Assert.That(clientA.RejectReason, Does.Contain("InputDelayTicks"),
                "the refusal names the differing fingerprint field");
            server.Stop();
        }

        [Test]
        public void SidecarFingerprintMismatch_IsFoundByTheCentralComparator()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var clientA = new RelayMatchClient();
            var clientB = new RelayMatchClient();
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, clientA, clientB, () => clientA.HasOffer && clientB.HasOffer, "offers");
            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);
            MatchFingerprint good = hostA.CreateFingerprint();
            var sidecarMismatch = new MatchFingerprint(
                good.StateSchemaVersion, good.CommandSchemaVersion, good.PayloadSchemaVersion,
                good.SnapshotSchemaVersion, unchecked((ushort)(good.SidecarSchemaVersion + 1)),
                good.NumericModelId, good.TicksPerSecond, good.PrngId,
                good.RulesHash64, good.DefinitionsHash64, good.MapHash64,
                good.GetSlotOccupancyCopy(), good.GetSlotFactionCopy(),
                good.StartSeed, good.InitialStateHash, good.InputDelayTicks);

            clientA.SubmitLocalProof(good.Serialize(), hostA.Kernel.SaveSnapshot());
            clientB.SubmitLocalProof(sidecarMismatch.Serialize(), hostB.Kernel.SaveSnapshot());
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Ended
                    && clientB.Phase == RelayClientPhase.Ended,
                "sidecar mismatch rejection");

            Assert.That(clientA.RejectReason, Does.Contain("SidecarSchemaVersion"));
            Assert.That(clientB.RejectReason, Does.Contain("SidecarSchemaVersion"));
            server.Stop();
        }

        [TestCase("StartSeed")]
        [TestCase("InputDelayTicks")]
        [TestCase("DefinitionsHash64")]
        [TestCase("InitialSnapshot")]
        public void IdenticalPeerProofs_StillMustMatchTheRelayOffer(string mismatchField)
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var clientA = new RelayMatchClient();
            var clientB = new RelayMatchClient();
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, clientA, clientB, () => clientA.HasOffer && clientB.HasOffer, "offers");

            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);
            MatchFingerprint good = hostA.CreateFingerprint();
            ulong seed = mismatchField == "StartSeed" ? good.StartSeed ^ 1UL : good.StartSeed;
            uint delay = mismatchField == "InputDelayTicks" ? good.InputDelayTicks + 1 : good.InputDelayTicks;
            ulong definitions = mismatchField == "DefinitionsHash64"
                ? good.DefinitionsHash64 ^ 1UL
                : good.DefinitionsHash64;
            ulong initialHash = mismatchField == "InitialSnapshot"
                ? good.InitialStateHash ^ 1UL
                : good.InitialStateHash;
            MatchFingerprint offeredMismatch = MatchFingerprint.CreateCurrent(
                good.RulesHash64, definitions, good.MapHash64,
                good.GetSlotOccupancyCopy(), good.GetSlotFactionCopy(),
                seed, initialHash, delay);

            byte[] snapshotA = hostA.Kernel.SaveSnapshot();
            byte[] snapshotB = hostB.Kernel.SaveSnapshot();
            clientA.SubmitLocalProof(offeredMismatch.Serialize(), snapshotA);
            clientB.SubmitLocalProof(offeredMismatch.Serialize(), snapshotB);
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Ended && clientB.Phase == RelayClientPhase.Ended,
                "offer mismatch rejection");

            Assert.That(clientA.RejectReason, Does.Contain(mismatchField));
            Assert.That(clientB.RejectReason, Does.Contain(mismatchField));
            server.Stop();
        }

        [Test]
        public void ProofSnapshotWithoutKernelIdentity_IsRejectedWithoutCrashingTheRelay()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            RawPeer replacement0 = null;
            RawPeer replacement1 = null;
            try
            {
                server.Start(0);
                raw0 = new RawPeer(server.Port);
                raw1 = new RawPeer(server.Port);
                raw0.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
                raw1.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
                PumpRawUntil(server, raw0, raw1,
                    () => raw0.Offer != null && raw1.Offer != null,
                    "offers for invalid snapshot proof");

                var snapshotWriter = new SnapshotWriter();
                snapshotWriter.AddBlock(
                    SnapshotBlockIds.EntityStore, ReadOnlySpan<byte>.Empty);
                byte[] snapshot = snapshotWriter.ToArray();
                Assert.That(SnapshotReader.TryRead(
                    snapshot, out SnapshotFile parsed, out SnapshotReadError readError),
                    Is.True, readError.ToString());
                MatchFingerprint baseline = ClientHost.CreatePlayback().CreateFingerprint();
                MatchFingerprint proof = MatchFingerprint.CreateCurrent(
                    baseline.RulesHash64,
                    baseline.DefinitionsHash64,
                    baseline.MapHash64,
                    baseline.GetSlotOccupancyCopy(),
                    baseline.GetSlotFactionCopy(),
                    Seed,
                    parsed.StateHash,
                    Delay);
                byte[] fingerprint = proof.Serialize();
                raw0.Send(RelayFrameType.Fingerprint, fingerprint);
                raw0.Send(RelayFrameType.InitialSnapshot, snapshot);
                raw1.Send(RelayFrameType.Fingerprint, fingerprint);
                raw1.Send(RelayFrameType.InitialSnapshot, snapshot);

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0
                        && raw0.RejectReason != null && raw1.RejectReason != null,
                    "ordered rejection of snapshot without kernel identity");
                Assert.That(raw0.RejectReason, Does.Contain("InitialSnapshot"));
                Assert.That(raw0.RejectReason, Does.Contain("kernel"));

                raw0.Dispose();
                raw1.Dispose();
                raw0 = null;
                raw1 = null;
                (replacement0, replacement1) = StartRawMatch(server);
                Assert.That(replacement0.Started, Is.True);
                Assert.That(replacement1.Started, Is.True);
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                replacement0?.Dispose();
                replacement1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void ProofSnapshotAfterTickZero_IsRejectedAndTheRelayCanBeReused()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            RawPeer replacement0 = null;
            RawPeer replacement1 = null;
            try
            {
                server.Start(0);
                raw0 = new RawPeer(server.Port);
                raw1 = new RawPeer(server.Port);
                raw0.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
                raw1.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
                PumpRawUntil(server, raw0, raw1,
                    () => raw0.Offer != null && raw1.Offer != null,
                    "offers for nonzero-tick snapshot proof");

                ClientHost source = ClientHost.CreatePlayback();
                source.Kernel.StepTick();
                Assert.That(source.Kernel.CurrentTick.Value, Is.EqualTo(1));
                byte[] snapshot = source.Kernel.SaveSnapshot();
                byte[] fingerprint = source.CreateFingerprint().Serialize();
                raw0.Send(RelayFrameType.Fingerprint, fingerprint);
                raw0.Send(RelayFrameType.InitialSnapshot, snapshot);
                raw1.Send(RelayFrameType.Fingerprint, fingerprint);
                raw1.Send(RelayFrameType.InitialSnapshot, snapshot);

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0
                        && raw0.RejectReason != null && raw1.RejectReason != null,
                    "ordered rejection of a nonzero-tick snapshot proof");
                Assert.That(raw0.RejectReason, Does.Contain("InitialSnapshot"));
                Assert.That(raw0.RejectReason, Does.Contain("expected tick 0"));

                raw0.Dispose();
                raw1.Dispose();
                raw0 = null;
                raw1 = null;
                (replacement0, replacement1) = StartRawMatch(server);
                Assert.That(replacement0.Started, Is.True);
                Assert.That(replacement1.Started, Is.True);
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                replacement0?.Dispose();
                replacement1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void WrongMatchCode_IsRejected()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var client = new RelayMatchClient();
            client.Connect("127.0.0.1", server.Port, Token + 1);
            PumpUntil(server, client, null, () => client.Phase == RelayClientPhase.Ended, "rejection");
            Assert.That(client.RejectReason, Does.Contain("match code"));
            server.Stop();
        }

        [Test]
        public void PreHelloInvalidPing_IsRejectedAndItsSlotCanBeReused()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            RawPeer attacker = null;
            RawPeer replacement0 = null;
            RawPeer replacement1 = null;
            try
            {
                server.Start(0);
                attacker = new RawPeer(server.Port);
                long acceptGuard = 10_000;
                while (server.PeerCount != 1 && acceptGuard-- > 0)
                {
                    server.Poll();
                }
                Assert.That(acceptGuard, Is.GreaterThan(0),
                    "relay did not accept the unauthenticated peer");
                attacker.Send(RelayFrameType.Ping, new byte[5]);
                long guard = 10_000;
                while (server.PeerCount != 0 && guard-- > 0)
                {
                    server.Poll();
                    attacker.Pump();
                }
                Assert.That(guard, Is.GreaterThan(0),
                    "an unauthenticated invalid Ping must release its slot");

                attacker.Dispose();
                attacker = null;
                (replacement0, replacement1) = StartRawMatch(server);
                Assert.That(replacement0.Started, Is.True);
                Assert.That(replacement1.Started, Is.True);
            }
            finally
            {
                attacker?.Dispose();
                replacement0?.Dispose();
                replacement1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void UnknownClientFrameDuringMatch_IsAProtocolViolation()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                raw0.Send((RelayFrameType)255, Array.Empty<byte>());

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "unknown client frame rejection");
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void WrongSecondPeer_DoesNotResetTheValidWaitingPeer_AndSlotIsReusable()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var valid = new RelayMatchClient();
            var wrong = new RelayMatchClient();
            valid.Connect("127.0.0.1", server.Port, Token);
            wrong.Connect("127.0.0.1", server.Port, Token + 1);
            PumpUntil(server, valid, wrong,
                () => valid.HasOffer && wrong.Phase == RelayClientPhase.Ended,
                "valid offer and wrong-peer rejection");

            Assert.That(valid.AssignedSlot, Is.EqualTo(0));
            Assert.That(valid.Phase, Is.EqualTo(RelayClientPhase.WaitingOffer));
            Assert.That(server.PeerCount, Is.EqualTo(1));

            var replacement = new RelayMatchClient();
            replacement.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, valid, replacement, () => replacement.HasOffer,
                "replacement receives the freed slot");
            Assert.That(replacement.AssignedSlot, Is.EqualTo(1));
            Assert.That(valid.Phase, Is.EqualTo(RelayClientPhase.WaitingOffer));
            server.Stop();
        }

        [Test]
        public void CleanFinFromSlot1_EndsRunningMatchWithoutBreakingThePollLoop()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-clean-fin-record-test-" + Guid.NewGuid().ToString("N"));
            var server = new RelayServerCore(Token, Seed, Delay, directory, _ => { });
            ClientHost hostA = null;
            ClientHost hostB = null;
            try
            {
                server.Start(0);
                (hostA, hostB) = StartMatch(server);
                RelayMatchClient slot1 = hostA.Client.AssignedSlot == 1 ? hostA.Client : hostB.Client;
                RelayMatchClient survivor = hostA.Client.AssignedSlot == 0 ? hostA.Client : hostB.Client;

                slot1.Disconnect();
                PumpUntil(server, survivor, null,
                    () => survivor.Phase == RelayClientPhase.Ended,
                    "slot-1 FIN and ordered peer-lost shutdown");

                Assert.That(survivor.EndReason, Does.Contain("peer slot 1"));
                Assert.That(server.PeerCount, Is.Zero,
                    "the deferred reset closes peers only after the poll iteration");
                Assert.That(RelayRecordStream.TryRead(
                        File.ReadAllBytes(server.LastRecordPath),
                        out RelayRecordStreamFile recording, out string readError),
                    Is.True, readError);
                Assert.That(recording.TerminalReason,
                    Is.EqualTo(RelayRecordTerminalReason.PeerLost));
            }
            finally
            {
                hostA?.Client.Disconnect();
                hostB?.Client.Disconnect();
                server.Stop();
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void MalformedRunningRecord_SealsAProtocolViolation()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-protocol-record-test-" + Guid.NewGuid().ToString("N"));
            var server = new RelayServerCore(Token, Seed, Delay, directory, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                raw0.Send(RelayFrameType.CommandRecord, new byte[] { 0 });

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "malformed running record rejection");
                Assert.That(RelayRecordStream.TryRead(
                        File.ReadAllBytes(server.LastRecordPath),
                        out RelayRecordStreamFile recording, out string readError),
                    Is.True, readError);
                Assert.That(recording.TerminalReason,
                    Is.EqualTo(RelayRecordTerminalReason.ProtocolViolation));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void OversizedRunningFrameEnvelope_SealsAProtocolViolation()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-frame-envelope-record-test-" + Guid.NewGuid().ToString("N"));
            var server = new RelayServerCore(Token, Seed, Delay, directory, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                var oversizedHeader = new byte[RelayProtocol.HeaderBytes];
                RelayProtocol.WriteUInt32(
                    oversizedHeader, 0, RelayProtocol.MaxFramePayloadBytes + 1u);
                oversizedHeader[4] = (byte)RelayFrameType.CommandRecord;
                raw0.SendRaw(oversizedHeader);

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "oversized running frame-envelope rejection");
                Assert.That(RelayRecordStream.TryRead(
                        File.ReadAllBytes(server.LastRecordPath),
                        out RelayRecordStreamFile recording, out string readError),
                    Is.True, readError);
                Assert.That(recording.TerminalReason,
                    Is.EqualTo(RelayRecordTerminalReason.ProtocolViolation));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void HelloAndProofTimeouts_FreeOnlyTheExpiredSlot()
        {
            uint now = 100;
            var server = new RelayServerCore(
                Token, Seed, Delay, string.Empty, _ => { }, () => now);
            server.Start(0);

            using (var silent = new TcpClient())
            {
                silent.Connect("127.0.0.1", server.Port);
                long acceptGuard = 100_000;
                while (server.PeerCount != 1 && acceptGuard-- > 0)
                {
                    server.Poll();
                }
                Assert.That(acceptGuard, Is.GreaterThan(0),
                    "relay did not accept the silent peer before testing its timeout");
                now += RelayServerCore.HelloTimeoutMilliseconds + 1;
                server.Poll();
                Assert.That(server.PeerCount, Is.Zero);
            }

            var proofless = new RelayMatchClient();
            proofless.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, proofless, null, () => proofless.HasOffer, "proofless peer offer");
            Assert.That(proofless.AssignedSlot, Is.EqualTo(0));
            now += RelayServerCore.ProofTimeoutMilliseconds + 1;
            PumpUntil(server, proofless, null,
                () => proofless.Phase == RelayClientPhase.Ended,
                "proof timeout rejection");
            Assert.That(proofless.RejectReason, Does.Contain("proof timed out"));
            Assert.That(server.PeerCount, Is.Zero);

            var replacement = new RelayMatchClient();
            replacement.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, replacement, null, () => replacement.HasOffer,
                "slot freed after proof timeout");
            Assert.That(replacement.AssignedSlot, Is.EqualTo(0));
            server.Stop();
        }

        [Test]
        public void CompleteProof_MayWaitForOpponentForTwoMinutes_FromProofCompletion()
        {
            uint now = 500;
            var server = new RelayServerCore(
                Token, Seed, Delay, string.Empty, _ => { }, () => now);
            RawPeer peer = null;
            try
            {
                server.Start(0);
                peer = new RawPeer(server.Port);
                peer.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
                long offerGuard = 100_000;
                while (peer.Offer == null && offerGuard-- > 0)
                {
                    server.Poll();
                    peer.Pump();
                }
                Assert.That(offerGuard, Is.GreaterThan(0));

                ClientHost source = ClientHost.CreatePlayback();
                peer.Send(RelayFrameType.Fingerprint, source.CreateFingerprint().Serialize());
                peer.Send(RelayFrameType.InitialSnapshot, source.Kernel.SaveSnapshot());
                // A single Poll is not enough: the OS may not have exposed
                // both bytes yet, and advancing the injected clock first
                // would move the proof-completion timestamp with it.
                long proofGuard = 100_000;
                while (server.CompleteProofPeerCount != 1 && proofGuard-- > 0)
                {
                    server.Poll();
                    peer.Pump();
                }
                Assert.That(proofGuard, Is.GreaterThan(0),
                    "relay did not process the complete proof before the clock advanced");

                now += RelayServerCore.OpponentWaitTimeoutMilliseconds - 1;
                server.Poll();
                peer.Pump();
                Assert.That(server.PeerCount, Is.EqualTo(1));
                Assert.That(peer.RejectReason, Is.Null);

                now += 1;
                server.Poll();
                long rejectGuard = 100_000;
                while (peer.RejectReason == null && rejectGuard-- > 0)
                {
                    peer.Pump();
                }
                Assert.That(server.PeerCount, Is.Zero);
                Assert.That(rejectGuard, Is.GreaterThan(0));
                Assert.That(peer.RejectReason, Does.Contain("waiting for opponent timed out"));
            }
            finally
            {
                peer?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void Desync_WritesOneParseableSnapshotAndRecordStreamPerClient()
        {
            string root = Path.Combine(Path.GetTempPath(), "nova-desync-test-" + Guid.NewGuid().ToString("N"));
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            var clientA = new RelayMatchClient { DiagnosticDirectory = Path.Combine(root, "a") };
            var clientB = new RelayMatchClient { DiagnosticDirectory = Path.Combine(root, "b") };
            var (hostA, hostB) = StartMatch(server, clientA, clientB);

            // Diverge comfortably before the first 50-tick checkpoint: the
            // drive helper permits the normal input-delay pipeline lead, so
            // aiming at 49 could already have crossed tick 50 on one end.
            Drive(server, clientA, clientB, hostA, hostB, 25);
            ref PlayerEconomyState divergentEconomy = ref hostB.Economy.GetPlayerEconomy(0);
            divergentEconomy.AddCredits(1);
            Drive(server, clientA, clientB, hostA, hostB, 50);
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Ended && clientB.Phase == RelayClientPhase.Ended,
                "desync broadcast and client diagnoses");

            Assert.That(clientA.Desynced, Is.True);
            Assert.That(clientB.Desynced, Is.True);
            Assert.That(clientA.LastDiagnosticError, Is.Empty);
            Assert.That(clientB.LastDiagnosticError, Is.Empty);
            Assert.That(File.Exists(clientA.LastDiagnosticPath), Is.True);
            Assert.That(File.Exists(clientB.LastDiagnosticPath), Is.True);
            Assert.That(DesyncDiagnostic.TryRead(
                File.ReadAllBytes(clientA.LastDiagnosticPath), out DesyncDiagnosticFile diagnosisA, out string errorA),
                Is.True, errorA);
            Assert.That(DesyncDiagnostic.TryRead(
                File.ReadAllBytes(clientB.LastDiagnosticPath), out DesyncDiagnosticFile diagnosisB, out string errorB),
                Is.True, errorB);
            Assert.That(diagnosisA.LocalSlot, Is.Not.EqualTo(diagnosisB.LocalSlot));
            Assert.That(diagnosisA.DesyncTick, Is.EqualTo(50));
            Assert.That(diagnosisB.DesyncTick, Is.EqualTo(50));
            Assert.That(diagnosisA.Records.Count, Is.GreaterThan(0));
            Assert.That(diagnosisB.Records.Count, Is.GreaterThan(0));
            Assert.That(SnapshotReader.TryRead(
                diagnosisA.SnapshotBytes, out SnapshotFile snapshotA, out _), Is.True);
            Assert.That(SnapshotReader.TryRead(
                diagnosisB.SnapshotBytes, out SnapshotFile snapshotB, out _), Is.True);
            Assert.That(snapshotA.StateHash, Is.Not.EqualTo(snapshotB.StateHash),
                "the diagnosis preserves the two actually diverged client states");
            Assert.That(DesyncDiagnostic.TryReadSnapshotIdentity(
                diagnosisA.SnapshotBytes, out uint tickA, out ulong hashA, out _), Is.True);
            Assert.That(DesyncDiagnostic.TryReadSnapshotIdentity(
                diagnosisB.SnapshotBytes, out uint tickB, out ulong hashB, out _), Is.True);
            Assert.That(tickA, Is.EqualTo(diagnosisA.DesyncTick));
            Assert.That(tickB, Is.EqualTo(diagnosisB.DesyncTick));
            Assert.That(hashA, Is.EqualTo(diagnosisA.StateHash));
            Assert.That(hashB, Is.EqualTo(diagnosisB.StateHash));
            Assert.That(diagnosisA.Records.Count, Is.EqualTo(diagnosisB.Records.Count));
            for (int i = 0; i < diagnosisA.Records.Count; i++)
            {
                Assert.That(diagnosisA.Records[i], Is.EqualTo(diagnosisB.Records[i]),
                    $"applied canonical record {i} differs between diagnostics");
            }
            server.Stop();
        }

        [Test]
        public void NovaRecord2Reader_RejectsTruncationGapsAndDuplicateRecords()
        {
            ClientHost source = ClientHost.CreatePlayback();
            MatchFingerprint fingerprint = source.CreateFingerprint();
            byte[] snapshot = source.Kernel.SaveSnapshot();

            byte[] valid;
            using (var stream = new MemoryStream())
            {
                RelayRecordStream.WriteHeader(stream, fingerprint.Serialize(), snapshot);
                for (uint tick = 1; tick <= 50; tick++)
                {
                    RelayRecordStream.WriteTickFrame(stream, tick, Array.Empty<CommandRecord>());
                }
                RelayRecordStream.WriteCheckpoint(stream, 50, 0x123456789ABCDEF0UL);
                RelayRecordStream.WriteTickFrame(stream, 51, Array.Empty<CommandRecord>());
                RelayRecordStream.WriteEnd(
                    stream, RelayRecordTerminalReason.ServerStopped, 51, 51, 50);
                valid = stream.ToArray();
            }
            Assert.That(RelayRecordStream.TryRead(
                valid, out RelayRecordStreamFile parsed, out string validError), Is.True, validError);
            Assert.That(parsed.LastRecordedTick, Is.EqualTo(51));
            Assert.That(parsed.LastCheckpointTick, Is.EqualTo(50));
            Assert.That(parsed.TerminalTick, Is.EqualTo(51));

            var truncated = new byte[valid.Length - 1];
            Array.Copy(valid, truncated, truncated.Length);
            Assert.That(RelayRecordStream.TryRead(
                truncated, out _, out string truncatedError), Is.False);
            Assert.That(truncatedError, Does.Contain("end marker"));

            var withTrailingByte = new byte[valid.Length + 1];
            Array.Copy(valid, withTrailingByte, valid.Length);
            Assert.That(RelayRecordStream.TryRead(withTrailingByte, out _, out _), Is.False);

            byte[] sealedPartial;
            using (var stream = new MemoryStream())
            {
                RelayRecordStream.WriteHeader(stream, fingerprint.Serialize(), snapshot);
                for (uint tick = 1; tick <= 50; tick++)
                {
                    RelayRecordStream.WriteTickFrame(stream, tick, Array.Empty<CommandRecord>());
                }
                RelayRecordStream.WriteCheckpoint(stream, 50, 0x123456789ABCDEF0UL);
                RelayRecordStream.WriteEnd(
                    stream, RelayRecordTerminalReason.RecordingLimitExceeded,
                    terminalTick: 51, lastRecordedTick: 50, lastCheckpointTick: 50);
                sealedPartial = stream.ToArray();
            }
            Assert.That(RelayRecordStream.TryRead(
                sealedPartial, out RelayRecordStreamFile partial, out string partialError),
                Is.True, partialError);
            Assert.That(partial.IsComplete, Is.False);
            ClientHost refusedPlayback = ClientHost.CreatePlayback();
            Assert.That(RelayRecordPlayback.TryPlay(
                    partial, fingerprint, refusedPlayback.Kernel, refusedPlayback.Ingress,
                    out _, out RelayRecordPlaybackError incompleteError, out _),
                Is.False);
            Assert.That(incompleteError, Is.EqualTo(RelayRecordPlaybackError.IncompleteRecording));

            byte[] gapped;
            using (var stream = new MemoryStream())
            {
                RelayRecordStream.WriteHeader(stream, fingerprint.Serialize(), snapshot);
                RelayRecordStream.WriteTickFrame(stream, 2, Array.Empty<CommandRecord>());
                RelayRecordStream.WriteEnd(
                    stream, RelayRecordTerminalReason.ServerStopped, 2, 2, 0);
                gapped = stream.ToArray();
            }
            Assert.That(RelayRecordStream.TryRead(gapped, out _, out _), Is.False);

            CommandRecord duplicate = CreateStopRecord(0, 1, 0, Delay);
            byte[] duplicated;
            using (var stream = new MemoryStream())
            {
                RelayRecordStream.WriteHeader(stream, fingerprint.Serialize(), snapshot);
                RelayRecordStream.WriteTickFrame(stream, 1, Array.Empty<CommandRecord>());
                RelayRecordStream.WriteTickFrame(stream, 2, Array.Empty<CommandRecord>());
                RelayRecordStream.WriteTickFrame(stream, 3, new[] { duplicate, duplicate });
                duplicated = stream.ToArray();
            }
            Assert.That(RelayRecordStream.TryRead(duplicated, out _, out string duplicateError), Is.False);
            Assert.That(duplicateError, Does.Contain("duplicate").Or.Contain("canonical"));
        }

        [Test]
        public void NovaRecord2Writer_ReservesTheFooterInsideTheSharedByteBudget()
        {
            var stream = new CountingSeekableStream
            {
                Position = RelayRecordStream.MaxRecordingBytes
                    - RelayRecordStream.EndEntryBytes - (1 + 4 + 8),
            };

            RelayRecordStream.WriteCheckpoint(stream, 50, 1);
            long beforeRejectedEntry = stream.Position;
            Assert.Throws<RelayRecordBudgetExceededException>(() =>
                RelayRecordStream.WriteCheckpoint(stream, 100, 2));
            Assert.That(stream.Position, Is.EqualTo(beforeRejectedEntry),
                "the first entry outside the cap must be rejected before any byte is written");

            RelayRecordStream.WriteEnd(
                stream, RelayRecordTerminalReason.ServerStopped, 0, 0, 0);
            Assert.That(stream.Position, Is.EqualTo(RelayRecordStream.MaxRecordingBytes));
        }

        [Test]
        public void DesyncDiagnostic_SpoolsAndRoundTripsMoreThan65536Records()
        {
            const uint recordCount = 65_537;
            const uint terminalTick = 257;
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-large-diag-test-" + Guid.NewGuid().ToString("N"));
            string path = null;
            try
            {
                ClientHost source = ClientHost.CreatePlayback();
                for (uint tick = 1; tick <= terminalTick; tick++) source.Kernel.StepTick();
                ulong stateHash = source.Kernel.CalculateStateHash();
                byte[] snapshot = source.Kernel.SaveSnapshot();

                using (var spool = new DiagnosticRecordSpool())
                {
                    for (uint sequence = 1; sequence <= recordCount; sequence++)
                    {
                        uint targetTick = (sequence - 1) / CommandLimits.MaxBatchRecordsPerTick + 1;
                        byte[] raw = CreateStopRecord(
                            0, sequence, targetTick - 1, inputDelayTicks: 1).Serialize();
                        Assert.That(spool.TryAppend(raw, out string appendError),
                            Is.True, $"record {sequence}: {appendError}");
                    }
                    Assert.That(spool.RecordCount, Is.EqualTo(recordCount));
                    Assert.That(DesyncDiagnostic.TryWrite(
                            directory, 0, terminalTick, stateHash, snapshot, spool,
                            out path, out string writeError),
                        Is.True, writeError);
                }

                Assert.That(DesyncDiagnostic.TryRead(
                        File.ReadAllBytes(path), out DesyncDiagnosticFile diagnosis,
                        out string readError),
                    Is.True, readError);
                Assert.That(diagnosis.Records.Count, Is.EqualTo((int)recordCount));
                Assert.That(diagnosis.Records[0].Sequence, Is.EqualTo(1));
                Assert.That(diagnosis.Records[(int)recordCount - 1].Sequence, Is.EqualTo(recordCount));
            }
            finally
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void DiagnosticRecordSpool_FailsBeforeCrossingItsInjectedByteBudget()
        {
            byte[] first = CreateStopRecord(0, 1, 0, Delay).Serialize();
            byte[] second = CreateStopRecord(0, 2, 0, Delay).Serialize();
            using (var spool = new DiagnosticRecordSpool(4 + first.Length))
            {
                Assert.That(spool.TryAppend(first, out string firstError), Is.True, firstError);
                long fullLength = spool.ByteLength;
                Assert.That(spool.TryAppend(second, out string secondError), Is.False);
                Assert.That(secondError, Does.Contain("byte budget exceeded"));
                Assert.That(spool.ByteLength, Is.EqualTo(fullLength));
                Assert.That(spool.RecordCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void RelayRecordPlayback_RejectsAThirdHashAtTheDesyncTick()
        {
            ClientHost source = ClientHost.CreatePlayback();
            MatchFingerprint fingerprint = source.CreateFingerprint();
            byte[] snapshot = source.Kernel.SaveSnapshot();
            ulong hash50 = 0;
            ulong hash100 = 0;
            ulong hash150 = 0;
            for (uint tick = 1; tick <= 150; tick++)
            {
                source.Kernel.StepTick();
                if (tick == 50) hash50 = source.Kernel.CalculateStateHash();
                if (tick == 100) hash100 = source.Kernel.CalculateStateHash();
                if (tick == 150) hash150 = source.Kernel.CalculateStateHash();
            }

            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                RelayRecordStream.WriteHeader(stream, fingerprint.Serialize(), snapshot);
                for (uint tick = 1; tick <= 150; tick++)
                {
                    RelayRecordStream.WriteTickFrame(stream, tick, Array.Empty<CommandRecord>());
                    if (tick == 50) RelayRecordStream.WriteCheckpoint(stream, tick, hash50);
                    if (tick == 100) RelayRecordStream.WriteCheckpoint(stream, tick, hash100);
                }
                RelayRecordStream.WriteDesync(stream, 150, hash150 ^ 1UL, hash150 ^ 2UL);
                RelayRecordStream.WriteEnd(
                    stream, RelayRecordTerminalReason.Desync, 150, 150, 100);
                bytes = stream.ToArray();
            }
            Assert.That(RelayRecordStream.TryRead(
                bytes, out RelayRecordStreamFile recording, out string readError), Is.True, readError);

            ClientHost playback = ClientHost.CreatePlayback();
            Assert.That(RelayRecordPlayback.TryPlay(
                    recording, fingerprint, playback.Kernel, playback.Ingress,
                    out RelayRecordPlaybackResult result,
                    out RelayRecordPlaybackError playbackError, out string playbackDetail),
                Is.False, playbackDetail);
            Assert.That(result, Is.Null);
            Assert.That(playbackError, Is.EqualTo(RelayRecordPlaybackError.DesyncHashMismatch));

            ClientHost prefixPlayback = ClientHost.CreatePlayback();
            Assert.That(RelayRecordPlayback.TryPlayThrough(
                    recording, 100, fingerprint, prefixPlayback.Kernel, prefixPlayback.Ingress,
                    out RelayRecordPlaybackResult prefixResult,
                    out RelayRecordPlaybackError prefixError, out string prefixDetail),
                Is.True, $"{prefixError}: {prefixDetail}");
            Assert.That(prefixResult.StateHash, Is.EqualTo(hash100));
        }

        [TestCase("duplicate")]
        [TestCase("count-mismatch")]
        [TestCase("capacity")]
        public void RelayServer_FailsClosedOnNonCanonicalRecordAccounting(string violation)
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            server.Start(0);
            (RawPeer raw0, RawPeer raw1) = StartRawMatch(server);
            try
            {
                if (violation == "duplicate")
                {
                    byte[] record = CreateStopRecord(0, 1, 0, Delay).Serialize();
                    raw0.Send(RelayFrameType.CommandRecord, record);
                    raw0.Send(RelayFrameType.CommandRecord, record);
                }
                else if (violation == "count-mismatch")
                {
                    raw0.Send(RelayFrameType.CommandRecord,
                        CreateStopRecord(0, 1, 0, Delay).Serialize());
                    raw0.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(0, 1, 0));
                    raw0.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(0, 2, 0));
                    raw0.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(0, 3, 0));
                }
                else
                {
                    for (uint sequence = 1;
                        sequence <= CommandLimits.MaxBatchRecordsPerTick + 1;
                        sequence++)
                    {
                        raw0.Send(RelayFrameType.CommandRecord,
                            CreateStopRecord(0, sequence, 0, Delay).Serialize());
                    }
                }

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    $"server rejection of {violation}");
                Assert.That(server.LastRecordedTick, Is.Zero,
                    "no uncheckpointed or malformed stream may become trusted recording state");
            }
            finally
            {
                raw0.Dispose();
                raw1.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void RelayServer_RejectsThe1025thPendingRecordAcrossTickBuckets()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-global-pending-cap-test-" + Guid.NewGuid().ToString("N"));
            var logs = new List<string>();
            var server = new RelayServerCore(Token, Seed, Delay, directory, logs.Add);
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                for (int index = 0; index <= CommandLimits.MaxPendingRecords; index++)
                {
                    uint sequence = unchecked((uint)index + 1u);
                    uint enqueueTick = unchecked((uint)(index / CommandLimits.MaxBatchRecordsPerTick));
                    raw0.Send(RelayFrameType.CommandRecord,
                        CreateStopRecord(0, sequence, enqueueTick, Delay).Serialize());
                }

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "global pending-record capacity rejection");
                Assert.That(logs, Has.Some.Contains("pending record capacity"));
                Assert.That(RelayRecordStream.TryRead(
                        File.ReadAllBytes(server.LastRecordPath),
                        out RelayRecordStreamFile recording, out string readError),
                    Is.True, readError);
                Assert.That(recording.TerminalReason,
                    Is.EqualTo(RelayRecordTerminalReason.ProtocolViolation));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        [Test]
        public void RelayServer_ReleasesGlobalPendingCapacityAsTicksConfirm()
        {
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, _ => { });
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                for (int index = 0; index < CommandLimits.MaxPendingRecords; index++)
                {
                    uint sequence = unchecked((uint)index + 1u);
                    uint enqueueTick = unchecked((uint)(index / CommandLimits.MaxBatchRecordsPerTick));
                    raw0.Send(RelayFrameType.CommandRecord,
                        CreateStopRecord(0, sequence, enqueueTick, Delay).Serialize());
                }
                PumpRawUntil(server, raw0, raw1,
                    () => server.PendingRecordCount == CommandLimits.MaxPendingRecords,
                    "the exact global pending-record capacity");
                Assert.That(server.PendingTickCount, Is.EqualTo(4));

                for (uint tick = 1; tick <= 6; tick++)
                {
                    int slot0Count = tick >= 3
                        ? CommandLimits.MaxBatchRecordsPerTick
                        : 0;
                    raw0.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(0, tick, slot0Count));
                    raw1.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(1, tick, 0));
                }
                PumpRawUntil(server, raw0, raw1,
                    () => server.PendingRecordCount == 0,
                    "confirmed ticks release pending-record capacity");

                raw0.Send(RelayFrameType.CommandRecord,
                    CreateStopRecord(
                        0, CommandLimits.MaxPendingRecords + 1u, 4, Delay).Serialize());
                PumpRawUntil(server, raw0, raw1,
                    () => server.PendingRecordCount == 1,
                    "one record accepted after capacity release");
                Assert.That(server.PeerCount, Is.EqualTo(2));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void RelayServer_BoundsPendingEmptyTickBuckets()
        {
            var logs = new List<string>();
            var server = new RelayServerCore(Token, Seed, Delay, string.Empty, logs.Add);
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                for (uint tick = 1; tick <= CommandLimits.MaxPendingRecords; tick++)
                {
                    raw0.Send(RelayFrameType.TickComplete,
                        RelayProtocol.CreateTickCompletePayload(0, tick, 0));
                }
                PumpRawUntil(server, raw0, raw1,
                    () => server.PendingTickCount == CommandLimits.MaxPendingRecords,
                    "the exact pending empty-tick bucket capacity");

                raw0.Send(RelayFrameType.TickComplete,
                    RelayProtocol.CreateTickCompletePayload(
                        0, CommandLimits.MaxPendingRecords + 1u, 0));
                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "pending empty-tick bucket capacity rejection");
                Assert.That(logs, Has.Some.Contains("accepted -1"));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
            }
        }

        [Test]
        public void GeneratedRelaySeed_RetriesZeroCandidates()
        {
            ulong[] candidates = { 0, 0, Seed };
            int index = 0;

            ulong generated = RelayServerCore.FirstNonZeroGeneratedSeed(
                () => candidates[index++]);

            Assert.That(generated, Is.EqualTo(Seed));
            Assert.That(index, Is.EqualTo(3));
            Assert.Throws<ArgumentNullException>(() =>
                RelayServerCore.FirstNonZeroGeneratedSeed(null));
        }

        [Test]
        public void RelayServer_RejectsASequenceWhoseTargetTickMovesBackward()
        {
            string directory = Path.Combine(
                Path.GetTempPath(), "nova-target-order-test-" + Guid.NewGuid().ToString("N"));
            var logs = new List<string>();
            var server = new RelayServerCore(Token, Seed, Delay, directory, logs.Add);
            RawPeer raw0 = null;
            RawPeer raw1 = null;
            try
            {
                server.Start(0);
                (raw0, raw1) = StartRawMatch(server);
                raw0.Send(RelayFrameType.CommandRecord,
                    CreateStopRecord(0, 1, 97, Delay).Serialize());
                raw0.Send(RelayFrameType.CommandRecord,
                    CreateStopRecord(0, 2, 0, Delay).Serialize());

                PumpRawUntil(server, raw0, raw1,
                    () => server.PeerCount == 0 && raw1.PeerLost,
                    "backward target-tick rejection");
                Assert.That(logs, Has.Some.Contains("non-monotone"));
                Assert.That(RelayRecordStream.TryRead(
                        File.ReadAllBytes(server.LastRecordPath),
                        out RelayRecordStreamFile recording, out string readError),
                    Is.True, readError);
                Assert.That(recording.TerminalReason,
                    Is.EqualTo(RelayRecordTerminalReason.ProtocolViolation));
            }
            finally
            {
                raw0?.Dispose();
                raw1?.Dispose();
                server.Stop();
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }

        // ------------------------------------------------------------------
        // Drive helpers
        // ------------------------------------------------------------------

        private static (ClientHost, ClientHost) StartMatch(
            RelayServerCore server, RelayMatchClient clientA = null, RelayMatchClient clientB = null)
        {
            clientA = clientA ?? new RelayMatchClient();
            clientB = clientB ?? new RelayMatchClient();
            clientA.Connect("127.0.0.1", server.Port, Token);
            clientB.Connect("127.0.0.1", server.Port, Token);
            PumpUntil(server, clientA, clientB, () => clientA.HasOffer && clientB.HasOffer, "offers");
            ClientHost hostA = ClientHost.Create(clientA);
            ClientHost hostB = ClientHost.Create(clientB);
            clientA.SubmitLocalProof(hostA.CreateFingerprint().Serialize(), hostA.Kernel.SaveSnapshot());
            clientB.SubmitLocalProof(hostB.CreateFingerprint().Serialize(), hostB.Kernel.SaveSnapshot());
            PumpUntil(server, clientA, clientB,
                () => clientA.Phase == RelayClientPhase.Running && clientB.Phase == RelayClientPhase.Running,
                "match start");
            return (hostA, hostB);
        }

        private static void Drive(RelayServerCore server, RelayMatchClient clientA, RelayMatchClient clientB,
            ClientHost hostA, ClientHost hostB, uint untilTick)
        {
            long guard = 100_000;
            while ((hostA.Kernel.CurrentTick.Value < untilTick || hostB.Kernel.CurrentTick.Value < untilTick)
                && guard-- > 0)
            {
                server.Poll();
                clientA.Poll();
                clientB.Poll();
                if (hostA.Kernel.CurrentTick.Value < untilTick)
                {
                    hostA.RunScript();
                    clientA.TryStepTick(hostA.Kernel);
                }
                if (hostB.Kernel.CurrentTick.Value < untilTick)
                {
                    hostB.RunScript();
                    clientB.TryStepTick(hostB.Kernel);
                }
            }
            Assert.That(guard, Is.GreaterThan(0), "drive guard exhausted — a client wedged");
        }

        /// <summary>
        /// Steps ONLY the end that is behind, until both kernels stand on the
        /// same tick — the precondition for comparing state hashes at all.
        /// <para>
        /// The leader does not need to step again for this: completeness for
        /// tick X is announced at session tick X - InputDelay + 1, so an end
        /// that is ahead has already announced through the ticks the laggard
        /// still has to execute. And the per-slot script is keyed on the
        /// kernel tick (fires exactly once per tick value), so a laggard
        /// catching up issues exactly the commands the leader issued at those
        /// same ticks — stepping one side alone cannot change the outcome.
        /// </para>
        /// </summary>
        private static void DriveUntilLevel(RelayServerCore server, RelayMatchClient clientA, RelayMatchClient clientB,
            ClientHost hostA, ClientHost hostB)
        {
            long guard = 100_000;
            while (hostA.Kernel.CurrentTick.Value != hostB.Kernel.CurrentTick.Value && guard-- > 0)
            {
                server.Poll();
                clientA.Poll();
                clientB.Poll();
                if (hostA.Kernel.CurrentTick.Value < hostB.Kernel.CurrentTick.Value)
                {
                    hostA.RunScript();
                    clientA.TryStepTick(hostA.Kernel);
                }
                else
                {
                    hostB.RunScript();
                    clientB.TryStepTick(hostB.Kernel);
                }
            }
            Assert.That(guard, Is.GreaterThan(0), "level-up guard exhausted — the laggard could not catch up");
        }

        private static void PumpUntil(RelayServerCore server, RelayMatchClient clientA, RelayMatchClient clientB,
            Func<bool> condition, string what)
        {
            long guard = 100_000;
            while (!condition() && guard-- > 0)
            {
                server.Poll();
                clientA.Poll();
                clientB?.Poll();
            }
            Assert.That(guard, Is.GreaterThan(0), $"pump guard exhausted waiting for: {what}");
        }

        private static (ScriptedRelayServer, RelayMatchClient) ConnectScriptedClient()
        {
            var server = new ScriptedRelayServer();
            var client = new RelayMatchClient();
            client.Connect("127.0.0.1", server.Port, Token);
            server.AcceptClient();
            PollClientUntil(client,
                () => client.State == RelayConnectionState.Connected,
                "poll-driven connection and Hello send");
            server.PumpUntilReceived(RelayFrameType.Hello, "client Hello");
            Assert.That(client.Phase, Is.EqualTo(RelayClientPhase.WaitingOffer));
            return (server, client);
        }

        private static ClientHost AdvanceScriptedClientToWaitingStart(
            ScriptedRelayServer server, RelayMatchClient client)
        {
            server.Send(RelayFrameType.Offer, RelayProtocol.CreateOfferPayload(
                0, new byte[] { 0, 1 }, Seed, Delay,
                SimDefinitions.ComputeDefinitionsHash64()));
            PollClientUntil(client, () => client.HasOffer, "scripted Offer");
            ClientHost host = ClientHost.Create(client);
            client.SubmitLocalProof(
                host.CreateFingerprint().Serialize(), host.Kernel.SaveSnapshot());
            server.PumpUntilReceived(
                RelayFrameType.InitialSnapshot, "scripted proof submission");
            Assert.That(client.Phase, Is.EqualTo(RelayClientPhase.WaitingStart));
            return host;
        }

        private static void StartScriptedClient(
            ScriptedRelayServer server, RelayMatchClient client)
        {
            server.Send(RelayFrameType.Start, Array.Empty<byte>());
            PollClientUntil(client,
                () => client.Phase == RelayClientPhase.Running,
                "scripted Start");
            server.Pump();
        }

        private static void PollClientUntil(
            RelayMatchClient client, Func<bool> condition, string what)
        {
            long guard = 100_000;
            while (!condition() && guard-- > 0)
            {
                client.Poll();
                System.Threading.Thread.Yield();
            }
            Assert.That(guard, Is.GreaterThan(0),
                $"client pump guard exhausted waiting for: {what}");
        }

        private static CommandRecord CreateStopRecord(
            byte slot, uint sequence, uint enqueueTick, uint inputDelayTicks)
        {
            CommandIntent intent = CommandIntent.Create(new StopPayload(new uint[] { 1u << 10 }));
            return new CommandRecord(
                enqueueTick, enqueueTick + inputDelayTicks, slot, sequence,
                intent.Kind, intent.PayloadVersion, intent.PayloadBytes.ToArray());
        }

        private static (RawPeer, RawPeer) StartRawMatch(RelayServerCore server)
        {
            var raw0 = new RawPeer(server.Port);
            var raw1 = new RawPeer(server.Port);
            raw0.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
            raw1.Send(RelayFrameType.Hello, RelayProtocol.CreateHelloPayload(Token));
            PumpRawUntil(server, raw0, raw1,
                () => raw0.Offer != null && raw1.Offer != null,
                "raw offers");
            Assert.That(RelayProtocol.TryParseOffer(
                raw0.Offer, out byte slot0, out _, out _, out _, out _), Is.True);
            Assert.That(RelayProtocol.TryParseOffer(
                raw1.Offer, out byte slot1, out _, out _, out _, out _), Is.True);
            Assert.That(new[] { slot0, slot1 }, Is.EquivalentTo(new byte[] { 0, 1 }));

            ClientHost source = ClientHost.CreatePlayback();
            byte[] snapshot = source.Kernel.SaveSnapshot();
            byte[] fingerprint = source.CreateFingerprint().Serialize();
            raw0.Send(RelayFrameType.Fingerprint, fingerprint);
            raw0.Send(RelayFrameType.InitialSnapshot, snapshot);
            raw1.Send(RelayFrameType.Fingerprint, fingerprint);
            raw1.Send(RelayFrameType.InitialSnapshot, snapshot);
            PumpRawUntil(server, raw0, raw1,
                () => raw0.Started && raw1.Started,
                "raw match start");

            return slot0 == 0 ? (raw0, raw1) : (raw1, raw0);
        }

        private static void PumpRawUntil(
            RelayServerCore server, RawPeer a, RawPeer b,
            Func<bool> condition, string what)
        {
            long guard = 100_000;
            while (!condition() && guard-- > 0)
            {
                server.Poll();
                a.Pump();
                b.Pump();
            }
            Assert.That(guard, Is.GreaterThan(0), $"raw pump guard exhausted waiting for: {what}");
        }

        private sealed class CountingSeekableStream : Stream
        {
            private long _position;
            private long _length;

            public override bool CanRead => false;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => _length;
            public override long Position
            {
                get => _position;
                set
                {
                    if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                    _position = value;
                    if (_position > _length) _length = _position;
                }
            }

            public override void Flush() { }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                long target = origin == SeekOrigin.Begin
                    ? offset
                    : origin == SeekOrigin.Current
                        ? _position + offset
                        : _length + offset;
                Position = target;
                return _position;
            }

            public override void SetLength(long value)
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _length = value;
                if (_position > value) _position = value;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset > buffer.Length - count)
                {
                    throw new ArgumentOutOfRangeException();
                }
                Position = checked(_position + count);
            }

            public override void WriteByte(byte value)
            {
                Position = checked(_position + 1);
            }
        }

        private sealed class RawPeer : IDisposable
        {
            private readonly TcpClient _client = new TcpClient();
            private readonly RelayProtocol.FrameCutter _cutter = new RelayProtocol.FrameCutter();
            private readonly byte[] _buffer = new byte[64 * 1024];
            private NetworkStream _stream;

            public byte[] Offer { get; private set; }
            public bool Started { get; private set; }
            public bool PeerLost { get; private set; }
            public string RejectReason { get; private set; }

            public RawPeer(int port)
            {
                _client.NoDelay = true;
                _client.Connect("127.0.0.1", port);
                _stream = _client.GetStream();
            }

            public void Send(RelayFrameType type, byte[] payload)
            {
                byte[] frame = RelayProtocol.CreateFrame(type, payload);
                _stream.Write(frame, 0, frame.Length);
            }

            public void SendRaw(byte[] bytes)
            {
                _stream.Write(bytes, 0, bytes.Length);
            }

            public void Pump()
            {
                while (_client.Available > 0)
                {
                    int readCapacity = Math.Min(
                        _buffer.Length, _cutter.RemainingCapacity);
                    Assert.That(readCapacity, Is.GreaterThan(0),
                        "raw-peer frame carry must be drained before another read");
                    int read = _stream.Read(_buffer, 0, readCapacity);
                    if (read <= 0) break;
                    _cutter.Feed(_buffer.AsSpan(0, read));
                    while (_cutter.TryTakeFrame(
                        out RelayFrameType type, out byte[] payload))
                    {
                        if (type == RelayFrameType.Offer) Offer = payload;
                        else if (type == RelayFrameType.Start) Started = true;
                        else if (type == RelayFrameType.PeerLost) PeerLost = true;
                        else if (type == RelayFrameType.Reject)
                        {
                            RejectReason = RelayProtocol.ParseReasonPayload(payload);
                        }
                    }
                }
            }

            public void Dispose()
            {
                try { _stream?.Dispose(); } catch { /* ignore */ }
                try { _client.Dispose(); } catch { /* ignore */ }
            }
        }

        private sealed class ScriptedRelayServer : IDisposable
        {
            private sealed class ReceivedFrame
            {
                public RelayFrameType Type;
                public byte[] Payload;
            }

            private readonly TcpListener _listener;
            private readonly RelayProtocol.FrameCutter _cutter =
                new RelayProtocol.FrameCutter();
            private readonly byte[] _buffer = new byte[64 * 1024];
            private readonly List<ReceivedFrame> _received =
                new List<ReceivedFrame>();
            private TcpClient _client;
            private NetworkStream _stream;

            public ScriptedRelayServer()
            {
                _listener = new TcpListener(IPAddress.Loopback, 0);
                _listener.Start();
            }

            public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;

            public void AcceptClient()
            {
                _client = _listener.AcceptTcpClient();
                _client.NoDelay = true;
                _stream = _client.GetStream();
            }

            public void Send(RelayFrameType type, byte[] payload)
            {
                byte[] frame = RelayProtocol.CreateFrame(type, payload);
                _stream.Write(frame, 0, frame.Length);
            }

            public void Pump()
            {
                while (_client.Available > 0)
                {
                    int readCapacity = Math.Min(
                        _buffer.Length, _cutter.RemainingCapacity);
                    Assert.That(readCapacity, Is.GreaterThan(0));
                    int read = _stream.Read(_buffer, 0, readCapacity);
                    if (read <= 0) break;
                    _cutter.Feed(_buffer.AsSpan(0, read));
                    while (_cutter.TryTakeFrame(
                        out RelayFrameType type, out byte[] payload))
                    {
                        _received.Add(new ReceivedFrame
                        {
                            Type = type,
                            Payload = payload,
                        });
                    }
                }
            }

            public void PumpUntilReceived(RelayFrameType type, string what)
            {
                long guard = 100_000;
                while (ReceivedCount(type) == 0 && guard-- > 0)
                {
                    Pump();
                }
                Assert.That(guard, Is.GreaterThan(0),
                    $"scripted relay guard exhausted waiting for: {what}");
            }

            public int ReceivedCount(RelayFrameType type)
            {
                int count = 0;
                for (int i = 0; i < _received.Count; i++)
                {
                    if (_received[i].Type == type) count++;
                }
                return count;
            }

            public bool TryGetFirstPayload(
                RelayFrameType type, out byte[] payload)
            {
                for (int i = 0; i < _received.Count; i++)
                {
                    if (_received[i].Type != type) continue;
                    payload = _received[i].Payload;
                    return true;
                }
                payload = null;
                return false;
            }

            public void Dispose()
            {
                try { _stream?.Dispose(); } catch { /* ignore */ }
                try { _client?.Dispose(); } catch { /* ignore */ }
                try { _listener.Stop(); } catch { /* ignore */ }
            }
        }
    }
}
