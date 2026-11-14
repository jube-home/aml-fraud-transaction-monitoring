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
using Jube.Dto.Options;
using Jube.Service.Repository.CaseSuppression;
using Xunit;
using SuppressionPoco = Jube.Data.Poco.CaseSuppression;

namespace Jube.Test.Service.Repository.CaseSuppression
{
    [Trait("Category", "Unit")]
    public sealed class CaseSuppressionLapseTests
    {
        private static readonly DateTime now = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        private static SuppressionPoco Row(CaseSuppressionState state = CaseSuppressionState.Pending,
            int createdDaysAgo = 1, int? updatedDaysAgo = null, int expiresInDays = 30) =>
            new()
            {
                StateId = (int)state, CreatedDate = now.AddDays(-createdDaysAgo),
                UpdatedDate = updatedDaysAgo.HasValue ? now.AddDays(-updatedDaysAgo.Value) : null,
                ExpiryDate = now.AddDays(expiresInDays)
            };

        [Theory]
        [InlineData(null, 366)]
        [InlineData("", 366)]
        [InlineData("abc", 366)]
        [InlineData("0", 366)]
        [InlineData("-5", 366)]
        [InlineData("30", 30)]
        public void TheMaximumDurationFallsBackToTheDefaultWhenTheSettingIsMissingOrInvalid(string? value,
            int expected)
        {
            CaseSuppressionLimits.MaxDurationDays(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(null, 14)]
        [InlineData("x", 14)]
        [InlineData("0", 14)]
        [InlineData("-1", 14)]
        [InlineData("3", 3)]
        public void ThePendingLapseFallsBackToTheDefaultWhenTheSettingIsMissingOrInvalid(string? value, int expected)
        {
            CaseSuppressionLimits.PendingLapseDays(value).Should().Be(expected);
        }

        [Fact]
        public void APendingRequestLapsesOnceTheWindowHasPassedSinceItWasCreated()
        {
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 13), now, 14).Should().BeFalse();
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 14), now, 14).Should().BeTrue();
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 40), now, 14).Should().BeTrue();
        }

        [Fact]
        public void AResubmissionRestartsTheWindowFromItsOwnDate()
        {
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 40, updatedDaysAgo: 2), now, 14).Should().BeFalse();
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 40, updatedDaysAgo: 20), now, 14).Should().BeTrue();
        }

        [Fact]
        public void APendingRequestWhoseExpiryHasPassedIsLapsedWhateverItsAge()
        {
            CaseSuppressionLapse.IsLapsed(Row(createdDaysAgo: 1, expiresInDays: -1), now, 14).Should().BeTrue();
        }

        [Theory]
        [InlineData(CaseSuppressionState.Approved)]
        [InlineData(CaseSuppressionState.Rejected)]
        [InlineData(CaseSuppressionState.Retracted)]
        public void OnlyAPendingRequestCanLapse(CaseSuppressionState state)
        {
            CaseSuppressionLapse.IsLapsed(Row(state, createdDaysAgo: 400, expiresInDays: -5), now, 14).Should()
                .BeFalse();
        }

        [Fact]
        public void ALapsedRequestIsShownAsExpiredAndALiveOneKeepsItsStanding()
        {
            CaseSuppressionLapse.Apply(Row(createdDaysAgo: 20), CaseSuppressionStanding.Pending, now, 14).Should()
                .Be(CaseSuppressionStanding.Expired);
            CaseSuppressionLapse.Apply(Row(createdDaysAgo: 2), CaseSuppressionStanding.Pending, now, 14).Should()
                .Be(CaseSuppressionStanding.Pending);
        }
    }
}
