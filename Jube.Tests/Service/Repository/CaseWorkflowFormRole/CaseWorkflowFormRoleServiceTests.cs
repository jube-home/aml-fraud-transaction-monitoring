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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowFormRole;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseWorkflowFormRole;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFormRole;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowFormRole
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFormRoleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdFormIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var formGuids = await dbContext.CaseWorkflowForm
                .Where(w => createdFormIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>()
                .Where(w => formGuids.Contains(w.CaseWorkflowFormGuid)).DeleteAsync();
            await dbContext.CaseWorkflowForm.Where(w => createdFormIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowFormRoleService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowFormRoleService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
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

        private async Task<int> CreateWorkflowAsync(DbContext dbContext, int entityAnalysisModelId,
            string createdUser)
        {
            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = entityAnalysisModelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdWorkflowIds.Add(workflowId);
            return workflowId;
        }

        private async Task<Guid> CreateFormAsync(DbContext dbContext, int caseWorkflowId, string createdUser)
        {
            var formGuid = Guid.NewGuid();
            var formId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowForm
            {
                Name = $"{DatabaseFixture.Prefix}Form{Guid.NewGuid():N}"[..40],
                Guid = formGuid,
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Html = "<div></div>",
                EnableHttpEndpoint = 0,
                EnableNotification = 0,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdFormIds.Add(formId);
            return formGuid;
        }

        private async Task<(int modelId, int workflowId, Guid formGuid)> CreateModelWorkflowFormAsync(
            DbContext dbContext, string userName)
        {
            var modelId = await CreateModelAsync(dbContext, userName);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, userName);
            var formGuid = await CreateFormAsync(dbContext, workflowId, userName);
            return (modelId, workflowId, formGuid);
        }

        private static Task<Guid> GetRoleRegistryGuidAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, userName));
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByCaseWorkflowFormGuidAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            }));

            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>()
                .AnyAsync(w => w.CaseWorkflowFormGuid == formGuid)).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1));
        }

        [Fact]
        public async Task InsertPersistsAndReturnsGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            saved.Id.Should().BeGreaterThan(0);
            saved.CaseWorkflowFormGuid.Should().Be(formGuid);
            saved.RoleRegistryGuid.Should().Be(roleRegistryGuid);
        }

        [Fact]
        public async Task InsertWithEmptyCaseWorkflowFormGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = Guid.Empty,
                RoleRegistryGuid = roleRegistryGuid,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithUnknownCaseWorkflowFormGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithCrossTenantCaseWorkflowFormGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuidB) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserTenantB);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuidB,
                RoleRegistryGuid = roleRegistryGuid,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = Guid.Empty,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuidB = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("CaseWorkflowFormRole");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = Guid.Empty,
                RoleRegistryGuid = Guid.Empty,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByCaseWorkflowFormGuidReturnsGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            var grants = await service.GetByCaseWorkflowFormGuidAsync(formGuid);

            grants.Should().ContainSingle();
            grants[0].RoleRegistryGuid.Should().Be(roleRegistryGuid);
        }

        [Fact]
        public async Task ListForUnknownFormGuidReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var grants = await service.GetByCaseWorkflowFormGuidAsync(Guid.NewGuid());

            grants.Should().BeEmpty();
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuidB) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserTenantB);
            var roleRegistryGuidB = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserTenantB);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            await serviceB.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuidB,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var grantsForA = await serviceA.GetByCaseWorkflowFormGuidAsync(formGuidB);

            grantsForA.Should().BeEmpty();
        }

        [Fact]
        public async Task ListExcludesDeletedGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(saved.Id);

            var grants = await service.GetByCaseWorkflowFormGuidAsync(formGuid);

            grants.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            await service.DeleteAsync(saved.Id);

            var row = await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>()
                .FirstAsync(w => w.Id == saved.Id);
            row.Deleted.Should().Be(1);

            bus.Published.Should().HaveCount(2);
            bus.Published[1].Kind.Should().Be(ServiceChangeKind.Deleted);
            bus.Published[1].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(int.MaxValue));
        }

        [Fact]
        public async Task DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuidB) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserTenantB);
            var roleRegistryGuidB = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserTenantB);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var savedB = await serviceB.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuidB,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceA.DeleteAsync(savedB.Id));
        }

        [Fact]
        public async Task DeleteDoesNotPublishChangeEventOnFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(int.MaxValue));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var cancelledToken = cts.Token;
            var act = () => service.GetByCaseWorkflowFormGuidAsync(Guid.NewGuid(), cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);

            var act = () => service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = Guid.NewGuid(),
                RoleRegistryGuid = Guid.NewGuid(),
            });
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);

            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);
            await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task ListLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.GetByCaseWorkflowFormGuidAsync(Guid.NewGuid());

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public void CatalogueListsUniqueCaseWorkflowFormRoleToolNames()
        {
            var tools = ServiceToolCatalogue.All;
            var names = tools.Where(t => t.Name.StartsWith("CaseWorkflowFormRole"))
                .Select(t => t.Name).ToList();

            names.Should().BeEquivalentTo([
                "CaseWorkflowFormRoleListByCaseWorkflowFormGuid",
                "CaseWorkflowFormRoleCreate",
                "CaseWorkflowFormRoleDelete",
            ]);
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var second = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            second.Id.Should().Be(first.Id);
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                w.CaseWorkflowFormGuid == formGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(first.Id);
            var again = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            again.Id.Should().NotBe(first.Id, "a soft-deleted grant must not block granting again");
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                w.CaseWorkflowFormGuid == formGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task RepeatedGrantAndRevokeLeavesNoAccessBehindAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var repeated = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(repeated.Id);

            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                    w.CaseWorkflowFormGuid == formGuid
                    && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should()
                .Be(0, "one revoke must remove the role's access");
        }

        [Fact]
        public async Task TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var (_, _, formGuidTwo) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);

            var one = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var two = await service.InsertAsync(new CaseWorkflowFormRoleDto
            {
                CaseWorkflowFormGuid = formGuidTwo,
                RoleRegistryGuid = roleRegistryGuid,
            });

            two.Id.Should().NotBe(one.Id);
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                w.CaseWorkflowFormGuid == formGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                w.CaseWorkflowFormGuid == formGuidTwo
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, formGuid) = await CreateModelWorkflowFormAsync(dbContext, fx.Seed.UserWithPermission);
            var roleRegistryGuid = await GetRoleRegistryGuidAsync(dbContext, fx.Seed.UserWithPermission);

            var ids = new ConcurrentBag<int>();
            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var raceContext = fx.GetDbContext();
                var raceService = await BuildServiceAsync(raceContext, fx.Seed.UserWithPermission);
                var saved = await raceService.InsertAsync(new CaseWorkflowFormRoleDto
                {
                    CaseWorkflowFormGuid = formGuid,
                    RoleRegistryGuid = roleRegistryGuid,
                });
                ids.Add(saved.Id);
            }));

            ids.Distinct().Should().ContainSingle("every racing grant must resolve to the one live row");
            (await dbContext.GetTable<Data.Poco.CaseWorkflowFormRole>().CountAsync(w =>
                w.CaseWorkflowFormGuid == formGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }
    }
}