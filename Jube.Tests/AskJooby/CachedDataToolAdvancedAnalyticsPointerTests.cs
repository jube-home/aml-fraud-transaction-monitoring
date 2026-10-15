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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.AskJooby.DataCache.Models;
using Jube.AskJooby.Tools.DataCache;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.AskJoobyDataCache;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using Jube.Analytics.Datasets;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class CachedDataToolAdvancedAnalyticsPointerTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdTenantIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (createdTenantIds.Count == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();

            await dbContext.AdvancedAnalyticsCache.Where(w => createdTenantIds.Contains(w.TenantRegistryId))
                .DeleteAsync();

            await dbContext.AskJoobyDataCache.Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value))
                .DeleteAsync();

            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedTenantAsync(DbContext dbContext)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}CachedDataToolAdvancedAnalyticsTenant{Guid.NewGuid():N}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(id);

            return id;
        }

        [Fact]
        public async Task InspectPreviewAndQueryAllWorkAgainstAnAdvancedAnalyticsPointerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var advancedAnalyticsCacheRepository = new AdvancedAnalyticsCacheRepository(dbContext, tenantId, "rosa");

            var entry = await advancedAnalyticsCacheRepository.PutAsync(
                AdvancedAnalyticsPacking.Pack([10, 20, 1, 30, 40, 0]), rows: 2, columns: 3);

            var client = new AskJoobyDataCacheClient(
                new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa"), null,
                advancedAnalyticsCache: new AdvancedAnalyticsCachedTableReader(dbContext, tenantId, "rosa"));

            var tool = new CachedDataTool(client);
            var pointer = entry.Guid.ToString();

            var schema = await tool.InspectDataSchema(pointer);
            schema.Should().Contain("2 row(s), 3 column(s)");
            schema.Should().Contain("Column0");
            schema.Should().Contain("Label");

            var preview = await tool.PreviewDataRows(pointer);
            preview.Should().Contain("Column0=10").And.Contain("Label=1");

            var queried = await tool.QueryCachedData(pointer, "Label = 0");
            queried.Should().Contain("1 row(s) matched");
            queried.Should().Contain("Column0=30");
        }

        [Fact]
        public async Task InspectPreviewAndQueryAllStillWorkAgainstATblPointerOnTheSameToolAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);

            var client = new AskJoobyDataCacheClient(
                new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa"), null,
                advancedAnalyticsCache: new AdvancedAnalyticsCachedTableReader(dbContext, tenantId, "rosa"));

            var tool = new CachedDataTool(client);

            var table = new CachedTable(
                [
                    new CachedColumn("id", "long"),
                    new CachedColumn("status", "string")
                ],
                [
                    new Dictionary<string, object?> { ["id"] = 1L, ["status"] = "active" },
                    new Dictionary<string, object?> { ["id"] = 2L, ["status"] = "closed" }
                ]);

            var pointer = await client.StoreAsync(table);

            var schema = await tool.InspectDataSchema(pointer);
            schema.Should().Contain("2 row(s), 2 column(s)");

            var queried = await tool.QueryCachedData(pointer, "status = \"closed\"");
            queried.Should().Contain("1 row(s) matched");
            queried.Should().Contain("id=2");
        }
    }
}