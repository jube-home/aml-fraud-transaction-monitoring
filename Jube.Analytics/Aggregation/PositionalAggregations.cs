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
    public static class PositionalAggregations
    {
        [Aggregation(32, OrderSensitive = true)]
        [Description("The value at a given position in iteration order. Zero when there are fewer values than the position asks for.")]
        public static double NthValue(double[] values,
            [Range(1, int.MaxValue)] [Description("One-based position in iteration order. 1 is the first value.")]
            int position = 1)
        {
            return position > values.Length ? double.NaN : values[position - 1];
        }

        [Aggregation(33, OrderSensitive = true)]
        [Description("The last value minus the first value in iteration order: the net movement across the matched records.")]
        public static double FirstToLastChange(double[] values)
        {
            return values.Length == 0 ? double.NaN : values[^1] - values[0];
        }

        [Aggregation(34, OrderSensitive = true)]
        [Description("The largest absolute difference between two consecutive values in iteration order: the biggest single jump from one record to the next.")]
        public static double LargestStep(double[] values)
        {
            if (values.Length < 2)
            {
                return double.NaN;
            }

            var largest = 0d;

            for (var i = 1; i < values.Length; i++)
            {
                largest = Math.Max(largest, Math.Abs(values[i] - values[i - 1]));
            }

            return largest;
        }
    }
}
