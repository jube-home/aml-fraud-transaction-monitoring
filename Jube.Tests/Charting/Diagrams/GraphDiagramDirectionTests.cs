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
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphDiagramDirectionTests
    {
        private static GraphDiagramBuilder Fan(int leaves, GraphDirection direction, int perLayer)
        {
            var builder = Diagram.Graph().Layout(GraphLayoutKind.Layered).Direction(direction).MaxPerLayer(perLayer)
                .Node("root", "Root");

            for (var i = 0; i < leaves; i++)
            {
                builder.Node("leaf" + i, "Leaf number " + i).Edge("root", "leaf" + i);
            }

            return builder;
        }

        [Fact]
        public void LeftToRightPutsEachLevelInItsOwnColumnAndPointsTheArrowsRight()
        {
            var layout = Diagram.Graph().Layout(GraphLayoutKind.Layered).Direction(GraphDirection.LeftToRight)
                .Node("a", "Alpha").Node("b", "Beta").Node("c", "Gamma").Edge("a", "b").Edge("b", "c").Build().Layout();

            layout.Nodes[0].X.Should().BeLessThan(layout.Nodes[1].X);
            layout.Nodes[1].X.Should().BeLessThan(layout.Nodes[2].X);
            layout.Nodes.Select(n => n.Y).Distinct().Should().HaveCount(1);
            layout.Edges.Should().OnlyContain(e => e.DirX > 0 && e.DirY == 0);
            layout.Width.Should().BeGreaterThan(layout.Height);
        }

        [Fact]
        public void TopDownIsUnchangedWhenNoDirectionIsGiven()
        {
            var plain = Diagram.Graph().Layout(GraphLayoutKind.Layered).Node("a", "Alpha").Node("b", "Beta")
                .Edge("a", "b").Build().Layout();

            plain.Nodes[0].Y.Should().BeLessThan(plain.Nodes[1].Y);
            plain.Edges.Single().DirY.Should().BeGreaterThan(0);
        }

        [Fact]
        public void AWideLevelWrapsIntoSeveralColumnsOfNoMoreThanTheCap()
        {
            var layout = Fan(60, GraphDirection.LeftToRight, 12).Build().Layout();

            var columns = layout.Nodes.Skip(1).GroupBy(n => Math.Round(n.X)).ToList();

            columns.Should().HaveCount(5);
            columns.Should().OnlyContain(c => c.Count() <= 12);
        }

        [Fact]
        public void WithoutACapTheSameLevelStaysOneColumn()
        {
            var layout = Fan(60, GraphDirection.LeftToRight, 0).MaxExtent(6000).Build().Layout();

            layout.Nodes.Skip(1).Select(n => Math.Round(n.X)).Distinct().Should().HaveCount(1);
        }

        [Fact]
        public void AWrappedFanOfSixtyDrawsInsideTheCanvas()
        {
            var svg = Fan(60, GraphDirection.LeftToRight, 12).Render();

            var width = double.Parse(Regex.Match(svg, "viewBox=\"0 0 ([0-9.]+)").Groups[1].Value,
                System.Globalization.CultureInfo.InvariantCulture);

            width.Should().BeLessThanOrEqualTo(DiagramLimits.MaxCanvas);
        }

        [Fact]
        public void ASelfUseDrawsBelowTheNodeWhenLeftToRight()
        {
            var layout = Diagram.Graph().Layout(GraphLayoutKind.Layered).Direction(GraphDirection.LeftToRight)
                .Node("a", "Alpha").Node("b", "Beta").Edge("a", "b").Edge("a", "a").Build().Layout();

            var loop = layout.Edges.Single(e => e.SelfLoop);

            loop.TipY.Should().BeApproximately(layout.Nodes[0].Y + layout.Nodes[0].Height / 2, 0.01);
        }

        [Fact]
        public void AnEmphasisedNodeGetsARingInTheWarningOrErrorSeries()
        {
            var svg = Diagram.Graph().Layout(GraphLayoutKind.Layered)
                .Node(new GraphNode("a", "Alpha") { Emphasis = GraphEmphasis.Warning })
                .Node(new GraphNode("b", "Beta") { Emphasis = GraphEmphasis.Error })
                .Node("c", "Gamma").Edge("a", "b").Edge("b", "c").Render();

            Regex.Matches(svg, "class=\"nb s3\"").Count.Should().Be(1);
            Regex.Matches(svg, "class=\"nb s7\"").Count.Should().Be(1);
        }
    }
}
