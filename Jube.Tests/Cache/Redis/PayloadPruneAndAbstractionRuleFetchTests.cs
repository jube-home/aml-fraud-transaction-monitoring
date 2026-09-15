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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;
using BackgroundContext = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PayloadPruneAndAbstractionRuleFetchTests : IAsyncLifetime
    {
        private Guid modelGuidForCleanup;

        private static string ConnectionString =>
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString")
            ??
            "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;";

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var payloadBatchIds = await dbContext.GetTable<CachePayloadRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuidForCleanup).Select(s => s.Id).ToListAsync();
            if (payloadBatchIds.Count > 0)
            {
                await dbContext.GetTable<CachePayloadRemovalBatchEntry>()
                    .Where(w => payloadBatchIds.Contains(w.CachePayloadRemovalBatchId)).DeleteAsync();
                await dbContext.GetTable<CachePayloadRemovalBatchResponseTime>()
                    .Where(w => payloadBatchIds.Contains(w.CachePayloadRemovalBatchId)).DeleteAsync();
                await dbContext.GetTable<CachePayloadRemovalBatch>()
                    .Where(w => payloadBatchIds.Contains(w.Id)).DeleteAsync();
            }

            var latestBatchIds = await dbContext.GetTable<CachePayloadLatestRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuidForCleanup).Select(s => s.Id).ToListAsync();
            if (latestBatchIds.Count > 0)
            {
                await dbContext.GetTable<CachePayloadLatestRemovalBatchEntry>()
                    .Where(w => latestBatchIds.Contains(w.CachePayloadLatestRemovalBatchId)).DeleteAsync();
                await dbContext.GetTable<CachePayloadLatestRemovalBatchResponseTime>()
                    .Where(w => latestBatchIds.Contains(w.CachePayloadLatestRemovalBatchId)).DeleteAsync();
                await dbContext.GetTable<CachePayloadLatestRemovalBatch>()
                    .Where(w => latestBatchIds.Contains(w.Id)).DeleteAsync();
            }
        }

        [Fact]
        public async Task
            PayloadPrunedByTheRealPruneJobDisappearsFromTheAbstractionRuleFetchPathButSurvivorsRemainAsync()
        {
            const int tenant = 920001;
            var modelGuid = Guid.NewGuid();
            modelGuidForCleanup = modelGuid;

            var cacheService = TestCacheService.Create(out _, TestLog.NoOp, ConnectionString);

            const string searchKey = "AccountId";
            const string searchKeyValue = "ACC-FETCH-TEST";

            var expiredEntryGuid = Guid.NewGuid();
            var survivingEntryGuid = Guid.NewGuid();
            var expiredAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var survivingAt = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
            var referenceDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);

            var expiredPayload = new DictionaryNoBoxing<int>();
            expiredPayload.Add(0, "expired-payload-marker");
            var survivingPayload = new DictionaryNoBoxing<int>();
            survivingPayload.Add(0, "surviving-payload-marker");

            await cacheService.CachePayloadRepository.InsertAsync(tenant, modelGuid, expiredPayload, expiredAt,
                expiredEntryGuid);
            await cacheService.CachePayloadRepository.InsertPayloadJournalAndLedgerAsync(tenant, modelGuid,
                searchKey, searchKeyValue, expiredAt, expiredEntryGuid);
            await cacheService.CachePayloadRepository.InsertAsync(tenant, modelGuid, survivingPayload, survivingAt,
                survivingEntryGuid);
            await cacheService.CachePayloadRepository.InsertPayloadJournalAndLedgerAsync(tenant, modelGuid,
                searchKey, searchKeyValue, survivingAt, survivingEntryGuid);

            var keysBefore = await cacheService.CachePayloadRepository.GetSortedSetKeysAsync(tenant, modelGuid,
                searchKey, searchKeyValue, 100, Guid.NewGuid());
            keysBefore.Select(k => k.ToString()).Should()
                .Contain(expiredEntryGuid.ToString("N")).And.Contain(survivingEntryGuid.ToString("N"));

            var batchBefore = await cacheService.CachePayloadRepository.GetPayloadBatchAsync(tenant, modelGuid,
                keysBefore, Guid.NewGuid());
            batchBefore.Should().ContainKey(expiredEntryGuid.ToString("N"));
            batchBefore.Should().ContainKey(survivingEntryGuid.ToString("N"));

            await cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(tenant, modelGuid,
                referenceDate);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { TenantRegistryId = tenant, Guid = modelGuid },
                Cache = { CacheTtlInterval = 'd', CacheTtlIntervalValue = 30 }
            };
            var context = new BackgroundContext
            {
                Services =
                {
                    CacheService = cacheService,
                    Log = TestLog.NoOp,
                    DynamicEnvironment = TestDynamicEnvironment.Create(),
                    TaskCoordinator = new TaskCoordinator(new CancellationTokenProvider())
                }
            };
            var starter = new CachePruneTaskStarter(context);
            await starter.PruneModelAsync(model, 1000);

            var keysAfter = await cacheService.CachePayloadRepository.GetSortedSetKeysAsync(tenant, modelGuid,
                searchKey, searchKeyValue, 100, Guid.NewGuid());
            keysAfter.Select(k => k.ToString()).Should()
                .NotContain(expiredEntryGuid.ToString("N"))
                .And.Contain(survivingEntryGuid.ToString("N"));

            var batchAfter = await cacheService.CachePayloadRepository.GetPayloadBatchAsync(tenant, modelGuid,
                keysAfter, Guid.NewGuid());
            batchAfter.Should().NotContainKey(expiredEntryGuid.ToString("N"),
                "the abstraction rule fetch path must no longer be able to retrieve the pruned payload document");
            batchAfter.Should().ContainKey(survivingEntryGuid.ToString("N"),
                "the surviving payload document must still be retrievable through the same fetch path");

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var batch = await dbContext.GetTable<CachePayloadRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).SingleOrDefaultAsync();
            batch.Should().NotBeNull();
            batch!.ExpiredSortedSetCount.Should().Be(1,
                "only the expired entry should have been picked up for deletion, not the surviving one");

            var entry = await dbContext.GetTable<CachePayloadRemovalBatchEntry>()
                .Where(w => w.CachePayloadRemovalBatchId == batch.Id).SingleOrDefaultAsync();
            entry.Should().NotBeNull();
            entry!.EntityAnalysisModelGuid.Should().Be(expiredEntryGuid);
        }

        [Fact]
        public async Task PayloadLatestWrittenThroughTheRealUpsertIsPrunedAndAuditedByTheRealPruneJobAsync()
        {
            const int tenant = 920002;
            var modelGuid = Guid.NewGuid();
            modelGuidForCleanup = modelGuid;

            var cacheService = TestCacheService.Create(out var redis, TestLog.NoOp, ConnectionString);

            const string searchKey = "AccountId";
            const string expiredValue = "expired-account";
            const string survivingValue = "surviving-account";

            var expiredAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var survivingAt = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);
            var referenceDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);

            await cacheService.CachePayloadLatestRepository.UpsertAsync(tenant, modelGuid, expiredAt,
                Guid.NewGuid(), searchKey, expiredValue);
            await cacheService.CachePayloadLatestRepository.UpsertAsync(tenant, modelGuid, survivingAt,
                Guid.NewGuid(), searchKey, survivingValue);

            await cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(tenant, modelGuid,
                referenceDate);

            var model = new EntityAnalysisModelDomain
            {
                Instance = { TenantRegistryId = tenant, Guid = modelGuid },
                Cache = { CacheTtlInterval = 'd', CacheTtlIntervalValue = 30 }
            };

            var context = new BackgroundContext
            {
                Services =
                {
                    CacheService = cacheService,
                    Log = TestLog.NoOp,
                    DynamicEnvironment = TestDynamicEnvironment.Create(),
                    TaskCoordinator = new TaskCoordinator(new CancellationTokenProvider())
                }
            };
            var starter = new CachePruneTaskStarter(context);
            await starter.PruneModelAsync(model, 1000);

            var latestReferenceDateSortedSetKey = $"ReferenceDateLatest:{tenant}:{modelGuid:N}:{searchKey}";
            var remaining = await redis.SortedSetRangeByScoreWithScoresAsync(latestReferenceDateSortedSetKey);
            remaining.Select(s => s.Element.ToString()).Should()
                .NotContain(expiredValue)
                .And.Contain(survivingValue);

            await using var dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(ConnectionString, TestLog.NoOp);

            var latestBatch = await dbContext.GetTable<CachePayloadLatestRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).SingleOrDefaultAsync();
            latestBatch.Should().NotBeNull();
            latestBatch!.ReferenceDate.Should().Be(referenceDate);

            var latestEntry = await dbContext.GetTable<CachePayloadLatestRemovalBatchEntry>()
                .Where(w => w.CachePayloadLatestRemovalBatchId == latestBatch.Id).SingleOrDefaultAsync();
            latestEntry.Should().NotBeNull();
            latestEntry!.Value.Should().Be(expiredValue);
        }
    }
}