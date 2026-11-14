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
    public static class TrendAggregations
    {
        [Aggregation(44, OrderSensitive = true)]
        [Description("Least-squares slope of the values against their position in iteration order: the average change from one record to the next. Positive means the values are rising.")]
        public static double TrendSlope(double[] values)
        {
            if (values.Length < 2)
            {
                return double.NaN;
            }

            var meanPosition = (values.Length - 1) / 2d;
            var meanValue = AggregationMath.Mean(values);
            var numerator = 0d;
            var denominator = 0d;

            for (var i = 0; i < values.Length; i++)
            {
                var offset = i - meanPosition;
                numerator += offset * (values[i] - meanValue);
                denominator += offset * offset;
            }

            return numerator / denominator;
        }

        [Aggregation(45, OrderSensitive = true)]
        [Description("Autocorrelation of the values with themselves a number of records earlier in iteration order, from -1 to 1. Values near 1 mean a repeating or persistent pattern.")]
        public static double Autocorrelation(double[] values,
            [Range(1, int.MaxValue)] [Description("How many records back to compare each value with. 1 compares each value with the one before it.")]
            int lag = 1)
        {
            if (values.Length <= lag)
            {
                return double.NaN;
            }

            var mean = AggregationMath.Mean(values);
            var numerator = 0d;
            var denominator = 0d;

            for (var i = 0; i < values.Length; i++)
            {
                var deviation = values[i] - mean;
                denominator += deviation * deviation;

                if (i >= lag)
                {
                    numerator += deviation * (values[i - lag] - mean);
                }
            }

            return numerator / denominator;
        }
    }
}
