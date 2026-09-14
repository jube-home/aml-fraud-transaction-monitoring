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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Engine.Sanctions;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.VisualBasic.FileIO;
using Xunit;
using SanctionEntry = Jube.Data.Poco.SanctionEntry;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionEntryFileImporterReconciliationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string ThreeRowCsv =
            "1,Alpha Person,USA\n2,Bravo Person,USA\n3,Charlie Person,USA\n";

        private const string TwoRowCsvWithBravoRemoved =
            "1,Alpha Person,USA\n3,Charlie Person,USA\n";

        private int sourceId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            sourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}SanctionSourceRecon{Guid.NewGuid():N}"[..40],
                Delimiter = ',',
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                Skip = 0
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == sourceId).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => w.Id == sourceId).DeleteAsync();
        }

        private static TextFieldParser NewParser(string csv)
        {
            return new TextFieldParser(new StringReader(csv))
            {
                TextFieldType = FieldType.Delimited,
                Delimiters = [","]
            };
        }

        private async Task<HashSet<string>> LoadAsync(DbContext dbContext, string csv)
        {
            var repository = new SanctionsEntryRepository(dbContext);
            using var parser = NewParser(csv);

            var result = await SanctionEntryFileImporter.ImportAsync(parser, sourceId, "1", 0, 0,
                async (record, token) =>
                {
                    await repository.UpsertAsync(new SanctionEntry
                    {
                        SanctionEntryElementValue = record.ElementValue,
                        SanctionEntrySourceId = sourceId,
                        SanctionPayload = record.Payload,
                        SanctionEntryReference = record.Reference,
                        SanctionEntryHash = record.Hash,
                        CreatedDate = DateTime.UtcNow,
                        CreatedUser = DatabaseFixture.Prefix
                    }, token).ConfigureAwait(false);
                },
                null,
                TestLog.NoOp).ConfigureAwait(false);

            return result.Hashes;
        }

        [Fact]
        public async Task ReloadingTheIdenticalFileASecondTimeLeavesEveryRowUnchangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            await LoadAsync(dbContext, ThreeRowCsv);
            var beforeIds = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId))
                .Select(e => e.Id).OrderBy(i => i).ToList();

            await LoadAsync(dbContext, ThreeRowCsv);
            var afterIds = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId))
                .Select(e => e.Id).OrderBy(i => i).ToList();

            afterIds.Should().Equal(beforeIds, "an identical reload must not create new rows or duplicate ids");
        }

        [Fact]
        public async Task AReloadWhoseFileNoLongerContainsARowSoftDeletesOnlyThatRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            await LoadAsync(dbContext, ThreeRowCsv);
            var seenHashes = await LoadAsync(dbContext, TwoRowCsvWithBravoRemoved);

            var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId, seenHashes,
                DatabaseFixture.Prefix, TestLog.NoOp);

            var removedEntry = removed.Should().ContainSingle().Subject;
            removedEntry.SanctionEntryReference.Should().Be("2");

            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId);
            var sanctionEntries = active as SanctionEntry[] ?? active.ToArray();
            sanctionEntries.Should().HaveCount(2);
            sanctionEntries.Should().NotContain(e => e.SanctionEntryReference == "2");
            sanctionEntries.Should().Contain(e => e.SanctionEntryReference == "1");
            sanctionEntries.Should().Contain(e => e.SanctionEntryReference == "3");
        }

        [Fact]
        public async Task ARowThatReappearsInALaterFileAfterBeingRemovedIsRevivedNotDuplicatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);

            await LoadAsync(dbContext, ThreeRowCsv);
            var originalBravoId = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId))
                .Single(e => e.SanctionEntryReference == "2").Id;

            var seenAfterRemoval = await LoadAsync(dbContext, TwoRowCsvWithBravoRemoved);
            await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId, seenAfterRemoval,
                DatabaseFixture.Prefix, TestLog.NoOp);

            await LoadAsync(dbContext, ThreeRowCsv);

            var active = (await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId)).ToList();
            active.Should().HaveCount(3);
            var revivedBravo = active.Should().ContainSingle(e => e.SanctionEntryReference == "2").Subject;
            revivedBravo.Id.Should().Be(originalBravoId, "reviving must reuse the original row, not insert a new one");
            revivedBravo.Deleted.Should().BeNull();
            revivedBravo.DeletedDate.Should().BeNull();
            revivedBravo.DeletedUser.Should().BeNull();
        }

        [Fact]
        public async Task ReconcileRemovedAsyncWithEmptyHashesDoesNotWipeOutTheWholeSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            await LoadAsync(dbContext, ThreeRowCsv);

            var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId,
                new HashSet<string>(), DatabaseFixture.Prefix, TestLog.NoOp);

            removed.Should().BeEmpty();
            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId);
            active.Should().HaveCount(3);
        }

        [Fact]
        public async Task ReconcileRemovedAsyncWithEmptyHashesLogsAtWarnNotInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            await LoadAsync(dbContext, ThreeRowCsv);
            var log = new TestLog();

            await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId, new HashSet<string>(),
                DatabaseFixture.Prefix, log);

            log.Entries.Should().ContainSingle(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "INFO");
        }

        [Fact]
        public async Task ReconcileRemovedAsyncNeverTouchesRowsBelongingToAnotherSourceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var otherSourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}SanctionSourceReconOther{Guid.NewGuid():N}"[..40],
                Delimiter = ',',
                MultiPartStringIndex = "1",
                ReferenceIndex = 0,
                Skip = 0
            });

            try
            {
                var repository = new SanctionsEntryRepository(dbContext);
                await repository.UpsertAsync(new SanctionEntry
                {
                    SanctionEntrySourceId = otherSourceId,
                    SanctionEntryElementValue = "Untouched Person",
                    SanctionEntryReference = "1",
                    SanctionPayload = "Untouched Person",
                    SanctionEntryHash = Guid.NewGuid().ToString("N"),
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });

                await LoadAsync(dbContext, ThreeRowCsv);

                await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId,
                    ["some-hash-that-does-not-exist"], DatabaseFixture.Prefix, TestLog.NoOp);

                var otherActive = await repository.GetActiveBySanctionEntrySourceIdAsync(otherSourceId);
                otherActive.Should().ContainSingle();
            }
            finally
            {
                await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId == otherSourceId).DeleteAsync();
                await dbContext.SanctionEntrySource.Where(w => w.Id == otherSourceId).DeleteAsync();
            }
        }

        [Fact]
        public async Task WhenEveryRowInASourceIsGenuinelyGoneReconcileRemovedAsyncSoftDeletesAllOfThemAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new SanctionsEntryRepository(dbContext);
            await LoadAsync(dbContext, ThreeRowCsv);

            var removed = await SanctionEntryFileImporter.ReconcileRemovedAsync(repository, sourceId,
                ["unrelated-hash-1", "unrelated-hash-2"], DatabaseFixture.Prefix, TestLog.NoOp);

            removed.Should().HaveCount(3);
            var active = await repository.GetActiveBySanctionEntrySourceIdAsync(sourceId);
            active.Should().BeEmpty();
        }
    }
}