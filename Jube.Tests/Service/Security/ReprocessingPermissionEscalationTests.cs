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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.RoleRegistryPermission;
using Jube.Dto.Repository.UserRegistry;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using GrantForbiddenException = Jube.Service.Exceptions.Repository.RoleRegistryPermission.ForbiddenException;
using GrantService = Jube.Service.Repository.RoleRegistryPermission.RoleRegistryPermissionService;
using UserForbiddenException = Jube.Service.Exceptions.Repository.UserRegistry.ForbiddenException;
using UserService = Jube.Service.Repository.UserRegistry.UserRegistryService;

namespace Jube.Test.Service.Security
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ReprocessingPermissionEscalationTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int EditRule = 26;
        private const int TriggerRun = 73;
        private const int StopRun = 74;
        private const int RolePermissionsPermission = 36;
        private const int UsersPermission = 35;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<string> createdActorNames = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdUserIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.UserRegistryVersion>()
                .Where(w => createdUserIds.Contains(w.UserRegistryId)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserIds.Contains(w.Id)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => createdActorNames.Contains(w.User)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.RoleRegistryVersion>()
                .Where(w => createdRoleIds.Contains(w.RoleRegistryId)).DeleteAsync();

            var grantIds = await dbContext.RoleRegistryPermission
                .Where(w => createdRoleIds.Contains(w.RoleRegistryId ?? -1)).Select(w => w.Id).ToListAsync();

            await dbContext.GetTable<Data.Poco.RoleRegistryPermissionVersion>()
                .Where(w => grantIds.Contains(w.RoleRegistryPermissionId)).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => grantIds.Contains(w.Id)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
        }

        private static string UniqueName(string label) => $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];

        private Task<GrantService> GrantServiceAsync(DbContext dbContext, string userName) =>
            GrantService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestLog.NoOp);

        private Task<UserService> UserServiceAsync(DbContext dbContext, string userName) =>
            UserService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestDynamicEnvironment.Create(), TestLog.NoOp);

        private Task<int> TenantOfAsync(DbContext dbContext) =>
            dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission).Select(w => w.TenantRegistryId)
                .FirstAsync();

        private async Task<(int Id, Guid Guid)> CreateRoleAsync(DbContext dbContext, int tenantRegistryId,
            params int[] specs)
        {
            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = guid, Name = UniqueName("RpEsc"), TenantRegistryId = tenantRegistryId, Active = 1, Locked = 0,
                Deleted = 0, Version = 1, CreatedUser = DatabaseFixture.Prefix, CreatedDate = DateTime.UtcNow
            });

            createdRoleIds.Add(id);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), RoleRegistryId = id, PermissionSpecificationId = spec, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow
                });
            }

            return (id, guid);
        }

        private async Task<string> CreateActorAsync(DbContext dbContext, int tenantRegistryId, params int[] specs)
        {
            var role = await CreateRoleAsync(dbContext, tenantRegistryId, specs);
            var name = UniqueName("RpActor");

            var userId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = role.Guid, Name = name, Email = $"{name}@example.invalid",
                Password = "not-used-by-permission-checks", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedUser = DatabaseFixture.Prefix, CreatedDate = DateTime.UtcNow
            });

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = name, TenantRegistryId = tenantRegistryId });

            createdUserIds.Add(userId);
            createdActorNames.Add(name);

            return name;
        }

        private static RoleRegistryPermissionDto GrantDto(int roleRegistryId, int spec) =>
            new() { RoleRegistryId = roleRegistryId, PermissionSpecificationId = spec, Active = true, Locked = false };

        private static UserRegistryDto UserDto(Guid roleRegistryGuid, string name) =>
            new() { RoleRegistryGuid = roleRegistryGuid, Name = name, Email = $"{name}@example.invalid" };

        public static IEnumerable<object[]> NewPermissions() => [[TriggerRun], [StopRun]];

        [Theory]
        [MemberData(nameof(NewPermissions))]
        public async Task AnAdministratorHoldingTheOriginalPermissionButNotTheNewOneCannotGrantItAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, EditRule);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, actor);

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.InsertAsync(GrantDto(target.Id, spec)));

            (await dbContext.RoleRegistryPermission.AnyAsync(w => w.RoleRegistryId == target.Id)).Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(NewPermissions))]
        public async Task AnAdministratorHoldingTheNewPermissionCanGrantItButNotTheOtherNewOneAsync(int held)
        {
            var other = held == TriggerRun ? StopRun : TriggerRun;

            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, held);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, actor);

            (await service.InsertAsync(GrantDto(target.Id, held))).PermissionSpecificationId.Should().Be(held);

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.InsertAsync(GrantDto(target.Id, other)));
        }

        [Theory]
        [MemberData(nameof(NewPermissions))]
        public async Task SelfGrantOfTheNewPermissionIsRefusedWhenTheAdministratorLacksItAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, EditRule);
            var role = await dbContext.UserRegistry.Where(w => w.Name == actor).Select(w => w.RoleRegistryGuid)
                .FirstAsync();
            var roleId = await dbContext.RoleRegistry.Where(w => w.Guid == role).Select(w => w.Id).FirstAsync();
            var service = await GrantServiceAsync(dbContext, actor);

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.InsertAsync(GrantDto(roleId, spec)));
        }

        [Theory]
        [MemberData(nameof(NewPermissions))]
        public async Task AUserAdministratorWithoutTheNewPermissionCannotPutAUserInARoleCarryingItAsync(int spec)
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, EditRule);
            var carrying = await CreateRoleAsync(dbContext, tenant, EditRule, spec);
            var service = await UserServiceAsync(dbContext, actor);

            await Assert.ThrowsAsync<UserForbiddenException>(() =>
                service.InsertAsync(UserDto(carrying.Guid, UniqueName("RpEscUser"))));
        }

        [Fact]
        public async Task AUserAdministratorHoldingAllThreeCanPutAUserInARoleCarryingAllThreeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, EditRule, TriggerRun, StopRun);
            var carrying = await CreateRoleAsync(dbContext, tenant, EditRule, TriggerRun, StopRun);
            var service = await UserServiceAsync(dbContext, actor);

            var saved = await service.InsertAsync(UserDto(carrying.Guid, UniqueName("RpEscOk")));
            createdUserIds.Add(saved.Id);

            saved.RoleRegistryGuid.Should().Be(carrying.Guid);
        }
    }
}
