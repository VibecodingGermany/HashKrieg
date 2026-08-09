using System;
using System.Net;
using UnityEngine;

namespace Nova.Gameplay
{
    /// <summary>
    /// Lobby endpoint configuration (sprint 14, D-092): the Supabase Edge
    /// Functions base URL plus the anon key that authorizes this client.
    /// Resolution order: the environment variables
    /// <see cref="UrlEnvironmentVariable"/> and
    /// <see cref="AnonKeyEnvironmentVariable"/> (both must be set), then the
    /// git-ignored Resources asset "lobby-config" (template:
    /// lobby-config.example.json). <see cref="TryLoad"/> reports false when
    /// nothing usable is configured — the menu then disables the lobby and
    /// keeps the direct connection available.
    /// <para>
    /// The anon key is a publishable Supabase key, not a user secret — but it
    /// still never appears in logs or exception messages.
    /// </para>
    /// </summary>
    public sealed class LobbyConfig
    {
        public const string UrlEnvironmentVariable = "NOVA_LOBBY_URL";
        public const string AnonKeyEnvironmentVariable = "NOVA_LOBBY_ANON_KEY";

        private LobbyConfig(string supabaseFunctionsUrl, string anonKey)
        {
            SupabaseFunctionsUrl = supabaseFunctionsUrl;
            AnonKey = anonKey;
        }

        /// <summary>Base URL of the Edge Functions, e.g. https://&lt;project&gt;.supabase.co/functions/v1.</summary>
        public string SupabaseFunctionsUrl { get; }

        /// <summary>Supabase anon key sent as "apikey" and bearer token. Never logged.</summary>
        public string AnonKey { get; }

        /// <summary>
        /// Resolves the configuration from the environment or the Resources
        /// asset. False means "not configured" (or configured invalid) — never
        /// throws.
        /// </summary>
        public static bool TryLoad(out LobbyConfig config)
        {
            config = null;

            string envUrl = Environment.GetEnvironmentVariable(UrlEnvironmentVariable);
            string envKey = Environment.GetEnvironmentVariable(AnonKeyEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(envUrl) || !string.IsNullOrWhiteSpace(envKey))
            {
                if (TryCreate(envUrl, envKey, out config))
                {
                    return true;
                }
                Debug.LogWarning(
                    "[LobbyConfig] Environment lobby configuration is incomplete or invalid (both " +
                    UrlEnvironmentVariable + " and " + AnonKeyEnvironmentVariable +
                    " are required; the URL must be https, http only on loopback). " +
                    "Falling back to the Resources asset.");
            }

            TextAsset asset;
            try
            {
                asset = Resources.Load<TextAsset>("lobby-config");
            }
            catch (Exception)
            {
                return false;
            }
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                return false;
            }

            ConfigJson json;
            try
            {
                json = JsonUtility.FromJson<ConfigJson>(asset.text);
            }
            catch (Exception)
            {
                return false;
            }
            if (json == null || !TryCreate(json.supabaseFunctionsUrl, json.anonKey, out config))
            {
                Debug.LogWarning(
                    "[LobbyConfig] Resources/lobby-config.json is missing a valid " +
                    "supabaseFunctionsUrl (https, http only on loopback) or anonKey — the lobby stays disabled.");
                return false;
            }
            return true;
        }

        private static bool TryCreate(string url, string anonKey, out LobbyConfig config)
        {
            config = null;
            if (string.IsNullOrWhiteSpace(anonKey) || string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
            {
                return false;
            }
            // Same rule as LobbyClient: https everywhere, plain http only on loopback.
            if (uri.Scheme != Uri.UriSchemeHttps
                && !(uri.Scheme == Uri.UriSchemeHttp && IsLoopbackHost(uri.Host)))
            {
                return false;
            }
            config = new LobbyConfig(uri.GetLeftPart(UriPartial.Path).TrimEnd('/'), anonKey.Trim());
            return true;
        }

        private static bool IsLoopbackHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            // Uri.Host keeps the brackets of an IPv6 literal.
            string bare = host.TrimStart('[').TrimEnd(']');
            return IPAddress.TryParse(bare, out IPAddress address) && IPAddress.IsLoopback(address);
        }

        [Serializable]
        private sealed class ConfigJson
        {
            public string supabaseFunctionsUrl;
            public string anonKey;
        }
    }
}
