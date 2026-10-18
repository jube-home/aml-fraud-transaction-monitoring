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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal static class CategoricalNumerics
    {
        public static double Median(double[] sorted)
        {
            var n = sorted.Length;

            return n % 2 == 1 ? sorted[n / 2] : (sorted[n / 2 - 1] + sorted[n / 2]) / 2d;
        }

        public static double Quantile(double[] sorted, double q)
        {
            var position = q * (sorted.Length - 1);
            var lower = (int)Math.Floor(position);
            var upper = Math.Min(lower + 1, sorted.Length - 1);

            return sorted[lower] + (position - lower) * (sorted[upper] - sorted[lower]);
        }

        public static double[] AverageRanks(double[] values)
        {
            var order = Enumerable.Range(0, values.Length).OrderBy(i => values[i]).ToArray();
            var ranks = new double[values.Length];
            var start = 0;

            while (start < order.Length)
            {
                var end = start;

                while (end + 1 < order.Length && Math.Abs(values[order[end + 1]] - values[order[start]]) < 0.0001)
                {
                    end++;
                }

                var rank = (start + end) / 2d + 1d;

                for (var k = start; k <= end; k++)
                {
                    ranks[order[k]] = rank;
                }

                start = end + 1;
            }

            return ranks;
        }
    }
}