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
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntryValue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormEntryValue;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowFormEntryValue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFormEntryValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdEntryIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowFormEntryValue>()
                .Where(w => w.CaseWorkflowFormEntryId != null &&
                            createdEntryIds.Contains(w.CaseWorkflowFormEntryId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowFormEntry.Where(w => createdEntryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids1 = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids1.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids2 = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids2.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowFormEntryValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowFormEntryValueService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new Data.Repository.EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
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

        private async Task<(Guid CaseWorkflowGuid, Guid CaseWorkflowStatusGuid, int ModelId)>
            CreateWorkflowWithRoleAsync(DbContext dbContext, string userName, bool grantRole = true)
        {
            var modelId = await CreateModelAsync(dbContext, userName);

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            var caseWorkflowGuid = Guid.NewGuid();
            var caseWorkflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowGuid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = userName,
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
                Deleted = 0,
                Priority = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(caseWorkflowStatusId);

            if (!grantRole)
            {
                return (caseWorkflowGuid, caseWorkflowStatusGuid, modelId);
            }

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return (caseWorkflowGuid, caseWorkflowStatusGuid, modelId);
        }

        private async Task<int> CreateCaseAsync(DbContext dbContext, Guid caseWorkflowGuid,
            Guid caseWorkflowStatusGuid, string caseKey, string caseKeyValue)
        {
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
                Json = "{}",
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseIds.Add(caseId);
            return caseId;
        }

        private async Task<int> CreateEntryWithValuesAsync(DbContext dbContext, string userName, int caseId,
            string caseKey, string caseKeyValue, params (string Name, string Value)[] values)
        {
            var entryId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowFormEntry
            {
                CaseId = caseId,
                CaseWorkflowFormId = 1,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdEntryIds.Add(entryId);

            foreach (var (name, value) in values)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowFormEntryValue
                {
                    CaseWorkflowFormEntryId = entryId,
                    Name = name,
                    Value = value,
                });
            }

            return entryId;
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, null);
            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task CreateWithUnknownTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            // ReSharper disable once AccessToDisposedClosure
            var act = () => BuildServiceAsync(dbContext, fx.Seed.UnknownUser);
            await act.Should().ThrowAsync<NotAuthenticatedException>();
        }

        [Fact]
        public async Task GetByCaseWorkflowFormEntryIdWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.GetByCaseWorkflowFormEntryIdAsync(1);
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ListReturnsValuesNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var entryId = await CreateEntryWithValuesAsync(dbContext, fx.Seed.UserWithPermission, caseId, "account",
                caseKeyValue, ("firstName", "Alice"), ("lastName", "Smith"));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseWorkflowFormEntryIdAsync(entryId);

            result.Should().HaveCount(2);
            result.Select(r => r.Name).Should().Contain(["firstName", "lastName"]);
            result[0].Id.Should().BeGreaterThan(result[1].Id);
        }

        [Fact]
        public async Task ListRequiresBothCaseWorkflowAndCaseWorkflowStatusRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission, grantRole: false);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var entryId = await CreateEntryWithValuesAsync(dbContext, fx.Seed.UserWithPermission, caseId, "account",
                caseKeyValue, ("field", "value"));

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseWorkflowFormEntryIdAsync(entryId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var entryId = await CreateEntryWithValuesAsync(dbContext, fx.Seed.UserWithPermission, caseId, "account",
                caseKeyValue, ("field", "value"));

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var result = await otherTenantService.GetByCaseWorkflowFormEntryIdAsync(entryId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ListForUnknownEntryIdReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var result = await service.GetByCaseWorkflowFormEntryIdAsync(int.MaxValue);
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();
            var cancelledToken = cts.Token;
            var act = () => service.GetByCaseWorkflowFormEntryIdAsync(1, cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var entryId = await CreateEntryWithValuesAsync(dbContext, fx.Seed.UserWithPermission, caseId, "account",
                caseKeyValue, ("field", "value"));

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: bus);
            await service.GetByCaseWorkflowFormEntryIdAsync(entryId);

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);
            var act = () => service.GetByCaseWorkflowFormEntryIdAsync(1);
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task ListLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var entryId = await CreateEntryWithValuesAsync(dbContext, fx.Seed.UserWithPermission, caseId, "account",
                caseKeyValue, ("field", "value"));

            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);
            await service.GetByCaseWorkflowFormEntryIdAsync(entryId);

            capturingAuditLog.Entries.Should().HaveCount(1);
        }

        [Fact]
        public void CatalogueRegistersExpectedToolNames()
        {
            var names = Jube.Service.Agent.ServiceToolCatalogue.ServiceToolCatalogue.All
                .Select(t => t.Name).ToList();
            names.Should().Contain("CaseWorkflowFormEntryValueListByCaseWorkflowFormEntryId");
        }
    }
}