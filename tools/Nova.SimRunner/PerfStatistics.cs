using System;
using System.Collections.Generic;

namespace Nova.SimRunner
{
    /// <summary>
    /// Sample statistics with the exact semantics of the gate evidence
    /// validator (quality/scripts/validate_gate_evidence.py, D-063):
    /// nearest-rank quantiles WITHOUT interpolation and WITHOUT outlier
    /// removal — index = max(0, ceil(q * n) - 1) over the ascending sort;
    /// min/max are taken directly from the raw samples. Raw samples are never
    /// modified.
    /// </summary>
    internal static class PerfStatistics
    {
        /// <summary>
        /// Nearest-rank quantile identical to the validator's
        /// <c>_nearest_rank</c>: ascending sort, rank = ceil(q * n), 1-based,
        /// clamped to [1, n]; returns the element at rank - 1.
        /// </summary>
        public static double NearestRank(IReadOnlyList<double> samples, double quantile)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (samples.Count == 0) throw new ArgumentException("Samples must not be empty.", nameof(samples));
            if (quantile < 0.0 || quantile > 1.0) throw new ArgumentOutOfRangeException(nameof(quantile));

            var ordered = new double[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                ordered[i] = samples[i];
            }
            Array.Sort(ordered);
            return OrderedNearestRank(ordered, quantile);
        }

        /// <summary>Nearest rank over an already ascending-sorted array.</summary>
        public static double OrderedNearestRank(double[] ordered, double quantile)
        {
            int index = Math.Max(0, (int)Math.Ceiling(quantile * ordered.Length) - 1);
            return ordered[index];
        }

        /// <summary>
        /// Min/Max/P95/P99 over the raw samples (nearest rank, no
        /// interpolation, no outlier removal — validator semantics).
        /// </summary>
        public readonly struct Summary
        {
            public Summary(int count, double min, double max, double p95, double p99)
            {
                Count = count;
                Min = min;
                Max = max;
                P95 = p95;
                P99 = p99;
            }

            public int Count { get; }
            public double Min { get; }
            public double Max { get; }
            public double P95 { get; }
            public double P99 { get; }
        }

        public static Summary Summarize(IReadOnlyList<double> samples)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (samples.Count == 0) throw new ArgumentException("Samples must not be empty.", nameof(samples));

            var ordered = new double[samples.Count];
            for (int i = 0; i < samples.Count; i++)
            {
                ordered[i] = samples[i];
            }
            Array.Sort(ordered);
            return new Summary(
                ordered.Length,
                ordered[0],
                ordered[ordered.Length - 1],
                OrderedNearestRank(ordered, 0.95),
                OrderedNearestRank(ordered, 0.99));
        }
    }
}
