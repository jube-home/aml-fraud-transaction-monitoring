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
    public sealed class CacheTtlCounterRepositoryTests
    {
        [Fact]
        public async Task IncrementThenGetReturnsTheAccumulatedValueAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.IncrementTtlCounterCacheAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, 3,
                DateTime.UtcNow);
            await repository.IncrementTtlCounterCacheAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, 4,
                DateTime.UtcNow);

            var value = await repository.GetByNameDataNameDataValueAsync(1, modelGuid, ttlCounterGuid, "AccountId",
                "555");

            value.Should().Be(7);
        }

        [Fact]
        public async Task IncrementUsesDocumentedTtlCounterKeyFormatAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.IncrementTtlCounterCacheAsync(2, modelGuid, "AccountId", "555", ttlCounterGuid, 1,
                DateTime.UtcNow);

            var raw = await fake.HashGetAsync($"TtlCounter:2:{modelGuid:N}:{ttlCounterGuid:N}:AccountId", "555");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task DecrementToExactlyZeroDeletesTheHashFieldAndReturnsZeroAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.IncrementTtlCounterCacheAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, 5,
                DateTime.UtcNow);
            var result = await repository.DecrementTtlCounterCacheAsync(1, modelGuid, ttlCounterGuid, "AccountId",
                "555", 5);

            result.Should().Be(0);
            var raw = await fake.HashGetAsync($"TtlCounter:1:{modelGuid:N}:{ttlCounterGuid:N}:AccountId", "555");
            raw.HasValue.Should().BeFalse("the field is deleted once the counter reaches zero");
        }

        [Fact]
        public async Task DecrementBelowZeroAlsoClampsToZeroAndDeletesTheHashFieldAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.IncrementTtlCounterCacheAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, 5,
                DateTime.UtcNow);
            var result = await repository.DecrementTtlCounterCacheAsync(1, modelGuid, ttlCounterGuid, "AccountId",
                "555", 8);

            result.Should().Be(0, "the current implementation clamps an overshoot decrement to zero rather than " +
                                  "reporting the negative value");
            var raw = await fake.HashGetAsync($"TtlCounter:1:{modelGuid:N}:{ttlCounterGuid:N}:AccountId", "555");
            raw.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task DecrementAboveZeroReturnsTheRemainingValueAndKeepsTheHashFieldAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.IncrementTtlCounterCacheAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, 5,
                DateTime.UtcNow);
            var result = await repository.DecrementTtlCounterCacheAsync(1, modelGuid, ttlCounterGuid, "AccountId",
                "555", 2);

            result.Should().Be(3);
            var raw = await fake.HashGetAsync($"TtlCounter:1:{modelGuid:N}:{ttlCounterGuid:N}:AccountId", "555");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task GetOnANeverIncrementedCounterReturnsZeroDespiteTheInternalCastFailureAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterRepository(fake, TestLog.NoOp);

            var value = await repository.GetByNameDataNameDataValueAsync(1, Guid.NewGuid(), Guid.NewGuid(),
                "AccountId", "555");

            value.Should().Be(0);
        }
    }
}