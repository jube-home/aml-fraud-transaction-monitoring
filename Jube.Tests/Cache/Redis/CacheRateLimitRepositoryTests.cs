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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheRateLimitRepositoryTests
    {
        [Fact]
        public async Task EachCallIncrementsAndReturnsTheRunningCountAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheRateLimitRepository(fake, TestLog.NoOp);

            (await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.10", TimeSpan.FromSeconds(60)))
                .Should().Be(1);
            (await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.10", TimeSpan.FromSeconds(60)))
                .Should().Be(2);
            (await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.10", TimeSpan.FromSeconds(60)))
                .Should().Be(3);
        }

        [Fact]
        public async Task DifferentSourceKeysAreCountedIndependentlyAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheRateLimitRepository(fake, TestLog.NoOp);

            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.11", TimeSpan.FromSeconds(60));
            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.11", TimeSpan.FromSeconds(60));

            (await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.12", TimeSpan.FromSeconds(60)))
                .Should().Be(1, "a different source key must start its own count");
        }

        [Fact]
        public async Task DifferentRateLimitNamesAreCountedIndependentlyForTheSameSourceKeyAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheRateLimitRepository(fake, TestLog.NoOp);

            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.13", TimeSpan.FromSeconds(60));

            (await repository.IncrementRateLimitCacheAsync("SomethingElse", "203.0.113.13", TimeSpan.FromSeconds(60)))
                .Should().Be(1, "a different rate limit name is a distinct counter even for the same source key");
        }

        [Fact]
        public async Task TheKeyIsExpiredOnceOnTheFirstIncrementOnlyAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheRateLimitRepository(fake, TestLog.NoOp);
            var window = TimeSpan.FromSeconds(30);

            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.14", window);
            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.14", window);
            await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.14", window);

            fake.KeyExpirations.Should().HaveCount(1,
                "the TTL only needs setting once - re-setting it on every increment would keep sliding the " +
                "window forward and the counter would never reset");
            var (key, expiry) = fake.KeyExpirations.Single();
            key.ToString().Should().Be("RateLimit:GlobalHttp:203.0.113.14");
            expiry.Should().Be(window);
        }

        [Fact]
        public async Task AnExceptionFromRedisIsSwallowedAndTreatedAsNotExceededAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = "HashIncrementAsync"
            };
            var repository = new CacheRateLimitRepository(fake, TestLog.NoOp);

            var count = await repository.IncrementRateLimitCacheAsync("GlobalHttp", "203.0.113.15",
                TimeSpan.FromSeconds(60));

            count.Should().Be(0, "a Redis failure must fail open rather than take the whole request path down");
        }
    }
}