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
using Jube.Dto.Repository.CaseWorkflowFormEntry;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormEntry;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormEntry;
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

namespace Jube.Test.Service.Repository.CaseWorkflowFormEntry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFormEntryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private readonly List<int> createdEntryIds = [];
        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowFormIds = [];
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
            await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>()
                // ReSharper disable once AccessToDisposedClosure
                .Where(w => dbContext.CaseWorkflowForm.Where(f => createdCaseWorkflowFormIds.Contains(f.Id))
                    .Select(f => f.Guid).Contains(w.CaseWorkflowFormGuid)).DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => createdCaseWorkflowFormIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                // ReSharper disable once AccessToDisposedClosure
                .Where(w => dbContext.CaseWorkflowStatus.Where(s => createdCaseWorkflowStatusIds.Contains(s.Id))
                    .Select(s => s.Guid).Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                // ReSharper disable once AccessToDisposedClosure
                .Where(w => dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                    .Select(s => s.Guid).Contains(w.CaseWorkflowGuid)).DeleteAsync();
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

        private static Task<CaseWorkflowFormEntryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowFormEntryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
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

        private async Task<int> CreateWorkflowFormAsync(DbContext dbContext, string userName, Guid caseWorkflowGuid,
            byte enableNotification = 0, byte enableHttpEndpoint = 0)
        {
            var caseWorkflowId = await dbContext.CaseWorkflow.Where(w => w.Guid == caseWorkflowGuid)
                .Select(w => w.Id).FirstAsync();

            var formId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
            {
                Name = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Html = "<div></div>",
                EnableNotification = enableNotification,
                EnableHttpEndpoint = enableHttpEndpoint,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowFormIds.Add(formId);

            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();
            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowFormRole
            {
                CaseWorkflowFormGuid = await dbContext.CaseWorkflowForm.Where(f => f.Id == formId)
                    .Select(f => f.Guid).FirstAsync(),
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });

            return formId;
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
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = 1,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
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
        public async Task InsertWithZeroCaseWorkflowFormIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = 1,
                CaseWorkflowFormId = 0,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithEmptyPayloadThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?>(),
                CaseId = 1,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithZeroCaseIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = 0,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
            });
            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithUnknownCaseIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = int.MaxValue,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertWithUnknownCaseWorkflowFormIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = caseId,
                CaseWorkflowFormId = int.MaxValue,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertForCaseInAnotherTenantThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserTenantB, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task InsertPersistsEntryAndValueRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["firstName"] = "Ada", ["age"] = "36" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.CaseId.Should().Be(caseId);
            saved.CaseWorkflowFormId.Should().Be(formId);
            saved.CaseKey.Should().Be("account");
            saved.CaseKeyValue.Should().Be(caseKeyValue);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.Payload.Should().BeNull();

            var values = await dbContext.GetTable<Data.Poco.CaseWorkflowFormEntryValue>()
                .Where(w => w.CaseWorkflowFormEntryId == saved.Id).ToListAsync();
            values.Should().HaveCount(2);
            values.Should().Contain(v => v.Name == "firstName" && v.Value == "Ada");
            values.Should().Contain(v => v.Name == "age" && v.Value == "36");
        }

        [Fact]
        public async Task InsertSkipsNullPayloadValuesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["firstName"] = "Ada", ["skipped"] = null },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(saved.Id);

            var values = await dbContext.GetTable<Data.Poco.CaseWorkflowFormEntryValue>()
                .Where(w => w.CaseWorkflowFormEntryId == saved.Id).ToListAsync();
            values.Should().ContainSingle();
            values[0].Name.Should().Be("firstName");
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(saved.Id);

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("CaseWorkflowFormEntry");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?>(),
                CaseId = 1,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
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
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "first" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(first.Id);
            var second = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "second" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(second.Id);

            var entries = await service.GetByCaseKeyValueAsync("account", caseKeyValue);

            entries.Should().HaveCount(2);
            entries[0].Id.Should().Be(second.Id);
            entries[1].Id.Should().Be(first.Id);
            entries.Should().OnlyContain(e => e.Payload == null);
        }

        [Fact]
        public async Task GetByCaseKeyValueIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuidB, statusGuidB, _) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserTenantB);
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            var caseIdB = await CreateCaseAsync(dbContext, workflowGuidB, statusGuidB, "account", caseKeyValue);
            var formIdB = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserTenantB, workflowGuidB);

            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var entryB = await serviceB.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = caseIdB,
                CaseWorkflowFormId = formIdB,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(entryB.Id);

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var entriesForA = await serviceA.GetByCaseKeyValueAsync("account", caseKeyValue);

            entriesForA.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueWithNoMatchesReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var entries = await service.GetByCaseKeyValueAsync("account", $"no-such-{Guid.NewGuid():N}");
            entries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCaseKeyValueHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new System.Threading.CancellationTokenSource();
            await cts.CancelAsync();
            // ReSharper disable once AccessToDisposedClosure
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
            var act = () => service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = 1,
                CaseWorkflowFormId = 1,
                CaseKey = "account",
                CaseKeyValue = "acc-1",
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
            var caseId = await CreateCaseAsync(dbContext, workflowGuid, statusGuid, "account", caseKeyValue);
            var formId = await CreateWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission, workflowGuid);

            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: capturingAuditLog);
            var saved = await service.InsertAsync(new CaseWorkflowFormEntryDto
            {
                Payload = new Dictionary<string, object?> { ["field"] = "value" },
                CaseId = caseId,
                CaseWorkflowFormId = formId,
                CaseKey = "account",
                CaseKeyValue = caseKeyValue,
            });
            createdEntryIds.Add(saved.Id);

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public void CatalogueRegistersExpectedToolNames()
        {
            var names = Jube.Service.Agent.ServiceToolCatalogue.ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().Contain("CaseWorkflowFormEntryInsert");
            names.Should().Contain("CaseWorkflowFormEntryListByCaseKeyValue");
        }
    }
}