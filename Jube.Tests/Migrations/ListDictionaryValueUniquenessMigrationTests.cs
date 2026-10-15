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
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Migrations.Branches.ListDictionaryValueUniqueness.Helpers;
using Jube.Service.Concurrency;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Migrations
{
    using ListValuePoco = Data.Poco.EntityAnalysisModelListValue;
    using KvpPoco = Data.Poco.EntityAnalysisModelDictionaryKvp;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ListDictionaryValueUniquenessMigrationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdListIds = [];
        private readonly List<int> createdDictionaryIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var listId in createdListIds)
            {
                var valueIds = dbContext.GetTable<ListValuePoco>()
                    .Where(v => v.EntityAnalysisModelListId == listId)
                    .Select(v => (int?)v.Id);

                await dbContext.EntityApproval
                    .Where(w => w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelListValue &&
                                valueIds.Contains(w.EntityId))
                    .DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValueVersion>()
                    .Where(w => w.EntityAnalysisModelListId == listId).DeleteAsync();
                await dbContext.GetTable<ListValuePoco>().Where(w => w.EntityAnalysisModelListId == listId)
                    .DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelListVersion>()
                    .Where(w => w.EntityAnalysisModelListId == listId).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelList>().Where(w => w.Id == listId)
                    .DeleteAsync();
            }

            foreach (var dictionaryId in createdDictionaryIds)
            {
                var kvpIds = dbContext.GetTable<KvpPoco>()
                    .Where(v => v.EntityAnalysisModelDictionaryId == dictionaryId)
                    .Select(v => (int?)v.Id);

                await dbContext.EntityApproval
                    .Where(w => w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp &&
                                kvpIds.Contains(w.EntityId))
                    .DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvpVersion>()
                    .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).DeleteAsync();
                await dbContext.GetTable<KvpPoco>().Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId)
                    .DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryVersion>()
                    .Where(w => w.EntityAnalysisModelDictionaryId == dictionaryId).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionary>().Where(w => w.Id == dictionaryId)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                    .Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private async Task<int> CreateParentListAsync(Data.Context.DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var list = await new EntityAnalysisModelListRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModelList
                {
                    EntityAnalysisModelGuid = model.Guid,
                    Name = $"{DatabaseFixture.Prefix}List{Guid.NewGuid():N}"[..40],
                    Active = 1,
                    Locked = 0
                }).ConfigureAwait(false);
            createdListIds.Add(list.Id);

            return list.Id;
        }

        private async Task<int> CreateParentDictionaryAsync(Data.Context.DbContext dbContext, string createdUser)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, createdUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);
            createdModelIds.Add(model.Id);

            var dictionary = await new EntityAnalysisModelDictionaryRepository(dbContext, createdUser)
                .InsertAsync(new Data.Poco.EntityAnalysisModelDictionary
                {
                    EntityAnalysisModelGuid = model.Guid,
                    Name = $"{DatabaseFixture.Prefix}Dictionary{Guid.NewGuid():N}"[..40],
                    DataName = "AccountId",
                    Active = 1,
                    Locked = 0
                }).ConfigureAwait(false);
            createdDictionaryIds.Add(dictionary.Id);

            return dictionary.Id;
        }

        [Fact]
        public async Task ListValueDuplicatesAreDedupedKeepingNewestWithAuditAndApprovalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupValue = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];
                var keepValue = $"{DatabaseFixture.Prefix}Keep{Guid.NewGuid():N}"[..40];

                var older = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var newer = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var distinct = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = keepValue });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var olderRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == older.Id);
                var newerRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == newer.Id);
                var distinctRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == distinct.Id);

                olderRow.Deleted.Should().Be(1, "the older of the two duplicates is the one soft-deleted");
                olderRow.DeletedUser.Should().Be("Administrator");
                olderRow.Version.Should().Be(2);
                (newerRow.Deleted ?? 0).Should().Be(0, "the newest duplicate survives");
                (distinctRow.Deleted ?? 0).Should().Be(0, "a non-duplicate value is left untouched");

                var auditRows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValueVersion>()
                    .CountAsync(w => w.EntityAnalysisModelListValueId == older.Id);
                auditRows.Should().Be(2, "a pre-delete and a post-delete snapshot, mirroring a real delete");

                var approval = await dbContext.EntityApproval.FirstOrDefaultAsync(w =>
                    w.EntityId == older.Id &&
                    w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelListValue);
                approval.Should().NotBeNull();
                approval!.EntityVersion.Should().Be(2);
                approval.StateId.Should().Be((int)EntityApprovalState.Approved);
                approval.CreatedUser.Should().Be("Administrator");

                (await dbContext.ExecuteAsync<int>(
                    $"SELECT count(*) FROM pg_indexes WHERE indexname = '{indexName}'")).Should().Be(1);

                var duplicateInsert = await Record.ExceptionAsync(() => dbContext.ExecuteAsync(
                    "INSERT INTO \"EntityAnalysisModelListValue\" (\"EntityAnalysisModelListId\", \"ListValue\") " +
                    $"VALUES ({listId}, '{newerRow.ListValue}')"));
                UniqueViolation.Is(duplicateInsert.Required()).Should().BeTrue(
                    "the recreated index rejects a second live row with the same value");
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueThreeOrMoreDuplicatesKeepsOnlyTheNewestAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupValue = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var first = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var second = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var third = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var fourth = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var rows = await dbContext.GetTable<ListValuePoco>()
                    .Where(w => w.Id == first.Id || w.Id == second.Id || w.Id == third.Id || w.Id == fourth.Id)
                    .ToListAsync();

                (rows.Single(r => r.Id == fourth.Id).Deleted ?? 0).Should().Be(0,
                    "the newest of four duplicates is the only survivor");
                rows.Where(r => r.Id != fourth.Id).Should().OnlyContain(r => r.Deleted == 1,
                    "every other duplicate, not just the second-oldest, is soft-deleted");
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueMultipleDuplicateGroupsInTheSameListAreDedupedIndependentlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var valueA = $"{DatabaseFixture.Prefix}GroupA{Guid.NewGuid():N}"[..40];
                var valueB = $"{DatabaseFixture.Prefix}GroupB{Guid.NewGuid():N}"[..40];

                var olderA = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = valueA });
                var newerA = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = valueA });
                var olderB = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = valueB });
                var newerB = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = valueB });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var rows = await dbContext.GetTable<ListValuePoco>()
                    .Where(w => w.Id == olderA.Id || w.Id == newerA.Id || w.Id == olderB.Id || w.Id == newerB.Id)
                    .ToListAsync();

                rows.Single(r => r.Id == olderA.Id).Deleted.Should().Be(1);
                (rows.Single(r => r.Id == newerA.Id).Deleted ?? 0).Should().Be(0);
                rows.Single(r => r.Id == olderB.Id).Deleted.Should().Be(1,
                    "a second, unrelated duplicate group in the same list is deduped independently");
                (rows.Single(r => r.Id == newerB.Id).Deleted ?? 0).Should().Be(0);
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueDuplicatesAcrossDifferentListsAreDedupedIndependentlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listAId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var listBId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var sameText = $"{DatabaseFixture.Prefix}SharedText{Guid.NewGuid():N}"[..40];

                var olderInA = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listAId, ListValue = sameText });
                var newerInA = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listAId, ListValue = sameText });
                var olderInB = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listBId, ListValue = sameText });
                var newerInB = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listBId, ListValue = sameText });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var rows = await dbContext.GetTable<ListValuePoco>()
                    .Where(w => w.Id == olderInA.Id || w.Id == newerInA.Id || w.Id == olderInB.Id ||
                                w.Id == newerInB.Id)
                    .ToListAsync();

                rows.Single(r => r.Id == olderInA.Id).Deleted.Should().Be(1);
                (rows.Single(r => r.Id == newerInA.Id).Deleted ?? 0).Should().Be(0);
                rows.Single(r => r.Id == olderInB.Id).Deleted.Should().Be(1,
                    "the same text in a different list is a wholly separate duplicate group, scoped by parent");
                (rows.Single(r => r.Id == newerInB.Id).Deleted ?? 0).Should().Be(0,
                    "list B keeps its own newest row -- it must not have been collapsed with list A's group");
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueAlreadyDeletedRowSharingTheValueIsLeftUntouchedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupValue = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var alreadyDeleted = await repository.InsertAsync(new ListValuePoco
                {
                    EntityAnalysisModelListId = listId, ListValue = dupValue, Deleted = 1,
                    DeletedDate = DateTime.UtcNow, DeletedUser = fx.Seed.UserWithPermission
                });
                var older = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var newer = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var untouched = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == alreadyDeleted.Id);
                untouched.Version.Should().Be(1, "an already-deleted row is invisible to the dedupe pass");
                untouched.DeletedUser.Should().Be(fx.Seed.UserWithPermission,
                    "its original deletion attribution must not be overwritten with 'Administrator'");

                var untouchedAuditRows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValueVersion>()
                    .CountAsync(w => w.EntityAnalysisModelListValueId == alreadyDeleted.Id);
                untouchedAuditRows.Should().Be(0);

                (await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == older.Id)).Deleted.Should().Be(1,
                    "the live pair is still deduped as normal, independently of the pre-existing deleted row");
                ((await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == newer.Id)).Deleted ?? 0)
                    .Should().Be(0);
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueRunningTheMigrationAgainWithNothingNewToDedupeIsANoOpAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupValue = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var older = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });
                var newer = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue });

                var sql = DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []);

                await dbContext.ExecuteAsync(sql);
                await dbContext.ExecuteAsync(sql);
                await dbContext.ExecuteAsync(sql);

                var olderRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == older.Id);
                olderRow.Version.Should().Be(2, "repeated runs must not keep bumping the version of an already " +
                                                "resolved duplicate");

                var newerRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == newer.Id);
                newerRow.Version.Should().Be(1, "the surviving row is never touched by any run");

                var auditRows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelListValueVersion>()
                    .CountAsync(w => w.EntityAnalysisModelListValueId == older.Id);
                auditRows.Should().Be(2, "no further audit rows should accumulate on repeated runs");

                var approvalCount = await dbContext.EntityApproval.CountAsync(w =>
                    w.EntityId == older.Id &&
                    w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelListValue);
                approvalCount.Should().Be(1, "no duplicate approval rows should accumulate on repeated runs");

                (await dbContext.ExecuteAsync<int>(
                    $"SELECT count(*) FROM pg_indexes WHERE indexname = '{indexName}'")).Should().Be(1);
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task ListValueKeepsTheNewestNonExpiredDuplicateOverAnExpiredNewerOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var listId = await CreateParentListAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelListValueRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelListValue_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupValue = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var olderStillValid = await repository.InsertAsync(new ListValuePoco
                    { EntityAnalysisModelListId = listId, ListValue = dupValue, DeleteExpiryDate = null });
                var newerButExpired = await repository.InsertAsync(new ListValuePoco
                {
                    EntityAnalysisModelListId = listId, ListValue = dupValue,
                    DeleteExpiryDate = DateTime.UtcNow.AddDays(-1)
                });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));

                var validRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == olderStillValid.Id);
                var expiredRow = await dbContext.GetTable<ListValuePoco>().FirstAsync(w => w.Id == newerButExpired.Id);

                (validRow.Deleted ?? 0).Should().Be(0,
                    "the older row is still a genuinely live, un-expired value and must survive");
                expiredRow.Deleted.Should().Be(1,
                    "a newer row that has already expired is not a better survivor than an older, still-valid one");
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelListValue", "EntityAnalysisModelListValueVersion",
                    "EntityAnalysisModelListValueId", "EntityAnalysisModelListId", "ListValue",
                    "EntityAnalysisModelList", (int)EntityApprovalKind.EntityAnalysisModelListValue, []));
            }
        }

        [Fact]
        public async Task DictionaryKvpDuplicatesAreDedupedKeepingNewestWithAuditAndApprovalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelDictionaryKvpRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelDictionaryKvp_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupKey = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var older = await repository.InsertAsync(new KvpPoco
                    { EntityAnalysisModelDictionaryId = dictionaryId, KvpKey = dupKey, KvpValue = 1 });
                var newer = await repository.InsertAsync(new KvpPoco
                    { EntityAnalysisModelDictionaryId = dictionaryId, KvpKey = dupKey, KvpValue = 2 });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelDictionaryKvp", "EntityAnalysisModelDictionaryKvpVersion",
                    "EntityAnalysisModelDictionaryKvpId", "EntityAnalysisModelDictionaryId", "KvpKey",
                    "EntityAnalysisModelDictionary", (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                    ["KvpValue"]));

                var olderRow = await dbContext.GetTable<KvpPoco>().FirstAsync(w => w.Id == older.Id);
                var newerRow = await dbContext.GetTable<KvpPoco>().FirstAsync(w => w.Id == newer.Id);

                olderRow.Deleted.Should().Be(1);
                olderRow.DeletedUser.Should().Be("Administrator");
                (newerRow.Deleted ?? 0).Should().Be(0);
                newerRow.KvpValue.Should().Be(2, "the surviving row keeps its own value, not the loser's");

                var auditRows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelDictionaryKvpVersion>()
                    .CountAsync(w => w.EntityAnalysisModelDictionaryKvpId == older.Id);
                auditRows.Should().Be(2);

                var approval = await dbContext.EntityApproval.FirstOrDefaultAsync(w =>
                    w.EntityId == older.Id &&
                    w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp);
                approval.Should().NotBeNull();
                approval!.StateId.Should().Be((int)EntityApprovalState.Approved);
                approval.CreatedUser.Should().Be("Administrator");

                (await dbContext.ExecuteAsync<int>(
                    $"SELECT count(*) FROM pg_indexes WHERE indexname = '{indexName}'")).Should().Be(1);
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelDictionaryKvp", "EntityAnalysisModelDictionaryKvpVersion",
                    "EntityAnalysisModelDictionaryKvpId", "EntityAnalysisModelDictionaryId", "KvpKey",
                    "EntityAnalysisModelDictionary", (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                    ["KvpValue"]));
            }
        }

        [Fact]
        public async Task DictionaryKvpKeepsTheNewestNonExpiredDuplicateOverAnExpiredNewerOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var dictionaryId = await CreateParentDictionaryAsync(dbContext, fx.Seed.UserWithPermission);
            var repository = new EntityAnalysisModelDictionaryKvpRepository(dbContext, fx.Seed.UserWithPermission);
            const string indexName = "UX_EntityAnalysisModelDictionaryKvp_Live";

            await dbContext.ExecuteAsync($"DROP INDEX IF EXISTS \"{indexName}\"");
            try
            {
                var dupKey = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];

                var olderStillValid = await repository.InsertAsync(new KvpPoco
                {
                    EntityAnalysisModelDictionaryId = dictionaryId, KvpKey = dupKey, KvpValue = 1,
                    DeleteExpiryDate = null
                });
                var newerButExpired = await repository.InsertAsync(new KvpPoco
                {
                    EntityAnalysisModelDictionaryId = dictionaryId, KvpKey = dupKey, KvpValue = 2,
                    DeleteExpiryDate = DateTime.UtcNow.AddDays(-1)
                });

                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelDictionaryKvp", "EntityAnalysisModelDictionaryKvpVersion",
                    "EntityAnalysisModelDictionaryKvpId", "EntityAnalysisModelDictionaryId", "KvpKey",
                    "EntityAnalysisModelDictionary", (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                    ["KvpValue"]));

                var validRow = await dbContext.GetTable<KvpPoco>().FirstAsync(w => w.Id == olderStillValid.Id);
                var expiredRow = await dbContext.GetTable<KvpPoco>().FirstAsync(w => w.Id == newerButExpired.Id);

                (validRow.Deleted ?? 0).Should().Be(0,
                    "the older row is still a genuinely live, un-expired value and must survive");
                expiredRow.Deleted.Should().Be(1,
                    "a newer row that has already expired is not a better survivor than an older, still-valid one");
            }
            finally
            {
                await dbContext.ExecuteAsync(DuplicateValueUniqueIndexSql.Up(
                    "EntityAnalysisModelDictionaryKvp", "EntityAnalysisModelDictionaryKvpVersion",
                    "EntityAnalysisModelDictionaryKvpId", "EntityAnalysisModelDictionaryId", "KvpKey",
                    "EntityAnalysisModelDictionary", (int)EntityApprovalKind.EntityAnalysisModelDictionaryKvp,
                    ["KvpValue"]));
            }
        }
    }
}