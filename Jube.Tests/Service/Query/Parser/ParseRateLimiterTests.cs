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
using Jube.Service.Query.Parser;
using Xunit;

namespace Jube.Test.Service.Query.Parser
{
    [Trait("Category", "Unit")]
    public sealed class ParseRateLimiterTests
    {
        private static readonly TimeSpan window = TimeSpan.FromSeconds(60);
        private static readonly DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void TheCallPastTheLimitIsRefusedUntilTheWindowHasPassed()
        {
            var key = $"unit:{Guid.NewGuid():N}";

            ParseRateLimiter.TryAcquire(key, 2, window, start).Should().BeTrue();
            ParseRateLimiter.TryAcquire(key, 2, window, start.AddSeconds(1)).Should().BeTrue();
            ParseRateLimiter.TryAcquire(key, 2, window, start.AddSeconds(2)).Should().BeFalse();
            ParseRateLimiter.TryAcquire(key, 2, window, start.AddSeconds(59)).Should().BeFalse();
            ParseRateLimiter.TryAcquire(key, 2, window, start.AddSeconds(60)).Should().BeTrue();
        }

        [Fact]
        public void KeysAreCountedSeparately()
        {
            var one = $"unit:{Guid.NewGuid():N}";
            var two = $"unit:{Guid.NewGuid():N}";

            ParseRateLimiter.TryAcquire(one, 1, window, start).Should().BeTrue();
            ParseRateLimiter.TryAcquire(one, 1, window, start).Should().BeFalse();
            ParseRateLimiter.TryAcquire(two, 1, window, start).Should().BeTrue();
        }

        [Fact]
        public void ManyDistinctKeysNeverGrowTheTableWithoutBoundOrRefuseANewKey()
        {
            var run = Guid.NewGuid().ToString("N");

            for (var i = 0; i < 12_000; i++)
            {
                ParseRateLimiter.TryAcquire($"flood:{run}:{i}", 1, window, start).Should().BeTrue();
            }

            ParseRateLimiter.TryAcquire($"flood:{run}:late", 1, window, start.AddSeconds(61)).Should().BeTrue();
        }
    }
}
