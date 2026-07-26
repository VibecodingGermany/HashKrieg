using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Nova.SimRunner
{
    /// <summary>
    /// The deserialized platform profile artifact of a DETERMINISM_10000 run
    /// (strict schema, see <see cref="DeterminismArtifacts.WriteProfile"/>).
    /// </summary>
    internal sealed class PlatformProfile
    {
        public string ScenarioId;
        public string PlatformId;
        public int Ticks;
        public int CheckpointIntervalTicks;
        public ulong Seed;
        public ulong FingerprintHash64;
        public string ReplaySha256;
        public bool ManagedPathOnly;
        public string[] DeterminismDefines = Array.Empty<string>();
        public readonly List<CheckpointEntry> Checkpoints = new List<CheckpointEntry>();
        public int FinalSnapshotBytes;
        public string FinalSnapshotSha256;
    }

    /// <summary>
    /// Writes and reads the DETERMINISM_10000 platform profile artifact
    /// (<c>scenario.DETERMINISM_10000.&lt;platform&gt;.json</c>) and the
    /// D-062 boolean assertion artifacts.
    /// <para>
    /// Artifact format (documented harness choice — D-062 fixes the
    /// <c>scenario.&lt;ID&gt;.&lt;metric&gt;</c> naming and the strict
    /// <c>{name, unit, samples}</c> shape of metric/assertion artifacts, but
    /// no metric specification exists for a cross-platform state-hash
    /// series): one strict JSON profile per platform with exactly these keys:
    /// <code>
    /// name:            "scenario.DETERMINISM_10000.platformProfile"
    /// scenarioId:      "DETERMINISM_10000"
    /// platform:        { id, os, architecture, runtimeVersion, dotnetSdk,
    ///                  build, executionPath, burstEnabled } — the vocabulary
    ///                  of the gate evidence "environments" structure
    ///                  (os/architecture/build/executionPath/burstEnabled)
    ///                  plus runtimeVersion/dotnetSdk for the .NET lane;
    ///                  dotnetSdk is the repo-pinned SDK of global.json
    /// ticks:           10000
    /// checkpointIntervalTicks: 100
    /// seed:            "0x..." (hex string — u64 exceeds JSON's exact range)
    /// determinismDefines: ["NOVA_FIXED_POINT"] (actually compiled defines)
    /// managedPathOnly: true
    /// fingerprintHash64: "0x..." (MatchFingerprint.ComputeHash)
    /// replaySha256:    SHA-256 of the generated replay container bytes
    /// checkpoints:     [{ tick, stateHash64: "0x..." }] ascending
    /// finalSnapshot:   { bytes, sha256 }
    /// </code>
    /// 64-bit values are hex strings because JSON numbers cannot represent
    /// u64 exactly. The reader is strict: every required key must exist with
    /// the right type, unknown keys are rejected. All artifacts are
    /// diagnosis material (output/, gitignored), never gate evidence.
    /// </para>
    /// </summary>
    internal static class DeterminismArtifacts
    {
        public const string ProfileMetricName = "scenario." + DeterminismOptions.ScenarioId + ".platformProfile";

        private static readonly string[] ProfileKeys =
        {
            "name", "scenarioId", "platform", "ticks", "checkpointIntervalTicks", "seed",
            "determinismDefines", "managedPathOnly", "fingerprintHash64", "replaySha256",
            "checkpoints", "finalSnapshot",
        };

        private static readonly string[] PlatformKeys =
        {
            "id", "os", "architecture", "runtimeVersion", "dotnetSdk", "build", "executionPath", "burstEnabled",
        };

        private static readonly string[] CheckpointKeys = { "tick", "stateHash64" };
        private static readonly string[] SnapshotKeys = { "bytes", "sha256" };

        /// <summary>Auto platform tag, e.g. "macos-arm64" or "windows-x64".</summary>
        public static string DetectPlatformId()
        {
            string os;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) os = "macos";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) os = "windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) os = "linux";
            else os = "unknown";
            return os + "-" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Writes the platform profile artifact of a run.
        /// </summary>
        public static void WriteProfile(string directory, string fileName, DeterminismRunResult result)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("name", ProfileMetricName);
                writer.WriteString("scenarioId", DeterminismOptions.ScenarioId);

                writer.WritePropertyName("platform");
                writer.WriteStartObject();
                writer.WriteString("id", result.PlatformId);
                writer.WriteString("os", RuntimeInformation.OSDescription);
                writer.WriteString("architecture", RuntimeInformation.ProcessArchitecture.ToString());
                writer.WriteString("runtimeVersion", RuntimeInformation.FrameworkDescription);
                writer.WriteString("dotnetSdk", DetectPinnedSdk());
#if DEBUG
                writer.WriteString("build", "Debug");
#else
                writer.WriteString("build", "Release");
#endif
                writer.WriteString("executionPath", "managed");
                writer.WriteBoolean("burstEnabled", false);
                writer.WriteEndObject();

                writer.WriteNumber("ticks", result.Ticks);
                writer.WriteNumber("checkpointIntervalTicks", result.CheckpointIntervalTicks);
                writer.WriteString("seed", Hex64(result.Seed));

                writer.WritePropertyName("determinismDefines");
                writer.WriteStartArray();
#if NOVA_FIXED_POINT
                writer.WriteStringValue("NOVA_FIXED_POINT");
#endif
                writer.WriteEndArray();
                writer.WriteBoolean("managedPathOnly", true);

                writer.WriteString("fingerprintHash64", Hex64(result.FingerprintHash64));
                writer.WriteString("replaySha256", result.ReplaySha256);

                writer.WritePropertyName("checkpoints");
                writer.WriteStartArray();
                foreach (CheckpointEntry checkpoint in result.Checkpoints)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("tick", checkpoint.Tick);
                    writer.WriteString("stateHash64", Hex64(checkpoint.StateHash64));
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();

                writer.WritePropertyName("finalSnapshot");
                writer.WriteStartObject();
                writer.WriteNumber("bytes", result.FinalSnapshotLength);
                writer.WriteString("sha256", result.FinalSnapshotSha256);
                writer.WriteEndObject();

                writer.WriteEndObject();
                writer.Flush();
            }
        }

        /// <summary>
        /// Strictly parses a platform profile artifact. Every required key
        /// must exist with the expected type; unknown keys, malformed hex
        /// strings or trailing content make the load fail with a
        /// human-readable <paramref name="error"/>.
        /// </summary>
        public static bool TryLoadProfile(string path, out PlatformProfile profile, out string error)
        {
            profile = null;
            error = null;
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(File.ReadAllText(path));
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException || exception is UnauthorizedAccessException)
            {
                error = $"profile artifact is not readable strict JSON: {exception.Message}";
                return false;
            }

            using (doc)
            {
                string keyError = null;
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !HasExactly(root, ProfileKeys, out keyError))
                {
                    error = $"profile root: {keyError ?? "not a JSON object"}";
                    return false;
                }
                if (!TryGetString(root, "name", out string name) || name != ProfileMetricName)
                {
                    error = $"profile name must be \"{ProfileMetricName}\"";
                    return false;
                }
                if (!TryGetString(root, "scenarioId", out string scenarioId) || scenarioId != DeterminismOptions.ScenarioId)
                {
                    error = $"profile scenarioId must be \"{DeterminismOptions.ScenarioId}\"";
                    return false;
                }

                JsonElement platform = root.GetProperty("platform");
                if (platform.ValueKind != JsonValueKind.Object || !HasExactly(platform, PlatformKeys, out keyError))
                {
                    error = $"profile platform: {keyError ?? "not a JSON object"}";
                    return false;
                }
                if (!TryGetString(platform, "id", out string platformId))
                {
                    error = "profile platform.id missing or not a string";
                    return false;
                }

                var parsed = new PlatformProfile { ScenarioId = scenarioId, PlatformId = platformId };
                if (!TryGetInt(root, "ticks", out parsed.Ticks)
                    || !TryGetInt(root, "checkpointIntervalTicks", out parsed.CheckpointIntervalTicks)
                    || !TryGetU64Hex(root, "seed", out parsed.Seed)
                    || !TryGetU64Hex(root, "fingerprintHash64", out parsed.FingerprintHash64)
                    || !TryGetString(root, "replaySha256", out parsed.ReplaySha256)
                    || !TryGetBool(root, "managedPathOnly", out parsed.ManagedPathOnly))
                {
                    error = "profile scalar fields missing or of the wrong type";
                    return false;
                }

                var defines = new List<string>();
                foreach (JsonElement define in root.GetProperty("determinismDefines").EnumerateArray())
                {
                    if (define.ValueKind != JsonValueKind.String)
                    {
                        error = "profile determinismDefines entries must be strings";
                        return false;
                    }
                    defines.Add(define.GetString());
                }
                parsed.DeterminismDefines = defines.ToArray();

                JsonElement checkpoints = root.GetProperty("checkpoints");
                if (checkpoints.ValueKind != JsonValueKind.Array)
                {
                    error = "profile checkpoints must be an array";
                    return false;
                }
                foreach (JsonElement element in checkpoints.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object || !HasExactly(element, CheckpointKeys, out keyError))
                    {
                        error = $"profile checkpoint: {keyError ?? "not a JSON object"}";
                        return false;
                    }
                    if (!TryGetInt(element, "tick", out int tickValue) || tickValue < 0
                        || !TryGetU64Hex(element, "stateHash64", out ulong hash))
                    {
                        error = "profile checkpoint fields missing or of the wrong type";
                        return false;
                    }
                    parsed.Checkpoints.Add(new CheckpointEntry { Tick = (uint)tickValue, StateHash64 = hash });
                }

                JsonElement snapshot = root.GetProperty("finalSnapshot");
                if (snapshot.ValueKind != JsonValueKind.Object || !HasExactly(snapshot, SnapshotKeys, out keyError))
                {
                    error = $"profile finalSnapshot: {keyError ?? "not a JSON object"}";
                    return false;
                }
                if (!TryGetInt(snapshot, "bytes", out parsed.FinalSnapshotBytes)
                    || !TryGetString(snapshot, "sha256", out parsed.FinalSnapshotSha256))
                {
                    error = "profile finalSnapshot fields missing or of the wrong type";
                    return false;
                }

                profile = parsed;
                return true;
            }
        }

        /// <summary>Profile artifact file name of a platform.</summary>
        public static string ProfileFileName(string platformId)
        {
            return $"scenario.{DeterminismOptions.ScenarioId}.{platformId}.json";
        }

        /// <summary>
        /// Writes the run's artifact set into <paramref name="outDir"/>: the
        /// platform profile plus the always-on boolean assertions
        /// (managed-path-only, same-sources-and-determinism-defines). In
        /// verify mode (<paramref name="comparison"/> non-null) the two
        /// comparison assertions are added. Assertion names and file names
        /// follow the D-062 scheme
        /// (<c>scenario.&lt;ID&gt;.assertion.&lt;name&gt;.json</c>, strict
        /// {name, unit: bool, samples: [1|0]}).
        /// </summary>
        public static void WriteRunArtifacts(
            string outDir, DeterminismRunResult result, DeterminismComparison comparison)
        {
            string prefix = $"scenario.{DeterminismOptions.ScenarioId}";
            WriteProfile(outDir, ProfileFileName(result.PlatformId), result);

            // managed-path-only: trivially true in this lane — the runner is
            // 100% managed C#; Burst is a Unity compiler path that does not
            // exist here (documented self-report).
            ScenarioArtifacts.WriteAssertion(outDir, $"{prefix}.assertion.managed-path-only.json",
                $"{prefix}.assertion.managed-path-only", passed: true);

            // same-sources-and-determinism-defines: build self-report —
            // NOVA_FIXED_POINT compiled in, sources linked from
            // Assets/_Project by Nova.SimRunner.csproj (SimulationCore.md
            // section 9).
            ScenarioArtifacts.WriteAssertion(outDir, $"{prefix}.assertion.same-sources-and-determinism-defines.json",
                $"{prefix}.assertion.same-sources-and-determinism-defines", result.DeterminismDefineActive);

            if (comparison != null)
            {
                ScenarioArtifacts.WriteAssertion(outDir, $"{prefix}.assertion.exact-state-hash-every-checkpoint.json",
                    $"{prefix}.assertion.exact-state-hash-every-checkpoint", comparison.CheckpointsExact);
                ScenarioArtifacts.WriteAssertion(outDir, $"{prefix}.assertion.exact-final-snapshot-bytes.json",
                    $"{prefix}.assertion.exact-final-snapshot-bytes", comparison.SnapshotExact);
            }
        }

        /// <summary>The repo-pinned SDK of global.json (walked up from the binary), else "unknown".</summary>
        private static string DetectPinnedSdk()
        {
            try
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null)
                {
                    string candidate = Path.Combine(directory.FullName, "global.json");
                    if (File.Exists(candidate))
                    {
                        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(candidate));
                        if (doc.RootElement.TryGetProperty("sdk", out JsonElement sdk)
                            && sdk.TryGetProperty("version", out JsonElement version)
                            && version.ValueKind == JsonValueKind.String)
                        {
                            return version.GetString();
                        }
                    }
                    directory = directory.Parent;
                }
            }
            catch (Exception exception) when (exception is IOException || exception is JsonException || exception is UnauthorizedAccessException)
            {
                // fall through to "unknown"
            }
            return "unknown";
        }

        internal static string Hex64(ulong value) => "0x" + value.ToString("X16", CultureInfo.InvariantCulture);

        private static bool HasExactly(JsonElement element, string[] expectedKeys, out string error)
        {
            var seen = new HashSet<string>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                seen.Add(property.Name);
            }
            foreach (string key in expectedKeys)
            {
                if (!seen.Contains(key))
                {
                    error = $"missing key \"{key}\"";
                    return false;
                }
            }
            foreach (string key in seen)
            {
                if (Array.IndexOf(expectedKeys, key) < 0)
                {
                    error = $"unknown key \"{key}\"";
                    return false;
                }
            }
            error = null;
            return true;
        }

        private static bool TryGetString(JsonElement element, string key, out string value)
        {
            value = null;
            return element.TryGetProperty(key, out JsonElement property)
                && property.ValueKind == JsonValueKind.String
                && (value = property.GetString()) != null;
        }

        private static bool TryGetInt(JsonElement element, string key, out int value)
        {
            value = 0;
            return element.TryGetProperty(key, out JsonElement property)
                && property.ValueKind == JsonValueKind.Number
                && property.TryGetInt32(out value);
        }

        private static bool TryGetBool(JsonElement element, string key, out bool value)
        {
            value = false;
            if (!element.TryGetProperty(key, out JsonElement property)
                || (property.ValueKind != JsonValueKind.True && property.ValueKind != JsonValueKind.False))
            {
                return false;
            }
            value = property.GetBoolean();
            return true;
        }

        private static bool TryGetU64Hex(JsonElement element, string key, out ulong value)
        {
            value = 0;
            if (!TryGetString(element, key, out string text)
                || !text.StartsWith("0x", StringComparison.Ordinal)
                || text.Length != 18)
            {
                return false;
            }
            return ulong.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
        }
    }
}
