using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Nova.Core;
using Nova.SimRunner;
using NUnit.Framework;

namespace Nova.SimRunner.Tests
{
    /// <summary>
    /// Tests for the SCALE_500_PRECOMBAT performance harness
    /// (tools/Nova.SimRunner): nearest-rank statistics with validator
    /// semantics, strict D-062/D-063 artifact schemas, measurability of a
    /// short scenario run and workload determinism under active timing.
    /// </summary>
    [TestFixture]
    public sealed class PerfHarnessTests
    {
        // ----------------------------------------------------------
        // Nearest rank (validator semantics: quality/scripts/
        // validate_gate_evidence.py::_nearest_rank — ascending sort,
        // index = max(0, ceil(q * n) - 1), no interpolation)
        // ----------------------------------------------------------

        [Test]
        public void NearestRank_EvenCount_HandValues()
        {
            var samples = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            // n = 10: ceil(0.95 * 10) = 10 -> index 9
            Assert.That(PerfStatistics.NearestRank(samples, 0.95), Is.EqualTo(10.0));
            // ceil(0.99 * 10) = 10 -> index 9
            Assert.That(PerfStatistics.NearestRank(samples, 0.99), Is.EqualTo(10.0));
            // ceil(0.50 * 10) = 5 -> index 4 (nearest rank, NOT the 5.5 average)
            Assert.That(PerfStatistics.NearestRank(samples, 0.50), Is.EqualTo(5.0));
            // ceil(0.05 * 10) = 1 -> index 0
            Assert.That(PerfStatistics.NearestRank(samples, 0.05), Is.EqualTo(1.0));
        }

        [Test]
        public void NearestRank_OddCount_HandValues()
        {
            var samples = new double[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };
            // n = 9: ceil(0.95 * 9) = ceil(8.55) = 9 -> index 8
            Assert.That(PerfStatistics.NearestRank(samples, 0.95), Is.EqualTo(90.0));
            // ceil(0.50 * 9) = 5 -> index 4
            Assert.That(PerfStatistics.NearestRank(samples, 0.50), Is.EqualTo(50.0));
        }

        [Test]
        public void NearestRank_LargerEvenCount_MatchesCeilFormula()
        {
            var samples = new double[20];
            for (int i = 0; i < 20; i++) samples[i] = i + 1;
            // n = 20: ceil(0.95 * 20) = 19 -> index 18 -> value 19
            Assert.That(PerfStatistics.NearestRank(samples, 0.95), Is.EqualTo(19.0));
            // ceil(0.99 * 20) = 20 -> index 19 -> value 20
            Assert.That(PerfStatistics.NearestRank(samples, 0.99), Is.EqualTo(20.0));
        }

        [Test]
        public void NearestRank_SingleSample_AndUnsortedInput()
        {
            Assert.That(PerfStatistics.NearestRank(new[] { 42.0 }, 0.99), Is.EqualTo(42.0));

            var unsorted = new double[] { 9, 1, 7, 3, 5 };
            // sorted: 1,3,5,7,9; ceil(0.95 * 5) = 5 -> index 4 -> 9
            Assert.That(PerfStatistics.NearestRank(unsorted, 0.95), Is.EqualTo(9.0));
            // input array must stay unmodified (raw samples are never touched)
            Assert.That(unsorted, Is.EqualTo(new[] { 9.0, 1.0, 7.0, 3.0, 5.0 }));
        }

        [Test]
        public void Summarize_MinMaxComeDirectlyFromSamples()
        {
            var samples = new double[] { 0.5, 9.9, 1.0, 4.0, 2.0 };
            PerfStatistics.Summary summary = PerfStatistics.Summarize(samples);
            Assert.That(summary.Count, Is.EqualTo(5));
            Assert.That(summary.Min, Is.EqualTo(0.5));
            Assert.That(summary.Max, Is.EqualTo(9.9));
            Assert.That(summary.P95, Is.LessThanOrEqualTo(summary.Max));
            Assert.That(summary.P95, Is.LessThanOrEqualTo(summary.P99));
        }

        // ----------------------------------------------------------
        // Artifact schema (strict D-062/D-063 key sets)
        // ----------------------------------------------------------

        [Test]
        public void MetricArtifact_HasExactValidatorSchema()
        {
            string dir = CreateTempDir();
            var runs = new List<double[]>
            {
                new[] { 1.5, 2.5 },
                new[] { 3.5 },
            };
            ScenarioArtifacts.WriteMetric(
                dir, "m.combined.json", "scenario.SCALE_500_PRECOMBAT.pathfindingMs", "ms",
                "performanceMethod", warmupSeconds: 30, measurementSeconds: 120,
                runs, singleRunIndex: 0);

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "m.combined.json")));
            JsonElement root = doc.RootElement;
            Assert.That(PropertyNames(root), Is.EquivalentTo(new[] { "name", "unit", "measurement" }));
            Assert.That(root.GetProperty("name").GetString(), Is.EqualTo("scenario.SCALE_500_PRECOMBAT.pathfindingMs"));
            Assert.That(root.GetProperty("unit").GetString(), Is.EqualTo("ms"));

            JsonElement measurement = root.GetProperty("measurement");
            Assert.That(PropertyNames(measurement), Is.EquivalentTo(new[] { "methodRef", "warmupSeconds", "runs" }));
            Assert.That(measurement.GetProperty("methodRef").GetString(), Is.EqualTo("performanceMethod"));
            Assert.That(measurement.GetProperty("warmupSeconds").GetInt32(), Is.EqualTo(30));

            JsonElement runsElement = measurement.GetProperty("runs");
            Assert.That(runsElement.GetArrayLength(), Is.EqualTo(2));
            int expectedIndex = 1;
            foreach (JsonElement run in runsElement.EnumerateArray())
            {
                Assert.That(PropertyNames(run), Is.EquivalentTo(new[] { "index", "measurementSeconds", "samples" }));
                Assert.That(run.GetProperty("index").GetInt32(), Is.EqualTo(expectedIndex));
                Assert.That(run.GetProperty("measurementSeconds").GetInt32(), Is.EqualTo(120));
                expectedIndex++;
            }
            Assert.That(runsElement[0].GetProperty("samples").GetArrayLength(), Is.EqualTo(2));
            Assert.That(runsElement[0].GetProperty("samples")[0].GetDouble(), Is.EqualTo(1.5));
            Assert.That(runsElement[1].GetProperty("samples").GetArrayLength(), Is.EqualTo(1));
        }

        [Test]
        public void MetricArtifact_SingleRun_ContainsExactlyThatRun()
        {
            string dir = CreateTempDir();
            var runs = new List<double[]> { new[] { 1.0 }, new[] { 2.0 }, new[] { 3.0 } };
            ScenarioArtifacts.WriteMetric(
                dir, "m.run2.json", "scenario.SCALE_500_PRECOMBAT.precombatRestSimulationMs", "ms",
                "performanceMethod", warmupSeconds: 30, measurementSeconds: 120,
                runs, singleRunIndex: 2);

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "m.run2.json")));
            JsonElement runsElement = doc.RootElement.GetProperty("measurement").GetProperty("runs");
            Assert.That(runsElement.GetArrayLength(), Is.EqualTo(1));
            Assert.That(runsElement[0].GetProperty("index").GetInt32(), Is.EqualTo(2));
            Assert.That(runsElement[0].GetProperty("samples")[0].GetDouble(), Is.EqualTo(2.0));
        }

        [Test]
        public void AssertionArtifact_HasExactBoolSchema()
        {
            string dir = CreateTempDir();
            ScenarioArtifacts.WriteAssertion(dir, "a.json", "scenario.SCALE_500_PRECOMBAT.assertion.no-crash", passed: true);
            ScenarioArtifacts.WriteAssertion(dir, "b.json", "scenario.SCALE_500_PRECOMBAT.assertion.no-crash", passed: false);

            using JsonDocument passDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "a.json")));
            JsonElement root = passDoc.RootElement;
            Assert.That(PropertyNames(root), Is.EquivalentTo(new[] { "name", "unit", "samples" }));
            Assert.That(root.GetProperty("unit").GetString(), Is.EqualTo("bool"));
            Assert.That(root.GetProperty("samples").GetArrayLength(), Is.EqualTo(1));
            Assert.That(root.GetProperty("samples")[0].GetInt32(), Is.EqualTo(1));

            using JsonDocument failDoc = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "b.json")));
            Assert.That(failDoc.RootElement.GetProperty("samples")[0].GetInt32(), Is.EqualTo(0));
        }

        // ----------------------------------------------------------
        // Measurability: mini scenario run produces consistent artifacts
        // ----------------------------------------------------------

        [Test]
        public void MiniRun_ProducesValidArtifactsWithConsistentNumbers()
        {
            string dir = CreateTempDir();
            var options = new ScenarioOptions
            {
                Runs = 2,
                WarmupSeconds = 1,
                MeasureSeconds = 2,
                AgentCount = 50,
            };

            ScenarioResult result = Scale500PrecombatScenario.Run(options, NullNovaLogger.Instance);
            ScenarioArtifacts.WriteScenarioArtifacts(options, dir, result);

            Assert.That(result.NoCrash, Is.True, "no-crash assertion");
            Assert.That(result.MemoryGrowthBounded, Is.True, "no-unbounded-memory-growth assertion");
            Assert.That(result.Runs.Count, Is.EqualTo(2));

            const string prefix = "scenario.SCALE_500_PRECOMBAT";
            string[] expectedFiles =
            {
                prefix + ".pathfindingMs.run1.json",
                prefix + ".pathfindingMs.run2.json",
                prefix + ".pathfindingMs.combined.json",
                prefix + ".precombatRestSimulationMs.run1.json",
                prefix + ".precombatRestSimulationMs.run2.json",
                prefix + ".precombatRestSimulationMs.combined.json",
                prefix + ".assertion.no-crash.json",
                prefix + ".assertion.no-unbounded-memory-growth.json",
            };
            foreach (string file in expectedFiles)
            {
                Assert.That(File.Exists(Path.Combine(dir, file)), Is.True, $"missing artifact {file}");
            }

            // Artifacts record the ACTUALLY used method values, not the
            // contract defaults.
            using JsonDocument combined = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(dir, prefix + ".pathfindingMs.combined.json")));
            JsonElement measurement = combined.RootElement.GetProperty("measurement");
            Assert.That(measurement.GetProperty("warmupSeconds").GetInt32(), Is.EqualTo(1));
            JsonElement runs = measurement.GetProperty("runs");
            Assert.That(runs.GetArrayLength(), Is.EqualTo(2));

            var combinedSamples = new List<double>();
            for (int i = 0; i < 2; i++)
            {
                Assert.That(runs[i].GetProperty("index").GetInt32(), Is.EqualTo(i + 1));
                Assert.That(runs[i].GetProperty("measurementSeconds").GetInt32(), Is.EqualTo(2));
                JsonElement samples = runs[i].GetProperty("samples");
                Assert.That(samples.GetArrayLength(), Is.GreaterThan(0), "samples per run");
                foreach (JsonElement sample in samples.EnumerateArray())
                {
                    combinedSamples.Add(sample.GetDouble());
                }
            }

            // Consistency: artifact samples match the in-memory run samples
            // and P95 never exceeds the maximum.
            int expectedCount = result.Runs[0].PathfindingMs.Length + result.Runs[1].PathfindingMs.Length;
            Assert.That(combinedSamples.Count, Is.EqualTo(expectedCount));
            PerfStatistics.Summary summary = PerfStatistics.Summarize(combinedSamples);
            Assert.That(summary.P95, Is.LessThanOrEqualTo(summary.Max));
            Assert.That(summary.Max, Is.GreaterThan(0.0));

            using JsonDocument assertion = JsonDocument.Parse(
                File.ReadAllText(Path.Combine(dir, prefix + ".assertion.no-crash.json")));
            Assert.That(assertion.RootElement.GetProperty("samples")[0].GetInt32(), Is.EqualTo(1));
        }

        // ----------------------------------------------------------
        // Determinism: identical seed -> identical hash despite timing
        // ----------------------------------------------------------

        [Test]
        public void Workload_SameSeedSameHash_TimingDoesNotInfluenceSimulation()
        {
            var options = new ScenarioOptions { AgentCount = 100 };
            ulong hash1 = Scale500PrecombatScenario.RunFixedTicks(options, 150, NullNovaLogger.Instance);
            ulong hash2 = Scale500PrecombatScenario.RunFixedTicks(options, 150, NullNovaLogger.Instance);
            Assert.That(hash2, Is.EqualTo(hash1), "Equal seeds must produce equal state hashes with timing active.");

            var otherSeed = new ScenarioOptions { AgentCount = 100, Seed = options.Seed + 1 };
            ulong hash3 = Scale500PrecombatScenario.RunFixedTicks(otherSeed, 150, NullNovaLogger.Instance);
            Assert.That(hash3, Is.Not.EqualTo(hash1), "Different seeds should diverge (sanity check).");
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
            string dir = Path.Combine(Path.GetTempPath(), "nova-perf-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}
