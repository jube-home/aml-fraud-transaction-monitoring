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

using Jube.Charting.Exceptions;

namespace Jube.Charting.Ticks
{
    public static class LogTicks
    {
        public static IReadOnlyList<double> Generate(double min, double max)
        {
            if (!(min > 0) || !(max > 0) || !double.IsFinite(min) || !double.IsFinite(max))
            {
                throw new ChartSpecificationException("Log ticks need a positive finite domain.");
            }

            if (min > max)
            {
                (min, max) = (max, min);
            }

            var low = (int)Math.Floor(Math.Log10(min));
            var high = (int)Math.Ceiling(Math.Log10(max));
            var ticks = Collect(min, max, low, high, [1.0]);

            if (ticks.Count < 3)
            {
                ticks = Collect(min, max, low, high, [1.0, 2.0, 5.0]);
            }

            if (ticks.Count == 0)
            {
                ticks.Add(min);
                ticks.Add(max);
            }

            return ticks;
        }

        private static List<double> Collect(double min, double max, int low, int high, double[] multipliers)
        {
            var ticks = new List<double>();

            for (var exponent = low; exponent <= high; exponent++)
            {
                foreach (var multiplier in multipliers)
                {
                    var value = multiplier * Math.Pow(10, exponent);

                    if (value >= min * (1 - 1e-9) && value <= max * (1 + 1e-9))
                    {
                        ticks.Add(value);
                    }
                }
            }

            return ticks;
        }
    }
}