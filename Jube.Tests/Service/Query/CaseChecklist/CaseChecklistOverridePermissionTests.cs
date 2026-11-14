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
using Jube.Data.Poco;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseChecklist;
using Jube.Service.Exceptions.Query.CaseChecklist;
using Jube.Service.Query.CaseChecklist;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.Service.Query.CaseChecklist
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseChecklistOverridePermissionTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = Connection(),
                ["ReportConnectionString"] = Connection(),
            });

        private readonly List<Guid> roleGuids = [];
        private int modelId;
        private int workflowId;
        private int statusId;
        private int checklistId;
        private int itemId;
        private int roleId;
        private int caseId;
        private string supervisorName = string.Empty;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;

            var model = await new Data.Repository.EntityAnalysisModelRepository(dbContext, user).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                });
            modelId = model.Id;

            var workflowGuid = Guid.NewGuid();
            workflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = workflowGuid,
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            });

            var statusGuid = Guid.NewGuid();
            statusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 3,
                Version = 1,
                CreatedUser = user,
                CreatedDate = DateTime.UtcNow,
            });

            var checklistGuid = Guid.NewGuid();
            checklistId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowChecklist
            {
                Name = "zzChkPermission", Guid = checklistGuid, CaseWorkflowStatusGuid = statusGuid, Priority = 0,
                Active = 1, Deleted = 0, Version = 1, CreatedUser = user, CreatedDate = DateTime.UtcNow
            });
            itemId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowChecklistItem
            {
                Name = "zzChkPermissionItem", Guid = Guid.NewGuid(), CaseWorkflowChecklistGuid = checklistGuid,
                Priority = 0, Active = 1, Deleted = 0, Version = 1, CreatedUser = user, CreatedDate = DateTime.UtcNow
            });

            var tenantId = await dbContext.UserInTenant.Where(w => w.User == user).Select(s => s.TenantRegistryId)
                .FirstAsync();
            var supervisorRole = Guid.NewGuid();
            supervisorName = $"zzChkSupervisor{Guid.NewGuid():N}"[..24];

            roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = supervisorRole, Name = $"zzChkSupRole{Guid.NewGuid():N}"[..24], Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = "zzChk"
            });

            foreach (var spec in new[] { 1, CaseChecklistService.OverridePermission })
            {
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
                });
            }

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = supervisorRole, Name = supervisorName,
                Email = $"{supervisorName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
            });
            await dbContext.InsertAsync(new UserInTenant { User = supervisorName, TenantRegistryId = tenantId });

            var holderRole = await dbContext.UserRegistry.Where(w => w.Name == user).Select(s => s.RoleRegistryGuid)
                .FirstAsync();

            foreach (var role in new[] { supervisorRole, holderRole })
            {
                roleGuids.Add(role);
                await dbContext.InsertAsync(new CaseWorkflowRole
                {
                    Guid = Guid.NewGuid(), CaseWorkflowGuid = workflowGuid, RoleRegistryGuid = role,
                    CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk", Deleted = 0, Version = 1
                });
                await dbContext.InsertAsync(new CaseWorkflowStatusRole
                {
                    Guid = Guid.NewGuid(), CaseWorkflowStatusGuid = statusGuid, RoleRegistryGuid = role,
                    CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk", Deleted = 0, Version = 1
                });
            }

            caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(), CaseWorkflowGuid = workflowGuid,
                CaseWorkflowStatusGuid = statusGuid, CaseKey = "AccountId", CaseKeyValue = "zzChkPermission",
                Json = "{\"payload\":{}}", CreatedDate = DateTime.UtcNow, Locked = 1,
                LockedUser = "zzSomeoneElse", LockedDate = DateTime.UtcNow, ClosedStatusId = 0, Rating = 0,
                LastClosedStatus = 0
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var holderRole = await dbContext.UserRegistry.Where(w => w.Name == fx.Seed.UserWithPermission)
                .Select(s => s.RoleRegistryGuid).FirstAsync();

            await dbContext.CaseChecklistOverride.Where(w => w.CaseId == caseId).DeleteAsync();
            await dbContext.CaseChecklistEvent.Where(w => w.CaseId == caseId).DeleteAsync();
            await dbContext.CaseChecklistItem.Where(w => w.CaseId == caseId).DeleteAsync();
            await dbContext.CaseEvent.Where(w => w.CaseId == caseId).DeleteAsync();
            await dbContext.Case.Where(w => w.Id == caseId).DeleteAsync();
            await dbContext.CaseWorkflowChecklistVersion.Where(w => w.CaseWorkflowChecklistId == checklistId).DeleteAsync();
            await dbContext.CaseWorkflowChecklistItem.Where(w => w.Id == itemId).DeleteAsync();
            await dbContext.CaseWorkflowChecklist.Where(w => w.Id == checklistId).DeleteAsync();
            await dbContext.CaseWorkflowStatusRole.Where(w => roleGuids.Contains(w.RoleRegistryGuid)
                                                              && w.CreatedUser == "zzChk").DeleteAsync();
            await dbContext.CaseWorkflowRole.Where(w => roleGuids.Contains(w.RoleRegistryGuid)
                                                        && w.CreatedUser == "zzChk").DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => w.Id == statusId).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => w.Id == workflowId).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            await dbContext.UserInTenant.Where(w => w.User == supervisorName).DeleteAsync();
            await dbContext.UserRegistry.Where(w => w.Name == supervisorName).DeleteAsync();
            await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleId).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => w.Id == roleId).DeleteAsync();
            _ = holderRole;
        }

        private static string Connection()
        {
            return Environment.GetEnvironmentVariable("JubeTestConnectionString")
                   ?? Environment.GetEnvironmentVariable("ConnectionString")
                   ?? "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChangeForPg;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;";
        }

        private static Task<CaseChecklistService> ServiceAsync(DbContext dbContext, string user)
        {
            return CaseChecklistService.CreateAsync(dbContext, user, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp,
                new ManualUtcTimeProvider(DateTime.UtcNow));
        }

        [Fact]
        public async Task AUserWithoutPermissionSeventyOneCannotOverrideTheGate()
        {
            await using var dbContext = fx.GetDbContext();
            var ordinary = await ServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await FluentActions.Awaiting(() => ordinary.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "Urgent." })).Should()
                .ThrowAsync<ForbiddenException>();
            (await dbContext.CaseChecklistOverride.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task ASupervisorWithPermissionSeventyOneRecordsTheOverrideAgainstTheirOwnName()
        {
            await using var dbContext = fx.GetDbContext();
            var supervisor = await ServiceAsync(dbContext, supervisorName);

            var granted = await supervisor.OverrideAsync(caseId,
                new CaseChecklistOverrideRequestDto { Reason = "  Customer verified by phone.  " });

            granted.GrantedUser.Should().Be(supervisorName);
            granted.Reason.Should().Be("Customer verified by phone.");
            granted.OpenItems.Should().Equal("zzChkPermissionItem");
            (await dbContext.CaseChecklistEvent.CountAsync(w => w.CaseId == caseId
                && w.ActionId == (byte)CaseChecklistAction.GateOverridden && w.Actor == supervisorName))
                .Should().Be(1);
        }
    }
}
