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
using Jube.Service.Authentication;
using Xunit;

namespace Jube.Test.Service.Authentication;

[Trait("Category", "Unit")]
public sealed class LoginSourceIpThrottleTests
{
    [Fact]
    public void SuccessfulOrUnrecordedAttemptsAreNeverThrottled()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 100; i++)
        {
            throttle.IsExceeded("203.0.113.4", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
        }
    }

    [Fact]
    public void FailedAttemptsWithinTheThresholdAreNeverExceeded()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 20; i++)
        {
            throttle.IsExceeded("203.0.113.5", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
            throttle.RecordFailedAttempt("203.0.113.5", TimeSpan.FromSeconds(60), clock);
        }

        throttle.IsExceeded("203.0.113.5", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
    }

    [Fact]
    public void TheFailedAttemptThatCrossesTheThresholdIsExceeded()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 21; i++)
        {
            throttle.RecordFailedAttempt("203.0.113.6", TimeSpan.FromSeconds(60), clock);
        }

        throttle.IsExceeded("203.0.113.6", 20, TimeSpan.FromSeconds(60), clock).Should().BeTrue();
    }

    [Fact]
    public void DifferentSourceIpsAreCountedIndependently()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 21; i++)
        {
            throttle.RecordFailedAttempt("203.0.113.7", TimeSpan.FromSeconds(60), clock);
        }

        throttle.IsExceeded("203.0.113.7", 20, TimeSpan.FromSeconds(60), clock).Should().BeTrue();
        throttle.IsExceeded("203.0.113.8", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
    }

    [Fact]
    public void TheWindowResetsOnceItHasElapsed()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 21; i++)
        {
            throttle.RecordFailedAttempt("203.0.113.9", TimeSpan.FromSeconds(60), clock);
        }

        throttle.IsExceeded("203.0.113.9", 20, TimeSpan.FromSeconds(60), clock).Should().BeTrue();

        clock.Advance(TimeSpan.FromSeconds(61));

        throttle.IsExceeded("203.0.113.9", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
    }

    [Fact]
    public void ANullOrEmptyRemoteIpIsNeverThrottled()
    {
        var throttle = new LoginSourceIpThrottle();
        var clock = new FakeClock(DateTimeOffset.UtcNow);

        for (var i = 0; i < 50; i++)
        {
            throttle.RecordFailedAttempt(null, TimeSpan.FromSeconds(60), clock);
            throttle.RecordFailedAttempt("", TimeSpan.FromSeconds(60), clock);
        }

        throttle.IsExceeded(null, 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
        throttle.IsExceeded("", 20, TimeSpan.FromSeconds(60), clock).Should().BeFalse();
    }
}