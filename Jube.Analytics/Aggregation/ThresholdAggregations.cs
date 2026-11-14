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
    public static class ThresholdAggregations
    {
        private const string ThresholdText = "The value to compare against. A value equal to the threshold is not counted.";

        [Aggregation(35)]
        [Description("How many values are strictly above the threshold.")]
        public static double CountAbove(double[] values, [Description(ThresholdText)] double threshold = 0)
        {
            return values.Count(value => value > threshold);
        }

        [Aggregation(36)]
        [Description("How many values are strictly below the threshold.")]
        public static double CountBelow(double[] values, [Description(ThresholdText)] double threshold = 0)
        {
            return values.Count(value => value < threshold);
        }

        [Aggregation(37)]
        [Description("The share of values, from 0 to 1, that are strictly above the threshold.")]
        public static double ShareAbove(double[] values, [Description(ThresholdText)] double threshold = 0)
        {
            return values.Length == 0 ? double.NaN : CountAbove(values, threshold) / values.Length;
        }

        [Aggregation(38)]
        [Description("The sum of only those values that are strictly above the threshold.")]
        public static double SumAbove(double[] values, [Description(ThresholdText)] double threshold = 0)
        {
            return values.Where(value => value > threshold).Sum();
        }

        [Aggregation(39, OrderSensitive = true)]
        [Description("The longest run of consecutive values, in iteration order, that are all strictly above the threshold.")]
        public static double LongestRunAbove(double[] values, [Description(ThresholdText)] double threshold = 0)
        {
            var longest = 0;
            var current = 0;

            foreach (var value in values)
            {
                current = value > threshold ? current + 1 : 0;
                longest = Math.Max(longest, current);
            }

            return longest;
        }
    }
}
