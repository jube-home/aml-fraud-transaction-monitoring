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
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Query.GetCaseSuppressionOverrideStateQuery;
using Jube.Dto.Options;
using Jube.Service.Repository.CaseSuppression;
using Xunit;

namespace Jube.Test.Service.Repository.CaseSuppression
{
    [Trait("Category", "Unit")]
    public sealed class CaseSuppressionStandingResolverTests
    {
        private static readonly DateTime now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);

        private static CaseSuppressionTarget Target(int? applied, string? rule = "Rule") =>
            new() { AppliedOverrideId = applied, RuleName = rule };

        private static GetCaseSuppressionOverrideStateQueryDto Row(int id, bool deleted = false,
            DateTime? expiry = null, bool isRule = true) =>
            new() { Id = id, IsRule = isRule, Deleted = deleted, DeleteExpiryDate = expiry };

        [Theory]
        [InlineData(CaseSuppressionState.Pending, CaseSuppressionStanding.Pending)]
        [InlineData(CaseSuppressionState.Rejected, CaseSuppressionStanding.Rejected)]
        [InlineData(CaseSuppressionState.Retracted, CaseSuppressionStanding.Retracted)]
        public void ARequestThatIsNotApprovedKeepsItsState(CaseSuppressionState state,
            CaseSuppressionStanding expected)
        {
            CaseSuppressionStandingResolver.ResolveTarget(state, now.AddDays(5), Target(1), [], now).Should()
                .Be(expected);
            CaseSuppressionStandingResolver.Resolve(state, [expected]).Should().Be(expected);
        }

        [Fact]
        public void AnApprovedTargetWhoseOverrideIsLiveIsApproved()
        {
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(5), Target(1),
                [Row(1, expiry: now.AddDays(5))], now).Should().Be(CaseSuppressionStanding.Approved);
        }

        [Fact]
        public void AnApprovedTargetWhoseOverrideHasPassedItsExpiryIsExpired()
        {
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(-1), Target(1),
                [Row(1, expiry: now.AddDays(-1))], now).Should().Be(CaseSuppressionStanding.Expired);
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(-1), Target(1),
                [Row(1, true)], now).Should().Be(CaseSuppressionStanding.Expired);
        }

        [Fact]
        public void AnOverrideDeletedBeforeItsExpiryOrMissingIsRemoved()
        {
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(5), Target(1),
                [Row(1, true)], now).Should().Be(CaseSuppressionStanding.Removed);
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(5), Target(1),
                [], now).Should().Be(CaseSuppressionStanding.Removed);
        }

        [Fact]
        public void ModelAndRuleOverridesWithTheSameIdAreNotConfused()
        {
            CaseSuppressionStandingResolver.ResolveTarget(CaseSuppressionState.Approved, now.AddDays(5),
                Target(7, null), [Row(7, true, isRule: true), Row(7, isRule: false)], now).Should()
                .Be(CaseSuppressionStanding.Approved);
        }

        [Fact]
        public void TheRequestIsRemovedIfAnyTargetIsRemovedAndExpiredOnlyWhenAllAre()
        {
            List<CaseSuppressionStanding> mixed = [CaseSuppressionStanding.Approved, CaseSuppressionStanding.Removed];
            List<CaseSuppressionStanding> expired = [CaseSuppressionStanding.Expired, CaseSuppressionStanding.Expired];
            List<CaseSuppressionStanding> some = [CaseSuppressionStanding.Expired, CaseSuppressionStanding.Approved];

            CaseSuppressionStandingResolver.Resolve(CaseSuppressionState.Approved, mixed).Should()
                .Be(CaseSuppressionStanding.Removed);
            CaseSuppressionStandingResolver.Resolve(CaseSuppressionState.Approved, expired).Should()
                .Be(CaseSuppressionStanding.Expired);
            CaseSuppressionStandingResolver.Resolve(CaseSuppressionState.Approved, some).Should()
                .Be(CaseSuppressionStanding.Approved);
        }
    }
}
