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
    public sealed class TrianglesTests
    {
        [Fact]
        public void TwoTriangleBridgeGraph()
        {
            var graph = CompressedGraphBuilder.FromEdges(
            [
                (1L, 2L, 1f, (byte)0), (2L, 3L, 1f, (byte)0), (3L, 1L, 1f, (byte)0),
                (3L, 7L, 1f, (byte)0), (7L, 4L, 1f, (byte)0),
                (4L, 5L, 1f, (byte)0), (5L, 6L, 1f, (byte)0), (6L, 4L, 1f, (byte)0)
            ], true);

            var (triangles, clustering) = Triangles.Count(graph);

            (triangles.Sum() / 3).Should().Be(2);
            triangles[graph.IndexOf(7L)].Should().Be(0);
            clustering[graph.IndexOf(7L)].Should().Be(0d);
            clustering[graph.IndexOf(1L)].Should().Be(1d);
            clustering[graph.IndexOf(3L)].Should().BeApproximately(1d / 3d, 1e-12);
        }

        [Fact]
        public void DirectedEdgesAreTreatedAsUndirectedForTriangles()
        {
            var graph = CompressedGraphBuilder.FromEdges(
                [(1L, 2L, 1f, (byte)0), (2L, 3L, 1f, (byte)0), (1L, 3L, 1f, (byte)0)], false);

            var (triangles, clustering) = Triangles.Count(graph);

            triangles.Should().Equal(1, 1, 1);
            clustering.Should().OnlyContain(c => c == 1d);
        }
    }
}
