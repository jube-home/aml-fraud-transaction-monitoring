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
    public static class JohnsonCycles
    {
        public static IReadOnlyList<int[]> Enumerate(CompressedGraph directedGraph, int maxLength)
        {
            ArgumentNullException.ThrowIfNull(directedGraph);

            if (maxLength < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            var n = directedGraph.NodeCount;
            var cycles = new List<int[]>();
            var onPath = new bool[n];
            var path = new List<int>();

            for (var start = 0; start < n; start++)
            {
                path.Add(start);
                onPath[start] = true;
                Extend(directedGraph, start, start, maxLength, onPath, path, cycles);
                onPath[start] = false;
                path.RemoveAt(path.Count - 1);
            }

            return cycles;
        }

        private static void Extend(CompressedGraph graph, int start, int current, int maxLength, bool[] onPath,
            List<int> path, List<int[]> cycles)
        {
            for (var e = graph.Offsets[current]; e < graph.Offsets[current + 1]; e++)
            {
                var next = graph.Neighbours[e];

                if (next == start)
                {
                    if (path.Count >= 2)
                    {
                        cycles.Add(path.ToArray());
                    }

                    continue;
                }

                if (next < start || onPath[next] || path.Count >= maxLength)
                {
                    continue;
                }

                onPath[next] = true;
                path.Add(next);
                Extend(graph, start, next, maxLength, onPath, path, cycles);
                path.RemoveAt(path.Count - 1);
                onPath[next] = false;
            }
        }
    }
}
