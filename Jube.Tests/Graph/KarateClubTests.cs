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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class KarateClubTests
    {
        [Fact]
        public void GraphHasThirtyFourNodesAndSeventyEightUndirectedEdges()
        {
            var graph = KarateClub.Graph();

            graph.NodeCount.Should().Be(34);
            graph.EdgeCount.Should().Be(156);
            KarateClub.Edges.Should().HaveCount(78);
        }

        [Fact]
        public void UnionFindGivesOneComponent()
        {
            var graph = KarateClub.Graph();

            var (component, sizes) = UnionFind.Components(graph);

            sizes.Should().Equal(34);
            component.Should().OnlyContain(c => c == 0);
        }

        [Fact]
        public void LouvainModularityIsInTheKnownRange()
        {
            var graph = KarateClub.Graph();

            var (community, modularity) = Louvain.Communities(graph);

            modularity.Should().BeInRange(0.40, 0.42);
            Louvain.Modularity(graph, community).Should().BeApproximately(modularity, 1e-9);
            community.Max().Should().BeInRange(2, 4);
        }

        [Fact]
        public void LouvainRecoversTheTwoFactions()
        {
            var graph = KarateClub.Graph();
            var (community, _) = Louvain.Communities(graph);
            var communities = community.Distinct().OrderBy(c => c).ToArray();
            var majority = new Dictionary<int, bool>();

            foreach (var c in communities)
            {
                var members = Enumerable.Range(0, graph.NodeCount).Where(i => community[i] == c)
                    .Select(i => (int)graph.NodeIds[i]).ToArray();
                var hi = members.Count(KarateClub.MrHi.Contains);
                var purity = Math.Max(hi, members.Length - hi) / (double)members.Length;

                purity.Should().BeGreaterThanOrEqualTo(0.8, $"community {c} should be mostly one faction");
                majority[c] = hi * 2 >= members.Length;
            }

            majority.Values.Should().Contain(true);
            majority.Values.Should().Contain(false);
            Connected(graph, Enumerable.Range(0, graph.NodeCount).Where(i => majority[community[i]]).ToArray())
                .Should().BeTrue();
            Connected(graph, Enumerable.Range(0, graph.NodeCount).Where(i => !majority[community[i]]).ToArray())
                .Should().BeTrue();
        }

        [Fact]
        public void PageRankRanksThirtyFourFirstAndOneSecond()
        {
            var graph = KarateClub.Graph();

            var rank = PageRank.Compute(graph);

            rank.Sum().Should().BeApproximately(1d, 1e-9);
            var order = Enumerable.Range(0, graph.NodeCount).OrderByDescending(i => rank[i])
                .Select(i => graph.NodeIds[i]).ToArray();
            order[0].Should().Be(34);
            order[1].Should().Be(1);
        }

        [Fact]
        public void MaximumCoreNumberIsFour()
        {
            var graph = KarateClub.Graph();

            var cores = KCore.CoreNumbers(graph);

            cores.Max().Should().Be(4);
            cores[KarateClub.Index(graph, 12)].Should().Be(1);
            cores[KarateClub.Index(graph, 1)].Should().Be(4);
            cores[KarateClub.Index(graph, 34)].Should().Be(4);
        }

        private static bool Connected(CompressedGraph graph, int[] nodes)
        {
            var set = nodes.ToHashSet();
            var sets = new UnionFind(graph.NodeCount);

            foreach (var u in nodes)
            {
                for (var e = graph.Offsets[u]; e < graph.Offsets[u + 1]; e++)
                {
                    if (set.Contains(graph.Neighbours[e]))
                    {
                        sets.Union(u, graph.Neighbours[e]);
                    }
                }
            }

            return nodes.Select(sets.Find).Distinct().Count() == 1;
        }
    }
}
