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

using Jube.Analytics;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal static class ThresholdSelection
    {
        public static double MaximiseF1(double[] probabilities, int[] labels)
        {
            var candidates = probabilities.Distinct().OrderBy(v => v).ToArray();

            if (candidates.Length > 200)
            {
                candidates = Enumerable.Range(0, 200)
                    .Select(i => candidates[(int)((candidates.Length - 1) * i / 199d)]).Distinct().ToArray();
            }

            var best = 0.5;
            var bestF1 = -1d;

            foreach (var candidate in candidates)
            {
                var f1 = BinaryMetrics.Confusion(probabilities, labels, candidate).F1;

                if (double.IsFinite(f1) && f1 > bestF1)
                {
                    bestF1 = f1;
                    best = candidate;
                }
            }

            return best;
        }
    }
}