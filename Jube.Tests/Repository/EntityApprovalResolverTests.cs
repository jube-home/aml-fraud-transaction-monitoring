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

using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Data.Query;
using Jube.Data.Query.Models;
using Jube.Data.Repository;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Unit")]
    public class EntityApprovalResolverTests
    {
        private static EntityApprovalRow Approved(int version, string user = "checker", int entityId = 1)
        {
            return new EntityApprovalRow(entityId, version, EntityApprovalState.Approved, user);
        }

        private static EntityApprovalRow Rejected(int version, string user = "checker", int entityId = 1)
        {
            return new EntityApprovalRow(entityId, version, EntityApprovalState.Rejected, user);
        }

        [Fact]
        public void AnEntityWithNoApprovalAtAllHasNoEffectiveVersion()
        {
            EntityApprovalResolver.EffectiveVersion([], 1).Should().BeNull();
        }

        [Fact]
        public void TheCurrentVersionRunsWhenItIsApproved()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3)], 3).Should().Be(3);
        }

        [Fact]
        public void AnUnapprovedEditFallsBackToTheLastApprovedVersion()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2)], 4).Should().Be(2);
        }

        [Fact]
        public void TheHighestApprovedVersionWins()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(1), Approved(3), Approved(2)], 5).Should().Be(3);
        }

        [Fact]
        public void AnApprovalForAVersionAheadOfTheCurrentRowIsIgnored()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2), Approved(9)], 3).Should().Be(2);
        }

        [Fact]
        public void ARejectedVersionNeverRuns()
        {
            EntityApprovalResolver.EffectiveVersion([Rejected(3)], 3).Should().BeNull();
        }

        [Fact]
        public void ARejectionOnTheCurrentVersionFallsBackToTheEarlierApprovedOne()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2), Rejected(3)], 3).Should().Be(2);
        }

        [Fact]
        public void ARejectionSittingBetweenTwoApprovalsDoesNotBlockTheLaterOne()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(1), Rejected(2), Approved(3)], 3).Should().Be(3);
        }

        [Fact]
        public void ARejectionOnAnOtherwiseApprovedVersionWinsOverTheApproval()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3), Rejected(3)], 3).Should().BeNull();
        }

        [Fact]
        public void OneApprovalIsNotEnoughWhenTwoAreRequired()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3)], 3, 2).Should().BeNull();
        }

        [Fact]
        public void TwoDistinctApproversSatisfyATwoApproverThreshold()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3, "first"), Approved(3, "second")], 3, 2)
                .Should().Be(3);
        }

        [Fact]
        public void TheSameApproverTwiceDoesNotSatisfyATwoApproverThreshold()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3), Approved(3, "CHECKER")], 3, 2)
                .Should().BeNull();
        }

        [Fact]
        public void AThresholdBelowOneIsTreatedAsOne()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(3)], 3, 0).Should().Be(3);
        }

        [Fact]
        public void WithTwoRequiredTheResolverFallsBackToAFullyApprovedEarlierVersion()
        {
            EntityApprovalRow[] approvals =
            [
                Approved(2, "first"), Approved(2, "second"),
                Approved(3, "first")
            ];

            EntityApprovalResolver.EffectiveVersion(approvals, 3, 2).Should().Be(2);
        }

        [Fact]
        public void EffectiveVersionsAreResolvedPerEntity()
        {
            EntityApprovalRow[] approvals =
            [
                Approved(1, entityId: 1), Approved(2, entityId: 1),
                Approved(1, entityId: 2),
                Rejected(1, entityId: 3)
            ];

            var current = new Dictionary<int, int> { [1] = 3, [2] = 1, [3] = 1, [4] = 1 };

            var effective = EntityApprovalResolver.EffectiveVersionsByEntity(approvals, current);

            using var scope = new AssertionScope();
            effective.Should().HaveCount(2);
            effective[1].Should().Be(2);
            effective[2].Should().Be(1);
            effective.Should().NotContainKey(3);
            effective.Should().NotContainKey(4);
        }

        [Fact]
        public void AnEntityWithApprovalsButNoCurrentRowIsNotResolved()
        {
            var effective = EntityApprovalResolver.EffectiveVersionsByEntity([Approved(1, entityId: 7)],
                new Dictionary<int, int>());

            effective.Should().BeEmpty();
        }

        [Fact]
        public void RejectionIsReportedForTheVersionItWasMadeAgainst()
        {
            EntityApprovalRow[] approvals = [Rejected(3)];

            using var scope = new AssertionScope();
            EntityApprovalResolver.IsRejected(approvals, 3).Should().BeTrue();
            EntityApprovalResolver.IsRejected(approvals, 2).Should().BeFalse();
        }

        [Fact]
        public void AVersionWithNoRowsAtAllIsPending()
        {
            EntityApprovalResolver.IsPending([], 4).Should().BeTrue();
        }

        [Fact]
        public void AnApprovedVersionIsNotPending()
        {
            EntityApprovalResolver.IsPending([Approved(4)], 4).Should().BeFalse();
        }

        [Fact]
        public void ARejectedVersionIsNotPendingBecauseItNeedsAnEditToStartAgain()
        {
            EntityApprovalResolver.IsPending([Rejected(4)], 4).Should().BeFalse();
        }

        [Fact]
        public void AVersionShortOfItsApproverThresholdIsStillPending()
        {
            EntityApprovalResolver.IsPending([Approved(4, "first")], 4, 2).Should().BeTrue();
        }

        [Fact]
        public void APendingDeleteResolvesToThePreDeleteVersionSoTheEntityKeepsRunning()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2)], 3).Should().Be(2,
                "the delete bumped the row to version three, which no checker has approved yet");
        }

        [Fact]
        public void AnApprovedDeleteResolvesToTheDeletedVersionSoTheEntityCanBeDropped()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2), Approved(3)], 3).Should().Be(3,
                "resolving to the deleted version is what lets the caller drop the row");
        }

        [Fact]
        public void ARejectedDeleteLeavesTheEntityRunningAtThePreDeleteVersion()
        {
            EntityApprovalResolver.EffectiveVersion([Approved(2), Rejected(3)], 3).Should().Be(2,
                "a refused deletion must not take the entity out of the engine");
        }

        [Fact]
        public void ADeleteOfANeverApprovedEntityLeavesNothingToFallBackTo()
        {
            EntityApprovalResolver.EffectiveVersion([], 2).Should().BeNull(
                "an entity that never ran does not start running because someone deleted it");
        }
    }
}