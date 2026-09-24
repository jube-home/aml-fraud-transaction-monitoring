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
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.AbstractionRulesWithSearchKeys;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Xunit;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation
{
    [Trait("Category", "Unit")]
    public sealed class TimeWindowTests
    {
        private static readonly DateTime reference = new(2026, 3, 31, 12, 0, 0, DateTimeKind.Utc);

        [Theory]
        [InlineData("s", 30, "2026-03-31T11:59:30")]
        [InlineData("n", 90, "2026-03-31T10:30:00")]
        [InlineData("h", 25, "2026-03-30T11:00:00")]
        [InlineData("d", 31, "2026-02-28T12:00:00")]
        [InlineData("m", 1, "2026-02-28T12:00:00")]
        [InlineData("y", 1, "2025-03-31T12:00:00")]
        public void ATtlCounterWindowIsTheEnginesOwnArithmetic(string interval, int value, string from)
        {
            var window = TimeWindow.Calculate(TimeWindowKind.TtlCounter, interval, value, reference);

            window.From.Should().Be(DateTime.Parse(from, null, System.Globalization.DateTimeStyles.AdjustToUniversal |
                                                               System.Globalization.DateTimeStyles.AssumeUniversal));
            window.From.Should().Be(TtlCounterExtensions.ApplyTtlCounterInterval(reference, interval, value));
            window.To.Should().Be(reference);
            window.Length.Should().Be(reference - window.From);
        }

        [Theory]
        [InlineData("s", 45)]
        [InlineData("n", 15)]
        [InlineData("h", 6)]
        [InlineData("d", 7)]
        public void AnAbstractionRuleWindowIsTheEnginesVisualBasicDateAdd(string interval, int value)
        {
            var window = TimeWindow.Calculate(TimeWindowKind.AbstractionRule, interval, value, reference);

            window.From.Should().Be(Microsoft.VisualBasic.DateAndTime.DateAdd(interval, -value, reference));
        }

        [Fact]
        public void TheAbstractionWindowIsShortenedBySearchKeyTtlExactlyAsTheEngineDoes()
        {
            var window = TimeWindow.AbstractionRule("d", 7, "h", 24, reference);

            window.From.Should().Be(reference.AddHours(-24));
            window.From.Should().Be(AbstractionWindow.FromDate("d", 7, "h", 24, reference));
            TimeWindow.AbstractionRule("h", 2, "d", 1, reference).From.Should().Be(reference.AddHours(-2));
        }

        [Fact]
        public void ACalendarWindowAlsoOffersWeeksAndMeansYearsByY()
        {
            TimeWindow.Calculate(TimeWindowKind.Calendar, "w", 2, reference).From.Should()
                .Be(reference.AddDays(-14));
            TimeWindow.Calculate(TimeWindowKind.Calendar, "y", 2, reference).From.Should()
                .Be(reference.AddYears(-2));
        }

        [Theory]
        [InlineData(TimeWindowKind.AbstractionRule, "m")]
        [InlineData(TimeWindowKind.AbstractionRule, "y")]
        [InlineData(TimeWindowKind.SearchKey, "w")]
        [InlineData(TimeWindowKind.TtlCounter, "w")]
        [InlineData(TimeWindowKind.Calendar, "q")]
        [InlineData(TimeWindowKind.Calendar, null)]
        public void AnIntervalTheEntityDoesNotAllowIsRefused(TimeWindowKind kind, string interval)
        {
            TimeWindow.IsAllowed(kind, interval).Should().BeFalse();
            var act = () => TimeWindow.Calculate(kind, interval, 1, reference);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void EveryIntervalHasAName()
        {
            foreach (var intervals in TimeWindow.Intervals.Values)
            {
                foreach (var interval in intervals)
                {
                    TimeWindow.IntervalNames.Should().ContainKey(interval);
                }
            }
        }
    }
}