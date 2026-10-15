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
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.More;
using Jube.Charting.More.Models;
using Xunit;

namespace Jube.Test.Charting.More
{
    public class StreamLegendTests
    {
        [Fact]
        public void SilhouetteBaselineCentresTheStack()
        {
            StreamChart.Baseline([[1.0, 2], [3.0, 2]], StreamOffset.Silhouette).Should().Equal(-2.0, -2.0);
        }

        [Fact]
        public void WiggleBaselineMatchesTheHandWorkedAlgorithm()
        {
            var baseline = StreamChart.Baseline([[1.0, 2, 3], [2.0, 2, 2]], StreamOffset.Wiggle);
            baseline[0].Should().BeApproximately(0, 1e-12);
            baseline[1].Should().BeApproximately(-0.75, 1e-12);
            baseline[2].Should().BeApproximately(-1.45, 1e-12);
            StreamChart.Baseline([[1.0, 2], [3.0, 2]], StreamOffset.Zero).Should().Equal(0.0, 0.0);
        }

        [Fact]
        public void InsideOutOrderPutsEarlyPeaksOnOneSideAndLatePeaksOnTheOther()
        {
            var values = new IReadOnlyList<double>[]
            {
                [9.0, 1, 0, 0], [0.0, 8, 0, 0], [0.0, 0, 6, 0], [0.0, 0, 0, 4]
            };

            StreamChart.StackOrder(values, StreamOrder.InsideOut).Should().Equal(3, 0, 1, 2);
            StreamChart.StackOrder(values, StreamOrder.None).Should().Equal(0, 1, 2, 3);
            StreamChart.StackOrder(values, StreamOrder.Descending).Should().Equal(0, 1, 2, 3);
        }

        [Fact]
        public void StreamChartsDrawOneShapePerSeriesWithSlotMarks()
        {
            var svg = MoreSamples.Stream(false).Render();
            Regex.Matches(svg, "<path class=\"st\"").Count.Should().Be(6);
            Regex.Matches(svg, "class=\"mk s\\d\"").Count.Should().Be(6 * 48);
            svg.Should().Contain("Share:").And.Contain("class=\"sbar\"");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void PercentStreamsShowAPercentAxisAndSharesSumToOne()
        {
            var svg = MoreSamples.Stream(true).Render();
            svg.Should().Contain(">100%<").And.NotContain("class=\"sbar\"");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void StreamsRejectBadInputAndTreatNaNAsZero()
        {
            Action negative = () => Chart.Stream().X([0.0, 1]).Series("a", [1.0, -1]).Render();
            negative.Should().Throw<ChartSpecificationException>();
            Action length = () => Chart.Stream().X([0.0, 1]).Series("a", [1.0]).Render();
            length.Should().Throw<ChartSpecificationException>();
            Action order = () => Chart.Stream().X([1.0, 1]).Series("a", [1.0, 1]).Render();
            order.Should().Throw<ChartSpecificationException>();
            var svg = Chart.Stream().X([0.0, 1, 2]).Series("a", [1.0, double.NaN, 2]).Series("b", [1.0, 1, 1]).Render();
            MoreAssert.IsSafe(svg);
            Chart.Stream().Render().Should().Contain("No data");
            var many = Chart.Stream().X([0.0, 1]);

            for (var i = 0; i < ChartLimits.MaxSeries + 1; i++)
            {
                many.Series("s" + i, [1.0, 1]);
            }

            Action cap = () => many.Render();
            cap.Should().Throw<ChartLimitException>();
        }

        private static string[] LegendTexts(string svg)
        {
            return
            [
                .. Regex.Matches(svg,
                        "<g class=\"lg[^\"]*\">.*?<text class=\"lgt\" x=\"[0-9.]+\" y=\"([0-9.]+)\">([^<]*)</text>")
                    .Select(m => m.Groups[1].Value + "|" + m.Groups[2].Value)
            ];
        }

        [Fact]
        public void LegendWrapsIntoAtMostTheRequestedRowsWithAMoreItem()
        {
            var svg = MoreSamples.LegendMany().Render();
            var items = LegendTexts(svg);
            items.Select(i => i.Split('|')[0]).Distinct().Count().Should().BeLessThanOrEqualTo(2);
            items.Last().Should().MatchRegex("\\|\\+\\d+ more$");
            var shown = items.Count(i => i.Contains("Series number"));
            items.Last().Should().EndWith("+" + (24 - shown) + " more");
            MoreAssert.IsSafe(svg);
        }

        [Fact]
        public void UnlimitedLegendsAreUnchangedByDefaultAndAutoCapsAtThreeRows()
        {
            LegendTexts(Chart.Line().Title("t").Series("a", [1.0, 2]).Series("b", [2.0, 1]).Render()).Should()
                .HaveCount(2);

            var builder = Chart.Line();

            for (var k = 0; k < 40; k++)
            {
                builder.Series("Long series name " + k, [1.0 + k, 2]);
            }

            var unlimited = LegendTexts(builder.Render());
            unlimited.Should().HaveCount(40);
            unlimited.Select(i => i.Split('|')[0]).Distinct().Count().Should().BeGreaterThan(3);
            var auto = LegendTexts(builder.LegendMaxRows(-1).Render());
            auto.Select(i => i.Split('|')[0]).Distinct().Count().Should().BeLessThanOrEqualTo(3);
            auto.Last().Should().MatchRegex("\\+\\d+ more$");
        }

        [Fact]
        public void RightAndBottomLegendsAlsoOverflowIntoAMoreItem()
        {
            var right = Chart.Line().Legend(LegendPosition.Right).LegendMaxRows(4);
            var bottom = Chart.Line().Legend(LegendPosition.Bottom).LegendMaxRows(1);

            for (var k = 0; k < 12; k++)
            {
                right.Series("Series " + k, [1.0 + k, 2]);
                bottom.Series("Series " + k, [1.0 + k, 2]);
            }

            var r = LegendTexts(right.Render());
            r.Should().HaveCount(4);
            r.Last().Should().EndWith("+9 more");
            var b = LegendTexts(bottom.Render());
            b.Select(i => i.Split('|')[0]).Distinct().Should().HaveCount(1);
            b.Last().Should().MatchRegex("\\+\\d+ more$");

            var autoRight = LegendTexts(Chart.Line().Legend(LegendPosition.Right).LegendMaxRows(-1).Size(640, 200)
                .Series("a", [1.0, 2]).Series("b", [1.0, 2]).Render());
            autoRight.Should().HaveCount(2);
        }

        [Fact]
        public void TheMoreItemIsNotLinkedToAnySeries()
        {
            var svg = MoreSamples.LegendMany().Render();
            svg.Should().Contain("<g class=\"lg more\">");
            svg.Should().NotContain("class=\"lg s23\"");
        }
    }
}