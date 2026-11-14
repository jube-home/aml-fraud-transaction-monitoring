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
    public static class ShapeAggregations
    {
        [Aggregation(30)]
        [Description("Shannon entropy, in bits, of how often each distinct value occurs. Zero when every value is the same, higher the more evenly the values are spread across distinct amounts.")]
        public static double Entropy(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var counts = new Dictionary<double, int>();

            foreach (var value in values)
            {
                counts[value] = counts.GetValueOrDefault(value) + 1;
            }

            return 0d - counts.Values.Select(count => (double)count / values.Length)
                .Sum(share => share * Math.Log2(share));
        }

        [Aggregation(31)]
        [Description("Gini coefficient: 0 when every value is equal, approaching 1 when one value accounts for nearly the whole total. Values must not be negative, otherwise the result is zero.")]
        public static double GiniCoefficient(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var sorted = AggregationMath.Sorted(values);

            if (sorted[0] < 0)
            {
                return double.NaN;
            }

            var total = 0d;
            var weighted = 0d;

            for (var i = 0; i < sorted.Length; i++)
            {
                total += sorted[i];
                weighted += (i + 1) * sorted[i];
            }

            var n = sorted.Length;

            return 2d * weighted / (n * total) - (n + 1d) / n;
        }
    }
}
