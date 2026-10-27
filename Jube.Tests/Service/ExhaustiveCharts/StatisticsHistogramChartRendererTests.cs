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
using System.Xml;
using FluentAssertions;
using Jube.Dto.Payload;
using Jube.Service.Charts;
using Xunit;

namespace Jube.Test.Service.ExhaustiveCharts
{
    [Trait("Category", "Service")]
    public sealed class StatisticsHistogramChartRendererTests
    {
        private static ColumnStatistics Statistics(params (double Bin, int Frequency)[] buckets)
        {
            return new ColumnStatistics(0, 40, 12.5, 10, 8.25,
                [.. buckets.Select(b => new HistogramBucket(b.Bin, b.Frequency))]);
        }

        private static StatisticsHistogramChartRequest Request(ColumnStatistics statistics, string title = "Total",
            bool dark = false, string? color = null)
        {
            return new StatisticsHistogramChartRequest(title, "ms", dark, color, statistics);
        }

        private static List<string> Titles(string svg)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.LoadXml(svg);

            return [.. document.GetElementsByTagName("title").Cast<XmlNode>().Select(n => n.InnerText)];
        }

        [Fact]
        public void ABarPerBucketIsDrawnWithAMeanAndMedianLineAndHoverText()
        {
            var svg = StatisticsHistogramChartRenderer.Render(
                Request(Statistics((0, 5), (10, 12), (20, 7), (30, 2))));

            Regex.Matches(svg, "class=\"ov m\"").Count.Should().Be(4);
            Regex.Matches(svg, "<line class=\"ref\"").Count.Should().Be(2);

            var titles = Titles(svg);

            titles.Should().Contain(t => t.StartsWith("Mean"));
            titles.Should().Contain(t => t.StartsWith("Median"));
            titles.Should().Contain(t => t.Contains("Rows: 12"));
            svg.Should().Contain("Value (ms)");
        }

        [Fact]
        public void AnEmptyOversizedOrNonFiniteHistogramIsRefusedNeverRendered()
        {
            var tooMany = Enumerable.Range(0, StatisticsHistogramChartRenderer.MaximumBuckets + 1)
                .Select(i => ((double)i, 1)).ToArray();

            var empty = () => StatisticsHistogramChartRenderer.Render(Request(Statistics()));
            var oversized = () => StatisticsHistogramChartRenderer.Render(Request(Statistics(tooMany)));
            var infinite = () =>
                StatisticsHistogramChartRenderer.Render(Request(Statistics((double.PositiveInfinity, 1))));

            empty.Should().Throw<ArgumentException>();
            oversized.Should().Throw<ArgumentException>();
            infinite.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void OneBucketOrBinsThatRoundedToTheSameEdgeStillRender()
        {
            var one = StatisticsHistogramChartRenderer.Render(Request(Statistics((5, 9))));
            var collided = StatisticsHistogramChartRenderer.Render(Request(Statistics((1, 3), (1, 4), (1, 5))));

            one.Should().Contain("<svg");
            Regex.Matches(collided, "class=\"ov m\"").Count.Should().Be(3);
        }

        [Fact]
        public void TheSeriesTakesTheCallersColourAndAnInvalidColourIsRefused()
        {
            var statistics = Statistics((0, 5), (10, 12));
            var coloured = StatisticsHistogramChartRenderer.Render(Request(statistics, color: "#005f7f"));
            var invalid = () => StatisticsHistogramChartRenderer.Render(Request(statistics, color: "navy"));

            coloured.Should().ContainEquivalentOf("#005f7f");
            invalid.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TheThemeIsChosenByTheCallerAndAHostileTitleIsEscaped()
        {
            var statistics = Statistics((0, 5), (10, 12));
            var light = StatisticsHistogramChartRenderer.Render(Request(statistics));
            var dark = StatisticsHistogramChartRenderer.Render(Request(statistics, dark: true));
            var hostile = StatisticsHistogramChartRenderer.Render(Request(statistics, "<script>alert(1)</script>"));

            light.Should().NotBe(dark);
            hostile.Should().NotContainEquivalentOf("<script");
            new XmlDocument { XmlResolver = null }.Invoking(d => d.LoadXml(hostile)).Should().NotThrow();
        }
    }
}
