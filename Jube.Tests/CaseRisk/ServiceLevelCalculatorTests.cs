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
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class ServiceLevelCalculatorTests
    {
        private static readonly DateTime created = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);

        private static ServiceLevelInputs Inputs(double nowMinutes, DateTime? viewed = null, DateTime? owned = null,
            DateTime? closed = null, DateTime? stay = null, int? view = null, int? own = null, int? resolve = null,
            int? dwell = null) => new(created.AddMinutes(nowMinutes), created, closed, viewed, owned, stay, view, own,
            resolve, dwell);

        [Fact]
        public void NoTargetsMeansNoTargetAndNoMeasures()
        {
            var result = ServiceLevelCalculator.Assess(Inputs(500));

            result.State.Should().Be(ServiceLevelState.NoTarget);
            result.Worst.Should().BeNull();
            result.Measures.Should().BeEmpty();
        }

        [Theory]
        [InlineData(10, ServiceLevelState.OnTrack)]
        [InlineData(23.9, ServiceLevelState.OnTrack)]
        [InlineData(24, ServiceLevelState.AtRisk)]
        [InlineData(29.9, ServiceLevelState.AtRisk)]
        [InlineData(30, ServiceLevelState.Breached)]
        [InlineData(500, ServiceLevelState.Breached)]
        public void AnUnreachedTargetMovesFromOnTrackToAtRiskAtEightyPercentToBreachedAtTheTarget(double minutes,
            ServiceLevelState expected)
        {
            var result = ServiceLevelCalculator.Assess(Inputs(minutes, view: 30));

            result.State.Should().Be(expected);
            result.Worst!.Dimension.Should().Be(ServiceLevelDimension.FirstView);
            result.Worst.DueDate.Should().Be(created.AddMinutes(30));
        }

        [Fact]
        public void ATargetReachedInTimeIsMetAndOneReachedLateStaysBreachedForever()
        {
            var inTime = ServiceLevelCalculator.Assess(Inputs(1000, viewed: created.AddMinutes(20), view: 30));
            var late = ServiceLevelCalculator.Assess(Inputs(1000, viewed: created.AddMinutes(45), view: 30));

            inTime.State.Should().Be(ServiceLevelState.Met);
            inTime.Worst!.ActualMinutes.Should().Be(20);
            late.State.Should().Be(ServiceLevelState.Breached);
            late.Worst!.ActualMinutes.Should().Be(45);
        }

        [Fact]
        public void TheWorstDimensionDecidesTheOverallStateAndTheMostElapsedBreaksATie()
        {
            var result = ServiceLevelCalculator.Assess(Inputs(100, viewed: created.AddMinutes(5), view: 30, own: 60,
                resolve: 600));

            result.Measures.Should().HaveCount(3);
            result.State.Should().Be(ServiceLevelState.Breached);
            result.Worst!.Dimension.Should().Be(ServiceLevelDimension.Ownership);
            result.Measures.Single(m => m.Dimension == ServiceLevelDimension.FirstView).State
                .Should().Be(ServiceLevelState.Met);
            result.Measures.Single(m => m.Dimension == ServiceLevelDimension.Resolution).State
                .Should().Be(ServiceLevelState.OnTrack);
        }

        [Fact]
        public void ACaseClosedWithoutEverBeingViewedOrOwnedIsNotJudgedOnThoseTargetsAfterClosure()
        {
            var result = ServiceLevelCalculator.Assess(Inputs(5000, closed: created.AddMinutes(100), view: 30,
                own: 60, resolve: 600));

            result.Measures.Select(m => m.Dimension).Should().Equal(ServiceLevelDimension.Resolution);
            result.State.Should().Be(ServiceLevelState.Met);
        }

        [Fact]
        public void StatusDwellRunsFromTheMomentOfEntryAndStopsOnceTheCaseIsClosed()
        {
            var entered = created.AddMinutes(100);

            var open = ServiceLevelCalculator.Assess(Inputs(190, stay: entered, dwell: 60));
            var closed = ServiceLevelCalculator.Assess(Inputs(190, closed: created.AddMinutes(150), stay: entered,
                dwell: 60));

            open.Worst!.Dimension.Should().Be(ServiceLevelDimension.StatusDwell);
            open.Worst.State.Should().Be(ServiceLevelState.Breached);
            open.Worst.DueDate.Should().Be(entered.AddMinutes(60));
            closed.Measures.Should().BeEmpty();
        }

        [Fact]
        public void ZeroOrNegativeTargetsAreTreatedAsNoTarget()
        {
            ServiceLevelCalculator.Assess(Inputs(500, view: 0, own: -5)).Measures.Should().BeEmpty();
        }
    }
}
