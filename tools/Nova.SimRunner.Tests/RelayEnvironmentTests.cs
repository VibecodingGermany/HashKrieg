using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;
using Nova.RelayServer;

namespace Nova.SimRunner.Tests
{
    [TestFixture]
    public sealed class RelayEnvironmentTests
    {
        private const string ValidToken = "0123456789ABCDEF";

        [Test]
        public void RequiredToken_WithAbsentOptionalValues_UsesFailSafeDefaults()
        {
            var values = ValidValues();

            Assert.That(RelayEnvironment.TryParse(
                    values, out RelayEnvironment environment, out string error),
                Is.True, error);
            Assert.That(environment.MatchToken, Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(environment.BindAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(environment.Port, Is.EqualTo(47_777));
            Assert.That(environment.SlotCount, Is.EqualTo(2));
            Assert.That(environment.InputDelayTicks, Is.EqualTo(3));
            Assert.That(environment.RecordDirectory, Is.Empty);
            Assert.That(environment.RecordingEnabled, Is.False);
            Assert.That(environment.Seed, Is.Zero);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("0000000000000000")]
        [TestCase("0123456789ABCDE")]
        [TestCase("0123456789ABCDEF0")]
        [TestCase("0x123456789ABCDE")]
        [TestCase("SECRET1234567890")]
        [TestCase(" 123456789ABCDE")]
        public void MatchToken_IsExactNonZeroHex_AndNeverLeaks(string token)
        {
            var values = new Dictionary<string, string>();
            if (token != null) values.Add(RelayEnvironment.MatchTokenVariable, token);

            AssertRejectedWithoutValue(values, token, RelayEnvironment.MatchTokenVariable);
        }

        [TestCase("1024", 1024)]
        [TestCase("65535", 65535)]
        public void Port_AcceptsInclusiveBoundaries(string text, int expected)
        {
            RelayEnvironment environment = ParseWith(RelayEnvironment.PortVariable, text);
            Assert.That(environment.Port, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("1023")]
        [TestCase("65536")]
        [TestCase("+1024")]
        [TestCase(" 1024")]
        [TestCase("not-a-port")]
        public void Port_RejectsAnythingOutsideStrictDecimalRange(string text)
        {
            AssertRejectedWithoutValue(
                With(RelayEnvironment.PortVariable, text), text,
                RelayEnvironment.PortVariable);
        }

        [TestCase("127.0.0.1")]
        [TestCase("0.0.0.0")]
        [TestCase("::1")]
        public void Bind_AcceptsNumericAddresses(string text)
        {
            RelayEnvironment environment = ParseWith(RelayEnvironment.BindVariable, text);
            Assert.That(environment.BindAddress, Is.EqualTo(IPAddress.Parse(text)));
        }

        [TestCase("")]
        [TestCase("localhost")]
        [TestCase(" 127.0.0.1")]
        public void Bind_RejectsNonNumericOrDecoratedValues(string text)
        {
            AssertRejectedWithoutValue(
                With(RelayEnvironment.BindVariable, text), text,
                RelayEnvironment.BindVariable);
        }

        [Test]
        public void SlotCount_AcceptsOnlyTwo()
        {
            Assert.That(RelayEnvironment.RequiredSlotCount,
                Is.EqualTo(Nova.Networking.RelayServerCore.MaxPeers));
            Assert.That(ParseWith(RelayEnvironment.SlotCountVariable, "2").SlotCount,
                Is.EqualTo(2));
            AssertRejectedWithoutValue(
                With(RelayEnvironment.SlotCountVariable, "1"), "1",
                RelayEnvironment.SlotCountVariable);
            AssertRejectedWithoutValue(
                With(RelayEnvironment.SlotCountVariable, "3"), "3",
                RelayEnvironment.SlotCountVariable);
        }

        [TestCase("1", 1u)]
        [TestCase("60", 60u)]
        public void Delay_AcceptsInclusiveBoundaries(string text, uint expected)
        {
            Assert.That(ParseWith(RelayEnvironment.InputDelayVariable, text).InputDelayTicks,
                Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("0")]
        [TestCase("61")]
        [TestCase("+3")]
        [TestCase("not-a-delay")]
        public void Delay_RejectsAnythingOutsideStrictDecimalRange(string text)
        {
            AssertRejectedWithoutValue(
                With(RelayEnvironment.InputDelayVariable, text), text,
                RelayEnvironment.InputDelayVariable);
        }

        [Test]
        public void RecordDirectory_IsOffOnlyWhenAbsent_AndOtherwiseAbsoluteNonRoot()
        {
            RelayEnvironment accepted = ParseWith(
                RelayEnvironment.RecordDirectoryVariable, "/var/lib/hashkrieg-relay");
            Assert.That(accepted.RecordDirectory, Is.EqualTo("/var/lib/hashkrieg-relay"));
            Assert.That(accepted.RecordingEnabled, Is.True);

            AssertRejectedWithoutValue(
                With(RelayEnvironment.RecordDirectoryVariable, ""), "",
                RelayEnvironment.RecordDirectoryVariable);
            AssertRejectedWithoutValue(
                With(RelayEnvironment.RecordDirectoryVariable, "/"), "/",
                RelayEnvironment.RecordDirectoryVariable);
            AssertRejectedWithoutValue(
                With(RelayEnvironment.RecordDirectoryVariable, "/./"), "/./",
                RelayEnvironment.RecordDirectoryVariable);
            AssertRejectedWithoutValue(
                With(RelayEnvironment.RecordDirectoryVariable, "/tmp/.."), "/tmp/..",
                RelayEnvironment.RecordDirectoryVariable);
            AssertRejectedWithoutValue(
                With(RelayEnvironment.RecordDirectoryVariable, "relative/records"),
                "relative/records", RelayEnvironment.RecordDirectoryVariable);
        }

        [Test]
        public void Seed_IsGeneratedWhenAbsent_AndStrictWhenPresent()
        {
            Assert.That(ParseWith(
                    RelayEnvironment.SeedVariable, "FEDCBA9876543210").Seed,
                Is.EqualTo(0xFEDCBA9876543210UL));

            foreach (string rejected in new[]
            {
                "", "0000000000000000", "1234", "0x123456789ABCDE", "SEED123456789012",
            })
            {
                AssertRejectedWithoutValue(
                    With(RelayEnvironment.SeedVariable, rejected), rejected,
                    RelayEnvironment.SeedVariable);
            }
        }

        [Test]
        public void ErrorMessages_DoNotEchoSensitiveOrOtherSuppliedValues()
        {
            var values = ValidValues();
            const string sensitive = "PRIVATE_MATCH_TOKEN";
            values[RelayEnvironment.MatchTokenVariable] = sensitive;
            values[RelayEnvironment.BindVariable] = "private-host.internal";
            values[RelayEnvironment.RecordDirectoryVariable] = "/private/operator/path";

            Assert.That(RelayEnvironment.TryParse(
                    values, out _, out string error), Is.False);
            Assert.That(error, Does.Not.Contain(sensitive));
            Assert.That(error, Does.Not.Contain("private-host.internal"));
            Assert.That(error, Does.Not.Contain("/private/operator/path"));
        }

        [Test]
        public void ExplicitValuesArePreserved_AndUnknownEnvironmentIsIgnored()
        {
            var values = ValidValues();
            values[RelayEnvironment.BindVariable] = "::1";
            values[RelayEnvironment.PortVariable] = "1024";
            values[RelayEnvironment.SlotCountVariable] = "2";
            values[RelayEnvironment.InputDelayVariable] = "60";
            values[RelayEnvironment.RecordDirectoryVariable] = "/var/lib/hashkrieg-relay/records";
            values[RelayEnvironment.SeedVariable] = "abcdef0123456789";
            values["UNRELATED_PROCESS_VARIABLE"] = "ignored";

            Assert.That(RelayEnvironment.TryParse(
                    values, out RelayEnvironment environment, out string error),
                Is.True, error);
            Assert.That(environment.BindAddress, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(environment.Port, Is.EqualTo(1024));
            Assert.That(environment.SlotCount, Is.EqualTo(2));
            Assert.That(environment.InputDelayTicks, Is.EqualTo(60));
            Assert.That(environment.RecordDirectory,
                Is.EqualTo("/var/lib/hashkrieg-relay/records"));
            Assert.That(environment.Seed, Is.EqualTo(0xABCDEF0123456789UL));
        }

        private static RelayEnvironment ParseWith(string name, string value)
        {
            var values = With(name, value);
            Assert.That(RelayEnvironment.TryParse(
                    values, out RelayEnvironment environment, out string error),
                Is.True, error);
            return environment;
        }

        private static Dictionary<string, string> With(string name, string value)
        {
            var values = ValidValues();
            values[name] = value;
            return values;
        }

        private static Dictionary<string, string> ValidValues()
        {
            return new Dictionary<string, string>
            {
                [RelayEnvironment.MatchTokenVariable] = ValidToken,
            };
        }

        private static void AssertRejectedWithoutValue(
            IReadOnlyDictionary<string, string> values, string suppliedValue, string variable)
        {
            Assert.That(RelayEnvironment.TryParse(
                    values, out RelayEnvironment environment, out string error),
                Is.False);
            Assert.That(environment, Is.Null);
            Assert.That(error, Does.Contain(variable));
            // Short numeric values can legitimately occur in the rule text
            // itself (for example 0 in the upper bound 60). Longer sentinels
            // must never be reflected back.
            if (suppliedValue != null && suppliedValue.Length >= 8)
            {
                Assert.That(error, Does.Not.Contain(suppliedValue));
            }
        }
    }

    [TestFixture]
    public sealed class RelayHostTests
    {
        private const string ExceptionSentinel = "PRIVATE_EXCEPTION_DETAIL";

        [Test]
        public void InvalidArguments_ReturnConfigurationExitWithoutReadingEnvironment()
        {
            var harness = new HostHarness();
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(
                new[] { "--unexpected" }, stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitConfiguration));
            Assert.That(harness.ConfigurationReads, Is.Zero);
            Assert.That(harness.PreflightCalls, Is.Zero);
            Assert.That(harness.FactoryCalls, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfigurationFailure_ReturnsConfigurationExitWithoutLeakingDetails(
            bool throws)
        {
            var harness = new HostHarness
            {
                ConfigurationSucceeds = false,
                ConfigurationThrows = throws,
            };
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitConfiguration));
            Assert.That(harness.PreflightCalls, Is.Zero);
            Assert.That(harness.FactoryCalls, Is.Zero);
            AssertNoExceptionDetail(harness);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreflightFailure_ReturnsConfigurationExitWithoutLeakingDetails(
            bool throws)
        {
            var harness = new HostHarness
            {
                PreflightSucceeds = false,
                PreflightThrows = throws,
            };
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitConfiguration));
            Assert.That(harness.PreflightCalls, Is.EqualTo(1));
            Assert.That(harness.FactoryCalls, Is.Zero);
            AssertNoExceptionDetail(harness);
        }

        [Test]
        public void RuntimeFactoryException_ReturnsRuntimeFailureWithoutStopOrLeak()
        {
            var harness = new HostHarness { FactoryThrows = true };
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitRuntimeFailure));
            Assert.That(harness.Runtime.StartCalls, Is.Zero);
            Assert.That(harness.Runtime.StopCalls, Is.Zero);
            AssertNoExceptionDetail(harness);
        }

        [Test]
        public void StartException_AttemptsOneStopWithoutClaimingStoppedOrLeakingDetails()
        {
            var harness = new HostHarness();
            harness.Runtime.StartAction = () => throw new InvalidOperationException(
                ExceptionSentinel);
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitRuntimeFailure));
            Assert.That(harness.Runtime.StartCalls, Is.EqualTo(1));
            Assert.That(harness.Runtime.StopCalls, Is.EqualTo(1));
            Assert.That(harness.Output.ToString(), Does.Not.Contain("[Relay] stopped."));
            AssertNoExceptionDetail(harness);
        }

        [Test]
        public void PollException_ReturnsRuntimeFailureAndStopsExactlyOnce()
        {
            var harness = new HostHarness();
            harness.Runtime.PollAction = () => throw new InvalidOperationException(
                ExceptionSentinel);
            using var stopRequested = new ManualResetEventSlim(false);

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitRuntimeFailure));
            Assert.That(harness.Runtime.StartCalls, Is.EqualTo(1));
            Assert.That(harness.Runtime.PollCalls, Is.EqualTo(1));
            Assert.That(harness.Runtime.StopCalls, Is.EqualTo(1));
            Assert.That(harness.Output.ToString(), Does.Contain("[Relay] stopped."));
            AssertNoExceptionDetail(harness);
        }

        [Test]
        public void StopException_ReturnsRuntimeFailureWithoutClaimingStopped()
        {
            var harness = new HostHarness();
            harness.Runtime.StopAction = () => throw new InvalidOperationException(
                ExceptionSentinel);
            using var stopRequested = new ManualResetEventSlim(false);
            harness.Runtime.PollAction = stopRequested.Set;

            int exitCode = harness.CreateHost().Run(Array.Empty<string>(), stopRequested);

            Assert.That(exitCode, Is.EqualTo(RelayHost.ExitRuntimeFailure));
            Assert.That(harness.Runtime.StopCalls, Is.EqualTo(1));
            Assert.That(harness.Output.ToString(), Does.Not.Contain("[Relay] stopped."));
            AssertNoExceptionDetail(harness);
        }

        [Test]
        public void SigintBeforePreflight_ExitsZeroWithoutCreatingRuntime()
        {
            var harness = new HostHarness();
            var registrations = new Dictionary<PosixSignal, Action>();

            int exitCode = Program.Run(
                Array.Empty<string>(),
                harness.CreateHost(),
                (signal, requestStop) =>
                {
                    registrations.Add(signal, requestStop);
                    if (signal == PosixSignal.SIGINT) requestStop();
                    return new NoopDisposable();
                });

            Assert.That(exitCode, Is.Zero);
            Assert.That(registrations.Keys,
                Is.EquivalentTo(new[] { PosixSignal.SIGINT, PosixSignal.SIGTERM }));
            Assert.That(harness.ConfigurationReads, Is.EqualTo(1));
            Assert.That(harness.PreflightCalls, Is.Zero);
            Assert.That(harness.FactoryCalls, Is.Zero);
        }

        [Test]
        public void SigtermDuringStart_ExitsZeroAndStopsExactlyOnceWithoutReadyClaim()
        {
            var harness = new HostHarness();
            var registrations = new Dictionary<PosixSignal, Action>();
            harness.Runtime.StartAction = () => registrations[PosixSignal.SIGTERM]();

            int exitCode = Program.Run(
                Array.Empty<string>(),
                harness.CreateHost(),
                (signal, requestStop) =>
                {
                    registrations.Add(signal, requestStop);
                    return new NoopDisposable();
                });

            Assert.That(exitCode, Is.Zero);
            Assert.That(harness.Runtime.StartCalls, Is.EqualTo(1));
            Assert.That(harness.Runtime.PollCalls, Is.Zero);
            Assert.That(harness.Runtime.StopCalls, Is.EqualTo(1));
            Assert.That(harness.Output.ToString(), Does.Not.Contain("[Relay] ready on "));
            Assert.That(harness.Output.ToString(), Does.Contain("[Relay] stopped."));
        }

        [Test]
        public void SigintAfterReady_ExitsZeroAndStopsExactlyOnce()
        {
            var harness = new HostHarness();
            var registrations = new Dictionary<PosixSignal, Action>();
            harness.Runtime.PollAction = () => registrations[PosixSignal.SIGINT]();

            int exitCode = Program.Run(
                Array.Empty<string>(),
                harness.CreateHost(),
                (signal, requestStop) =>
                {
                    registrations.Add(signal, requestStop);
                    return new NoopDisposable();
                });

            Assert.That(exitCode, Is.Zero);
            Assert.That(harness.Runtime.PollCalls, Is.EqualTo(1));
            Assert.That(harness.Runtime.StopCalls, Is.EqualTo(1));
            Assert.That(harness.Output.ToString(), Does.Contain("[Relay] ready on "));
            Assert.That(harness.Output.ToString(), Does.Contain("[Relay] stopped."));
        }

        private static void AssertNoExceptionDetail(HostHarness harness)
        {
            Assert.That(harness.Output.ToString(), Does.Not.Contain(ExceptionSentinel));
            Assert.That(harness.Error.ToString(), Does.Not.Contain(ExceptionSentinel));
        }

        private sealed class HostHarness
        {
            private readonly RelayEnvironment _configuration = CreateConfiguration();

            internal readonly StringWriter Output = new StringWriter();
            internal readonly StringWriter Error = new StringWriter();
            internal readonly FakeRuntime Runtime = new FakeRuntime();

            internal bool ConfigurationSucceeds = true;
            internal bool ConfigurationThrows;
            internal bool PreflightSucceeds = true;
            internal bool PreflightThrows;
            internal bool FactoryThrows;
            internal int ConfigurationReads;
            internal int PreflightCalls;
            internal int FactoryCalls;

            internal RelayHost CreateHost()
            {
                return new RelayHost(
                    Output, Error, ReadConfiguration, Preflight, CreateRuntime);
            }

            private bool ReadConfiguration(
                out RelayEnvironment configuration, out string error)
            {
                ConfigurationReads++;
                if (ConfigurationThrows)
                {
                    throw new InvalidOperationException(ExceptionSentinel);
                }
                configuration = ConfigurationSucceeds ? _configuration : null;
                error = ConfigurationSucceeds
                    ? string.Empty
                    : RelayEnvironment.MatchTokenVariable + " is invalid.";
                return ConfigurationSucceeds;
            }

            private bool Preflight(string recordDirectory, out string error)
            {
                PreflightCalls++;
                if (PreflightThrows)
                {
                    throw new IOException(ExceptionSentinel);
                }
                error = PreflightSucceeds
                    ? string.Empty
                    : RelayEnvironment.RecordDirectoryVariable + " is unavailable.";
                return PreflightSucceeds;
            }

            private IRelayRuntime CreateRuntime(
                RelayEnvironment configuration, Action<string> log)
            {
                FactoryCalls++;
                if (FactoryThrows)
                {
                    throw new InvalidOperationException(ExceptionSentinel);
                }
                return Runtime;
            }
        }

        private sealed class FakeRuntime : IRelayRuntime
        {
            internal Action StartAction;
            internal Action PollAction;
            internal Action StopAction;
            internal int StartCalls;
            internal int PollCalls;
            internal int StopCalls;

            public int Port => 47_777;

            public void Start(int port, IPAddress bindAddress)
            {
                StartCalls++;
                StartAction?.Invoke();
            }

            public void Poll()
            {
                PollCalls++;
                PollAction?.Invoke();
            }

            public void Stop()
            {
                StopCalls++;
                StopAction?.Invoke();
            }
        }

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }

        private static RelayEnvironment CreateConfiguration()
        {
            var values = new Dictionary<string, string>
            {
                [RelayEnvironment.MatchTokenVariable] = "0123456789ABCDEF",
            };
            if (!RelayEnvironment.TryParse(
                    values, out RelayEnvironment configuration, out string error))
            {
                throw new InvalidOperationException(error);
            }
            return configuration;
        }
    }
}
