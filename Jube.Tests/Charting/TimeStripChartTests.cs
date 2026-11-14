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
using System.Xml.Linq;
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Core.Models;
using Jube.Charting.Exceptions;
using Jube.Charting.Interfaces;
using Xunit;

namespace Jube.Test.Charting
{
    public class TimeStripChartTests
    {
        private static readonly DateTimeOffset start = new(2026, 3, 2, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void EveryBarCarriesItsBinIndexAndUnixRange()
        {
            var document = XDocument.Parse(ChartSamples.TimeStrip().Render());

            var bars = document.Descendants().Where(e => e.Name.LocalName == "g" && e.Attribute("data-bin") is not null)
                .ToList();

            bars.Should().HaveCount(24);
            bars.Select(b => b.Attribute("data-bin")!.Value).Should().Equal(Enumerable.Range(0, 24).Select(i => i.ToString()));
            bars[0].Attribute("data-from")!.Value.Should().Be(start.ToUnixTimeSeconds().ToString());
            bars[0].Attribute("data-to")!.Value.Should().Be(start.AddHours(1).ToUnixTimeSeconds().ToString());
            bars[23].Attribute("data-to")!.Value.Should().Be(start.AddHours(24).ToUnixTimeSeconds().ToString());
            bars.Should().OnlyContain(b => b.Descendants().Any(e => e.Name.LocalName == "rect"));
        }

        [Fact]
        public void HasABaselineAndThreeTickLabels()
        {
            var document = XDocument.Parse(ChartSamples.TimeStrip().Render());
            document.Descendants().Count(e => e.Name.LocalName == "line" && e.Attribute("class")?.Value == "ax").Should().Be(1);

            var ticks = document.Descendants().Where(e => e.Name.LocalName == "text" && (e.Attribute("class")?.Value ?? string.Empty).StartsWith("tk"))
                .ToList();

            ticks.Select(t => t.Attribute("class")!.Value).Should().Equal("tk", "tk ctr", "tk end");
            ticks.Select(t => t.Value).Should().Equal("00:00", "12:00", "00:00");
        }

        [Fact]
        public void RawTimestampsAreBinnedIntoTheRequestedNumberOfBins()
        {
            var stamps = Enumerable.Range(0, 100).Select(i => start.AddMinutes(i * 7)).ToList();
            var chart = Chart.TimeStrip().Times(stamps, 10).Build();
            chart.Spec.Bins.Should().HaveCount(10);
            chart.Spec.Bins.Sum(b => b.Count).Should().Be(100);
            chart.Spec.Bins.Select(b => b.From).Should().BeInAscendingOrder();
            chart.Spec.Bins[0].From.Should().Be(start);
            chart.Spec.Bins[9].To.Should().BeOnOrAfter(stamps[^1]);
            SvgAssert.IsSafe(chart.Render());
        }

        [Fact]
        public void RenderingIsDeterministicSafeAndThemed()
        {
            var svg = ChartSamples.TimeStrip().Render();
            svg.Should().Be(ChartSamples.TimeStrip().Render());
            SvgAssert.IsSafe(svg);
            svg.Should().Contain("prefers-color-scheme:dark").And.Contain("class=\"tt\"").And.Contain("tabindex=\"0\"");
            svg.Should().Contain("Movements: 14");
        }

        [Fact]
        public void ExternalStyleModeDropsTheStyleElementAndKeepsTheData()
        {
            var external = ((IStyleModeChart)ChartSamples.TimeStrip()).WithStyleMode(ChartStyleMode.External).Render();
            external.Should().NotContain("<style").And.Contain("data-bin=\"5\"");
            external.Should().Contain("class=\"jc\"");
        }

        [Fact]
        public void EmptyAndMalformedStripsAreHandled()
        {
            var empty = Chart.TimeStrip().Render();
            SvgAssert.IsSafe(empty);
            empty.Should().Contain("class=\"ax\"").And.NotContain("data-bin");

            Chart.TimeStrip().Bins([(start, -1)]).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();

            Chart.TimeStrip().Times([start], 0).Invoking(b => b.Render()).Should()
                .Throw<ChartSpecificationException>();
        }
    }
}
