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
using Jube.Dto.Repository.CaseWorkflowAction;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.CaseWorkflowAction;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflowAction;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflowAction
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowActionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdActionIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var actionGuids = await dbContext.CaseWorkflowAction
                .Where(w => createdActionIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();

            await dbContext.GetTable<Data.Poco.CaseWorkflowActionRole>()
                .Where(w => actionGuids.Contains(w.CaseWorkflowActionGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowActionVersion>()
                .Where(w => w.CaseWorkflowActionId != null && createdActionIds.Contains(w.CaseWorkflowActionId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflowAction.Where(w => createdActionIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowGuids1 = dbContext.CaseWorkflow.Where(s => createdWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids1.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowActionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowActionService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
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

        private async Task GrantActionRoleAsync(DbContext dbContext, Guid caseWorkflowActionGuid, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.CaseWorkflowActionRole
            {
                CaseWorkflowActionGuid = caseWorkflowActionGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private static CaseWorkflowActionDto ValidDto(int caseWorkflowId, string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40],
            CaseWorkflowId = caseWorkflowId,
            Active = true,
            Locked = false,
            EnableHttpEndpoint = false,
            EnableNotification = false,
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
        public async Task GetByIdWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(workflowId, name)));

            (await dbContext.CaseWorkflowAction.AnyAsync(w => w.Name == name)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            created.Name += "x";

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task InsertPersistsAndDropsIdentityFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Id = 999999;
            dto.Guid = Guid.NewGuid();
            dto.CreatedUser = "someone-else";

            var saved = await service.InsertAsync(dto);
            createdActionIds.Add(saved.Id);

            saved.Id.Should().NotBe(999999);
            saved.Guid.Should().NotBe(dto.Guid);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            await Task.Delay(50);
            var updaterService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            created.Name += "-updated";
            var updated = await updaterService.UpdateAsync(created);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(10));
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermissionNoApproveByReview);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            created.Guid = Guid.NewGuid();
            created.CreatedUser = "tampered";
            created.Version = 999;
            var updated = await service.UpdateAsync(created);

            updated.Guid.Should().NotBe(created.Guid);
            updated.CreatedUser.Should().NotBe("tampered");
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateWithUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Id = 999999;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Locked = true;
            var created = await service.InsertAsync(dto);
            createdActionIds.Add(created.Id);

            created.Name += "x";
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteRemovesRowAndSubsequentGetReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999999));
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserTenantB);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var list = await service.GetAsync();

            list.Should().NotContain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserTenantB);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            created.Name += "x";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithOverLongNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId, new string('a', 257));

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameWorkflowThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(workflowId, name));
            createdActionIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(workflowId, name)));
        }

        [Fact]
        public async Task InsertWithNonExistentCaseWorkflowIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(999999)));
        }

        [Fact]
        public async Task InsertWithEnableHttpEndpointAndNoUrlThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpointTypeId = 1;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithEnableHttpEndpointAndInvalidTypeThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpoint = "https://example.test/hook";
            dto.HttpEndpointTypeId = 99;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithEnableHttpEndpointAndValidDataSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpoint = "https://example.test/hook";
            dto.HttpEndpointTypeId = 1;

            var saved = await service.InsertAsync(dto);
            createdActionIds.Add(saved.Id);

            saved.EnableHttpEndpoint.Should().BeTrue();
            saved.HttpEndpoint.Should().Be("https://example.test/hook");
        }

        [Fact]
        public async Task InsertWithEnableNotificationAndNoDestinationThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableNotification = true;
            dto.NotificationTypeId = 1;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithEnableNotificationAndInvalidTypeThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableNotification = true;
            dto.NotificationDestination = "ops@example.test";
            dto.NotificationTypeId = 99;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithEnableNotificationAndValidDataSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.EnableNotification = true;
            dto.NotificationDestination = "ops@example.test";
            dto.NotificationTypeId = 1;

            var saved = await service.InsertAsync(dto);
            createdActionIds.Add(saved.Id);

            saved.EnableNotification.Should().BeTrue();
            saved.NotificationDestination.Should().Be("ops@example.test");
        }

        [Fact]
        public async Task InsertIgnoresDescriptionFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Description = "should not be persisted";

            var saved = await service.InsertAsync(dto);
            createdActionIds.Add(saved.Id);

            saved.Description.Should().BeNull();
        }

        [Fact]
        public async Task ListByWorkflowIncludesInactiveRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(workflowId);
            dto.Active = false;
            var created = await service.InsertAsync(dto);
            createdActionIds.Add(created.Id);

            var list = await service.GetByCaseWorkflowIdAsync(workflowId);

            list.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListByWorkflowIdActiveOnlyRequiresBothWorkflowAndActionRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermissionNoApproveByReview);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);

            (await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId)).Should()
                .NotContain(d => d.Id == created.Id);

            await GrantWorkflowRoleAsync(dbContext, workflowId, fx.Seed.UserWithPermissionNoApproveByReview);
            (await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId)).Should()
                .NotContain(d => d.Id == created.Id);

            await GrantActionRoleAsync(dbContext, created.Guid, fx.Seed.UserWithPermissionNoApproveByReview);
            (await service.GetByCasesWorkflowIdActiveOnlyAsync(workflowId)).Should()
                .Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListByWorkflowGuidActiveOnlyReturnsRowsOnceBothRolesGrantedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermissionNoApproveByReview);
            var workflowGuid = await dbContext.CaseWorkflow.Where(w => w.Id == workflowId)
                .Select(w => w.Guid).FirstAsync();
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            await GrantWorkflowRoleAsync(dbContext, workflowId, fx.Seed.UserWithPermissionNoApproveByReview);
            await GrantActionRoleAsync(dbContext, created.Guid, fx.Seed.UserWithPermissionNoApproveByReview);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var list = await service.GetByCasesWorkflowGuidActiveOnlyAsync(workflowGuid);

            list.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("CaseWorkflowAction");
            change.Kind.Should().Be(ServiceChangeKind.Created);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var dto = ValidDto(workflowId);
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task DeletePublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            await service.DeleteAsync(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("CaseWorkflowAction");
            change.Kind.Should().Be(ServiceChangeKind.Deleted);
            change.EntityId.Should().Be(created.Id);
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
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, testLog);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(1)));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertWritesOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            var created = await service.InsertAsync(ValidDto(workflowId));
            createdActionIds.Add(created.Id);

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public void CatalogueRegistersUniqueCaseWorkflowActionToolNames()
        {
            var tools = ServiceToolCatalogue.All;

            var names = new[]
            {
                "CaseWorkflowActionList", "CaseWorkflowActionListByWorkflowIdActiveOnly",
                "CaseWorkflowActionListByWorkflowGuidActiveOnly", "CaseWorkflowActionGet",
                "CaseWorkflowActionCreate", "CaseWorkflowActionUpdate", "CaseWorkflowActionDelete",
            };

            foreach (var name in names)
            {
                tools.Should().ContainSingle(t => t.Name == name);
            }

            tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
        }
    }
}