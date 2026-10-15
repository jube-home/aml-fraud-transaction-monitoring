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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting.Diagrams;
using Xunit;
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class ComponentsDiagramTests
    {
        private static double[] BarHeights(string svg)
        {
            return
            [
                .. XDocument.Parse(svg).Descendants()
                    .Where(e => e.Name.LocalName == "rect" && (string)e.Attribute("class")! == "rb")
                    .Select(e =>
                        double.Parse((string)e.Attribute("height")!, CultureInfo.InvariantCulture))
            ];
        }

        [Fact]
        public void ScreeBarsAreProportionalToExplainedVariance()
        {
            var heights = BarHeights(DiagramSamples.Pca().Render());
            heights.Should().HaveCount(6).And.BeInDescendingOrder();
            (heights[1] / heights[0]).Should().BeApproximately(0.21 / 0.34, 0.02);
        }

        [Fact]
        public void LoadingsHeatmapHasOneCellPerFeatureAndComponentWithSymmetricColours()
        {
            var svg = DiagramSamples.Pca().Render();
            SvgAssert.IsSafe(svg, false);
            Regex.Matches(svg, "class=\"cell d\\d+\"").Count.Should().Be(8 * 6 + 11);
            svg.Should().Contain("class=\"cell d10\"").And.Contain("class=\"cell d1\"");
            svg.Should().Contain("Share of component: ").And.Contain("Cumulative: 93%");
            svg.Should().Contain("Top loadings: amount_z 0.52");
        }

        [Fact]
        public void FeaturesKeepTheirDeclaredOrder()
        {
            DiagramSamples.Pca().Build().ShownFeatures().Should().Equal(Enumerable.Range(0, 8));
        }

        [Fact]
        public void HoverLinksTheScreeBarsAndTheHeatmapColumns()
        {
            var svg = DiagramSamples.Pca().Render();
            svg.Should().Contain(":has(.pcb.c0:hover,.pcb.c0:focus-visible) .hc:not(.cc0)");
            svg.Should().Contain(":has(.hc.rr0.cc0:hover,.hc.rr0.cc0:focus-visible) .pcb:not(.c0)");
            svg.Should().Contain("class=\"tp tp0\"").And.Contain("class=\"tp th0x0\"");
            svg.Should().NotContain("}.pcb").And.NotContain("}.hc");
        }

        [Fact]
        public void ManyFeaturesAndComponentsAreElidedWithNotes()
        {
            var features = Enumerable.Range(0, 100).Select(i => "f" + i).ToArray();
            var b = Diagram.Components().Features(features);

            for (var c = 0; c < 20; c++)
            {
                var number = c + 1;

                b.Component("PC" + number, 0.04,
                    features.Select((_, i) => Math.Sin(i * number) * (i % 7 == 0 ? 2 : 1)));
            }

            var svg = b.Render();
            svg.Should().Contain("first 12 of 20 components").And.Contain("top 40 of 100 features by loading");
            Regex.Matches(svg, "class=\"cell d\\d+\"").Count.Should().Be(40 * 12 + 11);
            b.Build().ShownFeatures().Should().HaveCount(40).And.BeInAscendingOrder();
        }

        [Fact]
        public void ClustersAreSortedBySizeWithProportionalBars()
        {
            var svg = DiagramSamples.Clusters().Render();
            SvgAssert.IsSafe(svg, false);

            var heights = XDocument.Parse(svg).Descendants()
                .Where(e => e.Name.LocalName == "rect" && (string)e.Attribute("class")! == "rb")
                .Select(e =>
                    double.Parse((string)e.Attribute("width")!, CultureInfo.InvariantCulture))
                .ToArray();

            heights.Should().HaveCount(5).And.BeInDescendingOrder();
            (heights[1] / heights[0]).Should().BeApproximately(1800.0 / 5200, 0.01);
            Regex.Matches(svg, "class=\"cell d\\d+\"").Count.Should().Be(5 * 9 + 11);
            svg.Should().Contain("5200 · 54%").And.Contain("Defining features: ").And.Contain("Standardised value");
            svg.Should().Contain(":has(.hc.rr0:hover,.hc.rr0:focus-visible) .hc:not(.rr0)");
        }

        [Fact]
        public void ClusterFeatureColumnsAreCappedToTheWidestSpread()
        {
            var features = Enumerable.Range(0, 60).Select(i => "f" + i).ToArray();

            var b = Diagram.Components().Features(features)
                .Cluster("a", 10, features.Select((_, i) => i == 41 ? 3.0 : 0.1))
                .Cluster("b", 5, features.Select((_, i) => i == 7 ? -2.5 : 0.0));

            var shown = b.Build().ShownFeatures();
            shown.Should().HaveCount(24).And.Contain(41).And.Contain(7);
            b.Render().Should().Contain("showing 24 of 60 features with the widest spread");
        }

        [Fact]
        public void InvalidCombinationsAndLimitsAreReported()
        {
            var one = Diagram.Components().Features(["a"]).Component("PC1", 0.5, [1.0]).Cluster("c", 1, [1.0]);
            one.Invoking(b => b.Render()).Should().Throw<DiagramNotSupportedException>();

            Diagram.Components().Features(["a"]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Components().Component("PC1", 0.5, [1.0]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Components().Features(["a", "b"]).Component("PC1", 0.5, [1.0]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Components().Features(["a"]).Component("PC1", 1.5, [1.0]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Components().Features(["a"]).Component("PC1", 0.7, [1.0]).Component("PC2", 0.7, [1.0])
                .Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>().WithMessage("*more than 1*");

            Diagram.Components().Features(["a"]).Component("PC1", 0.5, [double.NaN]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Diagram.Components().Features(["a"]).Cluster("c", -1, [1.0]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            var features = Enumerable.Range(0, 201).Select(i => "f" + i).ToArray();

            Diagram.Components().Features(features).Cluster("c", 1, features.Select(_ => 0.0)).Invoking(b => b.Render())
                .Should().Throw<DiagramTooLargeException>().Which.Limit.Should().Be("features");

            var comps = Diagram.Components().Features(["a"]);

            for (var i = 0; i < 41; i++)
            {
                comps.Component("PC" + i, 0.01, [1.0]);
            }

            comps.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("components");

            var clusters = Diagram.Components().Features(["a"]);

            for (var i = 0; i < 41; i++)
            {
                clusters.Cluster("c" + i, 1, [1.0]);
            }

            clusters.Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which.Limit.Should()
                .Be("clusters");
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.Pca().Render().Should().Be(DiagramSamples.Pca().Render());
            DiagramSamples.Clusters().Render().Should().Be(DiagramSamples.Clusters().Render());
        }
    }
}