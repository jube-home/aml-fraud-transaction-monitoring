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
using Jube.CaseRisk;
using Jube.Dto.Options;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Unit")]
    public sealed class CaseDiaryLaneTests
    {
        private static readonly CaseRiskSettings settings = new();
        private static readonly DateTime now = new(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

        private static LaneAssessment Lane(bool diary, DateTime? due, CaseDiaryType type) =>
            LaneCalculator.Decide(CaseDiaryLane.Inputs(now, diary, due, type), settings);

        [Fact]
        public void APaymentHoldPutsTheCaseInTheLiveMoneyLaneAndBreachesAtItsDeadline()
        {
            var open = Lane(true, now.AddHours(3), CaseDiaryType.PaymentHold);
            var late = Lane(true, now.AddHours(-1), CaseDiaryType.PaymentHold);

            open.Lane.Should().Be(CaseRiskLane.LiveMoney);
            open.NextDeadline.Should().Be(now.AddHours(3));
            open.Breached.Should().BeFalse();
            late.Lane.Should().Be(CaseRiskLane.LiveMoney);
            late.Breached.Should().BeTrue();
        }

        [Fact]
        public void ARegulatoryDeadlinePutsTheCaseInTheMandatoryLaneWhateverTheDistanceToIt()
        {
            var far = Lane(true, now.AddDays(30), CaseDiaryType.RegulatoryDeadline);
            var late = Lane(true, now.AddDays(-2), CaseDiaryType.RegulatoryDeadline);

            far.Lane.Should().Be(CaseRiskLane.Mandatory);
            far.Reason.Should().Contain("regulatory");
            far.NextDeadline.Should().Be(now.AddDays(30));
            far.Breached.Should().BeFalse();
            late.Lane.Should().Be(CaseRiskLane.Mandatory);
            late.Breached.Should().BeTrue();
        }

        [Theory]
        [InlineData(CaseDiaryType.Review, "The case diary")]
        [InlineData(CaseDiaryType.CustomerResponse, "The customer response deadline")]
        public void ReviewAndCustomerResponseDiariesRankByHowNearTheyAreAndNameThemselvesInTheReason(
            CaseDiaryType type, string label)
        {
            var far = Lane(true, now.AddDays(10), type);
            var near = Lane(true, now.AddDays(1), type);
            var late = Lane(true, now.AddDays(-1), type);

            far.Lane.Should().Be(CaseRiskLane.Standard);
            near.Lane.Should().Be(CaseRiskLane.DeadlineNear);
            near.Reason.Should().StartWith(label);
            late.Lane.Should().Be(CaseRiskLane.DeadlineNear);
            late.Breached.Should().BeTrue();
        }

        [Theory]
        [InlineData(CaseDiaryType.PaymentHold)]
        [InlineData(CaseDiaryType.RegulatoryDeadline)]
        [InlineData(CaseDiaryType.CustomerResponse)]
        [InlineData(CaseDiaryType.Review)]
        public void ADiaryThatIsOffOrHasNoDateNeverChangesTheLane(CaseDiaryType type)
        {
            Lane(false, now.AddHours(1), type).Lane.Should().Be(CaseRiskLane.Standard);
            Lane(true, null, type).Lane.Should().Be(CaseRiskLane.Standard);
            Lane(false, null, type).NextDeadline.Should().BeNull();
        }
    }
}
