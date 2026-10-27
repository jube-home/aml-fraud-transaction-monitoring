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
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Exceptions.Query.CaseKeyJournal;
using Jube.Service.Query.CaseKeyJournal;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseKeyJournalTagJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly List<Guid> createdArchiveGuids = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.ArchiveTag>()
                .Where(w => createdArchiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid)).DeleteAsync();

            await dbContext.Case.Where(w => createdArchiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();

            await dbContext.Archive.Where(w => createdArchiveGuids.Contains(w.EntityAnalysisModelInstanceEntryGuid))
                .DeleteAsync();

            var caseWorkflowGuids =
                dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id)).Select(s => s.Guid);

            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();

            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTag>()
                .Where(w => w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value))
                .DeleteAsync();

            await dbContext.EntityAnalysisModelRequestXpath
                .Where(w => w.EntityAnalysisModelId != null && createdModelIds.Contains(w.EntityAnalysisModelId.Value))
                .DeleteAsync();

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseKeyJournalTagJobService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalTagJobService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, Me).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(saved.Id);

            return saved.Id;
        }

        private async Task<Guid> CreateWorkflowAsync(DbContext dbContext, int modelId, bool grantRoleToUser = true)
        {
            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdWorkflowIds.Add(id);

            if (!grantRoleToUser)
            {
                return guid;
            }

            var roleGuid = await dbContext.UserRegistry.Where(u => u.Name == Me)
                .Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return guid;
        }

        private async Task CreateTagAsync(DbContext dbContext, int modelId, string name, bool active = true,
            bool deleted = false)
        {
            await dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelTag
            {
                EntityAnalysisModelId = modelId,
                Name = name,
                Active = (byte)(active ? 1 : 0),
                Locked = 0,
                Deleted = (byte)(deleted ? 1 : 0),
                Guid = Guid.NewGuid(),
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow,
                Version = 1,
            });
        }

        private async Task<Guid> CreateArchiveAsync(DbContext dbContext, int modelId, string accountId, params string[] tags)
        {
            var guid = Guid.NewGuid();
            var json = new JObject { ["payload"] = new JObject { ["AccountId"] = accountId }, ["tag"] = new JArray(tags) };

            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                EntryKeyValue = $"{DatabaseFixture.Prefix}Key",
                ResponseElevation = 5,
                ActivationRuleCount = 0,
                CreatedDate = DateTime.UtcNow,
                ReferenceDate = DateTime.UtcNow,
            }).ConfigureAwait(false);
            createdArchiveGuids.Add(guid);

            return guid;
        }

        private static async Task<string[]> StoredTagsAsync(DbContext dbContext, Guid guid)
        {
            var json = await dbContext.Archive.Where(a => a.EntityAnalysisModelInstanceEntryGuid == guid)
                .Select(a => a.Json).FirstAsync();

            return (JObject.Parse(json)["tag"] as JArray)!.Select(t => t.ToString()).ToArray();
        }

        private async Task<(Guid Workflow, int Model)> SetupAsync(DbContext dbContext, params string[] tags)
        {
            var model = await CreateModelAsync(dbContext);
            var workflow = await CreateWorkflowAsync(dbContext, model);

            foreach (var tag in tags)
            {
                await CreateTagAsync(dbContext, model, tag);
            }

            return (workflow, model);
        }

        private CaseKeyJournalTagJobRunner Runner() =>
            new(dynamicEnvironment, TestLog.NoOp, localizers, new NullServiceChangeBus());

        private static CaseKeyJournalTagJobRequestDto Request(Guid workflow, string key, string[]? add = null,
            string[]? remove = null)
        {
            return new CaseKeyJournalTagJobRequestDto
            {
                CaseWorkflowGuid = workflow, Key = "AccountId", Value = key, Add = add, Remove = remove
            };
        }

        private async Task<(Guid Workflow, int Model, string Key, List<Guid> Rows)> SeedAsync(DbContext dbContext,
            int rows, params string[] tags)
        {
            var (workflow, model) = await SetupAsync(dbContext, tags.Concat(["Alpha", "Beta"]).Distinct().ToArray());
            var key = $"{DatabaseFixture.Prefix}K{Guid.NewGuid():N}"[..30];
            var guids = new List<Guid>();

            for (var i = 0; i < rows; i++)
            {
                guids.Add(await CreateArchiveAsync(dbContext, model, key, tags));
            }

            return (workflow, model, key, guids);
        }

        private async Task RunAllAsync()
        {
            while (await Runner().RunNextAsync())
            {
            }
        }

        [Fact]
        public async Task PreviewCountsTheTransactionsOfTheViewAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, _) = await SeedAsync(dbContext, 7);
            await CreateArchiveAsync(dbContext, model, "someone-else");
            var service = await BuildServiceAsync(dbContext, Me);

            var preview = await service.PreviewAsync(Request(workflow, key));

            preview.Matching.Should().Be(7);
            preview.TooMany.Should().BeFalse();
            preview.Limit.Should().Be(CaseKeyJournalTagJobService.MaximumMatches);
            (await service.PreviewAsync(Request(workflow, "nobody"))).Matching.Should().Be(0);
        }

        [Fact]
        public async Task ASubmittedJobTagsEveryMatchingTransactionAndReportsItsProgressAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, rows) = await SeedAsync(dbContext, 1200);
            var other = await CreateArchiveAsync(dbContext, model, "someone-else");
            var service = await BuildServiceAsync(dbContext, Me);

            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            queued.State.Should().Be("Queued");
            queued.Kind.Should().Be("Tag");
            queued.Total.Should().Be(1200);
            queued.Add.Should().Equal("Alpha");
            queued.CanUndo.Should().BeFalse();

            await RunAllAsync();

            var done = await service.GetAsync(queued.Guid);

            done.State.Should().Be("Done");
            done.Total.Should().Be(1200);
            done.Processed.Should().Be(1200);
            done.Changed.Should().Be(1200);
            done.Skipped.Should().Be(0);
            done.Locked.Should().Be(0);
            done.CanUndo.Should().BeTrue();
            done.StartedDate.Should().NotBeNull();
            done.FinishedDate.Should().NotBeNull();

            foreach (var row in rows.Take(3).Concat(rows.TakeLast(3)))
            {
                (await StoredTagsAsync(dbContext, row)).Should().Equal("Alpha");
            }

            (await LinqToDB.Data.DataConnectionExtensions.QueryToArrayAsync<long>(dbContext,
                "select count(*) from \"Archive\" where \"Json\" @> jsonb_build_object('payload', " +
                "jsonb_build_object('AccountId', @key), 'tag', jsonb_build_array('Alpha'))",
                new LinqToDB.Data.DataParameter("key", key)))[0].Should().Be(1200);
            (await StoredTagsAsync(dbContext, other)).Should().BeEmpty();
        }

        [Fact]
        public async Task AJobAddsAndRemovesAndLeavesRowsThatAlreadyHaveTheTagAloneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 4, "Beta");
            var service = await BuildServiceAsync(dbContext, Me);

            var queued = await service.SubmitAsync(Request(workflow, key, ["Beta", "Alpha"], ["Beta"]));

            await RunAllAsync();

            var done = await service.GetAsync(queued.Guid);

            done.State.Should().Be("Done");
            done.Changed.Should().Be(4);
            (await StoredTagsAsync(dbContext, rows[0])).Should().Equal("Alpha");

            var again = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            await RunAllAsync();

            var second = await service.GetAsync(again.Guid);

            second.Processed.Should().Be(4);
            second.Changed.Should().Be(0);
            second.CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task UndoPutsEveryChangedTransactionBackAndCanOnlyBeAskedForOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 700, "Beta");
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"], ["Beta"]));

            await RunAllAsync();

            (await StoredTagsAsync(dbContext, rows[10])).Should().Equal("Alpha");

            var undo = await service.UndoAsync(queued.Guid);

            undo.Kind.Should().Be("Undo");
            undo.UndoOf.Should().Be(queued.Guid);
            undo.Total.Should().Be(700);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.UndoAsync(queued.Guid));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("TagJobNotUndoable");

            await RunAllAsync();

            var done = await service.GetAsync(undo.Guid);

            done.State.Should().Be("Done");
            done.Changed.Should().Be(700);
            done.CanUndo.Should().BeFalse();
            (await StoredTagsAsync(dbContext, rows[10])).Should().Equal("Beta");
            (await StoredTagsAsync(dbContext, rows[^1])).Should().Equal("Beta");
            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeFalse();
        }

        [Fact]
        public async Task ACaseLockedToAnotherUserIsLeftAloneAndCountedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 3);
            await dbContext.InsertAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = rows[0],
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 1,
                LockedUser = fx.Seed.UserTenantB,
                ClosedStatusId = 0,
                CaseKey = "AccountId"
            });
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            try
            {
                await RunAllAsync();

                var done = await service.GetAsync(queued.Guid);

                done.Changed.Should().Be(2);
                done.Locked.Should().Be(1);
                (await StoredTagsAsync(dbContext, rows[0])).Should().BeEmpty();
            }
            finally
            {
                await dbContext.Case.Where(c => c.EntityAnalysisModelInstanceEntryGuid == rows[0]).DeleteAsync();
            }
        }

        [Fact]
        public async Task AQueuedJobCanBeCancelledAndANeverRunJobIsNotPickedUpAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 3);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            var cancelled = await service.CancelAsync(queued.Guid);

            cancelled.State.Should().Be("Cancelled");
            cancelled.FinishedDate.Should().NotBeNull();

            await RunAllAsync();

            (await service.GetAsync(queued.Guid)).Processed.Should().Be(0);
            (await StoredTagsAsync(dbContext, rows[0])).Should().BeEmpty();
        }

        [Fact]
        public async Task ARunningJobStopsAtTheEndOfTheBatchWhenCancelledAndCanStillBeUndoneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 1100);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));
            var repository = new CaseKeyJournalTagJobRepository(dbContext);

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.CancelRequested, (byte)1).UpdateAsync();

            (await Runner().RunNextAsync()).Should().BeFalse();
            (await service.GetAsync(queued.Guid)).State.Should().Be("Queued");

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.CancelRequested, (byte)0).UpdateAsync();

            var claimed = await repository.ClaimNextAsync();

            claimed!.Guid.Should().Be(queued.Guid);

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.CancelRequested, (byte)1).UpdateAsync();
            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.HeartbeatDate, DateTime.UtcNow.AddHours(-1)).UpdateAsync();

            (await Runner().RunNextAsync()).Should().BeTrue();

            var stopped = await service.GetAsync(queued.Guid);

            stopped.State.Should().Be("Cancelled");
            stopped.Changed.Should().Be(0);
            (await StoredTagsAsync(dbContext, rows[0])).Should().BeEmpty();
        }

        [Fact]
        public async Task AJobWhoseTagIsNoLongerAllowedFailsWithWhyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, rows) = await SeedAsync(dbContext, 3);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelTag>()
                .Where(t => t.EntityAnalysisModelId == model && t.Name == "Alpha")
                .Set(t => t.Active, (byte)0).UpdateAsync();

            await RunAllAsync();

            var failed = await service.GetAsync(queued.Guid);

            failed.State.Should().Be("Failed");
            failed.Error.Should().NotBeNullOrEmpty();
            failed.FinishedDate.Should().NotBeNull();
            (await StoredTagsAsync(dbContext, rows[0])).Should().BeEmpty();
        }

        [Fact]
        public async Task ARunningJobThatStoppedReportingIsTakenUpAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 3);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));
            var repository = new CaseKeyJournalTagJobRepository(dbContext);

            await repository.ClaimNextAsync();

            (await repository.ClaimNextAsync()).Should().BeNull();

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.HeartbeatDate, DateTime.UtcNow.AddMinutes(-30)).UpdateAsync();

            await RunAllAsync();

            (await service.GetAsync(queued.Guid)).State.Should().Be("Done");
            (await StoredTagsAsync(dbContext, rows[0])).Should().Equal("Alpha");
        }

        [Fact]
        public async Task TheViewIsValidatedAndTooManyOrNoMatchesAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, _) = await SeedAsync(dbContext, 2);
            var service = await BuildServiceAsync(dbContext, Me);

            async Task<string[]> CodesAsync(CaseKeyJournalTagJobRequestDto request)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SubmitAsync(request));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            (await CodesAsync(Request(workflow, "nobody", ["Alpha"]))).Should().Contain("TagJobNoMatches");
            (await CodesAsync(Request(workflow, key))).Should().Contain("TagChangeRequired");
            (await CodesAsync(Request(workflow, key, ["Invented"]))).Should().Contain("TagUnknown");
            (await CodesAsync(Request(workflow, key, [" "]))).Should().Contain("TagNameInvalid");
            (await CodesAsync(new CaseKeyJournalTagJobRequestDto
                { CaseWorkflowGuid = workflow, Key = "", Value = "", Add = ["Alpha"] }))
                .Should().Contain("KeyRequired").And.Contain("ValueRequired");
            (await CodesAsync(new CaseKeyJournalTagJobRequestDto
                {
                    CaseWorkflowGuid = workflow, Key = "AccountId", Value = key, Add = ["Alpha"],
                    From = DateTimeOffset.UtcNow, To = DateTimeOffset.UtcNow.AddDays(-1)
                })).Should().Contain("PeriodInvalid");
            (await CodesAsync(new CaseKeyJournalTagJobRequestDto
                {
                    CaseWorkflowGuid = workflow, Key = "AccountId", Value = key, Add = ["Alpha"], Merge = "{bad"
                })).Should().Contain("MergeInvalid");
        }

        [Fact]
        public async Task MergedKeysAndTheWindowAndActivationsDecideWhichTransactionsMatchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, _) = await SeedAsync(dbContext, 2);
            var device = $"{DatabaseFixture.Prefix}Dev{Guid.NewGuid():N}"[..30];
            await InsertWithDeviceAsync(dbContext, model, "someone-else", device);
            await InsertWithDeviceAsync(dbContext, model, key, device);
            var service = await BuildServiceAsync(dbContext, Me);
            var merge = $"[{{\"key\":\"DeviceId\",\"value\":\"{device}\"}}]";

            (await service.PreviewAsync(Request(workflow, key))).Matching.Should().Be(3);

            var request = Request(workflow, key);
            request.Merge = merge;

            (await service.PreviewAsync(request)).Matching.Should().Be(4);

            request.ActivationsOnly = true;

            (await service.PreviewAsync(request)).Matching.Should().Be(0);

            var past = Request(workflow, key);
            past.To = DateTimeOffset.UtcNow.AddDays(-1);
            past.From = DateTimeOffset.UtcNow.AddDays(-5);

            (await service.PreviewAsync(past)).Matching.Should().Be(0);
        }

        [Fact]
        public async Task PreviewPagesThroughMoreMatchesThanOnePageHoldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, _) = await SeedAsync(dbContext, 0);
            var service = await BuildServiceAsync(dbContext, Me);
            var count = JournalMatcher.PageSize + 100;
            var archives = Enumerable.Range(0, count).Select(_ => new Data.Poco.Archive
            {
                Json = new JObject { ["payload"] = new JObject { ["AccountId"] = key }, ["tag"] = new JArray() }
                    .ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                EntityAnalysisModelId = model,
                EntryKeyValue = "k",
                ResponseElevation = 1,
                ActivationRuleCount = 0,
                CreatedDate = DateTime.UtcNow,
                ReferenceDate = DateTime.UtcNow
            }).ToList();

            await LinqToDB.Data.DataConnectionExtensions.BulkCopyAsync(dbContext, archives);
            createdArchiveGuids.AddRange(archives.Select(a => a.EntityAnalysisModelInstanceEntryGuid));

            (await service.PreviewAsync(Request(workflow, key))).Matching.Should().Be(count);

            var filter = new JournalMatchFilter("AccountId", key, DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddMinutes(5), false, 0, []);
            var capped = await JournalMatcher.GuidsAsync(connectionString, TestLog.NoOp, model, Me, filter,
                new HashSet<string>(), 10, CancellationToken.None);

            capped.Should().HaveCount(JournalMatcher.PageSize);
        }

        [Fact]
        public async Task AStoppedOrFailedJobCannotBeUndoneAndACancelledUndoCanBeAskedForAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, _) = await SeedAsync(dbContext, 4);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            await RunAllAsync();

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.StateId, (byte)4).UpdateAsync();
            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeFalse();

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.StateId, (byte)5).UpdateAsync();
            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeFalse();

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.StateId, (byte)3).UpdateAsync();
            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeTrue();

            var undo = await service.UndoAsync(queued.Guid);

            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeFalse();
            await service.CancelAsync(undo.Guid);
            (await service.GetAsync(queued.Guid)).CanUndo.Should().BeTrue();
            (await service.UndoAsync(queued.Guid)).Guid.Should().NotBe(undo.Guid);
        }

        [Fact]
        public async Task AnUndoCountsTheTransactionsItLeftAloneBecauseTheirCaseIsLockedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 3);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));

            await RunAllAsync();

            await dbContext.InsertAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = rows[0],
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 1,
                LockedUser = fx.Seed.UserTenantB,
                ClosedStatusId = 0,
                CaseKey = "AccountId"
            });

            try
            {
                var undo = await service.UndoAsync(queued.Guid);

                await RunAllAsync();

                var done = await service.GetAsync(undo.Guid);

                done.Locked.Should().Be(1);
                done.Changed.Should().Be(2);
                (await StoredTagsAsync(dbContext, rows[0])).Should().Equal("Alpha");
            }
            finally
            {
                await dbContext.Case.Where(c => c.EntityAnalysisModelInstanceEntryGuid == rows[0]).DeleteAsync();
            }
        }

        [Fact]
        public async Task ARunningJobIsStoppedByTheServiceAtTheNextBatchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, rows) = await SeedAsync(dbContext, 3);
            var service = await BuildServiceAsync(dbContext, Me);
            var queued = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));
            var repository = new CaseKeyJournalTagJobRepository(dbContext);

            await repository.ClaimNextAsync();

            var asked = await service.CancelAsync(queued.Guid);

            asked.State.Should().Be("Running");

            await dbContext.CaseKeyJournalTagJob.Where(j => j.Guid == queued.Guid)
                .Set(j => j.HeartbeatDate, DateTime.UtcNow.AddHours(-1)).UpdateAsync();
            await RunAllAsync();

            (await service.GetAsync(queued.Guid)).State.Should().Be("Cancelled");
            (await StoredTagsAsync(dbContext, rows[0])).Should().BeEmpty();
        }

        [Fact]
        public async Task TheMatcherStopsReadingOnceMoreThanTheMaximumMatchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, key, _) = await SeedAsync(dbContext, 6);
            var filter = new JournalMatchFilter("AccountId", key, DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddMinutes(5), false, 0, []);

            var all = await JournalMatcher.GuidsAsync(connectionString, TestLog.NoOp, model, Me, filter,
                new HashSet<string>(), 100, CancellationToken.None);
            var capped = await JournalMatcher.GuidsAsync(connectionString, TestLog.NoOp, model, Me, filter,
                new HashSet<string>(), 3, CancellationToken.None);

            all.Should().HaveCount(6);
            capped.Count.Should().BeGreaterThan(3);
        }

        private Task CreateRequestFieldAsync(DbContext dbContext, int model, string name, int dataTypeId)
        {
            return dbContext.InsertAsync(new Data.Poco.EntityAnalysisModelRequestXpath
            {
                EntityAnalysisModelId = model,
                Name = name,
                DataTypeId = dataTypeId,
                XPath = $"$.{name}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = Me,
                CreatedDate = DateTime.UtcNow
            });
        }

        private static string ChannelFilter(string operatorName, string value, string field = "Payload.Channel")
        {
            return new JObject
            {
                ["condition"] = "AND",
                ["rules"] = new JArray(new JObject { ["id"] = field, ["operator"] = operatorName, ["value"] = value })
            }.ToString(Newtonsoft.Json.Formatting.None);
        }

        private async Task<Guid> CreateWithPayloadAsync(DbContext dbContext, int model, string accountId,
            string channel, object amount, string? device = null)
        {
            var guid = Guid.NewGuid();
            var payload = new JObject { ["AccountId"] = accountId, ["Channel"] = channel, ["Amount"] = JToken.FromObject(amount) };

            if (device is not null)
            {
                payload["DeviceId"] = device;
            }

            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = new JObject { ["payload"] = payload, ["tag"] = new JArray() }.ToString(
                    Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = model,
                EntryKeyValue = "k",
                ResponseElevation = 1,
                ActivationRuleCount = 0,
                CreatedDate = DateTime.UtcNow,
                ReferenceDate = DateTime.UtcNow
            });
            createdArchiveGuids.Add(guid);

            return guid;
        }

        [Fact]
        public async Task AFilterOnTheArchiveDecidesWhichTransactionsAreCountedAndTaggedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha", "Beta", "Gamma");
            await CreateRequestFieldAsync(dbContext, model, "Channel", 1);
            await CreateRequestFieldAsync(dbContext, model, "Amount", 3);
            var key = $"{DatabaseFixture.Prefix}K{Guid.NewGuid():N}"[..30];
            var cards = new List<Guid>();
            var webs = new List<Guid>();

            for (var i = 0; i < 3; i++)
            {
                cards.Add(await CreateWithPayloadAsync(dbContext, model, key, "Card", 10));
            }

            for (var i = 0; i < 2; i++)
            {
                webs.Add(await CreateWithPayloadAsync(dbContext, model, key, "Web", 99));
            }

            var none = await CreateArchiveAsync(dbContext, model, key);
            var service = await BuildServiceAsync(dbContext, Me);

            (await service.PreviewAsync(Request(workflow, key))).Matching.Should().Be(6);

            var preview = Request(workflow, key);
            preview.Filter = ChannelFilter("equal", "Card");

            (await service.PreviewAsync(preview)).Matching.Should().Be(3);

            preview.Filter = ChannelFilter("greater", "50", "Payload.Amount");

            (await service.PreviewAsync(preview)).Matching.Should().Be(2);

            preview.Filter = ChannelFilter("not_equal", "Web");

            (await service.PreviewAsync(preview)).Matching.Should().Be(3);

            preview.Filter = ChannelFilter("like", "W%");

            (await service.PreviewAsync(preview)).Matching.Should().Be(2);

            var submit = Request(workflow, key, ["Gamma"]);
            submit.Filter = ChannelFilter("equal", "Card");

            var queued = await service.SubmitAsync(submit);

            queued.Total.Should().Be(3);

            await RunAllAsync();

            (await service.GetAsync(queued.Guid)).Changed.Should().Be(3);

            foreach (var guid in cards)
            {
                (await StoredTagsAsync(dbContext, guid)).Should().BeEquivalentTo("Gamma");
            }

            foreach (var guid in webs.Append(none))
            {
                (await StoredTagsAsync(dbContext, guid)).Should().NotContain("Gamma");
            }

            var stored = await dbContext.CaseKeyJournalTagJob.Where(job => job.Guid == queued.Guid).FirstAsync();

            stored.FilterJson.Should().Be(submit.Filter);
        }

        [Fact]
        public async Task AFieldWhoseStoredValueCannotBeReadAsItsTypeIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            await CreateRequestFieldAsync(dbContext, model, "Amount", 3);
            var key = $"{DatabaseFixture.Prefix}K{Guid.NewGuid():N}"[..30];
            await CreateWithPayloadAsync(dbContext, model, key, "Card", 1);
            await CreateWithPayloadAsync(dbContext, model, key, "Card", "abc");
            var service = await BuildServiceAsync(dbContext, Me);

            var preview = Request(workflow, key);
            preview.Filter = ChannelFilter("greater", "0", "Payload.Amount");

            var ex = await Record.ExceptionAsync(() => service.PreviewAsync(preview));

            ex.Should().BeOfType<DtoValidationException>().Which.Result.Errors.Select(e => e.ErrorCode)
                .Should().Contain("FilterUnreadable");
        }

        [Fact]
        public async Task TheFilterAppliesToEveryLaneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            await CreateRequestFieldAsync(dbContext, model, "Channel", 1);
            var key = $"{DatabaseFixture.Prefix}K{Guid.NewGuid():N}"[..30];
            var device = $"{DatabaseFixture.Prefix}Dev{Guid.NewGuid():N}"[..30];
            await CreateWithPayloadAsync(dbContext, model, key, "Card", 1);
            await CreateWithPayloadAsync(dbContext, model, key, "Web", 1);
            await CreateWithPayloadAsync(dbContext, model, "someone-else", "Card", 1, device);
            await CreateWithPayloadAsync(dbContext, model, "someone-else", "Web", 1, device);
            var service = await BuildServiceAsync(dbContext, Me);

            var request = Request(workflow, key);
            request.Merge = $"[{{\"key\":\"DeviceId\",\"value\":\"{device}\"}}]";

            (await service.PreviewAsync(request)).Matching.Should().Be(4);

            request.Filter = ChannelFilter("equal", "Card");

            (await service.PreviewAsync(request)).Matching.Should().Be(2);
        }

        [Fact]
        public async Task AFilterThatIsNotValidIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, _) = await SeedAsync(dbContext, 2);
            await CreateRequestFieldAsync(dbContext, model, "Channel", 1);
            var service = await BuildServiceAsync(dbContext, Me);

            async Task<string[]> CodesAsync(CaseKeyJournalTagJobRequestDto request)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SubmitAsync(request));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            var broken = Request(workflow, key, ["Alpha"]);
            broken.Filter = "not json";

            (await CodesAsync(broken)).Should().Contain("FilterInvalid");

            var unknown = Request(workflow, key, ["Alpha"]);
            unknown.Filter = ChannelFilter("equal", "Card", "Payload.NoSuchField");

            (await CodesAsync(unknown)).Should().Contain("FilterInvalid");

            var wrongOperator = Request(workflow, key, ["Alpha"]);
            wrongOperator.Filter = ChannelFilter("contains", "Card");

            (await CodesAsync(wrongOperator)).Should().Contain("FilterInvalid");
        }

        private async Task InsertWithDeviceAsync(DbContext dbContext, int model, string accountId, string device)
        {
            var guid = Guid.NewGuid();
            var json = new JObject
            {
                ["payload"] = new JObject { ["AccountId"] = accountId, ["DeviceId"] = device },
                ["tag"] = new JArray()
            };

            await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json.ToString(Newtonsoft.Json.Formatting.None),
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = model,
                EntryKeyValue = "k",
                ResponseElevation = 1,
                ActivationRuleCount = 0,
                CreatedDate = DateTime.UtcNow,
                ReferenceDate = DateTime.UtcNow
            });
            createdArchiveGuids.Add(guid);
        }

        [Fact]
        public async Task OnlyTheCallersOwnJobsAreVisibleAndTheListIsNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, _, key, _) = await SeedAsync(dbContext, 2);
            var service = await BuildServiceAsync(dbContext, Me);
            var first = await service.SubmitAsync(Request(workflow, key, ["Alpha"]));
            var second = await service.SubmitAsync(Request(workflow, key, ["Beta"]));
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.ListAsync(workflow)).Select(j => j.Guid).Should().Equal(second.Guid, first.Guid);

            var ex = await Record.ExceptionAsync(() => outsider.GetAsync(first.Guid));
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<NotFoundException>(() => service.CancelAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<NotFoundException>(() => service.UndoAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model, key, _) = await SeedAsync(dbContext, 2);
            var hidden = await CreateWorkflowAsync(dbContext, model, grantRoleToUser: false);
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.PreviewAsync(Request(workflow, key)));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.SubmitAsync(Request(workflow, key, ["Alpha"])));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.ListAsync(workflow));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.CancelAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.UndoAsync(Guid.NewGuid()));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.PreviewAsync(Request(hidden, key)));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.ListAsync(hidden));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.PreviewAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.SubmitAsync(null));

            var ex = await Record.ExceptionAsync(() => outsider.PreviewAsync(Request(workflow, key)));
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
        }
    }
}
