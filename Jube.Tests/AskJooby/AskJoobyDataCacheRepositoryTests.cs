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
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class AskJoobyDataCacheRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
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
                Name = $"{DatabaseFixture.Prefix}AskJoobyDataCacheTenant{Guid.NewGuid():N}",
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
        public async Task InsertAndGetRoundTripTheStoredJsonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "priya");

            const string pointerKey = "tbl_roundtrip_test";
            const string columnsJson = """[{"Name":"id","Type":"long"}]""";
            const string dataJson = """[{"id":1},{"id":2}]""";

            await repository.InsertAsync(pointerKey, columnsJson, dataJson, 2, "a label", null,
                DateTime.UtcNow.AddHours(1));

            var row = await repository.GetByPointerKeyAsync(pointerKey);

            row.Should().NotBeNull();
            row!.ColumnsJson.Should().Contain("\"id\"");
            row.DataJson.Should().Contain("1").And.Contain("2");
        }

        [Fact]
        public async Task GetReturnsNullForAnExpiredEntryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "priya");

            const string pointerKey = "tbl_expired_test";
            await repository.InsertAsync(pointerKey, "[]", "[]", 0, null, null, DateTime.UtcNow.AddMinutes(-5));

            var row = await repository.GetByPointerKeyAsync(pointerKey);

            row.Should().BeNull("an expired cache entry must never be readable again");
        }

        [Fact]
        public async Task GetReturnsNullForAPointerBelongingToADifferentTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantA = await SeedTenantAsync(dbContext);
            var tenantB = await SeedTenantAsync(dbContext);
            var repositoryA = new AskJoobyDataCacheRepository(dbContext, tenantA, "priya");
            var repositoryB = new AskJoobyDataCacheRepository(dbContext, tenantB, "quinn");

            const string pointerKey = "tbl_cross_tenant_test";
            await repositoryA.InsertAsync(pointerKey, "[]", "[]", 0, null, null, DateTime.UtcNow.AddHours(1));

            var row = await repositoryB.GetByPointerKeyAsync(pointerKey);

            row.Should().BeNull("a cache entry must never be readable from a different tenant");
        }

        [Fact]
        public async Task GetReturnsNullForAnUnknownPointerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantId = await SeedTenantAsync(dbContext);
            var repository = new AskJoobyDataCacheRepository(dbContext, tenantId, "priya");

            var row = await repository.GetByPointerKeyAsync("tbl_does_not_exist");

            row.Should().BeNull();
        }
    }
}