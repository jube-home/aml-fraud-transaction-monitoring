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
    public static class Louvain
    {
        private const int MaximumPasses = 100;
        private const double MinimumGain = 1e-12;

        public static (int[] Community, double Modularity) Communities(CompressedGraph graph, double resolution = 1.0)
        {
            ArgumentNullException.ThrowIfNull(graph);

            if (resolution <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolution));
            }

            var adjacency = SymmetricAdjacency.Build(graph);
            var n = adjacency.NodeCount;
            var membership = Enumerable.Range(0, n).ToArray();

            if (n == 0)
            {
                return (membership, 0d);
            }

            var level = adjacency;
            var levelMembership = membership;
            var totalWeight = TotalWeight(adjacency);

            if (totalWeight <= 0)
            {
                return (membership, 0d);
            }

            while (true)
            {
                var (local, moved) = MoveNodes(level, resolution, totalWeight);
                var relabelled = Relabel(local, out var communityCount);

                for (var i = 0; i < n; i++)
                {
                    membership[i] = relabelled[levelMembership[i]];
                }

                if (!moved || communityCount == level.NodeCount)
                {
                    break;
                }

                level = Aggregate(level, relabelled, communityCount);
                levelMembership = membership.ToArray();
            }

            return (Relabel(membership, out _), Modularity(adjacency, membership, resolution, totalWeight));
        }

        public static double Modularity(CompressedGraph graph, int[] community, double resolution = 1.0)
        {
            ArgumentNullException.ThrowIfNull(graph);
            ArgumentNullException.ThrowIfNull(community);

            var adjacency = SymmetricAdjacency.Build(graph);
            var totalWeight = TotalWeight(adjacency);

            return totalWeight <= 0 ? 0d : Modularity(adjacency, community, resolution, totalWeight);
        }

        private static double TotalWeight(SymmetricAdjacency adjacency)
        {
            var sum = 0d;

            for (var u = 0; u < adjacency.NodeCount; u++)
            {
                sum += adjacency.WeightedDegree(u);
            }

            return sum / 2d;
        }

        private static double Modularity(SymmetricAdjacency adjacency, int[] community, double resolution,
            double totalWeight)
        {
            var count = community.Length == 0 ? 0 : community.Max() + 1;
            var inside = new double[count];
            var total = new double[count];

            for (var u = 0; u < adjacency.NodeCount; u++)
            {
                var c = community[u];
                total[c] += adjacency.WeightedDegree(u);
                inside[c] += adjacency.SelfLoops[u];

                for (var e = adjacency.Offsets[u]; e < adjacency.Offsets[u + 1]; e++)
                {
                    if (community[adjacency.Neighbours[e]] == c)
                    {
                        inside[c] += adjacency.Weights[e];
                    }
                }
            }

            var q = 0d;
            var twoM = 2d * totalWeight;

            for (var c = 0; c < count; c++)
            {
                q += inside[c] / twoM - resolution * (total[c] / twoM) * (total[c] / twoM);
            }

            return q;
        }

        private static (int[] Community, bool Moved) MoveNodes(SymmetricAdjacency adjacency, double resolution,
            double totalWeight)
        {
            var n = adjacency.NodeCount;
            var community = Enumerable.Range(0, n).ToArray();
            var degree = new double[n];
            var communityTotal = new double[n];

            for (var u = 0; u < n; u++)
            {
                degree[u] = adjacency.WeightedDegree(u);
                communityTotal[u] = degree[u];
            }

            var twoM = 2d * totalWeight;
            var movedAny = false;
            var neighbourWeight = new Dictionary<int, double>();

            for (var pass = 0; pass < MaximumPasses; pass++)
            {
                var movedThisPass = false;

                for (var u = 0; u < n; u++)
                {
                    neighbourWeight.Clear();

                    for (var e = adjacency.Offsets[u]; e < adjacency.Offsets[u + 1]; e++)
                    {
                        var c = community[adjacency.Neighbours[e]];
                        neighbourWeight[c] = neighbourWeight.TryGetValue(c, out var existing)
                            ? existing + adjacency.Weights[e]
                            : adjacency.Weights[e];
                    }

                    var current = community[u];
                    communityTotal[current] -= degree[u];
                    var currentLinks = neighbourWeight.GetValueOrDefault(current, 0d);
                    var bestCommunity = current;
                    var bestGain = Gain(currentLinks, communityTotal[current], degree[u], resolution, twoM);

                    foreach (var c in neighbourWeight.Keys.OrderBy(k => k))
                    {
                        if (c == current)
                        {
                            continue;
                        }

                        var gain = Gain(neighbourWeight[c], communityTotal[c], degree[u], resolution, twoM);

                        if (gain > bestGain + MinimumGain)
                        {
                            bestGain = gain;
                            bestCommunity = c;
                        }
                    }

                    communityTotal[bestCommunity] += degree[u];
                    community[u] = bestCommunity;

                    if (bestCommunity != current)
                    {
                        movedThisPass = true;
                        movedAny = true;
                    }
                }

                if (!movedThisPass)
                {
                    break;
                }
            }

            return (community, movedAny);
        }

        private static double Gain(double links, double communityTotal, double degree, double resolution, double twoM)
        {
            return links - resolution * communityTotal * degree / twoM;
        }

        private static int[] Relabel(int[] community, out int count)
        {
            var label = new Dictionary<int, int>();
            var result = new int[community.Length];

            for (var i = 0; i < community.Length; i++)
            {
                if (!label.TryGetValue(community[i], out var l))
                {
                    l = label.Count;
                    label[community[i]] = l;
                }

                result[i] = l;
            }

            count = label.Count;

            return result;
        }

        private static SymmetricAdjacency Aggregate(SymmetricAdjacency adjacency, int[] community, int count)
        {
            var edges = new List<(long From, long To, float Weight, byte Kind)>();
            var selfLoops = new double[count];
            var between = new SortedDictionary<(int, int), double>();

            for (var u = 0; u < adjacency.NodeCount; u++)
            {
                var cu = community[u];
                selfLoops[cu] += adjacency.SelfLoops[u];

                for (var e = adjacency.Offsets[u]; e < adjacency.Offsets[u + 1]; e++)
                {
                    var v = adjacency.Neighbours[e];
                    var cv = community[v];

                    if (cu == cv)
                    {
                        selfLoops[cu] += adjacency.Weights[e];

                        continue;
                    }

                    if (cu < cv)
                    {
                        var key = (cu, cv);
                        between[key] = between.TryGetValue(key, out var existing)
                            ? existing + adjacency.Weights[e]
                            : adjacency.Weights[e];
                    }
                }
            }

            for (var c = 0; c < count; c++)
            {
                edges.Add((c, c, (float)selfLoops[c], 0));
            }

            foreach (var ((a, b), w) in between)
            {
                edges.Add((a, b, (float)w, 0));
            }

            return SymmetricAdjacency.Build(CompressedGraphBuilder.FromEdges(edges, false));
        }
    }
}
