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
using Jube.Dto.Repository.CaseWorkflowFilter;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseWorkflowFilter;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowFilter;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowFilter
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowFilterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdFilterIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var filterGuids = await dbContext.CaseWorkflowFilter
                .Where(w => createdFilterIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowFilterRole>()
                .Where(w => filterGuids.Contains(w.CaseWorkflowFilterGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowFilterVersion>()
                .Where(w => w.CaseWorkflowFilterId != null && createdFilterIds.Contains(w.CaseWorkflowFilterId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowFilter.Where(w => createdFilterIds.Contains(w.Id)).DeleteAsync();
            var workflowGuids = dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => workflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowFilterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowFilterService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
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

        private async Task GrantWorkflowRoleAsync(DbContext dbContext, int caseWorkflowId, string userName)
        {
            var caseWorkflowGuid = await dbContext.CaseWorkflow.Where(w => w.Id == caseWorkflowId)
                .Select(w => w.Guid).FirstAsync();
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = caseWorkflowGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private async Task GrantFilterRoleAsync(DbContext dbContext, Guid caseWorkflowFilterGuid, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowFilterRole
            {
                CaseWorkflowFilterGuid = caseWorkflowFilterGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private static CaseWorkflowFilterDto ValidDto(int caseWorkflowId, string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Filter{Guid.NewGuid():N}"[..40],
            CaseWorkflowId = caseWorkflowId,
            Active = true,
            Locked = false,
            SelectJson = "{\"columns\":[\"key\"]}",
            FilterJson = "{\"rules\":[]}",
            FilterTokens = "{}",
        };

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

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}Filter{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(workflowId, name)));

            var stillExists = await dbContext.CaseWorkflowFilter.AnyAsync(w => w.Name == name);
            stillExists.Should().BeFalse();
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId, "");

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithUnknownCaseWorkflowIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(-1)));
        }

        [Fact]
        public async Task InsertWithBlankSelectJsonThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.SelectJson = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Filter{Guid.NewGuid():N}"[..40];

            var first = await service.InsertAsync(ValidDto(workflowId, name));
            createdFilterIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(workflowId, name)));
        }

        [Fact]
        public async Task InsertIgnoresDescriptionAndHttpNotificationFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Description = "should be dropped";
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpoint = "https://example.test";
            dto.HttpEndpointTypeId = 1;
            dto.EnableNotification = true;
            dto.NotificationType = 1;
            dto.NotificationDestination = "someone@example.test";
            dto.NotificationSubject = "subject";
            dto.NotificationBody = "body";

            var saved = await service.InsertAsync(dto);
            createdFilterIds.Add(saved.Id);

            saved.Description.Should().BeNull();
            saved.EnableHttpEndpoint.Should().BeFalse();
            saved.HttpEndpoint.Should().BeNull();
            saved.EnableNotification.Should().BeFalse();
            saved.NotificationDestination.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);
            var originalCreatedUser = created.CreatedUser;
            var originalCreatedDate = created.CreatedDate;

            await Task.Delay(50);

            var toUpdate = ValidDto(workflowId, created.Name);
            toUpdate.Id = created.Id;
            var updated = await service.UpdateAsync(toUpdate);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(originalCreatedUser);
            updated.CreatedDate.Should().BeCloseTo(originalCreatedDate.Required(), TimeSpan.FromSeconds(1));
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 1,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.CaseWorkflowFilter.Where(w => w.Id == created.Id)
                .Set(w => w.ImportId, importId).UpdateAsync();

            try
            {
                var toUpdate = ValidDto(workflowId, created.Name);
                toUpdate.Id = created.Id;
                await service.UpdateAsync(toUpdate);

                var persistedImportId = await dbContext.CaseWorkflowFilter.Where(w => w.Id == created.Id)
                    .Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.CaseWorkflowFilter.Where(w => w.Id == created.Id)
                    .Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Data.Poco.Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task UpdateOfUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Id = int.MaxValue;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfCrossTenantRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var otherModelId = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var otherWorkflowId = await CreateWorkflowAsync(dbContext, otherModelId, fx.Seed.UserTenantB);
            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var dto = ValidDto(otherWorkflowId, created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenantService.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateWithCaseWorkflowIdFromAnotherTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var dto = ValidDto(workflowId, created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<DtoValidationException>(() => otherTenantService.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);
            bus.Published.Clear();

            await service.DeleteAsync(created.Id);

            var stillVisible = await service.GetByIdAsync(created.Id);
            stillVisible.Should().BeNull();
            bus.Published.Should().ContainSingle(e => e.Kind == ServiceChangeKind.Deleted);
        }

        [Fact]
        public async Task DeleteOfUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue));
        }

        [Fact]
        public async Task ListByWorkflowIdIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var results = await otherTenantService.GetByCaseWorkflowIdAsync(workflowId);

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndFilterRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var ownerService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await ownerService.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var beforeAnyGrant = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            beforeAnyGrant.Should().BeEmpty();

            await GrantWorkflowRoleAsync(dbContext, workflowId, fx.Seed.UserWithPermission);
            var withOnlyWorkflowGrant = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            withOnlyWorkflowGrant.Should().BeEmpty();

            await GrantFilterRoleAsync(dbContext, created.Guid, fx.Seed.UserWithPermission);
            var withBothGrants = await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId);
            withBothGrants.Should().ContainSingle(d => d.Id == created.Id);
        }

        [Fact]
        public async Task GetByGuidReturnsMatchingFilterAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(workflowId));
            createdFilterIds.Add(created.Id);

            var found = await service.GetByGuidAsync(created.Guid);

            found.Should().NotBeNull();
            found.Required().Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await service.GetAsync();

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersExpectedToolNamesForThisArea()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToHashSet();

            names.Should().Contain("CaseWorkflowFilterList");
            names.Should().Contain("CaseWorkflowFilterListByWorkflowIdActiveOnly");
            names.Should().Contain("CaseWorkflowFilterListByWorkflowGuidActiveOnly");
            names.Should().Contain("CaseWorkflowFilterGet");
            names.Should().Contain("CaseWorkflowFilterGetByGuid");
            names.Should().Contain("CaseWorkflowFilterCreate");
            names.Should().Contain("CaseWorkflowFilterUpdate");
            names.Should().Contain("CaseWorkflowFilterDelete");
        }
    }
}