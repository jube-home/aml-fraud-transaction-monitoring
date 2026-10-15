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
    public sealed class AdvancedAnalyticsCachedTableReaderTests(DatabaseFixture fx) : IAsyncLifetime
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
            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedTenantAsync(DbContext dbContext)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}AdvancedAnalyticsCachedTableReaderTenant{Guid.NewGuid():N}",
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
        public async Task GetAsyncConvertsPackedDoublesIntoAGenericallyNamedCachedTableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var cacheRepository = new AdvancedAnalyticsCacheRepository(dbContext, tenantId, "rosa");
            double[] values = [1.5, 2.5, 1.0, 3.5, 4.5, 0.0];
            var entry = await cacheRepository.PutAsync(AdvancedAnalyticsPacking.Pack(values), rows: 2, columns: 3);
            var reader = new AdvancedAnalyticsCachedTableReader(dbContext, tenantId, "rosa");

            var table = await reader.GetAsync(entry.Guid);

            table.Should().NotBeNull();
            table!.Columns.Select(c => c.Name).Should().Equal("Column0", "Column1", "Label");
            table.Rows.Should().HaveCount(2);
            table.Rows[0]["Column0"].Should().Be(1.5);
            table.Rows[0]["Column1"].Should().Be(2.5);
            table.Rows[0]["Label"].Should().Be(1.0);
            table.Rows[1]["Column0"].Should().Be(3.5);
            table.Rows[1]["Column1"].Should().Be(4.5);
            table.Rows[1]["Label"].Should().Be(0.0);
        }

        [Fact]
        public async Task GetAsyncReturnsNullForAnUnknownGuidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var reader = new AdvancedAnalyticsCachedTableReader(dbContext, tenantId, "rosa");

            var table = await reader.GetAsync(Guid.NewGuid());

            table.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncReturnsNullForAForeignTenantsPointerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ownerTenantId = await SeedTenantAsync(dbContext);
            var otherTenantId = await SeedTenantAsync(dbContext);
            var cacheRepository = new AdvancedAnalyticsCacheRepository(dbContext, ownerTenantId, "rosa");
            var entry = await cacheRepository.PutAsync(AdvancedAnalyticsPacking.Pack([1, 2]), rows: 1, columns: 2);
            var reader = new AdvancedAnalyticsCachedTableReader(dbContext, otherTenantId, "rosa");

            var table = await reader.GetAsync(entry.Guid);

            table.Should().BeNull();
        }
    }
}