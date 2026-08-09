using System;
using System.IO;
using System.Net;
using System.Threading;
using Nova.Networking;

namespace Nova.RelayServer
{
    internal delegate bool RelayConfigurationReader(
        out RelayEnvironment configuration, out string error);

    internal delegate bool RelayRecordDirectoryPreflight(
        string recordDirectory, out string error);

    internal interface IRelayRuntime
    {
        int Port { get; }
        void Start(int port, IPAddress bindAddress);
        void Poll();
        void Stop();
    }

    /// <summary>Testable configuration, startup, polling, and shutdown lifecycle.</summary>
    internal sealed class RelayHost
    {
        internal const int ExitRuntimeFailure = 1;
        internal const int ExitConfiguration = 78;

        private readonly TextWriter _output;
        private readonly TextWriter _error;
        private readonly RelayConfigurationReader _readConfiguration;
        private readonly RelayRecordDirectoryPreflight _preflightRecordDirectory;
        private readonly Func<RelayEnvironment, Action<string>, IRelayRuntime> _createRuntime;

        internal RelayHost(
            TextWriter output,
            TextWriter error,
            RelayConfigurationReader readConfiguration,
            RelayRecordDirectoryPreflight preflightRecordDirectory,
            Func<RelayEnvironment, Action<string>, IRelayRuntime> createRuntime)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _readConfiguration = readConfiguration
                ?? throw new ArgumentNullException(nameof(readConfiguration));
            _preflightRecordDirectory = preflightRecordDirectory
                ?? throw new ArgumentNullException(nameof(preflightRecordDirectory));
            _createRuntime = createRuntime
                ?? throw new ArgumentNullException(nameof(createRuntime));
        }

        internal static RelayHost CreateDefault()
        {
            return new RelayHost(
                Console.Out,
                Console.Error,
                RelayEnvironment.TryReadProcess,
                TryPreflightRecordDirectory,
                (configuration, log) => new RelayCoreRuntime(
                    new RelayServerCore(
                        configuration.MatchToken,
                        configuration.Seed,
                        configuration.InputDelayTicks,
                        configuration.RecordDirectory,
                        log,
                        lobbyTokenSecret: configuration.LobbyTokenSecret)));
        }

        internal int Run(string[] args, ManualResetEventSlim stopRequested)
        {
            if (stopRequested == null)
            {
                throw new ArgumentNullException(nameof(stopRequested));
            }

            _output.WriteLine("=== Project Nova - Relay Server ===");
            if (args == null || args.Length != 0)
            {
                _error.WriteLine(
                    "[Fatal] The relay does not accept command-line arguments; "
                    + "use its environment contract.");
                return ExitConfiguration;
            }

            RelayEnvironment configuration;
            string configurationError;
            try
            {
                if (!_readConfiguration(out configuration, out configurationError))
                {
                    _error.WriteLine("[Fatal] " + configurationError);
                    return ExitConfiguration;
                }
            }
            catch (Exception)
            {
                _error.WriteLine("[Fatal] Relay configuration could not be read.");
                return ExitConfiguration;
            }

            // A stop requested while configuration was read must not touch the
            // filesystem or create a listener.
            if (stopRequested.IsSet) return 0;

            string recordError;
            try
            {
                if (!_preflightRecordDirectory(
                        configuration.RecordDirectory, out recordError))
                {
                    _error.WriteLine("[Fatal] " + recordError);
                    return ExitConfiguration;
                }
            }
            catch (Exception)
            {
                _error.WriteLine("[Fatal] Relay record directory preflight failed.");
                return ExitConfiguration;
            }

            if (stopRequested.IsSet) return 0;

            int exitCode = 0;
            IRelayRuntime runtime = null;
            bool stopRequired = false;
            bool started = false;
            try
            {
                runtime = _createRuntime(
                    configuration,
                    message => _output.WriteLine("[Relay] " + message));
                if (runtime == null) throw new InvalidOperationException();

                // A partially completed Start may own a listener or record stream,
                // so it receives exactly one matching Stop attempt.
                stopRequired = true;
                runtime.Start(configuration.Port, configuration.BindAddress);
                started = true;

                if (!stopRequested.IsSet)
                {
                    _output.WriteLine(
                        $"[Relay] ready on {configuration.BindAddress}:{runtime.Port}; "
                        + $"slots={configuration.SlotCount}; "
                        + $"input-delay={configuration.InputDelayTicks} ticks; "
                        + $"recording={(configuration.RecordingEnabled ? "on" : "off")}");
                }
                while (!stopRequested.Wait(1))
                {
                    runtime.Poll();
                }
            }
            catch (Exception)
            {
                _error.WriteLine("[Fatal] Relay runtime failure.");
                exitCode = ExitRuntimeFailure;
            }
            finally
            {
                if (stopRequired)
                {
                    try
                    {
                        runtime.Stop();
                        if (started) _output.WriteLine("[Relay] stopped.");
                    }
                    catch (Exception)
                    {
                        _error.WriteLine("[Fatal] Relay shutdown failed.");
                        exitCode = ExitRuntimeFailure;
                    }
                }
            }
            return exitCode;
        }

        private static bool TryPreflightRecordDirectory(
            string recordDirectory, out string error)
        {
            error = string.Empty;
            if (recordDirectory.Length == 0) return true;

            string probePath = null;
            try
            {
                Directory.CreateDirectory(recordDirectory);
                probePath = Path.Combine(
                    recordDirectory,
                    ".nova-relay-write-probe-" + Guid.NewGuid().ToString("N"));
                using (var probe = new FileStream(
                    probePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    probe.WriteByte(0x4E);
                    probe.Flush(flushToDisk: true);
                }
                File.Delete(probePath);
                return true;
            }
            catch (Exception)
            {
                if (probePath != null)
                {
                    try { File.Delete(probePath); } catch { /* best-effort cleanup */ }
                }
                error = RelayEnvironment.RecordDirectoryVariable
                    + " could not be created, written, and cleaned up.";
                return false;
            }
        }

        private sealed class RelayCoreRuntime : IRelayRuntime
        {
            private readonly RelayServerCore _core;

            internal RelayCoreRuntime(RelayServerCore core)
            {
                _core = core;
            }

            public int Port => _core.Port;

            public void Start(int port, IPAddress bindAddress)
            {
                _core.Start(port, bindAddress);
            }

            public void Poll()
            {
                _core.Poll();
            }

            public void Stop()
            {
                _core.Stop();
            }
        }
    }
}
