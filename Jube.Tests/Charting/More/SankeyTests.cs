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
using Jube.Charting.More;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.More
{
    public class SankeyTests
    {
        private static string ClassOf(string svg, string ariaStart)
        {
            return Regex.Match(svg,
                    "<g class=\"([^\"]*)\" tabindex=\"0\" role=\"graphics-symbol\" aria-label=\"" +
                    Regex.Escape(ariaStart))
                .Groups[1].Value;
        }

        [Fact]
        public void NodeTotalsAreTheSumsOfTheirLinks()
        {
            var svg = MoreSamples.Sankey().Render();
            svg.Should().Contain(">Inflow: 440<").And.Contain(">Outflow: 440<");
            svg.Should().Contain(">Inflow: 840<");
            svg.Should().Contain("76.7% of Reviewed outflow");
            svg.Should().Contain("100% of Cleared inflow");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void NodeHoverHighlightsTheWholeFlowThroughItAndNoOtherLinks()
        {
            var svg = MoreSamples.Sankey().Render();
            var cashToRules = ClassOf(svg, "Cash to Rules");
            cashToRules.Split(' ').Should().Contain("m3").And.Contain("m1").And.Contain("m5");
            cashToRules.Split(' ').Should().NotContain("m4");
            var cardToModel = ClassOf(svg, "Card to Model");
            cardToModel.Split(' ').Should().Contain("m0").And.Contain("m4").And.Contain("m5").And.NotContain("m2");
            svg.Should().Contain(":has(.nd.n4:hover) .lk:not(.m4) .lkp");
        }

        [Fact]
        public void LinkHoverHighlightsItsUpstreamAndDownstreamLinks()
        {
            var svg = MoreSamples.Sankey().Render();
            var reviewedToEscalated = ClassOf(svg, "Reviewed to Escalated");
            var tokens = reviewedToEscalated.Split(' ');

            tokens.Should().Contain("l9").And.Contain("k9").And.Contain("k11").And.Contain("k12").And.Contain("k5").And
                .Contain("k7").And.Contain("k0");

            tokens.Should().NotContain("k6").And.NotContain("k8").And.NotContain("k10");
            svg.Should().Contain(":has(.lk.l9:hover) .lk:not(.k9) .lkp");
        }

        [Fact]
        public void LargeGraphsFallBackToDirectLinkHighlighting()
        {
            var builder = Chart.Sankey();

            for (var i = 0; i < 60; i++)
            {
                builder.Link("s" + i, "t" + i % 5, 1 + i % 3);
            }

            var svg = builder.Render();
            svg.Should().Contain(":has(.nd.n0:hover)");
            svg.Should().NotContain(":has(.lk.l0:hover) .lk:not(.k0)");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void NonPositiveAndNonFiniteLinksAreSkippedAndJustifyMovesSinksRight()
        {
            var svg = Chart.Sankey().Link("a", "b", 5).Link("a", "c", double.NaN).Link("a", "d", -2).Link("a", "e", 0)
                .Link("b", "f", 2).Link("a", "sink", 3).Render();

            svg.Should().NotContain(">c<").And.NotContain(">d<").And.NotContain(">e<");
            MoreAssert.IsSafe(svg);

            var justified = Regex.Matches(svg, "<rect class=\"bar\" x=\"([0-9.]+)\"")
                .Select(m => double.Parse(m.Groups[1].Value)).ToArray();

            justified.Max().Should().BeGreaterThan(justified.Min());
        }

        [Fact]
        public void CyclesSelfLinksAndBadSpecsAreRejected()
        {
            Action cycle = () => Chart.Sankey().Link("a", "b", 1).Link("b", "c", 1).Link("c", "a", 1).Render();
            cycle.Should().Throw<ChartSpecificationException>();
            Action self = () => Chart.Sankey().Link("a", "a", 1).Render();
            self.Should().Throw<ChartSpecificationException>();
            Action width = () => Chart.Sankey().NodeWidth(1).Link("a", "b", 1).Render();
            width.Should().Throw<ChartSpecificationException>();
            Action colour = () => Chart.Sankey().Node("a", "red").Link("a", "b", 1).Render();
            colour.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void CapsAreEnforcedAndEmptyRenders()
        {
            var nodes = Chart.Sankey();

            for (var i = 0; i < SankeyChart.MaxNodes; i++)
            {
                nodes.Link("s" + i, "t" + i, 1);
            }

            Action many = () => nodes.Render();
            many.Should().Throw<ChartLimitException>();
            var links = Chart.Sankey();

            for (var i = 0; i < SankeyChart.MaxLinks + 1; i++)
            {
                links.Link("a", "b", 1);
            }

            Action tooMany = () => links.Render();
            tooMany.Should().Throw<ChartLimitException>();
            Chart.Sankey().Render().Should().Contain("No data");
        }

        [Fact]
        public void NodesCanBeDeclaredWithColoursAndLabelsAreTruncated()
        {
            var svg = Chart.Sankey().Size(300, 200).Node("Source", "#336699").Link("Source", new string('x', 500), 3)
                .Render();
            svg.Should().Contain("--sc:#336699");
            MoreAssert.IsSafe(svg);
        }
    }
}