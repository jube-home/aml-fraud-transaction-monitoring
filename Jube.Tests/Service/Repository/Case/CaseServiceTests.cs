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
using Jube.Dto.Repository.Case;
using Jube.Service.Exceptions.Repository.Case;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.Case;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;
using EngineJsonSerializationHelper = Jube.Engine.Helpers.JsonSerializationHelper;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Repository.Case
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly EngineJsonSerializationHelper jsonSerializationHelper = new();

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<long> createdArchiveIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseEvent>()
                .Where(w => w.CaseId != null && createdCaseIds.Contains(w.CaseId.Value)).DeleteAsync();
            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
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
            await dbContext.Archive.Where(w => createdArchiveIds.Contains(w.Id)).DeleteAsync();
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

        private static Task<CaseService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), dynamicEnvironment,
                jsonSerializationHelper, auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
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
            CreateWorkflowWithRoleAsync(DbContext dbContext, string userName, byte enableNotification = 0,
                byte enableHttpEndpoint = 0, byte priority = 1)
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
                Priority = priority,
                EnableNotification = enableNotification,
                EnableHttpEndpoint = enableHttpEndpoint,
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

        private async Task CreateArchiveAsync(DbContext dbContext, int modelId, string caseKey,
            string caseKeyValue)
        {
            var guid = Guid.NewGuid();
            var json = new JObject
            {
                ["payload"] = new JObject
                {
                    [caseKey] = caseKeyValue,
                },
            }.ToString(Newtonsoft.Json.Formatting.None);

            var id = await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.Archive
            {
                Json = json,
                EntityAnalysisModelInstanceEntryGuid = guid,
                EntityAnalysisModelId = modelId,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdArchiveIds.Add(id);
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
        public async Task GetWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var act = () => service.GetAsync();
            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var caseA = await serviceA.InsertAsync(new CaseDto
            {
                Id = 1,
                CaseWorkflowStatusGuid = statusGuid,
                ClosedStatusId = 0,
                Rating = 1,
                Payload = "{}",
            });
            createdCaseIds.Add(caseA.Id);
            await dbContext.Case.Where(w => w.Id == caseA.Id).Set(s => s.CaseWorkflowGuid, workflowGuid)
                .UpdateAsync();

            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var casesForB = await serviceB.GetAsync();
            casesForB.Should().NotContain(c => c.Id == caseA.Id);

            var casesForA = await serviceA.GetAsync();
            casesForA.Should().Contain(c => c.Id == caseA.Id);
        }

        [Fact]
        public async Task InsertPersistsAndDropsIdentityFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var busSpy = new CapturingBus();
            var (_, statusGuid, _) = await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: busSpy);

            var saved = await service.InsertAsync(new CaseDto
            {
                Id = 1,
                CaseWorkflowStatusGuid = statusGuid,
                ClosedStatusId = 0,
                Rating = 3,
                Payload = "{}",
                LockedUser = "attacker-supplied-ignored",
            });
            createdCaseIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            busSpy.Published.Should().ContainSingle(e => e.EntityId == saved.Id && e.Kind == ServiceChangeKind.Created);
        }

        [Fact]
        public async Task InsertWithInvalidRatingThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseDto
            {
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                ClosedStatusId = 0,
                Rating = 9,
                Payload = "{}",
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task CreateFromCaseKeyValueRaisesCaseFromArchiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var busSpy = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: busSpy);

            var created = await service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            });
            created = created.Required();

            created.Should().NotBeNull();
            createdCaseIds.Add(created.Id);
            created.CaseKeyValue.Should().Be(caseKeyValue);
            busSpy.Published.Should().ContainSingle(e => e.EntityId == created.Id);
        }

        [Fact]
        public async Task CreateFromCaseKeyValueWithExistingOpenCaseThrowsConflictAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var createCaseDto = new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            };

            var first = await service.CreateFromCaseKeyValueAsync(createCaseDto);
            createdCaseIds.Add(first.Required().Id);

            var act = () => service.CreateFromCaseKeyValueAsync(createCaseDto);
            await act.Should().ThrowAsync<ConflictException>();
        }

        [Fact]
        public async Task CreateFromCaseKeyValueWithUnknownWorkflowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CaseKey = "account",
                CaseKeyValue = "does-not-matter",
            });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task CreateFromCaseKeyValueWithNoMatchingArchiveThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, _) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var act = () => service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = "account",
                CaseKeyValue = $"no-archive-{Guid.NewGuid():N}",
            });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateWithUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.UpdateAsync(new CaseDto
            {
                Id = 2_000_000_000,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                ClosedStatusId = 0,
                Rating = 1,
                Payload = "{}",
            });

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateLockingAnUnlockedCaseSetsLockedDateAndUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            });
            created = created.Required();
            createdCaseIds.Add(created.Id);
            created.Locked.Should().BeFalse();

            var updated = await service.UpdateAsync(new CaseDto
            {
                Id = created.Id,
                CaseWorkflowStatusGuid = created.CaseWorkflowStatusGuid,
                ClosedStatusId = created.ClosedStatusId,
                Locked = true,
                LockedUser = fx.Seed.UserWithPermission,
                Rating = created.Rating == 0 ? (byte)1 : created.Rating,
                Payload = "{}",
            });

            updated.Locked.Should().Be(1);
            updated.LockedDate.Should().NotBeNull();

            var events = await dbContext.CaseEvent.Where(w => w.CaseId == created.Id).ToListAsync();
            events.Should().Contain(e => e.CaseEventTypeId == 6 && e.CaseKeyValue == caseKeyValue);
        }

        [Fact]
        public async Task UpdateUnlockingALockedCaseClearsLockedDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            });
            created = created.Required();
            createdCaseIds.Add(created.Id);

            var locked = await service.UpdateAsync(new CaseDto
            {
                Id = created.Id,
                CaseWorkflowStatusGuid = created.CaseWorkflowStatusGuid,
                ClosedStatusId = created.ClosedStatusId,
                Locked = true,
                LockedUser = fx.Seed.UserWithPermission,
                Rating = created.Rating == 0 ? (byte)1 : created.Rating,
                Payload = "{}",
            });
            locked.LockedDate.Should().NotBeNull();

            var unlocked = await service.UpdateAsync(new CaseDto
            {
                Id = created.Id,
                CaseWorkflowStatusGuid = created.CaseWorkflowStatusGuid,
                ClosedStatusId = created.ClosedStatusId,
                Locked = false,
                LockedUser = locked.LockedUser,
                Rating = locked.Rating.GetValueOrDefault(),
                Payload = "{}",
            });

            unlocked.Locked.Should().Be(0);
            unlocked.LockedDate.Should().BeNull();
        }

        [Fact]
        public async Task UpdateWithUnknownWorkflowStatusThrowsInvalidCaseWorkflowStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            });
            created = created.Required();
            createdCaseIds.Add(created.Id);

            var act = () => service.UpdateAsync(new CaseDto
            {
                Id = created.Id,
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                ClosedStatusId = created.ClosedStatusId,
                Rating = 1,
                Payload = "{}",
            });

            await act.Should().ThrowAsync<InvalidCaseWorkflowStatusException>();
        }

        [Fact]
        public async Task UpdateChangingRatingWritesCaseEventAndPublishesChangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflowGuid, statusGuid, modelId) =
                await CreateWorkflowWithRoleAsync(dbContext, fx.Seed.UserWithPermission);
            var caseKey = "account";
            var caseKeyValue = $"acc-{Guid.NewGuid():N}"[..20];
            await CreateArchiveAsync(dbContext, modelId, caseKey, caseKeyValue);

            var busSpy = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: busSpy);
            var created = await service.CreateFromCaseKeyValueAsync(new CreateCaseDto
            {
                CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid,
                CaseKey = caseKey,
                CaseKeyValue = caseKeyValue,
            });
            created = created.Required();
            createdCaseIds.Add(created.Id);
            busSpy.Published.Clear();

            var updated = await service.UpdateAsync(new CaseDto
            {
                Id = created.Id,
                CaseWorkflowStatusGuid = created.CaseWorkflowStatusGuid,
                ClosedStatusId = created.ClosedStatusId,
                Rating = 4,
                Payload = "{}",
            });

            updated.Rating.Should().Be(4);
            var events = await dbContext.CaseEvent.Where(w => w.CaseId == created.Id).ToListAsync();
            events.Should().Contain(e => e.CaseEventTypeId == 11);
            busSpy.Published.Should().ContainSingle(e => e.EntityId == created.Id);
        }

        [Fact]
        public async Task UpdateDoesNotPublishChangeOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var busSpy = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: busSpy);

            var act = () => service.UpdateAsync(new CaseDto
            {
                Id = 1,
                CaseWorkflowStatusGuid = Guid.Empty,
                ClosedStatusId = 0,
                Rating = 1,
                Payload = "{}",
            });

            await act.Should().ThrowAsync<DtoValidationException>();
            busSpy.Published.Should().BeEmpty();
        }
    }
}