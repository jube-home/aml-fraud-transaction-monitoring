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

namespace Jube.Graph.Algorithms
{
    public static class GreedyPeeling
    {
        private const double DefaultColumnOffset = 5d;

        public static (int[] Nodes, double Density) DensestBlock(CompressedGraph graph,
            Func<int, double>? columnWeight = null)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var n = graph.NodeCount;

            if (n == 0)
            {
                return ([], 0d);
            }

            var inOffsets = new int[n + 1];

            for (var e = 0; e < graph.EdgeCount; e++)
            {
                inOffsets[graph.Neighbours[e] + 1]++;
            }

            for (var i = 0; i < n; i++)
            {
                inOffsets[i + 1] += inOffsets[i];
            }

            var position = new int[n];
            Array.Copy(inOffsets, position, n);
            var inSources = new int[graph.EdgeCount];
            var inWeights = new float[graph.EdgeCount];

            for (var u = 0; u < n; u++)
            {
                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var p = position[graph.Neighbours[e]]++;
                    inSources[p] = u;
                    inWeights[p] = graph.Weights[e];
                }
            }

            columnWeight ??= j => 1d / Math.Log(inOffsets[j + 1] - inOffsets[j] + DefaultColumnOffset);
            var column = new double[n];

            for (var j = 0; j < n; j++)
            {
                column[j] = columnWeight(j);
            }

            var priority = new double[n];
            var total = 0d;

            for (var u = 0; u < n; u++)
            {
                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];
                    var w = graph.Weights[e] * column[v];
                    priority[u] += w;
                    total += w;

                    if (v != u)
                    {
                        priority[v] += w;
                    }
                }
            }

            var live = new SortedSet<int>(Comparer<int>.Create((a, b) =>
            {
                var c = priority[a].CompareTo(priority[b]);

                return c != 0 ? c : a.CompareTo(b);
            }));

            for (var u = 0; u < n; u++)
            {
                live.Add(u);
            }

            var removed = new bool[n];
            var order = new int[n];
            var remaining = n;
            var bestDensity = total / n;
            var bestRemaining = n;
            var step = 0;

            while (remaining > 1)
            {
                var u = live.Min;
                live.Remove(u);
                removed[u] = true;
                order[step++] = u;
                remaining--;
                total -= priority[u];

                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];

                    if (!removed[v])
                    {
                        Adjust(live, priority, v, graph.Weights[e] * column[v]);
                    }
                }

                for (var e = inOffsets[u]; e < inOffsets[u + 1]; e++)
                {
                    var v = inSources[e];

                    if (!removed[v])
                    {
                        Adjust(live, priority, v, inWeights[e] * column[u]);
                    }
                }

                var density = total / remaining;

                if (density > bestDensity + 1e-12)
                {
                    bestDensity = density;
                    bestRemaining = remaining;
                }
            }

            var kept = new bool[n];
            Array.Fill(kept, true);

            for (var i = 0; i < n - bestRemaining; i++)
            {
                kept[order[i]] = false;
            }

            return (Enumerable.Range(0, n).Where(i => kept[i]).ToArray(), bestDensity);
        }

        private static void Adjust(SortedSet<int> live, double[] priority, int v, double delta)
        {
            live.Remove(v);
            priority[v] -= delta;
            live.Add(v);
        }
    }
}
