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

using System.Globalization;

namespace Jube.Charting.Ticks
{
    public static class NumberFormat
    {
        public static string Axis(double value, double step, double maxAbs)
        {
            var unit = 1.0;
            var suffix = string.Empty;

            if (maxAbs >= 1e9 && step >= 1e8)
            {
                unit = 1e9;
                suffix = "B";
            }
            else if (maxAbs >= 1e6 && step >= 1e5)
            {
                unit = 1e6;
                suffix = "M";
            }
            else if (maxAbs >= 1e4 && step >= 100)
            {
                unit = 1e3;
                suffix = "k";
            }

            if (value == 0)
            {
                return "0";
            }

            var scaledStep = step / unit;
            var decimals = DecimalsFor(scaledStep);

            return Fixed(value / unit, decimals, suffix.Length > 0) + suffix;
        }

        public static IReadOnlyList<string> Axis(IReadOnlyList<double> values)
        {
            if (values.Count == 0)
            {
                return [];
            }

            var step = values.Count > 1 ? Math.Abs(values[1] - values[0]) : Math.Max(Math.Abs(values[0]), 1);
            var maxAbs = values.Max(Math.Abs);

            return [.. values.Select(v => Axis(v, step, maxAbs))];
        }

        public static string Value(double value)
        {
            if (double.IsNaN(value))
            {
                return "n/a";
            }

            if (double.IsPositiveInfinity(value))
            {
                return "+inf";
            }

            if (double.IsNegativeInfinity(value))
            {
                return "-inf";
            }

            if (value == 0)
            {
                return "0";
            }

            var abs = Math.Abs(value);

            if (abs >= 1e15 || abs < 1e-6)
            {
                return value.ToString("0.###E+0", CultureInfo.InvariantCulture);
            }

            var digits = Math.Clamp(3 - (int)Math.Floor(Math.Log10(abs)), 0, 10);

            return Fixed(value, digits, true);
        }

        public static string Percent(double fraction, int decimals = 1)
        {
            return double.IsFinite(fraction) ? Fixed(fraction * 100, decimals, true) + "%" : "n/a";
        }

        private static int DecimalsFor(double step)
        {
            if (!(step > 0) || !double.IsFinite(step))
            {
                return 0;
            }

            for (var d = 0; d <= 10; d++)
            {
                if (Math.Abs(Math.Round(step, d) - step) < step * 1e-6)
                {
                    return d;
                }
            }

            return 10;
        }

        private static string Fixed(double value, int decimals, bool trim = false)
        {
            var rounded = Math.Round(value, decimals, MidpointRounding.AwayFromZero);

            if (rounded == 0)
            {
                return "0";
            }

            return rounded.ToString(trim ? "0." + new string('#', decimals) : "F" + decimals,
                    CultureInfo.InvariantCulture)
                .TrimEnd('.');
        }
    }
}