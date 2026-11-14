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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Diagrams.Models;
using Xunit;

namespace Jube.Test.Charting.Diagrams
{
    public class GraphNetworkLayoutTests
    {
        private static IEnumerable<GraphDiagramBuilder> Samples()
        {
            yield return DiagramSamples.ForceRing();
            yield return DiagramSamples.RadialRing();
            yield return DiagramSamples.FlowChain();
            yield return DiagramSamples.Combos();
        }

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

        private static IEnumerable<XElement> Marks(XDocument document, string attribute)
        {
            return document.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute(attribute) is not null);
        }

        [Fact]
        public void RenderingEachNetworkLayoutTwiceGivesIdenticalBytes()
        {
            foreach (var sample in Samples())
            {
                sample.Render().Should().Be(sample.Render());
            }

            DiagramSamples.ForceRing().Build().Layout().Kind.Should().Be(GraphLayoutKind.Force);
            DiagramSamples.RadialRing().Build().Layout().Kind.Should().Be(GraphLayoutKind.Radial);
            DiagramSamples.FlowChain().Build().Layout().Kind.Should().Be(GraphLayoutKind.Flow);
        }

        [Fact]
        public void NoTwoNodeBoxesOverlapOnTheSamplesAndEveryPointIsInsideTheCanvas()
        {
            foreach (var sample in Samples())
            {
                var layout = sample.Build().Layout();
                NoOverlap(layout);

                foreach (var node in layout.Nodes)
                {
                    node.X.Should().BeInRange(0, layout.Width);
                    node.Y.Should().BeInRange(0, layout.Height);
                }

                foreach (var p in layout.Edges.SelectMany(e => e.Points))
                {
                    double.IsFinite(p.X).Should().BeTrue();
                    double.IsFinite(p.Y).Should().BeTrue();
                }
            }
        }

        [Fact]
        public void AnchoredNodesMoveUnderTwoPixelsWhenALeafIsAdded()
        {
            var before = DiagramSamples.ForceRing().Build().Layout();
            var anchors = before.Positions;
            anchors.Should().HaveCount(before.Nodes.Count);

            var after = DiagramSamples.ForceRing().Anchors(anchors)
                .Node("new", "New leaf", GraphNodeShape.Circle, group: "Accounts").Edge("acct-3", "new")
                .Build().Layout();

            foreach (var anchor in anchors)
            {
                var moved = after.Nodes.Single(n => n.Id == anchor.Id);
                var distance = Math.Sqrt(Math.Pow(moved.X - anchor.X, 2) + Math.Pow(moved.Y - anchor.Y, 2));
                distance.Should().BeLessThan(2, anchor.Id + " should stay anchored");
            }

            after.Nodes.Should().Contain(n => n.Id == "new");
            NoOverlap(after);
        }

        [Fact]
        public void FlowLayoutPutsNoTwoNodesOfOneLaneInOneColumnAndOrdersLanesAndTimes()
        {
            var layout = DiagramSamples.FlowChain().Build().Layout();

            foreach (var lane in layout.Nodes.GroupBy(n => n.Layer))
            {
                lane.Select(n => n.X).Should().OnlyHaveUniqueItems();
                lane.Select(n => n.Y).Distinct().Should().HaveCount(1);
            }

            layout.LayerCount.Should().Be(3);
            var byId = layout.Nodes.ToDictionary(n => n.Id);
            byId["a1"].Y.Should().BeLessThan(byId["b1"].Y);
            byId["b1"].Y.Should().BeLessThan(byId["m1"].Y);
            byId["a1"].X.Should().BeLessThan(byId["a2"].X);
            byId["a2"].X.Should().BeLessThan(byId["b1"].X);
            byId["b1"].X.Should().BeLessThan(byId["m1"].X);
            byId["m1"].X.Should().BeLessThan(byId["x1"].X);
        }

        [Fact]
        public void FlowLayoutMergesParallelEdgesAndLabelsTheCount()
        {
            var layout = DiagramSamples.FlowChain().Build().Layout();
            layout.Edges.Should().HaveCount(8);
            var merged = layout.Edges.Single(e => e.Merged is { Count: 2 });
            merged.Merged.Should().Equal(3, 4);
            layout.Edges.Should().OnlyContain(e => e.Path.Contains('Q'));
            var svg = DiagramSamples.FlowChain().Render();
            svg.Should().Contain(">×2 3000<").And.Contain(">×3 4500<");
            svg.Should().Contain("Bank A").And.Contain("Mules");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void FlowLayoutRejectsMoreColumnsThanTheExtentAllows()
        {
            var b = Diagram.Graph().Layout(GraphLayoutKind.Flow).MaxExtent(600);

            for (var i = 0; i < 24; i++)
            {
                b.Node("n" + i, "N" + i, GraphNodeShape.Circle, group: "Lane").Time("n" + i, DiagramSamples.Epoch.AddMinutes(i));
            }

            b.Invoking(x => x.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("flow columns");
        }

        [Fact]
        public void RadialLayoutPutsTheSeedAtTheCentreWithRingsByHopDistance()
        {
            var layout = DiagramSamples.RadialRing().Build().Layout();
            var seed = layout.Nodes.Single(n => n.Id == "acct-0");
            seed.Layer.Should().Be(0);
            layout.LayerCount.Should().Be(3);

            double Radius(PlacedGraphNode n)
            {
                return Math.Sqrt(Math.Pow(n.X - seed.X, 2) + Math.Pow(n.Y - seed.Y, 2));
            }

            var ring1 = layout.Nodes.Where(n => n.Layer == 1).Select(Radius).ToArray();
            var ring2 = layout.Nodes.Where(n => n.Layer == 2).Select(Radius).ToArray();
            ring1.Should().HaveCount(4);
            ring2.Should().HaveCount(6);
            ring1.Max().Should().BeApproximately(ring1.Min(), 1);
            ring2.Min().Should().BeGreaterThan(ring1.Max());
        }

        [Fact]
        public void SizeByScalesTheRadiusBetweenOneAndTwoAndAHalfTimes()
        {
            var layout = DiagramSamples.Combos().Build().Layout();
            layout.Nodes.Single(n => n.Id == "c1").Width.Should().BeApproximately(70, 0.01);
            layout.Nodes.Single(n => n.Id == "p1").Width.Should().BeApproximately(28, 0.01);
            DiagramSamples.RadialRing().Build().Layout().Nodes.Should().OnlyContain(n => Math.Abs(n.Width - 28) < 0.01);
        }

        [Fact]
        public void EdgesCarryIdentityKindAndTimesAndShareOneArrowMarker()
        {
            var svg = DiagramSamples.ForceRing().Render();
            var document = XDocument.Parse(svg);
            var edges = Marks(document, "data-edge").ToList();
            edges.Should().HaveCount(DiagramSamples.ForceRing().Build().Spec.Edges.Count);
            var first = edges.Single(e => e.Attribute("data-edge")!.Value == "t0");
            first.Attribute("data-from")!.Value.Should().Be("acct-0");
            first.Attribute("data-to")!.Value.Should().Be("hub");
            first.Attribute("data-kind")!.Value.Should().Be("directed");
            first.Attribute("data-first")!.Value.Should().Be(DiagramSamples.Epoch.ToUnixTimeSeconds().ToString());
            first.Attribute("data-last")!.Value.Should().Be(DiagramSamples.Epoch.AddHours(2).ToUnixTimeSeconds().ToString());
            edges.Should().Contain(e => e.Attribute("data-edge")!.Value.StartsWith("x"));

            var markers = document.Descendants().Where(e => e.Name.LocalName == "marker").ToList();
            markers.Should().HaveCount(1);
            var id = markers[0].Attribute("id")!.Value;
            id.Should().EndWith("-ar");

            document.Descendants().Where(e => e.Attribute("marker-end") is not null).Should()
                .OnlyContain(e => e.Attribute("marker-end")!.Value == "url(#" + id + ")").And.NotBeEmpty();

            Regex.Matches(svg, "class=\"ah\"").Count.Should().Be(1);
            svg.Should().Contain("<defs>");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void NodesCarryShapeKindFirstSeenAndBadges()
        {
            var document = XDocument.Parse(DiagramSamples.ForceRing().Render());
            var nodes = Marks(document, "data-node").ToList();
            var hub = nodes.Single(n => n.Attribute("data-node")!.Value == "hub");
            hub.Attribute("data-kind")!.Value.Should().Be("hexagon");
            hub.Descendants().Count(e => e.Name.LocalName == "rect" && e.Attribute("rx")?.Value == "7").Should().Be(2);
            hub.Descendants().Should().Contain(e => e.Name.LocalName == "text" && e.Value == "hub");
            nodes.Single(n => n.Attribute("data-node")!.Value == "dev-1").Attribute("data-kind")!.Value.Should()
                .Be("square");
            nodes.Single(n => n.Attribute("data-node")!.Value == "ip-1").Attribute("data-kind")!.Value.Should()
                .Be("diamond");

            nodes.Single(n => n.Attribute("data-node")!.Value == "cp-1").Descendants().Should()
                .Contain(e => e.Name.LocalName == "text" && e.Value == "★");

            var flow = XDocument.Parse(DiagramSamples.FlowChain().Render());
            var victim = Marks(flow, "data-node").Single(n => n.Attribute("data-node")!.Value == "a1");
            victim.Attribute("data-first")!.Value.Should().Be(DiagramSamples.Epoch.ToUnixTimeSeconds().ToString());
            victim.Attribute("data-kind")!.Value.Should().Be("circle");

            var combos = XDocument.Parse(DiagramSamples.Combos().Render());
            var cluster = Marks(combos, "data-node").Single(n => n.Attribute("data-node")!.Value == "c1");
            cluster.Attribute("data-kind")!.Value.Should().Be("stack");
            cluster.Descendants().Should().Contain(e => e.Name.LocalName == "text" && e.Value == "×12");
        }

        [Fact]
        public void NetworkCssHidesSelectsAndFillsHubsWhileExistingLayoutsAreUntouched()
        {
            var svg = DiagramSamples.ForceRing().Render();
            svg.Should().Contain("[data-hidden]{opacity:.08;pointer-events:none}");
            svg.Should().Contain("[data-selected] .ep");
            svg.Should().Contain("[data-kind=\"hexagon\"] .nt");
            svg.Should().Contain(":has(.nd.n0:hover,.nd.n0:focus-visible)");

            var layered = DiagramSamples.Tan().Render();
            layered.Should().NotContain("[data-hidden]").And.NotContain("data-edge").And.NotContain("<defs>");
            layered.Should().Contain("class=\"ah\"");
        }

        [Fact]
        public void PositionsRoundTripAsAnchorsAndUndirectedEdgesHaveNoMarker()
        {
            var layout = DiagramSamples.RadialRing().Build().Layout();
            layout.Positions.Select(p => p.Id).Should().Equal(layout.Nodes.Select(n => n.Id));
            layout.Positions.Should().OnlyContain(p => double.IsFinite(p.X) && double.IsFinite(p.Y));

            var svg = DiagramSamples.RadialRing().Undirected().Render();
            svg.Should().NotContain("<defs>").And.NotContain("marker-end");
        }

        [Fact]
        public void AnchorsForUnknownNodesAndMissingSeedsAreIgnored()
        {
            var layout = Diagram.Graph().Layout(GraphLayoutKind.Force).Anchor("ghost", 10, 10).Node("a").Node("b")
                .Edge("a", "b").Build().Layout();

            layout.Nodes.Should().HaveCount(2);

            var radial = Diagram.Graph().Layout(GraphLayoutKind.Radial).Seed("ghost").Node("a").Node("b").Node("c")
                .Edge("a", "b").Build().Layout();

            radial.Nodes.Single(n => n.Id == "a").Layer.Should().Be(0);
            radial.Nodes.Single(n => n.Id == "c").Layer.Should().Be(2);
            SvgAssert.IsSafe(Diagram.Graph().Layout(GraphLayoutKind.Flow).Node("only").Render(), false);
        }
    }
}
