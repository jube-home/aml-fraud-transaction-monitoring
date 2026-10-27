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
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Exceptions;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphDiagramUnconnectedTests
    {
        private static GraphDiagramBuilder WithLoose(int loose)
        {
            var builder = Diagram.Graph().Node("a", "Alpha").Node("b", "Beta").Edge("a", "b");

            for (var i = 0; i < loose; i++)
            {
                builder.Node("loose" + i, "Loose " + i);
            }

            return builder;
        }

        [Fact]
        public void ManyUnconnectedNodesGoInAGridBelowTheConnectedGraphInsteadOfOneWideLevel()
        {
            var layout = WithLoose(40).Build().Layout();

            layout.Nodes.Should().HaveCount(42);
            layout.Nodes.Select(n => n.Index).Should().Equal(Enumerable.Range(0, 42));
            layout.Width.Should().BeLessThan(1100);

            var connectedBottom = layout.Nodes.Take(2).Max(n => n.Y + n.Height / 2);

            layout.Nodes.Skip(2).Should().OnlyContain(n => n.Y - n.Height / 2 > connectedBottom);
            layout.Nodes.Skip(2).Select(n => n.Y).Distinct().Count().Should().BeGreaterThan(1);
            layout.Edges.Single().From.Should().Be(0);
            layout.Edges.Single().To.Should().Be(1);
        }

        [Fact]
        public void TheGridNeverOverlapsAndStaysInsideTheDrawing()
        {
            var layout = WithLoose(25).Build().Layout();

            foreach (var node in layout.Nodes)
            {
                (node.X - node.Width / 2).Should().BeGreaterThanOrEqualTo(0);
                (node.X + node.Width / 2).Should().BeLessThanOrEqualTo(layout.Width + 0.01);
                (node.Y + node.Height / 2).Should().BeLessThanOrEqualTo(layout.Height + 0.01);
            }

            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    var a = layout.Nodes[i];
                    var b = layout.Nodes[j];

                    (Math.Abs(a.X - b.X) < (a.Width + b.Width) / 2 && Math.Abs(a.Y - b.Y) < (a.Height + b.Height) / 2)
                        .Should().BeFalse(a.Id + " overlaps " + b.Id);
                }
            }
        }

        [Fact]
        public void FewUnconnectedNodesKeepTheirPlaceOnTheFirstLevelAsBefore()
        {
            var layout = WithLoose(3).Build().Layout();

            layout.Nodes.Where(n => n.Id.StartsWith("loose")).Should().OnlyContain(n => n.Layer == 0);
        }

        [Fact]
        public void AGraphOfNothingButUnconnectedNodesStillDraws()
        {
            var builder = Diagram.Graph();

            for (var i = 0; i < 12; i++)
            {
                builder.Node("n" + i);
            }

            builder.Build().Layout().Nodes.Should().HaveCount(12);
        }

        [Fact]
        public void TheLargestDrawableExtentCanBeRaisedButNotWithoutLimit()
        {
            var wide = Diagram.Graph().Node("root", "Root");

            for (var i = 0; i < 60; i++)
            {
                wide.Node("leaf" + i, "A rather long leaf label " + i).Edge("root", "leaf" + i);
            }

            var small = () => wide.Build().Layout();
            var raised = () => wide.MaxExtent(12000).Build().Layout();

            small.Should().Throw<DiagramTooLargeException>();
            raised.Should().NotThrow();
        }
    }
}
