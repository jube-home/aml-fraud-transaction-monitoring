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

using System.Linq;
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class VelocityCalculatorTests
    {
        [Fact]
        public void TheGridCountsTransactionsByWeekdayFromMondayAndHour()
        {
            var events = new[] { Event(1, Start), Event(2, Start.AddMinutes(10)), Event(3, Start.AddDays(2)) }.ToList();

            var grid = VelocityCalculator.Compute(events).HourByWeekday;

            grid.Should().HaveCount(7);
            grid[0][9].Should().Be(2);
            grid[2][9].Should().Be(1);
            grid.Sum(day => day.Sum()).Should().Be(3);
        }

        [Fact]
        public void TheBusiestHourIsFoundAgainstTheTypicalOne()
        {
            var events = Enumerable.Range(0, 20).Select(i => Event(i + 1, Start.AddDays(i))).ToList();
            events.AddRange(Enumerable.Range(0, 6).Select(i => Event(100 + i, Start.AddDays(10).AddMinutes(i * 5 + 1))));

            var hour = VelocityCalculator.Compute(events).Bursts.Single(burst => burst.WindowHours == 1);

            hour.Max.Should().Be(7);
            hour.At.Date.Should().Be(Start.AddDays(10).Date);
            hour.Median.Should().Be(1);
            hour.Ratio.Should().Be(7);
        }

        [Fact]
        public void EachOfTheThreeWindowsIsReportedAndNothingGivesNoBursts()
        {
            VelocityCalculator.Compute([Event(1, Start)]).Bursts.Select(b => b.WindowHours)
                .Should().Equal(1, 24, 168);
            VelocityCalculator.Compute([]).Bursts.Should().BeEmpty();
        }

        [Fact]
        public void TheWindowIsHalfOpenSoAnEventExactlyOneWindowLaterIsNotInIt()
        {
            var events = new[] { Event(1, Start), Event(2, Start.AddHours(1)) }.ToList();

            VelocityCalculator.Compute(events).Bursts.Single(b => b.WindowHours == 1).Max.Should().Be(1);
            VelocityCalculator.Compute(events).Bursts.Single(b => b.WindowHours == 24).Max.Should().Be(2);
        }
    }
}
