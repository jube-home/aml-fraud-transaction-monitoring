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
    public static class KCore
    {
        public static int[] CoreNumbers(CompressedGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var adjacency = SymmetricAdjacency.Build(graph);
            var n = adjacency.NodeCount;
            var degree = new int[n];
            var maximumDegree = 0;

            for (var u = 0; u < n; u++)
            {
                degree[u] = adjacency.Degree(u);
                maximumDegree = Math.Max(maximumDegree, degree[u]);
            }

            var bin = new int[maximumDegree + 1];

            for (var u = 0; u < n; u++)
            {
                bin[degree[u]]++;
            }

            var start = 0;

            for (var d = 0; d <= maximumDegree; d++)
            {
                var size = bin[d];
                bin[d] = start;
                start += size;
            }

            var position = new int[n];
            var vertices = new int[n];

            for (var u = 0; u < n; u++)
            {
                position[u] = bin[degree[u]];
                vertices[position[u]] = u;
                bin[degree[u]]++;
            }

            for (var d = maximumDegree; d >= 1; d--)
            {
                bin[d] = bin[d - 1];
            }

            if (maximumDegree >= 0 && bin.Length > 0)
            {
                bin[0] = 0;
            }

            for (var i = 0; i < n; i++)
            {
                var v = vertices[i];

                for (var e = adjacency.Offsets[v]; e < adjacency.Offsets[v + 1]; e++)
                {
                    var u = adjacency.Neighbours[e];

                    if (degree[u] <= degree[v])
                    {
                        continue;
                    }

                    var du = degree[u];
                    var pu = position[u];
                    var pw = bin[du];
                    var w = vertices[pw];

                    if (u != w)
                    {
                        position[u] = pw;
                        vertices[pu] = w;
                        position[w] = pu;
                        vertices[pw] = u;
                    }

                    bin[du]++;
                    degree[u]--;
                }
            }

            return degree;
        }
    }
}
