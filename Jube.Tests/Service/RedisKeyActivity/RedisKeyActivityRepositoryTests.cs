/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Service.RedisKeyActivity;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Service.RedisKeyActivity
{
    [Trait("Category", "Service")]
    public sealed class RedisKeyActivityRepositoryTests
    {
        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestRedisConnectionString") ?? "localhost,defaultDatabase=1";

        [Fact]
        public async Task SampleAsyncReadsIdleTimeAndLeavesFrequencyNullUnderTheDefaultEvictionPolicyAsync()
        {
            var key = $"jube:test:redis-key-activity:{Guid.NewGuid()}";

            var seedConnection = await ConnectionMultiplexer.ConnectAsync(ConnectionString);

            await using (seedConnection.ConfigureAwait(false))
            {
                await seedConnection.GetDatabase().StringSetAsync(key, "smoke", TimeSpan.FromMinutes(5));
            }

            using var repository = new RedisKeyActivityRepository(ConnectionString);

            var rows = await repository.SampleAsync("jube:test:redis-key-activity:*");

            var row = rows.SingleOrDefault(r => r.Key == key);
            row.Should().NotBeNull("the seeded key should be visible to a SCAN matching its own prefix");
            row!.Type.Should().Be("String");
            row.IdleTimeSeconds.Should().HaveValue().And.Subject.Should().BeGreaterOrEqualTo(0);
            row.TimeToLiveSeconds.Should().HaveValue().And.Subject.Should().BeGreaterThan(0);

            row.Frequency.Should().BeNull();
        }

        [Fact]
        public async Task SampleAsyncNeverReturnsMoreThanTheFixedCapAsync()
        {
            using var repository = new RedisKeyActivityRepository(ConnectionString);

            var rows = await repository.SampleAsync(null);

            rows.Count.Should().BeLessOrEqualTo(100);
        }
    }
}