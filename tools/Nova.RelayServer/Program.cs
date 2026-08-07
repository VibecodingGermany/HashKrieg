using System;
using System.Threading;
using Nova.Networking;

namespace Nova.RelayServer
{
    /// <summary>
    /// Host process of the Project Nova input relay (docs/tech/RelayServer.md).
    /// Thin shell around <see cref="RelayServerCore"/>: configuration comes
    /// EXCLUSIVELY from environment variables — no secrets in the repository,
    /// no token in a unit file's plaintext Exec line:
    /// <list type="bullet">
    /// <item><c>NOVA_MATCH_TOKEN</c> (required): the shared match code. Clients
    /// must present it at Hello; the process refuses to start without it.</item>
    /// <item><c>NOVA_RELAY_PORT</c> (default 47777): the only open port.</item>
    /// <item><c>NOVA_INPUT_DELAY_TICKS</c> (default 3): the network lockstep
    /// input delay; 300 ms at 10 Hz (sprint A2a).</item>
    /// <item><c>NOVA_RECORD_DIR</c> (default empty = recording off): directory
    /// for the per-match *.novarec command-stream dumps.</item>
    /// <item><c>NOVA_RELAY_SEED</c> (default 0 = generated per match): fixed
    /// match seed for reproducible test runs.</item>
    /// </list>
    /// The process logs to stdout (journald under systemd) and pumps the
    /// single-threaded server core; SIGTERM/SIGINT stop it ordered.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            Console.WriteLine("=== Project Nova - Relay Server ===");

            string tokenText = Environment.GetEnvironmentVariable("NOVA_MATCH_TOKEN");
            if (string.IsNullOrEmpty(tokenText) || !ulong.TryParse(tokenText, System.Globalization.NumberStyles.HexNumber, null, out ulong token))
            {
                Console.Error.WriteLine("[Fatal] NOVA_MATCH_TOKEN is required (hex u64) — an open relay port without a shared secret lets anyone join the round.");
                return 2;
            }

            int port = ParseInt("NOVA_RELAY_PORT", 47777);
            uint delay = unchecked((uint)ParseInt("NOVA_INPUT_DELAY_TICKS", 3));
            string recordDir = Environment.GetEnvironmentVariable("NOVA_RECORD_DIR") ?? string.Empty;
            ulong seed = ParseUlongHex("NOVA_RELAY_SEED", 0UL);

            var core = new RelayServerCore(token, seed, delay, recordDir, message => Console.WriteLine($"[Relay] {message}"));
            core.Start(port);

            var stopped = new ManualResetEventSlim(false);
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                stopped.Set();
            };

            Console.WriteLine($"[Relay] ready on port {core.Port} (Ctrl+C to stop)");
            while (!stopped.IsSet)
            {
                core.Poll();
                Thread.Sleep(1);
            }

            core.Stop();
            Console.WriteLine("[Relay] stopped.");
            return 0;
        }

        private static int ParseInt(string name, int fallback)
        {
            string text = Environment.GetEnvironmentVariable(name);
            return int.TryParse(text, out int value) ? value : fallback;
        }

        private static ulong ParseUlongHex(string name, ulong fallback)
        {
            string text = Environment.GetEnvironmentVariable(name);
            return ulong.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out ulong value) ? value : fallback;
        }
    }
}
