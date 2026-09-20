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
using Jube.Dto.Repository.CaseNote;
using Jube.Service.Exceptions.Repository.CaseNote;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseNote;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.CaseNote
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseNoteServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private readonly List<int> createdCaseNoteIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowActionIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseNote.Where(w => createdCaseNoteIds.Contains(w.Id)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowActionGuids1 = dbContext.CaseWorkflowAction
                .Where(a => createdCaseWorkflowActionIds.Contains(a.Id))
                .Select(a => a.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowActionRole>()
                .Where(w => caseWorkflowActionGuids1.Contains(w.CaseWorkflowActionGuid)).DeleteAsync();
            await dbContext.CaseWorkflowAction.Where(w => createdCaseWorkflowActionIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids2 = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids2.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids3 = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids3.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ??
                                       "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;",
            });

        private static Task<CaseNoteService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseNoteService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment,
                jsonSerializationHelper, auditLog ?? TestLog.NoOp);
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
            CreateWorkflowWithRoleAsync(DbContext dbContext, string userName)
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

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

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

        private async Task<int> CreateWorkflowActionAsync(DbContext dbContext, string userName, Guid caseWorkflowGuid,
            byte enableNotification = 0, byte enableHttpEndpoint = 0)
        {
            var caseWorkflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == caseWorkflowGuid)
                .Select(w => w.Id).FirstAsync();

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            var actionGuid = Guid.NewGuid();
            var actionId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowAction
            {
                Name = $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40],
                Guid = actionGuid,
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableNotification = enableNotification,
                EnableHttpEndpoint = enableHttpEndpoint,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowActionIds.Add(actionId);

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowActionRole
            {
                CaseWorkflowActionGuid = actionGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return actionId;
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
        public async Task InsertWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = 1,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task GetByCaseKeyValueWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.GetByCaseKeyValueAsync("account", "acc-1");
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task InsertWithBlankNoteThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "",
                ActionId = 1,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithZeroActionIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = 0,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithBlankPayloadThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = 1,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithUnknownCaseIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = 1,
                PriorityId = 1,
                CaseId = int.MaxValue,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertWithUnknownActionIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = int.MaxValue,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertForCaseInAnotherTenantThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);
            var actionId = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserTenantB, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = actionId,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertPersistsAndReturnsNoteWithServerAssignedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);
            var actionId = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(new CaseNoteDto
            {
                Note = "A note about this case.",
                ActionId = actionId,
                PriorityId = 2,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Note.Should().Be("A note about this case.");
            saved.ActionId.Should().Be(actionId);
            saved.PriorityId.Should().Be(2);
            saved.CaseId.Should().Be(caseId);
            saved.CaseKey.Should().Be("account");
            saved.CaseKeyValue.Should().Be(caseKeyValue);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.Payload.Should().BeNull();
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);
            var actionId = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = actionId,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(saved.Id);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("CaseNote");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "",
                ActionId = 1,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueReturnsNewestFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);
            var actionId = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(new CaseNoteDto
            {
                Note = "first",
                ActionId = actionId,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(first.Id);
            var second = await service.InsertAsync(new CaseNoteDto
            {
                Note = "second",
                ActionId = actionId,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(second.Id);

            var notes = await service.GetByCaseKeyValueAsync("account", caseKeyValue);

            notes.Should().HaveCount(2);
            notes[0].Note.Should().Be("second");
            notes[1].Note.Should().Be("first");
        }

        [Fact]
        public async Task GetByCaseKeyValueIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuidB, statusGuidB, _) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseIdB = await CreateCaseAsync(dbContext, workflowGuidB, statusGuidB,
                "account", caseKeyValue);
            var actionIdB = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserTenantB, workflowGuidB);

            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var noteB = await serviceB.InsertAsync(new CaseNoteDto
            {
                Note = "tenant b note",
                ActionId = actionIdB,
                PriorityId = 1,
                CaseId = caseIdB,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(noteB.Id);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var notesForA = await serviceA.GetByCaseKeyValueAsync("account", caseKeyValue);

            notesForA.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueWithNoMatchesReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var notes = await service.GetByCaseKeyValueAsync("account", $"no-such-{Guid.NewGuid():N}");
            notes.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new System.Threading.CancellationTokenSource();
            await cts.CancelAsync();
            var cancelledToken = cts.Token;
            var act = () => service.GetByCaseKeyValueAsync("account", "acc-1", cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);
            var act = () => service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = 1,
                PriorityId = 1,
                CaseId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
                Payload = "{}",
            });
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid,
                "account", caseKeyValue);
            var actionId = await CreateWorkflowActionAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: capturingAuditLog);
            var saved = await service.InsertAsync(new CaseNoteDto
            {
                Note = "note",
                ActionId = actionId,
                PriorityId = 1,
                CaseId = caseId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
                Payload = "{}",
            });
            createdCaseNoteIds.Add(saved.Id);

            capturingAuditLog.Entries.Should().ContainSingle();
        }
    }
}