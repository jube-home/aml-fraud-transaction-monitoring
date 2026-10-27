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
using Jube.CaseRisk.Analytics;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseAnalyticsDistributionTests
    {
        [Fact]
        public void PercentilesInterpolateBetweenTheSortedValuesLikeAStatisticsPackage()
        {
            var stats = DistributionStats.Of([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

            stats.Count.Should().Be(10);
            stats.Mean.Should().Be(5.5);
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(10);
            stats.P50.Should().Be(5.5);
            stats.P75.Should().BeApproximately(7.75, 1e-9);
            stats.P90.Should().BeApproximately(9.1, 1e-9);
            stats.P95.Should().BeApproximately(9.55, 1e-9);
        }

        [Fact]
        public void AnEmptyOrSingleValueDistributionNeverThrows()
        {
            DistributionStats.Of([]).Should().Be(DistributionStats.Empty);
            DistributionStats.Of([double.NaN]).Count.Should().Be(0);

            var one = DistributionStats.Of([42]);
            one.P50.Should().Be(42);
            one.P95.Should().Be(42);
            one.Count.Should().Be(1);
        }

        [Fact]
        public void TheHistogramAssignsEveryValueToExactlyOneBucketAndLabelsTheEdges()
        {
            var buckets = Histograms.Build([1, 5, 14.9, 15, 59, 60, 5000, 99999], Histograms.MinuteEdges,
                Histograms.Minutes);

            buckets.Should().HaveCount(Histograms.MinuteEdges.Length + 1);
            buckets.Sum(b => b.Count).Should().Be(8);
            buckets[0].Label.Should().Be("< 5 min");
            buckets[0].Count.Should().Be(1);
            buckets.Single(b => b.From == 5).Count.Should().Be(2);
            buckets.Single(b => b.From == 15).Count.Should().Be(1);
            buckets.Single(b => b.From == 30).Count.Should().Be(1);
            buckets.Single(b => b.From == 60).Count.Should().Be(1);
            buckets[^1].Label.Should().Be("7 d+");
            buckets[^1].Count.Should().Be(1);
            buckets[^1].To.Should().BeNull();
        }

        [Theory]
        [InlineData(30, "30 min")]
        [InlineData(90, "1.5 h")]
        [InlineData(1440, "1 d")]
        [InlineData(2160, "1.5 d")]
        public void MinutesAreShownInTheUnitThatReadsBest(double minutes, string expected)
        {
            Histograms.Minutes(minutes).Should().Be(expected);
        }

        [Fact]
        public void CycleTimesMeasureFromCreationAndCountWhatWasNeverViewedOrOwned()
        {
            var now = Start.AddHours(10);

            var facts = new[]
            {
                Fact(1, 0, 600, viewedMinutes: 10, ownedMinutes: 20),
                Fact(2, 0, 300, viewedMinutes: 30, ownedMinutes: 40),
                Fact(3, viewedMinutes: 50),
                Fact(4, 60),
                Fact(5, 120)
            };

            var report = CycleTimeCalculator.Compute(facts, now);

            report.Cases.Should().Be(5);
            report.FirstView.Count.Should().Be(3);
            report.FirstView.P50.Should().Be(30);
            report.Ownership.Count.Should().Be(2);
            report.Ownership.Mean.Should().Be(30);
            report.Resolution.Count.Should().Be(2);
            report.Resolution.Mean.Should().Be(450);
            report.NeverViewed.Should().Be(2);
            report.NeverOwned.Should().Be(3);
            report.WaitingUnviewed.Count.Should().Be(2);
            report.WaitingUnviewed.Max.Should().Be(540);
            report.FirstViewHistogram.Sum(b => b.Count).Should().Be(3);
            report.ResolutionHistogram.Sum(b => b.Count).Should().Be(2);
        }

        [Fact]
        public void ACaseViewedBeforeItWasCreatedNeverProducesANegativeCycleTime()
        {
            var report = CycleTimeCalculator.Compute([Fact(1, 100, viewedMinutes: 50)], Start.AddHours(5));

            report.FirstView.Min.Should().Be(0);
        }

        [Fact]
        public void FlowCountsOpenedClosedAndTheBacklogAtTheEndOfEachDay()
        {
            var day = Start.Date;

            var facts = new[]
            {
                Fact(1, created: day.AddHours(10), closedMinutes: null),
                Fact(2, created: day.AddHours(11)) with { ClosedDate = day.AddDays(1).AddHours(2), Closed = true },
                Fact(3, created: day.AddDays(1).AddHours(5)),
                Fact(4, created: day.AddDays(-3)) with { ClosedDate = day.AddHours(1), Closed = true }
            };

            var stays = new[]
            {
                new StayFact(1, Status, day.AddHours(10), null),
                new StayFact(2, Status, day.AddHours(11), day.AddDays(1).AddHours(2)),
                new StayFact(3, Guid.Empty, day.AddDays(1).AddHours(5), null)
            };

            var days = FlowCalculator.Daily(facts, stays, day, day.AddDays(1));

            days.Should().HaveCount(2);
            days[0].Opened.Should().Be(2);
            days[0].Closed.Should().Be(1);
            days[0].Backlog.Should().Be(2);
            days[0].ByStatus[Status].Should().Be(2);
            days[1].Opened.Should().Be(1);
            days[1].Closed.Should().Be(1);
            days[1].Backlog.Should().Be(2);
            days[1].ByStatus[Status].Should().Be(1);
            days[1].ByStatus[Guid.Empty].Should().Be(1);
        }

        [Fact]
        public void DwellSharesTheTimeBetweenStatusesAndCountsBreachesAgainstTheTarget()
        {
            var now = Start.AddHours(10);
            var other = Guid.Parse("00000000-0000-0000-0000-0000000000b2");

            var stays = new[]
            {
                new StayFact(1, Status, Start, Start.AddMinutes(30)),
                new StayFact(2, Status, Start, Start.AddMinutes(90)),
                new StayFact(3, Status, Start.AddHours(8), null),
                new StayFact(4, other, Start, Start.AddMinutes(60))
            };

            var result = DwellCalculator.ByStatus(stays, now, new System.Collections.Generic.Dictionary<Guid, int?>
            {
                [Status] = 60,
                [other] = null
            });

            var main = result.Single(r => r.CaseWorkflowStatusGuid == Status);
            main.Stays.Should().Be(3);
            main.CurrentlyIn.Should().Be(1);
            main.Completed.Count.Should().Be(2);
            main.Completed.Mean.Should().Be(60);
            main.Current.Max.Should().Be(120);
            main.TotalMinutes.Should().Be(240);
            main.Breaches.Should().Be(2);
            main.MaxDwellMinutes.Should().Be(60);
            result.Sum(r => r.ShareOfTime).Should().BeApproximately(1, 1e-9);
            result[0].CaseWorkflowStatusGuid.Should().Be(Status);
            result.Single(r => r.CaseWorkflowStatusGuid == other).Breaches.Should().Be(0);
        }

        [Fact]
        public void AgingBucketsOnlyTheOpenCasesByAgeAndLaneAndListsTheOldest()
        {
            var now = Start.AddDays(10);

            var facts = new[]
            {
                Fact(1, createdMinutes: 14 * 24 * 60.0 * -1 + 10 * 24 * 60 - 0, lane: 1),
                Fact(2, created: now.AddMinutes(-30), lane: 4),
                Fact(3, created: now.AddHours(-3)),
                Fact(4, created: now.AddDays(-2), lane: 3, priority: 70),
                Fact(5, created: now.AddDays(-2)) with { Closed = true, ClosedDate = now },
                Fact(6, created: now.AddDays(-60), lane: 2, priority: 90)
            };

            var report = AgingCalculator.Open(facts, now, 2);

            report.Open.Should().Be(5);
            report.Buckets.Sum(b => b.Total).Should().Be(5);
            report.Buckets[0].Total.Should().Be(1);
            report.Buckets[0].ByLane[4].Should().Be(1);
            report.Buckets[1].Total.Should().Be(1);
            report.Buckets[1].ByLane[4].Should().Be(1);
            report.Buckets[^1].Label.Should().Be("30 d+");
            report.Buckets[^1].Total.Should().Be(1);
            report.Buckets.Single(b => b.FromMinutes == 20160).Total.Should().Be(1);
            report.Buckets.Single(b => b.FromMinutes == 1440).Total.Should().Be(1);
            report.Oldest.Select(o => o.CaseId).Should().Equal(6, 1);
            report.Oldest[0].Lane.Should().Be(2);
            report.Oldest[0].Priority.Should().Be(90);
        }

        [Fact]
        public void ACaseClosedExactlyAtMidnightBelongsToTheNewDayAndLeavesTheBacklogThatEvening()
        {
            var day = Start.Date;
            var facts = new[] { Fact(1, created: day.AddHours(8)) with { Closed = true, ClosedDate = day.AddDays(1) } };

            var days = FlowCalculator.Daily(facts, [], day, day.AddDays(1));

            days[0].Closed.Should().Be(0);
            days[0].Backlog.Should().Be(1);
            days[1].Closed.Should().Be(1);
            days[1].Backlog.Should().Be(0);
        }
    }
}
