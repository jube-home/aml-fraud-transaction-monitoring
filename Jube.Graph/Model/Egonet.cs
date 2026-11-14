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
    public sealed class Egonet
    {
        public Egonet(CompressedGraph graph, int seedIndex, short[] depth)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(depth);

            if (depth.Length != graph.NodeCount)
            {
                throw new ArgumentException("Depth must have one entry per node.");
            }

            if (seedIndex < 0 || seedIndex >= graph.NodeCount)
            {
                throw new ArgumentOutOfRangeException(nameof(seedIndex));
            }

            Graph = graph;
            SeedIndex = seedIndex;
            Depth = depth;
        }

        public CompressedGraph Graph { get; }

        public int SeedIndex { get; }

        public short[] Depth { get; }

        public static Egonet FromSeed(CompressedGraph graph, int seedIndex)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var depth = new short[graph.NodeCount];
            Array.Fill(depth, (short)-1);
            depth[seedIndex] = 0;
            var queue = new Queue<int>();
            queue.Enqueue(seedIndex);

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();

                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];

                    if (depth[v] >= 0)
                    {
                        continue;
                    }

                    depth[v] = (short)(depth[u] + 1);
                    queue.Enqueue(v);
                }
            }

            return new Egonet(graph, seedIndex, depth);
        }
    }
}
