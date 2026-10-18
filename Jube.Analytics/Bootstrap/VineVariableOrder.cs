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

namespace Jube.Analytics.Bootstrap
{
    internal static class VineVariableOrder
    {
        public static int[] GreedyPathByAbsoluteTau(double[][] columns)
        {
            var d = columns.Length;

            if (d <= 2)
            {
                return Enumerable.Range(0, d).ToArray();
            }

            var tau = new double[d, d];

            for (var i = 0; i < d; i++)
            {
                for (var j = i + 1; j < d; j++)
                {
                    var value = Math.Abs(KendallTau.Compute(columns[i], columns[j]));
                    tau[i, j] = value;
                    tau[j, i] = value;
                }
            }

            var bestI = 0;
            var bestJ = 1;

            for (var i = 0; i < d; i++)
            {
                for (var j = i + 1; j < d; j++)
                {
                    if (tau[i, j] > tau[bestI, bestJ])
                    {
                        bestI = i;
                        bestJ = j;
                    }
                }
            }

            var path = new List<int> { bestI, bestJ };
            var remaining = new HashSet<int>(Enumerable.Range(0, d));
            remaining.Remove(bestI);
            remaining.Remove(bestJ);

            while (remaining.Count > 0)
            {
                var front = path[0];
                var back = path[^1];
                var bestCandidate = -1;
                var bestScore = -1d;
                var prependToFront = true;

                foreach (var candidate in remaining)
                {
                    var frontScore = tau[front, candidate];

                    if (frontScore > bestScore)
                    {
                        bestScore = frontScore;
                        bestCandidate = candidate;
                        prependToFront = true;
                    }

                    var backScore = tau[back, candidate];

                    if (backScore > bestScore)
                    {
                        bestScore = backScore;
                        bestCandidate = candidate;
                        prependToFront = false;
                    }
                }

                if (prependToFront)
                {
                    path.Insert(0, bestCandidate);
                }
                else
                {
                    path.Add(bestCandidate);
                }

                remaining.Remove(bestCandidate);
            }

            return path.ToArray();
        }
    }
}