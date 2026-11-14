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

namespace Jube.Graph.Model
{
    public static class CompressedGraphBuilder
    {
        public static CompressedGraph FromEdges(IEnumerable<(long From, long To, float Weight, byte Kind)> edges,
            bool undirected)
        {
            ArgumentNullException.ThrowIfNull(edges);

            var indexByNodeId = new Dictionary<long, int>();
            var nodeIds = new List<long>();
            var merged = new Dictionary<(int From, int To, byte Kind), int>();
            var from = new List<int>();
            var to = new List<int>();
            var weight = new List<float>();
            var kind = new List<byte>();

            foreach (var edge in edges)
            {
                var f = Index(edge.From, indexByNodeId, nodeIds);
                var t = Index(edge.To, indexByNodeId, nodeIds);

                Add(f, t, edge.Weight, edge.Kind, merged, from, to, weight, kind);

                if (undirected && f != t)
                {
                    Add(t, f, edge.Weight, edge.Kind, merged, from, to, weight, kind);
                }
            }

            var n = nodeIds.Count;
            var offsets = new int[n + 1];

            for (var e = 0; e < from.Count; e++)
            {
                offsets[from[e] + 1]++;
            }

            for (var i = 0; i < n; i++)
            {
                offsets[i + 1] += offsets[i];
            }

            var position = new int[n];
            Array.Copy(offsets, position, n);
            var neighbours = new int[from.Count];
            var weights = new float[from.Count];
            var kinds = new byte[from.Count];

            for (var e = 0; e < from.Count; e++)
            {
                var p = position[from[e]]++;
                neighbours[p] = to[e];
                weights[p] = weight[e];
                kinds[p] = kind[e];
            }

            return new CompressedGraph(offsets, neighbours, weights, kinds, nodeIds.ToArray());
        }

        private static int Index(long nodeId, Dictionary<long, int> indexByNodeId, List<long> nodeIds)
        {
            if (indexByNodeId.TryGetValue(nodeId, out var index))
            {
                return index;
            }

            index = nodeIds.Count;
            nodeIds.Add(nodeId);
            indexByNodeId[nodeId] = index;

            return index;
        }

        private static void Add(int f, int t, float w, byte k, Dictionary<(int From, int To, byte Kind), int> merged,
            List<int> from, List<int> to, List<float> weight, List<byte> kind)
        {
            if (merged.TryGetValue((f, t, k), out var existing))
            {
                weight[existing] += w;

                return;
            }

            merged[(f, t, k)] = from.Count;
            from.Add(f);
            to.Add(t);
            weight.Add(w);
            kind.Add(k);
        }
    }
}
