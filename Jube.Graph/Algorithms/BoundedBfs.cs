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
    public static class BoundedBfs
    {
        public static (short[] Distance, int[] Within1, int[] Within2, int[] Within3) DistancesFrom(
            CompressedGraph graph, IReadOnlyList<int> seedIndices, int cap)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(seedIndices);

            if (cap < 0 || cap > short.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(cap));
            }

            var n = graph.NodeCount;
            var distance = new short[n];
            Array.Fill(distance, (short)-1);
            var within1 = new int[n];
            var within2 = new int[n];
            var within3 = new int[n];
            var seeds = seedIndices.Distinct().OrderBy(i => i).ToArray();
            var queue = new Queue<int>();

            foreach (var s in seeds)
            {
                distance[s] = 0;
                queue.Enqueue(s);
            }

            while (queue.Count > 0)
            {
                var u = queue.Dequeue();

                if (distance[u] >= cap)
                {
                    continue;
                }

                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    var v = graph.Neighbours[e];

                    if (distance[v] >= 0)
                    {
                        continue;
                    }

                    distance[v] = (short)(distance[u] + 1);
                    queue.Enqueue(v);
                }
            }

            var local = new short[n];
            var touched = new List<int>();
            var limit = Math.Min(cap, 3);

            foreach (var s in seeds)
            {
                touched.Clear();
                local[s] = 1;
                touched.Add(s);
                queue.Enqueue(s);

                while (queue.Count > 0)
                {
                    var u = queue.Dequeue();
                    var hops = local[u];

                    if (hops - 1 >= limit)
                    {
                        continue;
                    }

                    for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                    {
                        var v = graph.Neighbours[e];

                        if (local[v] > 0)
                        {
                            continue;
                        }

                        local[v] = (short)(hops + 1);
                        touched.Add(v);
                        queue.Enqueue(v);
                    }
                }

                foreach (var v in touched)
                {
                    var hops = local[v] - 1;

                    if (hops == 1)
                    {
                        within1[v]++;
                    }

                    if (hops is >= 1 and <= 2)
                    {
                        within2[v]++;
                    }

                    if (hops is >= 1 and <= 3)
                    {
                        within3[v]++;
                    }

                    local[v] = 0;
                }
            }

            return (distance, within1, within2, within3);
        }
    }
}
