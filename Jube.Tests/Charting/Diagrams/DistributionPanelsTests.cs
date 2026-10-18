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
using Jube.Charting.Diagrams.Exceptions;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting.Diagrams
{
    public class DistributionPanelsTests
    {
        [Fact]
        public void NormalDensityPeaksAtTheMeanAndIntegratesToOne()
        {
            var s = DistributionSeries.Normal("n", 3, 2, 401);
            s.Y.Max().Should().BeApproximately(1 / (2 * Math.Sqrt(2 * Math.PI)), 1e-9);
            s.X[s.Y.ToList().IndexOf(s.Y.Max())].Should().BeApproximately(3, 1e-9);
            var area = 0.0;

            for (var i = 1; i < s.X.Count; i++)
            {
                area += (s.X[i] - s.X[i - 1]) * (s.Y[i] + s.Y[i - 1]) / 2;
            }

            area.Should().BeApproximately(1, 0.001);
        }

        [Fact]
        public void EveryPanelGetsHoverColumnsWithAllSeriesInTheTooltip()
        {
            var svg = DiagramSamples.Distributions(4).Render();
            SvgAssert.IsSafe(svg, false);
            Regex.Matches(svg, "class=\"tp td\\d+c\\d+\"").Count.Should().Be(4 * 28);
            svg.Should().Contain("amount_z = ").And.Contain("legitimate: ").And.Contain("fraud: ");
            svg.Should().Contain("class=\"vl\"").And.Contain("class=\"cx s1\"");
            svg.Should().Contain("class=\"lg s0\"").And.Contain("class=\"lg s1\"");
            svg.Should().Contain("class=\"sr s0\"").And.Contain("class=\"ar\"");
        }

        [Fact]
        public void HistogramSeriesReportTheHeightOfTheBinUnderTheCursor()
        {
            var svg = DiagramSamples.Distributions(5).Render();
            svg.Should().Contain("class=\"db\"").And.Contain("legitimate: 0.34").And.Contain("fraud: 0.31");
        }

        [Fact]
        public void ColumnsPerPanelShrinkAsPanelsGrowSoMarksStayWithinBudget()
        {
            var svg = DiagramSamples.Distributions(24).Render();
            Regex.Matches(svg, "class=\"mk ").Count.Should().BeLessThanOrEqualTo(1200);
            SvgAssert.IsSafe(svg, false);
        }

        [Fact]
        public void TooManyPanelsSeriesOrPointsThrowDiagramTooLarge()
        {
            DiagramSamples.Distributions(25).Invoking(b => b.Render()).Should().Throw<DiagramTooLargeException>().Which
                .Limit.Should().Be("distribution panels");

            var series = Enumerable.Range(0, 9).Select(i => DistributionSeries.Normal("s" + i, i, 1)).ToArray();

            Diagram.Distributions().Panel("p", series).Invoking(b => b.Render()).Should()
                .Throw<DiagramTooLargeException>().Which.Limit.Should().Be("series per panel");

            var xs = Enumerable.Range(0, 401).Select(i => (double)i).ToArray();

            Diagram.Distributions().Panel("p", new DistributionSeries("s", xs, xs)).Invoking(b => b.Render()).Should()
                .Throw<DiagramTooLargeException>().Which.Limit.Should().Be("points per series");
        }

        [Fact]
        public void MalformedSpecificationsAreRejected()
        {
            Diagram.Distributions().Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();
            Diagram.Distributions().Panel("p").Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Distributions().Panel("p", new DistributionSeries("s", [1.0, 2], [1.0])).Invoking(b => b.Render())
                .Should().Throw<ChartSpecificationException>();

            Diagram.Distributions().Panel("p", new DistributionSeries("s", [2.0, 1], [1.0, 1]))
                .Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Distributions().Panel("p", new DistributionSeries("s", [1.0, 2], [1.0, -1]))
                .Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Distributions().Panel("p", new DistributionSeries("s", [1.0, 2], [1.0, double.NaN]))
                .Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>();

            Diagram.Distributions().Panel("a", DistributionSeries.Normal("s", 0, 1)).Panel("b",
                    DistributionSeries.Normal("s", 0, 1), DistributionSeries.Normal("t", 0, 1))
                .Invoking(b => b.Render()).Should().Throw<ChartSpecificationException>().WithMessage("*same series*");

            Action bad = () => DistributionSeries.Normal("s", 0, -1);
            bad.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void RenderingIsDeterministic()
        {
            DiagramSamples.Distributions(4).Render().Should().Be(DiagramSamples.Distributions(4).Render());
        }
    }
}