using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Nova.SimRunner
{
    /// <summary>
    /// Writes the D-062/D-063-conformant raw metric artifacts of the
    /// performance harness. Every artifact is strict JSON with exactly the
    /// keys the gate evidence validator
    /// (quality/scripts/validate_gate_evidence.py, <c>_numeric_sample_runs</c>)
    /// expects:
    /// <list type="bullet">
    /// <item>point/boolean metric: exactly <c>{name, unit, samples}</c>;
    /// boolean assertions use <c>unit = "bool"</c> and <c>samples = [1]</c>
    /// (or <c>[0]</c> on failure),</item>
    /// <item>performance metric: exactly <c>{name, unit, measurement}</c>
    /// with <c>measurement = {methodRef, warmupSeconds, runs}</c> and one
    /// <c>{index, measurementSeconds, samples}</c> object per run. The
    /// combined artifact carries all runs (indices 1..N) and is the file a
    /// later gate evidence references as the raw artifact; the per-run files
    /// carry exactly one run each for human inspection.</item>
    /// </list>
    /// Raw samples are written unmodified (rawSamplesRequired,
    /// outlierRemoval = false in the scenario contract). The artifacts
    /// record the ACTUALLY used warmup/measurement durations — only a run
    /// with the contract defaults (30 s warmup, 3 x 120 s) satisfies the
    /// validator's performance-method checks.
    /// </summary>
    internal static class ScenarioArtifacts
    {
        /// <summary>
        /// Writes one performance metric artifact.
        /// <paramref name="runSamples"/> holds the raw per-tick samples per
        /// run; <paramref name="singleRunIndex"/> (1-based) restricts the
        /// artifact to exactly that run, otherwise all runs are written.
        /// </summary>
        public static void WriteMetric(
            string directory,
            string fileName,
            string metricName,
            string unit,
            string methodRef,
            int warmupSeconds,
            int measurementSeconds,
            IReadOnlyList<double[]> runSamples,
            int singleRunIndex = 0)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("name", metricName);
                writer.WriteString("unit", unit);
                writer.WritePropertyName("measurement");
                writer.WriteStartObject();
                writer.WriteString("methodRef", methodRef);
                writer.WriteNumber("warmupSeconds", warmupSeconds);
                writer.WritePropertyName("runs");
                writer.WriteStartArray();
                for (int i = 0; i < runSamples.Count; i++)
                {
                    int runIndex = i + 1;
                    if (singleRunIndex != 0 && runIndex != singleRunIndex)
                    {
                        continue;
                    }
                    writer.WriteStartObject();
                    writer.WriteNumber("index", runIndex);
                    writer.WriteNumber("measurementSeconds", measurementSeconds);
                    writer.WritePropertyName("samples");
                    writer.WriteStartArray();
                    double[] samples = runSamples[i];
                    for (int s = 0; s < samples.Length; s++)
                    {
                        writer.WriteNumberValue(samples[s]);
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.Flush();
            }
        }

        /// <summary>
        /// Writes a boolean assertion artifact: exactly
        /// <c>{name, unit: "bool", samples: [1]}</c> on pass, <c>[0]</c> on
        /// failure (D-062 assertion semantics).
        /// </summary>
        public static void WriteAssertion(string directory, string fileName, string assertionName, bool passed)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, fileName);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteString("name", assertionName);
                writer.WriteString("unit", "bool");
                writer.WritePropertyName("samples");
                writer.WriteStartArray();
                writer.WriteNumberValue(passed ? 1 : 0);
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
            }
        }

        /// <summary>
        /// Writes the complete artifact set of a SCALE_500_PRECOMBAT
        /// execution into <paramref name="outDir"/>: per metric
        /// (pathfindingMs, precombatRestSimulationMs) one file per run plus
        /// the combined file a later gate evidence references, and the two
        /// boolean assertion artifacts. File names follow the D-062 metric
        /// naming (<c>scenario.&lt;ID&gt;.&lt;metric&gt;[.runN].json</c>).
        /// </summary>
        public static void WriteScenarioArtifacts(ScenarioOptions options, string outDir, ScenarioResult result)
        {
            const string prefix = "scenario." + ScenarioOptions.ScenarioId;
            var pathRuns = new List<double[]>();
            var restRuns = new List<double[]>();
            for (int i = 0; i < result.Runs.Count; i++)
            {
                pathRuns.Add(result.Runs[i].PathfindingMs);
                restRuns.Add(result.Runs[i].PrecombatRestMs);
            }

            if (pathRuns.Count > 0)
            {
                WriteMetricRuns(options, outDir, prefix + ".pathfindingMs", pathRuns);
                WriteMetricRuns(options, outDir, prefix + ".precombatRestSimulationMs", restRuns);
            }

            WriteAssertion(outDir, prefix + ".assertion.no-crash.json",
                prefix + ".assertion.no-crash", result.NoCrash);
            WriteAssertion(outDir, prefix + ".assertion.no-unbounded-memory-growth.json",
                prefix + ".assertion.no-unbounded-memory-growth", result.NoCrash && result.MemoryGrowthBounded);
        }

        private static void WriteMetricRuns(ScenarioOptions options, string outDir, string metricName, List<double[]> runSamples)
        {
            for (int i = 0; i < runSamples.Count; i++)
            {
                WriteMetric(
                    outDir, $"{metricName}.run{i + 1}.json", metricName, "ms",
                    ScenarioOptions.MethodRef, options.WarmupSeconds, options.MeasureSeconds,
                    runSamples, singleRunIndex: i + 1);
            }
            WriteMetric(
                outDir, $"{metricName}.combined.json", metricName, "ms",
                ScenarioOptions.MethodRef, options.WarmupSeconds, options.MeasureSeconds,
                runSamples);
        }
    }
}
