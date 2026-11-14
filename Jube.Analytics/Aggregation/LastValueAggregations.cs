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

namespace Jube.Analytics.Aggregation
{
    public static class LastValueAggregations
    {
        private const double ModifiedZScoreConstant = 0.6745;

        [Aggregation(40, OrderSensitive = true)]
        [Description("How many standard deviations the last value in iteration order sits from the mean of the values before it. Under Ascending the last value is the current transaction, so this scores it against its own history. Needs at least three values.")]
        public static double LastValueZScore(double[] values)
        {
            if (values.Length < 3)
            {
                return double.NaN;
            }

            var history = AggregationMath.AllButLast(values);

            return (values[^1] - AggregationMath.Mean(history)) / Math.Sqrt(AggregationMath.Variance(history, true));
        }

        [Aggregation(41, OrderSensitive = true)]
        [Description("Robust score of the last value in iteration order against the values before it, using their median and median absolute deviation, so earlier extremes do not hide a new one. Needs at least three values.")]
        public static double LastValueModifiedZScore(double[] values)
        {
            if (values.Length < 3)
            {
                return double.NaN;
            }

            var history = AggregationMath.AllButLast(values);

            return ModifiedZScoreConstant * (values[^1] - AggregationMath.Median(history)) /
                   AggregationMath.RawMedianAbsoluteDeviation(history);
        }

        [Aggregation(42, OrderSensitive = true)]
        [Description("The percentage, from 0 to 100, of the values before the last one in iteration order that are less than or equal to it. 100 means the last value is the largest seen.")]
        public static double LastValuePercentileRank(double[] values)
        {
            if (values.Length < 2)
            {
                return double.NaN;
            }

            var last = values[^1];
            var count = 0;

            for (var i = 0; i < values.Length - 1; i++)
            {
                if (values[i] <= last)
                {
                    count++;
                }
            }

            return 100d * count / (values.Length - 1);
        }

        [Aggregation(43, OrderSensitive = true)]
        [Description("The last value in iteration order divided by the mean of the values before it. 3 means the last value is three times the earlier average.")]
        public static double LastValueToMeanRatio(double[] values)
        {
            return values.Length < 2
                ? double.NaN
                : values[^1] / AggregationMath.Mean(AggregationMath.AllButLast(values));
        }
    }
}
