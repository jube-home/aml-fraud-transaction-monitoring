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
    public sealed class CaseKeyJournalPinServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
        private readonly List<int> createdCaseIds = [];

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseKeyJournalPin.Where(w => createdCaseIds.Contains(w.CaseId)).DeleteAsync();

            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();

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

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();

            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseKeyJournalPinService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return CaseKeyJournalPinService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
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

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid workflow, string? lockedUser = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflow,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = (byte)(lockedUser is null ? 0 : 1),
                LockedUser = lockedUser,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
            });
            createdCaseIds.Add(id);

            return id;
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

        private async Task<(Guid Workflow, int Model, int Case)> SetupAsync(DbContext dbContext,
            string? lockedUser = null)
        {
            var model = await CreateModelAsync(dbContext);
            var workflow = await CreateWorkflowAsync(dbContext, model);

            return (workflow, model, await CreateCaseAsync(dbContext, workflow, lockedUser));
        }

        [Fact]
        public async Task PinningAndUnpinningIsSharedAndListedMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var first = await CreateArchiveAsync(dbContext, model);
            var second = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [first], Pinned = true
            });
            await Task.Delay(20);
            var both = await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [second], Pinned = true
            });

            both.Transactions.Should().Equal(second, first);

            var left = await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [second], Pinned = false
            });

            left.Transactions.Should().Equal(first);
            (await service.ListAsync(caseId)).Transactions.Should().Equal(first);

            var again = await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [second, first], Pinned = true
            });

            again.Transactions.Should().BeEquivalentTo([first, second]);
            (await dbContext.CaseKeyJournalPin.CountAsync(p => p.CaseId == caseId)).Should().Be(2);
        }

        [Fact]
        public async Task AWorkflowRowThatWasReplacedWithAnotherModelPinsTheLiveRowsTransactionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, oldModel, caseId) = await SetupAsync(dbContext);
            var newModel = await CreateModelAsync(dbContext);
            await ReplaceWorkflowRowWithAnotherModelAsync(dbContext, workflow, newModel);
            var old = await CreateArchiveAsync(dbContext, oldModel);
            var live = await CreateArchiveAsync(dbContext, newModel);
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [old, live], Pinned = true
            });

            result.Transactions.Should().Equal(live);
        }

        [Fact]
        public async Task OnlyTransactionsOfTheWorkflowsModelCanBePinnedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var mine = await CreateArchiveAsync(dbContext, model);
            var theirs = await CreateArchiveAsync(dbContext, await CreateModelAsync(dbContext));
            var service = await BuildServiceAsync(dbContext, Me);

            var result = await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [mine, theirs, Guid.NewGuid()], Pinned = true
            });

            result.Transactions.Should().Equal(mine);
        }

        [Fact]
        public async Task ACaseHoldsNoMoreThanFiftyPinsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var guids = new List<Guid>();

            for (var i = 0; i < 51; i++)
            {
                guids.Add(await CreateArchiveAsync(dbContext, model));
            }

            var service = await BuildServiceAsync(dbContext, Me);
            await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = caseId, Transactions = [.. guids.Take(50)], Pinned = true
            });

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SetAsync(
                new CaseKeyJournalPinRequestDto { CaseId = caseId, Transactions = [guids[50]], Pinned = true }));

            ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("PinLimitInvalid");

            var many = await Assert.ThrowsAsync<DtoValidationException>(() => service.SetAsync(
                new CaseKeyJournalPinRequestDto
                {
                    CaseId = caseId, Transactions = [.. guids, Guid.NewGuid()], Pinned = true
                }));

            many.Result.Errors.Select(e => e.ErrorCode).Should().Contain("PinTransactionsInvalid");
            (await service.ListAsync(caseId)).Transactions.Should().HaveCount(50);
        }

        [Fact]
        public async Task TheTransactionListIsValidatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var guid = await CreateArchiveAsync(dbContext, model);
            var service = await BuildServiceAsync(dbContext, Me);

            foreach (var transactions in new[] { Array.Empty<Guid>(), new[] { guid, guid } })
            {
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.SetAsync(
                    new CaseKeyJournalPinRequestDto { CaseId = caseId, Transactions = transactions, Pinned = true }));

                ex.Result.Errors.Select(e => e.ErrorCode).Should().Contain("PinTransactionsInvalid");
            }
        }

        [Fact]
        public async Task ACaseLockedToAnotherUserCannotBeChangedButMyOwnLockCanAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, theirCase) = await SetupAsync(dbContext, fx.Seed.UserTenantB);
            var (_, ownModel, ownCase) = await SetupAsync(dbContext, Me);
            var theirs = await CreateArchiveAsync(dbContext, model);
            var mine = await CreateArchiveAsync(dbContext, ownModel);
            var service = await BuildServiceAsync(dbContext, Me);

            await Assert.ThrowsAsync<ConflictException>(() => service.SetAsync(
                new CaseKeyJournalPinRequestDto { CaseId = theirCase, Transactions = [theirs], Pinned = true }));

            (await service.ListAsync(theirCase)).Transactions.Should().BeEmpty();
            (await service.SetAsync(new CaseKeyJournalPinRequestDto
            {
                CaseId = ownCase, Transactions = [mine], Pinned = true
            })).Transactions.Should().Equal(mine);
        }

        [Fact]
        public async Task ThePermissionTheWorkflowRolesAndTheTenantAreCheckedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, model, caseId) = await SetupAsync(dbContext);
            var hiddenModel = await CreateModelAsync(dbContext);
            var hiddenWorkflow = await CreateWorkflowAsync(dbContext, hiddenModel, grantRoleToUser: false);
            var hiddenCase = await CreateCaseAsync(dbContext, hiddenWorkflow);
            var guid = await CreateArchiveAsync(dbContext, model);
            var request = new CaseKeyJournalPinRequestDto { CaseId = caseId, Transactions = [guid], Pinned = true };
            var denied = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var allowed = await BuildServiceAsync(dbContext, Me);
            var outsider = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<ForbiddenException>(() => denied.SetAsync(request));
            await Assert.ThrowsAsync<ForbiddenException>(() => denied.ListAsync(caseId));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.ListAsync(hiddenCase));
            await Assert.ThrowsAsync<NotFoundException>(() => allowed.ListAsync(int.MaxValue));
            await Assert.ThrowsAsync<ArgumentNullException>(() => allowed.SetAsync(null));

            var ex = await Record.ExceptionAsync(() => outsider.SetAsync(request));
            ex.Should().BeAssignableTo<Exception>().Which.GetType().Name.Should()
                .BeOneOf("NotFoundException", "ForbiddenException");
            (await allowed.ListAsync(caseId)).Transactions.Should().BeEmpty();
        }
    }
}
