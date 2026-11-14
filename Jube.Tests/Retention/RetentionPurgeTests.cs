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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Engine.Retention;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Xunit;

namespace Jube.Test.Retention
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RetentionPurgeTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime now = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly List<Guid> entries = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ArchiveKey.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.ArchiveTag.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.Archive.Where(w => entries.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private async Task<Guid> ArchiveAsync(DbContext dbContext, int daysOld)
        {
            var guid = Guid.NewGuid();
            entries.Add(guid);

            await dbContext.InsertAsync(new Archive
            {
                Json = "{}",
                EntityAnalysisModelInstanceEntryGuid = guid,
                CreatedDate = now.AddDays(-daysOld),
                ReferenceDate = now.AddDays(-daysOld)
            });
            await dbContext.InsertAsync(new ArchiveKey
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Key = "AccountId", KeyValueString = "a", ProcessingTypeId = 1
            });
            await dbContext.InsertAsync(new ArchiveTag
            {
                EntityAnalysisModelInstanceEntryGuid = guid, Name = "Fraud", Version = 1, CreatedDate = now
            });

            return guid;
        }

        private async Task CaseForAsync(DbContext dbContext, Guid entry, byte closedStatusId, byte legalHold = 0)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, closedStatusId);
            await dbContext.Case.Where(w => w.Id == caseId)
                .Set(s => s.EntityAnalysisModelInstanceEntryGuid, entry)
                .Set(s => s.LegalHold, legalHold).UpdateAsync();
        }

        private async Task<bool> ExistsAsync(DbContext dbContext, Guid guid) =>
            await dbContext.Archive.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == guid);

        [Fact]
        public async Task OldArchivedTransactionsAreRemovedWithTheirKeysAndTagsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var old = await ArchiveAsync(dbContext, 400);

            var result = await RetentionPurge.RunAsync(dbContext, 365, false, 1000, now);

            result.Archive.Should().BeGreaterThanOrEqualTo(1);
            (await ExistsAsync(dbContext, old)).Should().BeFalse();
            (await dbContext.ArchiveKey.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == old)).Should().BeFalse();
            (await dbContext.ArchiveTag.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == old)).Should().BeFalse();
        }

        [Fact]
        public async Task RecentArchivedTransactionsAreKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var recent = await ArchiveAsync(dbContext, 10);

            await RetentionPurge.RunAsync(dbContext, 365, false, 1000, now);

            (await ExistsAsync(dbContext, recent)).Should().BeTrue();
            (await dbContext.ArchiveKey.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == recent)).Should().BeTrue();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public async Task ATransactionAnOpenOrSuspendedCaseWasRaisedFromIsKeptWhateverItsAgeAsync(byte closedStatusId)
        {
            await using var dbContext = fx.GetDbContext();
            var old = await ArchiveAsync(dbContext, 900);
            await CaseForAsync(dbContext, old, closedStatusId);

            await RetentionPurge.RunAsync(dbContext, 365, false, 1000, now);

            (await ExistsAsync(dbContext, old)).Should().BeTrue();
        }

        [Fact]
        public async Task ATransactionACaseUnderLegalHoldWasRaisedFromIsKeptEvenWhenTheCaseIsClosedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var held = await ArchiveAsync(dbContext, 900);
            await CaseForAsync(dbContext, held, 3, 1);

            await RetentionPurge.RunAsync(dbContext, 365, false, 1000, now);

            (await ExistsAsync(dbContext, held)).Should().BeTrue();
        }

        [Fact]
        public async Task ATransactionAClosedCaseWithNoHoldWasRaisedFromIsRemovedAndTheCaseSurvivesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var old = await ArchiveAsync(dbContext, 900);
            await CaseForAsync(dbContext, old, 3);

            await RetentionPurge.RunAsync(dbContext, 365, false, 1000, now);

            (await ExistsAsync(dbContext, old)).Should().BeFalse();
            (await dbContext.Case.AnyAsync(w => w.EntityAnalysisModelInstanceEntryGuid == old)).Should().BeTrue();
        }

        [Fact]
        public async Task ADryRunCountsWhatItWouldRemoveAndRemovesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var old = await ArchiveAsync(dbContext, 400);

            var result = await RetentionPurge.RunAsync(dbContext, 365, true, 1000, now);

            result.DryRun.Should().BeTrue();
            result.Archive.Should().BeGreaterThanOrEqualTo(1);
            (await ExistsAsync(dbContext, old)).Should().BeTrue();
        }

        [Fact]
        public async Task ZeroDaysKeepsEverythingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var old = await ArchiveAsync(dbContext, 5000);

            var result = await RetentionPurge.RunAsync(dbContext, 0, false, 1000, now);

            result.Archive.Should().Be(0);
            (await ExistsAsync(dbContext, old)).Should().BeTrue();
        }

        [Fact]
        public async Task ABacklogLargerThanTheChunkIsRemovedInSeveralChunksAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var olds = new List<Guid>();

            for (var i = 0; i < 7; i++)
            {
                olds.Add(await ArchiveAsync(dbContext, 400 + i));
            }

            await RetentionPurge.RunAsync(dbContext, 365, false, 3, now);

            foreach (var old in olds)
            {
                (await ExistsAsync(dbContext, old)).Should().BeFalse();
            }
        }

        [Fact]
        public async Task ExpiredAskJoobyMemoryAndCacheAreRemovedAndLiveOnesKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var marker = $"{DatabaseFixture.Prefix}{Guid.NewGuid():N}";

            await dbContext.ExecuteAsync(
                "INSERT INTO \"AskJoobyDataCache\" (\"PointerKey\", \"TenantRegistryId\", \"CreatedUser\", \"Label\", \"ColumnsJson\", \"RowCount\", \"DataJson\", \"CreatedDate\", \"ExpiresAt\") VALUES (@k1, 1, 'u', 'l', '[]', 0, '[]', now() at time zone 'utc', (now() at time zone 'utc') - interval '1 day'), (@k2, 1, 'u', 'l', '[]', 0, '[]', now() at time zone 'utc', (now() at time zone 'utc') + interval '1 day')",
                new DataParameter("k1", marker + "old"), new DataParameter("k2", marker + "live"));

            try
            {
                await RetentionPurge.RunAsync(dbContext, 0, false, 1000, now);

                var left = await dbContext.QueryToListAsync<string>(
                    "SELECT \"PointerKey\" FROM \"AskJoobyDataCache\" WHERE \"PointerKey\" LIKE @p",
                    new DataParameter("p", marker + "%"));

                left.Should().Equal(marker + "live");
            }
            finally
            {
                await dbContext.ExecuteAsync("DELETE FROM \"AskJoobyDataCache\" WHERE \"PointerKey\" LIKE @p",
                    new DataParameter("p", marker + "%"));
            }
        }

        [Fact]
        public async Task InboundQueueRowsOfADeletedOrMissingModelAreRemovedAndLiveOnesKeptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var live = Guid.NewGuid();
            var deleted = Guid.NewGuid();
            var missing = Guid.NewGuid();
            var tenant = await dbContext.TenantRegistry.Select(s => s.Id).FirstAsync();

            var liveId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"ZzOrphanLive{Guid.NewGuid():N}"[..30], Guid = live, Active = 1, Locked = 0, Deleted = 0,
                TenantRegistryId = tenant
            });
            var deletedId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"ZzOrphanGone{Guid.NewGuid():N}"[..30], Guid = deleted, Active = 1, Locked = 0, Deleted = 1,
                TenantRegistryId = tenant
            });

            var ids = new List<long>();

            foreach (var guid in new[] { live, deleted, missing })
            {
                ids.Add(await dbContext.InsertWithInt64IdentityAsync(new TransactionInboundQueue
                {
                    EntityAnalysisModelGuid = guid, Payload = [123, 125], EnqueuedBy = "orphan-test"
                }));
            }

            try
            {
                var dry = await RetentionPurge.RunAsync(dbContext, 0, true, 1000, now);
                dry.Queue.Should().BeGreaterThanOrEqualTo(2);
                (await dbContext.TransactionInboundQueue.CountAsync(w => ids.Contains(w.Id))).Should().Be(3);

                var result = await RetentionPurge.RunAsync(dbContext, 0, false, 1000, now);

                result.Queue.Should().BeGreaterThanOrEqualTo(2);
                (await dbContext.TransactionInboundQueue.Where(w => ids.Contains(w.Id)).Select(s => s.EntityAnalysisModelGuid)
                    .ToListAsync()).Should().BeEquivalentTo([live]);
            }
            finally
            {
                await dbContext.TransactionInboundQueue.Where(w => ids.Contains(w.Id)).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == liveId || w.Id == deletedId).DeleteAsync();
            }
        }
    }
}
