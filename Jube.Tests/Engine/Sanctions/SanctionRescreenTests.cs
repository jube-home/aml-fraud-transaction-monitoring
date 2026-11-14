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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.Options;
using Jube.Engine.Sanctions;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using SanctionEntry = Jube.Engine.Sanctions.Models.SanctionEntry;

namespace Jube.Test.Engine.Sanctions
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class SanctionRescreenTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly DateTime now = DateTime.UtcNow;
        private readonly CaseRiskSeeder seeder = new(fx);
        private readonly List<int> runIds = [];
        private readonly List<Guid> archiveGuids = [];
        private readonly List<int> sourceIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            SanctionExemptionSet.Shared.Clear();
            await using var dbContext = fx.GetDbContext();
            await dbContext.SanctionRescreenHit.Where(w => runIds.Contains(w.RunId)).DeleteAsync();
            await dbContext.SanctionRescreenRun.Where(w => runIds.Contains(w.Id)).DeleteAsync();
            await dbContext.UserNotification.Where(w => w.DedupeKey.StartsWith("sanction-rescreen:")
                                                        && runIds.Select(r => "sanction-rescreen:" + r).Contains(w.DedupeKey))
                .DeleteAsync();
            await dbContext.Archive.Where(w => archiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();
            await dbContext.SanctionEntry.Where(w => w.SanctionEntrySourceId != null
                                                     && sourceIds.Contains(w.SanctionEntrySourceId.Value)).DeleteAsync();
            await dbContext.SanctionEntrySource.Where(w => sourceIds.Contains(w.Id)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private async Task<(int ModelId, int Tenant)> ModelAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var model = await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Join(dbContext.EntityAnalysisModel, c => c.EntityAnalysisModelId, m => m.Id,
                    (c, m) => new { m.Id, m.TenantRegistryId }).FirstAsync();

            return (model.Id, model.TenantRegistryId.GetValueOrDefault());
        }

        private async Task ScreenedAsync(DbContext dbContext, int modelId, string name, int daysAgo = 1)
        {
            var guid = Guid.NewGuid();
            archiveGuids.Add(guid);

            await dbContext.InsertAsync(new Archive
            {
                Json = $"{{\"payload\":{{\"JoinedName\":\"{name}\"}}}}",
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                CreatedDate = now.AddDays(-daysAgo),
                ReferenceDate = now.AddDays(-daysAgo)
            });
        }

        private static ConcurrentDictionary<int, SanctionEntry> Entries(params (int Id, string Reference, string[] Words)[] entries)
        {
            var dictionary = new ConcurrentDictionary<int, SanctionEntry>();

            foreach (var (id, reference, words) in entries)
            {
                dictionary.TryAdd(id, new SanctionEntry
                {
                    SanctionEntryId = id, SanctionEntrySourceId = 5, SanctionEntryReference = reference,
                    SanctionElementValue = words
                });
            }

            return dictionary;
        }

        private async Task<SanctionRescreenRun> RunAsync(DbContext dbContext, int modelId, int tenant,
            ConcurrentDictionary<int, SanctionEntry> all, HashSet<int> changed, int sinceDays = 30,
            int maximumNames = 1000, int distance = 0)
        {
            var run = await SanctionRescreen.RunAsync(dbContext,
                [new RescreenCheck(tenant, modelId, "JoinedName", distance, 0.5, 5.0)], all, changed,
                new ConcurrentDictionary<string, byte>(), sinceDays, maximumNames, now, 1);
            runIds.Add(run.Id);

            return run;
        }

        [Fact]
        public async Task ARecentlyScreenedNameThatMatchesAChangedEntryIsRecordedAsAHitAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");
            await ScreenedAsync(dbContext, modelId, "Maria Gomez");
            var all = Entries((1, "R1", ["ivan", "petrov"]));

            var run = await RunAsync(dbContext, modelId, tenant, all, [1]);

            run.Hits.Should().Be(1);
            run.NamesScreened.Should().Be(2);
            run.EntriesConsidered.Should().Be(1);
            var hit = await dbContext.SanctionRescreenHit.SingleAsync(w => w.RunId == run.Id);
            hit.ScreenedName.Should().Be("Ivan Petrov");
            hit.SanctionEntryReference.Should().Be("R1");
            hit.ListedName.Should().Be("ivan petrov");
            hit.TenantRegistryId.Should().Be(tenant);
            (await dbContext.SanctionRescreenRun.SingleAsync(w => w.Id == run.Id)).EndDate.Should().NotBeNull();
        }

        [Fact]
        public async Task AnEntryThatDidNotChangeIsNotRescreenedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");
            var all = Entries((1, "R1", ["ivan", "petrov"]), (2, "R2", ["ivan", "petrov"]));

            var run = await RunAsync(dbContext, modelId, tenant, all, [2]);

            run.Hits.Should().Be(1);
            (await dbContext.SanctionRescreenHit.SingleAsync(w => w.RunId == run.Id)).SanctionEntryReference
                .Should().Be("R2");
        }

        [Fact]
        public async Task RunningAgainRecordsNoDuplicateHitAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");
            var all = Entries((1, "R1", ["ivan", "petrov"]));

            await RunAsync(dbContext, modelId, tenant, all, [1]);
            var second = await RunAsync(dbContext, modelId, tenant, all, [1]);

            second.Hits.Should().Be(0);
            (await dbContext.SanctionRescreenHit.CountAsync(w => w.TenantRegistryId == tenant
                                                                  && w.SanctionEntryId == 1
                                                                  && w.ScreenedName == "Ivan Petrov")).Should().Be(1);
        }

        [Fact]
        public async Task ANameOutsideTheWindowIsNotRescreenedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov", 90);

            var run = await RunAsync(dbContext, modelId, tenant, Entries((1, "R1", ["ivan", "petrov"])), [1], 30);

            run.Hits.Should().Be(0);
            run.NamesScreened.Should().Be(0);
        }

        [Fact]
        public async Task AnApprovedExemptionKeepsTheNameFromBeingReportedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");
            SanctionExemptionSet.Shared.Replace([
                new ApprovedSanctionExemption(1, tenant, SanctionExemptionSet.NameKey("Ivan Petrov"), 5, "R1", null)
            ]);

            var run = await RunAsync(dbContext, modelId, tenant, Entries((1, "R1", ["ivan", "petrov"])), [1]);

            run.Hits.Should().Be(0);
        }

        [Fact]
        public async Task TheNumberOfNamesRescreenedForACheckIsCappedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);

            for (var i = 0; i < 5; i++)
            {
                await ScreenedAsync(dbContext, modelId, $"Person Number{i}");
            }

            var run = await RunAsync(dbContext, modelId, tenant, Entries((1, "R1", ["ivan", "petrov"])), [1], 30, 3);

            run.NamesScreened.Should().Be(3);
        }

        [Fact]
        public async Task NothingIsScreenedWhenNoEntryChangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");

            var run = await RunAsync(dbContext, modelId, tenant, Entries((1, "R1", ["ivan", "petrov"])), []);

            run.Hits.Should().Be(0);
            run.NamesScreened.Should().Be(0);
        }

        [Fact]
        public async Task AHitNotifiesTheHoldersOfTheSanctionsPermissionOncePerRunAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (modelId, tenant) = await ModelAsync(dbContext);
            await ScreenedAsync(dbContext, modelId, "Ivan Petrov");
            await ScreenedAsync(dbContext, modelId, "Ivan Petrof");

            var run = await RunAsync(dbContext, modelId, tenant, Entries((1, "R1", ["ivan", "petrov"])), [1], 30,
                1000, 1);

            run.Hits.Should().Be(2);
            var mine = await dbContext.UserNotification
                .Where(w => w.DedupeKey == "sanction-rescreen:" + run.Id && w.RecipientUser == fx.Seed.UserWithPermission)
                .ToListAsync();
            mine.Should().ContainSingle();
            mine[0].KindId.Should().Be((short)NotificationKind.SanctionRescreenHits);
            Newtonsoft.Json.Linq.JObject.Parse(mine[0].ArgumentsJson).Value<int>("count").Should().Be(2);
            (await dbContext.UserNotification.AnyAsync(w => w.DedupeKey == "sanction-rescreen:" + run.Id
                                                            && w.RecipientUser == fx.Seed.UserWithoutPermission))
                .Should().BeFalse();
        }

        [Fact]
        public async Task UpsertingWithAnImportStampsInsertedAndRevivedEntriesButNotUnchangedOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var sourceId = await dbContext.InsertWithInt32IdentityAsync(new SanctionEntrySource
            {
                Name = $"{DatabaseFixture.Prefix}Rescreen{Guid.NewGuid():N}"[..40], Delimiter = ',',
                MultiPartStringIndex = "0", ReferenceIndex = 0
            });
            sourceIds.Add(sourceId);
            var repository = new SanctionEntryRepository(dbContext);

            Data.Poco.SanctionEntry Row(string hash) => new()
            {
                SanctionEntrySourceId = sourceId, SanctionEntryElementValue = "A B", SanctionEntryReference = "X",
                SanctionEntryHash = hash, CreatedDate = now, Deleted = 0
            };

            var (inserted, insertOutcome) = await repository.UpsertAsync(Row("h1"), default, 11);
            insertOutcome.Should().Be(SanctionEntryUpsertOutcome.Inserted);
            (await dbContext.SanctionEntry.SingleAsync(w => w.Id == inserted.Id)).LastImportId.Should().Be(11);

            var (_, unchangedOutcome) = await repository.UpsertAsync(Row("h1"), default, 12);
            unchangedOutcome.Should().Be(SanctionEntryUpsertOutcome.Unchanged);
            (await dbContext.SanctionEntry.SingleAsync(w => w.Id == inserted.Id)).LastImportId.Should().Be(11);

            await repository.DeleteAsync(inserted.Id, "test");
            var (_, revivedOutcome) = await repository.UpsertAsync(Row("h1"), default, 13);
            revivedOutcome.Should().Be(SanctionEntryUpsertOutcome.Revived);
            (await dbContext.SanctionEntry.SingleAsync(w => w.Id == inserted.Id)).LastImportId.Should().Be(13);

            var changed = await new SanctionRescreenRepository(dbContext).ListEntryIdsChangedAfterAsync(12);
            changed.Should().Contain(inserted.Id);
            (await new SanctionRescreenRepository(dbContext).ListEntryIdsChangedAfterAsync(13)).Should()
                .NotContain(inserted.Id);
        }
    }
}
