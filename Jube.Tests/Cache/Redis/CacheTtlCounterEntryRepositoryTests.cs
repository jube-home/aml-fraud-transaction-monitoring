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
    public sealed class CacheTtlCounterEntryRepositoryTests
    {
        [Fact]
        public async Task UpsertThenGetAllExpiredReturnsTheEntryOnceReferenceDateHasPassedAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow.AddMinutes(-10);

            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, referenceDate, 3.0);

            var expired = await repository.GetAllExpiredByTtlCounterPreferReplicaAsync(1, modelGuid, ttlCounterGuid,
                "AccountId", DateTime.UtcNow, 100);

            expired.Should().ContainSingle();
            expired[0].Value.Should().Be(3.0);
        }

        [Fact]
        public async Task GetAllExpiredExcludesEntriesNewerThanTheReferenceDateThresholdAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();

            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid,
                DateTime.UtcNow.AddMinutes(10), 3.0);

            var expired = await repository.GetAllExpiredByTtlCounterPreferReplicaAsync(1, modelGuid, ttlCounterGuid,
                "AccountId", DateTime.UtcNow, 100);

            expired.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllExpiredCorrectlyMapsDataNameAndDataValueAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow.AddMinutes(-10);

            await repository.UpsertAsync(1, modelGuid, "AccountId", "account-value-555", ttlCounterGuid,
                referenceDate, 3.0);

            var expired = await repository.GetAllExpiredByTtlCounterPreferReplicaAsync(1, modelGuid, ttlCounterGuid,
                "AccountId", DateTime.UtcNow, 100);

            expired.Should().ContainSingle();
            expired[0].DataName.Should().Be("AccountId");
            expired[0].DataValue.Should().Be("account-value-555");
        }

        [Fact]
        public async Task DeleteRemovesTheEntryFromBothTheCounterHashAndTheExpiryIndexAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow.AddMinutes(-10);

            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, referenceDate, 3.0);
            await repository.DeleteAsync(1, modelGuid, ttlCounterGuid, "AccountId", "555", referenceDate);

            var expired = await repository.GetAllExpiredByTtlCounterPreferReplicaAsync(1, modelGuid, ttlCounterGuid,
                "AccountId", DateTime.UtcNow, 100);

            expired.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAggregationSumsOnlyEntriesWithinTheDateRangeAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();
            var inRange = DateTime.UtcNow;
            var outOfRange = DateTime.UtcNow.AddDays(-2);

            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, inRange, 5.0);
            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, outOfRange, 100.0);

            var sum = await repository.GetAggregationPreferReplicaAsync(1, modelGuid, ttlCounterGuid, "AccountId",
                "555", inRange.AddMinutes(-1), inRange.AddMinutes(1));

            sum.Should().Be(5.0);
        }

        [Fact]
        public async Task GetAggregationOnNoMatchingEntriesReturnsZeroAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);

            var sum = await repository.GetAggregationPreferReplicaAsync(1, Guid.NewGuid(), Guid.NewGuid(),
                "AccountId", "555", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

            sum.Should().Be(0d);
        }

        [Fact]
        public async Task UpsertIncrementsAnExistingEntryAtTheSameReferenceDateInsteadOfDuplicatingItAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheTtlCounterEntryRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var ttlCounterGuid = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow.AddMinutes(-10);

            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, referenceDate, 3.0);
            await repository.UpsertAsync(1, modelGuid, "AccountId", "555", ttlCounterGuid, referenceDate, 4.0);

            var expired = await repository.GetAllExpiredByTtlCounterPreferReplicaAsync(1, modelGuid, ttlCounterGuid,
                "AccountId", DateTime.UtcNow, 100);

            expired.Should().ContainSingle();
            expired[0].Value.Should().Be(7.0);
        }
    }
}