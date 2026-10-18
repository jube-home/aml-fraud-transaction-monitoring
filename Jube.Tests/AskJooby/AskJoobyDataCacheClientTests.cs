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
    public sealed class AskJoobyDataCacheClientTests(DatabaseFixture fx) : IAsyncLifetime
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

            await dbContext.AskJoobyDataCache.Where(w => createdTenantIds.Contains(w.TenantRegistryId!.Value))
                .DeleteAsync();

            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedTenantAsync(DbContext dbContext)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AskJoobyDataCacheClientTenant{Guid.NewGuid():N}",
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
        public async Task StoreThenGetRoundTripsColumnsAndNormalisesRowValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");
            var client = new AskJoobyDataCacheClient(repository, null);

            var table = new CachedTable(
                [
                    new CachedColumn("id", "long"), new CachedColumn("name", "string"),
                    new CachedColumn("active", "bool")
                ],
                [
                    new Dictionary<string, object?> { ["id"] = 1L, ["name"] = "Alice", ["active"] = true },
                    new Dictionary<string, object?> { ["id"] = 2L, ["name"] = null, ["active"] = false }
                ]);

            var pointer = await client.StoreAsync(table, "test table");
            var roundTripped = await client.GetAsync(pointer);

            roundTripped.Should().NotBeNull();
            roundTripped!.Columns.Should().HaveCount(3);
            roundTripped.Rows.Should().HaveCount(2);
            roundTripped.Rows[0]["id"].Should().Be(1L);
            roundTripped.Rows[0]["name"].Should().Be("Alice");
            roundTripped.Rows[0]["active"].Should().Be(true);
            roundTripped.Rows[1]["name"].Should().BeNull();
        }

        [Fact]
        public async Task GetReturnsNullForAnUnknownPointerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");
            var client = new AskJoobyDataCacheClient(repository, null);

            var result = await client.GetAsync("tbl_totally_made_up");

            result.Should().BeNull();
        }

        [Fact]
        public async Task StoreThrowsWhenRowCountExceedsTheConfiguredCapAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");
            var client = new AskJoobyDataCacheClient(repository, null, 2);

            var table = new CachedTable(
                [new CachedColumn("id", "long")],
                [
                    new Dictionary<string, object?> { ["id"] = 1L },
                    new Dictionary<string, object?> { ["id"] = 2L },
                    new Dictionary<string, object?> { ["id"] = 3L }
                ]);

            var act = () => client.StoreAsync(table);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetDispatchesAGuidShapedPointerToTheAdvancedAnalyticsReaderAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");
            var advancedAnalyticsCache = new AdvancedAnalyticsCacheRepository(dbContext, tenantId, "rosa");

            var entry = await advancedAnalyticsCache.PutAsync(AdvancedAnalyticsPacking.Pack([1, 2, 1]), rows: 1,
                columns: 3);

            var client = new AskJoobyDataCacheClient(repository, null,
                advancedAnalyticsCache: new AdvancedAnalyticsCachedTableReader(dbContext, tenantId, "rosa"));

            var table = await client.GetAsync(entry.Guid.ToString());

            table.Should().NotBeNull();
            table!.Columns.Select(c => c.Name).Should().Equal("Column0", "Column1", "Label");
            table.Rows.Should().ContainSingle();
            table.Rows[0]["Label"].Should().Be(1.0);

            await dbContext.AdvancedAnalyticsCache.Where(w => w.Guid == entry.Guid).DeleteAsync();
        }

        [Fact]
        public async Task GetWithoutAnAdvancedAnalyticsReaderLeavesAGuidShapedPointerUnresolvedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");
            var advancedAnalyticsCache = new AdvancedAnalyticsCacheRepository(dbContext, tenantId, "rosa");

            var entry = await advancedAnalyticsCache.PutAsync(AdvancedAnalyticsPacking.Pack([1, 2]), rows: 1,
                columns: 2);

            var client = new AskJoobyDataCacheClient(repository, null);

            var table = await client.GetAsync(entry.Guid.ToString());

            table.Should().BeNull("with no advanced analytics reader bound, a Guid-shaped pointer simply "
                                  + "misses on this store's own tbl_-keyed rows, same as any other unknown pointer");

            await dbContext.AdvancedAnalyticsCache.Where(w => w.Guid == entry.Guid).DeleteAsync();
        }

        [Fact]
        public async Task StoreAppliesTheDefaultTtlWhenNoneIsSuppliedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "rosa");

            var client = new AskJoobyDataCacheClient(repository, null,
                defaultTtl: TimeSpan.FromMilliseconds(1));

            var table = new CachedTable([new CachedColumn("id", "long")],
                [new Dictionary<string, object?> { ["id"] = 1L }]);

            var pointer = await client.StoreAsync(table);
            await Task.Delay(50);
            var result = await client.GetAsync(pointer);

            result.Should().BeNull("a table stored with a 1ms default TTL should already have expired");
        }
    }
}