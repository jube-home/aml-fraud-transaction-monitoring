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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis.Models;
using Jube.Data.Poco;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministration;
using Jube.ResilientRedisConnection;
using Jube.Test.Infrastructure;
using Xunit;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using EntityAnalysisModelTtlCounter =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelTtlCounter;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Unit")]
    public sealed class TtlCounterAdministrationTaskStarterTests
    {
        private const double Tolerance = 0.001;

        private static EntityAnalysisModelDomain NewModel(int tenantRegistryId,
            out FakeHybridResilientRedisDatabase redis)
        {
            var cacheService = TestCacheService.Create(out redis);
            var model = new EntityAnalysisModelDomain
            {
                Instance =
                {
                    TenantRegistryId = tenantRegistryId,
                    Guid = Guid.NewGuid()
                },
                Services =
                {
                    Log = TestLog.NoOp,
                    CacheService = cacheService
                }
            };
            return model;
        }

        private static EntityAnalysisModelTtlCounter NewCounter(string dataName = "AccountId")
        {
            return new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "TransactionCount",
                TtlCounterDataName = dataName,
                TtlCounterInterval = "d",
                TtlCounterValue = 30
            };
        }

        private static async Task SeedCounterAndEntryAsync(EntityAnalysisModelDomain model,
            EntityAnalysisModelTtlCounter counter, string dataValue, DateTime referenceDate, double initialCount)
        {
            await model.Services.CacheService.CacheTtlCounterRepository.IncrementTtlCounterCacheAsync(
                model.Instance.TenantRegistryId, model.Instance.Guid, counter.TtlCounterDataName, dataValue,
                counter.Guid, initialCount, referenceDate);

            await model.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                model.Instance.TenantRegistryId, model.Instance.Guid, counter.TtlCounterDataName, dataValue,
                counter.Guid, referenceDate, initialCount);
        }

        [Fact]
        public async Task ProcessTtlCounterDeprecationDecrementsTheCounterByTheExpiredEntrysValueAsync()
        {
            var model = NewModel(1, out _);
            var counter = NewCounter();
            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await SeedCounterAndEntryAsync(model, counter, "555000111", referenceDate, 10);

            var expiredEntry = new ExpiredTtlCounterEntry
            {
                DataName = counter.TtlCounterDataName,
                DataValue = "555000111",
                Value = 4,
                ReferenceDate = referenceDate
            };
            var ttlCounterAdministrationCacheService = new TtlCounterAdministrationCacheService(model);
            var batch = new CacheTtlCounterEntryRemovalBatch { Id = 1 };
            var entries = new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();

            await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter, expiredEntry, entries, batch);

            var remaining = await model.Services.CacheService.CacheTtlCounterRepository
                .GetByNameDataNameDataValueAsync(model.Instance.TenantRegistryId, model.Instance.Guid, counter.Guid,
                    counter.TtlCounterDataName, "555000111");
            remaining.Should().Be(6, "10 - 4 = 6");
        }

        [Fact]
        public async Task ProcessTtlCounterDeprecationDeletesTheExpiredCounterEntryAsync()
        {
            var model = NewModel(1, out _);
            var counter = NewCounter();
            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await SeedCounterAndEntryAsync(model, counter, "555000111", referenceDate, 10);

            var expiredEntry = new ExpiredTtlCounterEntry
            {
                DataName = counter.TtlCounterDataName,
                DataValue = "555000111",
                Value = 4,
                ReferenceDate = referenceDate
            };
            var ttlCounterAdministrationCacheService = new TtlCounterAdministrationCacheService(model);
            var batch = new CacheTtlCounterEntryRemovalBatch { Id = 1 };
            var entries = new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();

            await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter, expiredEntry, entries, batch);

            var stillExpired = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAllExpiredByTtlCounterPreferReplicaAsync(model.Instance.TenantRegistryId, model.Instance.Guid,
                    counter.Guid, counter.TtlCounterDataName, referenceDate.AddDays(1), 100);
            stillExpired.Should().BeEmpty("the entry was deleted from both the hash and the expiry index");
        }

        [Fact]
        public async Task ProcessTtlCounterDeprecationRecordsTheRemovalBatchEntryUsingDataValueNotDataNameAsync()
        {
            var model = NewModel(1, out _);
            var counter = NewCounter();
            var referenceDate = new DateTime(2024, 3, 10, 0, 0, 0, DateTimeKind.Utc);
            await SeedCounterAndEntryAsync(model, counter, "555000111", referenceDate, 10);

            var expiredEntry = new ExpiredTtlCounterEntry
            {
                DataName = counter.TtlCounterDataName,
                DataValue = "555000111",
                Value = 4,
                ReferenceDate = referenceDate
            };
            var ttlCounterAdministrationCacheService = new TtlCounterAdministrationCacheService(model);
            var batch = new CacheTtlCounterEntryRemovalBatch { Id = 42 };
            var entries = new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();

            await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter, expiredEntry, entries, batch);

            entries.Should().ContainSingle();
            var entry = entries.Single();
            entry.CacheTtlCounterEntryRemovalBatchId.Should().Be(42);
            entry.Value.Should().Be("555000111",
                "the audit Value column must be the per-record DataValue, not DataName");
            entry.DecrementCount.Should().Be(4);
            entry.RevisedCount.Should().Be(6);
            entry.ReferenceDate.Should().Be(referenceDate);
        }

        [Fact]
        public async Task ADecrementFailureDoesNotPreventTheEntryFromStillBeingDeletedAsync()
        {
            var model = NewModel(1, out var redis);
            var counter = NewCounter();
            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await SeedCounterAndEntryAsync(model, counter, "555000111", referenceDate, 10);

            var expiredEntry = new ExpiredTtlCounterEntry
            {
                DataName = counter.TtlCounterDataName,
                DataValue = "555000111",
                Value = 4,
                ReferenceDate = referenceDate
            };
            var ttlCounterAdministrationCacheService = new TtlCounterAdministrationCacheService(model);
            var batch = new CacheTtlCounterEntryRemovalBatch { Id = 1 };
            var entries = new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();

            redis.ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashDecrementAsync);

            var act = async () => await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter, expiredEntry, entries, batch);

            await act.Should().NotThrowAsync();

            var stillExpired = await model.Services.CacheService.CacheTtlCounterEntryRepository
                .GetAllExpiredByTtlCounterPreferReplicaAsync(model.Instance.TenantRegistryId, model.Instance.Guid,
                    counter.Guid, counter.TtlCounterDataName, referenceDate.AddDays(1), 100);
            stillExpired.Should().BeEmpty("the delete should still happen even though the decrement failed");
        }

        [Fact]
        public async Task MultipleExpiredEntriesProcessedInSequenceDoNotCrossContaminateBatchEntriesAsync()
        {
            var model = NewModel(1, out _);
            var counter = NewCounter();
            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            await SeedCounterAndEntryAsync(model, counter, "account-a", referenceDate, 10);
            await SeedCounterAndEntryAsync(model, counter, "account-b", referenceDate, 20);

            var ttlCounterAdministrationCacheService = new TtlCounterAdministrationCacheService(model);
            var batch = new CacheTtlCounterEntryRemovalBatch { Id = 1 };
            var entries = new ConcurrentBag<CacheTtlCounterEntryRemovalBatchEntry>();

            await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter,
                new ExpiredTtlCounterEntry
                {
                    DataName = counter.TtlCounterDataName, DataValue = "account-a", Value = 3,
                    ReferenceDate = referenceDate
                },
                entries, batch);
            await TtlCounterAdministrationTaskStarter.ProcessTtlCounterDeprecationAsync(model,
                ttlCounterAdministrationCacheService, counter,
                new ExpiredTtlCounterEntry
                {
                    DataName = counter.TtlCounterDataName, DataValue = "account-b", Value = 7,
                    ReferenceDate = referenceDate
                },
                entries, batch);

            entries.Should().HaveCount(2);
            entries.Should().Contain(e =>
                e.Value == "account-a" && Math.Abs(e.DecrementCount - 3) < Tolerance &&
                Math.Abs(e.RevisedCount - 7) < 0.001);
            entries.Should().Contain(e =>
                e.Value == "account-b" && Math.Abs(e.DecrementCount - 7) < Tolerance &&
                Math.Abs(e.RevisedCount - 13) < 0.001);
        }
    }
}