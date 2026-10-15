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
using Jube.Service.Extensions.DateArithmetic;
using Jube.Service.Extensions.DateArithmetic.Models;
using Xunit;

namespace Jube.Test.Service.Extensions
{
    [Trait("Category", "Unit")]
    public sealed class DateArithmeticToolsTests
    {
        [Fact]
        [Trait("Covers", "AddToDate")]
        public void AddToDateClampsMonthEnds()
        {
            DateArithmeticTools.AddToDate(new DateTime(2026, 1, 31), 1, "Months").Should()
                .Be(new DateTime(2026, 2, 28));

            DateArithmeticTools.AddToDate(new DateTime(2026, 1, 1), 1.5, "Days").Should()
                .Be(new DateTime(2026, 1, 2, 12, 0, 0));

            DateArithmeticTools.AddToDate(new DateTime(2026, 1, 1), -2, "weeks").Should()
                .Be(new DateTime(2025, 12, 18));

            var fractionalMonths = () => DateArithmeticTools.AddToDate(DateTime.MinValue, 1.5, "Months");
            fractionalMonths.Should().Throw<ArgumentException>();
            var outOfRange = () => DateArithmeticTools.AddToDate(DateTime.MaxValue, 1, "Days");
            outOfRange.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "AddBusinessDays")]
        [Trait("Covers", "BusinessDaysBetween")]
        public void BusinessDays()
        {
            DateArithmeticTools.AddBusinessDays(new DateTime(2026, 9, 25), 1).Should().Be(new DateTime(2026, 9, 28));

            DateArithmeticTools.BusinessDaysBetween(new DateTime(2026, 9, 23), new DateTime(2026, 9, 30)).Should()
                .Be(5);
        }

        [Fact]
        [Trait("Covers", "TimeBetween")]
        [Trait("Covers", "AgeAsOf")]
        public void ElapsedTimeAndAge()
        {
            var elapsed =
                DateArithmeticTools.TimeBetween(new DateTime(2024, 1, 15), new DateTime(2026, 3, 16, 12, 0, 0));
            elapsed.WholeMonths.Should().Be(26);
            elapsed.WholeYears.Should().Be(2);
            elapsed.TotalHours.Should().Be(elapsed.TotalDays * 24);

            DateArithmeticTools.AgeAsOf(new DateTime(1990, 6, 15), new DateTime(2026, 6, 14)).Should()
                .Be(new AgeDetails(35, 13148));
        }

        [Fact]
        [Trait("Covers", "IntervalsOverlap")]
        [Trait("Covers", "IsDateBetween")]
        public void IntervalsAndRanges()
        {
            var start = new DateTime(2026, 1, 1);

            DateArithmeticTools.IntervalsOverlap(start, start.AddDays(2), start.AddDays(1), start.AddDays(3)).Should()
                .BeTrue();
            DateArithmeticTools.IntervalsOverlap(start, start.AddDays(1), start.AddDays(1), start.AddDays(3)).Should()
                .BeFalse();

            DateArithmeticTools.IsDateBetween(start.AddDays(1), start, start.AddDays(1)).Should().BeTrue();
            var act = () => DateArithmeticTools.IntervalsOverlap(start.AddDays(1), start, start, start);
            act.Should().Throw<ArgumentException>();
        }
    }
}