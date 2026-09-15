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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionsEntryRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private int sourceAId;
        private int sourceBId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            sourceAId = await InsertSourceAsync(dbContext, "A");
            sourceBId = await InsertSourceAsync(dbContext, "B");
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in new[] { sourceAId, sourceBId })
            {
                await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == id).DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<int> InsertSourceAsync(DbContext dbContext, string suffix)
        {
            return dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}SanctionSource{suffix}{Guid.NewGuid():N}"[..40],
                Delimiter = ',',
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                Skip = 0
            });
        }

        private static SanctionEntry NewEntry(int sourceId, string hash, string elementValue = "Robert Mugabe",
            string reference = "1")
        {
            return new SanctionEntry
            {
                SanctionEntrySourceId = sourceId,
                SanctionEntryElementValue = elementValue,
                SanctionEntryReference = reference,
                SanctionPayload = elementValue,
                SanctionEntryHash = hash,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };
        }

        [Fact]
        public async Task UpsertAsyncOnANewHashInsertsAndReturnsInsertedOutcomeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var hash = Guid.NewGuid().ToString("N");

            var (entry, outcome) = await repository.UpsertAsync(NewEntry(sourceAId, hash));

            outcome.Should().Be(SanctionEntryUpsertOutcome.Inserted);
            entry.Id.Should().BeGreaterThan(0);

            var persisted = await dbContext.SanctionEntry.FirstAsync(w => w.Id == entry.Id);
            persisted.SanctionEntryElementValue.Should().Be("Robert Mugabe");
            persisted.SanctionEntryReference.Should().Be("1");
            persisted.SanctionEntryHash.Should().Be(hash);
            persisted.Deleted.Should().BeNull();
        }

        [Fact]
        public async Task UpsertAsyncWithTheSameHashAndSourceASecondTimeIsUnchangedAndDoesNotDuplicateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var hash = Guid.NewGuid().ToString("N");

            var (first, _) = await repository.UpsertAsync(NewEntry(sourceAId, hash));
            var (second, outcome) = await repository.UpsertAsync(NewEntry(sourceAId, hash));

            outcome.Should().Be(SanctionEntryUpsertOutcome.Unchanged);
            second.Id.Should().Be(first.Id);

            var count = await dbContext.SanctionEntry.CountAsync(w => w.SanctionEntryHash == hash
                                                                      && w.SanctionEntrySourceId == sourceAId);
            count.Should().Be(1);
        }

        [Fact]
        public async Task UpsertAsyncWithTheSameHashButADifferentSourceInsertsASeparateRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var hash = Guid.NewGuid().ToString("N");

            var (entryA, outcomeA) = await repository.UpsertAsync(NewEntry(sourceAId, hash));
            var (entryB, outcomeB) = await repository.UpsertAsync(NewEntry(sourceBId, hash));

            outcomeA.Should().Be(SanctionEntryUpsertOutcome.Inserted);
            outcomeB.Should().Be(SanctionEntryUpsertOutcome.Inserted);
            entryA.Id.Should().NotBe(entryB.Id);
        }

        [Fact]
        public async Task DeleteAsyncSoftDeletesAndSetsAuditColumnsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var hash = Guid.NewGuid().ToString("N");
            var (entry, _) = await repository.UpsertAsync(NewEntry(sourceAId, hash));

            await repository.DeleteAsync(entry.Id, "ZzTestDeleter");

            var persisted = await dbContext.SanctionEntry.FirstAsync(w => w.Id == entry.Id);
            persisted.Deleted.Should().Be(1);
            persisted.DeletedUser.Should().Be("ZzTestDeleter");
            persisted.DeletedDate.Should().NotBeNull();
            persisted.DeletedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task UpsertAsyncOnAPreviouslyDeletedHashRevivesItAndClearsDeletionColumnsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var hash = Guid.NewGuid().ToString("N");
            var (entry, _) = await repository.UpsertAsync(NewEntry(sourceAId, hash));
            await repository.DeleteAsync(entry.Id, "ZzTestDeleter");

            var (revived, outcome) = await repository.UpsertAsync(NewEntry(sourceAId, hash));

            outcome.Should().Be(SanctionEntryUpsertOutcome.Revived);
            revived.Id.Should().Be(entry.Id);

            var persisted = await dbContext.SanctionEntry.FirstAsync(w => w.Id == entry.Id);
            persisted.Deleted.Should().BeNull();
            persisted.DeletedDate.Should().BeNull();
            persisted.DeletedUser.Should().BeNull();
        }

        [Fact]
        public async Task GetAsyncExcludesSoftDeletedRowsButIncludesActiveOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var (active, _) = await repository.UpsertAsync(NewEntry(sourceAId, Guid.NewGuid().ToString("N"),
                reference: "active"));
            var (deleted, _) = await repository.UpsertAsync(NewEntry(sourceAId, Guid.NewGuid().ToString("N"),
                reference: "deleted"));
            await repository.DeleteAsync(deleted.Id, "ZzTestDeleter");

            var all = (await repository.GetAsync()).ToList();

            all.Should().Contain(e => e.Id == active.Id);
            all.Should().NotContain(e => e.Id == deleted.Id);
        }

        [Fact]
        public async Task GetActiveBySanctionEntrySourceIdAsyncOnlyReturnsRowsForThatSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var (entryA, _) = await repository.UpsertAsync(NewEntry(sourceAId, Guid.NewGuid().ToString("N")));
            var (entryB, _) = await repository.UpsertAsync(NewEntry(sourceBId, Guid.NewGuid().ToString("N")));

            var forSourceA = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceAId)).ToList();

            forSourceA.Should().Contain(e => e.Id == entryA.Id);
            forSourceA.Should().NotContain(e => e.Id == entryB.Id);
        }

        [Fact]
        public async Task GetActiveBySanctionEntrySourceIdAsyncExcludesSoftDeletedRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            var (entry, _) = await repository.UpsertAsync(NewEntry(sourceAId, Guid.NewGuid().ToString("N")));
            await repository.DeleteAsync(entry.Id, "ZzTestDeleter");

            var active = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceAId)).ToList();

            active.Should().NotContain(e => e.Id == entry.Id);
        }

        [Fact]
        public async Task GetActiveBySanctionEntrySourceIdAsyncForASourceWithNoRowsReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(sourceAId);

            active.Should().BeEmpty();
        }
    }
}