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

using Accord.Math;
using Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ContaminationEstimator
    {
        public static ContaminationEstimate[] Estimate(double[] scores)
        {
            var values = scores.Where(double.IsFinite).OrderBy(v => v).ToArray();

            if (values.Length < 20)
            {
                return
                [
                    new ContaminationEstimate("None", null, "Fewer than 20 finite scores; no estimate is reliable.")
                ];
            }

            var estimates = new List<ContaminationEstimate>
            {
                ModifiedZScore(values),
                Tukey(values),
                Gap(values),
                Mixture(values)
            };

            return estimates.ToArray();
        }

        public static double? Recommend(ContaminationEstimate[] estimates)
        {
            var shares = estimates.Where(e => e.Share.HasValue && e.Share.Value > 0).Select(e => e.Share!.Value)
                .OrderBy(v => v).ToArray();

            if (shares.Length == 0)
            {
                return null;
            }

            var median = shares.Length % 2 == 1
                ? shares[shares.Length / 2]
                : (shares[shares.Length / 2 - 1] + shares[shares.Length / 2]) / 2d;

            return Math.Min(0.3, Math.Max(0.001, median));
        }

        private static ContaminationEstimate ModifiedZScore(double[] sorted)
        {
            var median = ModelMath.Quantile(sorted, 0.5);
            var deviations = sorted.Select(v => Math.Abs(v - median)).OrderBy(v => v).ToArray();
            var mad = ModelMath.Quantile(deviations, 0.5);

            if (mad <= 0)
            {
                return new ContaminationEstimate("ModifiedZScore", null,
                    "The median absolute deviation is zero, so robust z scores are undefined.");
            }

            var flagged = sorted.Count(v => 0.6745 * (v - median) / mad > 3.5);

            return new ContaminationEstimate("ModifiedZScore", flagged / (double)sorted.Length,
                "Share of scores with an upward modified z score above 3.5 (Iglewicz and Hoaglin).");
        }

        private static ContaminationEstimate Tukey(double[] sorted)
        {
            var q1 = ModelMath.Quantile(sorted, 0.25);
            var q3 = ModelMath.Quantile(sorted, 0.75);
            var iqr = q3 - q1;

            if (iqr <= 0)
            {
                return new ContaminationEstimate("TukeyFence", null, "The interquartile range is zero.");
            }

            var flagged = sorted.Count(v => v > q3 + 1.5 * iqr);

            return new ContaminationEstimate("TukeyFence", flagged / (double)sorted.Length,
                "Share of scores above the upper Tukey fence, Q3 plus 1.5 interquartile ranges.");
        }

        private static ContaminationEstimate Gap(double[] sorted)
        {
            var n = sorted.Length;
            var range = sorted[^1] - sorted[0];

            if (range <= 0)
            {
                return new ContaminationEstimate("LargestGap", null, "All scores are equal.");
            }

            var limit = Math.Max(1, (int)(0.3 * n));
            var bestGap = 0d;
            var bestCount = 0;

            for (var k = 1; k <= limit; k++)
            {
                var gap = sorted[n - k] - sorted[n - k - 1];

                if (gap > bestGap)
                {
                    bestGap = gap;
                    bestCount = k;
                }
            }

            if (bestGap / range < 0.05)
            {
                return new ContaminationEstimate("LargestGap", null,
                    "No clear break in the upper 30 percent of the sorted scores.");
            }

            return new ContaminationEstimate("LargestGap", bestCount / (double)n,
                $"The largest break in the upper 30 percent of the sorted scores is below the top {bestCount} rows.");
        }

        private static ContaminationEstimate Mixture(double[] sorted)
        {
            var positive = sorted.All(v => v > 0);
            var data = positive ? sorted.Select(v => Math.Log(v)).ToArray() : sorted;
            var n = data.Length;
            double[] mean = [ModelMath.Quantile(data, 0.4), ModelMath.Quantile(data, 0.95)];
            var overall = data.Average();
            var spread = Math.Max(Math.Sqrt(data.Sum(v => (v - overall) * (v - overall)) / n), 1e-9);
            double[] variance = [spread * spread, spread * spread / 4d];
            double[] weight = [0.9, 0.1];

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var responsibility = new double[n];

                for (var i = 0; i < n; i++)
                {
                    var a = weight[0] * Density(data[i], mean[0], variance[0]);
                    var b = weight[1] * Density(data[i], mean[1], variance[1]);
                    responsibility[i] = a + b > 0 ? b / (a + b) : 0d;
                }

                var high = responsibility.Sum();
                var low = n - high;

                if (high < 1e-6 || low < 1e-6)
                {
                    break;
                }

                mean[1] = Enumerable.Range(0, n).Sum(i => responsibility[i] * data[i]) / high;
                mean[0] = Enumerable.Range(0, n).Sum(i => (1d - responsibility[i]) * data[i]) / low;

                variance[1] = Math.Max(
                    Enumerable.Range(0, n).Sum(i => responsibility[i] * Math.Pow(data[i] - mean[1], 2)) / high,
                    1e-9 * spread * spread + 1e-12);

                variance[0] = Math.Max(
                    Enumerable.Range(0, n).Sum(i => (1d - responsibility[i]) * Math.Pow(data[i] - mean[0], 2)) / low,
                    1e-9 * spread * spread + 1e-12);

                weight[1] = high / n;
                weight[0] = low / n;
            }

            var separation = (mean[1] - mean[0]) / Math.Sqrt((variance[0] + variance[1]) / 2d);

            if (mean[1] <= mean[0] || separation < 2d || weight[1] > 0.5)
            {
                return new ContaminationEstimate("TwoComponentMixture", null,
                    "A two component mixture on the scores does not separate a distinct high-score group.");
            }

            return new ContaminationEstimate("TwoComponentMixture", weight[1],
                $"Weight of the upper component of a two component Gaussian mixture fitted to the {(positive ? "log " : string.Empty)}scores; the components are {separation:0.#} standard deviations apart.");
        }

        private static double Density(double x, double mean, double variance)
        {
            return Math.Exp(-0.5 * (x - mean) * (x - mean) / variance) / Math.Sqrt(2d * Math.PI * variance);
        }
    }
}