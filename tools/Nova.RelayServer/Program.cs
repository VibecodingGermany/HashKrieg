using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Nova.RelayServer
{
    /// <summary>POSIX process shell for the testable relay host lifecycle.</summary>
    internal static class Program
    {
        private const int ExitRuntimeFailure = 1;

        internal delegate IDisposable SignalRegistrar(
            PosixSignal signal, Action requestStop);

#if !NOVA_RELAY_TESTS
        private static int Main(string[] args)
        {
            return Run(args, RelayHost.CreateDefault(), RegisterSignal);
        }
#endif

        internal static int Run(
            string[] args, RelayHost host, SignalRegistrar registerSignal)
        {
            int exitCode = 0;
            IDisposable interruptRegistration = null;
            IDisposable terminateRegistration = null;
            using (var stopRequested = new ManualResetEventSlim(false))
            {
                try
                {
                    // Register both handlers before configuration, filesystem preflight,
                    // or listener startup can block or fail.
                    interruptRegistration = registerSignal(
                        PosixSignal.SIGINT, stopRequested.Set);
                    terminateRegistration = registerSignal(
                        PosixSignal.SIGTERM, stopRequested.Set);
                    exitCode = host.Run(args, stopRequested);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine("[Fatal] Relay process setup failed.");
                    exitCode = ExitRuntimeFailure;
                }
                finally
                {
                    bool disposalFailed = false;
                    try
                    {
                        terminateRegistration?.Dispose();
                    }
                    catch (Exception)
                    {
                        disposalFailed = true;
                    }
                    try
                    {
                        interruptRegistration?.Dispose();
                    }
                    catch (Exception)
                    {
                        disposalFailed = true;
                    }
                    if (disposalFailed)
                    {
                        Console.Error.WriteLine(
                            "[Fatal] Signal handler shutdown failed.");
                        exitCode = ExitRuntimeFailure;
                    }
                }
            }
            return exitCode;
        }

        private static IDisposable RegisterSignal(
            PosixSignal signal, Action requestStop)
        {
            return PosixSignalRegistration.Create(
                signal,
                context =>
                {
                    context.Cancel = true;
                    requestStop();
                });
        }
    }
}
