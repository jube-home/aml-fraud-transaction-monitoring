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

using System.Linq;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class PersonalisedPageRankTests
    {
        private static CompressedGraph Path()
        {
            return CompressedGraphBuilder.FromEdges(
                Enumerable.Range(0, 7).Select(i => ((long)i, (long)(i + 1), 1f, (byte)0)), true);
        }

        [Fact]
        public void SumsToAboutOneAndRanksTheNeighbourAboveAFarNode()
        {
            var graph = Path();

            var rank = PageRank.Personalised(graph, Seeds.Bad(0), epsilon: 1e-9);

            rank.Sum().Should().BeApproximately(1d, 1e-6);
            rank[0].Should().BeGreaterThan(rank[7]);
            rank[1].Should().BeGreaterThan(rank[7]);
            rank[1].Should().BeGreaterThan(rank[4]);
            rank.Should().OnlyContain(r => r >= 0);
        }

        [Fact]
        public void DanglingMassReturnsToTheSeeds()
        {
            var graph = CompressedGraphBuilder.FromEdges([(0L, 1L, 1f, (byte)0), (1L, 2L, 1f, (byte)0)], false);

            var rank = PageRank.Personalised(graph, Seeds.Bad(0), epsilon: 1e-10);

            rank.Sum().Should().BeApproximately(1d, 1e-6);
            rank[0].Should().BeGreaterThan(rank[2]);
        }

        [Fact]
        public void GlobalPageRankHandlesDanglingNodes()
        {
            var graph = CompressedGraphBuilder.FromEdges([(0L, 1L, 1f, (byte)0), (1L, 2L, 1f, (byte)0)], false);

            var rank = PageRank.Compute(graph);

            rank.Sum().Should().BeApproximately(1d, 1e-9);
            rank[2].Should().BeGreaterThan(rank[1]);
            rank[1].Should().BeGreaterThan(rank[0]);
        }
    }
}
