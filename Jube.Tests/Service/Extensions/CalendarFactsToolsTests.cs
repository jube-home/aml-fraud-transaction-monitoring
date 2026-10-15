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
using FluentAssertions;
using Jube.Service.Extensions.CalendarFacts;
using Jube.Service.Extensions.CalendarFacts.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class CalendarFactsToolsTests
    {
        [Fact]
        [Trait("Covers", "DescribeDate")]
        public void DescribeDateReportsCalendarFacts()
        {
            var facts = CalendarFactsTools.DescribeDate(new DateTime(2028, 2, 29, 15, 0, 0));
            facts.Date.Should().Be(new DateTime(2028, 2, 29));
            facts.DayOfWeek.Should().Be("Tuesday");
            facts.DayOfYear.Should().Be(60);
            facts.IsoWeekOfYear.Should().Be(9);
            facts.Quarter.Should().Be(1);
            facts.DaysInMonth.Should().Be(29);
            facts.IsWeekend.Should().BeFalse();
            facts.IsLastDayOfMonth.Should().BeTrue();
            facts.IsLeapYear.Should().BeTrue();
        }

        [Theory]
        [InlineData("Day", "2026-09-23T00:00:00", "2026-09-24T00:00:00")]
        [InlineData("Week", "2026-09-21T00:00:00", "2026-09-28T00:00:00")]
        [InlineData("Month", "2026-09-01T00:00:00", "2026-10-01T00:00:00")]
        [InlineData("Quarter", "2026-07-01T00:00:00", "2026-10-01T00:00:00")]
        [InlineData("year", "2026-01-01T00:00:00", "2027-01-01T00:00:00")]
        [Trait("Covers", "PeriodBoundaries")]
        public void PeriodBoundariesSpanThePeriod(string period, string start, string nextStart)
        {
            var range = CalendarFactsTools.PeriodBoundaries(new DateTime(2026, 9, 23, 13, 45, 0), period);
            range.Start.Should().Be(DateTime.Parse(start));
            range.End.Should().Be(DateTime.Parse(nextStart).AddTicks(-1));
        }

        [Fact]
        [Trait("Covers", "PeriodBoundaries")]
        public void PeriodBoundariesSupportSundayWeeks()
        {
            CalendarFactsTools.PeriodBoundaries(new DateTime(2026, 9, 23), "Week", "Sunday").Start.Should()
                .Be(new DateTime(2026, 9, 20));
        }

        [Fact]
        [Trait("Covers", "FiscalPeriod")]
        [Trait("Covers", "DaysInMonth")]
        [Trait("Covers", "IsLeapYear")]
        public void FiscalAndYearFacts()
        {
            CalendarFactsTools.FiscalPeriod(new DateTime(2026, 8, 17), 4).Should().Be(new FiscalPeriodDetails(2027, 2));
            CalendarFactsTools.DaysInMonth(2026, 2).Should().Be(28);
            CalendarFactsTools.IsLeapYear(1900).Should().BeFalse();
            CalendarFactsTools.IsLeapYear(2000).Should().BeTrue();
            var act = () => CalendarFactsTools.DaysInMonth(2026, 13);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "IsWithinBusinessHours")]
        [Trait("Covers", "IsSameCalendarDay")]
        public void BusinessHoursAndSameDay()
        {
            CalendarFactsTools.IsWithinBusinessHours(new DateTime(2026, 9, 23, 9, 0, 0)).Should().BeTrue();
            CalendarFactsTools.IsWithinBusinessHours(new DateTime(2026, 9, 23, 17, 0, 0)).Should().BeFalse();
            CalendarFactsTools.IsWithinBusinessHours(new DateTime(2026, 9, 26, 10, 0, 0)).Should().BeFalse();

            CalendarFactsTools.IsWithinBusinessHours(new DateTime(2026, 9, 26, 10, 0, 0), weekdaysOnly: false).Should()
                .BeTrue();

            CalendarFactsTools
                .IsSameCalendarDay(new DateTime(2026, 9, 23, 1, 0, 0), new DateTime(2026, 9, 23, 23, 0, 0)).Should()
                .BeTrue();

            var act = () => CalendarFactsTools.IsWithinBusinessHours(DateTime.MinValue, 17, 9);
            act.Should().Throw<ArgumentException>();
        }
    }
}