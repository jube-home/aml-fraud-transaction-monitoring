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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheActivationWatcherLastLocationRepositoryTests
    {
        private static readonly DateTime first = new(2026, 10, 21, 10, 0, 0, 123, DateTimeKind.Utc);
        private static readonly DateTime second = first.AddMinutes(20);

        [Fact]
        public async Task TheFirstSightingHasNoPreviousAndTheSecondFindsTheFirstAsync()
        {
            var repository = new CacheActivationWatcherLastLocationRepository(new FakeHybridResilientRedisDatabase(),
                TestLog.NoOp);

            var none = await repository.ExchangeAsync(7, "AccountId", "ACC-1", 51.5, -0.12, first);
            var previous = await repository.ExchangeAsync(7, "AccountId", "ACC-1", 48.86, 2.35, second);
            var latest = await repository.ExchangeAsync(7, "AccountId", "ACC-1", 40.7, -74, second.AddMinutes(5));

            none.Should().BeNull();
            previous.Should().Be(new LastLocation(51.5, -0.12, first));
            latest.Should().Be(new LastLocation(48.86, 2.35, second));
        }

        [Fact]
        public async Task EntitiesTenantsAndKeysDoNotShareALocationAsync()
        {
            var repository = new CacheActivationWatcherLastLocationRepository(new FakeHybridResilientRedisDatabase(),
                TestLog.NoOp);

            await repository.ExchangeAsync(7, "AccountId", "ACC-1", 51.5, -0.12, first);

            (await repository.ExchangeAsync(7, "AccountId", "ACC-2", 1, 1, second)).Should().BeNull();
            (await repository.ExchangeAsync(8, "AccountId", "ACC-1", 1, 1, second)).Should().BeNull();
            (await repository.ExchangeAsync(7, "CardNumber", "ACC-1", 1, 1, second)).Should().BeNull();
        }

        [Fact]
        public async Task TheLocationIsKeptUnderTheDocumentedKeyWithARetentionAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheActivationWatcherLastLocationRepository(fake, TestLog.NoOp);

            await repository.ExchangeAsync(7, "AccountId", "ACC-1", 51.5, -0.12, first);

            var raw = await fake.StringGetAsync("ActivationWatcherLastLocation:7:AccountId:ACC-1");

            raw.HasValue.Should().BeTrue();
            raw.ToString().Should().StartWith("51.5|-0.12|");
            CacheActivationWatcherLastLocationRepository.Retention.Should().Be(TimeSpan.FromDays(14));
        }

        [Fact]
        public async Task AMalformedStoredValueIsTreatedAsNoPreviousAndIsReplacedAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheActivationWatcherLastLocationRepository(fake, TestLog.NoOp);

            await fake.StringSetAsync("ActivationWatcherLastLocation:7:AccountId:ACC-1", "garbage");

            (await repository.ExchangeAsync(7, "AccountId", "ACC-1", 1, 2, first)).Should().BeNull();
            (await repository.ExchangeAsync(7, "AccountId", "ACC-1", 3, 4, second)).Should()
                .Be(new LastLocation(1, 2, first));
        }

        [Fact]
        public async Task AVeryLongKeyValueIsCappedSoTheKeyStaysBoundedAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheActivationWatcherLastLocationRepository(fake, TestLog.NoOp);
            var huge = new string('x', 5000);

            await repository.ExchangeAsync(7, "AccountId", huge, 1, 2, first);

            (await fake.StringGetAsync($"ActivationWatcherLastLocation:7:AccountId:{new string('x', 256)}"))
                .HasValue.Should().BeTrue();
        }
    }
}
