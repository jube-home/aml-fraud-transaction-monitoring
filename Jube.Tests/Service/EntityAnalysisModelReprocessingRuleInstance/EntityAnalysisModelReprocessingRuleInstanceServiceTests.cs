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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Exceptions.EntityAnalysisModelReprocessingRuleInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelReprocessingRuleInstance
{
    using EntityAnalysisModelReprocessingRuleInstanceService = EntityAnalysisModelReprocessingRuleInstanceService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelReprocessingRuleInstanceServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdInstanceIds = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdReprocessingRuleIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdInstanceIds)
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                    .Where(w => w.Id == id).DeleteAsync();

            foreach (var id in createdReprocessingRuleIds)
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRule>().Where(w => w.Id == id)
                    .DeleteAsync();

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelReprocessingRuleInstanceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelReprocessingRuleInstanceService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateParentModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private async Task<int> CreateParentReprocessingRuleAsync(DbContext dbContext, string createdUser,
            int modelId)
        {
            var repository = new EntityAnalysisModelReprocessingRuleRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModelReprocessingRule
            {
                EntityAnalysisModelId = modelId,
                Name = $"{DatabaseFixture.Prefix}ReprocessingRule{Guid.NewGuid():N}"[..40],
                Priority = 1,
                Active = 1,
                Locked = 0,
                RuleScriptTypeId = 1,
                BuilderRuleScript = "If (Payload.CurrencyAmount > 0) Then\n   Return True\nEnd If",
                Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
                CoderRuleScript = "Return True",
                ReprocessingSample = 100,
                ReprocessingValue = 1,
                ReprocessingInterval = "d"
            }).ConfigureAwait(false);

            createdReprocessingRuleIds.Add(saved.Id);
            return saved.Id;
        }

        private static EntityAnalysisModelReprocessingRuleInstanceDto NewDto(int entityAnalysisModelReprocessingRuleId)
        {
            return new EntityAnalysisModelReprocessingRuleInstanceDto
            {
                EntityAnalysisModelReprocessingRuleId = entityAnalysisModelReprocessingRuleId,
                StatusId = 0,
                AvailableCount = 0,
                SampledCount = 0,
                MatchedCount = 0,
                ProcessedCount = 0,
                ErrorCount = 0
            };
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.EntityAnalysisModelReprocessingRuleId.Should().Be(ruleId);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelReprocessingIdReturnsOnlyRowsForThatRuleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleAId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var ruleBId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA = await service.InsertAsync(NewDto(ruleAId));
            createdInstanceIds.Add(savedA.Id);
            var savedB = await service.InsertAsync(NewDto(ruleBId));
            createdInstanceIds.Add(savedB.Id);

            var forRuleA = await service.GetByEntityAnalysisModelReprocessingIdAsync(ruleAId);
            forRuleA.Should().Contain(d => d.Id == savedA.Id);
            forRuleA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task UpdateSupersedesRowWithNewIdAndIncrementsVersionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            var dto = NewDto(ruleId);
            dto.Id = saved.Id;
            dto.StatusId = 3;
            dto.SampledCount = 5;

            var updated = await service.UpdateAsync(dto);
            createdInstanceIds.Add(updated.Id);

            updated.Id.Should().NotBe(saved.Id);
            updated.Version.Should().Be(2);
            updated.StatusId.Should().Be(3);
            updated.SampledCount.Should().Be(5);

            var oldRow = await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                .FirstAsync(w => w.Id == saved.Id);
            oldRow.Deleted.Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var forRule = await service.GetByEntityAnalysisModelReprocessingIdAsync(ruleId);
            forRule.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task InsertByExistingUpdateUncompletedCreatesInstanceAtStatusZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            saved.StatusId.Should().Be(0);
            saved.EntityAnalysisModelReprocessingRuleId.Should().Be(ruleId);
        }

        [Fact]
        public async Task InsertByExistingUpdateUncompletedThrowsConflictWhenUncompletedInstanceExistsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId));
            createdInstanceIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<ConflictException>(() =>
                service.InsertByExistingUpdateUncompletedAsync(NewDto(ruleId)));
            ex.Code.Should().Be("Conflict");
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(writerDb, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(writerDb, fx.Seed.UserWithPermission, modelId);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                .CountAsync(w => w.Id == seedRow.Id);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(NewDto(ruleId)));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(ruleId);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                .CountAsync(w => w.Id == seedRow.Id);
            afterCount.Should().Be(beforeCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisModelReprocessingRuleInstanceService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(ownerDb, fx.Seed.UserWithPermission, modelId);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            await using var otherOwnerDb = fx.GetDbContext();
            var otherModelId = await CreateParentModelAsync(otherOwnerDb, fx.Seed.UserTenantB);
            var otherRuleId =
                await CreateParentReprocessingRuleAsync(otherOwnerDb, fx.Seed.UserTenantB, otherModelId);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var updateDto = NewDto(otherRuleId);
            updateDto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(ownerDb, fx.Seed.UserWithPermission, modelId);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task CrossTenantEntityAnalysisModelReprocessingRuleIdIsRejectedOnInsertAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(ownerDb, fx.Seed.UserWithPermission, modelId);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                otherTenant.InsertAsync(NewDto(ruleId)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelReprocessingRuleIdNotFound");

            var rows = await dbContext.GetTable<Data.Poco.EntityAnalysisModelReprocessingRuleInstance>()
                .CountAsync(w => w.EntityAnalysisModelReprocessingRuleId == ruleId);
            rows.Should().Be(0);
        }

        [Fact]
        public async Task CrossTenantEntityAnalysisModelReprocessingRuleIdIsRejectedOnUpdateAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var ruleAId = await CreateParentReprocessingRuleAsync(ownerDb, fx.Seed.UserWithPermission, modelId);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(ruleAId));
            createdInstanceIds.Add(saved.Id);

            await using var otherDb = fx.GetDbContext();
            var otherModelId = await CreateParentModelAsync(otherDb, fx.Seed.UserTenantB);
            var ruleBId = await CreateParentReprocessingRuleAsync(otherDb, fx.Seed.UserTenantB, otherModelId);

            await using var dbContext = fx.GetDbContext();
            var owningTenant = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(ruleBId);
            dto.Id = saved.Id;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => owningTenant.UpdateAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelReprocessingRuleIdNotFound");
        }

        [Fact]
        public async Task InvalidEntityAnalysisModelReprocessingRuleIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto(0)));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelReprocessingRuleInstanceDto
                    .EntityAnalysisModelReprocessingRuleId) &&
                e.ErrorCode == "EntityAnalysisModelReprocessingRuleIdInvalid");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public async Task InvalidStatusIdIsRejectedAsync(int statusId)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(ruleId);
            dto.StatusId = statusId;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "StatusIdInvalid");
        }

        [Fact]
        public async Task NegativeAvailableCountIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(ruleId);
            dto.AvailableCount = -1;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "AvailableCountRange");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(ruleId);
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdInstanceIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            var dto = NewDto(ruleId);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var updated = await service.UpdateAsync(dto);
            createdInstanceIds.Add(updated.Id);

            updated.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(ruleId);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(ruleId);
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByIdAsync(int.MaxValue - 1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

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
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelReprocessingRuleInstance.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"]! == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(ruleId));
            createdInstanceIds.Add(saved.Id);

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto(0)));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain(
            [
                "EntityAnalysisModelReprocessingRuleInstanceList", "EntityAnalysisModelReprocessingRuleInstanceGet",
                "EntityAnalysisModelReprocessingRuleInstanceGetByEntityAnalysisModelReprocessingId",
                "EntityAnalysisModelReprocessingRuleInstanceCreate",
                "EntityAnalysisModelReprocessingRuleInstanceInsertByExistingUpdateUncompleted",
                "EntityAnalysisModelReprocessingRuleInstanceUpdate",
                "EntityAnalysisModelReprocessingRuleInstanceDelete"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var ruleId = await CreateParentReprocessingRuleAsync(dbContext, fx.Seed.UserWithPermission, modelId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(ruleId));
                createdInstanceIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        private sealed class CapturingBus : IServiceChangeBus
        {
            public readonly List<ServiceChangeEvent> Published = [];

            public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
            {
                Published.Add(change);
                return Task.CompletedTask;
            }

            public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
            {
                return NoopSubscription.Instance;
            }

            private sealed class NoopSubscription : IDisposable
            {
                public static readonly NoopSubscription Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}