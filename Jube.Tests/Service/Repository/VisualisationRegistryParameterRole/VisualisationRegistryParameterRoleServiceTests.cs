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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.VisualisationRegistryParameterRole;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Repository.VisualisationRegistryParameterRole;
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
using VisualisationRegistryParameterRoleService = Jube.Service.Repository.VisualisationRegistryParameterRole.VisualisationRegistryParameterRoleService;

namespace Jube.Test.Service.Repository.VisualisationRegistryParameterRole
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class VisualisationRegistryParameterRoleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int VisualisationRegistryParameterAdministrationSpec = 32;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdVisualisationRegistryIds = [];
        private readonly List<int> createdVisualisationRegistryParameterIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var parameterGuids = await dbContext.VisualisationRegistryParameter
                .Where(w => createdVisualisationRegistryParameterIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>()
                .Where(w => parameterGuids.Contains(w.VisualisationRegistryParameterGuid)).DeleteAsync();
            await dbContext.VisualisationRegistryParameter
                .Where(w => createdVisualisationRegistryParameterIds.Contains(w.Id)).DeleteAsync();
            await dbContext.VisualisationRegistry
                .Where(w => createdVisualisationRegistryIds.Contains(w.Id)).DeleteAsync();
            await dbContext.UserInTenant
                .Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry
                .Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry
                .Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<VisualisationRegistryParameterRoleService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return VisualisationRegistryParameterRoleService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp,
                localizers, serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private static Task<int> GetTenantRegistryIdAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(s => s.TenantRegistryId).FirstAsync();

        private async Task<(string userName, Guid roleRegistryGuid)> CreateUserWithSpecAsync(DbContext dbContext,
            int tenantRegistryId, int[] specs, string label)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}Role{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix,
            }).ConfigureAwait(false);
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
                }).ConfigureAwait(false);
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
                CreatedUser = DatabaseFixture.Prefix,
            }).ConfigureAwait(false);
            await dbContext.InsertAsync(new Data.Poco.UserInTenant
                { User = userName, TenantRegistryId = tenantRegistryId }).ConfigureAwait(false);
            createdUserNames.Add(userName);

            return (userName, roleGuid);
        }

        private async Task<(int visualisationRegistryId, int visualisationRegistryParameterId, Guid
            visualisationRegistryParameterGuid)> CreateVisualisationRegistryParameterAsync(DbContext dbContext,
            int tenantRegistryId, string createdUser)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var visualisationRegistryId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.VisualisationRegistry
                {
                    Name = $"{DatabaseFixture.Prefix}Visualisation{suffix}",
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    TenantRegistryId = tenantRegistryId,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = createdUser,
                }).ConfigureAwait(false);
            createdVisualisationRegistryIds.Add(visualisationRegistryId);

            var parameterGuid = Guid.NewGuid();
            var visualisationRegistryParameterId = await dbContext.InsertWithInt32IdentityAsync(
                new Data.Poco.VisualisationRegistryParameter
                {
                    Name = $"{DatabaseFixture.Prefix}Parameter{suffix}",
                    Guid = parameterGuid,
                    VisualisationRegistryId = visualisationRegistryId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = createdUser,
                }).ConfigureAwait(false);
            createdVisualisationRegistryParameterIds.Add(visualisationRegistryParameterId);

            return (visualisationRegistryId, visualisationRegistryParameterId, parameterGuid);
        }

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

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.GetByVisualisationRegistryParameterGuidAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task InsertWithoutPermissionThrowsForbiddenAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (_, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId, [],
                "NoAdminSpec");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(
                new VisualisationRegistryParameterRoleDto
                {
                    VisualisationRegistryParameterGuid = parameterGuid,
                    RoleRegistryGuid = roleRegistryGuid,
                }));

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>()
                .AnyAsync(w => w.VisualisationRegistryParameterGuid == parameterGuid)).Should().BeFalse();
        }

        [Fact]
        public async Task DeleteWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1));
        }

        [Fact]
        public async Task InsertPersistsAndReturnsGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var saved = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            saved.Id.Should().BeGreaterThan(0);
            saved.VisualisationRegistryParameterGuid.Should().Be(parameterGuid);
            saved.RoleRegistryGuid.Should().Be(roleRegistryGuid);
        }

        [Fact]
        public async Task InsertWithEmptyVisualisationRegistryParameterGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var service = await BuildServiceAsync(dbContext, userName);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = Guid.Empty,
                RoleRegistryGuid = roleRegistryGuid,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithUnknownVisualisationRegistryParameterGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var service = await BuildServiceAsync(dbContext, userName);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithCrossTenantVisualisationRegistryParameterGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryIdA = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryIdB = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var (userNameA, roleRegistryGuidA) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdA,
                [VisualisationRegistryParameterAdministrationSpec], "AdminA");
            var (_, _, parameterGuidB) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryIdB,
                fx.Seed.UserTenantB);
            var service = await BuildServiceAsync(dbContext, userNameA);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuidB,
                RoleRegistryGuid = roleRegistryGuidA,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithEmptyRoleRegistryGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = Guid.Empty,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertWithCrossTenantRoleRegistryGuidThrowsValidationFailedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryIdA = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryIdB = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var (userNameA, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdA,
                [VisualisationRegistryParameterAdministrationSpec], "AdminA");
            var (_, roleRegistryGuidB) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdB, [], "OtherB");
            var (_, _, parameterGuidA) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryIdA,
                userNameA);
            var service = await BuildServiceAsync(dbContext, userNameA);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuidA,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
        }

        [Fact]
        public async Task InsertPublishesCreatedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            bus.Published.Should().ContainSingle();
            bus.Published[0].Area.Should().Be("VisualisationRegistryParameterRole");
            bus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            bus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task InsertDoesNotPublishChangeEventOnValidationFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");

            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: bus);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = Guid.Empty,
                RoleRegistryGuid = Guid.Empty,
            });

            await act.Should().ThrowAsync<DtoValidationException>();
            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListByVisualisationRegistryParameterGuidReturnsGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);
            await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            var grants = await service.GetByVisualisationRegistryParameterGuidAsync(parameterGuid);

            grants.Should().ContainSingle();
            grants[0].RoleRegistryGuid.Should().Be(roleRegistryGuid);
        }

        [Fact]
        public async Task ListForUnknownParameterGuidReturnsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var grants = await service.GetByVisualisationRegistryParameterGuidAsync(Guid.NewGuid());

            grants.Should().BeEmpty();
        }

        [Fact]
        public async Task ListIsScopedToCallersTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryIdA = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryIdB = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var (userNameA, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdA,
                [VisualisationRegistryParameterAdministrationSpec], "AdminA");
            var (userNameB, roleRegistryGuidB) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdB,
                [VisualisationRegistryParameterAdministrationSpec], "AdminB");
            var (_, _, parameterGuidB) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryIdB,
                userNameB);
            var serviceB = await BuildServiceAsync(dbContext, userNameB);
            await serviceB.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuidB,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            var serviceA = await BuildServiceAsync(dbContext, userNameA);
            var grantsForA = await serviceA.GetByVisualisationRegistryParameterGuidAsync(parameterGuidB);

            grantsForA.Should().BeEmpty();
        }

        [Fact]
        public async Task ListExcludesDeletedGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);
            var saved = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(saved.Id);

            var grants = await service.GetByVisualisationRegistryParameterGuidAsync(parameterGuid);

            grants.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteSoftDeletesAndPublishesDeletedChangeEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: bus);
            var saved = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            await service.DeleteAsync(saved.Id);

            var row = await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>()
                .FirstAsync(w => w.Id == saved.Id);
            row.Deleted.Should().Be(1);

            bus.Published.Should().HaveCount(2);
            bus.Published[1].Kind.Should().Be(ServiceChangeKind.Deleted);
            bus.Published[1].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task DeleteOfMissingRowThrowsKeyNotFoundExceptionNotA404OrA204Async()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var service = await BuildServiceAsync(dbContext, userName);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(int.MaxValue));
        }

        [Fact]
        public async Task DeleteOfCrossTenantRowThrowsKeyNotFoundExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryIdA = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var tenantRegistryIdB = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserTenantB);
            var (userNameA, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdA,
                [VisualisationRegistryParameterAdministrationSpec], "AdminA");
            var (userNameB, roleRegistryGuidB) = await CreateUserWithSpecAsync(dbContext, tenantRegistryIdB,
                [VisualisationRegistryParameterAdministrationSpec], "AdminB");
            var (_, _, parameterGuidB) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryIdB,
                userNameB);
            var serviceB = await BuildServiceAsync(dbContext, userNameB);
            var savedB = await serviceB.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuidB,
                RoleRegistryGuid = roleRegistryGuidB,
            });

            var serviceA = await BuildServiceAsync(dbContext, userNameA);
            await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceA.DeleteAsync(savedB.Id));
        }

        [Fact]
        public async Task DeleteDoesNotPublishChangeEventOnFailureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, _) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var bus = new CapturingBus();
            var service = await BuildServiceAsync(dbContext, userName, serviceChangeBus: bus);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.DeleteAsync(int.MaxValue));

            bus.Published.Should().BeEmpty();
        }

        [Fact]
        public async Task ListHonoursCancellationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            var cancelledToken = cts.Token;
            var act = () => service.GetByVisualisationRegistryParameterGuidAsync(Guid.NewGuid(), cancelledToken);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task InsertWarnsNotErrorsOnForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log: capturingLog);

            var act = () => service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = Guid.NewGuid(),
                RoleRegistryGuid = Guid.NewGuid(),
            });
            await act.Should().ThrowAsync<ForbiddenException>();

            capturingLog.Entries.Should().Contain(e => e.Level == "WARN");
            capturingLog.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task InsertLogsOneAuditRecordOnSuccessAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);

            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, userName, auditLog: capturingAuditLog);
            await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public async Task ListLogsOneAuditRecordAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var capturingAuditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                auditLog: capturingAuditLog);

            await service.GetByVisualisationRegistryParameterGuidAsync(Guid.NewGuid());

            capturingAuditLog.Entries.Should().ContainSingle();
        }

        [Fact]
        public void CatalogueListsUniqueVisualisationRegistryParameterRoleToolNames()
        {
            var tools = ServiceToolCatalogue.All;
            var names = tools.Where(t => t.Name.StartsWith("VisualisationRegistryParameterRole"))
                .Select(t => t.Name).ToList();

            names.Should().BeEquivalentTo([
                "VisualisationRegistryParameterRoleListByVisualisationRegistryParameterGuid",
                "VisualisationRegistryParameterRoleCreate",
                "VisualisationRegistryParameterRoleDelete",
            ]);
            names.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public async Task RepeatedGrantReturnsTheExistingGrantAndWritesNoSecondRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var first = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var second = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            second.Id.Should().Be(first.Id);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                w.VisualisationRegistryParameterGuid == parameterGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task RepeatedGrantAfterRevokeCreatesANewLiveGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var first = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(first.Id);
            var again = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });

            again.Id.Should().NotBe(first.Id, "a soft-deleted grant must not block granting again");
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                w.VisualisationRegistryParameterGuid == parameterGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task RepeatedGrantAndRevokeLeavesNoAccessBehindAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var repeated = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            await service.DeleteAsync(repeated.Id);

            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                    w.VisualisationRegistryParameterGuid == parameterGuid
                    && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should()
                .Be(0, "one revoke must remove the role's access");
        }

        [Fact]
        public async Task TheSameRoleOnTwoParentsIsTwoIndependentGrantsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);
            var service = await BuildServiceAsync(dbContext, userName);

            var (_, _, parameterGuidTwo) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);

            var one = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuid,
                RoleRegistryGuid = roleRegistryGuid,
            });
            var two = await service.InsertAsync(new VisualisationRegistryParameterRoleDto
            {
                VisualisationRegistryParameterGuid = parameterGuidTwo,
                RoleRegistryGuid = roleRegistryGuid,
            });

            two.Id.Should().NotBe(one.Id);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                w.VisualisationRegistryParameterGuid == parameterGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                w.VisualisationRegistryParameterGuid == parameterGuidTwo
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }

        [Fact]
        public async Task ConcurrentIdenticalGrantsProduceExactlyOneLiveRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantRegistryId = await GetTenantRegistryIdAsync(dbContext, fx.Seed.UserWithPermission);
            var (userName, roleRegistryGuid) = await CreateUserWithSpecAsync(dbContext, tenantRegistryId,
                [VisualisationRegistryParameterAdministrationSpec], "Admin");
            var (_, _, parameterGuid) = await CreateVisualisationRegistryParameterAsync(dbContext, tenantRegistryId,
                userName);

            var ids = new ConcurrentBag<int>();
            await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var raceContext = fx.GetDbContext();
                var raceService = await BuildServiceAsync(raceContext, userName);
                var saved = await raceService.InsertAsync(new VisualisationRegistryParameterRoleDto
                {
                    VisualisationRegistryParameterGuid = parameterGuid,
                    RoleRegistryGuid = roleRegistryGuid,
                });
                ids.Add(saved.Id);
            }));

            ids.Distinct().Should().ContainSingle("every racing grant must resolve to the one live row");
            (await dbContext.GetTable<Data.Poco.VisualisationRegistryParameterRole>().CountAsync(w =>
                w.VisualisationRegistryParameterGuid == parameterGuid
                && w.RoleRegistryGuid == roleRegistryGuid && (w.Deleted == 0 || w.Deleted == null))).Should().Be(1);
        }
    }
}