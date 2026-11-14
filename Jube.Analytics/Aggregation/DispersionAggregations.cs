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
    public static class DispersionAggregations
    {
        private const string UnbiasedText =
            "True uses the sample estimator (divides by count minus one) and needs at least two values; false uses the population estimator.";

        [Aggregation(23)]
        [Description("Variance: the average squared distance from the mean.")]
        public static double Variance(double[] values, [Description(UnbiasedText)] bool unbiased = true)
        {
            return AggregationMath.Variance(values, unbiased);
        }

        [Aggregation(24)]
        [Description("Range: the largest value minus the smallest value.")]
        public static double Range(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var minimum = values[0];
            var maximum = values[0];

            foreach (var value in values)
            {
                minimum = Math.Min(minimum, value);
                maximum = Math.Max(maximum, value);
            }

            return maximum - minimum;
        }

        [Aggregation(25)]
        [Description("Interquartile range: the 75th percentile minus the 25th percentile, the spread of the middle half of the values.")]
        public static double InterquartileRange(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sorted = AggregationMath.Sorted(values);

            return Numerics.Quantile(sorted, 0.75) - Numerics.Quantile(sorted, 0.25);
        }

        [Aggregation(26)]
        [Description("Percentile by linear interpolation between the two nearest sorted values, the spreadsheet PERCENTILE.INC convention.")]
        public static double Percentile(double[] values,
            [Range(0d, 100d)] [Description("Percentile to return, from 0 to 100. 50 is the median.")]
            double percentile = 95)
        {
            return values.Length == 0
                ? double.NaN
                : Numerics.Quantile(AggregationMath.Sorted(values), percentile / 100d);
        }

        [Aggregation(27)]
        [Description("Median absolute deviation: the median distance from the median, a spread measure that extreme values barely move.")]
        public static double MedianAbsoluteDeviation(double[] values,
            [Description("True multiplies by 1.4826 so the result is comparable to a standard deviation for normally distributed data.")]
            bool scaleToNormal = true)
        {
            var deviation = AggregationMath.RawMedianAbsoluteDeviation(values);

            return scaleToNormal ? deviation * AggregationMath.NormalConsistencyConstant : deviation;
        }

        [Aggregation(28)]
        [Description("Mean absolute deviation: the average distance from the centre of the values.")]
        public static double MeanAbsoluteDeviation(double[] values,
            [Description("True measures distance from the median; false measures distance from the mean.")]
            bool aroundMedian = false)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var centre = aroundMedian ? AggregationMath.Median(values) : AggregationMath.Mean(values);

            return values.Sum(value => Math.Abs(value - centre)) / values.Length;
        }

        [Aggregation(29)]
        [Description("Coefficient of variation: the standard deviation divided by the absolute mean, a unit-free measure of how erratic the values are.")]
        public static double CoefficientOfVariation(double[] values,
            [Description(UnbiasedText)] bool unbiased = true)
        {
            return Math.Sqrt(AggregationMath.Variance(values, unbiased)) / Math.Abs(AggregationMath.Mean(values));
        }
    }
}
