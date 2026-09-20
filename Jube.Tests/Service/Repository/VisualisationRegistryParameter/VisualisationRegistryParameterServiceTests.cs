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
using Jube.Dto.Repository.VisualisationRegistryParameter;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.VisualisationRegistryParameter;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.VisualisationRegistryParameter.Models;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.VisualisationRegistryParameter
{
    using VisualisationRegistryParameterService =
        global::Jube.Service.Repository.VisualisationRegistryParameter.VisualisationRegistryParameterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryParameterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int VisualisationParameterAdministrationSpec = 32;
        private const int VisualisationDirectorySpec = 28;
        private const int ReadWriteCaseSpec = 1;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<int> createdVisualisationRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                var parameterGuid = await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                    .Where(w => w.Id == id).Select(w => (Guid?)w.Guid).FirstOrDefaultAsync();

                if (parameterGuid.HasValue)
                {
                    await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>()
                        .Where(w => w.VisualisationRegistryParameterGuid == parameterGuid.Value).DeleteAsync();
                }

                await dbContext.GetTable<VisualisationRegistryParameterVersion>()
                    .Where(w => w.VisualisationRegistryParameterId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var visualisationRegistryId in createdVisualisationRegistryIds)
            {
                var guid = await dbContext.VisualisationRegistry.Where(w => w.Id == visualisationRegistryId)
                    .Select(w => (Guid?)w.Guid).FirstOrDefaultAsync();

                if (guid.HasValue)
                {
                    await dbContext.GetTable<Data.Poco.VisualisationRegistryRole>()
                        .Where(w => w.VisualisationRegistryGuid == guid.Value).DeleteAsync();
                }

                await dbContext.GetTable<VisualisationRegistryVersion>()
                    .Where(w => w.VisualisationRegistryId == visualisationRegistryId).DeleteAsync();
                await dbContext.VisualisationRegistry.Where(w => w.Id == visualisationRegistryId).DeleteAsync();
            }

            foreach (var userName in createdUserNames)
            {
                await dbContext.UserInTenant.Where(w => w.User == userName).DeleteAsync();
                await dbContext.UserRegistry.Where(w => w.Name == userName).DeleteAsync();
            }

            foreach (var roleRegistryId in createdRoleRegistryIds)
            {
                await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleRegistryId).DeleteAsync();
                await dbContext.RoleRegistry.Where(w => w.Id == roleRegistryId).DeleteAsync();
            }

            foreach (var tenantId in createdTenantIds)
            {
                await dbContext.TenantRegistry.Where(w => w.Id == tenantId).DeleteAsync();
            }
        }

        private static Task<VisualisationRegistryParameterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return VisualisationRegistryParameterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<(int TenantId, string UserName)> CreateTenantUserWithPermissionsAsync(
            DbContext dbContext, int[] specs, string label)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Tenant{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}Role{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
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
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}User{label}{suffix}";
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
                CreatedUser = DatabaseFixture.Prefix
            });
            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });

            return (tenantId, userName);
        }

        private async Task<int> CreateParentVisualisationRegistryAsync(DbContext dbContext, string createdUser)
        {
            var repository = new VisualisationRegistryRepository(dbContext, createdUser);
            var visualisationRegistry = await repository.InsertAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}VisualisationRegistry{Guid.NewGuid():N}",
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);
            createdVisualisationRegistryIds.Add(visualisationRegistry.Id);

            return visualisationRegistry.Id;
        }

        private static async Task GrantVisualisationRegistryRoleAsync(DbContext dbContext, int visualisationRegistryId,
            string userName)
        {
            var visualisationRegistryGuid = await dbContext.VisualisationRegistry
                .Where(w => w.Id == visualisationRegistryId).Select(w => w.Guid).FirstAsync();
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryRole
            {
                VisualisationRegistryGuid = visualisationRegistryGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            });
        }

        private static async Task GrantVisualisationRegistryParameterRoleAsync(DbContext dbContext,
            Guid visualisationRegistryParameterGuid, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryParameterRole
            {
                VisualisationRegistryParameterGuid = visualisationRegistryParameterGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            });
        }

        private static VisualisationRegistryParameterDto NewDto(int visualisationRegistryId, string? name = null,
            int dataTypeId = 1, string? defaultValue = "Test")
        {
            return new VisualisationRegistryParameterDto
            {
                VisualisationRegistryId = visualisationRegistryId,
                Name = name ?? UniqueName("Param"),
                Active = true,
                Locked = false,
                DataTypeId = dataTypeId,
                DefaultValue = defaultValue,
                Required = true
            };
        }

        private static string UniqueName(string label)
        {
            var name = $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}";
            return name[..Math.Min(40, name.Length)];
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Insert");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(userName);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate.Required().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "GetAll");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, name));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);
        }

        [Fact]
        public async Task GetByVisualisationRegistryIdReturnsRowsForThatRegistryIncludingInactiveOrderedByIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "ByRegistry");
            var registryAId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var registryBId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dtoA1 = NewDto(registryAId, UniqueName("A1"));
            var savedA1 = await service.InsertAsync(dtoA1);
            createdIds.Add(savedA1.Id);

            var dtoA2 = NewDto(registryAId, UniqueName("A2"));
            dtoA2.Active = false;
            var savedA2 = await service.InsertAsync(dtoA2);
            createdIds.Add(savedA2.Id);

            var savedB = await service.InsertAsync(NewDto(registryBId, UniqueName("B")));
            createdIds.Add(savedB.Id);

            var forRegistryA = await service.GetByVisualisationRegistryIdAsync(registryAId);
            forRegistryA.Should().Contain(d => d.Id == savedA1.Id);
            forRegistryA.Should().Contain(d => d.Id == savedA2.Id);
            forRegistryA.Should().NotContain(d => d.Id == savedB.Id);
            forRegistryA.Select(d => d.Id).Should().BeInAscendingOrder();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(int.MaxValue - 1)]
        public async Task GetByVisualisationRegistryIdWithZeroOrNonExistentIdReturnsEmptyListAsync(
            int visualisationRegistryId)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Missing");
            var service = await BuildServiceAsync(dbContext, userName);

            var result = await service.GetByVisualisationRegistryIdAsync(visualisationRegistryId);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task UpdateIncrementsVersionSetsUpdatedUserPreservesCreatedUserAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Update");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Update")));
            createdIds.Add(saved.Id);
            var originalCreatedDate = saved.CreatedDate;

            var newName = UniqueName("UpdateNew");
            var dto = NewDto(visualisationRegistryId, newName, 2, "5");
            dto.Id = saved.Id;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.Name.Should().Be(newName);
            updated.DataTypeId.Should().Be(2);
            updated.DefaultValue.Should().Be("5");
            updated.CreatedUser.Should().Be(userName);
            updated.CreatedDate.Should().BeCloseTo(originalCreatedDate.Required(), TimeSpan.FromSeconds(1));
            updated.UpdatedUser.Should().Be(userName);
            updated.UpdatedDate.Should().NotBeNull();

            var auditRows = await dbContext.GetTable<VisualisationRegistryParameterVersion>()
                .Where(w => w.VisualisationRegistryParameterId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePreservesImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "ImportId");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("ImportId")));
            createdIds.Add(saved.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.ImportId, importId)
                .UpdateAsync();

            try
            {
                var dto = NewDto(visualisationRegistryId, saved.Name.Required());
                dto.Id = saved.Id;
                await service.UpdateAsync(dto);

                var persistedImportId = await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                    .Where(w => w.Id == saved.Id).Select(w => w.ImportId).FirstAsync();
                persistedImportId.Should().Be(importId);
            }
            finally
            {
                await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                    .Where(w => w.Id == saved.Id).Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromByVisualisationRegistryIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Delete");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byRegistry = await service.GetByVisualisationRegistryIdAsync(visualisationRegistryId);
            byRegistry.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var (_, ownerUserName) = await CreateTenantUserWithPermissionsAsync(writerDb,
                [VisualisationParameterAdministrationSpec], "ForbiddenOwner");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(writerDb, ownerUserName);
            var writer = await BuildServiceAsync(writerDb, ownerUserName);
            var seedRow = await writer.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var (_, noPermissionUserName) = await CreateTenantUserWithPermissionsAsync(dbContext, [], "NoPerm");
            var service = await BuildServiceAsync(dbContext, noPermissionUserName);

            var beforeCount = await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .CountAsync(w => w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByVisualisationRegistryIdAsync(visualisationRegistryId));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(visualisationRegistryId, seedRow.Name.Required());
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .CountAsync(w => w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix));
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
                VisualisationRegistryParameterService.CreateAsync(dbContext, userName, log, localizers,
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
        public async Task UserInTenantBCannotUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (_, ownerUserName) = await CreateTenantUserWithPermissionsAsync(ownerDb,
                [VisualisationParameterAdministrationSpec], "IsolationA");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(ownerDb, ownerUserName);
            var owner = await BuildServiceAsync(ownerDb, ownerUserName);
            var saved = await owner.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var (_, otherUserName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "IsolationB");
            var otherTenant = await BuildServiceAsync(dbContext, otherUserName);

            var updateDto = NewDto(visualisationRegistryId, saved.Name.Required());
            updateDto.Id = saved.Id;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => otherTenant.UpdateAsync(updateDto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdNotFound");

            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var byRegistry = await owner.GetByVisualisationRegistryIdAsync(visualisationRegistryId);
            byRegistry.Should().Contain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task GetByVisualisationRegistryIdAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (_, ownerUserName) = await CreateTenantUserWithPermissionsAsync(ownerDb,
                [VisualisationParameterAdministrationSpec], "TenantAOnlyOwner");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(ownerDb, ownerUserName);
            var owner = await BuildServiceAsync(ownerDb, ownerUserName);
            var saved = await owner.InsertAsync(NewDto(visualisationRegistryId, UniqueName("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var (_, otherUserName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "TenantAOnlyOther");
            var otherTenant = await BuildServiceAsync(dbContext, otherUserName);
            var forRegistryAsTenantB = await otherTenant.GetByVisualisationRegistryIdAsync(visualisationRegistryId);

            forRegistryAsTenantB.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "NameReq");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId);
            dto.Name = name;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(VisualisationRegistryParameterDto.Name) &&
                e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "NameMax");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, new string('n', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task NameDuplicateWithinRegistryIsRejectedButAcrossRegistriesIsAcceptedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "NameDup");
            var registryAId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var registryBId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var name = UniqueName("Dup");
            var saved = await service.InsertAsync(NewDto(registryAId, name));
            createdIds.Add(saved.Id);

            var duplicate = NewDto(registryAId, name);
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(duplicate));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var savedInOtherRegistry = await service.InsertAsync(NewDto(registryBId, name));
            createdIds.Add(savedInOtherRegistry.Id);
            savedInOtherRegistry.Name.Should().Be(name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(6)]
        public async Task InvalidDataTypeIdIsRejectedAsync(int dataTypeId)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "BadDataType");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("BadDataType"), dataTypeId);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DataTypeIdInvalid");
        }

        [Theory]
        [InlineData(1, "Test")]
        [InlineData(2, "42")]
        [InlineData(3, "1.5")]
        [InlineData(4, "20260101")]
        [InlineData(5, "1")]
        public async Task ValidDataTypeIdsAreAcceptedAsync(int dataTypeId, string defaultValue)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "GoodDataType");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("GoodDataType"), dataTypeId, defaultValue);

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.DataTypeId.Should().Be(dataTypeId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task DefaultValueRequiredRejectsNullEmptyOrWhitespaceAsync(string? defaultValue)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "DefaultReq");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("DefaultReq"), 1, defaultValue);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(VisualisationRegistryParameterDto.DefaultValue) &&
                e.ErrorCode == "DefaultValueNotEmpty");
        }

        [Theory]
        [InlineData(2, "not-a-number")]
        [InlineData(3, "not-a-float")]
        [InlineData(4, "not-a-day-offset")]
        [InlineData(5, "not-a-byte")]
        public async Task DefaultValueNotParseableForDataTypeIsRejectedAsync(int dataTypeId, string defaultValue)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "BadDefaultFormat");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("BadDefaultFormat"), dataTypeId, defaultValue);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DefaultValueFormatInvalid");
        }

        [Theory]
        [InlineData(2, "42")]
        [InlineData(3, "4.2")]
        [InlineData(4, "7")]
        [InlineData(5, "1")]
        public async Task DefaultValueParseableForDataTypeIsAcceptedAsync(int dataTypeId, string defaultValue)
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "GoodDefaultFormat");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("GoodDefaultFormat"), dataTypeId, defaultValue);

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.DefaultValue.Should().Be(defaultValue);
        }

        [Fact]
        public async Task DefaultValueOverMaximumLengthIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "DefaultMax");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(visualisationRegistryId, UniqueName("DefaultMax"), 1, new string('v', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "DefaultValueMaximumLength");
        }

        [Fact]
        public async Task InvalidVisualisationRegistryIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "BadParent");
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(0, UniqueName("BadParent"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdInvalid");
        }

        [Fact]
        public async Task NonExistentVisualisationRegistryIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "MissingParent");
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(int.MaxValue - 1, UniqueName("MissingParent"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdNotFound");
        }

        [Fact]
        public async Task VisualisationRegistryIdBelongingToAnotherTenantIsRejectedAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (_, ownerUserName) = await CreateTenantUserWithPermissionsAsync(ownerDb,
                [VisualisationParameterAdministrationSpec], "CrossTenantOwner");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(ownerDb, ownerUserName);

            await using var dbContext = fx.GetDbContext();
            var (_, otherUserName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "CrossTenantOther");
            var service = await BuildServiceAsync(dbContext, otherUserName);
            var dto = NewDto(visualisationRegistryId, UniqueName("CrossTenant"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdNotFound");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "TamperInsert");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(visualisationRegistryId, UniqueName("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(userName);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "TamperUpdate");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(visualisationRegistryId, saved.Name.Required());
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(userName);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(userName);
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "LockedUpdate");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Locked")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.Locked, (byte)1)
                .UpdateAsync();

            var dto = NewDto(visualisationRegistryId, saved.Name.Required());
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "PreDeleted");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(visualisationRegistryId, saved.Name.Required());
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "NeverExisted");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(visualisationRegistryId, UniqueName("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "LockedDelete");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("LockedDelete")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.Locked, (byte)1)
                .UpdateAsync();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "DoubleDelete");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ActiveOnlyRequiresBothVisualisationRegistryAndParameterRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec, VisualisationDirectorySpec, ReadWriteCaseSpec],
                "ActiveOnlyRoles");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("ActiveOnly")));
            createdIds.Add(saved.Id);

            (await service.GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId)).Should()
                .NotContain(d => d.Id == saved.Id);

            await GrantVisualisationRegistryRoleAsync(dbContext, visualisationRegistryId, userName);
            (await service.GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId)).Should()
                .NotContain(d => d.Id == saved.Id);

            await GrantVisualisationRegistryParameterRoleAsync(dbContext, saved.Guid, userName);
            (await service.GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId)).Should()
                .Contain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task ActiveOnlyExcludesInactiveParameterEvenWithBothRoleGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec, VisualisationDirectorySpec, ReadWriteCaseSpec],
                "ActiveOnlyInactive");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(visualisationRegistryId, UniqueName("Inactive"));
            dto.Active = false;
            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            await GrantVisualisationRegistryRoleAsync(dbContext, visualisationRegistryId, userName);
            await GrantVisualisationRegistryParameterRoleAsync(dbContext, saved.Guid, userName);

            (await service.GetByVisualisationRegistryIdActiveOnlyAsync(visualisationRegistryId)).Should()
                .NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Cancel");
            var service = await BuildServiceAsync(dbContext, userName);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext, [], "WarnOnly");
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(userName));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "LogInsert");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("LogInsert")));
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
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Gated");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, userName, log);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Gated")));
            createdIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "FailLog");
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);

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
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Span");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "VisualisationRegistryParameter.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Metric");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Metric")));
            createdIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "AuditLog");
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Reactive");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName("Reactive")));
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
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "ReactiveNone");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            var badDto = NewDto(visualisationRegistryId, "");
            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(badDto));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantUserWithPermissionsAsync(dbContext,
                [VisualisationParameterAdministrationSpec], "Page");
            var visualisationRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(visualisationRegistryId, UniqueName($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }
    }
}