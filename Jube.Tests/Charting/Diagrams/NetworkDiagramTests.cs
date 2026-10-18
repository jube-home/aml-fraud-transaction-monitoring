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
using Jube.Charting.Core.Models;
using Jube.Charting.Diagrams;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class NetworkDiagramTests
    {
        [Fact]
        public void XorNetworkHasTheDeclaredShape()
        {
            var layout = DiagramSamples.Xor().Build().Layout();
            layout.Nodes.Should().HaveCount(5);
            layout.Edges.Should().HaveCount(6);
            layout.Nodes.Select(n => n.Layer).Should().Equal(0, 0, 1, 1, 2);

            layout.Edges.Select(e => (layout.Nodes[e.From].Layer, layout.Nodes[e.To].Layer))
                .Should().OnlyContain(p => p.Item2 == p.Item1 + 1);

            layout.Nodes.Should().OnlyContain(n => !n.IsAggregate);
            layout.Edges.Select(e => e.Weight).Should().BeEquivalentTo([5.1, -5.2, -4.8, 4.9, 6.3, 6.1]);
        }

        [Fact]
        public void XorRendersWellFormedSafeSvgWithLabelsAndBiases()
        {
            var svg = DiagramSamples.Xor().Render();
            SvgAssert.IsSafe(svg, false);
            svg.Should().Contain(">x1<").And.Contain(">xor<").And.Contain("tanh").And.Contain("Bias: -2.6");
            svg.Should().Contain("Weight: -5.2");
        }

        [Fact]
        public void EdgeTooltipsExplainWhatTheColourMeans()
        {
            var svg = DiagramSamples.Xor().Render();
            svg.Should().Contain("red, a positive weight").And.Contain("blue, a negative weight");
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.Wide().Render().Should().Be(DiagramSamples.Wide().Render());
            DiagramSamples.Xor().Render().Should().Be(DiagramSamples.Xor().Render());
        }

        [Fact]
        public void LayersAreOrderedLeftToRightAndNodesKeepDeclaredOrder()
        {
            var layout = DiagramSamples.Wide().Build().Layout();

            for (var l = 1; l < 4; l++)
            {
                layout.Nodes.First(n => n.Layer == l).X.Should()
                    .BeGreaterThan(layout.Nodes.First(n => n.Layer == l - 1).X);
            }

            foreach (var group in layout.Nodes.GroupBy(n => n.Layer))
            {
                group.Select(n => n.Y).Should().BeInAscendingOrder();
                group.Select(n => n.X).Distinct().Should().HaveCount(1);
            }
        }

        [Fact]
        public void NoNodesOverlapAndEdgesConnectDeclaredNodes()
        {
            foreach (var layout in new[]
                         { DiagramSamples.Xor().Build().Layout(), DiagramSamples.Wide().Build().Layout() })
            {
                for (var i = 0; i < layout.Nodes.Count; i++)
                {
                    for (var j = i + 1; j < layout.Nodes.Count; j++)
                    {
                        var a = layout.Nodes[i];
                        var b = layout.Nodes[j];

                        var overlap = Math.Abs(a.X - b.X) < a.HalfWidth + b.HalfWidth &&
                                      Math.Abs(a.Y - b.Y) < a.HalfHeight + b.HalfHeight;
                        overlap.Should().BeFalse();
                    }
                }

                layout.Edges.Should().OnlyContain(e => e.From >= 0 && e.From < layout.Nodes.Count && e.To >= 0 &&
                                                       e.To < layout.Nodes.Count &&
                                                       layout.Nodes[e.To].Layer == layout.Nodes[e.From].Layer + 1);
            }
        }

        [Fact]
        public void WideNetworkElidesToTheCapsWithAggregateNodes()
        {
            var layout = DiagramSamples.Wide().Build().Layout();
            layout.Nodes.Where(n => n.Layer == 0).Should().HaveCount(24);
            layout.Nodes.Where(n => n.IsAggregate).Select(n => n.Layer).Should().Equal(0, 1);
            layout.Nodes.Single(n => n.Layer == 0 && n.IsAggregate).HiddenCount.Should().Be(17);
            layout.Edges.Count.Should().BeLessThanOrEqualTo(450);
            layout.DrawnEdgesBeforeElision.Should().BeGreaterThan(450);
            var svg = DiagramSamples.Wide().Render();
            svg.Should().Contain("+17").And.Contain("nodes aggregated").And.Contain("connections shown");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void StrongestConnectionsSurviveElision()
        {
            var weights = Enumerable.Range(0, 30).Select(i =>
                Enumerable.Range(0, 30).Select(j => i == 3 && j == 4 ? 99.0 : 0.01 * (i + j)).ToArray()).ToArray();

            var layout = Diagram.Network().Input(Enumerable.Range(0, 30).Select(i => "a" + i))
                .Output(Enumerable.Range(0, 30).Select(i => "b" + i), weights).MaxNodesPerLayer(30).MaxEdges(50).Build()
                .Layout();

            layout.Edges.Should().Contain(e => e.Weight == 99.0);
            layout.Edges.Count.Should().Be(50);
        }

        [Fact]
        public void UnweightedNetworksAreThinnedToTheEdgeBudgetByShrinkingLayers()
        {
            var layout = Diagram.Network().Input(Enumerable.Range(0, 100).Select(i => "a" + i))
                .Output(Enumerable.Range(0, 100).Select(i => "b" + i), null).MaxEdges(200).Build().Layout();

            layout.Edges.Count.Should().BeLessThanOrEqualTo(200);
            layout.Edges.Should().OnlyContain(e => double.IsNaN(e.Weight));
        }

        [Fact]
        public void TooManyLayersThrowsDiagramTooLarge()
        {
            var b = Diagram.Network().Input(["a"]);

            for (var i = 0; i < 20; i++)
            {
                b.Hidden(["h"], null);
            }

            var act = () => b.Output(["o"], null).Render();
            act.Should().Throw<DiagramTooLargeException>().Which.Limit.Should().Be("network layers");
        }

        [Fact]
        public void EnormousInputThrowsDiagramTooLarge()
        {
            var act = () => Diagram.Network().Input(Enumerable.Range(0, 60_000).Select(i => "a" + i))
                .Output(Enumerable.Range(0, 60_000).Select(i => "b" + i), null).Render();

            act.Should().Throw<DiagramTooLargeException>().WithMessage("*too large*");
        }

        [Fact]
        public void ANetworkWithNoFiniteWeightsIsNotSupported()
        {
            var act = () => Diagram.Network().Input(["a", "b"])
                .Output(["o"], [[double.NaN, double.NaN]]).Render();
            act.Should().Throw<DiagramNotSupportedException>();
        }

        [Fact]
        public void MalformedSpecificationsThrowSpecificationExceptions()
        {
            var one = () => Diagram.Network().Input(["a"]).Render();
            one.Should().Throw<ChartSpecificationException>();
            var rows = () => Diagram.Network().Input(["a", "b"]).Output(["o"], [[1.0]]).Render();
            rows.Should().Throw<ChartSpecificationException>().WithMessage("*one weight per node*");

            var count = () =>
                Diagram.Network().Input(["a", "b"]).Output(["o", "p"], [[1.0, 2]]).Render();

            count.Should().Throw<ChartSpecificationException>().WithMessage("*one row per node*");

            var infinite = () =>
                Diagram.Network().Input(["a"]).Output(["o"], [[double.PositiveInfinity]]).Render();
            infinite.Should().Throw<ChartSpecificationException>();
            var empty = () => Diagram.Network().Input([]).Output(["o"], null).Render();
            empty.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void HoverCssTooltipsAndFocusAreEmitted()
        {
            var svg = DiagramSamples.Xor().Render();
            svg.Should().Contain(":has(.nd.n2:hover,.nd.n2:focus-visible)");
            svg.Should().Contain(".eg:not(.e2)");
            svg.Should().Contain(":has(.eg:hover)");
            svg.Should().Contain("tabindex=\"0\"");
            svg.Should().Contain("role=\"graphics-symbol\"");
            svg.Should().Contain("class=\"tp tn2\"");
            svg.Should().Contain("class=\"tp tx0\"");
            svg.Should().Contain("prefers-color-scheme:dark");
            svg.Should().Contain("prefers-reduced-motion");
            svg.Should().Contain("aria-labelledby");
            svg.Should().Contain("<desc").And.Contain("Neural network diagram with 3 layers");
        }

        [Fact]
        public void NodesCarryTheirNeighboursForHighlighting()
        {
            var svg = DiagramSamples.Xor().Render();
            svg.Should().Contain("nd s2 j0 j1 j4");
        }

        [Fact]
        public void ThemeCanBeForced()
        {
            DiagramSamples.Xor().Theme(ThemeMode.Dark).Render().Should().Contain("data-theme=\"dark\"");
            DiagramSamples.Xor().Theme(ThemeMode.Light).Render().Should().Contain("data-theme=\"light\"");
        }

        [Fact]
        public void NonInteractiveOutputHasNoMarksOrTooltips()
        {
            var svg = DiagramSamples.Xor().Interactive(false).Render();
            svg.Should().NotContain("tabindex");
            svg.Should().NotContain("class=\"tp ");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void RolesAreDistinguishedByColourClassAndLegend()
        {
            var svg = DiagramSamples.Xor().Render();
            svg.Should().Contain("class=\"lg s0\"").And.Contain("class=\"lg s2\"").And.Contain("class=\"lg s1\"");
        }
    }
}