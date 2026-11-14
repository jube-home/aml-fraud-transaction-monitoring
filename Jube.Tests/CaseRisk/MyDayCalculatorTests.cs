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
using FluentAssertions;
using Jube.CaseRisk.Analytics;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class MyDayCalculatorTests
    {
        private static readonly DateTime now = Start.AddHours(8);

        private static List<CaseFact> Closes(string user, params int[] perDayAgo)
        {
            var facts = new List<CaseFact>();
            var id = 1;

            for (var daysAgo = 0; daysAgo < perDayAgo.Length; daysAgo++)
            {
                for (var n = 0; n < perDayAgo[daysAgo]; n++)
                {
                    facts.Add(Fact(id++, closedMinutes: -daysAgo * 1440 + n, closedUser: user));
                }
            }

            return facts;
        }

        [Fact]
        public void OnlyTheCallersOwnClosuresAreCountedAndTheDaysRunOldestToNewest()
        {
            var facts = Closes("me", 3, 1).Concat(Closes("them", 9, 9)).ToList();

            var report = MyDayCalculator.Compute(facts, "me", now, null);

            report.Days.Should().HaveCount(MyDayCalculator.Window);
            report.Days.Select(d => d.Date).Should().BeInAscendingOrder();
            report.Days[^1].Date.Should().Be(now.Date);
            report.Days[^1].Closed.Should().Be(3);
            report.Days[^2].Closed.Should().Be(1);
            report.ClosedToday.Should().Be(3);
            report.BestDay.Should().Be(3);
        }

        [Fact]
        public void TheStreakCountsConsecutiveDaysThatMetTheGoalAndStopsAtTheFirstMiss()
        {
            var report = MyDayCalculator.Compute(Closes("me", 2, 3, 2, 1, 5), "me", now, 2);

            report.Streak.Should().Be(3);
            report.Goal.Should().Be(2);
        }

        [Fact]
        public void ADayThatHasNotYetMetTheGoalDoesNotBreakTheStreakFromYesterday()
        {
            var report = MyDayCalculator.Compute(Closes("me", 1, 3, 3, 0, 4), "me", now, 3);

            report.ClosedToday.Should().Be(1);
            report.Streak.Should().Be(2);
        }

        [Fact]
        public void WithNoGoalAStreakIsARunOfDaysWithAtLeastOneClosure()
        {
            var report = MyDayCalculator.Compute(Closes("me", 0, 1, 7, 0, 2), "me", now, null);

            report.Goal.Should().BeNull();
            report.Streak.Should().Be(2);
        }

        [Fact]
        public void ATypicalDayIsTheMedianOfTheThirteenDaysBeforeTodayAndNeverIncludesToday()
        {
            var report = MyDayCalculator.Compute(Closes("me", 40, 2, 4, 6), "me", now, null);

            report.TypicalDay.Should().Be(0);

            var busy = MyDayCalculator.Compute(
                Closes("me", 40, 2, 2, 2, 2, 2, 2, 2, 8, 8, 8, 8, 8, 8), "me", now, null);

            busy.TypicalDay.Should().Be(2);
            busy.BestDay.Should().Be(40);
        }

        [Fact]
        public void AQuietUserHasNoStreakAndZeroEverywhereRatherThanNothing()
        {
            var report = MyDayCalculator.Compute([], "me", now, 10);

            report.Streak.Should().Be(0);
            report.ClosedToday.Should().Be(0);
            report.BestDay.Should().Be(0);
            report.TypicalDay.Should().Be(0);
            report.Days.Should().OnlyContain(d => d.Closed == 0);
        }

        [Fact]
        public void AGoalOfZeroOrLessIsTreatedAsOneSoANilDayNeverCountsAsMet()
        {
            var report = MyDayCalculator.Compute(Closes("me", 0, 5), "me", now, 0);

            report.Streak.Should().Be(1);
        }
    }
}
