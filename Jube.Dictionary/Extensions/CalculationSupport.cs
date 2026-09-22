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

namespace Jube.Dictionary.Extensions
{
    internal static class CalculationSupport
    {
        internal static double Finite(double value)
        {
            return double.IsFinite(value) ? value : double.NaN;
        }

        internal static double Largest(double value, double[]? others)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            var largest = value;
            foreach (var other in others ?? [])
            {
                if (double.IsNaN(other))
                {
                    return double.NaN;
                }

                largest = Math.Max(largest, other);
            }

            return Finite(largest);
        }

        internal static double Smallest(double value, double[]? others)
        {
            if (double.IsNaN(value))
            {
                return double.NaN;
            }

            var smallest = value;
            foreach (var other in others ?? [])
            {
                if (double.IsNaN(other))
                {
                    return double.NaN;
                }

                smallest = Math.Min(smallest, other);
            }

            return Finite(smallest);
        }

        private static double[]? Shares(double value, double[]? others)
        {
            var all = new List<double> { value };
            all.AddRange(others ?? []);
            if (all.Any(x => !double.IsFinite(x) || x < 0))
            {
                return null;
            }

            var total = all.Sum();
            if (total <= 0)
            {
                return null;
            }

            return all.Select(x => x / total).ToArray();
        }

        internal static double Herfindahl(double value, double[]? others)
        {
            var shares = Shares(value, others);
            return shares is null ? double.NaN : Finite(shares.Sum(s => s * s));
        }

        internal static double EntropyBits(double value, double[]? others)
        {
            var shares = Shares(value, others);
            return shares is null ? double.NaN : Finite(-shares.Where(s => s > 0).Sum(s => s * Math.Log2(s)) + 0.0);
        }

        internal static bool Above(double value, double threshold)
        {
            return !double.IsNaN(value) && !double.IsNaN(threshold) && value > threshold;
        }

        internal static bool Below(double value, double threshold)
        {
            return !double.IsNaN(value) && !double.IsNaN(threshold) && value < threshold;
        }

        internal static bool InRange(double value, double minimum, double maximum)
        {
            return !double.IsNaN(value) && value >= minimum && value <= maximum;
        }

        internal static bool OutsideRange(double value, double minimum, double maximum)
        {
            return !double.IsNaN(value) && (value < minimum || value > maximum);
        }
    }
}
