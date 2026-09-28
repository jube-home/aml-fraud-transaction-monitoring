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
using Jube.App.Middlewares;
using Jube.Test.Service.Authentication;
using Xunit;

namespace Jube.Test.Middlewares
{
    [Trait("Category", "Unit")]
    public sealed class GlobalHttpRequestSourceIpThrottleTests
    {
        [Fact]
        public void RequestsWithinTheThresholdAreNeverExceeded()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 20; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.20", 20, TimeSpan.FromSeconds(60), clock)
                    .Should().BeFalse();
            }
        }

        [Fact]
        public void TheRequestThatCrossesTheThresholdIsExceeded()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 20; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.21", 20, TimeSpan.FromSeconds(60), clock);
            }

            throttle.TryRecordAndCheckExceeded("203.0.113.21", 20, TimeSpan.FromSeconds(60), clock).Should().BeTrue();
        }

        [Fact]
        public void EveryRequestCountsNotJustFailures()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 5; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.22", 5, TimeSpan.FromSeconds(60), clock)
                    .Should().BeFalse();
            }

            throttle.TryRecordAndCheckExceeded("203.0.113.22", 5, TimeSpan.FromSeconds(60), clock).Should().BeTrue();
        }

        [Fact]
        public void DifferentSourceIpsAreCountedIndependently()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 21; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.23", 20, TimeSpan.FromSeconds(60), clock);
            }

            throttle.TryRecordAndCheckExceeded("203.0.113.24", 20, TimeSpan.FromSeconds(60), clock)
                .Should().BeFalse();
        }

        [Fact]
        public void TheWindowResetsOnceItHasElapsed()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 21; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.25", 20, TimeSpan.FromSeconds(60), clock);
            }

            clock.Advance(TimeSpan.FromSeconds(61));

            throttle.TryRecordAndCheckExceeded("203.0.113.25", 20, TimeSpan.FromSeconds(60), clock)
                .Should().BeFalse();
        }

        [Fact]
        public void ANullOrEmptyRemoteIpIsNeverThrottled()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 50; i++)
            {
                throttle.TryRecordAndCheckExceeded(null, 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
                throttle.TryRecordAndCheckExceeded("", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
            }
        }

        [Fact]
        public void ANonPositiveThresholdIsNeverThrottled()
        {
            var throttle = new GlobalHttpRequestSourceIpThrottle();
            var clock = new FakeClock(DateTimeOffset.UtcNow);

            for (var i = 0; i < 10; i++)
            {
                throttle.TryRecordAndCheckExceeded("203.0.113.26", 0, TimeSpan.FromSeconds(60), clock)
                    .Should().BeFalse();
            }
        }
    }
}