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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.VisualisationRegistry;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.VisualisationRegistry;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using VisualisationRegistryService = Jube.Service.Repository.VisualisationRegistry.VisualisationRegistryService;

namespace Jube.Test.Service.Repository.VisualisationRegistry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int AdminPermissionSpecification = 31;
        private const int ShowInDirectoryPermissionSpecification = 28;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdRegistryIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        private string userAdminA = string.Empty;
        private string userAdminB = string.Empty;
        private string userShowInDirectoryOnlyA = string.Empty;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var tenantAId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            var tenantBId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserTenantB)
                .Select(w => w.TenantRegistryId).FirstAsync();

            userAdminA = await SeedActorAsync(dbContext, tenantAId, "AdminA", [AdminPermissionSpecification]);
            userAdminB = await SeedActorAsync(dbContext, tenantBId, "AdminB", [AdminPermissionSpecification]);
            userShowInDirectoryOnlyA = await SeedActorAsync(dbContext, tenantAId, "DirectoryA",
                [ShowInDirectoryPermissionSpecification]);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var visualisationRegistryGuids1 = dbContext.VisualisationRegistry
                .Where(s => createdRegistryIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryRole>()
                .Where(w => visualisationRegistryGuids1.Contains(w.VisualisationRegistryGuid)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryVersion>()
                .Where(w => createdRegistryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();
            await dbContext.VisualisationRegistry.Where(w => createdRegistryIds.Contains(w.Id)).DeleteAsync();

            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<string> SeedActorAsync(DbContext dbContext, int tenantRegistryId, string label,
            int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RoleVisReg{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            });
            createdRoleRegistryIds.Add(roleId);

            foreach (var spec in specs)
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
                });
            }

            var userName = $"{DatabaseFixture.Prefix}UserVisReg{label}{suffix}";
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
            });

            await dbContext.InsertAsync(new Data.Poco.UserInTenant
            {
                User = userName,
                TenantRegistryId = tenantRegistryId,
            });

            createdUserNames.Add(userName);
            return userName;
        }

        private static Task<VisualisationRegistryService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return VisualisationRegistryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private async Task GrantRegistryRoleAsync(DbContext dbContext, Guid visualisationRegistryGuid,
            string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryRole
            {
                VisualisationRegistryGuid = visualisationRegistryGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
            });
        }

        private static VisualisationRegistryDto ValidDto(string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}"[..40],
            Active = true,
            Locked = false,
            ShowInDirectory = false,
            Columns = 6,
            ColumnWidth = 300,
            RowHeight = 300,
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
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var name = $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}"[..40];

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto(name)));

            var stillExists = await dbContext.VisualisationRegistry.AnyAsync(w => w.Name == name);
            stillExists.Should().BeFalse();
        }

        [Fact]
        public async Task ListByShowInDirectoryRequiresExactSpecNotSatisfiedByAdminSpecAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByShowInDirectoryAsync());
        }

        [Fact]
        public async Task InsertRequiresAdminSpecNotSatisfiedByShowInDirectorySpecAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userShowInDirectoryOnlyA);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(ValidDto()));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());

            log.Entries.Should()
                .Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneWithAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var saved = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(userAdminA);
            saved.CreatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task InsertWithBlankNameThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto("")));
        }

        [Fact]
        public async Task InsertWithNameOverMaxLengthThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto(new string('a', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));

            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task InsertWithNameAtMaxLengthSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var name = $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}".PadRight(256, 'a')[..256];
            var dto = ValidDto(name);

            var saved = await service.InsertAsync(dto);
            createdRegistryIds.Add(saved.Id);

            saved.Name.Should().Be(name);
        }

        [Fact]
        public async Task InsertWithDuplicateNameInSameTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var name = $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}"[..40];

            var first = await service.InsertAsync(ValidDto(name));
            createdRegistryIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto(name)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task InsertWithDuplicateNameDifferentCasingInSameTenantThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var name = $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}"[..40];

            var first = await service.InsertAsync(ValidDto(name));
            createdRegistryIds.Add(first.Id);

            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(ValidDto(name.ToUpperInvariant())));
        }

        [Fact]
        public async Task InsertWithSameNameInDifferentTenantSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var serviceA = await BuildServiceAsync(dbContext, userAdminA);
            var serviceB = await BuildServiceAsync(dbContext, userAdminB);
            var name = $"{DatabaseFixture.Prefix}VisReg{Guid.NewGuid():N}"[..40];

            var first = await serviceA.InsertAsync(ValidDto(name));
            createdRegistryIds.Add(first.Id);
            var second = await serviceB.InsertAsync(ValidDto(name));
            createdRegistryIds.Add(second.Id);

            second.Id.Should().NotBe(first.Id);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InsertWithNonPositiveColumnsThrowsValidationFailedAsync(int columns)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.Columns = columns;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == nameof(VisualisationRegistryDto.Columns));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InsertWithNonPositiveColumnWidthThrowsValidationFailedAsync(int columnWidth)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.ColumnWidth = columnWidth;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == nameof(VisualisationRegistryDto.ColumnWidth));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task InsertWithNonPositiveRowHeightThrowsValidationFailedAsync(int rowHeight)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.RowHeight = rowHeight;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == nameof(VisualisationRegistryDto.RowHeight));
        }

        [Fact]
        public async Task InsertIgnoresTamperedIdentityAndAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.Id = 999_999;
            dto.Guid = Guid.NewGuid();
            dto.CreatedUser = "tampered";
            dto.CreatedDate = DateTimeOffset.UtcNow.AddYears(-5);
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdRegistryIds.Add(saved.Id);

            saved.Id.Should().NotBe(999_999);
            saved.Guid.Should().NotBe(dto.Guid);
            saved.CreatedUser.Should().Be(userAdminA);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePreservesCreatedFieldsAndSetsUpdatedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            var originalCreatedUser = created.CreatedUser;
            var originalCreatedDate = created.CreatedDate;

            await Task.Delay(50);

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;
            var updated = await service.UpdateAsync(toUpdate);

            updated.Version.Should().Be(2);
            updated.CreatedUser.Should().Be(originalCreatedUser);
            updated.CreatedDate.Should().BeCloseTo(originalCreatedDate.Required(), TimeSpan.FromSeconds(1));
            updated.UpdatedUser.Should().Be(userAdminA);
            updated.UpdatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateWritesAVersionAuditRowCarryingTheDeletedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;
            await service.UpdateAsync(toUpdate);

            var audit = await dbContext.GetTable<Data.Poco.VisualisationRegistryVersion>()
                .Where(w => w.VisualisationRegistryId == created.Id).FirstAsync();

            audit.Version.Should().Be(created.Version);
            audit.Name.Should().Be(created.Name);
            (audit.Deleted is null or 0).Should().BeTrue();
            audit.DeletedUser.Should().BeNull();
            audit.DeletedDate.Should().BeNull();
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var tenantAId = await dbContext.UserInTenant.Where(w => w.User == userAdminA)
                .Select(w => w.TenantRegistryId).FirstAsync();

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantAId,
                CreatedUser = userAdminA,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.VisualisationRegistry.Where(w => w.Id == created.Id)
                .Set(w => w.ImportId, importId).UpdateAsync();

            try
            {
                var toUpdate = ValidDto(created.Name);
                toUpdate.Id = created.Id;
                await service.UpdateAsync(toUpdate);

                var persistedImportId = await dbContext.VisualisationRegistry.Where(w => w.Id == created.Id)
                    .Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.VisualisationRegistry.Where(w => w.Id == created.Id)
                    .Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Data.Poco.Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task UpdateOfUnknownIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.Id = int.MaxValue;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            await dbContext.VisualisationRegistry.Where(w => w.Id == created.Id)
                .Set(w => w.Locked, (byte)1).UpdateAsync();

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(toUpdate));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            await service.DeleteAsync(created.Id);

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(toUpdate));
        }

        [Fact]
        public async Task UpdateOfCrossTenantRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ownerService = await BuildServiceAsync(dbContext, userAdminA);
            var created = await ownerService.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, userAdminB);
            var dto = ValidDto(created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenantService.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateIgnoresTamperedIdentityAndAuditFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;
            toUpdate.Guid = Guid.NewGuid();
            toUpdate.CreatedUser = "tampered";
            toUpdate.Version = 999;

            var updated = await service.UpdateAsync(toUpdate);

            updated.Guid.Should().Be(created.Guid);
            updated.CreatedUser.Should().Be(created.CreatedUser);
            updated.Version.Should().Be(2);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userAdminA, serviceChangeBus: bus);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
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
            var service = await BuildServiceAsync(dbContext, userAdminA);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue));
        }

        [Fact]
        public async Task DeleteOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            await dbContext.VisualisationRegistry.Where(w => w.Id == created.Id)
                .Set(w => w.Locked, (byte)1).UpdateAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task DeleteOfAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            await service.DeleteAsync(created.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(created.Id));
        }

        [Fact]
        public async Task GetByIdCrossTenantReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ownerService = await BuildServiceAsync(dbContext, userAdminA);
            var created = await ownerService.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var otherTenantService = await BuildServiceAsync(dbContext, userAdminB);
            var result = await otherTenantService.GetByIdAsync(created.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ListNeverContainsAnotherTenantsRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var serviceA = await BuildServiceAsync(dbContext, userAdminA);
            var created = await serviceA.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var serviceB = await BuildServiceAsync(dbContext, userAdminB);
            var resultsB = await serviceB.GetAsync();

            resultsB.Should().NotContain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task GetByGuidActiveOnlyWithoutRoleGrantReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var result = await service.GetByGuidActiveOnlyAsync(created.Guid);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByGuidActiveOnlyWithRoleGrantReturnsRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            await GrantRegistryRoleAsync(dbContext, created.Guid, userAdminA);

            var result = await service.GetByGuidActiveOnlyAsync(created.Guid);

            result.Should().NotBeNull();
            result.Required().Id.Should().Be(created.Id);
        }

        [Fact]
        public async Task GetByGuidActiveOnlyExcludesInactiveRowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.Active = false;
            var created = await service.InsertAsync(dto);
            createdRegistryIds.Add(created.Id);

            await GrantRegistryRoleAsync(dbContext, created.Guid, userAdminA);

            var result = await service.GetByGuidActiveOnlyAsync(created.Guid);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByGuidActiveOnlyCrossTenantReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var ownerService = await BuildServiceAsync(dbContext, userAdminA);
            var created = await ownerService.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            await GrantRegistryRoleAsync(dbContext, created.Guid, userAdminA);

            var otherTenantService = await BuildServiceAsync(dbContext, userAdminB);
            var result = await otherTenantService.GetByGuidActiveOnlyAsync(created.Guid);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ListByShowInDirectoryRequiresRoleGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var adminService = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.ShowInDirectory = true;
            var created = await adminService.InsertAsync(dto);
            createdRegistryIds.Add(created.Id);

            var directoryService = await BuildServiceAsync(dbContext, userShowInDirectoryOnlyA);

            var beforeGrant = await directoryService.GetByShowInDirectoryAsync();
            beforeGrant.Should().NotContain(d => d.Id == created.Id);

            await GrantRegistryRoleAsync(dbContext, created.Guid, userShowInDirectoryOnlyA);

            var afterGrant = await directoryService.GetByShowInDirectoryAsync();
            afterGrant.Should().Contain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListByShowInDirectoryExcludesRowsNotFlaggedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var adminService = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.ShowInDirectory = false;
            var created = await adminService.InsertAsync(dto);
            createdRegistryIds.Add(created.Id);

            await GrantRegistryRoleAsync(dbContext, created.Guid, userShowInDirectoryOnlyA);
            var directoryService = await BuildServiceAsync(dbContext, userShowInDirectoryOnlyA);

            var results = await directoryService.GetByShowInDirectoryAsync();

            results.Should().NotContain(d => d.Id == created.Id);
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task RoundTripPreservesLayoutAndFlagFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var dto = ValidDto();
            dto.Active = true;
            dto.Locked = false;
            dto.ShowInDirectory = true;
            dto.Columns = 9;
            dto.ColumnWidth = 275;
            dto.RowHeight = 190;

            var saved = await service.InsertAsync(dto);
            createdRegistryIds.Add(saved.Id);
            var fetched = await service.GetByIdAsync(saved.Id);
            fetched = fetched.Required();

            fetched.Should().NotBeNull();
            fetched.Active.Should().BeTrue();
            fetched.Locked.Should().BeFalse();
            fetched.ShowInDirectory.Should().BeTrue();
            fetched.Columns.Should().Be(9);
            fetched.ColumnWidth.Should().Be(275);
            fetched.RowHeight.Should().Be(190);
        }

        [Fact]
        public async Task GetByIdForMissingRowReturnsNullAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            var result = await service.GetByIdAsync(int.MaxValue);

            result.Should().BeNull();
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userAdminA, log);

            var saved = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, userAdminA, log);

            var saved = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userAdminA, log);

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
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var saved = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "VisualisationRegistry.Insert").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);
            var saved = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Insert");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, userAdminA, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task ListDoesNotPublishAChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userAdminA, serviceChangeBus: bus);

            await service.GetAsync();

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task FailedInsertPublishesNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userAdminA, serviceChangeBus: bus);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto("")));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task SuccessfulUpdatePublishesExactlyOneUpdatedEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userAdminA, serviceChangeBus: bus);
            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            bus.Published.Clear();

            var toUpdate = ValidDto(created.Name);
            toUpdate.Id = created.Id;
            await service.UpdateAsync(toUpdate);

            bus.Published.Should().ContainSingle(e => e.Kind == ServiceChangeKind.Updated && e.EntityId == created.Id);
        }

        [Fact]
        public async Task InsertWithBlankNameUsesFrenchMessageUnderFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, userAdminA);

                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(ValidDto("")));

                ex.Result.Errors.First(e => e.PropertyName == nameof(VisualisationRegistryDto.Name)).ErrorMessage
                    .Should().Be("Le nom est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }

        [Fact]
        public void CatalogueRegistersExpectedToolNamesForThisArea()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().Contain("VisualisationRegistryList");
            names.Should().Contain("VisualisationRegistryGet");
            names.Should().Contain("VisualisationRegistryGetByGuidActiveOnly");
            names.Should().Contain("VisualisationRegistryListByShowInDirectory");
            names.Should().Contain("VisualisationRegistryCreate");
            names.Should().Contain("VisualisationRegistryUpdate");
            names.Should().Contain("VisualisationRegistryDelete");
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, userAdminA);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(ValidDto());
                createdRegistryIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }
    }
}