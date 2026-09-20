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
using Jube.Dto.Repository.RoleRegistry;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.RoleRegistry;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.RoleRegistry;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.RoleRegistry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RoleRegistryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.RoleRegistryVersion>()
                .Where(w => createdRoleIds.Contains(w.RoleRegistryId)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<RoleRegistryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RoleRegistryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task<string> CreateUserWithRoleRegistryPermissionAsync(DbContext dbContext,
            int[]? specs = null, int? tenantId = null)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            if (tenantId is null)
            {
                tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
                {
                    Name = $"{DatabaseFixture.Prefix}RRTenant{suffix}",
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Landlord = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                }).ConfigureAwait(false);
                createdTenantIds.Add(tenantId.Value);
            }

            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RRRole{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId.Value,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            }).ConfigureAwait(false);
            createdRoleIds.Add(roleId);

            foreach (var spec in specs ?? [34])
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix,
                }).ConfigureAwait(false);
            }

            var userName = $"{DatabaseFixture.Prefix}RRUser{suffix}";
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            }).ConfigureAwait(false);
            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantId.Value,
            }).ConfigureAwait(false);

            return userName;
        }

        private static RoleRegistryDto ValidDto(string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Role{Guid.NewGuid():N}"[..40],
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}Role{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(name)));

            (await dbContext.RoleRegistry.AnyAsync(w => w.Name == name)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            created.Name += "x";

            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task InsertPersistsAndDropsIdentityFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Id = 999999;
            dto.Guid = Guid.NewGuid();
            dto.CreatedUser = "someone-else";

            var saved = await service.InsertAsync(dto);
            createdRoleIds.Add(saved.Id);

            saved.Id.Should().NotBe(999999);
            saved.Guid.Should().NotBe(dto.Guid);
            saved.CreatedUser.Should().Be(privilegedUser);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdateIncrementsVersionPreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var privilegedUserTenantId = await dbContext.UserInTenant
                .Where(w => w.User == privilegedUser).Select(w => w.TenantRegistryId).FirstAsync();
            var otherPrivilegedUser = await CreateUserWithRoleRegistryPermissionAsync(
                dbContext, tenantId: privilegedUserTenantId);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            await Task.Delay(50);
            var updaterService = await BuildServiceAsync(dbContext, otherPrivilegedUser);
            created.Name += "-updated";
            var updated = await updaterService.UpdateAsync(created);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(created.CreatedDate.Required(), TimeSpan.FromMilliseconds(10));
            updated.UpdatedUser.Should().Be(otherPrivilegedUser);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateTamperingIdentityAndAuditFieldsHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

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
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Id = 999999;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Locked = true;
            var created = await service.InsertAsync(dto);
            createdRoleIds.Add(created.Id);

            created.Name += "x";
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 1,
                CreatedUser = privilegedUser,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.RoleRegistry.Where(w => w.Id == created.Id)
                .Set(s => s.ImportId, importId).UpdateAsync();

            try
            {
                created.Name += "-updated";
                await service.UpdateAsync(created);

                var persistedImportId = await dbContext.RoleRegistry.Where(w => w.Id == created.Id)
                    .Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.RoleRegistry.Where(w => w.Id == created.Id)
                    .Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Data.Poco.Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task DeleteRemovesRowAndSubsequentGetReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            (await service.GetByIdAsync(created.Id)).Should().BeNull();
        }

        [Fact]
        public async Task DeleteSetsDeletedUserAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            await service.DeleteAsync(created.Id);

            var deletedUser = await dbContext.RoleRegistry
                .Where(w => w.Id == created.Id).Select(w => w.DeletedUser).FirstAsync();
            deletedUser.Should().Be(privilegedUser);
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(999999));
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var list = await service.GetAsync();

            list.Should().NotContain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task UpdateForRowInAnotherTenantThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUserTenantA = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var privilegedUserTenantB = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUserTenantA);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var service = await BuildServiceAsync(dbContext, privilegedUserTenantB);
            created.Name += "x";

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(created));
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithNullNameThrowsValidationFailedAndDoesNotThrowNullReferenceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Name = null;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithOverLongNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto(new string('a', 257));

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var name = $"{DatabaseFixture.Prefix}Role{Guid.NewGuid():N}"[..40];
            var first = await service.InsertAsync(ValidDto(name));
            createdRoleIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(name)));
        }

        [Fact]
        public async Task InsertWithDuplicateNameAcrossTenantsSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUserTenantA = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var privilegedUserTenantB = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var serviceA = await BuildServiceAsync(dbContext, privilegedUserTenantA);
            var serviceB = await BuildServiceAsync(dbContext, privilegedUserTenantB);
            var name = $"{DatabaseFixture.Prefix}Role{Guid.NewGuid():N}"[..40];

            var first = await serviceA.InsertAsync(ValidDto(name));
            createdRoleIds.Add(first.Id);
            var second = await serviceB.InsertAsync(ValidDto(name));
            createdRoleIds.Add(second.Id);

            second.Id.Should().NotBe(first.Id);
        }

        [Fact]
        public async Task InsertRoundTripsActiveAndLockedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Active = false;
            dto.Locked = false;

            var saved = await service.InsertAsync(dto);
            createdRoleIds.Add(saved.Id);

            saved.Active.Should().BeFalse();
        }

        [Fact]
        public async Task InsertDropsUnpersistedDescriptionFieldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var dto = ValidDto();
            dto.Description = "should not be persisted";

            var saved = await service.InsertAsync(dto);
            createdRoleIds.Add(saved.Id);

            saved.Description.Should().BeNull();
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
        public async Task ListAsyncClampsTakeAndPaginatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, privilegedUser);
            var first = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(first.Id);
            var second = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(second.Id);

            var allIds = (await service.GetAsync()).Select(d => d.Id).OrderBy(id => id).ToList();

            var page = await service.ListAsync(take: 1);
            page.Items.Should().HaveCount(1);
            page.Items[0].Id.Should().Be(allIds[0]);

            var secondPage = await service.ListAsync(take: 1, afterId: page.Items[0].Id);
            secondPage.Items.Should().HaveCount(1);
            secondPage.Items[0].Id.Should().Be(allIds[1]);

            var overLarge = await service.ListAsync(take: 100000);
            overLarge.Items.Count.Should().BeLessOrEqualTo(200);
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, privilegedUser, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("RoleRegistry");
            change.Kind.Should().Be(ServiceChangeKind.Created);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, privilegedUser, serviceChangeBus: bus);
            var dto = ValidDto();
            dto.Name = "";

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdatePublishesUpdatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, privilegedUser, serviceChangeBus: bus);
            created.Name += "-updated";
            await service.UpdateAsync(created);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("RoleRegistry");
            change.Kind.Should().Be(ServiceChangeKind.Updated);
            change.EntityId.Should().Be(created.Id);
        }

        [Fact]
        public async Task DeletePublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var owner = await BuildServiceAsync(dbContext, privilegedUser);
            var created = await owner.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, privilegedUser, serviceChangeBus: bus);
            await service.DeleteAsync(created.Id);

            var change = bus.Published.Should().ContainSingle().Subject;
            change.Area.Should().Be("RoleRegistry");
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

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto()));

            testLog.Entries.Should().Contain(e => e.Level == "WARN");
            testLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertWritesOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var privilegedUser = await CreateUserWithRoleRegistryPermissionAsync(dbContext);
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, privilegedUser, auditLog: auditLog);

            var created = await service.InsertAsync(ValidDto());
            createdRoleIds.Add(created.Id);

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public async Task ListWritesOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle(e => e.Level == "INFO");
        }

        [Fact]
        public void CatalogueRegistersUniqueRoleRegistryToolNames()
        {
            var tools = ServiceToolCatalogue.All;

            var names = new[]
            {
                "RoleRegistryList", "RoleRegistryGet", "RoleRegistryCreate", "RoleRegistryUpdate",
                "RoleRegistryDelete",
            };

            foreach (var name in names)
            {
                tools.Should().ContainSingle(t => t.Name == name);
            }

            tools.Select(t => t.Name).Should().OnlyHaveUniqueItems();
        }
    }
}