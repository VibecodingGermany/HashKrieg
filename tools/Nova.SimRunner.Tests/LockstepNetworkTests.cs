using System;
using System.Collections.Generic;
using System.IO;
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
    /// every 50-tick checkpoint and at the end of 10.000 ticks: this is the
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
        // A8 stage 1: the 10.000-tick two-client soak
        // ------------------------------------------------------------------

        [Test]
        public void TwoClients_OverRealRelay_StayBitIdentical_For10000Ticks()
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
            const int targetTicks = 10_000;
            long guard = targetTicks * 40L;
            while ((hostA.Kernel.CurrentTick.Value < targetTicks || hostB.Kernel.CurrentTick.Value < targetTicks)
                && guard-- > 0)
            {
                server.Poll();
                clientA.Poll();
                clientB.Poll();

                hostA.RunScript();
                hostB.RunScript();

                bool steppedA = clientA.TryStepTick(hostA.Kernel);
                bool steppedB = clientB.TryStepTick(hostB.Kernel);

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

            // The relay recorded the command stream for desync reproduction.
            string[] recordings = Directory.GetFiles(recordDir, "*.novarec");
            Assert.That(recordings, Has.Length.EqualTo(1), "one command-stream dump per match");
            Assert.That(new FileInfo(recordings[0]).Length, Is.GreaterThan(100),
                "the dump carries fingerprint, snapshot and records");
            server.Stop();
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

        // ------------------------------------------------------------------
        // Drive helpers
        // ------------------------------------------------------------------

        private static (ClientHost, ClientHost) StartMatch(RelayServerCore server)
        {
            var clientA = new RelayMatchClient();
            var clientB = new RelayMatchClient();
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
                hostA.RunScript();
                hostB.RunScript();
                clientA.TryStepTick(hostA.Kernel);
                clientB.TryStepTick(hostB.Kernel);
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
    }
}
