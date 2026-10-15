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

using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;
using Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.DistributionsAndHistograms
{
    internal static class ShapeAndTailEngine
    {
        public static NormalityShapeResult Shape(double[] values)
        {
            var n = values.Length;
            var mean = values.Mean();
            var sd = values.StandardDeviation();

            if (!(sd > 0))
            {
                throw new ArgumentException(
                    "All values are identical, so skewness and kurtosis are undefined. Supply values with some spread.",
                    "values");
            }

            var skew = values.Skewness();
            var kurt = values.Kurtosis();
            var seSkew = Math.Sqrt(6.0 * n * (n - 1.0) / ((n - 2.0) * (n + 1.0) * (n + 3.0)));
            var seKurt = 2.0 * seSkew * Math.Sqrt((n * (double)n - 1.0) / ((n - 3.0) * (n + 5.0)));
            var notes = new List<string>();

            double? jb = null;
            double? jbp = null;

            if (n >= 8)
            {
                var g1 = values.Skewness(false);
                var g2 = values.Kurtosis(false);
                jb = n / 6.0 * (g1 * g1 + g2 * g2 / 4.0);
                jbp = new ChiSquareDistribution(2).ComplementaryDistributionFunction(jb.Value);
            }
            else
            {
                notes.Add(
                    "Fewer than 8 values: the Jarque-Bera statistic is not computed and the shape check is unreliable.");
            }

            var zs = skew / seSkew;
            var zk = kurt / seKurt;
            string verdict;

            if (n < 8)
            {
                verdict = "Too few values for a reliable shape check.";
            }
            else
            {
                var skewed = Math.Abs(zs) > 1.96;
                var tailed = Math.Abs(zk) > 1.96;
                var parts = new List<string>();

                if (skewed)
                {
                    parts.Add(skew > 0 ? "right skewed (long upper tail)" : "left skewed (long lower tail)");
                }

                if (tailed)
                {
                    parts.Add(kurt > 0 ? "heavier tailed than normal" : "lighter tailed than normal");
                }

                verdict = parts.Count == 0
                    ? "No evidence against a normal shape: skewness and kurtosis are within about two standard errors of 0."
                    : "Not normal shaped: " + string.Join(" and ", parts) + ".";
            }

            return new NormalityShapeResult(n, mean, sd, skew, seSkew, zs, kurt, seKurt, zk,
                DistributionGuard.N(jb ?? double.NaN), DistributionGuard.N(jbp ?? double.NaN), verdict,
                notes.ToArray());
        }

        public static TailExceedanceRow Row(double[] values, double level, double threshold, TailDirection direction)
        {
            var excess = new List<double>();

            foreach (var v in values)
            {
                if (direction == TailDirection.Upper ? v > threshold : v < threshold)
                {
                    excess.Add(direction == TailDirection.Upper ? v - threshold : threshold - v);
                }
            }

            var count = excess.Count;

            return new TailExceedanceRow(
                level,
                threshold,
                count,
                (double)count / values.Length,
                count > 0 ? excess.Average() : null,
                count > 0
                    ? direction == TailDirection.Upper ? threshold + excess.Average() : threshold - excess.Average()
                    : null,
                count > 0 ? excess.Max() : null);
        }
    }
}