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
    internal sealed class SymmetricAdjacency
    {
        private SymmetricAdjacency(int[] offsets, int[] neighbours, double[] weights, double[] selfLoops)
        {
            Offsets = offsets;
            Neighbours = neighbours;
            Weights = weights;
            SelfLoops = selfLoops;
        }

        public int[] Offsets { get; }

        public int[] Neighbours { get; }

        public double[] Weights { get; }

        public double[] SelfLoops { get; }

        public int NodeCount => Offsets.Length - 1;

        public static SymmetricAdjacency Build(CompressedGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var n = graph.NodeCount;
            var rows = new SortedDictionary<int, double>[n];
            var selfLoops = new double[n];

            for (var u = 0; u < n; u++)
            {
                rows[u] = new SortedDictionary<int, double>();
            }

            for (var u = 0; u < n; u++)
            {
                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];
                    double w = graph.Weights[e];

                    if (u == v)
                    {
                        selfLoops[u] += w;

                        continue;
                    }

                    Accumulate(rows[u], v, w);
                    Accumulate(rows[v], u, w);
                }
            }

            var offsets = new int[n + 1];

            for (var u = 0; u < n; u++)
            {
                offsets[u + 1] = offsets[u] + rows[u].Count;
            }

            var neighbours = new int[offsets[n]];
            var weights = new double[offsets[n]];

            for (var u = 0; u < n; u++)
            {
                var p = offsets[u];

                foreach (var (v, w) in rows[u])
                {
                    neighbours[p] = v;
                    weights[p] = w;
                    p++;
                }
            }

            return new SymmetricAdjacency(offsets, neighbours, weights, selfLoops);
        }

        public int Degree(int u)
        {
            return Offsets[u + 1] - Offsets[u];
        }

        public double WeightedDegree(int u)
        {
            var sum = SelfLoops[u];

            for (var e = Offsets[u]; e < Offsets[u + 1]; e++)
            {
                sum += Weights[e];
            }

            return sum;
        }

        private static void Accumulate(SortedDictionary<int, double> row, int v, double w)
        {
            row[v] = row.TryGetValue(v, out var existing) ? existing + w : w;
        }
    }
}
