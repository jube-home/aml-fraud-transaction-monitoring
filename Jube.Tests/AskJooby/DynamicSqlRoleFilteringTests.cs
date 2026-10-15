/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Data.Poco;
using Jube.Service.DynamicSql;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    [Collection("Database")]
    public sealed class DynamicSqlRoleFilteringTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<int> createdTenantIds = [];
        private readonly List<string> createdUsers = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (createdModelIds.Count > 0)
            {
                var modelGuids = await dbContext.EntityAnalysisModel
                    .Where(w => createdModelIds.Contains(w.Id)).Select(w => w.Guid).ToListAsync();

                await dbContext.EntityAnalysisModelRole
                    .Where(w => modelGuids.Contains(w.EntityAnalysisModelGuid)).DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
            }

            if (createdUsers.Count > 0)
            {
                await dbContext.UserInTenant.Where(w => createdUsers.Contains(w.User)).DeleteAsync();
                await dbContext.UserRegistry.Where(w => createdUsers.Contains(w.Name)).DeleteAsync();
            }

            if (createdRoleIds.Count > 0)
            {
                await dbContext.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            }

            if (createdTenantIds.Count > 0)
            {
                await dbContext.TenantRegistry.Where(w => createdTenantIds.Contains(w.Id)).DeleteAsync();
            }
        }

        [Fact]
        public async Task
            EntityAnalysisModelWithARoleAssignedIsOnlyVisibleToAUserHoldingThatRoleNotJustAnyTenantMemberAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N");

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}RoleFilterTenant{suffix}", Active = 1, Locked = 0, Deleted = 0,
                Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var modelGuid = Guid.NewGuid();

            var modelId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Guid = modelGuid, Name = $"{DatabaseFixture.Prefix}RoleFilterModel{suffix}"[..40], Active = 1,
                Locked = 0, Deleted = 0, TenantRegistryId = tenantId
            });
            createdModelIds.Add(modelId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}RoleFilterRole{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            await dbContext.InsertAsync(new EntityAnalysisModelRole
            {
                Guid = Guid.NewGuid(), EntityAnalysisModelGuid = modelGuid, RoleRegistryGuid = roleGuid,
                Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });

            var grantedUser = $"{DatabaseFixture.Prefix}RoleFilterGranted{suffix}";
            var ungrantedUser = $"{DatabaseFixture.Prefix}RoleFilterUngranted{suffix}";
            createdUsers.Add(grantedUser);
            createdUsers.Add(ungrantedUser);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = grantedUser,
                Email = $"{grantedUser}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            var unrelatedRoleGuid = Guid.NewGuid();

            var unrelatedRoleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = unrelatedRoleGuid, Name = $"{DatabaseFixture.Prefix}RoleFilterUnrelatedRole{suffix}",
                Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(unrelatedRoleId);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = unrelatedRoleGuid, Name = ungrantedUser,
                Email = $"{ungrantedUser}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new UserInTenant { User = grantedUser, TenantRegistryId = tenantId });
            await dbContext.InsertAsync(new UserInTenant { User = ungrantedUser, TenantRegistryId = tenantId });

            var connectionString = (Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                    ?? Environment.GetEnvironmentVariable("ConnectionString"))!;

            var grantedClient = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, grantedUser, _ => true);
            var ungrantedClient = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, ungrantedUser, _ => true);

            var grantedRows = await grantedClient.ExecuteAsync(
                $"SELECT \"Guid\" FROM \"EntityAnalysisModel\" WHERE \"Guid\" = '{modelGuid}'");
            var ungrantedRows = await ungrantedClient.ExecuteAsync(
                $"SELECT \"Guid\" FROM \"EntityAnalysisModel\" WHERE \"Guid\" = '{modelGuid}'");

            grantedRows.Should().ContainSingle(
                "the granted user holds a real EntityAnalysisModelRole grant for this model");

            ungrantedRows.Should().BeEmpty(
                "tenant membership alone must not be enough once a model DOES have a role assigned " +
                "-- the ungranted user has no role granting access to this specific model, only to " +
                "the tenant itself");
        }

        [Fact]
        public async Task EntityAnalysisModelWithNoRolesAssignedAtAllIsVisibleToAnyTenantMemberAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var suffix = Guid.NewGuid().ToString("N");

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}RoleFilterNoRoleTenant{suffix}", Active = 1, Locked = 0,
                Deleted = 0, Landlord = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var modelGuid = Guid.NewGuid();

            var modelId = await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModel
            {
                Guid = modelGuid, Name = $"{DatabaseFixture.Prefix}RoleFilterNoRoleModel{suffix}"[..40],
                Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId
            });
            createdModelIds.Add(modelId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}RoleFilterNoRoleUserRole{suffix}", Active = 1,
                Locked = 0, Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            var plainTenantMember = $"{DatabaseFixture.Prefix}RoleFilterPlainMember{suffix}";
            createdUsers.Add(plainTenantMember);

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = plainTenantMember,
                Email = $"{plainTenantMember}@example.invalid", Password = "not-used-by-permission-checks",
                Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            await dbContext.InsertAsync(new UserInTenant { User = plainTenantMember, TenantRegistryId = tenantId });

            var connectionString = (Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                    ?? Environment.GetEnvironmentVariable("ConnectionString"))!;

            var client = new AskJoobySqlQueryClient(
                connectionString, dbContext, TestLog.NoOp, plainTenantMember, _ => true);

            var rows = await client.ExecuteAsync(
                $"SELECT \"Guid\" FROM \"EntityAnalysisModel\" WHERE \"Guid\" = '{modelGuid}'");

            rows.Should().ContainSingle(
                "a model with zero EntityAnalysisModelRole rows -- the normal post-creation state " +
                "-- must be visible to any tenant member, matching EntityAnalysisModelRepository's " +
                "own plain list/get behaviour, which applies no role filter at all");
        }
    }
}