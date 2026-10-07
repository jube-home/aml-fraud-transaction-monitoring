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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class DeleteTransactionTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> modelIds = [];
        private readonly List<int> xPathIds = [];
        private int tenantRegistryId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenantRegistryId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => xPathIds.Contains(w.EntityAnalysisModelRequestXpathId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(w => xPathIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedRequestXPathAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Tx{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            modelIds.Add(model.Id);

            var name = $"ZzTx{Guid.NewGuid():N}"[..20];
            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model.Id,
                Name = name,
                XPath = $"$.{name}",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            xPathIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ADeleteInsideACallersTransactionIsUndoneWhenThatTransactionRollsBackAsync()
        {
            var id = await SeedRequestXPathAsync();

            await using var dbContext = fx.GetDbContext();
            await dbContext.BeginTransactionAsync();
            await new EntityAnalysisModelRequestXpathRepository(dbContext, tenantRegistryId).DeleteAsync(id);
            await dbContext.RollbackTransactionAsync();

            var row = await dbContext.EntityAnalysisModelRequestXpath.FirstAsync(w => w.Id == id);
            row.Deleted.Should().Be(0);
            (await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .CountAsync(w => w.EntityAnalysisModelRequestXpathId == id)).Should().Be(0);
        }

        [Fact]
        public async Task AStandaloneDeleteCommitsTheRowAndBothVersionRowsTogetherAsync()
        {
            var id = await SeedRequestXPathAsync();

            await using var dbContext = fx.GetDbContext();
            await new EntityAnalysisModelRequestXpathRepository(dbContext, tenantRegistryId).DeleteAsync(id);

            var row = await dbContext.EntityAnalysisModelRequestXpath.FirstAsync(w => w.Id == id);
            row.Deleted.Should().Be(1);
            (await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .CountAsync(w => w.EntityAnalysisModelRequestXpathId == id)).Should().Be(2);
        }
    }
}