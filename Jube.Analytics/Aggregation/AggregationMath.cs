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

namespace Jube.Analytics.Aggregation
{
    internal static class AggregationMath
    {
        public const double NormalConsistencyConstant = 1.482602218505602;

        public static double Sum(double[] values)
        {
            return values.Sum();
        }

        public static double Mean(double[] values)
        {
            return values.Length == 0 ? double.NaN : Sum(values) / values.Length;
        }

        public static double[] Sorted(double[] values)
        {
            var sorted = (double[])values.Clone();
            Array.Sort(sorted);

            return sorted;
        }

        public static double Median(double[] values)
        {
            return values.Length == 0 ? double.NaN : Numerics.Median(Sorted(values));
        }

        public static double Variance(double[] values, bool unbiased)
        {
            var divisor = unbiased ? values.Length - 1 : values.Length;

            if (divisor <= 0)
            {
                return double.NaN;
            }

            var mean = Mean(values);

            return values.Sum(value => (value - mean) * (value - mean)) / divisor;
        }

        public static double RawMedianAbsoluteDeviation(double[] values)
        {
            if (values.Length == 0)
            {
                return double.NaN;
            }

            var median = Median(values);
            var deviations = new double[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                deviations[i] = Math.Abs(values[i] - median);
            }

            return Median(deviations);
        }

        public static double[] AllButLast(double[] values)
        {
            return values.Length <= 1 ? [] : values[..^1];
        }
    }
}
