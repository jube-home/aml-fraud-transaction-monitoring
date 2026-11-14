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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Jube.Analytics.Aggregation
{
    public static class CentralTendencyAggregations
    {
        [Aggregation(17)]
        [Description("Geometric mean: the nth root of the product of the values. Suits ratios and growth rates. Every value must be greater than zero, otherwise the result is zero.")]
        public static double GeometricMean(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sum = 0d;

            foreach (var value in values)
            {
                if (value <= 0)
                {
                    return double.NaN;
                }

                sum += Math.Log(value);
            }

            return Math.Exp(sum / values.Length);
        }

        [Aggregation(18)]
        [Description("Harmonic mean: the count divided by the sum of reciprocals. Suits rates. Every value must be greater than zero, otherwise the result is zero.")]
        public static double HarmonicMean(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sum = 0d;

            foreach (var value in values)
            {
                if (value <= 0)
                {
                    return double.NaN;
                }

                sum += 1d / value;
            }

            return 1d / (sum / values.Length);
        }

        [Aggregation(19)]
        [Description("Mean after discarding the lowest and highest share of the sorted values, so a few extreme transactions do not move it.")]
        public static double TrimmedMean(double[] values,
            [Range(0d, 0.5d, MaximumIsExclusive = true)]
            [Description("Share discarded from each tail, from 0 up to but not including 0.5.")]
            double trimFraction = 0.1)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sorted = AggregationMath.Sorted(values);
            var k = (int)Math.Floor(sorted.Length * trimFraction);

            return AggregationMath.Mean(sorted[k..^k]);
        }

        [Aggregation(20)]
        [Description("Mean after replacing the lowest and highest share of the sorted values with the nearest retained value, limiting extremes while keeping the count.")]
        public static double WinsorisedMean(double[] values,
            [Range(0d, 0.5d, MaximumIsExclusive = true)]
            [Description("Share replaced in each tail, from 0 up to but not including 0.5.")]
            double winsorFraction = 0.05)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sorted = AggregationMath.Sorted(values);
            var k = (int)Math.Floor(sorted.Length * winsorFraction);

            for (var i = 0; i < k; i++)
            {
                sorted[i] = sorted[k];
                sorted[sorted.Length - 1 - i] = sorted[sorted.Length - 1 - k];
            }

            return AggregationMath.Mean(sorted);
        }

        [Aggregation(21, OrderSensitive = true)]
        [Description("Exponentially weighted mean in iteration order: the last value has weight 1 and each earlier value counts for less by a constant factor, so under Ascending the most recent transactions dominate.")]
        public static double ExponentialWeightedMean(double[] values,
            [Range(0d, 1d, MinimumIsExclusive = true)]
            [Description("Smoothing factor, greater than 0 and at most 1. Higher values weight the latest records more heavily.")]
            double alpha = 0.3)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var decay = 1d - alpha;
            var weight = 1d;
            var weighted = 0d;
            var total = 0d;

            for (var i = values.Length - 1; i >= 0; i--)
            {
                weighted += weight * values[i];
                total += weight;
                weight *= decay;
            }

            return weighted / total;
        }

        [Aggregation(22)]
        [Description("Root mean square: the square root of the mean of the squared values. A magnitude measure that gives large values more weight than the plain mean.")]
        public static double RootMeanSquare(double[] values)
        {
            return values.Length == 0
                ? double.NaN
                : Math.Sqrt(values.Sum(value => value * value) / values.Length);
        }
    }
}
