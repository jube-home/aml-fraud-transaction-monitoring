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
    public static class Triangles
    {
        public static (int[] Triangles, double[] Clustering) Count(CompressedGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var adjacency = SymmetricAdjacency.Build(graph);
            var n = adjacency.NodeCount;
            var triangles = new int[n];

            for (var u = 0; u < n; u++)
            {
                for (var e = adjacency.Offsets[u]; e < adjacency.Offsets[u + 1]; e++)
                {
                    var v = adjacency.Neighbours[e];

                    if (v <= u)
                    {
                        continue;
                    }

                    var i = adjacency.Offsets[u];
                    var j = adjacency.Offsets[v];
                    var endU = adjacency.Offsets[u + 1];
                    var endV = adjacency.Offsets[v + 1];

                    while (i < endU && j < endV)
                    {
                        var a = adjacency.Neighbours[i];
                        var b = adjacency.Neighbours[j];

                        if (a < b)
                        {
                            i++;
                        }
                        else if (b < a)
                        {
                            j++;
                        }
                        else
                        {
                            if (a > v)
                            {
                                triangles[u]++;
                                triangles[v]++;
                                triangles[a]++;
                            }

                            i++;
                            j++;
                        }
                    }
                }
            }

            var clustering = new double[n];

            for (var u = 0; u < n; u++)
            {
                var d = adjacency.Degree(u);
                clustering[u] = d < 2 ? 0d : 2d * triangles[u] / (d * (double)(d - 1));
            }

            return (triangles, clustering);
        }
    }
}
