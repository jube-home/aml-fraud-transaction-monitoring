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
using Jube.Charting.Diagrams.Models;

namespace Jube.Charting.Diagrams
{
    public static class RadialLayout
    {
        public const int MaxGrowth = 40;

        public static GraphLayoutResult Compute(GraphDiagramSpec spec, Dictionary<string, int> index)
        {
            var n = spec.Nodes.Count;
            var sizes = NetworkGeometry.Sizes(spec, index);
            var edges = NetworkGeometry.Edges(spec, index);
            var adjacency = NetworkGeometry.Adjacency(n, edges);
            var seed = spec.SeedId is { } id && index.TryGetValue(id, out var s) ? s : 0;
            var depth = Depths(spec, index, adjacency, seed);
            var parent = Parents(n, adjacency, depth);
            var subtree = Subtrees(n, parent, depth);
            var groupRank = GroupRanks(spec);
            var children = new List<int>[n];
            var roots = new List<int>();

            for (var i = 0; i < n; i++)
            {
                children[i] = [];
            }

            for (var i = 0; i < n; i++)
            {
                if (i == seed)
                {
                    continue;
                }

                if (parent[i] >= 0)
                {
                    children[parent[i]].Add(i);
                }
                else
                {
                    roots.Add(i);
                }
            }

            int Rank(int i)
            {
                return groupRank.TryGetValue(spec.Nodes[i].Group ?? string.Empty, out var r) ? r : int.MaxValue;
            }

            List<int> Ordered(IEnumerable<int> items)
            {
                return [.. items.OrderBy(Rank).ThenBy(i => spec.Nodes[i].Label, StringComparer.Ordinal).ThenBy(i => i)];
            }

            var angle = new double[n];
            angle[seed] = -Math.PI / 2;

            void Spread(IEnumerable<int> items, double from, double total)
            {
                var list = Ordered(items);
                var weight = list.Sum(i => (double)subtree[i]);
                var cursor = from;

                foreach (var i in list)
                {
                    var share = total * subtree[i] / Math.Max(weight, 1);
                    angle[i] = cursor + share / 2;
                    cursor += share;
                    Spread(children[i], angle[i] - share / 2, share);
                }
            }

            var rootWeight = roots.Sum(i => (double)subtree[i]);
            var topWeight = children[seed].Sum(i => (double)subtree[i]);
            var rootShare = rootWeight > 0 ? 2 * Math.PI * rootWeight / (rootWeight + topWeight) : 0;
            Spread(children[seed], -Math.PI / 2, 2 * Math.PI - rootShare);
            Spread(roots, -Math.PI / 2 + 2 * Math.PI - rootShare, rootShare);

            var maxDiameter = sizes.Max(z => Math.Max(z.W, z.H));
            var gap = maxDiameter + 64;
            var centres = new (double X, double Y)[n];

            for (var attempt = 0; attempt <= MaxGrowth; attempt++)
            {
                for (var i = 0; i < n; i++)
                {
                    var r = depth[i] * gap;
                    centres[i] = (r * Math.Cos(angle[i]), r * Math.Sin(angle[i]));
                }

                if (!Collides(centres, sizes, depth) || attempt == MaxGrowth)
                {
                    break;
                }

                gap *= 1.2;
            }

            NetworkGeometry.Separate(centres, sizes, new bool[n], 8, NetworkGeometry.LabelHeight + 4);

            if (NetworkGeometry.FitExtent(centres, sizes, spec.MaxExtent))
            {
                NetworkGeometry.Separate(centres, sizes, new bool[n], 8, NetworkGeometry.LabelHeight + 4);
            }

            var (nodes, width, height) = NetworkGeometry.Place(spec, centres, sizes, depth, 0, 0);
            var links = NetworkGeometry.Links(nodes, edges);

            return new GraphLayoutResult(nodes, links, width, height, GraphLayoutKind.Radial, depth.Max() + 1, 0);
        }

        private static int[] Depths(GraphDiagramSpec spec, Dictionary<string, int> index, List<int>[] adjacency,
            int seed)
        {
            var n = spec.Nodes.Count;
            var hops = NetworkGeometry.Distances(n, adjacency, n)[seed];
            var depth = new int[n];
            var farthest = hops.Where(h => h <= n).DefaultIfEmpty(0).Max();

            for (var i = 0; i < n; i++)
            {
                depth[i] = hops[i] > n ? farthest + 1 : hops[i];
            }

            if (spec.Depths is { } given)
            {
                foreach (var pair in given)
                {
                    if (index.TryGetValue(pair.Key, out var i) && pair.Value >= 0)
                    {
                        depth[i] = pair.Value;
                    }
                }
            }

            depth[seed] = 0;

            return depth;
        }

        private static int[] Parents(int n, List<int>[] adjacency, int[] depth)
        {
            var parent = new int[n];

            for (var i = 0; i < n; i++)
            {
                parent[i] = depth[i] == 0 ? -1 : adjacency[i].FirstOrDefault(j => depth[j] == depth[i] - 1, -1);
            }

            return parent;
        }

        private static int[] Subtrees(int n, int[] parent, int[] depth)
        {
            var size = new int[n];
            Array.Fill(size, 1);

            foreach (var i in Enumerable.Range(0, n).OrderByDescending(i => depth[i]).ThenBy(i => i))
            {
                if (parent[i] >= 0)
                {
                    size[parent[i]] += size[i];
                }
            }

            return size;
        }

        private static Dictionary<string, int> GroupRanks(GraphDiagramSpec spec)
        {
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var name in spec.GroupOrder)
            {
                ranks.TryAdd(name, ranks.Count);
            }

            foreach (var node in spec.Nodes)
            {
                ranks.TryAdd(node.Group ?? string.Empty, ranks.Count);
            }

            return ranks;
        }

        private static bool Collides((double X, double Y)[] centres, (double W, double H)[] sizes, int[] depth)
        {
            for (var i = 0; i < centres.Length; i++)
            {
                for (var j = i + 1; j < centres.Length; j++)
                {
                    if (depth[i] != depth[j] || depth[i] == 0)
                    {
                        continue;
                    }

                    var dx = centres[i].X - centres[j].X;
                    var dy = centres[i].Y - centres[j].Y;
                    var need = (Math.Max(sizes[i].W, sizes[i].H) + Math.Max(sizes[j].W, sizes[j].H)) / 2 + 10;

                    if (dx * dx + dy * dy < need * need)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
