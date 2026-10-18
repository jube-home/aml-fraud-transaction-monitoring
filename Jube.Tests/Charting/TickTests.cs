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
using FluentAssertions;
using Jube.Charting;
using Jube.Charting.Ticks;
using Xunit;
using Jube.Charting.Exceptions;

namespace Jube.Test.Charting
{
    public class TickTests
    {
        [Theory]
        [InlineData(0, 1, 5, "0,0.25,0.5,0.75,1")]
        [InlineData(0, 100, 5, "0,25,50,75,100")]
        [InlineData(0, 10, 5, "0,2.5,5,7.5,10")]
        [InlineData(0, 1000, 5, "0,250,500,750,1000")]
        [InlineData(-1, 1, 5, "-1,-0.5,0,0.5,1")]
        [InlineData(0.1, 9.7, 5, "0,2.5,5,7.5,10")]
        [InlineData(0, 7, 5, "0,2,4,6,8")]
        [InlineData(13, 97, 6, "0,20,40,60,80,100")]
        [InlineData(0, 0.3, 5, "0,0.1,0.2,0.3")]
        [InlineData(1234, 5678, 5, "1000,2000,3000,4000,5000,6000")]
        [InlineData(-50, 150, 4, "-50,0,50,100,150")]
        [InlineData(0, 1, 3, "0,0.5,1")]
        public void ExtendedWilkinsonKnownAnswers(double min, double max, int target, string expected)
        {
            var ticks = ExtendedTicks.Generate(min, max, target);

            string.Join(",", ticks.Select(t => t.ToString("0.####", CultureInfo.InvariantCulture)))
                .Should().Be(expected);
        }

        [Theory]
        [InlineData(0.13, 0.87)]
        [InlineData(-3.3, 12.4)]
        [InlineData(1e-6, 4e-6)]
        [InlineData(1e6, 9e6)]
        [InlineData(0, 1)]
        public void LooseTicksAlwaysCoverTheData(double min, double max)
        {
            var ticks = ExtendedTicks.Generate(min, max, 5, true);
            ticks[0].Should().BeLessOrEqualTo(min + 1e-9);
            ticks[^1].Should().BeGreaterOrEqualTo(max - 1e-9);
        }

        [Fact]
        public void TicksAreEvenlySpacedAndIncreasing()
        {
            var ticks = ExtendedTicks.Generate(3.7, 91.2, 6);
            var step = ticks[1] - ticks[0];

            for (var i = 1; i < ticks.Count; i++)
            {
                (ticks[i] - ticks[i - 1]).Should().BeApproximately(step, 1e-9);
            }
        }

        [Fact]
        public void ReversedAndDegenerateBoundsAreHandled()
        {
            ExtendedTicks.Generate(10, 0).Should().Equal(ExtendedTicks.Generate(0, 10));
            ExtendedTicks.Generate(5, 5).Should().Equal(5);
        }

        [Fact]
        public void NonFiniteBoundsAreRejected()
        {
            var act = () => ExtendedTicks.Generate(double.NaN, 1);
            act.Should().Throw<ChartSpecificationException>();
        }

        [Fact]
        public void LogTicksAreDecades()
        {
            LogTicks.Generate(1, 1000).Should().Equal(1, 10, 100, 1000);
            LogTicks.Generate(3, 400).Should().Equal(5, 10, 20, 50, 100, 200);
        }

        [Fact]
        public void TimeTicksAlignToCalendarMonths()
        {
            var ticks = TimeTicks.Generate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

            ticks.Select(t => t.Label).Should().Equal("Jan 2026", "Feb 2026", "Mar 2026", "Apr 2026");
        }

        [Fact]
        public void TimeTicksUseClockLabelsWithinADay()
        {
            var ticks = TimeTicks.Generate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 1, 1, 6, 0, 0, DateTimeKind.Utc));

            ticks.Select(t => t.Label).Should().Equal("00:00", "01:00", "02:00", "03:00", "04:00", "05:00", "06:00");
        }

        [Fact]
        public void TimeTicksUseYearsForLongSpans()
        {
            var ticks = TimeTicks.Generate(new DateTime(2000, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            ticks.Select(t => t.Label).Should().Equal("2005", "2010", "2015", "2020", "2025");
        }

        [Theory]
        [InlineData(0, "0")]
        [InlineData(1500, "1.5k")]
        [InlineData(25000, "25k")]
        public void AxisLabelsUseCompactSuffixes(double value, string expected)
        {
            NumberFormat.Axis(value, 500, 50000).Should().Be(expected);
        }

        [Theory]
        [InlineData(0.0, "0")]
        [InlineData(1234.5678, "1235")]
        [InlineData(0.012345, "0.01235")]
        [InlineData(double.NaN, "n/a")]
        [InlineData(-0.5, "-0.5")]
        public void ValueFormattingIsInvariantAndBounded(double value, string expected)
        {
            NumberFormat.Value(value).Should().Be(expected);
        }

        [Fact]
        public void ValueFormattingIgnoresCurrentCulture()
        {
            var previous = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                NumberFormat.Value(1.5).Should().Be("1.5");
                Chart.Line().Series("s", [1.5, 2.5]).Render().Should().NotContain("1,5");
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }
    }
}