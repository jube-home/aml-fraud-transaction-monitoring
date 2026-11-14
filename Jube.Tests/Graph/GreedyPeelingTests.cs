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
using System.Linq;
using FluentAssertions;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Xunit;

namespace Jube.Test.Graph
{
    [Trait("Category", "Unit")]
    public sealed class GreedyPeelingTests
    {
        internal static IEnumerable<(long From, long To, float Weight, byte Kind)> PlantedBlock()
        {
            for (long i = 1; i <= 12; i++)
            {
                yield return (i, i + 1, 1f, 0);
            }

            yield return (3L, 100L, 1f, 0);
            yield return (105L, 9L, 1f, 0);

            for (long a = 100; a <= 105; a++)
            {
                for (long b = 100; b <= 105; b++)
                {
                    if (a != b)
                    {
                        yield return (a, b, 1f, 0);
                    }
                }
            }
        }

        [Fact]
        public void FindsThePlantedDenseBlock()
        {
            var graph = CompressedGraphBuilder.FromEdges(PlantedBlock(), false);

            var (nodes, density) = GreedyPeeling.DensestBlock(graph);

            nodes.Select(i => graph.NodeIds[i]).OrderBy(id => id).Should().Equal(100L, 101L, 102L, 103L, 104L, 105L);
            nodes.Should().BeInAscendingOrder();
            density.Should().BeGreaterThan(1.5);
        }

        [Fact]
        public void UniformColumnWeightsGiveEdgeDensity()
        {
            var graph = CompressedGraphBuilder.FromEdges(PlantedBlock(), false);

            var (nodes, density) = GreedyPeeling.DensestBlock(graph, _ => 1d);

            nodes.Should().HaveCount(6);
            density.Should().BeApproximately(5d, 1e-9);
        }
    }
}
