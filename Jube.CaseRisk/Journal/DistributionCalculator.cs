/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */


namespace Jube.CaseRisk.Journal
{
    public sealed record DistributionBin(double From, double To, int Count);

    public sealed record BoxSummary(double Q1, double Median, double Q3, double LowerFence, double UpperFence,
        int Outliers);

    public sealed record DistributionReport(
        string Field,
        int Count,
        double Min,
        double Max,
        double Mean,
        double Median,
        double P5,
        double P95,
        IReadOnlyList<DistributionBin> Bins,
        BoxSummary Box,
        double Latest,
        double LatestPercentile);

    public static class DistributionCalculator
    {
        public const int MinimumBins = 5;
        public const int MaximumBins = 40;
        private const double FenceMultiple = 1.5;

        public static IReadOnlyList<DistributionReport> Compute(IReadOnlyList<JournalEvent> events,
            IReadOnlyList<string> measures)
        {
            var reports = new List<DistributionReport>();

            foreach (var field in measures)
            {
                var present = events.Where(e => e.Measures.ContainsKey(field)).ToList();

                if (present.Count == 0)
                {
                    continue;
                }

                var sorted = RobustStatistics.Sorted(present.Select(e => e.Measures[field]));

                if (sorted.Length == 0)
                {
                    continue;
                }

                var latest = present.OrderByDescending(e => e.At).ThenByDescending(e => e.Id).First().Measures[field];

                reports.Add(Build(field, sorted, latest));
            }

            return reports;
        }

        public static DistributionReport Build(string field, double[] sorted, double latest)
        {
            var q1 = RobustStatistics.Quantile(sorted, 0.25);
            var q3 = RobustStatistics.Quantile(sorted, 0.75);
            var iqr = q3 - q1;
            var lower = q1 - FenceMultiple * iqr;
            var upper = q3 + FenceMultiple * iqr;

            var below = sorted.Count(value => value <= latest);
            var percentile = sorted.Length == 0 ? 0 : (double)below / sorted.Length;

            return new DistributionReport(field, sorted.Length, sorted[0], sorted[^1], sorted.Average(),
                RobustStatistics.Quantile(sorted, 0.5), RobustStatistics.Quantile(sorted, 0.05),
                RobustStatistics.Quantile(sorted, 0.95), Bins(sorted, iqr),
                new BoxSummary(q1, RobustStatistics.Quantile(sorted, 0.5), q3, lower, upper,
                    sorted.Count(value => value < lower || value > upper)), latest, percentile);
        }

        public static IReadOnlyList<DistributionBin> Bins(double[] sorted, double iqr)
        {
            var range = sorted[^1] - sorted[0];

            if (range == 0)
            {
                return [new DistributionBin(sorted[0], sorted[0], sorted.Length)];
            }

            var width = iqr > 0 ? 2 * iqr / Math.Cbrt(sorted.Length) : range / Math.Ceiling(Math.Sqrt(sorted.Length));
            var count = Math.Clamp((int)Math.Ceiling(range / width), MinimumBins, MaximumBins);
            var step = range / count;
            var counts = new int[count];

            foreach (var value in sorted)
            {
                counts[Math.Min(count - 1, (int)((value - sorted[0]) / step))]++;
            }

            return Enumerable.Range(0, count)
                .Select(index => new DistributionBin(sorted[0] + index * step, sorted[0] + (index + 1) * step,
                    counts[index])).ToList();
        }
    }
}
