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

using Jube.Charting.Exceptions;
using Jube.Charting.More.Models;

namespace Jube.Charting.More
{
    public static class DensityMath
    {
        private const int BinnedThreshold = 4096;
        private const int BinnedPoints = 2048;

        public static double[] Finite(IReadOnlyList<double> values)
        {
            var result = new List<double>(values.Count);

            foreach (var v in values)
            {
                if (double.IsFinite(v))
                {
                    result.Add(v);
                }
            }

            result.Sort();

            return [.. result];
        }

        public static double Quantile(IReadOnlyList<double> sorted, double p)
        {
            if (sorted.Count == 0)
            {
                return double.NaN;
            }

            var h = (sorted.Count - 1) * Math.Clamp(p, 0, 1);
            var lo = (int)Math.Floor(h);
            var hi = (int)Math.Ceiling(h);

            return sorted[lo] + (h - lo) * (sorted[hi] - sorted[lo]);
        }

        public static DistributionSummary Summarise(IReadOnlyList<double> values)
        {
            var s = Finite(values);

            if (s.Length == 0)
            {
                return new DistributionSummary(0, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN,
                    double.NaN,
                    double.NaN, double.NaN, double.NaN);
            }

            var mean = s.Average();
            var sd = StandardDeviation(s, mean);
            var q1 = Quantile(s, 0.25);
            var q3 = Quantile(s, 0.75);
            var lowFence = q1 - 1.5 * (q3 - q1);
            var highFence = q3 + 1.5 * (q3 - q1);

            return new DistributionSummary(s.Length, s[0], s[^1], mean, sd, q1, Quantile(s, 0.5), q3,
                s.First(v => v >= lowFence), s.Last(v => v <= highFence));
        }

        private static double StandardDeviation(double[] sorted, double mean)
        {
            if (sorted.Length < 2)
            {
                return 0;
            }

            var sum = 0.0;

            foreach (var v in sorted)
            {
                sum += (v - mean) * (v - mean);
            }

            return Math.Sqrt(sum / (sorted.Length - 1));
        }

        public static double SilvermanBandwidth(IReadOnlyList<double> values)
        {
            var s = Finite(values);

            if (s.Length < 2)
            {
                return 1;
            }

            var sd = StandardDeviation(s, s.Average());
            var iqr = Quantile(s, 0.75) - Quantile(s, 0.25);
            var spread = Math.Min(sd, iqr / 1.34);

            if (!(spread > 0))
            {
                spread = sd > 0 ? sd : Math.Abs(s[0]) > 0 ? Math.Abs(s[0]) : 1;
            }

            return 0.9 * spread * Math.Pow(s.Length, -0.2);
        }

        public static DensityEstimate Estimate(IReadOnlyList<double> values, double? bandwidth = null, int points = 128,
            double cut = 3, double? lower = null, double? upper = null)
        {
            if (points is < 8 or > 2048)
            {
                throw new ChartSpecificationException("Density grid size must be between 8 and 2048 points.");
            }

            if (bandwidth is { } b0 && !(b0 > 0 && double.IsFinite(b0)))
            {
                throw new ChartSpecificationException("The bandwidth must be a positive finite number.");
            }

            var s = Finite(values);
            var h = bandwidth ?? SilvermanBandwidth(s);

            if (s.Length == 0)
            {
                return new DensityEstimate([], [], h, 0);
            }

            var lo = lower ?? s[0] - cut * h;
            var hi = upper ?? s[^1] + cut * h;

            if (!(hi > lo))
            {
                lo -= 1;
                hi += 1;
            }

            var centres = s;
            double[]? weights = null;

            if (s.Length > BinnedThreshold)
            {
                var width = (s[^1] - s[0]) / BinnedPoints;

                if (width > 0)
                {
                    var counts = new double[BinnedPoints + 1];

                    foreach (var v in s)
                    {
                        counts[(int)Math.Min((v - s[0]) / width, BinnedPoints)]++;
                    }

                    var c = new List<double>();
                    var w = new List<double>();

                    for (var i = 0; i <= BinnedPoints; i++)
                    {
                        if (counts[i] > 0)
                        {
                            c.Add(s[0] + (i + 0.5) * width);
                            w.Add(counts[i]);
                        }
                    }

                    centres = [.. c];
                    weights = [.. w];
                }
            }

            var grid = new double[points];
            var density = new double[points];
            var norm = 1 / (s.Length * h * Math.Sqrt(2 * Math.PI));

            for (var g = 0; g < points; g++)
            {
                var x = lo + (hi - lo) * g / (points - 1);
                var sum = 0.0;

                for (var i = 0; i < centres.Length; i++)
                {
                    var z = (x - centres[i]) / h;

                    if (z > 8 || z < -8)
                    {
                        continue;
                    }

                    sum += (weights?[i] ?? 1) * Math.Exp(-0.5 * z * z);
                }

                grid[g] = x;
                density[g] = sum * norm;
            }

            return new DensityEstimate(grid, density, h, s.Length);
        }

        public static IReadOnlyList<(double X, double Y)> Ecdf(IReadOnlyList<double> values)
        {
            var s = Finite(values);
            var result = new List<(double, double)>();

            for (var i = 0; i < s.Length; i++)
            {
                if (i + 1 < s.Length && Math.Abs(s[i + 1] - s[i]) < 0.0001)
                {
                    continue;
                }

                result.Add((s[i], (double)(i + 1) / s.Length));
            }

            return result;
        }
    }
}