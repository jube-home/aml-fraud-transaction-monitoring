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
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseWorkflowFormEntryByCaseKeyValue;
using Jube.Service.Query.CaseWorkflowFormEntryByCaseKeyValue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseWorkflowFormEntryByCaseKeyValue.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.CaseWorkflowFormEntryByCaseKeyValue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFormEntryByCaseKeyValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowFormIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdEntryIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseWorkflowFormEntry.Where(w => createdEntryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => createdCaseWorkflowFormIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<CaseWorkflowFormEntryByCaseKeyValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowFormEntryByCaseKeyValueService.CreateAsync(dbContext, userName,
                log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private static string NewKey() => $"{DatabaseFixture.Prefix}Key{Guid.NewGuid():N}"[..40];

        private async Task<Fixture> CreateEntryAsync(DbContext dbContext, string tenantUser, string? workflowRoleUser,
            string? statusRoleUser, string key, string value, byte? workflowDeleted = 0, byte? statusDeleted = 0,
            byte? modelDeleted = 0, string? createdUser = null)
        {
            var repository = new Data.Repository.EntityAnalysisModelRepository(dbContext, tenantUser);
            var model = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = modelDeleted,
            });
            createdModelIds.Add(model.Id);

            var caseWorkflowGuid = Guid.NewGuid();
            var caseWorkflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = tenantUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(caseWorkflowId);

            var caseWorkflowStatusGuid = Guid.NewGuid();
            var caseWorkflowStatusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowStatusGuid,
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = statusDeleted,
                Priority = 1,
                CreatedUser = tenantUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(caseWorkflowStatusId);

            if (workflowRoleUser != null)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
                {
                    CaseWorkflowGuid = caseWorkflowGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleGuidAsync(dbContext, workflowRoleUser),
                    CreatedUser = tenantUser,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }

            if (statusRoleUser != null)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
                {
                    CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleGuidAsync(dbContext, statusRoleUser),
                    CreatedUser = tenantUser,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = 0,
                });
            }

            var formName = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..40];
            var formId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
            {
                Name = formName,
                Guid = Guid.NewGuid(),
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Html = "<div></div>",
                CreatedUser = tenantUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowFormIds.Add(formId);

            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                CaseKey = key,
                CaseKeyValue = value,
                Json = "{}",
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseIds.Add(caseId);

            var entryCreated = DateTime.UtcNow;
            var entryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFormEntry
            {
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = key,
                CaseKeyValue = value,
                CreatedUser = createdUser ?? tenantUser,
                CreatedDate = entryCreated,
            });
            createdEntryIds.Add(entryId);

            return new Fixture(entryId, caseId, formName, entryCreated);
        }

        private static Task<Guid> RoleGuidAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        private Task<Fixture> CreateVisibleToUserAAsync(DbContext dbContext, string key, string value) =>
            CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserWithPermission,
                fx.Seed.UserWithPermission, key, value);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowFormEntryByCaseKeyValueService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

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
        public async Task ForbiddenUserNeverReachesTheQueryEvenWhenMatchingDataExistsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserWithoutPermission, fx.Seed.UserWithoutPermission,
                fx.Seed.UserWithoutPermission, key, "v");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(key, "v"));
        }

        [Fact]
        public async Task GetAsyncReturnsExactlyMappedRowForMatchingCaseKeyValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            var created = await CreateVisibleToUserAAsync(dbContext, key, "acc-1");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "acc-1");

            var row = result.Should().ContainSingle().Subject;
            row.Id.Should().Be(created.EntryId);
            row.CaseId.Should().Be(created.CaseId);
            row.Name.Should().Be(created.FormName);
            row.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            row.CreatedDate.UtcDateTime.Should().Be(
                new DateTime(created.CreatedDate.Ticks - created.CreatedDate.Ticks % 10, DateTimeKind.Utc));
            row.CreatedDate.Offset.Should().Be(TimeSpan.Zero);
            row.ResponseStatusId.Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyListWhenNothingMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(NewKey(), "nothing");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncDoesNotReturnRowsForADifferentKeyOrValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateVisibleToUserAAsync(dbContext, key, "acc-1");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "acc-2")).Should().BeEmpty();
            (await service.GetAsync(NewKey(), "acc-1")).Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncOrdersByCaseIdDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            var first = await CreateVisibleToUserAAsync(dbContext, key, "acc-1");
            var second = await CreateVisibleToUserAAsync(dbContext, key, "acc-1");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(key, "acc-1");

            result.Select(r => r.Id).Should().Equal(second.EntryId, first.EntryId);
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserTenantB, fx.Seed.UserTenantB, fx.Seed.UserTenantB, key,
                "shared");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "shared")).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantBDataIsInvisibleToTenantAEvenWhenTenantAUsersRoleIsGrantedOnTenantBWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserTenantB, fx.Seed.UserWithPermission,
                fx.Seed.UserWithPermission, key, "shared");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "shared")).Should().BeEmpty();
        }

        [Fact]
        public async Task TenantADataIsInvisibleToTenantBAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateVisibleToUserAAsync(dbContext, key, "shared");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.GetAsync(key, "shared")).Should().BeEmpty();
        }

        [Fact]
        public async Task EachTenantSeesOnlyItsOwnRowsForTheSameKeyAndValueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            var a = await CreateVisibleToUserAAsync(dbContext, key, "shared");
            var b = await CreateEntryAsync(dbContext, fx.Seed.UserTenantB, fx.Seed.UserTenantB,
                fx.Seed.UserTenantB, key, "shared");

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(key, "shared")).Select(r => r.Id).Should().Equal(a.EntryId);
            (await serviceB.GetAsync(key, "shared")).Select(r => r.Id).Should().Equal(b.EntryId);
        }

        [Fact]
        public async Task RowIsHiddenWhenCallersRoleHoldsNoCaseWorkflowRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, null, fx.Seed.UserWithPermission, key, "v");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "v")).Should().BeEmpty();
        }

        [Fact]
        public async Task RowIsHiddenWhenCallersRoleHoldsNoCaseWorkflowStatusRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserWithPermission, null, key, "v");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "v")).Should().BeEmpty();
        }

        [Fact]
        public async Task RowIsHiddenWhenWorkflowRoleBelongsToAnotherRoleInTheSameTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserWithoutPermission,
                fx.Seed.UserWithPermission, key, "v");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "v")).Should().BeEmpty();
        }

        [Theory]
        [InlineData(1, 0, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, 0, 1)]
        public async Task RowIsHiddenWhenWorkflowStatusOrModelIsSoftDeletedAsync(byte workflowDeleted,
            byte statusDeleted, byte modelDeleted)
        {
            await using var dbContext = fx.GetDbContext();
            var key = NewKey();
            await CreateEntryAsync(dbContext, fx.Seed.UserWithPermission, fx.Seed.UserWithPermission,
                fx.Seed.UserWithPermission, key, "v", workflowDeleted, statusDeleted, modelDeleted);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(key, "v")).Should().BeEmpty();
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
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync(NewKey(), "v");

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var bus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var forbidden = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, serviceChangeBus: bus);

            await service.GetAsync(NewKey(), "v");
            await Assert.ThrowsAsync<ForbiddenException>(() => forbidden.GetAsync("k", "v"));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniqueToolName()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().Contain("CaseWorkflowFormEntryByCaseKeyValueGet");
        }
    }
}