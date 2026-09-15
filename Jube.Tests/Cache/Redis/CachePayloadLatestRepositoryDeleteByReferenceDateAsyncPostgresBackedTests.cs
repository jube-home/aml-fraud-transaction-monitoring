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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Extensions;
using Jube.Test.Infrastructure;
using LinqToDB;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CachePayloadLatestRepositoryDeleteByReferenceDateAsyncPostgresBackedTests : IAsyncLifetime
    {
        private readonly List<long> createdBatchIds = [];

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
            if (createdBatchIds.Count == 0)
            {
                return;
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ConnectionString, TestLog.NoOp);

            await dbContext.GetTable<CachePayloadLatestRemovalBatchEntry>()
                .Where(w => createdBatchIds.Contains(w.CachePayloadLatestRemovalBatchId)).DeleteAsync();
            await dbContext.GetTable<CachePayloadLatestRemovalBatchResponseTime>()
                .Where(w => createdBatchIds.Contains(w.CachePayloadLatestRemovalBatchId)).DeleteAsync();
            await dbContext.GetTable<CachePayloadLatestRemovalBatch>()
                .Where(w => createdBatchIds.Contains(w.Id)).DeleteAsync();
        }

        [Fact]
        public async Task
            DeleteByReferenceDateAsyncAppliesAPerSearchKeyOverrideAndFallsBackToTheGlobalThresholdAndPersistsTheRealReferenceDateAsync()
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ConnectionString, TestLog.NoOp);
            try
            {
                _ = await dbContext.GetTable<CachePayloadLatestRemovalBatch>().Take(1).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "This test needs a running, migrated Jube Postgres schema - point " +
                    "JubeTestConnectionString/ConnectionString at one.", ex);
            }

            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CachePayloadLatestRepository(ConnectionString, fake, TestLog.NoOp);
            var tenant = 1;
            var model = Guid.NewGuid();

            var referenceDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var globalThreshold = new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            const string overriddenSearchKeyName = "AccountId";
            var overriddenEntryTimestamp = new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var searchKeys = new List<(string name, string interval, int intervalValue)>
            {
                (overriddenSearchKeyName, "d", 30)
            };

            const string fallbackSearchKeyName = "IpAddress";
            var fallbackEntryTimestamp = new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc);

            var referenceDateLatestHashKey = $"ReferenceDateLatest:{tenant}:{model:N}";
            var overriddenSortedSetKey = $"ReferenceDateLatest:{tenant}:{model:N}:{overriddenSearchKeyName}";
            var fallbackSortedSetKey = $"ReferenceDateLatest:{tenant}:{model:N}:{fallbackSearchKeyName}";

            await fake.HashSetAsync(referenceDateLatestHashKey, overriddenSearchKeyName,
                overriddenEntryTimestamp.ToUnixTimeMilliSeconds());
            await fake.SortedSetAddAsync(overriddenSortedSetKey, "expired-account-value",
                overriddenEntryTimestamp.ToUnixTimeMilliSeconds());

            await fake.HashSetAsync(referenceDateLatestHashKey, fallbackSearchKeyName,
                fallbackEntryTimestamp.ToUnixTimeMilliSeconds());
            await fake.SortedSetAddAsync(fallbackSortedSetKey, "expired-ip-value",
                fallbackEntryTimestamp.ToUnixTimeMilliSeconds());

            await repository.DeleteByReferenceDateAsync(tenant, model, referenceDate, globalThreshold, 1000,
                searchKeys);

            var remainingOverridden = await fake.SortedSetRangeByScoreWithScoresAsync(overriddenSortedSetKey);
            remainingOverridden.Should().BeEmpty(
                "AccountId's own 30-day override (referenceDate - 30d = 2023-12-02) is older than the " +
                "seeded entry's 2023-06-01 timestamp, so it should be deleted under the override");

            var remainingFallback = await fake.SortedSetRangeByScoreWithScoresAsync(fallbackSortedSetKey);
            remainingFallback.Should().BeEmpty(
                "IpAddress has no searchKeys override, so it should fall back to the flat global threshold " +
                "(2023-01-01), which the seeded 2022-06-01 entry is older than");

            var batches = await dbContext.GetTable<CachePayloadLatestRemovalBatch>()
                .Where(w => w.EntityAnalysisModelGuid == model).ToListAsync();
            createdBatchIds.AddRange(batches.Select(b => b.Id));

            batches.Should().HaveCount(2, "one removal batch per expired search key");
            batches.Should().OnlyContain(b => b.ReferenceDate == referenceDate,
                "the persisted audit ReferenceDate must always be the real referenceDate, never a threshold");
        }
    }
}