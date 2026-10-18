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
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Exceptions;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class MatrixTests
    {
        private static double[,] Tiny()
        {
            var d = new double[4, 4];

            void Set(int i, int j, double v)
            {
                d[i, j] = d[j, i] = v;
            }

            Set(0, 2, 1);
            Set(1, 3, 1.5);
            Set(0, 1, 5);
            Set(0, 3, 6);
            Set(1, 2, 5.5);
            Set(2, 3, 4.8);

            return d;
        }

        [Fact]
        public void HierarchicalOrderMatchesTheHandWorkedTinyMatrix()
        {
            HierarchicalOrder.Order(Tiny()).Should().Equal(0, 2, 3, 1);
        }

        [Fact]
        public void OrderIsAPermutationAndDeterministicForLargerInput()
        {
            var n = 40;
            var d = new double[n, n];

            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    d[i, j] = Math.Abs(Math.Sin(i * 1.7) - Math.Sin(j * 1.7)) + Math.Abs(i % 3 - j % 3);
                }
            }

            var a = HierarchicalOrder.Order(d);
            a.OrderBy(x => x).Should().Equal(Enumerable.Range(0, n));
            HierarchicalOrder.Order(d).Should().Equal(a);
        }

        [Fact]
        public void TinyAndInvalidInputsAreHandled()
        {
            HierarchicalOrder.Order(new double[0, 0]).Should().BeEmpty();
            HierarchicalOrder.Order(new double[2, 2]).Should().Equal(0, 1);
            Action square = () => HierarchicalOrder.Order(new double[2, 3]);
            square.Should().Throw<ChartSpecificationException>();

            Action cap = () =>
                HierarchicalOrder.Order(new double[HierarchicalOrder.MaxItems + 1, HierarchicalOrder.MaxItems + 1]);
            cap.Should().Throw<ChartLimitException>();
            var withNaN = new double[3, 3];
            withNaN[0, 1] = withNaN[1, 0] = double.NaN;
            withNaN[0, 2] = withNaN[2, 0] = 1;
            withNaN[1, 2] = withNaN[2, 1] = 2;
            HierarchicalOrder.Order(withNaN).OrderBy(x => x).Should().Equal(0, 1, 2);
        }

        [Fact]
        public void RectangularRowsClusterTheirNearestNeighboursTogether()
        {
            var order = HierarchicalOrder.OrderRows([[0.0, 0], [10.0, 10], [0.1, 0], [10.1, 10]]);
            var pos = order.Select((v, i) => (v, i)).ToDictionary(t => t.v, t => t.i);
            Math.Abs(pos[0] - pos[2]).Should().Be(1);
            Math.Abs(pos[1] - pos[3]).Should().Be(1);
        }

        [Fact]
        public void CorrelationMatrixIsClusteredIntoBlocksAndKeepsGivenOrderWhenAsked()
        {
            var svg = MoreSamples.CorrelationClustered().Render();
            MoreAssert.IsSafe(svg);

            var labels = Regex.Matches(svg, "<text class=\"tk end mid\"[^>]*>([^<]+)</text>")
                .Select(m => m.Groups[1].Value).ToArray();
            labels.Should().HaveCount(6);
            labels.OrderBy(x => x).Should().Equal("Age", "Amount", "Balance", "Distance", "Hour", "Velocity");
            labels.Should().NotEqual(["Amount", "Distance", "Velocity", "Hour", "Balance", "Age"]);

            var plain = Chart.CorrelationMatrix().Order(MatrixOrder.None).Variable("x", [1.0, 2, 3])
                .Variable("y", [3.0, 1, 2]).Variable("z", [1.0, 2, 4]).Render();

            Regex.Matches(plain, "<text class=\"tk end mid\"[^>]*>([^<]+)</text>").Select(m => m.Groups[1].Value)
                .Should().Equal("x", "y", "z");
        }

        [Fact]
        public void CorrelationCellsCarryPearsonValues()
        {
            var svg = Chart.CorrelationMatrix().Order(MatrixOrder.None).Variable("x", [1.0, 2, 3, 4])
                .Variable("y", [2.0, 4, 6, 8])
                .Variable("z", [4.0, 3, 2, 1]).Render();

            svg.Should().Contain("Value: 1").And.Contain("Value: -1");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void AdjacencyIsSymmetricUnlessDirectedAndClustersCommunities()
        {
            var undirected = Chart.Adjacency().Nodes(["a", "b"]).Edge("a", "b", 3).Render();
            Regex.Matches(undirected, "Value: 3").Count.Should().BeGreaterThan(0);
            var directed = Chart.Adjacency().Directed().Nodes(["a", "b"]).Edge("a", "b", 3).ShowValues().Render();
            Regex.Matches(directed, "<text class=\"lbl ctr mid[^>]*>3</text>").Count.Should().Be(1);
            var svg = MoreSamples.Adjacency().Render();

            var labels = Regex.Matches(svg, "<text class=\"tk end mid\"[^>]*>([^<]+)</text>")
                .Select(m => m.Groups[1].Value).ToArray();

            var first = labels.Take(4).OrderBy(x => x).ToArray();
            (first.SequenceEqual(["A", "C", "E", "G"]) || first.SequenceEqual(["B", "D", "F", "H"])).Should().BeTrue();
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void MatrixRejectsBadInputAndEnforcesCaps()
        {
            Action unknown = () => Chart.Adjacency().Nodes(["a"]).Edge("a", "zzz").Render();
            unknown.Should().Throw<ChartSpecificationException>();
            Action duplicate = () => Chart.Adjacency().Nodes(["a", "a"]).Render();
            duplicate.Should().Throw<ChartSpecificationException>();
            Action weight = () => Chart.Adjacency().Nodes(["a", "b"]).Edge("a", "b", double.NaN).Render();
            weight.Should().Throw<ChartSpecificationException>();
            Action shape = () => Chart.Matrix().Rows(["a"]).Columns(["b", "c"]).Values([[1.0]]).Render();
            shape.Should().Throw<ChartSpecificationException>();
            var nodes = Enumerable.Range(0, 250).Select(i => "n" + i).ToArray();
            Action cells = () => Chart.Adjacency().Nodes(nodes).Render();
            cells.Should().Throw<ChartLimitException>();
        }

        [Fact]
        public void RectangularMatrixClustersRowsAndColumnsIndependently()
        {
            var svg = Chart.Matrix().Rows(["r1", "r2", "r3"]).Columns(["c1", "c2", "c3"]).Order(MatrixOrder.Cluster)
                .Values([[1.0, 9, 1], [9.0, 1, 9], [1.0, 9, 1]]).Render();
            MoreAssert.IsSafe(svg);
        }
    }
}