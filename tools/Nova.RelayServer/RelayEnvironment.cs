using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using Nova.Networking;

namespace Nova.RelayServer
{
    /// <summary>Fail-closed environment contract of the headless relay host.</summary>
    internal sealed class RelayEnvironment
    {
        public const string MatchTokenVariable = "NOVA_MATCH_TOKEN";
        public const string BindVariable = "NOVA_RELAY_BIND";
        public const string PortVariable = "NOVA_RELAY_PORT";
        public const string SlotCountVariable = "NOVA_RELAY_SLOT_COUNT";
        public const string InputDelayVariable = "NOVA_INPUT_DELAY_TICKS";
        public const string RecordDirectoryVariable = "NOVA_RECORD_DIR";
        public const string SeedVariable = "NOVA_RELAY_SEED";
        public const string TokenSecretVariable = "NOVA_RELAY_TOKEN_SECRET";

        public const int DefaultPort = 47_777;
        public const int MinimumPort = 1_024;
        public const int MaximumPort = 65_535;
        public const int RequiredSlotCount = 2;
        public const uint DefaultInputDelayTicks = 3;
        /// <summary>Configuration form of the lobby HMAC secret: 32 bytes as unprefixed hex.</summary>
        public const int TokenSecretHexCharacters = 64;

        private static readonly string[] KnownVariables =
        {
            MatchTokenVariable,
            BindVariable,
            PortVariable,
            SlotCountVariable,
            InputDelayVariable,
            RecordDirectoryVariable,
            SeedVariable,
            TokenSecretVariable,
        };

        private RelayEnvironment(
            ulong matchToken, IPAddress bindAddress, int port, int slotCount,
            uint inputDelayTicks, string recordDirectory, ulong seed,
            byte[] lobbyTokenSecret)
        {
            MatchToken = matchToken;
            BindAddress = bindAddress;
            Port = port;
            SlotCount = slotCount;
            InputDelayTicks = inputDelayTicks;
            RecordDirectory = recordDirectory;
            Seed = seed;
            LobbyTokenSecret = lobbyTokenSecret;
        }

        public ulong MatchToken { get; }
        public IPAddress BindAddress { get; }
        public int Port { get; }
        public int SlotCount { get; }
        public uint InputDelayTicks { get; }
        public string RecordDirectory { get; }
        public ulong Seed { get; }
        /// <summary>Shared lobby HMAC secret (D-093); null when the variable is absent.</summary>
        public byte[] LobbyTokenSecret { get; }
        public bool RecordingEnabled => RecordDirectory.Length != 0;

        public static bool TryReadProcess(
            out RelayEnvironment relayEnvironment, out string error)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < KnownVariables.Length; i++)
            {
                string value = Environment.GetEnvironmentVariable(KnownVariables[i]);
                if (value != null) values.Add(KnownVariables[i], value);
            }
            return TryParse(values, out relayEnvironment, out error);
        }

        public static bool TryParse(
            IReadOnlyDictionary<string, string> values,
            out RelayEnvironment relayEnvironment, out string error)
        {
            relayEnvironment = null;
            error = string.Empty;
            if (values == null)
            {
                error = "Relay environment values are unavailable.";
                return false;
            }

            if (!TryGet(values, MatchTokenVariable, out string tokenText)
                || !TryParseExactNonZeroHex(tokenText, out ulong matchToken))
            {
                error = MatchTokenVariable
                    + " must be exactly 16 unprefixed hexadecimal characters and non-zero.";
                return false;
            }

            IPAddress bindAddress = IPAddress.Loopback;
            if (TryGet(values, BindVariable, out string bindText)
                && (bindText.Length == 0 || bindText != bindText.Trim()
                    || !IPAddress.TryParse(bindText, out bindAddress)))
            {
                error = BindVariable + " must be a numeric IP address.";
                return false;
            }

            if (!TryReadDecimal(
                    values, PortVariable, DefaultPort,
                    MinimumPort, MaximumPort, out int port))
            {
                error = PortVariable + " must be a decimal integer from 1024 through 65535.";
                return false;
            }

            if (!TryReadDecimal(
                    values, SlotCountVariable, RequiredSlotCount,
                    RequiredSlotCount, RequiredSlotCount, out int slotCount))
            {
                error = SlotCountVariable + " must be exactly 2.";
                return false;
            }

            if (!TryReadDecimal(
                    values, InputDelayVariable, (int)DefaultInputDelayTicks,
                    (int)RelayProtocol.MinInputDelayTicks,
                    (int)RelayProtocol.MaxInputDelayTicks, out int inputDelay))
            {
                error = InputDelayVariable + " must be a decimal integer from 1 through 60.";
                return false;
            }

            string recordDirectory = string.Empty;
            if (TryGet(values, RecordDirectoryVariable, out string recordText))
            {
                try
                {
                    if (recordText.Length == 0 || !Path.IsPathFullyQualified(recordText))
                    {
                        error = RecordDirectoryVariable
                            + " must be an absolute non-root directory when set.";
                        return false;
                    }
                    recordDirectory = Path.GetFullPath(recordText);
                    if (recordDirectory == Path.GetPathRoot(recordDirectory))
                    {
                        error = RecordDirectoryVariable
                            + " must be an absolute non-root directory when set.";
                        return false;
                    }
                }
                catch (Exception exception) when (
                    exception is ArgumentException
                    || exception is NotSupportedException
                    || exception is PathTooLongException)
                {
                    error = RecordDirectoryVariable
                        + " must be an absolute non-root directory when set.";
                    return false;
                }
            }

            ulong seed = 0;
            if (TryGet(values, SeedVariable, out string seedText)
                && !TryParseExactNonZeroHex(seedText, out seed))
            {
                error = SeedVariable
                    + " must be exactly 16 unprefixed hexadecimal characters and non-zero when set.";
                return false;
            }

            byte[] lobbyTokenSecret = null;
            if (TryGet(values, TokenSecretVariable, out string tokenSecretText)
                && !TryParseExactHexBytes(
                    tokenSecretText, TokenSecretHexCharacters / 2, out lobbyTokenSecret))
            {
                error = TokenSecretVariable
                    + " must be exactly 64 unprefixed hexadecimal characters when set.";
                return false;
            }

            relayEnvironment = new RelayEnvironment(
                matchToken, bindAddress, port, slotCount,
                (uint)inputDelay, recordDirectory, seed, lobbyTokenSecret);
            return true;
        }

        private static bool TryReadDecimal(
            IReadOnlyDictionary<string, string> values, string name,
            int fallback, int minimum, int maximum, out int value)
        {
            if (!TryGet(values, name, out string text))
            {
                value = fallback;
                return true;
            }
            return int.TryParse(
                    text, NumberStyles.None, CultureInfo.InvariantCulture, out value)
                && value >= minimum && value <= maximum;
        }

        private static bool TryParseExactNonZeroHex(string text, out ulong value)
        {
            value = 0;
            return text != null
                && text.Length == RelayProtocol.MatchTokenHexCharacters
                && ulong.TryParse(
                    text, NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out value)
                && value != 0;
        }

        /// <summary>
        /// Parses an exact-length unprefixed hex byte string without
        /// accepting prefixes, signs or whitespace. Error paths must never
        /// include the source text.
        /// </summary>
        private static bool TryParseExactHexBytes(string text, int byteCount, out byte[] value)
        {
            value = null;
            if (text == null || text.Length != byteCount * 2) return false;
            var bytes = new byte[byteCount];
            for (int i = 0; i < bytes.Length; i++)
            {
                int high = HexNibble(text[i * 2]);
                int low = HexNibble(text[i * 2 + 1]);
                if (high < 0 || low < 0) return false;
                bytes[i] = (byte)((high << 4) | low);
            }
            value = bytes;
            return true;
        }

        private static int HexNibble(char character)
        {
            if (character >= '0' && character <= '9') return character - '0';
            if (character >= 'a' && character <= 'f') return character - 'a' + 10;
            if (character >= 'A' && character <= 'F') return character - 'A' + 10;
            return -1;
        }

        private static bool TryGet(
            IReadOnlyDictionary<string, string> values, string name, out string value)
        {
            return values.TryGetValue(name, out value) && value != null;
        }
    }
}
