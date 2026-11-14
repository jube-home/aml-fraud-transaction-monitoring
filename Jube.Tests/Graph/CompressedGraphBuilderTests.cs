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

using FluentAssertions;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class CompressedGraphBuilderTests
    {
        [Fact]
        public void MapsExternalIdsToDenseIndicesInFirstSeenOrder()
        {
            var graph = CompressedGraphBuilder.FromEdges(
                [(500L, 7L, 1f, (byte)1), (7L, 42L, 2f, (byte)2), (42L, 500L, 3f, (byte)1)], false);

            graph.NodeIds.Should().Equal(500L, 7L, 42L);
            graph.Offsets.Should().Equal(0, 1, 2, 3);
            graph.Neighbours.Should().Equal(1, 2, 0);
            graph.Weights.Should().Equal(1f, 2f, 3f);
            graph.Kinds.Should().Equal((byte)1, (byte)2, (byte)1);
            graph.IndexOf(42L).Should().Be(2);
            graph.TryGetIndex(99L, out _).Should().BeFalse();
        }

        [Fact]
        public void UndirectedAddsBothDirectionsAndMergesParallelEdges()
        {
            var graph = CompressedGraphBuilder.FromEdges(
                [(1L, 2L, 1f, (byte)0), (1L, 2L, 2f, (byte)0), (2L, 2L, 5f, (byte)0)], true);

            graph.NodeCount.Should().Be(2);
            graph.Degree(0).Should().Be(1);
            graph.Degree(1).Should().Be(2);
            graph.Weights[graph.Offsets[0]].Should().Be(3f);
            graph.WeightedDegree(1).Should().Be(8d);
        }

        [Fact]
        public void EgonetFromSeedRecordsDepth()
        {
            var graph = CompressedGraphBuilder.FromEdges(
                [(1L, 2L, 1f, (byte)0), (2L, 3L, 1f, (byte)0), (9L, 9L, 1f, (byte)0)], true);

            var egonet = Egonet.FromSeed(graph, 0);

            egonet.Depth.Should().Equal(0, 1, 2, -1);
            egonet.SeedIndex.Should().Be(0);
        }
    }
}
