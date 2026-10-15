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
    public enum CalibrationBinning
    {
        UniformWidth = 0,
        EqualCount = 1
    }

    internal static class CalibrationMath
    {
        public static void Clean(double[] probabilities, int[] labels, out double[] p, out int[] y, out int skipped)
        {
            ScoreGuards.Pairs(probabilities, labels, out p, out y, out skipped);
            ScoreGuards.Probabilities(p);
        }

        public static List<int[]> EqualCountGroups(double[] p, int groups)
        {
            var order = Enumerable.Range(0, p.Length).OrderBy(i => p[i]).ThenBy(i => i).ToArray();
            var result = new List<int[]>();
            var position = 0;

            for (var g = 0; g < groups; g++)
            {
                var size = p.Length / groups + (g < p.Length % groups ? 1 : 0);

                if (size == 0)
                {
                    continue;
                }

                result.Add(order.Skip(position).Take(size).ToArray());
                position += size;
            }

            return result;
        }
    }
}