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
    public sealed class DeleteAsVersionRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> activationRuleIds = [];
        private readonly List<int> listIds = [];
        private readonly List<int> listValueIds = [];
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

            await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                .Where(w => activationRuleIds.Contains(w.EntityAnalysisModelActivationRuleId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelActivationRule
                .Where(w => activationRuleIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelListValueVersion>()
                .Where(w => listValueIds.Contains(w.EntityAnalysisModelListValueId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelListValue
                .Where(w => listValueIds.Contains(w.Id)).DeleteAsync();

            await dbContext.EntityAnalysisModelListVersion
                .Where(w => listIds.Contains(w.EntityAnalysisModelListId)).DeleteAsync();
            await dbContext.EntityAnalysisModelList
                .Where(w => listIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel
                .Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<EntityAnalysisModel> SeedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}DelVer{Guid.NewGuid():N}"[..40],
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
            return model;
        }

        private async Task<int> SeedRequestXPathAsync(int modelId, string name, byte? locked = 0)
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                XPath = $"$.{name}",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = locked,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            requestXPathIds.Add(id);
            return id;
        }

        [Fact]
        public async Task DeletingARequestXPathWritesThePreDeleteRowToTheVersionTableAsync()
        {
            var model = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(model.Id, "ZzDelVerAmount");

            await using var dbContext = fx.GetDbContext();

            await new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(id, CancellationToken.None);

            var versions = await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId == id).ToListAsync();

            using var scope = new AssertionScope();
            versions.Should().HaveCount(2,
                "a delete is a pending change: it archives the pre-delete row for approval to fall back to, and the deleted state so history holds every version");
            var preDelete = versions.Single(w => w.Version == 1);
            preDelete.Name.Should().Be("ZzDelVerAmount");
            (preDelete.Deleted is null or 0).Should().BeTrue(
                "the version row is the row as it stood before the delete, so it is not itself deleted");
            var deletedState = versions.Single(w => w.Version == 2);
            deletedState.Deleted.Should().Be(1);
            deletedState.DeletedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task DeletingARequestXPathBumpsTheCoreVersionSoTheDeleteIsItsOwnApprovableVersionAsync()
        {
            var model = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(model.Id, "ZzDelVerBumped");

            await using var dbContext = fx.GetDbContext();

            await new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(id, CancellationToken.None);

            var core = await dbContext.EntityAnalysisModelRequestXpath.FirstAsync(w => w.Id == id);

            using var scope = new AssertionScope();
            core.Version.Should().Be(2,
                "without the bump the approval granted against version one would still match the deleted row");
            core.Deleted.Should().Be(1);
            core.DeletedUser.Should().Be(fx.Seed.UserWithPermission);
            core.DeletedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task DeletingARequestXPathWhoseVersionIsUnsetTreatsItAsVersionOneAsync()
        {
            var model = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(model.Id, "ZzDelVerNullVer");

            await using var dbContext = fx.GetDbContext();

            await dbContext.EntityAnalysisModelRequestXpath.Where(w => w.Id == id)
                .Set(s => s.Version, (int?)null).UpdateAsync();

            await new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(id, CancellationToken.None);

            var core = await dbContext.EntityAnalysisModelRequestXpath.FirstAsync(w => w.Id == id);

            core.Version.Should().Be(2,
                "a row predating versioning must still produce a distinct version for the delete to be approved against");
        }

        [Fact]
        public async Task DeletingALockedRequestXPathThrowsAndWritesNoVersionRowAsync()
        {
            var model = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(model.Id, "ZzDelVerLocked", 1);

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission);

            var act = async () => await repository.DeleteAsync(id, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();

            var versions = await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId == id).CountAsync();

            versions.Should().Be(0, "a refused delete must not leave an audit row claiming it happened");
        }

        [Fact]
        public async Task DeletingAnAlreadyDeletedRequestXPathThrowsAndDoesNotVersionItTwiceAsync()
        {
            var model = await SeedModelAsync();
            var id = await SeedRequestXPathAsync(model.Id, "ZzDelVerTwice");

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelRequestXpathRepository(dbContext, fx.Seed.UserWithPermission);

            await repository.DeleteAsync(id, CancellationToken.None);

            var act = async () => await repository.DeleteAsync(id, CancellationToken.None);

            await act.Should().ThrowAsync<KeyNotFoundException>();

            var versions = await dbContext.GetTable<EntityAnalysisModelRequestXpathVersion>()
                .Where(w => w.EntityAnalysisModelRequestXpathId == id).CountAsync();

            versions.Should().Be(2, "the first delete archives two rows and the refused second delete must add none");
        }

        [Fact]
        public async Task DeletingAnActivationRuleVersionsItBecauseItIsTheKindTheEngineGatesOnAsync()
        {
            var model = await SeedModelAsync();

            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelActivationRule
            {
                EntityAnalysisModelId = model.Id,
                Name = $"{DatabaseFixture.Prefix}DelVerRule",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 3,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            activationRuleIds.Add(id);

            await new EntityAnalysisModelActivationRuleRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(id, CancellationToken.None);

            var versions = await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                .Where(w => w.EntityAnalysisModelActivationRuleId == id).ToListAsync();
            var core = await dbContext.EntityAnalysisModelActivationRule.FirstAsync(w => w.Id == id);

            using var scope = new AssertionScope();
            versions.Should().HaveCount(2);
            versions.Should().Contain(w => w.Version == 3,
                "the superseded version is the one the row was on before the delete");
            versions.Should().Contain(w => w.Version == 4 && w.Deleted == 1,
                "the deleted state is archived as the version the delete produced");
            core.Version.Should().Be(4);
        }

        [Fact]
        public async Task DeletingAListValueVersionsItThroughTheGrandparentScopedPredicateAsync()
        {
            var model = await SeedModelAsync();

            await using var dbContext = fx.GetDbContext();

            var listId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelList
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"{DatabaseFixture.Prefix}DelVerList",
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            listIds.Add(listId);

            var id = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelListValue
            {
                EntityAnalysisModelListId = listId,
                ListValue = "ZzDelVerValue",
                Guid = Guid.NewGuid(),
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            listValueIds.Add(id);

            await new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(id, CancellationToken.None);

            var versions = await dbContext.GetTable<EntityAnalysisModelListValueVersion>()
                .Where(w => w.EntityAnalysisModelListValueId == id).ToListAsync();
            var core = await dbContext.EntityAnalysisModelListValue.FirstAsync(w => w.Id == id);

            using var scope = new AssertionScope();
            versions.Should().HaveCount(2);
            versions.Should().OnlyContain(w => w.ListValue == "ZzDelVerValue");
            versions.Should().OnlyContain(w => w.EntityAnalysisModelListId == listId,
                "per-value approval resolution needs the parent list to find the value's versions");
            core.Version.Should().Be(2);
        }

        [Fact]
        public async Task DeletingAModelVersionsItThroughTheDirectlyTenantedPredicateAsync()
        {
            var model = await SeedModelAsync();

            await using var dbContext = fx.GetDbContext();

            await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission)
                .DeleteAsync(model.Id, CancellationToken.None);

            var versions = await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => w.EntityAnalysisModelId == model.Id).ToListAsync();
            var core = await dbContext.EntityAnalysisModel.FirstAsync(w => w.Id == model.Id);

            using var scope = new AssertionScope();
            versions.Should().HaveCount(2);
            versions.Should().Contain(w => w.Version == 1);
            versions.Should().OnlyContain(w => w.TenantRegistryId == tenantRegistryId);
            core.Version.Should().Be(2);
            core.Deleted.Should().Be(1);
        }
    }
}