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
using Jube.Dto.Repository.RoleRegistryPermission;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.RoleRegistryPermission;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using RoleRegistryPermissionService = Jube.Service.Repository.RoleRegistryPermission.RoleRegistryPermissionService;

namespace Jube.Test.Service.Repository.RoleRegistryPermission
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RoleRegistryPermissionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int SelfPermissionSpecificationId = 36;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdGrantIds = [];
        private readonly List<int> createdRoleRegistryIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.RoleRegistryPermissionVersion>()
                .Where(w => createdGrantIds.Contains(w.RoleRegistryPermissionId)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => createdGrantIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.RoleRegistryPermissionVersion>()
                .Where(w => createdRoleRegistryIds.Contains(w.RoleRegistryId ?? -1)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => createdRoleRegistryIds.Contains(w.RoleRegistryId ?? -1)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<RoleRegistryPermissionService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RoleRegistryPermissionService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> ResolveTenantRegistryIdAsync(DbContext dbContext, string userName)
        {
            return dbContext.UserInTenant.Where(w => w.User == userName)
                .Select(w => w.TenantRegistryId).FirstAsync();
        }

        private static async Task<int> ResolveRoleRegistryIdAsync(DbContext dbContext, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry.Where(w => w.Name == userName)
                .Select(w => w.RoleRegistryGuid).FirstAsync();
            return await dbContext.RoleRegistry.Where(w => w.Guid == roleRegistryGuid)
                .Select(w => w.Id).FirstAsync();
        }

        private async Task EnsureActorCanManageRoleRegistryPermissionAsync(DbContext dbContext, string userName)
        {
            var actorRoleRegistryId = await ResolveRoleRegistryIdAsync(dbContext, userName);

            var alreadyGranted = await dbContext.RoleRegistryPermission.AnyAsync(w =>
                w.RoleRegistryId == actorRoleRegistryId
                && w.PermissionSpecificationId == SelfPermissionSpecificationId
                && (w.Deleted == 0 || w.Deleted == null));

            if (alreadyGranted)
            {
                return;
            }

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistryPermission
            {
                Guid = Guid.NewGuid(),
                RoleRegistryId = actorRoleRegistryId,
                PermissionSpecificationId = SelfPermissionSpecificationId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow,
            });

            createdGrantIds.Add(id);
        }

        private async Task<int> CreateTargetRoleRegistryAsync(DbContext dbContext, int tenantRegistryId)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = Guid.NewGuid(),
                Name = $"{DatabaseFixture.Prefix}TargetRole{Guid.NewGuid():N}"[..40],
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow,
            });

            createdRoleRegistryIds.Add(id);
            return id;
        }

        private static RoleRegistryPermissionDto ValidDto(int roleRegistryId, int permissionSpecificationId = 1) =>
            new()
            {
                RoleRegistryId = roleRegistryId,
                PermissionSpecificationId = permissionSpecificationId,
                Active = true,
                Locked = false,
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
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(targetRoleRegistryId)));

            (await dbContext.RoleRegistryPermission.AnyAsync(w => w.RoleRegistryId == targetRoleRegistryId))
                .Should().BeFalse();
        }

        [Fact]
        public async Task UpdateWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            created.Active = false;

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task InsertPersistsAndDropsIdentityFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(targetRoleRegistryId);
            dto.Id = 999999;
            dto.CreatedUser = "someone-else";

            var saved = await service.InsertAsync(dto);
            createdGrantIds.Add(saved.Id);

            saved.Id.Should().NotBe(999999);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext,
                fx.Seed.UserWithPermissionNoApproveByReview);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            await Task.Delay(50);
            var updaterService = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            created.Active = false;
            var updated = await updaterService.UpdateAsync(created);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(10));
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermissionNoApproveByReview);
            updated.UpdatedDate.Should().NotBeNull();
            updated.Active.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            created.CreatedUser = "tampered";
            created.Version = 999;
            var updated = await service.UpdateAsync(created);

            updated.CreatedUser.Should().NotBe("tampered");
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task UpdateWithUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(targetRoleRegistryId);
            dto.Id = 999999;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(targetRoleRegistryId);
            dto.Locked = true;
            var created = await service.InsertAsync(dto);
            createdGrantIds.Add(created.Id);

            created.Active = false;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.RoleRegistryPermission.Where(w => w.Id == created.Id)
                .Set(s => s.ImportId, importId).UpdateAsync();

            try
            {
                created.Active = false;
                await service.UpdateAsync(created);

                var persistedImportId = await dbContext.RoleRegistryPermission.Where(w => w.Id == created.Id)
                    .Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.RoleRegistryPermission.Where(w => w.Id == created.Id)
                    .Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Data.Poco.Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task DeleteRemovesRowAndSubsequentGetReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999999));
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserTenantB);
            var tenantBId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantBId);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var created = await owner.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var list = await service.GetAsync();

            list.Should().NotContain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task InsertForRoleRegistryInAnotherTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserTenantB);
            var tenantBId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantBId);

            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(targetRoleRegistryId)));
        }

        [Fact]
        public async Task UpdateForRowInAnotherTenantThrowsValidationFailedOnTheReferentialCheckAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserTenantB);
            var tenantBId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantBId);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var created = await owner.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            created.Active = false;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task InsertWithZeroRoleRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(0)));
        }

        [Fact]
        public async Task InsertWithNonExistentRoleRegistryIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(999999)));
        }

        [Fact]
        public async Task InsertWithZeroPermissionSpecificationIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(targetRoleRegistryId, 0)));
        }

        [Fact]
        public async Task InsertWithNonExistentPermissionSpecificationIdThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(targetRoleRegistryId, 999999)));
        }

        [Fact]
        public async Task InsertWithPermissionAlreadyGrantedToTheSameRoleThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(ValidDto(targetRoleRegistryId, 2));
            createdGrantIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(targetRoleRegistryId, 2)));
        }

        [Fact]
        public async Task UpdateOfSameRowWithSamePermissionDoesNotTriggerDuplicateValidationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId, 3));
            createdGrantIds.Add(created.Id);

            created.Active = false;
            var updated = await service.UpdateAsync(created);

            updated.PermissionSpecificationId.Should().Be(3);
        }

        [Fact]
        public async Task InsertRoundTripsActiveAndLockedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = ValidDto(targetRoleRegistryId, 4);
            dto.Active = false;
            dto.Locked = false;

            var saved = await service.InsertAsync(dto);
            createdGrantIds.Add(saved.Id);

            saved.Active.Should().BeFalse();
            saved.RoleRegistryId.Should().Be(targetRoleRegistryId);
            saved.PermissionSpecificationId.Should().Be(4);
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("RoleRegistryPermission");
            change.Kind.Should().Be(ServiceChangeKind.Created);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(999999)));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task DeletePublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var owner = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var created = await owner.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, serviceChangeBus: bus);
            await service.DeleteAsync(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("RoleRegistryPermission");
            change.Kind.Should().Be(ServiceChangeKind.Deleted);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
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
            await EnsureActorCanManageRoleRegistryPermissionAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryId = await ResolveTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var targetRoleRegistryId = await CreateTargetRoleRegistryAsync(dbContext, tenantRegistryId);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            var created = await service.InsertAsync(ValidDto(targetRoleRegistryId));
            createdGrantIds.Add(created.Id);

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public void CatalogueRegistersUniqueRoleRegistryPermissionToolNames()
        {
            var tools = ServiceToolCatalogue.All;

            var names = new[]
            {
                "RoleRegistryPermissionList", "RoleRegistryPermissionGet", "RoleRegistryPermissionCreate",
                "RoleRegistryPermissionUpdate", "RoleRegistryPermissionDelete",
            };

            foreach (var name in names)
            {
                tools.Should().ContainSingle(t => t.Name == name);
            }

            tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
        }
    }
}