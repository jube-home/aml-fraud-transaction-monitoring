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
    public sealed class DictionaryKvpReviveTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> modelIds = [];
        private readonly List<int> dictionaryIds = [];
        private readonly List<int> kvpIds = [];
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

            await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                .Where(w => kvpIds.Contains(w.EntityAnalysisModelDictionaryKvpId!.Value)).DeleteAsync();
            await dbContext.EntityAnalysisModelDictionaryKvp.Where(w => kvpIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelDictionaryVersion>()
                .Where(w => dictionaryIds.Contains(w.EntityAnalysisModelDictionaryId)).DeleteAsync();
            await dbContext.EntityAnalysisModelDictionary.Where(w => dictionaryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedKvpAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Kvp{Guid.NewGuid():N}"[..40],
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

            var dictionary = new EntityAnalysisModelDictionary
            {
                EntityAnalysisModelGuid = model.Guid,
                Name = $"ZzKvp{Guid.NewGuid():N}"[..20],
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            dictionary.Id = await dbContext.InsertWithInt32IdentityAsync(dictionary);
            dictionaryIds.Add(dictionary.Id);

            var kvp = new EntityAnalysisModelDictionaryKvp
            {
                EntityAnalysisModelDictionaryId = dictionary.Id,
                KvpKey = "Test1",
                KvpValue = 1000,
                Guid = Guid.NewGuid(),
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
            kvp.Id = await dbContext.InsertWithInt32IdentityAsync(kvp);
            kvpIds.Add(kvp.Id);
            return kvp.Id;
        }

        [Fact]
        public async Task ADeletedKeyValuePairIsReachableByIdAndRevivedByAnUpdateAsync()
        {
            var id = await SeedKvpAsync();

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId);

            await repository.DeleteAsync(id);

            var deleted = await repository.GetByIdAsync(id);
            deleted.Should().NotBeNull("a deleted key/value must be openable so it can be reviewed and revived");
            deleted.Deleted.Should().Be(1);

            deleted.KvpValue = 2000;
            await repository.UpdateAsync(deleted);

            var revived = await repository.GetByIdAsync(id);
            revived.Deleted.Should().Be(0);
            revived.DeletedUser.Should().BeNull();
            revived.KvpValue.Should().Be(2000);
        }

        [Fact]
        public async Task ReviveWritesOnlyTheTwoDeleteArchivesAndNoExtraVersionAsync()
        {
            var id = await SeedKvpAsync();

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId);

            await repository.DeleteAsync(id);
            var row = await repository.GetByIdAsync(id);
            row.KvpValue = 2000;
            await repository.UpdateAsync(row);

            (await dbContext.GetTable<EntityAnalysisModelDictionaryKvpVersion>()
                .CountAsync(w => w.EntityAnalysisModelDictionaryKvpId == id)).Should().Be(2,
                "the pre-delete image and the deleted state are archived; the revival does not archive the deleted state again");
        }
    }
}