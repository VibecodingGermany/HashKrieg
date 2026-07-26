using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Nova.Core;
using Nova.Simulation.CommandsV1;
using Nova.Simulation.Replays;
using Nova.SimRunner;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Tests for the DETERMINISM_10000 scenario harness
    /// (tools/Nova.SimRunner): the short variant (100 ticks, checkpoints
    /// every 10) must be deterministic across runs (identical checkpoint
    /// hashes, replay bytes and final snapshot bytes — the local
    /// determinism baseline), the verify mode must pass against an
    /// identical artifact and fail with the first divergence against a
    /// tampered one, and the platform profile artifact must follow the
    /// documented strict schema.
    /// </summary>
    [TestFixture]
    public sealed class Determinism10000Tests
    {
        private static DeterminismOptions ShortOptions(ulong seed = 0)
        {
            var options = new DeterminismOptions { Ticks = 100, CheckpointIntervalTicks = 10 };
            if (seed != 0)
            {
                options.Seed = seed;
            }
            return options;
        }

        // ----------------------------------------------------------
        // Determinism baseline: two runs are bit-identical
        // ----------------------------------------------------------

        [Test]
        public void ShortRun_TwoExecutions_ProduceIdenticalHashesAndSnapshotBytes()
        {
            DeterminismRunResult run1 = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismRunResult run2 = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);

            Assert.That(run1.PlaybackVerified, Is.True, () => $"run 1 self-verification: {run1.PlaybackFailure}");
            Assert.That(run2.PlaybackVerified, Is.True, () => $"run 2 self-verification: {run2.PlaybackFailure}");

            Assert.That(run2.Checkpoints.Count, Is.EqualTo(10), "100 ticks / interval 10 = 10 checkpoints");
            Assert.That(run2.Checkpoints.Count, Is.EqualTo(run1.Checkpoints.Count));
            for (int i = 0; i < run1.Checkpoints.Count; i++)
            {
                Assert.That(run2.Checkpoints[i].Tick, Is.EqualTo(run1.Checkpoints[i].Tick));
                Assert.That(run2.Checkpoints[i].StateHash64, Is.EqualTo(run1.Checkpoints[i].StateHash64),
                    $"checkpoint hash mismatch at index {i}");
            }

            Assert.That(run2.FinalStateHash, Is.EqualTo(run1.FinalStateHash));
            Assert.That(run2.FinalSnapshotLength, Is.EqualTo(run1.FinalSnapshotLength));
            Assert.That(run2.FinalSnapshotSha256, Is.EqualTo(run1.FinalSnapshotSha256),
                "final snapshot bytes must be identical across runs");
            Assert.That(run2.ReplaySha256, Is.EqualTo(run1.ReplaySha256),
                "the generator must reproduce the identical command stream (replay bytes)");
            Assert.That(run2.FingerprintHash64, Is.EqualTo(run1.FingerprintHash64));
            Assert.That(run1.DeterminismDefineActive, Is.True,
                "the test lane compiles with NOVA_FIXED_POINT (csproj define)");
        }

        [Test]
        public void ShortRun_DifferentSeed_Diverges_SanityCheck()
        {
            DeterminismRunResult run1 = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismRunResult runOther = Determinism10000Scenario.Run(
                ShortOptions(seed: new DeterminismOptions().Seed + 1), NullNovaLogger.Instance);
            Assert.That(runOther.FinalStateHash, Is.Not.EqualTo(run1.FinalStateHash),
                "a different seed should diverge (the equality results above are not vacuous)");
        }

        // ----------------------------------------------------------
        // Command stream: both slots, all opening domains
        // ----------------------------------------------------------

        [Test]
        public void ShortRun_CommandStream_CarriesBothSlotsAndOpeningDomains()
        {
            byte[] replayBytes = Determinism10000Scenario.GenerateReplay(
                ShortOptions(), NullNovaLogger.Instance, out _, out _);
            Assert.That(ReplayFile.TryParse(replayBytes, out ReplayFile replay, out ReplayReadError readError),
                Is.True, () => $"parse failed: {readError}");

            Assert.That(replay.Frames.Length, Is.EqualTo(100), "every tick is recorded, empty ticks included");
            var slotsSeen = new HashSet<byte>();
            var kindsSeen = new HashSet<CommandKind>();
            int applied = 0;
            foreach (ReplayTickFrame frame in replay.Frames)
            {
                for (int r = 0; r < frame.RecordCount; r++)
                {
                    slotsSeen.Add(frame.Records[r].PlayerSlot);
                    kindsSeen.Add(frame.Records[r].Kind);
                    if (frame.ResultCodes[r] == CommandResultCode.Applied)
                    {
                        applied++;
                    }
                }
            }
            Assert.That(slotsSeen, Is.EquivalentTo(new byte[] { 0, 1 }), "both active slots command");
            Assert.That(kindsSeen, Is.SupersetOf(new[]
                { CommandKind.Harvest, CommandKind.Move, CommandKind.AttackTarget, CommandKind.PlaceBuilding }),
                "the opening window already drives economy, movement, combat and construction");
            Assert.That(applied, Is.GreaterThan(0), "the stream carries applied commands, not only rejections");
        }

        // ----------------------------------------------------------
        // Verify mode: identical artifact passes, tampered fails
        // ----------------------------------------------------------

        [Test]
        public void Verify_AgainstIdenticalArtifact_PassesBothComparisonAssertions()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison: null);

            string profilePath = Path.Combine(dir, DeterminismArtifacts.ProfileFileName(own.PlatformId));
            Assert.That(DeterminismArtifacts.TryLoadProfile(profilePath, out PlatformProfile other, out string error),
                Is.True, () => error);

            DeterminismComparison comparison = Determinism10000Scenario.Compare(own, other);
            Assert.That(comparison.CheckpointsExact, Is.True, () => comparison.FirstDivergence);
            Assert.That(comparison.SnapshotExact, Is.True, () => comparison.FirstDivergence);
            Assert.That(comparison.FirstDivergence, Is.Empty);
        }

        [Test]
        public void Verify_FlippedCheckpointHash_FailsWithFirstDivergenceAtThatTick()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison: null);
            string profilePath = Path.Combine(dir, DeterminismArtifacts.ProfileFileName(own.PlatformId));

            // Flip the hash of checkpoint index 3 (tick 40) inside the artifact.
            string originalHash = DeterminismArtifacts.Hex64(own.Checkpoints[3].StateHash64);
            string flippedHash = originalHash == "0xFFFFFFFFFFFFFFFF" ? "0xFFFFFFFFFFFFFFFE" : "0xFFFFFFFFFFFFFFFF";
            Assert.That(own.Checkpoints[3].Tick, Is.EqualTo(40), "checkpoint index 3 is tick 40");
            string text = File.ReadAllText(profilePath);
            string needle = $"\"stateHash64\": \"{originalHash}\"";
            Assert.That(text, Does.Contain(needle), "the artifact carries the expected checkpoint entry");
            File.WriteAllText(profilePath, text.Replace(needle, $"\"stateHash64\": \"{flippedHash}\""));

            Assert.That(DeterminismArtifacts.TryLoadProfile(profilePath, out PlatformProfile tampered, out _),
                Is.True, "a tampered hash still parses (the schema does not protect content)");
            DeterminismComparison comparison = Determinism10000Scenario.Compare(own, tampered);
            Assert.That(comparison.CheckpointsExact, Is.False);
            Assert.That(comparison.FirstDivergence, Does.Contain("tick 40"), "the first divergence names the tick");
            Assert.That(comparison.FirstDivergence, Does.Contain(flippedHash));
            Assert.That(comparison.SnapshotExact, Is.True,
                "an untouched final snapshot stays exact — assertions are independent");
        }

        [Test]
        public void Verify_FlippedSnapshotSha_FailsOnlyTheSnapshotAssertion()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison: null);
            string profilePath = Path.Combine(dir, DeterminismArtifacts.ProfileFileName(own.PlatformId));

            string text = File.ReadAllText(profilePath);
            string needle = $"\"sha256\": \"{own.FinalSnapshotSha256}\"";
            Assert.That(text, Does.Contain(needle));
            string flipped = own.FinalSnapshotSha256.StartsWith("f", StringComparison.Ordinal)
                ? "e" + own.FinalSnapshotSha256.Substring(1)
                : "f" + own.FinalSnapshotSha256.Substring(1);
            File.WriteAllText(profilePath, text.Replace(needle, $"\"sha256\": \"{flipped}\""));

            Assert.That(DeterminismArtifacts.TryLoadProfile(profilePath, out PlatformProfile tampered, out _), Is.True);
            DeterminismComparison comparison = Determinism10000Scenario.Compare(own, tampered);
            Assert.That(comparison.CheckpointsExact, Is.True, "checkpoints are untouched");
            Assert.That(comparison.SnapshotExact, Is.False);
            Assert.That(comparison.FirstDivergence, Does.Contain("final snapshot differs"));
        }

        // ----------------------------------------------------------
        // Artifact schema (strict documented profile + D-062 assertions)
        // ----------------------------------------------------------

        [Test]
        public void ProfileArtifact_HasExactDocumentedSchema()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison: null);

            string profilePath = Path.Combine(dir, DeterminismArtifacts.ProfileFileName(own.PlatformId));
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(profilePath));
            JsonElement root = doc.RootElement;
            Assert.That(PropertyNames(root), Is.EquivalentTo(new[]
            {
                "name", "scenarioId", "platform", "ticks", "checkpointIntervalTicks", "seed",
                "determinismDefines", "managedPathOnly", "fingerprintHash64", "replaySha256",
                "checkpoints", "finalSnapshot",
            }));
            Assert.That(root.GetProperty("name").GetString(),
                Is.EqualTo("scenario.DETERMINISM_10000.platformProfile"));
            Assert.That(root.GetProperty("scenarioId").GetString(), Is.EqualTo("DETERMINISM_10000"));
            Assert.That(root.GetProperty("ticks").GetInt32(), Is.EqualTo(100));
            Assert.That(root.GetProperty("checkpointIntervalTicks").GetInt32(), Is.EqualTo(10));
            Assert.That(root.GetProperty("seed").GetString(), Does.StartWith("0x").And.Length.EqualTo(18),
                "u64 values are 16-digit hex strings (JSON numbers cannot hold u64 exactly)");
            Assert.That(root.GetProperty("managedPathOnly").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("determinismDefines").GetArrayLength(), Is.EqualTo(1));
            Assert.That(root.GetProperty("determinismDefines")[0].GetString(), Is.EqualTo("NOVA_FIXED_POINT"));

            JsonElement platform = root.GetProperty("platform");
            Assert.That(PropertyNames(platform), Is.EquivalentTo(new[]
            {
                "id", "os", "architecture", "runtimeVersion", "dotnetSdk", "build", "executionPath", "burstEnabled",
            }));
            Assert.That(platform.GetProperty("executionPath").GetString(), Is.EqualTo("managed"));
            Assert.That(platform.GetProperty("burstEnabled").GetBoolean(), Is.False);
            Assert.That(platform.GetProperty("dotnetSdk").GetString(), Is.EqualTo("8.0.318"),
                "the artifact records the repo-pinned SDK of global.json");

            JsonElement checkpoints = root.GetProperty("checkpoints");
            Assert.That(checkpoints.GetArrayLength(), Is.EqualTo(10));
            uint expectedTick = 10;
            foreach (JsonElement checkpoint in checkpoints.EnumerateArray())
            {
                Assert.That(PropertyNames(checkpoint), Is.EquivalentTo(new[] { "tick", "stateHash64" }));
                Assert.That(checkpoint.GetProperty("tick").GetUInt32(), Is.EqualTo(expectedTick));
                Assert.That(checkpoint.GetProperty("stateHash64").GetString(),
                    Does.StartWith("0x").And.Length.EqualTo(18));
                expectedTick += 10;
            }

            JsonElement snapshot = root.GetProperty("finalSnapshot");
            Assert.That(PropertyNames(snapshot), Is.EquivalentTo(new[] { "bytes", "sha256" }));
            Assert.That(snapshot.GetProperty("bytes").GetInt32(), Is.EqualTo(own.FinalSnapshotLength));
            Assert.That(snapshot.GetProperty("sha256").GetString(), Is.EqualTo(own.FinalSnapshotSha256));

            // The always-on D-062 assertion artifacts: exact {name, unit, samples}.
            const string prefix = "scenario.DETERMINISM_10000";
            foreach (string assertion in new[] { "managed-path-only", "same-sources-and-determinism-defines" })
            {
                string path = Path.Combine(dir, $"{prefix}.assertion.{assertion}.json");
                Assert.That(File.Exists(path), Is.True, $"missing assertion artifact {assertion}");
                using JsonDocument assertionDoc = JsonDocument.Parse(File.ReadAllText(path));
                JsonElement assertionRoot = assertionDoc.RootElement;
                Assert.That(PropertyNames(assertionRoot), Is.EquivalentTo(new[] { "name", "unit", "samples" }));
                Assert.That(assertionRoot.GetProperty("name").GetString(), Is.EqualTo($"{prefix}.assertion.{assertion}"));
                Assert.That(assertionRoot.GetProperty("unit").GetString(), Is.EqualTo("bool"));
                Assert.That(assertionRoot.GetProperty("samples").GetArrayLength(), Is.EqualTo(1));
                Assert.That(assertionRoot.GetProperty("samples")[0].GetInt32(), Is.EqualTo(1));
            }
        }

        [Test]
        public void VerifyMode_WritesBothComparisonAssertionArtifacts()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            var comparison = new DeterminismComparison { CheckpointsExact = true, SnapshotExact = false };
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison);

            const string prefix = "scenario.DETERMINISM_10000";
            using JsonDocument passDoc = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(dir, $"{prefix}.assertion.exact-state-hash-every-checkpoint.json")));
            Assert.That(passDoc.RootElement.GetProperty("samples")[0].GetInt32(), Is.EqualTo(1));
            using JsonDocument failDoc = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(dir, $"{prefix}.assertion.exact-final-snapshot-bytes.json")));
            Assert.That(failDoc.RootElement.GetProperty("samples")[0].GetInt32(), Is.EqualTo(0));
        }

        [Test]
        public void ProfileArtifact_UnknownKey_IsRejectedStrictly()
        {
            string dir = CreateTempDir();
            DeterminismRunResult own = Determinism10000Scenario.Run(ShortOptions(), NullNovaLogger.Instance);
            DeterminismArtifacts.WriteRunArtifacts(dir, own, comparison: null);
            string profilePath = Path.Combine(dir, DeterminismArtifacts.ProfileFileName(own.PlatformId));

            string text = File.ReadAllText(profilePath);
            File.WriteAllText(profilePath, text.Replace(
                "\"scenarioId\": \"DETERMINISM_10000\",",
                "\"scenarioId\": \"DETERMINISM_10000\",\n    \"injected\": true,"));

            Assert.That(DeterminismArtifacts.TryLoadProfile(profilePath, out _, out string error), Is.False);
            Assert.That(error, Does.Contain("injected"));
        }

        // ----------------------------------------------------------

        private static List<string> PropertyNames(JsonElement element)
        {
            var names = new List<string>();
            foreach (JsonProperty property in element.EnumerateObject())
            {
                names.Add(property.Name);
            }
            return names;
        }

        private static string CreateTempDir()
        {
            string dir = Path.Combine(Path.GetTempPath(), "nova-determinism-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
