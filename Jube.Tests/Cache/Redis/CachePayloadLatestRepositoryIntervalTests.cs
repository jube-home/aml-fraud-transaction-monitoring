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
using Jube.Cache.Redis;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CachePayloadLatestRepositoryIntervalTests
    {
        private static readonly DateTime referenceDate = new(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

        [Theory]
        [InlineData("d")]
        [InlineData("h")]
        [InlineData("n")]
        [InlineData("s")]
        [InlineData("m")]
        [InlineData("y")]
        public void EachRecognisedIntervalSubtractsFromReferenceDate(string interval)
        {
            var adjusted = CachePayloadLatestRepository.ApplySearchKeyTtlInterval(referenceDate, interval, 1);

            adjusted.Should().BeBefore(referenceDate);
        }

        [Fact]
        public void SecondsIntervalSupportsVeryShortRetentionWindows()
        {
            CachePayloadLatestRepository.ApplySearchKeyTtlInterval(referenceDate, "s", 5)
                .Should().Be(new DateTime(2024, 6, 15, 12, 0, 5, DateTimeKind.Utc));
        }

        [Fact]
        public void UnrecognisedIntervalFallsBackToDays()
        {
            CachePayloadLatestRepository.ApplySearchKeyTtlInterval(referenceDate, "bogus", 3)
                .Should().Be(referenceDate.AddDays(-3),
                    "unlike TtlCounterExtensions.ApplyTtlCounterInterval, this copy falls back to days for an unrecognised interval");
        }

        [Fact]
        public void ZeroIntervalValueReturnsReferenceDateUnchanged()
        {
            CachePayloadLatestRepository.ApplySearchKeyTtlInterval(referenceDate, "d", 0).Should().Be(referenceDate);
        }

        [Fact]
        public void NegativeIntervalValuePushesTheThresholdIntoTheFutureInstead()
        {
            CachePayloadLatestRepository.ApplySearchKeyTtlInterval(referenceDate, "d", -3)
                .Should().Be(referenceDate.AddDays(3));
        }
    }
}