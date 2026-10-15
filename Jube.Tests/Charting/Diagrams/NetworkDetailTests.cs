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
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Diagrams;
using Jube.Charting.Diagrams.Models;
using Jube.Test.Charting.External;
using Jube.Test.Charting.Performance;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class NetworkDetailTests
    {
        private static NetworkDiagram Build(int inputs, int[] hidden, int outputs,
            NetworkDetail detail = NetworkDetail.Auto)
        {
            return PerformanceSamples.NetworkBuilder(inputs, hidden, outputs, detail).Build();
        }

        private static List<string> Texts(string svg)
        {
            return
            [
                .. XDocument.Parse(svg).Descendants().Where(e => e.Name.LocalName is "text" or "title")
                    .Select(e => e.Value)
            ];
        }

        [Theory]
        [InlineData(2, new[] { 2 }, 1, NetworkDetail.Full)]
        [InlineData(4, new[] { 3 }, 1, NetworkDetail.Full)]
        [InlineData(6, new[] { 8 }, 1, NetworkDetail.NoWeightLabels)]
        [InlineData(26, new[] { 5 }, 1, NetworkDetail.NoWeightLabels)]
        [InlineData(47, new[] { 10 }, 1, NetworkDetail.NoWeightLabels)]
        [InlineData(60, new[] { 10 }, 1, NetworkDetail.Elided)]
        [InlineData(100, new[] { 40, 10 }, 1, NetworkDetail.Elided)]
        [InlineData(200, new[] { 100, 50 }, 1, NetworkDetail.Density)]
        [InlineData(1000, new[] { 500 }, 1, NetworkDetail.Density)]
        [InlineData(100, new[] { 5 }, 1, NetworkDetail.Elided)]
        [InlineData(130, new[] { 5 }, 1, NetworkDetail.Density)]
        public void AutoChoosesTheDetailFromTheSize(int inputs, int[] hidden, int outputs, NetworkDetail expected)
        {
            Build(inputs, hidden, outputs).Detail.Should().Be(expected);
            NetworkDetails.Resolve(Build(inputs, hidden, outputs).Spec).Should().Be(expected);
        }

        [Fact]
        public void AnUnweightedHugeNetworkIsElidedNeverADensityViewBecauseThereAreNoMagnitudes()
        {
            var svg = Diagram.Network().Input(Enumerable.Range(0, 500).Select(i => "a" + i)).Detail(NetworkDetail.Auto)
                .Output(Enumerable.Range(0, 300).Select(i => "b" + i), null).Build();

            svg.Detail.Should().Be(NetworkDetail.Elided);
            SvgAssert.IsSafe(svg.Render(), false);
        }

        [Fact]
        public void TheLibraryDefaultIsUnchangedSoExistingDiagramsAreByteIdentical()
        {
            var spec = Diagram.Network().Input(["a", "b"]).Output(["y"], [[1.0, 2.0]]).Build().Spec;

            spec.Detail.Should().Be(NetworkDetail.Full);
            DiagramSamples.Wide().Build().Detail.Should().Be(NetworkDetail.Full);
            DiagramSamples.Wide().Build().Layout().Nodes.Where(n => n.Layer == 0).Should().HaveCount(24);
        }

        [Fact]
        public void ARaisedCapShowsEveryNodeOfAModerateNetworkWithoutAggregatesOrWeightLabels()
        {
            var diagram = Build(40, [12], 1);

            var layout = diagram.Layout();
            var svg = diagram.Render();

            diagram.Detail.Should().Be(NetworkDetail.NoWeightLabels);
            layout.Nodes.Should().OnlyContain(n => !n.IsAggregate);
            layout.Nodes.Count(n => n.Layer == 0).Should().Be(40);
            svg.Should().NotContain("nodes aggregated");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void ElidedKeepsTheStrongestConnectionsAndSaysWhatWasLeftOut()
        {
            var diagram = Build(90, [40, 8], 1);

            var layout = diagram.Layout();
            var svg = diagram.Render();

            diagram.Detail.Should().Be(NetworkDetail.Elided);
            layout.Nodes.Should().Contain(n => n.IsAggregate);
            layout.Edges.Count.Should().BeLessThanOrEqualTo(NetworkDetails.RaisedEdges);
            Texts(svg).Should().Contain(t => t.Contains("nodes aggregated"));
            Texts(svg).Should().Contain(t => t.Contains("connections shown"));
        }

        [Fact]
        public void TheDensityViewStatesLayersElementsWeightsAndDensityAndDrawsAHeatmapPerLayerPair()
        {
            var svg = Build(200, [100, 50], 1).Render();
            var text = Texts(svg);

            text.Should().Contain("Weight magnitude between layers, rows are the earlier layer and columns the next");
            text.Should().Contain("200 inputs");
            text.Should().Contain("100 PEs");
            text.Should().Contain("50 PEs");
            text.Should().Contain("1 PE");

            text.Should().Contain(t =>
                t.StartsWith("151 processing elements (neurons) in 3 weighted layers, 200 inputs, 25050 weights"));

            text.Should().Contain("Input to Hidden 1");
            text.Should().Contain("Hidden 1 to Hidden 2");
            text.Should().Contain("Hidden 2 to Output");
            text.Should().Contain("200 x 100 = 20000 weights");
            text.Should().Contain(t => t.Contains("of weights are at least"));
            text.Should().Contain("Mean magnitude");
            text.Should().Contain(t => t.StartsWith("Input 1 to 12 to Hidden 1 1 to 6"));
            svg.Should().Contain("cell m q");
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void DensityCellsAggregateMeanMagnitudeAndStrongestWeightExactly()
        {
            double[][] weights =
            [
                [1.0, -3.0, 0.0],
                [2.0, 0.5, -4.0],
                [0.25, 0.25, 0.25],
                [double.NaN, 1.0, 1.0]
            ];

            var svg = Diagram.Network().Input(["a", "b", "c"]).Hidden(["p", "q", "r", "s"], weights)
                .Output(["y"], [[1.0, 1.0, 1.0, 1.0]]).Detail(NetworkDetail.Density).Build().Render();

            var text = Texts(svg);

            text.Should().Contain(t => t.StartsWith("Input 1 to Hidden 1 1"));
            text.Should().Contain("Mean magnitude: 1");
            text.Should().Contain("Strongest weight: 1");
            text.Should().Contain("Strongest weight: 4");
            text.Should().Contain("Weights in the cell: 1");
            text.Should().Contain("Input to Hidden 1");
            text.Should().Contain("3 x 4 = 12 weights");
            text.Should().Contain(t => t.StartsWith("Hidden 1") && t.Contains("Incoming weights: 12"));
        }

        [Fact]
        public void DensityCellsAreAggregatedWhenTheLayersAreWide()
        {
            var diagram = Build(1000, [500], 1);

            var svg = diagram.Render();

            diagram.Detail.Should().Be(NetworkDetail.Density);
            Texts(svg).Should().Contain(t => t.Contains("Weights in the cell: ") && t.Contains("Mean magnitude"));

            var cells = XDocument.Parse(svg).Descendants()
                .Count(e => e.Attribute("class")?.Value.StartsWith("cell m") == true);
            cells.Should().BeLessThanOrEqualTo(600);
            cells.Should().BeGreaterThan(50);
        }

        [Theory]
        [InlineData(200, new[] { 100, 50 })]
        [InlineData(1000, new[] { 500 })]
        [InlineData(2000, new[] { 1000, 200 })]
        public void LargeNetworksRenderQuicklyAndWithinTheOutputCaps(int inputs, int[] hidden)
        {
            var diagram = Build(inputs, hidden, 1);
            var watch = Stopwatch.StartNew();

            var inline = diagram.Render();
            var external = ExternalSamples.ToExternal(diagram).Render();

            watch.Stop();
            watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
            inline.Length.Should().BeLessThan(ChartLimits.MaxOutputCharacters);
            external.Length.Should().BeLessThan(600_000);
            ExternalStyleTests.AssertExternalSafe(external, false);
            XDocument.Parse(inline);
        }

        [Fact]
        public void TheDensityViewIsInteractiveEvenWhenTheNetworkIsHuge()
        {
            var svg = Build(1000, [500], 1).Render();

            svg.Should().Contain("tabindex=\"0\"");
            Texts(svg).Should().Contain(t => t.Contains("Incoming weights: 500"));
        }

        [Fact]
        public void ANetworkWithNoFiniteWeightsIsStillRefusedInTheDensityView()
        {
            var act = () => Diagram.Network().Input(["a", "b"]).Output(["y"], [[double.NaN, double.NaN]])
                .Detail(NetworkDetail.Density).Build().Render();

            act.Should().Throw<DiagramNotSupportedException>();
        }

        [Fact]
        public void TooManyLayersIsStillTooLargeInTheDensityView()
        {
            var b = Diagram.Network().Input(["a"]).Detail(NetworkDetail.Density);

            for (var i = 0; i < 20; i++)
            {
                b.Hidden(["h"], [[1.0]]);
            }

            var act = () => b.Output(["o"], [[1.0]]).Render();

            act.Should().Throw<DiagramTooLargeException>().Which.Limit.Should().Be("network layers");
        }

        [Fact]
        public void AnUnweightedNetworkInTheDensityViewShowsTheBlocksWithoutHeatmaps()
        {
            var svg = Diagram.Network().Input(["a", "b", "c"]).Output(["y"], null).Detail(NetworkDetail.Density).Build()
                .Render();

            Texts(svg).Should().Contain("3 inputs");
            Texts(svg).Should().NotContain(t => t.StartsWith("Weight magnitude"));
            svg.Should().NotContain("cell m");
        }

        [Fact]
        public void EachDetailCanBeForcedAndTheSmallNetworkStillRenders()
        {
            foreach (var detail in new[]
                     {
                         NetworkDetail.Full, NetworkDetail.NoWeightLabels, NetworkDetail.Elided, NetworkDetail.Density
                     })
            {
                var diagram = Build(3, [2], 1, detail);

                diagram.Detail.Should().Be(detail);
                SvgAssert.IsSafe(diagram.Render(), false);
            }
        }

        [Fact]
        public void HostileLayerNamesAndLabelsCannotInjectMarkupInTheDensityView()
        {
            const string hostile = "<script>alert(1)</script>\"><foreignObject onload=x>&amp;";

            var diagram = Diagram.Network().Title(hostile).Subtitle(hostile)
                .Input([hostile, "b"], hostile)
                .Hidden([hostile], [[1.0, 2.0]], hostile, [0.5], hostile)
                .Output([hostile], [[1.0]], hostile, null, hostile)
                .Detail(NetworkDetail.Density).Build();

            foreach (var svg in new[] { diagram.Render(), ExternalSamples.ToExternal(diagram).Render() })
            {
                svg.Should().NotContain("<script");
                svg.Should().NotContain("<foreignObject");
                XDocument.Parse(svg);
            }
        }

        [Fact]
        public void DensityOutputIsDeterministic()
        {
            Build(200, [100, 50], 1).Render().Should().Be(Build(200, [100, 50], 1).Render());
        }

        [Fact]
        public void EmphasisShadesInputsAddsAKeyAndHoverDetail()
        {
            var svg = PerformanceSamples.Emphasis().Render();
            var text = Texts(svg);

            text.Should().Contain("Input sensitivity");
            text.Should().Contain("low");
            text.Should().Contain("high");
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.31"));
            text.Should().Contain(t => t.Contains("Input sensitivity: 0.14"));
            svg.Should().Contain("nq8");
            svg.Should().Contain("nq4");
            svg.Should().Contain("nq1");
        }

        [Fact]
        public void EmphasisWithoutATitleOrOutsideZeroToOneIsRejectedOrIgnored()
        {
            var outside = () => Diagram.Network().Input(["a", "b"]).Output(["y"], [[1.0, 1.0]])
                .InputEmphasis("s", [2.0, 0.1]).Render();

            var mismatch = () => Diagram.Network().Input(["a", "b"]).InputEmphasis("s", [0.5]);
            var ignored = Diagram.Network().Input(["a", "b"]).Output(["y"], [[1.0, 1.0]]).Build().Render();

            outside.Should().Throw<ChartSpecificationException>();
            mismatch.Should().Throw<ChartSpecificationException>();
            ignored.Should().NotContain("nq");
        }
    }
}