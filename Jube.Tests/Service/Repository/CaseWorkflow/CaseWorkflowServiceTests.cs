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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Repository.CaseWorkflow;
using Jube.Service.Exceptions.Repository.CaseWorkflow;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.CaseWorkflow;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.CaseWorkflow
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseWorkflowServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .Where(w => w.CaseWorkflowId != null && createdCaseWorkflowIds.Contains(w.CaseWorkflowId.Value))
                .DeleteAsync();
            var caseWorkflowGuids1 = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids1.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseWorkflowService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseWorkflowService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
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
            string createdUser, byte active = 1, byte locked = 0, byte deleted = 0, string? name = null)
        {
            var caseWorkflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = name ?? $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = entityAnalysisModelId,
                Active = active,
                Locked = locked,
                Deleted = deleted,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow,
            }).ConfigureAwait(false);

            createdCaseWorkflowIds.Add(caseWorkflowId);
            return caseWorkflowId;
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

        private static CaseWorkflowDto ValidDto(int entityAnalysisModelId, string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
            EntityAnalysisModelId = entityAnalysisModelId,
            Active = true,
            Locked = false,
            EnableVisualisation = false,
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
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(modelId, name)));

            (await dbContext.CaseWorkflow.AnyAsync(w => w.Name == name)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var dto = ValidDto(modelId);
            dto.Id = workflowId;

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(workflowId));
        }

        [Fact]
        public async Task ListByModelActiveOnlyRequiresCaseWorkflowRoleNotJustTenantPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByEntityAnalysisModelIdActiveOnlyAsync(modelId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByModelActiveOnlyReturnsRowsOnceRoleIsGrantedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission);
            await GrantWorkflowRoleAsync(dbContext, workflowId, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByEntityAnalysisModelIdActiveOnlyAsync(modelId);

            result.Should().ContainSingle(w => w.Id == workflowId);
        }

        [Fact]
        public async Task LandlordUserDoesNotAutomaticallySeeOtherTenantsRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var all = await service.GetAsync();

            all.Should().NotContain(w => w.Id == workflowIdA);
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.CreatedDate.Should().NotBeNull().And.BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetByIdReturnsInsertedRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);

            fetched.Should().NotBeNull();
            fetched.Required().Name.Should().Be(saved.Name);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdReturnsRowsForThatModelOnlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelIdB = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            await CreateWorkflowAsync(dbContext, modelIdB, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByEntityAnalysisModelIdAsync(modelIdA);

            result.Should().ContainSingle(w => w.Id == workflowIdA);
        }

        [Fact]
        public async Task UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);

            await Task.Delay(10);
            var updateDto = ValidDto(modelId, $"{DatabaseFixture.Prefix}Renamed{Guid.NewGuid():N}"[..40]);
            updateDto.Id = created.Id;
            var updated = await service.UpdateAsync(updateDto);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(1));
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.UpdatedDate.Should().NotBeNull();
            updated.UpdatedDate.Should().BeAfter(created.CreatedDate.Required());
        }

        [Fact]
        public async Task UpdateWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);
            var updateDto = ValidDto(modelId);
            updateDto.Id = created.Id;

            await service.UpdateAsync(updateDto);

            (await dbContext.GetTable<Data.Poco.CaseWorkflowVersion>()
                .CountAsync(w => w.CaseWorkflowId == created.Id)).Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
            (await dbContext.CaseWorkflow.Where(w => w.Id == created.Id).Select(w => w.Deleted)
                .FirstAsync()).Should().Be(1);
        }

        [Fact]
        public async Task TenantBCannotGetByIdTenantARowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await service.GetByIdAsync(workflowIdA)).Should().BeNull();
        }

        [Fact]
        public async Task TenantBUpdateOfTenantARowThrowsNotFoundAndLeavesRowUntouchedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            var modelIdB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var dto = ValidDto(modelIdB);
            dto.Id = workflowIdA;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));

            (await dbContext.CaseWorkflow.Where(w => w.Id == workflowIdA).Select(w => w.Version)
                .FirstAsync()).Should().Be(1);
        }

        [Fact]
        public async Task TenantBDeleteOfTenantARowThrowsNotFoundAndRowRemainsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(workflowIdA));

            (await dbContext.CaseWorkflow.Where(w => w.Id == workflowIdA).Select(w => w.Deleted)
                .FirstAsync()).Should().NotBe(1);
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowIdA = await CreateWorkflowAsync(dbContext, modelIdA, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var result = await service.GetAsync();

            result.Should().NotContain(w => w.Id == workflowIdA);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.Name = name;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task InsertWithNameOverMaxLengthThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId, new string('a', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task InsertWithNameAtExactlyMaxLengthSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId,
                $"{DatabaseFixture.Prefix}{new string('a', 256 - DatabaseFixture.Prefix.Length)}");

            var saved = await service.InsertAsync(dto);
            createdCaseWorkflowIds.Add(saved.Id);

            saved.Name.Should().HaveLength(256);
        }

        [Fact]
        public async Task InsertWithUnicodeAndSqlLikeNamePersistsSafelyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Wörkflöw'; DROP TABLE CaseWorkflow;--{Guid.NewGuid():N}";
            var dto = ValidDto(modelId, name);

            var saved = await service.InsertAsync(dto);
            createdCaseWorkflowIds.Add(saved.Id);

            saved.Name.Should().Be(name);
            (await dbContext.CaseWorkflow.AnyAsync(w => w.Id == saved.Id)).Should().BeTrue();
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameTenantAndModelThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Dup{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(modelId, name));
            createdCaseWorkflowIds.Add(first.Id);

            var ex =
                await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(modelId, name)));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task InsertWithSameNameDifferentCasingInSameTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var name = $"{DatabaseFixture.Prefix}Casing{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(modelId, name));
            createdCaseWorkflowIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(modelId, name.ToUpperInvariant())));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task InsertWithSameNameInDifferentTenantSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdA = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelIdB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var name = $"{DatabaseFixture.Prefix}CrossTenant{Guid.NewGuid():N}"[..40];
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var first = await serviceA.InsertAsync(ValidDto(modelIdA, name));
            createdCaseWorkflowIds.Add(first.Id);

            var second = await serviceB.InsertAsync(ValidDto(modelIdB, name));
            createdCaseWorkflowIds.Add(second.Id);

            second.Id.Should().NotBe(first.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InsertWithNonPositiveEntityAnalysisModelIdThrowsValidationFailedAsync(int modelId)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelIdInvalid");
        }

        [Fact]
        public async Task InsertWithNonExistentEntityAnalysisModelIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(999_999_999);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelIdNotFound");
        }

        [Fact]
        public async Task InsertWithEntityAnalysisModelIdInAnotherTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelIdB = await CreateModelAsync(dbContext, fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelIdB);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelIdNotFound");
        }

        [Fact]
        public async Task InsertWithVisualisationEnabledAndEmptyGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.EnableVisualisation = true;
            dto.VisualisationRegistryGuid = Guid.Empty;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryGuidNotEmpty");
        }

        [Fact]
        public async Task InsertTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.CreatedUser = "someone-else";
            dto.CreatedDate = DateTimeOffset.UtcNow.AddYears(-5);
            dto.Guid = Guid.NewGuid();
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdCaseWorkflowIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
            saved.Guid.Should().NotBe(dto.Guid);
            saved.CreatedDate.Should().NotBe(dto.CreatedDate);
        }

        [Fact]
        public async Task UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);

            var dto = ValidDto(modelId);
            dto.Id = created.Id;
            dto.CreatedUser = "someone-else";
            dto.CreatedDate = DateTimeOffset.UtcNow.AddYears(-5);
            dto.Guid = Guid.NewGuid();
            dto.Version = 999;

            var updated = await service.UpdateAsync(dto);

            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(1));
            updated.Guid.Should().Be(created.Guid);
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, locked: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.Id = workflowId;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, deleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.Id = workflowId;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateWithUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.Id = 999_999_999;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999_999_999));
        }

        [Fact]
        public async Task DeleteOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var workflowId = await CreateWorkflowAsync(dbContext, modelId, fx.Seed.UserWithPermission, locked: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(workflowId));
        }

        [Fact]
        public async Task GetByIdForMissingRowReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetByIdAsync(999_999_999)).Should().BeNull();
        }

        [Fact]
        public async Task ActiveAndLockedByteRoundTripAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(modelId);
            dto.Active = false;
            dto.Locked = false;

            var saved = await service.InsertAsync(dto);
            createdCaseWorkflowIds.Add(saved.Id);

            saved.Active.Should().BeFalse();
            saved.Locked.Should().BeFalse();
        }

        [Fact]
        public async Task GetWithCancelledTokenThrowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var saved = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(saved.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("CaseWorkflow");
            change.Kind.Should().Be(ServiceChangeKind.Created);
            change.EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task UpdatePublishesUpdatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);

            var bus = new CapturingBus();
            var updateService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            var dto = ValidDto(modelId);
            dto.Id = created.Id;
            await updateService.UpdateAsync(dto);

            bus.Published.Should().ContainSingle(c => c.Kind == ServiceChangeKind.Updated);
        }

        [Fact]
        public async Task DeletePublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(created.Id);

            var bus = new CapturingBus();
            var deleteService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            await deleteService.DeleteAsync(created.Id);

            bus.Published.Should().ContainSingle(c => c.Kind == ServiceChangeKind.Deleted);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(0, "")));

            bus.Published.Should().BeEmpty();
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
        public async Task InsertWithoutPermissionLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, testLog);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(modelId)));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertUnexpectedFailureLogsErrorWithExceptionAttachedAsync()
        {
            var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            // Disposed deliberately (only once) to force an unexpected failure inside the service.
            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.InsertAsync(ValidDto(modelId)));

            var errorEntry = testLog.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task InsertSuccessLogsExactlyOneInfoEntryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var testLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            var saved = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(saved.Id);

            testLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public async Task ListWithGatesDisabledRecordsNoEntriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var testLog = new TestLog(enabled: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, testLog);

            await service.GetAsync();

            testLog.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task ListEmitsExactlyOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            var entry = auditLog.Entries.Should().ContainSingle().Subject;
            entry.Message.Should().Contain("area=CaseWorkflow").And.Contain("op=List").And.Contain("outcome=ok");
        }

        [Fact]
        public async Task InsertEmitsExactlyOneAuditRecordWithEntityIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            var saved = await service.InsertAsync(ValidDto(modelId));
            createdCaseWorkflowIds.Add(saved.Id);

            var entry = auditLog.Entries.Should().ContainSingle().Subject;
            entry.Message.Should().Contain($"entityId={saved.Id}");
        }

        [Fact]
        public async Task InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.InsertAsync(ValidDto(modelId, "")));

                ex.Result.Errors.First(e => e.PropertyName == "Name").ErrorMessage
                    .Should().Be("Le nom est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void ServiceToolCatalogueRegistersCaseWorkflowOperationsWithGloballyUniqueNames()
        {
            var names = Jube.Service.Agent.ServiceToolCatalogue.ServiceToolCatalogue.All
                .Select(t => t.Name).ToList();

            names.Should().Contain("CaseWorkflowList").And.Contain("CaseWorkflowListByModelActiveOnly")
                .And.Contain("CaseWorkflowGet").And.Contain("CaseWorkflowCreate")
                .And.Contain("CaseWorkflowUpdate").And.Contain("CaseWorkflowDelete");
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(ValidDto(modelId));
                createdCaseWorkflowIds.Add(saved.Id);
            }

            var page = await service.ListAsync(take: 500);
            page.Items.Count.Should().BeLessOrEqualTo(200);

            var firstPage = await service.ListAsync(take: 2);
            firstPage.Items.Should().HaveCount(2);
            var secondPage = await service.ListAsync(take: 2, afterId: firstPage.Items[^1].Id);
            secondPage.Items.Should().NotContain(w => firstPage.Items.Select(f => f.Id).Contains(w.Id));
        }
    }
}