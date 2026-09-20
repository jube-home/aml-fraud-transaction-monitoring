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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.CaseWorkflowStatusRole;
using Jube.Service.Reactivity;
using Jube.Service.Repository.CaseWorkflowStatusRole;
using Jube.Service.Repository.RoleRegistry;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RoleGrantAccess
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public abstract class RoleGrantAccessBase(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly string[] grantNames =
        [
            "CaseWorkflowRole", "CaseWorkflowStatusRole", "CaseWorkflowXPathRole", "CaseWorkflowActionRole",
            "CaseWorkflowDisplayRole", "CaseWorkflowFilterRole", "CaseWorkflowFormRole", "CaseWorkflowMacroRole",
            "VisualisationRegistryRole", "VisualisationRegistryDatasourceRole", "VisualisationRegistryParameterRole",
            "EntityAnalysisModelRole"
        ];

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<Guid> createdRoleGuids = [];
        private readonly List<int> createdRoleIds = [];
        private readonly List<string> createdUserNames = [];
        private readonly List<int> createdModelIds = [];
        private readonly List<int> createdWorkflowIds = [];
        private readonly List<int> createdRegistryIds = [];

        protected DatabaseFixture Fx => fx;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var roleGuids = createdRoleGuids.ToList();
            foreach (var grantName in grantNames)
            {
                foreach (var roleGuid in roleGuids)
                {
                    await dbContext.ExecuteAsync($"delete from \"{grantName}\" where \"RoleRegistryGuid\" = @r",
                        new DataParameter("r", roleGuid, DataType.Guid)).ConfigureAwait(false);
                }
            }

            var workflowIds = createdWorkflowIds.ToList();
            await dbContext.CaseWorkflowXPath
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowAction
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowDisplay
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowFilter
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowForm
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowMacro
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflowStatus
                .Where(w => w.CaseWorkflowId != null && workflowIds.Contains(w.CaseWorkflowId.Value)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();

            var modelIds = createdModelIds.ToList();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();

            var registryIds = createdRegistryIds.ToList();
            await dbContext.VisualisationRegistryParameter
                .Where(w => w.VisualisationRegistryId != null && registryIds.Contains(w.VisualisationRegistryId.Value))
                .DeleteAsync();
            await dbContext.VisualisationRegistryDatasource
                .Where(w => w.VisualisationRegistryId != null && registryIds.Contains(w.VisualisationRegistryId.Value))
                .DeleteAsync();
            await dbContext.GetTable<Data.Poco.VisualisationRegistry>().Where(w => registryIds.Contains(w.Id))
                .DeleteAsync();

            var userNames = createdUserNames.ToList();
            var userIds = await dbContext.UserRegistry.Where(u => userNames.Contains(u.Name)).Select(u => u.Id)
                .ToListAsync();
            await dbContext.GetTable<Data.Poco.UserRegistryApiKey>()
                .Where(k => k.UserRegistryId != null && userIds.Contains(k.UserRegistryId.Value)).DeleteAsync();
            await dbContext.UserInTenant.Where(u => userNames.Contains(u.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(u => userNames.Contains(u.Name)).DeleteAsync();

            var roleIds = createdRoleIds.ToList();
            await dbContext.GetTable<Data.Poco.RoleRegistryVersion>()
                .Where(v => roleIds.Contains(v.RoleRegistryId)).DeleteAsync();
            await dbContext.RoleRegistry.Where(r => roleIds.Contains(r.Id)).DeleteAsync();
        }

        protected async Task<RoleGrantWorld> CreateWorldAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var tenantRegistryId = await dbContext.UserInTenant.Where(u => u.User == fx.Seed.UserWithPermission)
                .Select(u => u.TenantRegistryId).FirstAsync();
            var tenantBRoleGuid = await dbContext.UserRegistry.Where(u => u.Name == fx.Seed.UserTenantB)
                .Select(u => u.RoleRegistryGuid).FirstAsync();

            var (roleAId, roleAGuid) = await InsertRoleAsync(dbContext, "RoleA", tenantRegistryId);
            var (roleBId, roleBGuid) = await InsertRoleAsync(dbContext, "RoleB", tenantRegistryId);
            var userA = await InsertUserAsync(dbContext, "UserA", roleAGuid, tenantRegistryId);
            var userB = await InsertUserAsync(dbContext, "UserB", roleBGuid, tenantRegistryId);

            return new RoleGrantWorld(tenantRegistryId, fx.Seed.UserWithPermission, roleAId, roleAGuid, userA, roleBId,
                roleBGuid, userB, tenantBRoleGuid, fx.Seed.UserTenantB);
        }

        private async Task<(int Id, Guid Guid)> InsertRoleAsync(DbContext dbContext, string label,
            int tenantRegistryId)
        {
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = guid,
                Name = $"{DatabaseFixture.Prefix}RG{label}{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            }).ConfigureAwait(false);

            createdRoleIds.Add(id);
            createdRoleGuids.Add(guid);
            return (id, guid);
        }

        private async Task<string> InsertUserAsync(DbContext dbContext, string label, Guid roleGuid,
            int tenantRegistryId)
        {
            var name = $"{DatabaseFixture.Prefix}RG{label}{Guid.NewGuid():N}"[..40];
            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = name,
                Email = $"{name}@example.invalid",
                Password = "not-used-by-access-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            }).ConfigureAwait(false);
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = name, TenantRegistryId = tenantRegistryId })
                .ConfigureAwait(false);

            createdUserNames.Add(name);
            return name;
        }

        protected async Task AddApiKeyAsync(string userName)
        {
            await using var dbContext = fx.GetDbContext();
            var userId = await dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.Id).FirstAsync();
            await dbContext.InsertAsync(new Data.Poco.UserRegistryApiKey
            {
                Guid = Guid.NewGuid(),
                Name = $"{DatabaseFixture.Prefix}Key",
                UserRegistryId = userId,
                ApiKey = Guid.NewGuid().ToString("N"),
                ApiKeyDisplay = "test",
                Deleted = 0,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            }).ConfigureAwait(false);
        }

        protected async Task<(int Id, Guid Guid)> CreateModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var saved = await new Data.Repository.EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission)
                .InsertAsync(new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}RGModel{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return (saved.Id, saved.Guid);
        }

        protected async Task<(int Id, Guid Guid)> CreateWorkflowAsync()
        {
            var (modelId, _) = await CreateModelAsync();

            await using var dbContext = fx.GetDbContext();
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}RGWorkflow{Guid.NewGuid():N}"[..40],
                Guid = guid,
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            createdWorkflowIds.Add(id);
            return (id, guid);
        }

        protected async Task<(int Id, Guid Guid)> CreateRegistryAsync(int tenantRegistryId)
        {
            await using var dbContext = fx.GetDbContext();
            var guid = Guid.NewGuid();
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}RGRegistry{Guid.NewGuid():N}"[..40],
                Guid = guid,
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                ShowInDirectory = 1,
                Version = 1,
                CreatedUser = fx.Seed.UserWithPermission,
                CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            createdRegistryIds.Add(id);
            return (id, guid);
        }

        protected async Task<int> GrantAsync(string grantName, Guid parentGuid, Guid roleGuid)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await CreateGrantServiceAsync(grantName, dbContext, fx.Seed.UserWithPermission);

            var dto = Activator.CreateInstance(GrantDtoType(grantName)).Required();
            SetProperty(dto, ParentProperty(grantName), parentGuid);
            SetProperty(dto, "RoleRegistryGuid", roleGuid);

            var saved = (await InvokeAsync(service, "InsertAsync", dto, CancellationToken.None)).Required();
            var id = saved.GetType().GetProperty("Id").Required().GetValue(saved).Required();
            return (int)id;
        }

        protected async Task RevokeAsync(string grantName, int grantId)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await CreateGrantServiceAsync(grantName, dbContext, fx.Seed.UserWithPermission);

            await InvokeAsync(service, "DeleteAsync", grantId, CancellationToken.None);
        }

        protected async Task ForgeGrantAsync(string grantName, Guid parentGuid, Guid roleGuid)
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.ExecuteAsync(
                $"insert into \"{grantName}\" (\"Guid\", \"{ParentProperty(grantName)}\", \"RoleRegistryGuid\", " +
                "\"CreatedDate\", \"CreatedUser\", \"Deleted\", \"Version\") values (@g, @p, @r, @d, @u, 0, 1)",
                new DataParameter("g", Guid.NewGuid(), DataType.Guid),
                new DataParameter("p", parentGuid, DataType.Guid),
                new DataParameter("r", roleGuid, DataType.Guid),
                new DataParameter("d", DateTime.UtcNow, DataType.DateTime2),
                new DataParameter("u", DatabaseFixture.Prefix, DataType.VarChar)).ConfigureAwait(false);

            if (!createdRoleGuids.Contains(roleGuid))
            {
                createdRoleGuids.Add(roleGuid);
            }
        }

        protected async Task DeleteRoleThroughServiceAsync(int roleId)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await RoleRegistryService.CreateAsync(dbContext, fx.Seed.UserWithPermission, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), TestLog.NoOp);

            await service.DeleteAsync(roleId);
        }

        protected async Task MoveUserToRoleAsync(string userName, Guid roleGuid)
        {
            await using var dbContext = fx.GetDbContext();
            (await dbContext.UserRegistry.Where(u => u.Name == userName)
                .Set(u => u.RoleRegistryGuid, roleGuid).UpdateAsync()).Should().Be(1);
        }

        protected async Task ScenarioNoGrantMeansNoAccessAsync(ArrangedSubject subject, RoleGrantWorld world)
        {
            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse("no grant exists for the entity");
            (await subject.HasAccessAsync(world.UserB)).Should().BeFalse("no grant exists for the entity");
            (await subject.HasAccessAsync(world.TenantBUser)).Should().BeFalse("no grant exists for the entity");
        }

        protected async Task ScenarioGrantGivesAccessToThatRoleOnlyAsync(ArrangedSubject subject, RoleGrantWorld world)
        {
            await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);

            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue("the role of this user was just granted");
            (await subject.HasAccessAsync(world.UserB)).Should().BeFalse("another role's grant must not leak");
            (await subject.HasAccessAsync(world.TenantBUser)).Should().BeFalse("another tenant is never granted");
        }

        protected async Task ScenarioRevokeRemovesAccessAtOnceAsync(ArrangedSubject subject, RoleGrantWorld world)
        {
            var grantId = await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);
            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue();

            await RevokeAsync(subject.GrantName, grantId);

            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse("the grant was removed: no stale access");
        }

        protected async Task ScenarioRegrantAfterRevokeRestoresAccessAsync(ArrangedSubject subject,
            RoleGrantWorld world)
        {
            var grantId = await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);
            await RevokeAsync(subject.GrantName, grantId);
            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse();

            await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);

            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue("a fresh grant after a revoke must work");
        }

        protected async Task ScenarioEachRoleKeepsItsOwnAccessAsync(ArrangedSubject subject, RoleGrantWorld world)
        {
            var grantA = await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);
            var grantB = await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleBGuid);
            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue();
            (await subject.HasAccessAsync(world.UserB)).Should().BeTrue();

            await RevokeAsync(subject.GrantName, grantA);
            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse("role A's grant was removed");
            (await subject.HasAccessAsync(world.UserB)).Should().BeTrue("role B's grant is untouched");

            await RevokeAsync(subject.GrantName, grantB);
            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse();
            (await subject.HasAccessAsync(world.UserB)).Should().BeFalse("both grants are gone");
        }

        protected async Task ScenarioDeletedRoleLosesAccessWithoutErrorsAsync(ArrangedSubject subject,
            RoleGrantWorld world)
        {
            await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);
            await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleBGuid);
            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue();

            await DeleteRoleThroughServiceAsync(world.RoleAId);

            (await subject.HasAccessAsync(world.UserA)).Should()
                .BeFalse("the role was deleted, so its grants convey nothing");
            (await subject.HasAccessAsync(world.UserB)).Should().BeTrue("another role is unaffected");
        }

        protected async Task ScenarioMovingAUserToAnotherRoleMovesTheirAccessAsync(ArrangedSubject subject,
            RoleGrantWorld world)
        {
            await GrantAsync(subject.GrantName, subject.ParentGuid, world.RoleAGuid);
            (await subject.HasAccessAsync(world.UserA)).Should().BeTrue();
            (await subject.HasAccessAsync(world.UserB)).Should().BeFalse();

            await MoveUserToRoleAsync(world.UserA, world.RoleBGuid);
            await MoveUserToRoleAsync(world.UserB, world.RoleAGuid);

            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse("the user left the granted role");
            (await subject.HasAccessAsync(world.UserB)).Should().BeTrue("the user joined the granted role");
        }

        protected async Task ScenarioAnotherTenantsRoleNeverGrantsAsync(ArrangedSubject subject, RoleGrantWorld world)
        {
            await ForgeGrantAsync(subject.GrantName, subject.ParentGuid, world.TenantBRoleGuid);

            (await subject.HasAccessAsync(world.TenantBUser)).Should()
                .BeFalse("a grant row naming another tenant's role must not open this tenant's entity");
            (await subject.HasAccessAsync(world.UserA)).Should().BeFalse();
        }

        private static string ParentProperty(string grantName)
        {
            return grantName[..^"Role".Length] + "Guid";
        }

        private static Type GrantDtoType(string grantName)
        {
            return typeof(CaseWorkflowStatusRoleDto).Assembly
                .GetType($"Jube.Dto.Repository.{grantName}.{grantName}Dto").Required();
        }

        private static async Task<object> CreateGrantServiceAsync(string grantName, DbContext dbContext,
            string userName)
        {
            var serviceType = typeof(CaseWorkflowStatusRoleService).Assembly
                .GetType($"Jube.Service.Repository.{grantName}.{grantName}Service").Required();
            var create = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(m => m.Name == "CreateAsync" && m.GetParameters().Length == 6 &&
                             m.GetParameters()[5].ParameterType == typeof(CancellationToken));

            var created = await InvokeMethodAsync(create, null, dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), CancellationToken.None);
            return created.Required();
        }

        private static void SetProperty(object target, string name, object value)
        {
            target.GetType().GetProperty(name).Required().SetValue(target, value);
        }

        private static Task<object?> InvokeAsync(object target, string method, params object[] arguments)
        {
            return InvokeMethodAsync(target.GetType().GetMethod(method).Required(), target, arguments);
        }

        private static async Task<object?> InvokeMethodAsync(MethodInfo method, object? target,
            params object?[] arguments)
        {
            object? task;
            try
            {
                task = method.Invoke(target, arguments);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException ?? ex).Throw();
                throw;
            }

            if (task is not Task awaitable)
            {
                return task;
            }

            await awaitable.ConfigureAwait(false);
            return awaitable.GetType().GetProperty("Result")?.GetValue(awaitable);
        }
    }
}