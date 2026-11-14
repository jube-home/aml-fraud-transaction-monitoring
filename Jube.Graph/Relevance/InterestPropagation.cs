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

using Jube.Graph.Model;

namespace Jube.Graph.Relevance
{
    public static class InterestPropagation
    {
        public static IReadOnlyList<(int Index, double Interest)> Compute(Egonet egonet, int seedIndex,
            IReadOnlyDictionary<int, double>? boosts, int hops = 5, double distanceDecay = 0.7,
            double thresholdRelativeToSeed = 0.1)
        {
            ArgumentNullException.ThrowIfNull(egonet);

            var graph = egonet.Graph;
            var n = graph.NodeCount;

            if (seedIndex < 0 || seedIndex >= n)
            {
                throw new ArgumentOutOfRangeException(nameof(seedIndex));
            }

            if (hops < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hops));
            }

            if (distanceDecay <= 0 || distanceDecay > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceDecay));
            }

            var interest = new double[n];
            var next = new double[n];
            interest[seedIndex] = 1d;
            var weightedDegree = new double[n];

            for (var u = 0; u < n; u++)
            {
                weightedDegree[u] = graph.WeightedDegree(u);
            }

            for (var hop = 0; hop < hops; hop++)
            {
                for (var u = 0; u < n; u++)
                {
                    if (u == seedIndex)
                    {
                        next[u] = 1d;

                        continue;
                    }

                    var sum = 0d;

                    for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                    {
                        sum += graph.Weights[e] * interest[graph.Neighbours[e]];
                    }

                    var mean = weightedDegree[u] > 0 ? sum / weightedDegree[u] : 0d;
                    var boost = boosts != null && boosts.TryGetValue(u, out var b) ? b : 0d;
                    next[u] = Math.Min(1d, distanceDecay * mean + boost);
                }

                (interest, next) = (next, interest);
            }

            var threshold = thresholdRelativeToSeed * interest[seedIndex];

            return Enumerable.Range(0, n)
                .Where(i => i == seedIndex || interest[i] >= threshold && interest[i] > 0)
                .OrderByDescending(i => interest[i])
                .ThenBy(i => egonet.Depth[i])
                .ThenBy(i => i)
                .Select(i => (i, interest[i]))
                .ToArray();
        }
    }
}
