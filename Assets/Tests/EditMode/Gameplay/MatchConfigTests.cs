using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Nova.Gameplay.Match;
using Nova.Networking;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.State;

namespace Nova.Gameplay.Tests
{
    [TestFixture]
    public sealed class MatchConfigTests
    {
        [Test]
        public void LocalDefault_ValidatesAndClones_AllOwnedArrays()
        {
            MatchConfig source = MatchConfig.LocalVsAi(42, 128, 128, 1024, 3000);
            MatchConfig clone = source.ValidateAndClone();

            Assert.That(clone.LocalSlot, Is.EqualTo((byte)0));
            Assert.That(clone.ActiveSlots, Is.EqualTo(new byte[] { 0, 1 }));
            Assert.That(clone.FactionPerSlot[0], Is.EqualTo(FactionId.Alliance));
            Assert.That(clone.FactionPerSlot[1], Is.EqualTo(FactionId.Legion));
            Assert.That(clone.AiSlots, Is.EqualTo(new byte[] { 1 }));
            Assert.That(clone.InputDelayTicks, Is.EqualTo(1));
            Assert.That(clone.Transport, Is.Null);
            Assert.That(clone.ActiveSlots, Is.Not.SameAs(source.ActiveSlots));
            Assert.That(clone.FactionPerSlot, Is.Not.SameAs(source.FactionPerSlot));
            Assert.That(clone.AiSlots, Is.Not.SameAs(source.AiSlots));

            source.ActiveSlots[0] = 7;
            source.FactionPerSlot[0] = FactionId.Legion;
            source.AiSlots[0] = 0;
            Assert.That(clone.ActiveSlots[0], Is.EqualTo(0));
            Assert.That(clone.FactionPerSlot[0], Is.EqualTo(FactionId.Alliance));
            Assert.That(clone.AiSlots[0], Is.EqualTo(1));
        }

        [Test]
        public void InvalidSlotAiFactionDelayAndTransport_AreRejected()
        {
            var config = new MatchConfig { LocalSlot = 7 };
            Assert.Throws<ArgumentException>(() => config.ValidateAndClone());

            config = new MatchConfig { ActiveSlots = new byte[] { 0, 0 } };
            Assert.Throws<ArgumentException>(() => config.ValidateAndClone());

            config = new MatchConfig { AiSlots = new byte[] { 0 } };
            Assert.Throws<ArgumentException>(() => config.ValidateAndClone());

            config = new MatchConfig { InputDelayTicks = 0 };
            Assert.Throws<ArgumentOutOfRangeException>(() => config.ValidateAndClone());

            config = new MatchConfig { InputDelayTicks = RelayProtocol.MaxInputDelayTicks + 1 };
            Assert.Throws<ArgumentOutOfRangeException>(() => config.ValidateAndClone());

            config = new MatchConfig { InputDelayTicks = uint.MaxValue };
            Assert.Throws<ArgumentOutOfRangeException>(() => config.ValidateAndClone());

            config = new MatchConfig();
            config.FactionPerSlot[1] = (FactionId)99;
            Assert.Throws<ArgumentException>(() => config.ValidateAndClone());

            config = MatchConfig.NetworkVsHuman("127.0.0.1", 47777, 1);
            MatchConfig networkClone = config.ValidateAndClone();
            Assert.That(networkClone.Transport, Is.SameAs(config.Transport),
                "the session transport is shared deliberately while mutable arrays are cloned");

            config.RelayHost = string.Empty;
            Assert.Throws<ArgumentException>(() => config.ValidateAndClone());
        }

        [Test]
        public void MatchRunner_UsesConfiguredLocalAndAiSlotsAndDelay()
        {
            var go = new GameObject("ConfiguredMatchRunner");
            try
            {
                MatchRunner runner = go.AddComponent<MatchRunner>();
                var config = new MatchConfig
                {
                    Seed = 99,
                    LocalSlot = 1,
                    AiSlots = new byte[] { 0 },
                    InputDelayTicks = 3,
                };
                runner.InitializeMatch(config);

                Assert.That(runner.Session.LocalSlot, Is.EqualTo((byte)1));
                Assert.That(runner.Session.InputDelayTicks, Is.EqualTo(3));
                Assert.That(runner.AiSession, Is.Not.Null);
                Assert.That(runner.AiSession.LocalSlot, Is.EqualTo((byte)0));
                Assert.That(runner.SkirmishAi, Is.Not.Null);
                Assert.That(runner.RelayClient, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NetworkRestart_IsRefusedWithoutFreshOfferAndTransport()
        {
            var go = new GameObject("NetworkRestartBootstrap");
            try
            {
                go.AddComponent<MatchRunner>();
                MatchBootstrap bootstrap = go.AddComponent<MatchBootstrap>();
                bootstrap.AutoStart = false;
                var network = MatchConfig.NetworkVsHuman("127.0.0.1", 47777, 1);
                typeof(MatchBootstrap).GetField(
                    "_activeConfig", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(bootstrap, network.ValidateAndClone());

                LogAssert.Expect(LogType.Error,
                    "[MatchBootstrap] Network restart refused: a fresh relay offer and a fresh transport are required.");
                Assert.That(bootstrap.RestartMatch(), Is.False);
                Assert.That(bootstrap.IsMatchReady, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void NetworkEnvironment_RequiresExactly16UnprefixedHexCharacters()
        {
            const string validToken = "0123456789ABCDEF";
            string previousPort = Environment.GetEnvironmentVariable("NOVA_RELAY_PORT");
            string previousToken = Environment.GetEnvironmentVariable("NOVA_MATCH_TOKEN");
            MethodInfo reader = typeof(MatchBootstrap).GetMethod(
                "TryReadNetworkEnvironment", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(reader, Is.Not.Null);
            try
            {
                Environment.SetEnvironmentVariable("NOVA_RELAY_PORT", "47777");
                Environment.SetEnvironmentVariable("NOVA_MATCH_TOKEN", validToken);
                object[] args = { 0, 0UL, string.Empty };
                Assert.That((bool)reader.Invoke(null, args), Is.True);
                Assert.That((int)args[0], Is.EqualTo(47777));
                Assert.That((ulong)args[1], Is.EqualTo(0x0123456789ABCDEFUL));

                Environment.SetEnvironmentVariable("NOVA_MATCH_TOKEN", "0x0123456789ABCDEF");
                args = new object[] { 0, 0UL, string.Empty };
                Assert.That((bool)reader.Invoke(null, args), Is.False);
                Assert.That((string)args[2], Does.Not.Contain("0123456789ABCDEF"),
                    "the match token must never be echoed in an error");
            }
            finally
            {
                Environment.SetEnvironmentVariable("NOVA_RELAY_PORT", previousPort);
                Environment.SetEnvironmentVariable("NOVA_MATCH_TOKEN", previousToken);
            }
        }

        [Test]
        public void MenuJoinValidation_IsFailClosed_AndNeverEchoesTheMatchCode()
        {
            var go = new GameObject("NetworkJoinValidation");
            try
            {
                go.AddComponent<MatchRunner>();
                MatchBootstrap bootstrap = go.AddComponent<MatchBootstrap>();
                bootstrap.AutoStart = false;

                LogAssert.Expect(LogType.Error,
                    "[MatchBootstrap] Network join failed: Die Serveradresse darf nicht leer sein.");
                Assert.That(bootstrap.TryStartNetworkJoin(
                    "", 47777, "0123456789ABCDEF", NetworkJoinRole.Host), Is.False);
                Assert.That(bootstrap.JoinStatus.Failure, Is.EqualTo(NetworkJoinFailure.InvalidHost));
                Assert.That(bootstrap.JoinStatus.Message, Does.Not.Contain("0123456789ABCDEF"));

                LogAssert.Expect(LogType.Error,
                    "[MatchBootstrap] Network join failed: Der Port muss eine Zahl von 1 bis 65535 sein.");
                Assert.That(bootstrap.TryStartNetworkJoin(
                    "127.0.0.1", " 47777", "0123456789ABCDEF", NetworkJoinRole.Host), Is.False);
                Assert.That(bootstrap.JoinStatus.Failure, Is.EqualTo(NetworkJoinFailure.InvalidPort));

                LogAssert.Expect(LogType.Error,
                    "[MatchBootstrap] Network join failed: Der Match-Code muss aus genau 16 Hexzeichen bestehen und darf nicht null sein.");
                Assert.That(bootstrap.TryStartNetworkJoin(
                    "127.0.0.1", 47777, "0x0123456789ABCDEF", NetworkJoinRole.Host), Is.False);
                Assert.That(bootstrap.JoinStatus.Failure, Is.EqualTo(NetworkJoinFailure.InvalidMatchCode));
                Assert.That(bootstrap.JoinStatus.Message, Does.Not.Contain("0123456789ABCDEF"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void MenuJoin_CancelDuringConnect_DropsTheClientAndReturnsToIdle()
        {
            var go = new GameObject("NetworkJoinCancel");
            try
            {
                go.AddComponent<MatchRunner>();
                MatchBootstrap bootstrap = go.AddComponent<MatchBootstrap>();
                bootstrap.AutoStart = false;

                Assert.That(bootstrap.TryStartNetworkJoin(
                    "127.0.0.1", 47777, "0123456789ABCDEF", NetworkJoinRole.Host), Is.True);
                Assert.That(bootstrap.JoinStatus.Phase, Is.EqualTo(NetworkJoinPhase.Connecting));
                Assert.That(bootstrap.JoinStatus.CanCancel, Is.True);
                RelayMatchClient firstClient = bootstrap.NetworkClient;
                Assert.That(firstClient, Is.Not.Null);

                Assert.That(bootstrap.CancelNetworkJoin(), Is.True);
                Assert.That(bootstrap.JoinStatus.Phase, Is.EqualTo(NetworkJoinPhase.Idle));
                Assert.That(bootstrap.NetworkClient, Is.Null);
                Assert.That(bootstrap.IsMatchReady, Is.False);

                Assert.That(bootstrap.TryStartNetworkJoin(
                    "127.0.0.1", 47777, "0123456789ABCDEF", NetworkJoinRole.Host), Is.True);
                RelayMatchClient retryClient = bootstrap.NetworkClient;
                Assert.That(retryClient, Is.Not.Null);
                Assert.That(retryClient, Is.Not.SameAs(firstClient),
                    "every retry must own a fresh single-session relay client");

                Assert.That(bootstrap.CancelNetworkJoin(), Is.True);
                Assert.That(bootstrap.JoinStatus.Phase, Is.EqualTo(NetworkJoinPhase.Idle));
                Assert.That(bootstrap.NetworkClient, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RelayRunner_RefusesLocalPauseAndASecondKernelStart()
        {
            var go = new GameObject("RelayPauseGuard");
            try
            {
                MatchRunner runner = go.AddComponent<MatchRunner>();
                MatchConfig network = MatchConfig.NetworkVsHuman("127.0.0.1", 47777, 1);
                runner.InitializeMatch(network);
                Assert.That(runner.StartMatch(), Is.True);

                LogAssert.Expect(LogType.Error,
                    "[MatchRunner] Local pause refused for a relay match.");
                Assert.That(runner.PauseMatch(), Is.False);
                Assert.That(runner.IsRunning, Is.True);

                LogAssert.Expect(LogType.Error,
                    "[MatchRunner] Relay match start/resume refused: a started network kernel cannot be reset.");
                Assert.That(runner.StartMatch(), Is.False);
                Assert.That(runner.Kernel.CurrentTick.Value, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RelayRunner_IngressUsesTheAuthoritativeStartGate()
        {
            var go = new GameObject("RelayIngressStartGate");
            try
            {
                MatchRunner runner = go.AddComponent<MatchRunner>();
                runner.InitializeMatch(
                    MatchConfig.NetworkVsHuman("127.0.0.1", 47777, 1));

                var stop = new StopPayload(new uint[] { 1u << 10 });
                Assert.That(
                    runner.Ingress.TrySubmitIntent(
                        CommandIntent.Create(stop), out CommandRejectReason streamReason),
                    Is.EqualTo(CommandIngressResult.Rejected));
                Assert.That(streamReason, Is.EqualTo(CommandRejectReason.TransportNotReady));
                Assert.That(
                    runner.Ingress.TrySubmitIntent(
                        CommandIntent.ForSessionAction(CommandKind.PauseRequest),
                        out CommandRejectReason actionReason),
                    Is.EqualTo(CommandIngressResult.Rejected));
                Assert.That(actionReason, Is.EqualTo(CommandRejectReason.TransportNotReady));
                Assert.That(runner.Ingress.DedupeState.NextLocalSequence(0), Is.EqualTo(1));
                Assert.That(runner.Ingress.PendingCount, Is.Zero);
                Assert.That(runner.Ingress.PendingSessionActionCount, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
