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
using Jube.Case.Outbound;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dto.EntityAnalysisModelResponseElevationRange;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelResponseElevationRange;
using Jube.Service.Exceptions.EntityAnalysisModelResponseElevationRange;
using Jube.Service.Exceptions.EntityApproval;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Reactivity.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.Reactivity;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelResponseElevationRange
{
    using EntityAnalysisModelResponseElevationRangeService = EntityAnalysisModelResponseElevationRangeService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelResponseElevationRangeServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static int rangeCounter;
        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];
        private int tenantRegistryId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            tenantRegistryId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.EntityApproval.Where(w => createdIds.Contains(w.EntityId ?? 0)
                                                      && w.EntityApprovalKindId ==
                                                      (int)EntityApprovalKind.EntityAnalysisModelResponseElevationRange).DeleteAsync();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelResponseElevationRangeVersion>()
                    .Where(w => w.EntityAnalysisModelResponseElevationRangeId == id).DeleteAsync();

                await dbContext.GetTable<Data.Poco.EntityAnalysisModelResponseElevationRange>().Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();

                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelResponseElevationRangeService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelResponseElevationRangeService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp, TestDynamicEnvironment.Create());
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

        private static EntityAnalysisModelResponseElevationRangeDto NewDto(int entityAnalysisModelId, string name)
        {
            var next = Interlocked.Increment(ref rangeCounter) * 10;

            return new EntityAnalysisModelResponseElevationRangeDto
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = name,
                Minimum = next,
                Maximum = next + 5
            };
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Insert")));
            createdIds.Add(saved.Id);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(modelId, name));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.Name.Should().Be(name);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdReturnsOnlyActiveRowsForThatModelOrderedByNameAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelAId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelBId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var activeDto = NewDto(modelAId, UniqueName("ByModelAActive"));
            activeDto.Active = true;
            var savedActive = await service.InsertAsync(activeDto);
            createdIds.Add(savedActive.Id);

            var inactiveDto = NewDto(modelAId, UniqueName("ByModelAInactive"));
            inactiveDto.Active = false;
            var savedInactive = await service.InsertAsync(inactiveDto);
            createdIds.Add(savedInactive.Id);

            var otherModelDto = NewDto(modelBId, UniqueName("ByModelB"));
            otherModelDto.Active = true;
            var savedB = await service.InsertAsync(otherModelDto);
            createdIds.Add(savedB.Id);

            var forModelA = await service.GetByEntityAnalysisModelIdAsync(modelAId);
            forModelA.Should().Contain(d => d.Id == savedActive.Id);
            forModelA.Should().NotContain(d => d.Id == savedInactive.Id);
            forModelA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Update")));
            createdIds.Add(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            dto.Active = true;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.Active.Should().Be(1);
            updated.Guid.Should().Be(saved.Guid);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelResponseElevationRangeVersion>()
                .Where(w => w.EntityAnalysisModelResponseElevationRangeId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task DeleteKeepsRowReachableByIdAndHidesItFromListsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull("a deleted row stays reachable by id so it can be opened and revived");

            var all = await service.GetAsync();
            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(modelId, UniqueName("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelResponseElevationRange>()
                .CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(modelId, UniqueName("Denied"))));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(modelId, seedRow.Name);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ResubmitAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.RetractAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelResponseElevationRange>()
                .CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));
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
                EntityAnalysisModelResponseElevationRangeService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp, TestDynamicEnvironment.Create()));

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
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelId, UniqueName("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var updateDto = NewDto(modelId, saved.Name);
            updateDto.Id = saved.Id;
            await Assert.ThrowsAsync<DtoValidationException>(() => otherTenant.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelId, UniqueName("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, name!);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelResponseElevationRangeDto.Name) &&
                e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, new string('n', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync()
        {
            var name = UniqueName("Dup");

            await using var dbContext = fx.GetDbContext();
            var modelAId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelBId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertAsync(NewDto(modelAId, name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelAId, name)));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var second = await service.InsertAsync(NewDto(modelBId, name));
            createdIds.Add(second.Id);
        }

        [Fact]
        public async Task DuplicateNameDifferingOnlyByCaseWithinSameModelFailsAsync()
        {
            var name = UniqueName("CaseDup");

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(NewDto(modelId, name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelId, name.ToUpperInvariant())));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task InvalidEntityAnalysisModelIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(0, UniqueName("BadModelId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelIdInvalid");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelId, UniqueName("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = saved.Version.GetValueOrDefault();
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.Guid.Should().Be(saved.Guid);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Locked")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelResponseElevationRange>()
                .Where(w => w.Id == saved.Id).Set(s => s.Locked, (byte)1).UpdateAsync();

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowRevivesItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            await service.UpdateAsync(dto);

            (await service.GetByIdAsync(saved.Id)).Should().NotBeNull(
                "an update to a deleted row revives it, so it is reachable by id");
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelId, UniqueName("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ResubmitOfNonRejectedVersionThrowsApprovalRefusedAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("ResubmitPending")));
            createdIds.Add(saved.Id);

            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.ResubmitAsync(saved.Id));

            (await service.GetByIdAsync(saved.Id))!.Version.Should().Be(1);
        }

        [Fact]
        public async Task ResubmitOfRejectedVersionBumpsVersionArchivesOldAndKeepsContentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("ResubmitRejected")));
            createdIds.Add(saved.Id);

            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 1, tenantRegistryId);

            var resubmitted = await service.ResubmitAsync(saved.Id);

            resubmitted.Version.Should().Be(2);
            resubmitted.Name.Should().Be(saved.Name);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelResponseElevationRangeVersion>()
                .Where(w => w.EntityAnalysisModelResponseElevationRangeId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task ResubmitOfRejectedDeleteKeepsDeletedTrueAndReopensApprovalAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("ResubmitDelete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 2, tenantRegistryId);

            var resubmitted = await service.ResubmitAsync(saved.Id);

            resubmitted.Version.Should().Be(3);
            resubmitted.Deleted.Should().Be(1,
                "resubmitting a rejected delete must re-open approval without un-deleting it");
        }

        [Fact]
        public async Task RetractOfNonRejectedVersionThrowsApprovalRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("RetractPending")));
            createdIds.Add(saved.Id);

            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.RetractAsync(saved.Id));
        }

        [Fact]
        public async Task RetractWithNoApprovedBaselineThrowsApprovalRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("RetractNoBaseline")));
            createdIds.Add(saved.Id);

            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 1, tenantRegistryId);

            await Assert.ThrowsAsync<ApprovalRefusedException>(() => service.RetractAsync(saved.Id));
        }

        [Fact]
        public async Task RetractOfRejectedEditRestoresLastApprovedContentAndStampsTheRejectionRetractedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var insertDto = NewDto(modelId, UniqueName("RetractEdit"));
            insertDto.Active = false;
            var saved = await service.InsertAsync(insertDto);
            createdIds.Add(saved.Id);

            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 1, tenantRegistryId);

            var updateDto = NewDto(modelId, saved.Name);
            updateDto.Id = saved.Id;
            updateDto.Active = true;
            await service.UpdateAsync(updateDto);

            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 2, tenantRegistryId);

            var retracted = await service.RetractAsync(saved.Id);

            retracted.Version.Should().Be(3);
            retracted.Active.Should().Be(0, "retracting abandons the rejected edit and restores the last " +
                                            "approved content");
            retracted.Name.Should().Be(saved.Name);

            var rejectionRow = await dbContext.EntityApproval.FirstAsync(w =>
                w.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelResponseElevationRange
                && w.EntityId == saved.Id && w.EntityVersion == 2
                && w.StateId == (int)EntityApprovalState.Rejected);
            rejectionRow.ReturnedDate.Should().NotBeNull();
            rejectionRow.ReturnedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task RetractOfRejectedDeleteUndeletesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("RetractDelete")));
            createdIds.Add(saved.Id);

            await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 1, tenantRegistryId);

            await service.DeleteAsync(saved.Id);
            await EntityApprovalSeed.RejectAsync(dbContext, EntityApprovalKind.EntityAnalysisModelResponseElevationRange,
                saved.Id, 2, tenantRegistryId);

            var retracted = await service.RetractAsync(saved.Id);

            retracted.Version.Should().Be(3);
            retracted.Deleted.GetValueOrDefault().Should().Be(0,
                "retracting a rejected delete undeletes the record");
        }

        [Fact]
        public async Task ActiveAndLockedRoundTripBoolToByteAndBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("ActiveLocked"));
            dto.Active = true;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched!.Active.Should().BeTrue();
            fetched.Locked.Should().BeFalse();
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
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("LogInsert")));
            createdIds.Add(saved.Id);

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
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Gated")));
            createdIds.Add(saved.Id);

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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelResponseElevationRange.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Metric")));
            createdIds.Add(saved.Id);

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

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Reactive")));
            createdIds.Add(saved.Id);

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

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelId, "")));

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
                "EntityAnalysisModelResponseElevationRangeList", "EntityAnalysisModelResponseElevationRangeGet",
                "EntityAnalysisModelResponseElevationRangeGetByEntityAnalysisModelId",
                "EntityAnalysisModelResponseElevationRangeCreate", "EntityAnalysisModelResponseElevationRangeUpdate",
                "EntityAnalysisModelResponseElevationRangeDelete", "EntityAnalysisModelResponseElevationRangeResubmit", "EntityAnalysisModelResponseElevationRangeRetract"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(modelId, UniqueName($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        [Fact]
        public async Task OverlappingRangeWithinSameModelIsRejectedNamingTheClashingRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = NewDto(modelId, UniqueName("First"));
            var saved = await service.InsertAsync(first);
            createdIds.Add(saved.Id);

            var overlapping = NewDto(modelId, UniqueName("Second"));
            overlapping.Minimum = first.Maximum;
            overlapping.Maximum = first.Maximum + 3;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(overlapping));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RangeOverlaps" && e.ErrorMessage.Contains(first.Name!));
        }

        [Fact]
        public async Task AdjacentRangesAndTheSameLevelsInAnotherModelAreAcceptedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var otherModelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = NewDto(modelId, UniqueName("Low"));
            createdIds.Add((await service.InsertAsync(first)).Id);

            var adjacent = NewDto(modelId, UniqueName("High"));
            adjacent.Minimum = first.Maximum + 1;
            adjacent.Maximum = first.Maximum + 4;
            createdIds.Add((await service.InsertAsync(adjacent)).Id);

            var elsewhere = NewDto(otherModelId, UniqueName("Other"));
            elsewhere.Minimum = first.Minimum;
            elsewhere.Maximum = first.Maximum;
            createdIds.Add((await service.InsertAsync(elsewhere)).Id);
        }

        [Fact]
        public async Task UpdatingARangeDoesNotClashWithItselfAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Self"));
            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            dto.Id = saved.Id;
            dto.Version = saved.Version.GetValueOrDefault();
            dto.Maximum += 2;

            var updated = await service.UpdateAsync(dto);

            updated.Maximum.Should().Be(dto.Maximum);
        }

        [Theory]
        [InlineData(-1, 5, "MinimumRange")]
        [InlineData(10, 9, "MaximumRange")]
        public async Task NegativeMinimumOrMaximumBelowMinimumIsRejectedAsync(int minimum, int maximum, string code)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Bounds"));
            dto.Minimum = minimum;
            dto.Maximum = maximum;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == code);
        }

        [Theory]
        [InlineData("http://169.254.169.254/latest/meta-data/")]
        [InlineData("http://10.0.0.5/hook")]
        [InlineData("http://127.0.0.1:9/hook")]
        [InlineData("http://localhost/hook")]
        [InlineData("http://[::1]/hook")]
        [InlineData("http://0x7f000001/hook")]
        public async Task AWebHookOnAnInternalHostIsRefusedAtSaveTimeUnlessAllowListedAsync(string url)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Internal"));
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpointTypeId = 1;
            dto.HttpEndpoint = url;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "HttpEndpointHostBlocked");

            var allowing = await EntityAnalysisModelResponseElevationRangeService.CreateAsync(dbContext,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp,
                TestDynamicEnvironment.Create(new Dictionary<string, string>
                {
                    [OutboundHostPolicy.AllowedCidrsKey] = "10.0.0.0/8,127.0.0.0/8,::1/128,169.254.0.0/16",
                    [OutboundHostPolicy.AllowedHostsKey] = "localhost"
                }));

            dto.Name = UniqueName("Allowed");
            var saved = await allowing.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task DeclineAndHoldTogetherIsRejectedButEitherAloneIsAcceptedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var both = NewDto(modelId, UniqueName("Both"));
            both.Decline = true;
            both.Hold = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(both));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DeclineAndHold");

            var decline = NewDto(modelId, UniqueName("Decline"));
            decline.Decline = true;
            var savedDecline = await service.InsertAsync(decline);
            createdIds.Add(savedDecline.Id);
            savedDecline.Decline.Should().Be(1);
            savedDecline.Hold.Should().Be(0);

            var hold = NewDto(modelId, UniqueName("Hold"));
            hold.Hold = true;
            var savedHold = await service.InsertAsync(hold);
            createdIds.Add(savedHold.Id);
            savedHold.Hold.Should().Be(1);
            savedHold.Decline.Should().Be(0);
        }

        [Fact]
        public async Task HookFieldsAreRequiredOnlyWhenTheirSwitchIsOnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Hooks"));
            dto.EnableHttpEndpoint = true;
            dto.HttpEndpointTypeId = 1;
            dto.EnableNotification = true;
            dto.NotificationTypeId = 1;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "HttpEndpointNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NotificationDestinationNotEmpty");

            dto.HttpEndpoint = "ftp://example.com/hook";
            dto.NotificationDestination = "name@example.com";

            var invalid = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            invalid.Result.Errors.Should().Contain(e => e.ErrorCode == "HttpEndpointInvalid");

            dto.HttpEndpoint = "https://example.com/hook";
            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
            saved.EnableHttpEndpoint.Should().Be(1);
            saved.EnableNotification.Should().Be(1);
        }

        [Fact]
        public async Task ColoursMustBeSixDigitHexAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Colour"));
            dto.ForeColor = "red";
            dto.BackColor = "#12345";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ForeColorInvalid");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "BackColorInvalid");
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdLabelsEachRangeWithItsNameAndLevelsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var single = NewDto(modelId, UniqueName("Single"));
            single.Active = true;
            single.Maximum = single.Minimum;
            createdIds.Add((await service.InsertAsync(single)).Id);

            var band = NewDto(modelId, UniqueName("Band"));
            band.Active = true;
            createdIds.Add((await service.InsertAsync(band)).Id);

            var listed = await service.GetByEntityAnalysisModelIdAsync(modelId);

            listed.Should().Contain(d => d.Label == $"{single.Name} ({single.Minimum})");
            listed.Should().Contain(d => d.Label == $"{band.Name} ({band.Minimum}-{band.Maximum})");
        }

        [Fact]
        public async Task RepositoryFindsTheActiveRangeContainingALevelAndNoneOutsideAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var active = NewDto(modelId, UniqueName("Live"));
            active.Active = true;
            createdIds.Add((await service.InsertAsync(active)).Id);

            var inactive = NewDto(modelId, UniqueName("Dormant"));
            inactive.Active = false;
            createdIds.Add((await service.InsertAsync(inactive)).Id);

            var repository = new EntityAnalysisModelResponseElevationRangeRepository(dbContext,
                fx.Seed.UserWithPermission);

            (await repository.GetActiveContainingAsync(modelId, active.Minimum + 2)).Should().NotBeNull();
            (await repository.GetActiveContainingAsync(modelId, active.Maximum + 1)).Should().BeNull();
            (await repository.GetActiveContainingAsync(modelId, inactive.Minimum)).Should().BeNull();
        }
    }
}
