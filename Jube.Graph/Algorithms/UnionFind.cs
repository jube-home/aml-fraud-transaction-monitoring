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
    public sealed class UnionFind
    {
        private readonly int[] parent;
        private readonly int[] rank;

        public UnionFind(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            parent = new int[count];
            rank = new int[count];

            for (var i = 0; i < count; i++)
            {
                parent[i] = i;
            }
        }

        public int Count => parent.Length;

        public static (int[] Component, int[] Sizes) Components(CompressedGraph graph)
        {
            ArgumentNullException.ThrowIfNull(graph);

            var n = graph.NodeCount;
            var sets = new UnionFind(n);

            for (var u = 0; u < n; u++)
            {
                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    sets.Union(u, graph.Neighbours[e]);
                }
            }

            var component = new int[n];
            var labelByRoot = new Dictionary<int, int>();
            var sizes = new List<int>();

            for (var u = 0; u < n; u++)
            {
                var root = sets.Find(u);

                if (!labelByRoot.TryGetValue(root, out var label))
                {
                    label = sizes.Count;
                    labelByRoot[root] = label;
                    sizes.Add(0);
                }

                component[u] = label;
                sizes[label]++;
            }

            return (component, sizes.ToArray());
        }

        public int Find(int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        public bool Union(int a, int b)
        {
            var ra = Find(a);
            var rb = Find(b);

            if (ra == rb)
            {
                return false;
            }

            if (rank[ra] < rank[rb])
            {
                (ra, rb) = (rb, ra);
            }

            parent[rb] = ra;

            if (rank[ra] == rank[rb])
            {
                rank[ra]++;
            }

            return true;
        }

        public bool Connected(int a, int b)
        {
            return Find(a) == Find(b);
        }
    }
}
