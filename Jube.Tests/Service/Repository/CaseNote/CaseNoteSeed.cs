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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;

namespace Jube.Test.Service.Repository.CaseNote
{
    public sealed class CaseNoteSeed : IAsyncDisposable
    {
        private readonly List<string> createdUsers = [];
        private readonly List<int> createdRoleIds = [];
        private readonly DatabaseFixture fx;

        private CaseNoteSeed(DatabaseFixture fx)
        {
            this.fx = fx;
        }

        public int ModelId { get; private set; }
        public int CaseWorkflowId { get; private set; }
        public Guid CaseWorkflowGuid { get; private set; }
        public Guid CaseWorkflowStatusGuid { get; private set; }
        public int ActionId { get; private set; }
        public int CaseId { get; private set; }
        public string CaseKey { get; } = "account";
        public string CaseKeyValue { get; } = $"acc-{Guid.NewGuid():N}"[..16];

        public static async Task<CaseNoteSeed> CreateAsync(DatabaseFixture fx, string userName,
            bool enableNarrative = false)
        {
            var seed = new CaseNoteSeed(fx);
            await seed.BuildAsync(userName, enableNarrative).ConfigureAwait(false);

            return seed;
        }

        public async Task<string> CreateUserAsync(string like, params int[] specs)
        {
            await using var db = fx.GetDbContext();
            var tenantId = await db.UserInTenant.Where(w => w.User == like).Select(s => s.TenantRegistryId)
                .FirstAsync().ConfigureAwait(false);

            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var name = $"{DatabaseFixture.Prefix}Note{suffix}";

            var roleId = await db.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid, Name = name, Active = 1, Locked = 0, Deleted = 0, TenantRegistryId = tenantId,
                Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            }).ConfigureAwait(false);

            createdRoleIds.Add(roleId);

            foreach (var spec in specs)
            {
                await db.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = spec, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                }).ConfigureAwait(false);
            }

            await db.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = name, Email = $"{name}@example.invalid",
                Password = "not-used", Active = 1, PasswordLocked = 0, Deleted = 0, Version = 1,
                CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
            }).ConfigureAwait(false);

            await db.InsertAsync(new Data.Poco.UserInTenant { User = name, TenantRegistryId = tenantId }).ConfigureAwait(false);
            createdUsers.Add(name);

            await db.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = CaseWorkflowGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid,
                CreatedUser = name, CreatedDate = DateTime.UtcNow, Deleted = 0
            }).ConfigureAwait(false);

            await db.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = CaseWorkflowStatusGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid,
                CreatedUser = name, CreatedDate = DateTime.UtcNow, Deleted = 0
            }).ConfigureAwait(false);

            return name;
        }

        public async Task LockToAsync(string? user)
        {
            await using var db = fx.GetDbContext();

            await db.Case.Where(w => w.Id == CaseId)
                .Set(w => w.Locked, (byte)(user is null ? 0 : 1))
                .Set(w => w.LockedUser, user)
                .UpdateAsync().ConfigureAwait(false);
        }

        public async Task<int> CreateNoteAsync(string user, int kindId = 1, int sourceId = 1, int? caseId = null,
            string? text = null)
        {
            await using var db = fx.GetDbContext();

            return await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseNote
            {
                Note = text ?? $"note {Guid.NewGuid():N}", ActionId = ActionId, PriorityId = 2,
                CaseId = caseId ?? CaseId, CaseKey = CaseKey, CaseKeyValue = CaseKeyValue, CreatedUser = user,
                CreatedDate = DateTime.UtcNow, SourceId = sourceId, KindId = kindId, Version = 1
            }).ConfigureAwait(false);
        }

        public async ValueTask DisposeAsync()
        {
            await using var db = fx.GetDbContext();

            await db.GetTable<Data.Poco.CaseFile>().Where(w => w.CaseId == CaseId).DeleteAsync().ConfigureAwait(false);
            await db.CaseResearchCitation.Where(c => db.CaseResearch.Where(r => r.CaseId == CaseId)
                .Select(r => r.Id).Contains(c.CaseResearchId)).DeleteAsync().ConfigureAwait(false);
            await db.CaseResearch.Where(w => w.CaseId == CaseId).DeleteAsync().ConfigureAwait(false);
            await db.CaseNote.Where(w => w.CaseId == CaseId).DeleteAsync().ConfigureAwait(false);
            await db.Case.Where(w => w.Id == CaseId).DeleteAsync().ConfigureAwait(false);

            await db.GetTable<Data.Poco.CaseWorkflowActionRole>()
                .Where(w => db.CaseWorkflowAction.Where(a => a.Id == ActionId).Select(a => a.Guid)
                    .Contains(w.CaseWorkflowActionGuid)).DeleteAsync().ConfigureAwait(false);

            await db.CaseWorkflowAction.Where(w => w.Id == ActionId).DeleteAsync().ConfigureAwait(false);
            await db.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => w.CaseWorkflowStatusGuid == CaseWorkflowStatusGuid).DeleteAsync().ConfigureAwait(false);
            await db.GetTable<Data.Poco.CaseWorkflowRole>().Where(w => w.CaseWorkflowGuid == CaseWorkflowGuid).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseWorkflowStatus.Where(w => w.Guid == CaseWorkflowStatusGuid).DeleteAsync()
                .ConfigureAwait(false);
            await db.CaseWorkflow.Where(w => w.Id == CaseWorkflowId).DeleteAsync().ConfigureAwait(false);
            await db.GetTable<Data.Poco.EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == ModelId)
                .DeleteAsync().ConfigureAwait(false);
            await db.EntityAnalysisModel.Where(w => w.Id == ModelId).DeleteAsync().ConfigureAwait(false);
            await db.UserInTenant.Where(w => createdUsers.Contains(w.User)).DeleteAsync().ConfigureAwait(false);
            await db.UserRegistry.Where(w => createdUsers.Contains(w.Name)).DeleteAsync().ConfigureAwait(false);
            await db.RoleRegistryPermission.Where(w => createdRoleIds.Contains(w.RoleRegistryId!.Value))
                .DeleteAsync().ConfigureAwait(false);
            await db.RoleRegistry.Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync().ConfigureAwait(false);
        }

        private async Task BuildAsync(string userName, bool enableNarrative)
        {
            await using var db = fx.GetDbContext();

            var model = await new Data.Repository.EntityAnalysisModelRepository(db, userName).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(),
                    Active = 1, Locked = 0, Deleted = 0
                }).ConfigureAwait(false);

            ModelId = model.Id;

            var roleRegistryGuid = await db.UserRegistry.Where(u => u.Name == userName)
                .Select(u => u.RoleRegistryGuid).FirstAsync().ConfigureAwait(false);

            CaseWorkflowGuid = Guid.NewGuid();

            CaseWorkflowId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40], Guid = CaseWorkflowGuid,
                EntityAnalysisModelId = ModelId, Active = 1, Locked = 0, Deleted = 0, CreatedUser = userName,
                CreatedDate = DateTime.UtcNow, EnableNarrative = (byte)(enableNarrative ? 1 : 0)
            }).ConfigureAwait(false);

            await db.InsertAsync(new Data.Poco.CaseWorkflowRole
            {
                CaseWorkflowGuid = CaseWorkflowGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName, CreatedDate = DateTime.UtcNow, Deleted = 0
            }).ConfigureAwait(false);

            CaseWorkflowStatusGuid = Guid.NewGuid();

            await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40], Guid = CaseWorkflowStatusGuid,
                CaseWorkflowId = CaseWorkflowId, Active = 1, Locked = 0, Deleted = 0, Priority = 1,
                CreatedUser = userName, CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            await db.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
            {
                CaseWorkflowStatusGuid = CaseWorkflowStatusGuid, Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid, CreatedUser = userName, CreatedDate = DateTime.UtcNow,
                Deleted = 0
            }).ConfigureAwait(false);

            var actionGuid = Guid.NewGuid();

            ActionId = await db.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowAction
            {
                Name = $"{DatabaseFixture.Prefix}Action{Guid.NewGuid():N}"[..40], Guid = actionGuid,
                CaseWorkflowId = CaseWorkflowId, Active = 1, Locked = 0, Deleted = 0, EnableNotification = 0,
                EnableHttpEndpoint = 0, CreatedUser = userName, CreatedDate = DateTime.UtcNow
            }).ConfigureAwait(false);

            await db.InsertAsync(new Data.Poco.CaseWorkflowActionRole
            {
                CaseWorkflowActionGuid = actionGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName, CreatedDate = DateTime.UtcNow, Deleted = 0
            }).ConfigureAwait(false);

            CaseId = await db.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                CaseWorkflowGuid = CaseWorkflowGuid, CaseWorkflowStatusGuid = CaseWorkflowStatusGuid,
                CaseKey = CaseKey, CaseKeyValue = CaseKeyValue, Json = "{}", CreatedDate = DateTime.UtcNow,
                ClosedStatusId = 0
            }).ConfigureAwait(false);
        }
    }
}
