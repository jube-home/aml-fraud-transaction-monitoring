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
using Jube.Data.Repository;
using Jube.Service.Exceptions.Query.EntityVersionHistory;
using Jube.Service.Query.CaseWorkflowChecklistItemVersionHistory;
using Jube.Service.Query.CaseWorkflowChecklistVersionHistory;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityVersionHistory
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ChecklistVersionHistoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> checklistIds = [];
        private readonly List<int> itemIds = [];
        private readonly List<int> modelIds = [];
        private readonly List<int> roleIds = [];
        private readonly List<int> statusIds = [];
        private readonly List<string> userNames = [];
        private readonly List<int> workflowIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<CaseWorkflowChecklistItemVersion>()
                .Where(w => w.CaseWorkflowChecklistItemId != null && itemIds.Contains(w.CaseWorkflowChecklistItemId.Value))
                .DeleteAsync();
            await dbContext.GetTable<CaseWorkflowChecklistVersion>()
                .Where(w => w.CaseWorkflowChecklistId != null && checklistIds.Contains(w.CaseWorkflowChecklistId.Value))
                .DeleteAsync();
            await dbContext.GetTable<CaseWorkflowChecklistItem>().Where(w => itemIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<CaseWorkflowChecklist>().Where(w => checklistIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => statusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => workflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => modelIds.Contains(w.Id)).DeleteAsync();
            await dbContext.UserInTenant.Where(w => userNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => userNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && roleIds.Contains(w.RoleRegistryId.Value)).DeleteAsync();
            await dbContext.RoleRegistry.Where(w => roleIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<Guid> CreateStatusAsync(DbContext dbContext, string userName)
        {
            var model = await new EntityAnalysisModelRepository(dbContext, userName).InsertAsync(
                new Jube.Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0
                });
            modelIds.Add(model.Id);

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = model.Id,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });
            workflowIds.Add(workflowId);

            var statusGuid = Guid.NewGuid();
            statusIds.Add(await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                Guid = statusGuid,
                CaseWorkflowId = workflowId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Priority = 3,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            }));

            return statusGuid;
        }

        private async Task<CaseWorkflowChecklist> CreateChecklistAsync(DbContext dbContext, string userName,
            Guid statusGuid, string name)
        {
            var checklist = await new CaseWorkflowChecklistRepository(dbContext, userName).InsertAsync(
                new CaseWorkflowChecklist
                {
                    Name = name, CaseWorkflowStatusGuid = statusGuid, Priority = 1, Active = 1, Deleted = 0
                });
            checklistIds.Add(checklist.Id);

            return checklist;
        }

        private async Task<string> CreateUserAsync(DbContext dbContext, string likeUser, params int[] specs)
        {
            var tenantId = await dbContext.UserInTenant.Where(w => w.User == likeUser)
                .Select(s => s.TenantRegistryId).FirstAsync();
            var suffix = Guid.NewGuid().ToString("N")[..10];
            var roleGuid = Guid.NewGuid();
            var userName = $"zzChkHist{suffix}";

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"zzChkHistRole{suffix}", Active = 1, Locked = 0, Deleted = 0,
                TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
            });
            roleIds.Add(roleId);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
                });
            }

            await dbContext.InsertAsync(new UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                Email = $"{userName}@example.invalid", Password = "not-used-by-permission-checks", Active = 1,
                PasswordLocked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = "zzChk"
            });
            await dbContext.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = tenantId });
            userNames.Add(userName);

            return userName;
        }

        [Fact]
        public async Task ChecklistHistoryNeverLeaksAnotherChecklistsVersionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;
            var statusGuid = await CreateStatusAsync(dbContext, userName);
            var repository = new CaseWorkflowChecklistRepository(dbContext, userName);
            var service = await CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext, userName,
                TestLog.NoOp, localizers);

            var checklistA = await CreateChecklistAsync(dbContext, userName, statusGuid, "zzHistA");
            checklistA.Name = "zzHistARenamed";
            await repository.UpdateAsync(checklistA);

            var checklistB = await CreateChecklistAsync(dbContext, userName, statusGuid, "zzHistB");
            checklistB.Name = "zzHistBOnce";
            await repository.UpdateAsync(checklistB);
            checklistB.Name = "zzHistBTwice";
            await repository.UpdateAsync(checklistB);

            var versionsForA = await service.GetByParentIdAsync(checklistA.Id);
            var versionsForB = await service.GetByParentIdAsync(checklistB.Id);

            versionsForA.Should().ContainSingle();
            versionsForB.Should().HaveCount(2);
            versionsForA.Select(v => v.Id).Should().NotIntersectWith(versionsForB.Select(v => v.Id));

            var latestForB = await service.GetLatestAsync(checklistB.Id);
            latestForB!.Id.Should().Be(versionsForB.Max(v => v.Id));
            latestForB.Name.Should().Be("zzHistBOnce");

            var byId = await service.GetByIdAsync(versionsForA[0].Id);
            byId!.Name.Should().Be("zzHistA");
        }

        [Fact]
        public async Task ChecklistCompareReportsTheChangedFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;
            var statusGuid = await CreateStatusAsync(dbContext, userName);
            var repository = new CaseWorkflowChecklistRepository(dbContext, userName);
            var service = await CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext, userName,
                TestLog.NoOp, localizers);

            var checklist = await CreateChecklistAsync(dbContext, userName, statusGuid, "zzCompareOne");
            checklist.Name = "zzCompareTwo";
            checklist.Priority = 7;
            await repository.UpdateAsync(checklist);
            checklist.Name = "zzCompareThree";
            await repository.UpdateAsync(checklist);

            var versions = (await service.GetByParentIdAsync(checklist.Id)).OrderBy(v => v.Id).ToList();
            var changes = await service.CompareAsync(versions[0].Id, versions[1].Id);

            changes.Select(c => c.PropertyName).Should().Contain(["Name", "Priority"]);
            changes.Single(c => c.PropertyName == "Priority").FromValue.Should().Be(1);
            changes.Single(c => c.PropertyName == "Priority").ToValue.Should().Be(7);
        }

        [Fact]
        public async Task ChecklistHistoryOfAnotherTenantIsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;
            var statusGuid = await CreateStatusAsync(dbContext, userName);
            var checklist = await CreateChecklistAsync(dbContext, userName, statusGuid, "zzTenantOne");
            checklist.Name = "zzTenantTwo";
            await new CaseWorkflowChecklistRepository(dbContext, userName).UpdateAsync(checklist);

            var own = await CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext, userName,
                TestLog.NoOp, localizers);
            var other = await CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext,
                fx.Seed.UserTenantB, TestLog.NoOp, localizers);

            var ownVersions = await own.GetByParentIdAsync(checklist.Id);
            ownVersions.Should().ContainSingle();

            (await other.GetByParentIdAsync(checklist.Id)).Should().BeEmpty();
            (await other.GetLatestAsync(checklist.Id)).Should().BeNull();
            (await other.GetByIdAsync(ownVersions[0].Id)).Should().BeNull();
            (await other.GetByDateRangeAsync(checklist.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)))
                .Should().BeEmpty();
            (await other.CompareAsync(ownVersions[0].Id, ownVersions[0].Id)).Should().BeEmpty();
        }

        [Fact]
        public async Task ChecklistHistoryRequiresPermissionNineteenAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var service = await CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext,
                fx.Seed.UserWithoutPermission, TestLog.NoOp, localizers);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByParentIdAsync(1));
        }

        [Fact]
        public async Task ItemHistoryHidesBothSqlFieldsWithoutPermissionSixtySevenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var author = fx.Seed.UserWithPermission;
            var statusGuid = await CreateStatusAsync(dbContext, author);
            var checklist = await CreateChecklistAsync(dbContext, author, statusGuid, "zzItemParent");
            var reader = await CreateUserAsync(dbContext, author, 19);
            var sqlReader = await CreateUserAsync(dbContext, author, 19, 67);

            var itemRepository = new CaseWorkflowChecklistItemRepository(dbContext, author);
            var item = await itemRepository.InsertAsync(new CaseWorkflowChecklistItem
            {
                Name = "zzItemOne", CaseWorkflowChecklistGuid = checklist.Guid, Priority = 1, Active = 1, Deleted = 0,
                ShowSql = "select true", ValidateSql = "select true", ValidateMessage = "first", SlaMinutes = 30
            });
            itemIds.Add(item.Id);

            item.ShowSql = "select false";
            item.ValidateSql = "select false";
            item.ValidateMessage = "second";
            await itemRepository.UpdateAsync(item);
            item.Name = "zzItemTwo";
            await itemRepository.UpdateAsync(item);

            var withSql = await CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext, sqlReader,
                TestLog.NoOp, localizers);
            var withoutSql = await CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext, reader,
                TestLog.NoOp, localizers);

            var full = (await withSql.GetByParentIdAsync(item.Id)).OrderBy(v => v.Id).ToList();
            full.Should().HaveCount(2);
            full[0].ShowSql.Should().Be("select true");
            full[0].ValidateSql.Should().Be("select true");

            var redacted = (await withoutSql.GetByParentIdAsync(item.Id)).OrderBy(v => v.Id).ToList();
            redacted.Should().HaveCount(2);
            redacted.Should().OnlyContain(v => v.ShowSql == null && v.ValidateSql == null);
            redacted[0].Name.Should().Be("zzItemOne");
            redacted[0].ValidateMessage.Should().Be("first");
            redacted[0].SlaMinutes.Should().Be(30);

            (await withoutSql.GetByIdAsync(full[0].Id))!.ShowSql.Should().BeNull();
            (await withoutSql.GetLatestAsync(item.Id))!.ValidateSql.Should().BeNull();
            (await withoutSql.GetByDateRangeAsync(item.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1)))
                .Should().OnlyContain(v => v.ShowSql == null && v.ValidateSql == null);
            (await withoutSql.GetByIdAndDateRangeAsync(full[0].Id, DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1)))!.ValidateSql.Should().BeNull();

            var sqlChanges = await withSql.CompareAsync(full[0].Id, full[1].Id);
            sqlChanges.Select(c => c.PropertyName).Should().Contain(["ShowSql", "ValidateSql", "ValidateMessage"]);

            var redactedChanges = await withoutSql.CompareAsync(full[0].Id, full[1].Id);
            redactedChanges.Select(c => c.PropertyName).Should().NotContain(["ShowSql", "ValidateSql"]);
            redactedChanges.Select(c => c.PropertyName).Should().Contain("ValidateMessage");
        }

        [Fact]
        public async Task ItemHistoryOfAnotherTenantIsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var author = fx.Seed.UserWithPermission;
            var statusGuid = await CreateStatusAsync(dbContext, author);
            var checklist = await CreateChecklistAsync(dbContext, author, statusGuid, "zzItemTenant");

            var itemRepository = new CaseWorkflowChecklistItemRepository(dbContext, author);
            var item = await itemRepository.InsertAsync(new CaseWorkflowChecklistItem
            {
                Name = "zzTenantItemOne", CaseWorkflowChecklistGuid = checklist.Guid, Priority = 0, Active = 1,
                Deleted = 0
            });
            itemIds.Add(item.Id);
            item.Name = "zzTenantItemTwo";
            await itemRepository.UpdateAsync(item);

            var own = await CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext, author,
                TestLog.NoOp, localizers);
            var other = await CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext,
                fx.Seed.UserTenantB, TestLog.NoOp, localizers);

            var ownVersions = await own.GetByParentIdAsync(item.Id);
            ownVersions.Should().ContainSingle();

            (await other.GetByParentIdAsync(item.Id)).Should().BeEmpty();
            (await other.GetLatestAsync(item.Id)).Should().BeNull();
            (await other.GetByIdAsync(ownVersions[0].Id)).Should().BeNull();
            (await other.CompareAsync(ownVersions[0].Id, ownVersions[0].Id)).Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task BothHistoryServicesRefuseABlankUserAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext, userName, TestLog.NoOp,
                    localizers));
        }

        [Fact]
        public async Task BothHistoryServicesRefuseAnUnknownUserAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowChecklistVersionHistoryService.CreateAsync(dbContext, fx.Seed.UnknownUser, TestLog.NoOp,
                    localizers));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowChecklistItemVersionHistoryService.CreateAsync(dbContext, fx.Seed.UnknownUser,
                    TestLog.NoOp, localizers));
        }
    }
}
