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
using Jube.Dto.Repository.UserRegistryApiKey;
using Jube.Resources;
using Jube.Service.Reactivity;
using Jube.Service.Repository.UserRegistryApiKey;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using ApiKeyForbiddenException = Jube.Service.Exceptions.Repository.UserRegistryApiKey.ForbiddenException;
using ApiKeyService = Jube.Service.Repository.UserRegistryApiKey.UserRegistryApiKeyService;
using GrantForbiddenException = Jube.Service.Exceptions.Repository.RoleRegistryPermission.ForbiddenException;
using GrantService = Jube.Service.Repository.RoleRegistryPermission.RoleRegistryPermissionService;
using RoleForbiddenException = Jube.Service.Exceptions.Repository.RoleRegistry.ForbiddenException;
using RoleService = Jube.Service.Repository.RoleRegistry.RoleRegistryService;
using UserForbiddenException = Jube.Service.Exceptions.Repository.UserRegistry.ForbiddenException;
using UserService = Jube.Service.Repository.UserRegistry.UserRegistryService;

namespace Jube.Test.Service.Security
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PrivilegeEscalationServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int RolesPermission = 34;
        private const int UsersPermission = 35;
        private const int RolePermissionsPermission = 36;
        private const int HeldPermission = 1;
        private const int OtherPermission = 2;

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly Dictionary<string, string> hmacKey =
            new() { ["ApiHmacKey"] = $"{DatabaseFixture.Prefix}HmacSecretForTests" };

        private readonly List<int> createdApiKeyIds = [];
        private readonly List<string> createdActorNames = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdUserIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.UserRegistryApiKey.Where(w => createdApiKeyIds.Contains(w.Id)).DeleteAsync();

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

        private static string Expected<T>(string key) =>
            localizers.Create(typeof(T))[key].Value;

        private Task<GrantService> GrantServiceAsync(DbContext dbContext, string userName) =>
            GrantService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestLog.NoOp);

        private Task<UserService> UserServiceAsync(DbContext dbContext, string userName) =>
            UserService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestDynamicEnvironment.Create(), TestLog.NoOp);

        private Task<RoleService> RoleServiceAsync(DbContext dbContext, string userName) =>
            RoleService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestLog.NoOp);

        private Task<ApiKeyService> ApiKeyServiceAsync(DbContext dbContext, string userName) =>
            ApiKeyService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestDynamicEnvironment.Create(hmacKey), NullUserRegistryApiKeyCacheInvalidator.Instance,
                TestLog.NoOp);

        private Task<int> TenantOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserInTenant.Where(w => w.User == userName).Select(w => w.TenantRegistryId).FirstAsync();

        private async Task<(int Id, Guid Guid)> CreateRoleAsync(DbContext dbContext, int tenantRegistryId,
            params int[] specs)
        {
            var guid = Guid.NewGuid();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = guid,
                Name = UniqueName("EscRole"),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });

            createdRoleIds.Add(id);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    RoleRegistryId = id,
                    PermissionSpecificationId = spec,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedUser = DatabaseFixture.Prefix,
                    CreatedDate = DateTime.UtcNow
                });
            }

            return (id, guid);
        }

        private async Task<(string Name, int UserId, int RoleId, Guid RoleGuid)> CreateActorAsync(
            DbContext dbContext, int tenantRegistryId, params int[] specs)
        {
            var role = await CreateRoleAsync(dbContext, tenantRegistryId, specs);
            var name = UniqueName("EscActor");

            var userId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = role.Guid,
                Name = name,
                Email = $"{name}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedUser = DatabaseFixture.Prefix,
                CreatedDate = DateTime.UtcNow
            });

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = name, TenantRegistryId = tenantRegistryId });

            createdUserIds.Add(userId);
            createdActorNames.Add(name);

            return (name, userId, role.Id, role.Guid);
        }

        private static RoleRegistryPermissionDto GrantDto(int roleRegistryId, int spec) =>
            new() { RoleRegistryId = roleRegistryId, PermissionSpecificationId = spec, Active = true, Locked = false };

        private static UserRegistryDto UserDto(Guid roleRegistryGuid, string name) =>
            new() { RoleRegistryGuid = roleRegistryGuid, Name = name, Email = $"{name}@example.invalid" };

        [Fact]
        public async Task GrantOfAPermissionTheCallerLacksIsRefusedAndWritesNoRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, actor.Name);

            await Assert.ThrowsAsync<GrantForbiddenException>(() =>
                service.InsertAsync(GrantDto(target.Id, OtherPermission)));

            (await dbContext.RoleRegistryPermission.AnyAsync(w => w.RoleRegistryId == target.Id)).Should().BeFalse();
        }

        [Fact]
        public async Task GrantOfAPermissionTheCallerHoldsIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, actor.Name);

            var saved = await service.InsertAsync(GrantDto(target.Id, HeldPermission));

            saved.PermissionSpecificationId.Should().Be(HeldPermission);
        }

        [Fact]
        public async Task SelfGrantOfAPermissionToTheCallersOwnRoleIsRefusedWhenTheCallerLacksItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission);
            var service = await GrantServiceAsync(dbContext, actor.Name);

            await Assert.ThrowsAsync<GrantForbiddenException>(() =>
                service.InsertAsync(GrantDto(actor.RoleId, UsersPermission)));

            (await dbContext.RoleRegistryPermission.AnyAsync(w =>
                w.RoleRegistryId == actor.RoleId && w.PermissionSpecificationId == UsersPermission)).Should().BeFalse();
        }

        [Fact]
        public async Task UpdateOfAGrantToAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var service = await GrantServiceAsync(dbContext, actor.Name);
            var existing = await dbContext.RoleRegistryPermission.FirstAsync(w => w.RoleRegistryId == target.Id);

            var dto = GrantDto(target.Id, OtherPermission);
            dto.Id = existing.Id;

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.UpdateAsync(dto));

            (await dbContext.RoleRegistryPermission.FirstAsync(w => w.Id == existing.Id)).PermissionSpecificationId
                .Should().Be(HeldPermission);
        }

        [Fact]
        public async Task UpdateOfAnExistingGrantOfAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant, OtherPermission);
            var service = await GrantServiceAsync(dbContext, actor.Name);
            var existing = await dbContext.RoleRegistryPermission.FirstAsync(w => w.RoleRegistryId == target.Id);

            var dto = GrantDto(target.Id, HeldPermission);
            dto.Id = existing.Id;

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfAGrantOfAPermissionTheCallerLacksIsRefusedAndTheRowSurvivesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant, OtherPermission);
            var service = await GrantServiceAsync(dbContext, actor.Name);
            var existing = await dbContext.RoleRegistryPermission.FirstAsync(w => w.RoleRegistryId == target.Id);

            await Assert.ThrowsAsync<GrantForbiddenException>(() => service.DeleteAsync(existing.Id));

            (await dbContext.RoleRegistryPermission.FirstAsync(w => w.Id == existing.Id)).Deleted
                .Should().NotBe((byte)1);
        }

        [Fact]
        public async Task DeleteOfAGrantOfAPermissionTheCallerHoldsIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var target = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var service = await GrantServiceAsync(dbContext, actor.Name);
            var existing = await dbContext.RoleRegistryPermission.FirstAsync(w => w.RoleRegistryId == target.Id);

            await service.DeleteAsync(existing.Id);

            (await service.GetByIdAsync(existing.Id)).Should().BeNull();
        }

        [Fact]
        public async Task LandlordMayGrantAnyPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.LandlordUser);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(GrantDto(target.Id, RolePermissionsPermission));

            saved.PermissionSpecificationId.Should().Be(RolePermissionsPermission);
        }

        [Fact]
        public async Task GrantRefusalIsGenericAndNamesNeitherThePermissionNorTheRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission);
            var target = await CreateRoleAsync(dbContext, tenant);
            var service = await GrantServiceAsync(dbContext, actor.Name);

            var ex = await Assert.ThrowsAsync<GrantForbiddenException>(() =>
                service.InsertAsync(GrantDto(target.Id, UsersPermission)));

            ex.Message.Should().Be(Expected<RoleRegistryPermissionResources>(
                RoleRegistryPermissionResources.InsufficientPrivilege));

            ex.Message.Should().NotContain(UsersPermission.ToString()).And.NotContain(target.Id.ToString());
            ex.RequiredSpecifications.Should().Equal(RolePermissionsPermission);
        }

        [Fact]
        public async Task CreatingAUserInARoleCarryingAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var higher = await CreateRoleAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);
            var name = UniqueName("EscNew");

            await Assert.ThrowsAsync<UserForbiddenException>(() => service.InsertAsync(UserDto(higher.Guid, name)));

            (await dbContext.UserRegistry.AnyAsync(w => w.Name == name)).Should().BeFalse();
        }

        [Fact]
        public async Task CreatingAUserInARoleWithEqualOrFewerPermissionsIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var equal = await CreateRoleAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var empty = await CreateRoleAsync(dbContext, tenant);
            var service = await UserServiceAsync(dbContext, actor.Name);

            foreach (var role in new[] { equal, lower, empty })
            {
                var saved = await service.InsertAsync(UserDto(role.Guid, UniqueName("EscOk")));
                createdUserIds.Add(saved.Id);
            }
        }

        [Fact]
        public async Task ChangingAUsersRoleToOneCarryingAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var higher = await CreateRoleAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);
            var created = await service.InsertAsync(UserDto(lower.Guid, UniqueName("EscMove")));
            createdUserIds.Add(created.Id);

            var dto = UserDto(higher.Guid, created.Name);
            dto.Id = created.Id;

            await Assert.ThrowsAsync<UserForbiddenException>(() => service.UpdateAsync(dto));

            (await dbContext.UserRegistry.FirstAsync(w => w.Id == created.Id)).RoleRegistryGuid
                .Should().Be(lower.Guid);
        }

        [Fact]
        public async Task ChangingAUsersRoleToOneWithEqualOrFewerPermissionsIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var start = await CreateRoleAsync(dbContext, tenant);
            var lower = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);
            var created = await service.InsertAsync(UserDto(start.Guid, UniqueName("EscMoveOk")));
            createdUserIds.Add(created.Id);

            var dto = UserDto(lower.Guid, created.Name);
            dto.Id = created.Id;

            var saved = await service.UpdateAsync(dto);

            saved.RoleRegistryGuid.Should().Be(lower.Guid);
        }

        [Fact]
        public async Task AssigningOneselfARoleCarryingAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission);
            var higher = await CreateRoleAsync(dbContext, tenant, UsersPermission, RolePermissionsPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            var dto = UserDto(higher.Guid, actor.Name);
            dto.Id = actor.UserId;

            await Assert.ThrowsAsync<UserForbiddenException>(() => service.UpdateAsync(dto));

            (await dbContext.UserRegistry.FirstAsync(w => w.Id == actor.UserId)).RoleRegistryGuid
                .Should().Be(actor.RoleGuid);
        }

        [Fact]
        public async Task PasswordResetForAUserWithPermissionsBeyondTheCallersIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var victim = await CreateActorAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);
            var before = (await dbContext.UserRegistry.FirstAsync(w => w.Id == victim.UserId)).Password;

            await Assert.ThrowsAsync<UserForbiddenException>(() =>
                service.UpdatePasswordAsync(new UserRegistryPasswordResetDto { Id = victim.UserId }));

            (await dbContext.UserRegistry.FirstAsync(w => w.Id == victim.UserId)).Password.Should().Be(before);
        }

        [Fact]
        public async Task PasswordResetForAUserWithEqualOrFewerPermissionsAndForOneselfIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var equal = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateActorAsync(dbContext, tenant, HeldPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            foreach (var id in new[] { equal.UserId, lower.UserId, actor.UserId })
            {
                var response = await service.UpdatePasswordAsync(new UserRegistryPasswordResetDto { Id = id });
                response.Password.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task LandlordMayResetThePasswordOfAnyUserOfItsTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.LandlordUser);
            var victim = await CreateActorAsync(dbContext, tenant, UsersPermission, RolePermissionsPermission);
            var service = await UserServiceAsync(dbContext, fx.Seed.LandlordUser);

            var response = await service.UpdatePasswordAsync(new UserRegistryPasswordResetDto { Id = victim.UserId });

            response.Password.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task UserRefusalIsGenericAndNamesNeitherThePermissionNorTheTargetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission);
            var victim = await CreateActorAsync(dbContext, tenant, UsersPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            var ex = await Assert.ThrowsAsync<UserForbiddenException>(() =>
                service.UpdatePasswordAsync(new UserRegistryPasswordResetDto { Id = victim.UserId }));

            ex.Message.Should().Be(Expected<UserRegistryResources>(UserRegistryResources.InsufficientPrivilege));
            ex.Message.Should().NotContain(victim.Name).And.NotContain(OtherPermission.ToString());
        }

        [Fact]
        public async Task ApiKeyForAUserWithPermissionsBeyondTheCallersIsRefusedAndNothingIsStoredAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var victim = await CreateActorAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await ApiKeyServiceAsync(dbContext, actor.Name);

            var ex = await Assert.ThrowsAsync<ApiKeyForbiddenException>(() =>
                service.InsertAsync(new UserRegistryApiKeyDto
                {
                    UserRegistryId = victim.UserId, Name = UniqueName("EscKey"), Description = "x"
                }));

            ex.Message.Should().Be(Expected<UserRegistryApiKeyResources>(
                UserRegistryApiKeyResources.InsufficientPrivilege));

            (await dbContext.UserRegistryApiKey.AnyAsync(w => w.UserRegistryId == victim.UserId)).Should().BeFalse();
        }

        [Fact]
        public async Task ApiKeyForAUserWithEqualOrFewerPermissionsAndForOneselfIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateActorAsync(dbContext, tenant, HeldPermission);
            var service = await ApiKeyServiceAsync(dbContext, actor.Name);

            foreach (var id in new[] { lower.UserId, actor.UserId })
            {
                var saved = await service.InsertAsync(new UserRegistryApiKeyDto
                {
                    UserRegistryId = id, Name = UniqueName("EscKeyOk"), Description = "x"
                });

                createdApiKeyIds.Add(saved.Id);
            }
        }

        [Fact]
        public async Task LandlordMayMintAnApiKeyForAnyUserOfItsTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.LandlordUser);
            var victim = await CreateActorAsync(dbContext, tenant, UsersPermission, RolePermissionsPermission);
            var service = await ApiKeyServiceAsync(dbContext, fx.Seed.LandlordUser);

            var saved = await service.InsertAsync(new UserRegistryApiKeyDto
            {
                UserRegistryId = victim.UserId, Name = UniqueName("EscKeyL"), Description = "x"
            });

            createdApiKeyIds.Add(saved.Id);
        }

        [Fact]
        public async Task ProfileEditOfAMorePrivilegedUserWithoutChangingTheirRoleIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var victim = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            var dto = UserDto(victim.RoleGuid, victim.Name);
            dto.Id = victim.UserId;
            dto.Active = false;
            dto.Email = "taken-over@example.invalid";

            await Assert.ThrowsAsync<UserForbiddenException>(() => service.UpdateAsync(dto));

            var row = await dbContext.UserRegistry.FirstAsync(w => w.Id == victim.UserId);
            row.Active.Should().Be(1);
            row.Email.Should().NotBe(dto.Email);
        }

        [Fact]
        public async Task ProfileEditOfAnEqualOrLowerUserAndOfOneselfIsAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateActorAsync(dbContext, tenant, HeldPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            foreach (var target in new[] { lower, actor })
            {
                var dto = UserDto(target.RoleGuid, target.Name);
                dto.Id = target.UserId;
                dto.Email = $"{Guid.NewGuid():N}@example.invalid";

                (await service.UpdateAsync(dto)).Email.Should().Be(dto.Email);
            }
        }

        [Fact]
        public async Task DeleteAndSessionRevocationOfAMorePrivilegedUserAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var victim = await CreateActorAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await UserServiceAsync(dbContext, actor.Name);

            await Assert.ThrowsAsync<UserForbiddenException>(() => service.DeleteAsync(victim.UserId));
            await Assert.ThrowsAsync<UserForbiddenException>(() => service.RevokeTokensAsync(victim.UserId));

            (await dbContext.UserRegistry.FirstAsync(w => w.Id == victim.UserId)).Deleted.Should().NotBe((byte)1);
        }

        [Fact]
        public async Task DeleteOfALowerUserIsAllowedAndLandlordMayDeleteAnyUserOfItsTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var lower = await CreateActorAsync(dbContext, tenant, HeldPermission);
            var landlordTenant = await TenantOfAsync(dbContext, fx.Seed.LandlordUser);
            var high = await CreateActorAsync(dbContext, landlordTenant, UsersPermission, RolePermissionsPermission);

            await (await UserServiceAsync(dbContext, actor.Name)).DeleteAsync(lower.UserId);
            await (await UserServiceAsync(dbContext, fx.Seed.LandlordUser)).DeleteAsync(high.UserId);
        }

        [Fact]
        public async Task ApiKeyRevocationForAMorePrivilegedUserIsRefusedAndForALowerUserAllowedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, UsersPermission, HeldPermission);
            var higher = await CreateActorAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var lower = await CreateActorAsync(dbContext, tenant, HeldPermission);
            var service = await ApiKeyServiceAsync(dbContext, actor.Name);

            var higherKey = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserRegistryApiKey
            {
                Guid = Guid.NewGuid(), Name = "esc", Description = "esc", UserRegistryId = higher.UserId,
                ApiKey = "hash-" + Guid.NewGuid().ToString("N"), ApiKeyDisplay = "abcd1234",
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Deleted = 0
            });

            var lowerKey = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.UserRegistryApiKey
            {
                Guid = Guid.NewGuid(), Name = "esc", Description = "esc", UserRegistryId = lower.UserId,
                ApiKey = "hash-" + Guid.NewGuid().ToString("N"), ApiKeyDisplay = "abcd1234",
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix, Deleted = 0
            });

            createdApiKeyIds.Add(higherKey);
            createdApiKeyIds.Add(lowerKey);

            await Assert.ThrowsAsync<ApiKeyForbiddenException>(() => service.DeleteAsync(higherKey));
            await service.DeleteAsync(lowerKey);

            (await dbContext.UserRegistryApiKey.FirstAsync(w => w.Id == higherKey)).Deleted.Should().NotBe((byte)1);
        }

        [Fact]
        public async Task RenamingOrDeactivatingARoleCarryingAPermissionTheCallerLacksIsRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolePermissionsPermission, HeldPermission);
            var higher = await CreateRoleAsync(dbContext, tenant, HeldPermission, OtherPermission);
            var service = await RoleServiceAsync(dbContext, actor.Name);
            var before = await dbContext.RoleRegistry.FirstAsync(w => w.Id == higher.Id);

            await Assert.ThrowsAsync<RoleForbiddenException>(() => service.UpdateAsync(
                new Jube.Dto.Repository.RoleRegistry.RoleRegistryDto
                {
                    Id = higher.Id, Name = UniqueName("EscRen"), Active = false
                }));

            await Assert.ThrowsAsync<RoleForbiddenException>(() => service.DeleteAsync(higher.Id));

            var after = await dbContext.RoleRegistry.FirstAsync(w => w.Id == higher.Id);
            after.Name.Should().Be(before.Name);
            after.Active.Should().Be(1);
            after.Deleted.Should().NotBe((byte)1);
        }

        [Fact]
        public async Task RenamingAndDeletingARoleWithEqualOrFewerPermissionsIsAllowedAndLandlordIsExemptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenant = await TenantOfAsync(dbContext, fx.Seed.UserWithPermission);
            var actor = await CreateActorAsync(dbContext, tenant, RolesPermission, RolePermissionsPermission,
                HeldPermission);
            var lower = await CreateRoleAsync(dbContext, tenant, HeldPermission);
            var own = actor.RoleId;
            var landlordTenant = await TenantOfAsync(dbContext, fx.Seed.LandlordUser);
            var high = await CreateRoleAsync(dbContext, landlordTenant, RolePermissionsPermission, UsersPermission);
            var service = await RoleServiceAsync(dbContext, actor.Name);

            await service.UpdateAsync(new Jube.Dto.Repository.RoleRegistry.RoleRegistryDto
            {
                Id = lower.Id, Name = UniqueName("EscRenOk"), Active = true
            });

            await service.UpdateAsync(new Jube.Dto.Repository.RoleRegistry.RoleRegistryDto
            {
                Id = own, Name = UniqueName("EscRenOwn"), Active = true
            });

            await service.DeleteAsync(lower.Id);

            await (await RoleServiceAsync(dbContext, fx.Seed.LandlordUser)).DeleteAsync(high.Id);
        }
    }
}
