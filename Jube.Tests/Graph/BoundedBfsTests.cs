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
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class BoundedBfsTests
    {
        private static CompressedGraph RingOfFourOnOneDevice()
        {
            return CompressedGraphBuilder.FromEdges(
            [
                (1L, 2L, 1f, (byte)2), (2L, 3L, 1f, (byte)2), (3L, 4L, 1f, (byte)2), (4L, 1L, 1f, (byte)2),
                (1L, 10L, 1f, (byte)1), (2L, 10L, 1f, (byte)1), (3L, 10L, 1f, (byte)1), (4L, 10L, 1f, (byte)1)
            ], true);
        }

        [Fact]
        public void ConfirmedNodeGivesDistancesOneAndTwo()
        {
            var graph = RingOfFourOnOneDevice();

            var (distance, within1, within2, within3) = BoundedBfs.DistancesFrom(graph, [graph.IndexOf(1L)], 4);

            distance[graph.IndexOf(1L)].Should().Be(0);
            distance[graph.IndexOf(2L)].Should().Be(1);
            distance[graph.IndexOf(4L)].Should().Be(1);
            distance[graph.IndexOf(10L)].Should().Be(1);
            distance[graph.IndexOf(3L)].Should().Be(2);
            within1[graph.IndexOf(2L)].Should().Be(1);
            within1[graph.IndexOf(3L)].Should().Be(0);
            within2[graph.IndexOf(3L)].Should().Be(1);
            within3[graph.IndexOf(3L)].Should().Be(1);
            within1[graph.IndexOf(1L)].Should().Be(0);
        }

        [Fact]
        public void CapHidesNodesBeyondIt()
        {
            var graph = RingOfFourOnOneDevice();

            var (distance, _, within2, _) = BoundedBfs.DistancesFrom(graph, [graph.IndexOf(1L)], 1);

            distance[graph.IndexOf(3L)].Should().Be(-1);
            within2[graph.IndexOf(3L)].Should().Be(0);
        }

        [Fact]
        public void MultipleSeedsAreCountedSeparately()
        {
            var graph = RingOfFourOnOneDevice();

            var (distance, within1, _, _) = BoundedBfs.DistancesFrom(graph,
                [graph.IndexOf(1L), graph.IndexOf(3L)], 3);

            distance[graph.IndexOf(2L)].Should().Be(1);
            within1[graph.IndexOf(2L)].Should().Be(2);
            within1[graph.IndexOf(10L)].Should().Be(2);
        }
    }
}
