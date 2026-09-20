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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseEventByCaseKeyValue;
using Jube.Service.Observability;
using Jube.Service.Query.CaseEventByCaseKeyValue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.CaseEventByCaseKeyValue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseEventByCaseKeyValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseEvent>()
                .Where(w => w.CaseId != null && createdCaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseEventByCaseKeyValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseEventByCaseKeyValueService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static DateTimeOffset ExpectedUtc(DateTime created) => new(
            new DateTime(created.Ticks - created.Ticks % 10, DateTimeKind.Utc), TimeSpan.Zero);

        private static string NewKey() => $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..30];

        private async Task<(Guid WorkflowGuid, Guid StatusGuid)> CreateWorkflowAsync(DbContext dbContext,
            string modelOwner, string[] workflowRoleUsers, string[] statusRoleUsers, byte workflowDeleted = 0,
            byte statusDeleted = 0)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, modelOwner);
            var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
            });
            createdModelIds.Add(model.Id);

            var workflowGuid = Guid.NewGuid();
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = modelOwner,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(workflowId);

            var statusGuid = Guid.NewGuid();
            var statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                Active = 1,
                Locked = 0,
                Deleted = statusDeleted,
                Priority = 1,
                CreatedUser = modelOwner,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(statusId);

            foreach (var user in workflowRoleUsers)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
                {
                    CaseWorkflowGuid = workflowGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleOfAsync(dbContext, user),
                    CreatedUser = modelOwner,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }

            foreach (var user in statusRoleUsers)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
                {
                    CaseWorkflowStatusGuid = statusGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleOfAsync(dbContext, user),
                    CreatedUser = modelOwner,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }

            return (workflowGuid, statusGuid);
        }

        private static Task<Guid> RoleOfAsync(DbContext dbContext, string userName) => dbContext.UserRegistry
            .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid workflowGuid, Guid statusGuid, string key,
            string value, params (int? TypeId, string? Before, string? After, string? User, DateTime Created)[] events)
        {
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = key,
                CaseKeyValue = value,
                CreatedDate = DateTime.UtcNow,
                Json = "{}",
            });
            createdCaseIds.Add(caseId);

            foreach (var e in events)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseEvent
                {
                    CaseId = caseId,
                    CaseEventTypeId = e.TypeId,
                    Before = e.Before,
                    After = e.After,
                    CreatedUser = e.User,
                    CreatedDate = e.Created,
                    CaseKey = key,
                    CaseKeyValue = value,
                });
            }

            return caseId;
        }

        private async Task<(string Key, string Value, int CaseId)> SeedVisibleCaseAsync(DbContext dbContext,
            string user, params (int? TypeId, string? Before, string? After, string? User, DateTime Created)[] events)
        {
            var (workflowGuid, statusGuid) = await CreateWorkflowAsync(dbContext, user, [user], [user]);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value, events);
            return (key, value, caseId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseEventByCaseKeyValueService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(),
                    TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));
            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
        }

        [Fact]
        public async Task GetAsyncReturnsSeededEventsNewestFirstWithExactMappingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var created1 = DateTime.UtcNow.AddMinutes(-10);
            var created2 = DateTime.UtcNow.AddMinutes(-5);
            var seeded = await SeedVisibleCaseAsync(dbContext, user,
                (8, "before-1", "after-1", "creator1", created1),
                (5, "before-2", "after-2", "creator2", created2));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(seeded.Key, seeded.Value);

            result.Should().HaveCount(2);
            result[0].Id.Should().BeGreaterThan(result[1].Id);
            result[0].CaseId.Should().Be(seeded.CaseId);
            result[0].CaseEventType.Should().Be("Closed");
            result[0].CreatedUser.Should().Be("creator2");
            result[0].Before.Should().Be("before-2");
            result[0].After.Should().Be("after-2");
            result[0].CreatedDate.Should().Be(ExpectedUtc(created2));
            result[1].CaseEventType.Should().Be("Workflow Change");
            result[1].CreatedUser.Should().Be("creator1");
            result[1].Before.Should().Be("before-1");
            result[1].After.Should().Be("after-1");
            result[1].CreatedDate.Should().Be(ExpectedUtc(created1));
        }

        [Theory]
        [InlineData(1, "Automatic Unlock")]
        [InlineData(2, "Skim Case")]
        [InlineData(3, "Automatic Lock")]
        [InlineData(4, "Full Case Fetch")]
        [InlineData(5, "Closed")]
        [InlineData(6, "Manual Lock")]
        [InlineData(7, "Diary")]
        [InlineData(8, "Workflow Change")]
        [InlineData(9, "Status Change")]
        [InlineData(10, "Diary Date Change")]
        [InlineData(11, "Rating Change")]
        [InlineData(12, "Suspend Closed")]
        [InlineData(13, "Suspend Open")]
        [InlineData(14, "Allocate Lock")]
        [InlineData(15, "Process Expired Case")]
        [InlineData(99, "Unknown")]
        [InlineData(null, "Unknown")]
        public async Task EventTypeIdIsMappedToDisplayNameAsync(int? typeId, string expected)
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedVisibleCaseAsync(dbContext, user, (typeId, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            var result = await service.GetAsync(seeded.Key, seeded.Value);

            result.Should().ContainSingle().Which.CaseEventType.Should().Be(expected);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyListForUnknownKeyValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(NewKey(), Guid.NewGuid().ToString("N"));

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncDoesNotReturnEventsOfAnotherCaseKeyValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var seeded = await SeedVisibleCaseAsync(dbContext, user, (5, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(seeded.Key, "other")).Should().BeEmpty();
            (await service.GetAsync("other", seeded.Value)).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBCaseIsInvisibleToTenantAUserEvenWhenTenantARoleIsGrantedItsWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userA = fx.Seed.UserWithPermission;
            var userB = fx.Seed.UserTenantB;
            var (workflowGuid, statusGuid) =
                await CreateWorkflowAsync(dbContext, userB, [userB, userA], [userB, userA]);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value,
                (5, null, null, userB, DateTime.UtcNow));

            var serviceA = await BuildServiceAsync(dbContext, userA);
            var serviceB = await BuildServiceAsync(dbContext, userB);

            (await serviceA.GetAsync(key, value)).Should().BeEmpty();
            (await serviceB.GetAsync(key, value)).Should().ContainSingle();
        }

        [Fact]
        public async Task SameKeyValueInTwoTenantsReturnsOnlyEachUsersOwnTenantEventsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userA = fx.Seed.UserWithPermission;
            var userB = fx.Seed.UserTenantB;
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");

            var (wfA, stA) = await CreateWorkflowAsync(dbContext, userA, [userA], [userA]);
            var caseA = await CreateCaseAsync(dbContext, wfA, stA, key, value, (5, null, null, userA, DateTime.UtcNow));
            var (wfB, stB) = await CreateWorkflowAsync(dbContext, userB, [userB], [userB]);
            var caseB = await CreateCaseAsync(dbContext, wfB, stB, key, value, (6, null, null, userB, DateTime.UtcNow));

            var serviceA = await BuildServiceAsync(dbContext, userA);
            var serviceB = await BuildServiceAsync(dbContext, userB);

            (await serviceA.GetAsync(key, value)).Should().ContainSingle().Which.CaseId.Should().Be(caseA);
            (await serviceB.GetAsync(key, value)).Should().ContainSingle().Which.CaseId.Should().Be(caseB);
        }

        [Fact]
        public async Task CaseIsInvisibleWhenUsersRoleIsNotGrantedTheWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var (workflowGuid, statusGuid) = await CreateWorkflowAsync(dbContext, user,
                [fx.Seed.UserWithoutPermission], [user]);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value,
                (5, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsInvisibleWhenUsersRoleIsNotGrantedTheStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var (workflowGuid, statusGuid) = await CreateWorkflowAsync(dbContext, user, [user],
                [fx.Seed.UserWithoutPermission]);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value,
                (5, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsInvisibleWhenWorkflowIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var (workflowGuid, statusGuid) = await CreateWorkflowAsync(dbContext, user, [user], [user],
                workflowDeleted: 1);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value,
                (5, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsInvisibleWhenStatusIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var (workflowGuid, statusGuid) = await CreateWorkflowAsync(dbContext, user, [user], [user],
                statusDeleted: 1);
            var key = NewKey();
            var value = Guid.NewGuid().ToString("N");
            await CreateCaseAsync(dbContext, workflowGuid, statusGuid, key, value,
                (5, null, null, user, DateTime.UtcNow));
            var service = await BuildServiceAsync(dbContext, user);

            (await service.GetAsync(key, value)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync("k", "v", cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync("k", "v"));

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.GetAsync("k", "v");

            var span = activities.Should().ContainSingle(a => a.OperationName == "CaseEventByCaseKeyValue.Get").Subject;
            span.GetTagItem("jube.outcome").Should().Be("ok");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync("k", "v");

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync("k", "v");
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.GetAsync("k", "v"));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("CaseEventByCaseKeyValueGet");
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("k", "v"));

                var frenchLocalizer = localizers.Create(typeof(CaseEventByCaseKeyValueResources));
                ex.Message.Should().Be(frenchLocalizer[CaseEventByCaseKeyValueResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}