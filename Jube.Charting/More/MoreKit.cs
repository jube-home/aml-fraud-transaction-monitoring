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

using Jube.Charting.Text;
using Jube.Charting.Ticks;
using Jube.Charting.Core.Models;

namespace Jube.Charting.More
{
    internal static class MoreKit
    {
        public static ChartOptions AutoLegend(ChartOptions options)
        {
            return options.LegendMaxRows == 0 ? options with { LegendMaxRows = -1 } : options;
        }

        public static List<T> Thin<T>(IReadOnlyList<T> items, int maximum)
        {
            if (items.Count <= maximum)
            {
                return [.. items];
            }

            var result = new List<T>(maximum);

            for (var i = 0; i < maximum; i++)
            {
                result.Add(items[(int)Math.Round((double)i * (items.Count - 1) / (maximum - 1))]);
            }

            return result;
        }

        public static string Names(IReadOnlyList<string> names, int max = 6)
        {
            return string.Join(", ", names.Take(max).Select(n => TextMetrics.Truncate(n, 200, 12)));
        }

        public static (double Min, double Max) NiceDomain(double min, double max, int target = 5)
        {
            var ticks = ExtendedTicks.Generate(min, max, target, true);

            return (ticks[0], ticks[^1]);
        }
    }
}