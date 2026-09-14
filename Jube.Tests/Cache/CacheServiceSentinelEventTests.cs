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

using FluentAssertions;
using Jube.Cache;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheServiceSentinelEventTests
    {
        [Fact]
        public void HandleSentinelEventCapturesEventAndLogsWarning()
        {
            RedisSentinelEventCapture.DrainAll();
            var log = new TestLog();
            var channel = RedisChannel.Literal("+switch-master");

            CacheService.HandleSentinelEvent(log, channel, "mymaster 10.0.0.1 6379 10.0.0.2 6379");

            var captured = RedisSentinelEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].Channel.Should().Be(channel.ToString());
            captured[0].Message.Should().Be("mymaster 10.0.0.1 6379 10.0.0.2 6379");

            log.Entries.Should().ContainSingle(e => e.Level == "WARN" && e.Message.Contains("Sentinel"));
        }
    }
}