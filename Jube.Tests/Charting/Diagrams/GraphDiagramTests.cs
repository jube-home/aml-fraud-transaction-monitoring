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
using Jube.Charting.Diagrams.Models;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphDiagramTests
    {
        private static void NoOverlap(GraphLayoutResult layout)
        {
            for (var i = 0; i < layout.Nodes.Count; i++)
            {
                for (var j = i + 1; j < layout.Nodes.Count; j++)
                {
                    var a = layout.Nodes[i];
                    var b = layout.Nodes[j];

                    var overlap = Math.Abs(a.X - b.X) < (a.Width + b.Width) / 2 &&
                                  Math.Abs(a.Y - b.Y) < (a.Height + b.Height) / 2;

                    overlap.Should().BeFalse(a.Id + " overlaps " + b.Id);
                }
            }
        }

        [Fact]
        public void DiamondHasThreeLevelsAndNoCrossings()
        {
            var layout = DiagramSamples.Diamond().Build().Layout();
            layout.Kind.Should().Be(GraphLayoutKind.Layered);
            layout.Nodes.Select(n => n.Layer).Should().Equal(0, 1, 1, 2);
            layout.Crossings.Should().Be(0);
            layout.Nodes[0].X.Should().BeApproximately((layout.Nodes[1].X + layout.Nodes[2].X) / 2, 0.5);
            layout.Nodes[3].X.Should().BeApproximately((layout.Nodes[1].X + layout.Nodes[2].X) / 2, 0.5);
            layout.Nodes[1].X.Should().BeLessThan(layout.Nodes[2].X);
            NoOverlap(layout);
        }

        [Fact]
        public void ChainIsAStraightVerticalLine()
        {
            var layout = DiagramSamples.Chain(5).Build().Layout();
            layout.Nodes.Select(n => n.Layer).Should().Equal(0, 1, 2, 3, 4);
            layout.Nodes.Select(n => n.Y).Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            layout.Nodes.Select(n => n.X).Max().Should().BeApproximately(layout.Nodes.Select(n => n.X).Min(), 0.01);
            layout.Crossings.Should().Be(0);
        }

        [Fact]
        public void NaiveBayesPutsTheClassAboveEveryFeature()
        {
            var layout = DiagramSamples.NaiveBayes().Build().Layout();
            layout.Nodes[0].Layer.Should().Be(0);
            layout.Nodes.Skip(1).Should().OnlyContain(n => n.Layer == 1);
            layout.Edges.Should().HaveCount(6).And.OnlyContain(e => e.From == 0);
            layout.Crossings.Should().Be(0);
            var features = layout.Nodes.Skip(1).Select(n => n.X).ToArray();
            layout.Nodes[0].X.Should().BeApproximately(features.Average(), 1.0);
            NoOverlap(layout);
        }

        [Fact]
        public void TreeAugmentedStructureKeepsEdgesBetweenDeclaredNodesAndDoesNotCross()
        {
            var layout = DiagramSamples.Tan().Build().Layout();
            layout.Edges.Should().OnlyContain(e => e.From >= 0 && e.From < 7 && e.To >= 0 && e.To < 7);

            layout.Nodes.First(n => n.Id == "velocity_1h").Layer.Should()
                .BeGreaterThan(layout.Nodes.First(n => n.Id == "amount_z").Layer);

            layout.Nodes.First(n => n.Id == "class").Layer.Should().Be(0);
            NoOverlap(layout);
        }

        [Fact]
        public void CyclesAreBrokenByReversalAndStillDrawn()
        {
            var layout = DiagramSamples.CyclicGraph().Build().Layout();
            layout.Edges.Count(e => e.Reversed).Should().Be(1);
            layout.Edges.Single(e => e.Reversed).From.Should().Be(3);
            layout.Nodes.Select(n => n.Layer).Should().Equal(0, 1, 2, 3);
            var svg = DiagramSamples.CyclicGraph().Render();
            SvgAssert.IsSafe(svg, false);
            svg.Should().Contain("-0.3");
        }

        [Fact]
        public void CrossingReductionFindsTheOrderWithoutCrossings()
        {
            var layout = Diagram.Graph().Node("a1").Node("a2").Node("b1").Node("b2").Edge("a1", "b2").Edge("a2", "b1")
                .Build().Layout();
            layout.Crossings.Should().Be(0);
        }

        [Fact]
        public void CompleteBipartiteGraphHasTheMinimumPossibleCrossings()
        {
            var b = Diagram.Graph();

            for (var i = 0; i < 3; i++)
            {
                b.Node("a" + i).Node("b" + i);
            }

            for (var i = 0; i < 3; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    b.Edge("a" + i, "b" + j);
                }
            }

            b.Build().Layout().Crossings.Should().Be(9);
        }

        [Fact]
        public void LongEdgesRouteThroughIntermediateLevels()
        {
            var layout = Diagram.Graph().Node("a").Node("b").Node("c").Node("d").Edge("a", "b").Edge("b", "c")
                .Edge("c", "d")
                .Edge("a", "d").Build().Layout();

            var @long = layout.Edges.Single(e => e.From == 0 && e.To == 3);
            @long.Points.Count.Should().Be(4);
            @long.Points.Select(p => p.Y).Should().BeInAscendingOrder();
            var direct = layout.Nodes[0].X;
            @long.Points[1].X.Should().NotBeApproximately(layout.Nodes[1].X, 5);
            direct.Should().BeGreaterThan(-1);
        }

        [Fact]
        public void StateGraphUsesACircleWithSelfLoopsAndCurvedEdges()
        {
            var layout = DiagramSamples.Hmm().Build().Layout();
            layout.Kind.Should().Be(GraphLayoutKind.Circular);
            layout.Edges.Should().HaveCount(8);
            layout.Edges.Count(e => e.SelfLoop).Should().Be(3);
            var cx = layout.Nodes.Average(n => n.X);
            var cy = layout.Nodes.Average(n => n.Y);

            var radii = layout.Nodes.Select(n => Math.Sqrt((n.X - cx) * (n.X - cx) + (n.Y - cy) * (n.Y - cy)))
                .ToArray();

            radii.Max().Should().BeApproximately(radii.Min(), 0.5);
            NoOverlap(layout);
            var svg = DiagramSamples.Hmm().Render();
            svg.Should().Contain(">0.92<").And.Contain("Probability: 0.92").And.Contain("Typical spending");
            svg.Should().Contain("class=\"ah\"");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void EveryLayoutPointIsFiniteAndInsideTheCanvas()
        {
            foreach (var b in new[]
                     {
                         DiagramSamples.Hmm(), DiagramSamples.Tan(), DiagramSamples.CyclicGraph(),
                         DiagramSamples.LatentTree()
                     })
            {
                var layout = b.Build().Layout();

                foreach (var n in layout.Nodes)
                {
                    n.X.Should().BeInRange(0, layout.Width);
                    n.Y.Should().BeInRange(0, layout.Height);
                }

                foreach (var p in layout.Edges.SelectMany(e => e.Points))
                {
                    double.IsFinite(p.X).Should().BeTrue();
                    double.IsFinite(p.Y).Should().BeTrue();
                }
            }
        }

        [Fact]
        public void UndirectedGraphsHaveNoArrowheads()
        {
            var svg = DiagramSamples.Diamond().Undirected().Render();
            svg.Should().NotContain("class=\"ah\"");
            DiagramSamples.Diamond().Render().Should().Contain("class=\"ah\"");
        }

        [Fact]
        public void HoverRulesTooltipsAndRolesAreEmitted()
        {
            var svg = DiagramSamples.LatentTree().Render();
            svg.Should().Contain(":has(.nd.n0:hover,.nd.n0:focus-visible)");
            svg.Should().Contain("class=\"tp tn0\"").And.Contain("class=\"tp tx0\"");
            svg.Should().Contain("Latent variable").And.Contain("nb dash");
            svg.Should().Contain("class=\"lg s4\"");
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.Tan().Render().Should().Be(DiagramSamples.Tan().Render());
            DiagramSamples.Hmm().Render().Should().Be(DiagramSamples.Hmm().Render());
        }

        [Fact]
        public void TooManyNodesOrTooWideALevelThrowsDiagramTooLarge()
        {
            var many = Diagram.Graph();

            for (var i = 0; i < 151; i++)
            {
                many.Node("n" + i);
            }

            many.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("graph nodes");

            var wide = Diagram.Graph().Node("c", "class", GraphNodeRole.Class);

            for (var i = 0; i < 60; i++)
            {
                wide.Node("feature_number_" + i).Edge("c", "feature_number_" + i);
            }

            wide.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("graph extent in pixels");

            var ring = Diagram.Graph().Layout(GraphLayoutKind.Circular);

            for (var i = 0; i < 41; i++)
            {
                ring.Node("s" + i, "s" + i, GraphNodeRole.State);
            }

            ring.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("circular graph nodes");
        }

        [Fact]
        public void MalformedSpecificationsAndUnknownLayoutsAreRejected()
        {
            Diagram.Graph().Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();
            Diagram.Graph().Node("a").Node("a").Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Graph().Node("a").Edge("a", "zzz").Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>().WithMessage("*not declared*");

            Diagram.Graph().Node("a").Node("b").Edge("a", "b").Edge("a", "b").Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Graph().Node("a").Node("b").Edge("a", "b", probability: 1.5).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Graph().Node("a").Layout((GraphLayoutKind)99).Invoking(b => b.Render()).Should()
                .Throw<DiagramNotSupportedException>();
        }

        [Fact]
        public void IsolatedNodesAndSingleNodeGraphsRender()
        {
            SvgAssert.IsSafe(Diagram.Graph().Node("only").Render(), false);

            SvgAssert.IsSafe(
                Diagram.Graph().Node("s", "S", GraphNodeRole.State).Edge("s", "s", probability: 1).Render(), false);

            SvgAssert.IsSafe(Diagram.Graph().Node("a").Node("b").Render(), false);
        }
    }
}