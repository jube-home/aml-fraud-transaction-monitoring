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
    public sealed class CompressedGraph
    {
        private readonly Dictionary<long, int> indexByNodeId;

        public CompressedGraph(int[] offsets, int[] neighbours, float[] weights, byte[] kinds, long[] nodeIds)
        {
            ArgumentNullException.ThrowIfNull(offsets);
            ArgumentNullException.ThrowIfNull(neighbours);
            ArgumentNullException.ThrowIfNull(weights);
            ArgumentNullException.ThrowIfNull(kinds);
            ArgumentNullException.ThrowIfNull(nodeIds);

            if (offsets.Length != nodeIds.Length + 1)
            {
                throw new ArgumentException("Offsets must have one more entry than there are nodes.");
            }

            if (neighbours.Length != weights.Length || neighbours.Length != kinds.Length)
            {
                throw new ArgumentException("Neighbours, weights and kinds must have the same length.");
            }

            if (offsets[^1] != neighbours.Length)
            {
                throw new ArgumentException("The last offset must equal the number of edges.");
            }

            Offsets = offsets;
            Neighbours = neighbours;
            Weights = weights;
            Kinds = kinds;
            NodeIds = nodeIds;
            indexByNodeId = new Dictionary<long, int>(nodeIds.Length);

            for (var i = 0; i < nodeIds.Length; i++)
            {
                indexByNodeId[nodeIds[i]] = i;
            }
        }

        public int[] Offsets { get; }

        public int[] Neighbours { get; }

        public float[] Weights { get; }

        public byte[] Kinds { get; }

        public long[] NodeIds { get; }

        public int NodeCount => NodeIds.Length;

        public int EdgeCount => Neighbours.Length;

        public int Degree(int index)
        {
            return Offsets[index + 1] - Offsets[index];
        }

        public double WeightedDegree(int index)
        {
            var sum = 0d;

            for (var e = Offsets[index]; e < Offsets[index + 1]; e++)
            {
                sum += Weights[e];
            }

            return sum;
        }

        public bool TryGetIndex(long nodeId, out int index)
        {
            return indexByNodeId.TryGetValue(nodeId, out index);
        }

        public int IndexOf(long nodeId)
        {
            return indexByNodeId.TryGetValue(nodeId, out var index)
                ? index
                : throw new KeyNotFoundException($"Node {nodeId} is not in the graph.");
        }
    }
}
