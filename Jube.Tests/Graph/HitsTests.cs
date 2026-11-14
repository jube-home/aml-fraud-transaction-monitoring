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
    public sealed class HitsTests
    {
        [Fact]
        public void HubsPointAtAuthorities()
        {
            var graph = CompressedGraphBuilder.FromEdges(
            [
                (1L, 3L, 1f, (byte)0), (1L, 4L, 1f, (byte)0), (1L, 5L, 1f, (byte)0),
                (2L, 3L, 1f, (byte)0), (2L, 4L, 1f, (byte)0)
            ], false);

            var (hub, authority) = Hits.Compute(graph);

            hub[graph.IndexOf(1L)].Should().Be(1d);
            hub[graph.IndexOf(2L)].Should().BeInRange(0.5, 0.99);
            hub[graph.IndexOf(3L)].Should().Be(0d);
            authority[graph.IndexOf(3L)].Should().Be(1d);
            authority[graph.IndexOf(4L)].Should().Be(1d);
            authority[graph.IndexOf(5L)].Should().BeInRange(0.3, 0.99);
            authority[graph.IndexOf(1L)].Should().Be(0d);
        }

        [Fact]
        public void EmptyGraphGivesEmptyArrays()
        {
            var graph = CompressedGraphBuilder.FromEdges([], false);

            var (hub, authority) = Hits.Compute(graph);

            hub.Should().BeEmpty();
            authority.Should().BeEmpty();
        }
    }
}
