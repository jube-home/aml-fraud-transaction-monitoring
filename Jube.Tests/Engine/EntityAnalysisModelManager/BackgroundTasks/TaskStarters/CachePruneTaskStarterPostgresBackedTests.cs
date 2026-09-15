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
using Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters;
using Jube.Extensions;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Context;
using EntityAnalysisModelDomain = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CachePruneTaskStarterPostgresBackedTests : IAsyncLifetime
    {
        private Guid modelGuid;

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
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ConnectionString, TestLog.NoOp);

            var payloadBatchIds = await dbContext.GetTable<CachePayloadRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).Select(s => s.Id).ToListAsync();

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
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).Select(s => s.Id).ToListAsync();
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
            PruneModelAsyncDeletesOnlyWhatsExpiredAcrossPayloadAndPayloadLatestAndAuditsBothToPostgresAsync()
        {
            await using (var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                             ConnectionString, TestLog.NoOp))
            {
                try
                {
                    _ = await dbContext.GetTable<CachePayloadRemovalBatch>().Take(1).ToListAsync();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "This test needs a running, migrated Jube Postgres schema - point " +
                        "JubeTestConnectionString/ConnectionString at one.", ex);
                }
            }

            var cacheService = TestCacheService.Create(out var redis, TestLog.NoOp, ConnectionString);
            var tenant = 4321;
            modelGuid = Guid.NewGuid();

            var referenceDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
            var expiredTimestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var survivingTimestamp = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc);

            var expiredInstanceGuid = Guid.NewGuid();
            var survivingInstanceGuid = Guid.NewGuid();

            var payloadReferenceDateKey = $"ReferenceDate:{tenant}:{modelGuid:N}";
            var payloadHashKey = $"Payload:{tenant}:{modelGuid:N}";

            await redis.SortedSetAddAsync(payloadReferenceDateKey, expiredInstanceGuid.ToString("N"),
                expiredTimestamp.ToUnixTimeMilliSeconds());

            await redis.SortedSetAddAsync(payloadReferenceDateKey, survivingInstanceGuid.ToString("N"),
                survivingTimestamp.ToUnixTimeMilliSeconds());

            await redis.HashSetAsync(payloadHashKey, expiredInstanceGuid.ToString("N"), new byte[] { 1, 2, 3 });
            await redis.HashSetAsync(payloadHashKey, survivingInstanceGuid.ToString("N"), new byte[] { 4, 5, 6 });

            const string searchKeyName = "AccountId";
            const string expiredSearchKeyValue = "expired-account";
            const string survivingSearchKeyValue = "surviving-account";

            var latestReferenceDateHashKey = $"ReferenceDateLatest:{tenant}:{modelGuid:N}";
            var latestReferenceDateSortedSetKey = $"ReferenceDateLatest:{tenant}:{modelGuid:N}:{searchKeyName}";
            var latestPayloadHashKey = $"PayloadLatest:{tenant}:{modelGuid:N}:{searchKeyName}";

            await redis.HashSetAsync(latestReferenceDateHashKey, searchKeyName,
                expiredTimestamp.ToUnixTimeMilliSeconds());

            await redis.SortedSetAddAsync(latestReferenceDateSortedSetKey, expiredSearchKeyValue,
                expiredTimestamp.ToUnixTimeMilliSeconds());

            await redis.SortedSetAddAsync(latestReferenceDateSortedSetKey, survivingSearchKeyValue,
                survivingTimestamp.ToUnixTimeMilliSeconds());

            await redis.HashSetAsync(latestPayloadHashKey, expiredSearchKeyValue, new byte[] { 7, 8, 9 });
            await redis.HashSetAsync(latestPayloadHashKey, survivingSearchKeyValue, new byte[] { 10, 11, 12 });

            await cacheService.CacheReferenceDateRepository.UpsertReferenceDateAsync(tenant, modelGuid, referenceDate);

            var model = new EntityAnalysisModelDomain
            {
                Instance =
                {
                    TenantRegistryId = tenant,
                    Guid = modelGuid
                },
                Cache =
                {
                    CacheTtlInterval = 'd',
                    CacheTtlIntervalValue = 30
                }
            };

            var context = new Context
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

            var remainingPayloadReferenceDates =
                await redis.SortedSetRangeByScoreWithScoresAsync(payloadReferenceDateKey);
            remainingPayloadReferenceDates.Select(s => s.Element.ToString()).Should()
                .NotContain(expiredInstanceGuid.ToString("N"))
                .And.Contain(survivingInstanceGuid.ToString("N"));

            (await redis.HashGetAsync(payloadHashKey, expiredInstanceGuid.ToString("N"))).HasValue.Should()
                .BeFalse("the expired payload entry must be deleted from the hash too, not just the index");
            (await redis.HashGetAsync(payloadHashKey, survivingInstanceGuid.ToString("N"))).HasValue.Should()
                .BeTrue("the surviving payload entry must not be touched");

            var remainingLatest = await redis.SortedSetRangeByScoreWithScoresAsync(latestReferenceDateSortedSetKey);
            remainingLatest.Select(s => s.Element.ToString()).Should()
                .NotContain(expiredSearchKeyValue)
                .And.Contain(survivingSearchKeyValue);

            (await redis.HashGetAsync(latestPayloadHashKey, expiredSearchKeyValue)).HasValue.Should().BeFalse(
                "the expired PayloadLatest entry must be deleted from the hash too, not just the index");
            (await redis.HashGetAsync(latestPayloadHashKey, survivingSearchKeyValue)).HasValue.Should()
                .BeTrue("the surviving PayloadLatest entry must not be touched");

            await using var assertDbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ConnectionString, TestLog.NoOp);

            var payloadBatches = await assertDbContext.GetTable<CachePayloadRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).ToListAsync();
            payloadBatches.Should().ContainSingle();
            payloadBatches.Single().ExpiredSortedSetCount.Should().Be(1,
                "only the expired instance should have been picked up for deletion, not the surviving one");

            var payloadBatchEntries = await assertDbContext.GetTable<CachePayloadRemovalBatchEntry>()
                .Where(w => w.CachePayloadRemovalBatchId == payloadBatches.Single().Id).ToListAsync();
            payloadBatchEntries.Should().ContainSingle();
            payloadBatchEntries.Single().EntityAnalysisModelGuid.Should().Be(expiredInstanceGuid);

            var latestBatches = await assertDbContext.GetTable<CachePayloadLatestRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == modelGuid).ToListAsync();
            latestBatches.Should().ContainSingle();
            latestBatches.Single().ReferenceDate.Should().Be(referenceDate,
                "the audit ReferenceDate column must be the real referenceDate, not the threshold");

            var latestBatchEntries = await assertDbContext.GetTable<CachePayloadLatestRemovalBatchEntry>()
                .Where(w => w.CachePayloadLatestRemovalBatchId == latestBatches.Single().Id).ToListAsync();
            latestBatchEntries.Should().ContainSingle();
            latestBatchEntries.Single().Value.Should().Be(expiredSearchKeyValue);
        }
    }
}