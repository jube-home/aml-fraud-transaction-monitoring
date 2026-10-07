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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ReviveDeletedRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> modelIds = [];
        private readonly List<int> requestXPathIds = [];
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
                .Where(w => requestXPathIds.Contains(w.EntityAnalysisModelRequestXpathId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => requestXPathIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel
                .Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Rev{Guid.NewGuid():N}"[..40],
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
            return model.Id;
        }

        private async Task<int> SeedRequestXPathAsync(int modelId, string name)
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
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

            requestXPathIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ADeletedRowIsReachableByIdSoItCanBeOpenedAndEditedAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, "ZzRevReach");

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission);

            await repository.DeleteAsync(id, CancellationToken.None);

            var deleted = await repository.GetByIdAsync(id, CancellationToken.None);

            using var scope = new AssertionScope();
            deleted.Should().NotBeNull("a deleted row must stay reachable so the operator can open it and fix it");
            deleted!.Deleted.Should().Be(1);
        }

        [Fact]
        public async Task RevivingADeletedRowMakesItLiveAgainAndArchivesTheDeletedStateOnceAsync()
        {
            var modelId = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(modelId, "ZzRevLive");

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission);

            await repository.DeleteAsync(id, CancellationToken.None);

            var deleted = await repository.GetByIdAsync(id, CancellationToken.None);
            deleted!.DefaultValue = "Revived";
            await repository.UpdateAsync(deleted, CancellationToken.None);

            var core = await dbContext.EntityAnalysisModelRequestXpath.FirstAsync(w => w.Id == id);
            var versions = await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId == id).ToListAsync();

            using var scope = new AssertionScope();
            (core.Deleted is null or 0).Should().BeTrue("an update implies the row is no longer deleted");
            core.DeletedDate.Should().BeNull();
            core.DeletedUser.Should().BeNull();
            core.DefaultValue.Should().Be("Revived");
            core.Version.Should().Be(3, "the revival is a new version after the deleted one");

            versions.Should().HaveCount(2,
                "history holds the pre-delete row and the deleted state, and the revival must not archive the deleted state a second time");
            versions.Count(w => w.Version == 2 && w.Deleted == 1).Should().Be(1);
            versions.Count(w => w.Version == 1).Should().Be(1);
        }
    }
}