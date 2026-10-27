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
using Jube.CaseRisk;
using Jube.CaseRisk.Analytics;
using Xunit;
using static Jube.Test.CaseRisk.FactBuilder;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseAnalyticsOperationsTests
    {
        [Fact]
        public void ServiceLevelAttainmentCountsMetBreachedAndInFlightByDimension()
        {
            var now = Start.AddHours(10);

            var facts = new[]
            {
                Fact(1, 0, 100, viewedMinutes: 10, ownedMinutes: 20, firstViewTarget: 30, ownershipTarget: 60,
                    resolutionTarget: 120),
                Fact(2, 0, 300, viewedMinutes: 45, ownedMinutes: 50, firstViewTarget: 30, ownershipTarget: 60,
                    resolutionTarget: 120),
                Fact(3, firstViewTarget: 30, resolutionTarget: 2000),
                Fact(4, 575, firstViewTarget: 30, resolutionTarget: 100)
            };

            var report = SlaCalculator.Compute(facts, now);

            var view = report.Dimensions.Single(d => d.Dimension == ServiceLevelDimension.FirstView);
            view.Met.Should().Be(1);
            view.Breached.Should().Be(2);
            view.Attainment.Should().BeApproximately(1.0 / 3, 1e-9);
            view.AtRisk.Should().Be(1);

            var resolution = report.Dimensions.Single(d => d.Dimension == ServiceLevelDimension.Resolution);
            resolution.Met.Should().Be(1);
            resolution.Breached.Should().Be(1);
            resolution.OnTrack.Should().Be(2);

            report.Dimensions.Should().NotContain(d => d.Dimension == ServiceLevelDimension.StatusDwell);
        }

        [Fact]
        public void TheWatchListHoldsOnlyOpenCasesStillMissingATargetWorstFirst()
        {
            var now = Start.AddHours(10);

            var facts = new[]
            {
                Fact(1, firstViewTarget: 30),
                Fact(2, 300, firstViewTarget: 30),
                Fact(3, 590, firstViewTarget: 30),
                Fact(4, 0, 100, viewedMinutes: 90, firstViewTarget: 30)
            };

            var watch = SlaCalculator.Compute(facts, now).Watch;

            watch.Select(w => w.CaseId).Should().Equal(1, 2);
            watch[0].ElapsedFraction.Should().BeGreaterThan(watch[1].ElapsedFraction);
            watch.Should().OnlyContain(w => w.State == ServiceLevelState.Breached);
        }

        [Fact]
        public void WeeklyAttainmentGroupsByTheMondayOfTheWeekTheCaseWasCreated()
        {
            var monday = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

            var facts = new[]
            {
                Fact(1, created: monday.AddDays(2), closedMinutes: null, viewedMinutes: null) with
                {
                    FirstViewedDate = monday.AddDays(2).AddMinutes(10), FirstViewTargetMinutes = 30
                },
                Fact(2, created: monday.AddDays(6)) with
                {
                    FirstViewedDate = monday.AddDays(6).AddMinutes(90), FirstViewTargetMinutes = 30
                },
                Fact(3, created: monday.AddDays(8)) with
                {
                    FirstViewedDate = monday.AddDays(8).AddMinutes(5), FirstViewTargetMinutes = 30
                }
            };

            var weeks = SlaCalculator.Compute(facts, monday.AddDays(30)).Weekly;

            weeks.Should().HaveCount(2);
            weeks[0].WeekStart.Should().Be(monday);
            weeks[0].Met.Should().Be(1);
            weeks[0].Breached.Should().Be(1);
            weeks[1].WeekStart.Should().Be(monday.AddDays(7));
            weeks[1].Met.Should().Be(1);
            SlaCalculator.WeekStart(new DateTime(2026, 10, 11, 23, 0, 0)).Should().Be(monday);
        }

        [Fact]
        public void TheRhythmCountsArrivalsByWeekdayAndHourAndFindsTheBusiestSlot()
        {
            var monday = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc);

            var facts = new[]
            {
                Fact(1, created: monday.AddHours(9)) with { FirstViewedDate = monday.AddHours(9).AddMinutes(10) },
                Fact(2, created: monday.AddHours(9).AddMinutes(30)) with
                {
                    FirstViewedDate = monday.AddHours(9).AddMinutes(30).AddMinutes(30)
                },
                Fact(3, created: monday.AddDays(6).AddHours(23)),
                Fact(4, created: monday.AddDays(2).AddHours(3)) with
                {
                    Closed = true, ClosedDate = monday.AddDays(2).AddHours(15)
                }
            };

            var rhythm = RhythmCalculator.Compute(facts);

            rhythm.ArrivalsByWeekdayAndHour[0][9].Should().Be(2);
            rhythm.ArrivalsByWeekdayAndHour[6][23].Should().Be(1);
            rhythm.ArrivalsByWeekdayAndHour[2][3].Should().Be(1);
            rhythm.BusiestWeekday.Should().Be(0);
            rhythm.BusiestHour.Should().Be(9);
            rhythm.MedianFirstViewMinutesByHour[9].Should().Be(20);
            rhythm.MedianFirstViewMinutesByHour[23].Should().BeNull();
            rhythm.ClosuresByHour[15].Should().Be(1);
            rhythm.ArrivalsByWeekdayAndHour.Sum(d => d.Sum()).Should().Be(4);
        }

        [Fact]
        public void WorkloadShowsTheCallersOwnFiguresAndOnlyShowsATeamOfThreeOrMore()
        {
            var facts = new[]
            {
                Fact(1, 0, 60, closedUser: "me"), Fact(2, 0, 120, closedUser: "me"),
                Fact(3, 0, 600, closedUser: "ann"), Fact(4, 0, 30, closedUser: "bob"),
                Fact(5, 0, 90, closedUser: "bob"), Fact(6, lockedUser: "me"), Fact(7, lockedUser: "ann")
            };

            var withTeam = WorkloadCalculator.Compute(facts, "me");

            withTeam.Mine.Closed.Should().Be(2);
            withTeam.Mine.Resolution.P50.Should().Be(90);
            withTeam.Mine.HeldNow.Should().Be(1);
            withTeam.Team.Should().NotBeNull();
            withTeam.Team!.People.Should().Be(3);
            withTeam.Team.MedianClosedPerPerson.Should().Be(2);
            withTeam.Team.MedianResolutionMinutes.Should().Be(90);

            var small = WorkloadCalculator.Compute(facts.Where(f => f.ClosedUser != "ann").ToArray(), "me");
            small.Team.Should().BeNull();
            small.Mine.Closed.Should().Be(2);
        }

        [Fact]
        public void HandoffsAndReopensAreBucketedAndPingPongCasesAreListedWorstFirst()
        {
            var facts = new[]
            {
                Fact(1), Fact(2, handoffs: 1), Fact(3, handoffs: 2), Fact(4, handoffs: 3, reopens: 1),
                Fact(5, handoffs: 7), Fact(6, reopens: 2)
            };

            var report = HandoffCalculator.Compute(facts);

            report.Distribution.Select(d => d.Cases).Should().Equal(2, 1, 1, 2);
            report.ReopenedCases.Should().Be(2);
            report.ReopenRate.Should().BeApproximately(2.0 / 6, 1e-9);
            report.PingPong.Select(p => p.CaseId).Should().Equal(5, 4, 6);
        }

        [Fact]
        public void OpenCasesAreCountedOncePerStateBreachedBeforeAtRiskAndClosedOnesNever()
        {
            var now = Start.AddHours(10);

            var facts = new[]
            {
                Fact(1, firstViewTarget: 30, resolutionTarget: 100),
                Fact(2, firstViewTarget: 30, ownershipTarget: 20),
                Fact(3, 575, firstViewTarget: 30),
                Fact(4, 0, 700, viewedMinutes: 200, firstViewTarget: 30),
                Fact(5, viewedMinutes: 5, firstViewTarget: 30, resolutionTarget: 2000)
            };

            var report = SlaCalculator.Compute(facts, now);

            report.OpenBreached.Should().Be(2);
            report.OpenAtRisk.Should().Be(1);
        }
    }
}
