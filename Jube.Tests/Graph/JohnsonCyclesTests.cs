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

using System.Collections.Generic;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class JohnsonCyclesTests
    {
        private static CompressedGraph FourCycleWithTail(bool backEdge)
        {
            var edges = new List<(long, long, float, byte)>
            {
                (0L, 1L, 1f, 0), (1L, 2L, 1f, 0), (2L, 3L, 1f, 0), (3L, 0L, 1f, 0), (3L, 4L, 1f, 0), (4L, 5L, 1f, 0)
            };

            if (backEdge)
            {
                edges.Add((1L, 0L, 1f, 0));
            }

            return CompressedGraphBuilder.FromEdges(edges, false);
        }

        [Fact]
        public void FindsTheSingleFourCycleAndIgnoresTheTail()
        {
            var cycles = JohnsonCycles.Enumerate(FourCycleWithTail(false), 6);

            cycles.Should().HaveCount(1);
            cycles[0].Should().Equal(0, 1, 2, 3);
        }

        [Fact]
        public void RespectsTheMaximumLengthAndFindsTwoCycles()
        {
            var graph = FourCycleWithTail(true);

            JohnsonCycles.Enumerate(graph, 6).Should().HaveCount(2);
            var short3 = JohnsonCycles.Enumerate(graph, 3);
            short3.Should().HaveCount(1);
            short3[0].Should().Equal(0, 1);
        }
    }
}
