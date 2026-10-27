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
using FluentAssertions;
using Jube.CaseRisk.Journal;
using Xunit;
using static Jube.Test.CaseRisk.Journal.JournalEvents;

namespace Jube.Test.CaseRisk.Journal
{
    [Trait("Category", "Unit")]
    public sealed class ProgressionCalculatorTests
    {
        private static readonly string[] amount = ["Amount"];

        [Theory]
        [InlineData(2, "hour")]
        [InlineData(30, "day")]
        [InlineData(400, "week")]
        public void TheBucketFollowsTheSpanOfTheHistory(int days, string expected)
        {
            var events = new[] { Event(1, Start, 1), Event(2, Start.AddDays(days), 2) }.ToList();

            ProgressionCalculator.Compute(events, amount).Bucket.Should().Be(expected);
        }

        [Fact]
        public void EachBucketHoldsTheMedianAndTheCountOfItsTransactions()
        {
            var events = new[]
            {
                Event(1, Start, 10), Event(2, Start.AddHours(1), 30), Event(3, Start.AddHours(2), 20),
                Event(4, Start.AddDays(10), 100)
            }.ToList();

            var series = ProgressionCalculator.Compute(events, amount).Series.Single();

            series.Points.Select(point => point.Median).Should().Equal(20, 100);
            series.Points.Select(point => point.Count).Should().Equal(3, 1);
            series.Latest.Should().Be(100);
        }

        [Fact]
        public void TheChangeIsTheLatestAgainstTheMedianOfTheBucketsBefore()
        {
            var series = ProgressionCalculator.Compute(Daily(100, 100, 100, 100, 200), amount).Series.Single();

            series.PreviousMedian.Should().Be(100);
            series.Change.Should().BeApproximately(1.0, 1e-9);
        }

        [Fact]
        public void ANarrowDailyHistoryHasNoPreviousAndAZeroPreviousHasNoChange()
        {
            ProgressionCalculator.Compute(Daily(5), amount).Series.Single().PreviousMedian.Should().BeNull();
            ProgressionCalculator.Compute(Daily(0, 0, 4), amount).Series.Single().Change.Should().BeNull();
        }

        [Fact]
        public void TooManyBucketsWidenToWeeksAndAFieldWithNoValuesIsSkipped()
        {
            var events = Enumerable.Range(0, 250).Select(i => Event(i + 1, Start.AddDays(i * 0.9), i)).ToList();

            var report = ProgressionCalculator.Compute(events, ["Amount", "Missing"], maximumPoints: 100);

            report.Bucket.Should().Be("week");
            report.Series.Select(s => s.Field).Should().Equal("Amount");
        }

        [Fact]
        public void WeeksStartOnMondayAndNothingGivesAnEmptyReport()
        {
            ProgressionCalculator.Start(new DateTime(2026, 10, 11, 15, 0, 0, DateTimeKind.Utc), "week")
                .Should().Be(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
            ProgressionCalculator.Compute([], amount).Series.Should().BeEmpty();
        }
    }
}
