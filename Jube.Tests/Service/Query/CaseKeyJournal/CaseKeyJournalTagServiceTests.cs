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
    public sealed class CaseKeyJournalTagServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseKeyJournalTagService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalTagService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
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

        private async Task<Guid> CreateArchiveAsync(DbContext dbContext, int modelId, params string[] tags)
        {
            var guid = Guid.NewGuid();
            var json = new JObject { ["payload"] = new JObject(), ["tag"] = new JArray(tags) };

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

        private async Task CreateCaseAsync(DbContext dbContext, Guid workflow, Guid transaction, string lockedUser,
            bool locked = true)
        {
            await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = transaction,
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = (byte)(locked ? 1 : 0),
                LockedUser = lockedUser,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
            });
        }

        private async Task ReplaceWorkflowRowWithAnotherModelAsync(DbContext dbContext, Guid workflow, int model)
        {
            var original = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow);

            await dbContext.CaseWorkflow.Where(w => w.Id == original.Id).Set(w => w.Deleted, (byte)1).UpdateAsync();

            original.Id = 0;
            original.EntityAnalysisModelId = model;
            original.Deleted = 0;
            createdWorkflowIds.Add(await dbContext.InsertWithInt32IdentityAsync(original));
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

        [Fact]
        public async Task OptionsListOnlyTheActiveTagsOfTheWorkflowsModelAlphabeticallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Zulu", "Alpha");
            await CreateTagAsync(dbContext, model, "Retired", false);
            await CreateTagAsync(dbContext, model, "Removed", deleted: true);
            var other = await CreateModelAsync(dbContext);
            await CreateTagAsync(dbContext, other, "Elsewhere");
            var service = await BuildServiceAsync(dbContext, Me);

            var options = await service.GetOptionsAsync(workflow);

            options.Tag.Should().Equal("Alpha", "Zulu");
        }

        [Fact]
        public async Task AddingAndRemovingChangesEverySelectedTransactionAndReportsBeforeAndAfterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha", "Beta", "Gamma");
            var first = await CreateArchiveAsync(dbContext, model, "Alpha", "Beta");
            var second = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow,
                Transactions = [first, second],
                Add = ["Gamma", "Alpha"],
                Remove = ["Beta"]
            });

            (await StoredTagsAsync(dbContext, first)).Should().Equal("Alpha", "Gamma");
            (await StoredTagsAsync(dbContext, second)).Should().Equal("Gamma", "Alpha");
            result.Skipped.Should().Be(0);
            result.Previous.Single(s => s.Transaction == first).Tag.Should().Equal("Alpha", "Beta");
            result.Previous.Single(s => s.Transaction == second).Tag.Should().BeEmpty();
            result.Current.Single(s => s.Transaction == first).Tag.Should().Equal("Alpha", "Gamma");
            result.Current.Single(s => s.Transaction == second).Tag.Should().Equal("Gamma", "Alpha");
        }

        [Fact]
        public async Task TheTagsAreRecordedInTheArchiveTagTableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var guid = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [guid], Add = ["Alpha"]
            });

            var rows = await dbContext.GetTable<Data.Poco.ArchiveTag>()
                .Where(t => t.EntityAnalysisModelInstanceEntryGuid == guid && (t.Deleted == null || t.Deleted == 0))
                .ToListAsync();
            rows.Select(r => r.Name).Should().Equal("Alpha");
            rows.Single().CreatedUser.Should().Be(Me);
        }

        [Fact]
        public async Task ATransactionThatAlreadyHasTheTagIsLeftUntouchedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var guid = await CreateArchiveAsync(dbContext, model, "Alpha");
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [guid], Add = ["Alpha"], Remove = ["Absent"]
            });

            result.Current.Single().Tag.Should().Equal("Alpha");
            (await dbContext.GetTable<Data.Poco.ArchiveTag>()
                .CountAsync(t => t.EntityAnalysisModelInstanceEntryGuid == guid)).Should().Be(0);
        }

        [Fact]
        public async Task RestoringThePreviousSetsUndoesATaggingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha", "Beta");
            var first = await CreateArchiveAsync(dbContext, model, "Beta");
            var second = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            var done = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [first, second], Add = ["Alpha"], Remove = ["Beta"]
            });

            var undone = await service.RestoreAsync(new CaseKeyJournalTagRestoreRequestDto
            {
                CaseWorkflowGuid = workflow, Sets = done.Previous
            });

            (await StoredTagsAsync(dbContext, first)).Should().Equal("Beta");
            (await StoredTagsAsync(dbContext, second)).Should().BeEmpty();
            undone.Previous.Single(s => s.Transaction == first).Tag.Should().Equal("Alpha");
        }

        [Fact]
        public async Task ATransactionOfAnotherModelIsSkippedAndNeverChangedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var mine = await CreateArchiveAsync(dbContext, model);
            var otherModel = await CreateModelAsync(dbContext);
            var theirs = await CreateArchiveAsync(dbContext, otherModel);
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [mine, theirs, Guid.NewGuid()], Add = ["Alpha"]
            });

            result.Skipped.Should().Be(2);
            result.Current.Select(s => s.Transaction).Should().Equal(mine);
            (await StoredTagsAsync(dbContext, theirs)).Should().BeEmpty();

            var restored = await service.RestoreAsync(new CaseKeyJournalTagRestoreRequestDto
            {
                CaseWorkflowGuid = workflow,
                Sets = [new CaseKeyJournalTagSetDto { Transaction = theirs, Tag = ["Alpha"] }]
            });

            restored.Skipped.Should().Be(1);
            (await StoredTagsAsync(dbContext, theirs)).Should().BeEmpty();
        }

        [Fact]
        public async Task ACaseLockedToAnotherUserIsLeftAloneWhileMyOwnLockAndNoLockAreTaggedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var free = await CreateArchiveAsync(dbContext, model);
            var mine = await CreateArchiveAsync(dbContext, model);
            var theirs = await CreateArchiveAsync(dbContext, model);
            var released = await CreateArchiveAsync(dbContext, model);
            await CreateCaseAsync(dbContext, workflow, mine, Me);
            await CreateCaseAsync(dbContext, workflow, theirs, fx.Seed.UserTenantB);
            await CreateCaseAsync(dbContext, workflow, released, fx.Seed.UserTenantB, locked: false);
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [free, mine, theirs, released], Add = ["Alpha"]
            });

            result.Locked.Should().Be(1);
            result.Skipped.Should().Be(0);
            result.Current.Select(s => s.Transaction).Should().BeEquivalentTo([free, mine, released]);
            (await StoredTagsAsync(dbContext, theirs)).Should().BeEmpty();
            (await StoredTagsAsync(dbContext, mine)).Should().Equal("Alpha");
            (await StoredTagsAsync(dbContext, released)).Should().Equal("Alpha");

            var restored = await service.RestoreAsync(new CaseKeyJournalTagRestoreRequestDto
            {
                CaseWorkflowGuid = workflow,
                Sets = [new CaseKeyJournalTagSetDto { Transaction = theirs, Tag = ["Alpha"] }]
            });

            restored.Locked.Should().Be(1);
            (await StoredTagsAsync(dbContext, theirs)).Should().BeEmpty();
        }

        [Fact]
        public async Task AWorkflowRowThatWasReplacedWithAnotherModelTagsTheLiveRowsTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, oldModel) = await SetupAsync(dbContext);
            var newModel = await CreateModelAsync(dbContext);
            await CreateTagAsync(dbContext, newModel, "Alpha");
            await ReplaceWorkflowRowWithAnotherModelAsync(dbContext, workflow, newModel);
            var old = await CreateArchiveAsync(dbContext, oldModel);
            var live = await CreateArchiveAsync(dbContext, newModel);
            var service = await BuildServiceAsync(dbContext, Me);

            var options = await service.GetOptionsAsync(workflow);
            var result = await service.UpdateAsync(new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [old, live], Add = ["Alpha"]
            });

            options.Tag.Should().Equal("Alpha");
            result.Skipped.Should().Be(1);
            (await StoredTagsAsync(dbContext, live)).Should().Equal("Alpha");
            (await StoredTagsAsync(dbContext, old)).Should().BeEmpty();
        }

        [Fact]
        public async Task ATagTheModelDoesNotAllowIsRefusedAndNothingChangesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            await CreateTagAsync(dbContext, model, "Retired", false);
            var guid = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            foreach (var unknown in new[] { "Invented", "Retired" })
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(
                    new CaseKeyJournalTagRequestDto
                    {
                        CaseWorkflowGuid = workflow, Transactions = [guid], Add = ["Alpha", unknown]
                    }));

                ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("TagUnknown");
            }

            (await StoredTagsAsync(dbContext, guid)).Should().BeEmpty();
        }

        [Fact]
        public async Task TheRequestIsValidatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var guid = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            async Task<string[]> CodesAsync(CaseKeyJournalTagRequestDto request)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(request));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            (await CodesAsync(new CaseKeyJournalTagRequestDto
                { CaseWorkflowGuid = workflow, Transactions = [], Add = ["Alpha"] }))
                .Should().Contain("TagTransactionsInvalid");
            (await CodesAsync(new CaseKeyJournalTagRequestDto
                { CaseWorkflowGuid = workflow, Transactions = [guid, guid], Add = ["Alpha"] }))
                .Should().Contain("TagTransactionsInvalid");
            (await CodesAsync(new CaseKeyJournalTagRequestDto
                { CaseWorkflowGuid = workflow, Transactions = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToArray(), Add = ["Alpha"] }))
                .Should().Contain("TagTransactionsInvalid");
            (await CodesAsync(new CaseKeyJournalTagRequestDto { CaseWorkflowGuid = workflow, Transactions = [guid] }))
                .Should().Contain("TagChangeRequired");
            (await CodesAsync(new CaseKeyJournalTagRequestDto
                { CaseWorkflowGuid = workflow, Transactions = [guid], Add = [" "] }))
                .Should().Contain("TagNameInvalid");
            (await CodesAsync(new CaseKeyJournalTagRequestDto
                { CaseWorkflowGuid = workflow, Transactions = [guid], Remove = [new string('x', 257)] }))
                .Should().Contain("TagNameInvalid");
            (await CodesAsync(new CaseKeyJournalTagRequestDto
                {
                    CaseWorkflowGuid = workflow, Transactions = [guid],
                    Remove = Enumerable.Range(0, 21).Select(i => $"T{i}").ToArray()
                })).Should().Contain("TagLimitInvalid");
        }

        [Fact]
        public async Task TheRestoreRequestIsValidatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext);
            var guid = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);
            var set = new CaseKeyJournalTagSetDto { Transaction = guid, Tag = ["Alpha"] };

            async Task<string[]> CodesAsync(params CaseKeyJournalTagSetDto[] sets)
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.RestoreAsync(
                    new CaseKeyJournalTagRestoreRequestDto { CaseWorkflowGuid = workflow, Sets = sets }));

                return ex.Result.Errors.Select(e => e.ErrorCode).ToArray();
            }

            (await CodesAsync()).Should().Contain("TagTransactionsInvalid");
            (await CodesAsync(set, set)).Should().Contain("TagTransactionsInvalid");
            (await CodesAsync(new CaseKeyJournalTagSetDto
                { Transaction = guid, Tag = Enumerable.Range(0, 21).Select(i => $"T{i}").ToArray() }))
                .Should().Contain("TagLimitInvalid");
        }

        [Fact]
        public async Task ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, model) = await SetupAsync(dbContext, "Alpha");
            var hidden = await CreateWorkflowAsync(dbContext, model, grantRoleToUser: false);
            var guid = await CreateArchiveAsync(dbContext, model);
            var request = new CaseKeyJournalTagRequestDto
            {
                CaseWorkflowGuid = workflow, Transactions = [guid], Add = ["Alpha"]
            };
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.UpdateAsync(request));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.GetOptionsAsync(workflow));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.RestoreAsync(
                new CaseKeyJournalTagRestoreRequestDto { CaseWorkflowGuid = workflow }));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.GetOptionsAsync(hidden));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.UpdateAsync(
                new CaseKeyJournalTagRequestDto { CaseWorkflowGuid = hidden, Transactions = [guid], Add = ["Alpha"] }));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.UpdateAsync(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.RestoreAsync(null));

            var ex = await Record.ExceptionAsync(() => outsider.UpdateAsync(request));
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
            (await StoredTagsAsync(dbContext, guid)).Should().BeEmpty();
        }
    }
}
