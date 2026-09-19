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
using Jube.Dto.Repository.VisualisationRegistryDatasource;
using Jube.Service.Exceptions.Repository.VisualisationRegistryDatasource;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Repository.VisualisationRegistryDatasource.Models;
using LinqToDB;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Repository.VisualisationRegistryDatasource
{
    using VisualisationRegistryDatasourceService =
        global::Jube.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryDatasourceServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int DatasourcePermissionSpec = 33;
        private const string ValidCommand = "SELECT 1::integer AS \"ExampleColumn\"";

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdDatasourceIds = [];
        private readonly List<int> createdVisualisationRegistryIds = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
            var datasourceGuids = dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(d => createdDatasourceIds.Contains(d.Id)).Select(d => d.Guid);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceRole>()
                .Where(w => datasourceGuids.Contains(w.VisualisationRegistryDatasourceGuid)).DeleteAsync();
            await dbContext.GetTable<VisualisationRegistryDatasourceVersion>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(w => createdDatasourceIds.Contains(w.Id)).DeleteAsync();

            var registryGuids = dbContext.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(r => createdVisualisationRegistryIds.Contains(r.Id)).Select(r => r.Guid);
            await dbContext.GetTable<Data.Poco.VisualisationRegistryRole>()
                .Where(w => registryGuids.Contains(w.VisualisationRegistryGuid)).DeleteAsync();
            await dbContext.GetTable<VisualisationRegistryVersion>()
                .Where(w => createdVisualisationRegistryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(w => createdVisualisationRegistryIds.Contains(w.Id)).DeleteAsync();

            foreach (var userName in createdUserNames)
            {
                await dbContext.GetTable<Data.Poco.UserInTenant>().Where(w => w.User == userName).DeleteAsync();
                await dbContext.GetTable<Data.Poco.UserRegistry>().Where(w => w.Name == userName).DeleteAsync();
            }

            await dbContext.GetTable<Data.Poco.RoleRegistryPermission>()
                .Where(w => createdRoleIds.Contains(w.RoleRegistryId ?? 0)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.RoleRegistry>().Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.TenantRegistry>().Where(w => createdTenantIds.Contains(w.Id))
                .DeleteAsync();
        }

        private static Task<VisualisationRegistryDatasourceService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null,
            IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            var realConnectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ?? string.Empty;
            var overrides = new Dictionary<string, string> { ["ReportConnectionString"] = realConnectionString };
            if (environmentOverrides != null)
            {
                foreach (var kvp in environmentOverrides)
                {
                    overrides[kvp.Key] = kvp.Value;
                }
            }

            var dynamicEnvironment = TestDynamicEnvironment.Create(overrides);

            return VisualisationRegistryDatasourceService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                dynamicEnvironment, auditLog ?? TestLog.NoOp);
        }

        private async Task<(int TenantRegistryId, string UserName)> CreateTenantWithPermissionAsync(
            DbContext dbContext, string label, int[] specs)
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
            createdRoleIds.Add(roleId);

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

        private async Task<int> CreateParentVisualisationRegistryAsync(DbContext dbContext, int tenantRegistryId,
            string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Registry{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow
            });

            createdVisualisationRegistryIds.Add(id);
            return id;
        }

        private static VisualisationRegistryDatasourceDto NewDto(int visualisationRegistryId, string? name)
        {
            return new VisualisationRegistryDatasourceDto
            {
                VisualisationRegistryId = visualisationRegistryId,
                Name = name,
                Active = true,
                Locked = false,
                Command = ValidCommand,
                Priority = 1,
                RowSpan = 1,
                ColumnSpan = 1,
                IncludeGrid = true,
                IncludeDisplay = false
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
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Insert", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Insert")));
            createdDatasourceIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(userName);
            saved.CreatedDate.Should().NotBeNull();
            saved.Guid.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "GetAll", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(registryId, name));
            createdDatasourceIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId.Required().Name.Should().Be(name);
        }

        [Fact]
        public async Task GetByVisualisationRegistryIdReturnsOnlyRowsForThatParentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "ByParent", [DatasourcePermissionSpec]);
            var registryAId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var registryBId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var savedA = await service.InsertAsync(NewDto(registryAId, UniqueName("ByRegistryA")));
            createdDatasourceIds.Add(savedA.Id);
            var savedB = await service.InsertAsync(NewDto(registryBId, UniqueName("ByRegistryB")));
            createdDatasourceIds.Add(savedB.Id);

            var forRegistryA = await service.GetByVisualisationRegistryIdAsync(registryAId);
            forRegistryA.Should().Contain(d => d.Id == savedA.Id);
            forRegistryA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task GetByVisualisationRegistryIdActiveOnlyRequiresRoleVisibilityOnBothParentAndRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "ActiveOnly", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("ActiveOnly")));
            createdDatasourceIds.Add(saved.Id);

            var beforeRoleGrant = await service.GetByVisualisationRegistryIdActiveOnlyAsync(registryId);
            beforeRoleGrant.Should().NotContain(d => d.Id == saved.Id);

            var role = await dbContext.RoleRegistry.FirstAsync(r =>
                r.TenantRegistryId == tenantId && r.Name != null && r.Name.Contains("ActiveOnly"));
            var registry = await dbContext.VisualisationRegistry.FirstAsync(r => r.Id == registryId);

            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryRole
            {
                Guid = Guid.NewGuid(),
                VisualisationRegistryGuid = registry.Guid,
                RoleRegistryGuid = role.Guid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
                Version = 1
            });
            await dbContext.InsertAsync(new Data.Poco.VisualisationRegistryDatasourceRole
            {
                Guid = Guid.NewGuid(),
                VisualisationRegistryDatasourceGuid = saved.Guid,
                RoleRegistryGuid = role.Guid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
                Version = 1
            });

            var afterRoleGrant = await service.GetByVisualisationRegistryIdActiveOnlyAsync(registryId);
            afterRoleGrant.Should().Contain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task UpdatePreservesCreatedFieldsSetsUpdatedFieldsAndKeepsImportIdAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Update", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Update")));
            createdDatasourceIds.Add(saved.Id);

            var importId = await dbContext.InsertWithInt32IdentityAsync(new Import
            {
                Guid = Guid.NewGuid(),
                TenantRegistryId = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                ExportGuid = Guid.NewGuid(),
            });

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>().Where(w => w.Id == saved.Id)
                .Set(s => s.ImportId, importId).UpdateAsync();

            try
            {
                var dto = NewDto(registryId, saved.Name);
                dto.Id = saved.Id;
                dto.Priority = 5;

                var updated = await service.UpdateAsync(dto);

                updated.Version.Should().Be(2);
                updated.Priority.Should().Be(5);
                updated.Guid.Should().Be(saved.Guid);
                updated.CreatedUser.Should().Be(saved.CreatedUser);
                updated.CreatedDate.Should().BeCloseTo(saved.CreatedDate.Required(), TimeSpan.FromMilliseconds(10));
                updated.UpdatedUser.Should().Be(userName);
                updated.UpdatedDate.Should().NotBeNull();
                updated.ImportId.Should().Be(importId);

                var auditRows = await dbContext.GetTable<VisualisationRegistryDatasourceVersion>()
                    .Where(w => w.VisualisationRegistryDatasourceId == saved.Id).CountAsync();
                auditRows.Should().Be(1);
            }
            finally
            {
                await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                    .Where(w => w.Id == saved.Id).Set(w => w.ImportId, (int?)null).UpdateAsync();
                await dbContext.GetTable<Import>().Where(w => w.Id == importId).DeleteAsync();
            }
        }

        [Fact]
        public async Task UserInTenantBCannotUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (tenantAId, ownerUserName) =
                await CreateTenantWithPermissionAsync(ownerDb, "IsolationA", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(ownerDb, tenantAId, ownerUserName);
            var owner = await BuildServiceAsync(ownerDb, ownerUserName);
            var saved = await owner.InsertAsync(NewDto(registryId, UniqueName("Isolation")));
            createdDatasourceIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var (tenantBId, otherUserName) =
                await CreateTenantWithPermissionAsync(dbContext, "IsolationB", [DatasourcePermissionSpec]);
            var otherTenant = await BuildServiceAsync(dbContext, otherUserName);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var tenantBRegistryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantBId, otherUserName);
            var updateDto = NewDto(tenantBRegistryId, saved.Name);
            updateDto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
            stillThere.Required().Version.Should().Be(1);
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var (tenantAId, ownerUserName) =
                await CreateTenantWithPermissionAsync(ownerDb, "TenantAOnlyA", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(ownerDb, tenantAId, ownerUserName);
            var owner = await BuildServiceAsync(ownerDb, ownerUserName);
            var saved = await owner.InsertAsync(NewDto(registryId, UniqueName("TenantAOnly")));
            createdDatasourceIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var (_, otherUserName) =
                await CreateTenantWithPermissionAsync(dbContext, "TenantAOnlyB", [DatasourcePermissionSpec]);
            var otherTenant = await BuildServiceAsync(dbContext, otherUserName);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Delete", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Delete")));
            createdDatasourceIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var all = await service.GetAsync();
            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var (tenantId, ownerUserName) =
                await CreateTenantWithPermissionAsync(writerDb, "ForbiddenOwner", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(writerDb, tenantId, ownerUserName);
            var writer = await BuildServiceAsync(writerDb, ownerUserName);
            var seedRow = await writer.InsertAsync(NewDto(registryId, UniqueName("Forbidden")));
            createdDatasourceIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var (_, noPermissionUserName) = await CreateTenantWithPermissionAsync(dbContext, "NoPermission", []);
            var service = await BuildServiceAsync(dbContext, noPermissionUserName);

            var beforeCount = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .CountAsync(w => w.Name != null && w.Name.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(registryId, UniqueName("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByVisualisationRegistryIdAsync(registryId));

            var updateDto = NewDto(registryId, seedRow.Name);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
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
                BuildServiceAsync(dbContext, userName, log));

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

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "NameReq", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, name);
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "NameLen", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, new string('n', 257));
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task DuplicateNameWithinSameParentFailsButDifferentParentSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Duplicate", [DatasourcePermissionSpec]);
            var registryAId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var registryBId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var name = UniqueName("Dup");
            var first = await service.InsertAsync(NewDto(registryAId, name));
            createdDatasourceIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(registryAId, name)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var second = await service.InsertAsync(NewDto(registryBId, name));
            createdDatasourceIds.Add(second.Id);
            second.Id.Should().NotBe(first.Id);
        }

        [Fact]
        public async Task InvalidVisualisationRegistryIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "InvalidParent", [DatasourcePermissionSpec]);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(0, UniqueName("InvalidParent"));
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdInvalid");
        }

        [Fact]
        public async Task VisualisationRegistryIdFromAnotherTenantIsRejectedAsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (foreignTenantId, foreignUserName) =
                await CreateTenantWithPermissionAsync(dbContext, "ForeignParentOwner", [DatasourcePermissionSpec]);
            var foreignRegistryId =
                await CreateParentVisualisationRegistryAsync(dbContext, foreignTenantId, foreignUserName);

            var (_, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "ForeignParent", [DatasourcePermissionSpec]);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(foreignRegistryId, UniqueName("ForeignParent"));
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationRegistryIdNotFound");
        }

        [Fact]
        public async Task CommandRequiredRejectsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "CommandReq", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("CommandReq"));
            dto.Command = "";
            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CommandNotEmpty");
        }

        [Theory]
        [InlineData(-1)]
        public async Task PriorityColumnSpanAndRowSpanNegativeAreRejectedAsync(int negative)
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Ranges", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var priorityDto = NewDto(registryId, UniqueName("Priority"));
            priorityDto.Priority = negative;
            var priorityEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(priorityDto));
            priorityEx.Result.Errors.Should().Contain(e => e.ErrorCode == "PriorityRange");

            var columnSpanDto = NewDto(registryId, UniqueName("ColumnSpan"));
            columnSpanDto.ColumnSpan = negative;
            var columnSpanEx =
                await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(columnSpanDto));
            columnSpanEx.Result.Errors.Should().Contain(e => e.ErrorCode == "ColumnSpanRange");

            var rowSpanDto = NewDto(registryId, UniqueName("RowSpan"));
            rowSpanDto.RowSpan = negative;
            var rowSpanEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(rowSpanDto));
            rowSpanEx.Result.Errors.Should().Contain(e => e.ErrorCode == "RowSpanRange");
        }

        [Fact]
        public async Task VisualisationTypeAndTextAreValidatedOnlyWhenIncludeDisplayEnabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Display", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var disabledDto = NewDto(registryId, UniqueName("DisplayOff"));
            disabledDto.IncludeDisplay = false;
            disabledDto.VisualisationTypeId = 0;
            disabledDto.VisualisationText = null;
            var saved = await service.InsertAsync(disabledDto);
            createdDatasourceIds.Add(saved.Id);
            saved.VisualisationTypeId.Should().Be(0);

            var missingTypeDto = NewDto(registryId, UniqueName("DisplayBadType"));
            missingTypeDto.IncludeDisplay = true;
            missingTypeDto.VisualisationTypeId = 99;
            missingTypeDto.VisualisationText = "text";
            var typeEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(missingTypeDto));
            typeEx.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationTypeIdInvalid");

            var missingTextDto = NewDto(registryId, UniqueName("DisplayNoText"));
            missingTextDto.IncludeDisplay = true;
            missingTextDto.VisualisationTypeId = 1;
            missingTextDto.VisualisationText = "";
            var textEx = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(missingTextDto));
            textEx.Result.Errors.Should().Contain(e => e.ErrorCode == "VisualisationTextNotEmpty");

            var validDisplayDto = NewDto(registryId, UniqueName("DisplayOk"));
            validDisplayDto.IncludeDisplay = true;
            validDisplayDto.VisualisationTypeId = 1;
            validDisplayDto.VisualisationText = "({})";
            var validDisplaySaved = await service.InsertAsync(validDisplayDto);
            createdDatasourceIds.Add(validDisplaySaved.Id);
        }

        [Fact]
        public async Task InsertWithInvalidSqlThrowsSqlValidationFailedExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "BadSql", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("BadSql"));
            dto.Command = "this is not valid sql";

            var ex = await Assert.ThrowsAsync<SqlValidationFailedException>(() => service.InsertAsync(dto));
            ex.Code.Should().Be("SqlValidationFailed");
            ex.Result.Errors.Should().ContainSingle(e => e.PropertyName == "SQL");
        }

        [Theory]
        [InlineData("select 1 as \"A\" where 1 = @Missing", "column \"missing\" does not exist", "42703")]
        [InlineData("select \"NoSuchColumn\" from \"ExampleCustomerCaseManagement\"",
            "column \"NoSuchColumn\" does not exist", "42703")]
        [InlineData("select 1 from \"NoSuchTableZz\"", "relation \"NoSuchTableZz\" does not exist", "42P01")]
        public async Task InsertWithSqlTheDatabaseRefusesSurfacesTheDatabaseMessageAsync(string command, string message,
            string sqlState)
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "DbMessage", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("DbMessage"));
            dto.Command = command;

            var ex = await Assert.ThrowsAsync<SqlValidationFailedException>(() => service.InsertAsync(dto));

            var text = ex.Result.Errors.Single(e => e.PropertyName == "SQL").ErrorMessage;
            text.Should().Contain(message).And.Contain(sqlState);
            text.Should().NotContain("Npgsql").And.NotContain("Exception").And.NotContain(" at ")
                .And.NotContain("select ", "the statement is never echoed");
        }

        [Fact]
        public async Task InsertWithNonSelectSqlThrowsSqlValidationFailedExceptionWhenSelectOnlyEnforcedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "DeleteSql", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName,
                environmentOverrides: new Dictionary<string, string> { ["ParserAssertSelectOnly"] = "True" });

            var dto = NewDto(registryId, UniqueName("DeleteSql"));
            dto.Command = "DELETE FROM \"TenantRegistry\"";

            await Assert.ThrowsAsync<SqlValidationFailedException>(() => service.InsertAsync(dto));
        }

        [Fact]
        public async Task InsertWithValidSqlPopulatesSeriesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Series", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("Series"));
            dto.Command = "SELECT 1::integer AS \"ExampleColumn\"";

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var series = await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => w.VisualisationRegistryDatasourceId == saved.Id).ToListAsync();
            series.Should().Contain(s => s.Name == "ExampleColumn");
        }

        [Fact]
        public async Task TamperedGuidOnInsertHasNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "TamperGuid", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var tamperedGuid = Guid.NewGuid();
            var dto = NewDto(registryId, UniqueName("TamperGuid"));
            dto.Guid = tamperedGuid;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            saved.Guid.Should().NotBe(tamperedGuid);
            saved.CreatedUser.Should().Be(userName);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "TamperUpdate", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("TamperUpdate")));
            createdDatasourceIds.Add(saved.Id);

            var dto = NewDto(registryId, saved.Name);
            dto.Id = saved.Id;
            dto.Guid = Guid.NewGuid();
            dto.CreatedUser = "someone-else";
            dto.Version = 999;
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(userName);
            updated.Guid.Should().Be(saved.Guid);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(userName);
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Locked", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Locked")));
            createdDatasourceIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(w => w.Id == saved.Id).Set(s => s.Locked, (byte)1).UpdateAsync();

            var dto = NewDto(registryId, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "PreDeleted", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("PreDeleted")));
            createdDatasourceIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(registryId, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Missing", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "DoubleDelete", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("DoubleDelete")));
            createdDatasourceIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ActiveAndLockedRoundTripBoolToByteAndBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "ActiveLocked", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var dto = NewDto(registryId, UniqueName("ActiveLocked"));
            dto.Active = true;
            dto.Locked = false;

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched.Required().Active.Should().BeTrue();
            fetched.Required().Locked.Should().BeFalse();
        }

        [Fact]
        public async Task DescriptionAndColumnsLegacyFieldsAreNeverPersistedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "DeadFields", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var dto = NewDto(registryId, UniqueName("DeadFields"));
            dto.Description = "should never be stored";
            dto.Columns = 12345;

            var saved = await service.InsertAsync(dto);
            createdDatasourceIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched.Required().Description.Should().BeNull();
            fetched.Required().Columns.Should().Be(0);
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "MissingGet", [DatasourcePermissionSpec]);
            var service = await BuildServiceAsync(dbContext, userName);

            var result = await service.GetByIdAsync(int.MaxValue - 1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Cancelled", [DatasourcePermissionSpec]);
            var service = await BuildServiceAsync(dbContext, userName);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantWithPermissionAsync(dbContext, "DeniedLog", []);
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
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "LogInsert", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, log);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("LogInsert")));
            createdDatasourceIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Gated", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, userName, log);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Gated")));
            createdDatasourceIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantWithPermissionAsync(dbContext, "Failure", [DatasourcePermissionSpec]);
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
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Span", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Span")));
            createdDatasourceIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "VisualisationRegistryDatasource.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Metric", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Metric")));
            createdDatasourceIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should()
                .ContainSingle(m => (string)m.Tags["operation"].Required() == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, userName) = await CreateTenantWithPermissionAsync(dbContext, "Audit", [DatasourcePermissionSpec]);
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
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Reactive", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(registryId, UniqueName("Reactive")));
            createdDatasourceIds.Add(saved.Id);

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "NoEvent", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(registryId, "")));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, "Page", [DatasourcePermissionSpec]);
            var registryId = await CreateParentVisualisationRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(registryId, UniqueName($"Page{i}")));
                createdDatasourceIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }
    }
}