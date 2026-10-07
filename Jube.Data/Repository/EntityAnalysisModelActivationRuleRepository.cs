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

using Jube.Data.Query.GetApprovedEntityQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Jube.Data.Context;
using Jube.Data.Poco;
using LinqToDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jube.Data.Repository
{
    public class EntityAnalysisModelActivationRuleRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public EntityAnalysisModelActivationRuleRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;
            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public EntityAnalysisModelActivationRuleRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public EntityAnalysisModelActivationRuleRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public Task<bool> ParentModelVisibleAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            return EntityAnalysisModelParentGuard.IsVisibleAsync(dbContext, tenantRegistryId, entityAnalysisModelId,
                token);
        }

        public Task<EntityAnalysisModelActivationRule> GetByNameEntityAnalysisModelIdAsync(string name,
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRule
                .FirstOrDefaultAsync(f =>
                    f.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                    && f.EntityAnalysisModelId == entityAnalysisModelId
                    && (f.Deleted == 0 || f.Deleted == null)
                    && f.Name.ToLower() == name.ToLower(), token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>> GetAsync(CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && (w.Deleted == 0 || w.Deleted == null))
                .ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>> GetByEntityAnalysisModelIdOrderByIdDescAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>>
            GetByEntityAnalysisModelIdOrderByNameDescAsync(
                int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId && (w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Name).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>>
            GetByEntityAnalysisModelIdOrderByPriorityDescAsync(
                int entityAnalysisModelId, CancellationToken token = default, bool includeDeleted = false)
        {
            return await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId &&
                    (includeDeleted || w.Deleted == 0 || w.Deleted == null))
                .OrderBy(o => o.Priority).ToListAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>>
            GetByEntityAnalysisModelIdInPriorityOrderAsync(
                int entityAnalysisModelId, CancellationToken token = default)
        {
            return await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId
                    && (w.Deleted == 0 || w.Deleted == null))
                .Where(s => s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                            s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                .OrderBy(o => o.Priority)
                .ThenByDescending(o => o.EnableResponseElevation)
                .ThenByDescending(o => o.ResponseElevation)
                .ThenByDescending(o => o.EnableCaseWorkflow)
                .ThenBy(o => o.CaseWorkflowStatus.Priority)
                .ThenByDescending(o => o.EnableNotification)
                .ThenBy(o => o.Name).ToListAsync(token).ConfigureAwait(false);
        }

        public Task<EntityAnalysisModelActivationRule> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRule.FirstOrDefaultAsync(w =>
                (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                && w.Id == id, token);
        }

        public async Task<EntityAnalysisModelActivationRule> InsertAsync(EntityAnalysisModelActivationRule model,
            CancellationToken token = default)
        {
            model.CreatedUser = userName ?? model.CreatedUser;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token);
            return model;
        }

        public Task UpdateCounterAsync(int id, long evaluationCounter, long activationCounter,
            long forcedActivationCounter,
            DateTime activationCounterDate, CancellationToken token = default)
        {
            return dbContext.InTransactionAsync(async () =>
            {
                var records = await dbContext.EntityAnalysisModelActivationRule
                    .Where(d =>
                        (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                        && d.Id == id
                        && (d.Deleted == 0 || d.Deleted == null))
                    .Set(s => s.ActivationCounter, s => (s.ActivationCounter ?? 0) + activationCounter)
                    .Set(s => s.EvaluationCounter, s => (s.EvaluationCounter ?? 0) + evaluationCounter)
                    .Set(s => s.ForcedActivationCounter,
                        s => (s.ForcedActivationCounter ?? 0) + forcedActivationCounter)
                    .Set(s => s.ActivationCounterDate,
                        s => s.ActivationCounterDate == null || s.ActivationCounterDate < activationCounterDate
                            ? activationCounterDate
                            : s.ActivationCounterDate)
                    .UpdateAsync(token).ConfigureAwait(false);

                await new EntityAnalysisModelActivationRuleCounterHistoryRepository(dbContext)
                    .InsertAsync(id, evaluationCounter, activationCounter, forcedActivationCounter, Dns.GetHostName(),
                        token)
                    .ConfigureAwait(false);

                if (records == 0)
                {
                    throw new KeyNotFoundException();
                }
            }, token);
        }

        public async Task ResetCounterAsync(int id, CancellationToken token = default)
        {
            var records = await dbContext.EntityAnalysisModelActivationRule
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ActivationCounter, 0L)
                .Set(s => s.EvaluationCounter, 0L)
                .UpdateAsync(token).ConfigureAwait(false);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }
        }

        public Task UpdateCompileStatusAsync(int id, bool compiled, string compileError,
            CancellationToken token = default)
        {
            var compiledValue = Convert.ToByte(compiled ? 1 : 0);

            var query = dbContext.EntityAnalysisModelActivationRule
                .Where(d => (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                            && d.Id == id);

            query = compileError == null
                ? query.Where(d => d.Compiled == null || d.Compiled != compiledValue || d.CompileError != null)
                : query.Where(d => d.Compiled == null || d.Compiled != compiledValue || d.CompileError == null
                                   || d.CompileError != compileError);

            return query
                .Set(s => s.Compiled, compiledValue)
                .Set(s => s.CompileError, compileError)
                .UpdateAsync(token);
        }

        public async Task<EntityAnalysisModelActivationRule> UpdateAsync(EntityAnalysisModelActivationRule model,
            CancellationToken token = default)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRule
                .FirstOrDefaultAsync(w => w.Id
                                          == model.Id
                                          && (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId ||
                                              !tenantRegistryId.HasValue)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            if (!await ParentModelVisibleAsync(model.EntityAnalysisModelId.GetValueOrDefault(), token)
                    .ConfigureAwait(false))
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Deleted = 0;
            model.DeletedDate = null;
            model.DeletedUser = null;
            model.Guid = existing.Guid;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            await dbContext.UpdateAsync(model, token: token);

            var mapper = new Mapper(new MapperConfiguration(
                cfg =>
                {
                    cfg.CreateMap<EntityAnalysisModelActivationRule, EntityAnalysisModelActivationRuleVersion>();
                }, NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelActivationRuleVersion>(existing);
            audit.EntityAnalysisModelActivationRuleId = existing.Id;

            if (existing.Deleted != 1)
            {
                await dbContext.InsertAsync(audit, token: token);
            }

            return model;
        }

        public Task DeleteAsync(int id, CancellationToken token = default)
        {
            return dbContext.InTransactionAsync(() => DeleteVersionedAsync(id, token), token);
        }

        private async Task DeleteVersionedAsync(int id, CancellationToken token)
        {
            var existing = await dbContext.EntityAnalysisModelActivationRule
                .FirstOrDefaultAsync(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.Id == id
                    && (w.Locked == 0 || w.Locked == null)
                    && (w.Deleted == 0 || w.Deleted == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            var mapper = new Mapper(new MapperConfiguration(
                cfg =>
                {
                    cfg.CreateMap<EntityAnalysisModelActivationRule, EntityAnalysisModelActivationRuleVersion>();
                },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<EntityAnalysisModelActivationRuleVersion>(existing);
            audit.EntityAnalysisModelActivationRuleId = existing.Id;

            await dbContext.InsertAsync(audit, token: token);

            var records = await dbContext.EntityAnalysisModelActivationRule
                .Where(d =>
                    (d.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && d.Id == id
                    && (d.Locked == 0 || d.Locked == null)
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.DeletedUser, userName)
                .Set(s => s.Version, (existing.Version ?? 1) + 1)
                .UpdateAsync(token);

            if (records == 0)
            {
                throw new KeyNotFoundException();
            }

            var deletedState = mapper.Map<EntityAnalysisModelActivationRuleVersion>(existing);
            deletedState.EntityAnalysisModelActivationRuleId = existing.Id;
            deletedState.Deleted = Convert.ToByte(1);
            deletedState.DeletedDate = DateTime.UtcNow;
            deletedState.DeletedUser = userName;
            deletedState.Version = (existing.Version ?? 1) + 1;

            await dbContext.InsertAsync(deletedState, token: token);
        }

        public Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance, int importId,
            CancellationToken token = default)
        {
            return dbContext.EntityAnalysisModelActivationRule
                .Where(d =>
                    d.EntityAnalysisModel.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                    && (d.Deleted == 0 || d.Deleted == null))
                .Set(s => s.ImportId, importId)
                .Set(s => s.Deleted, Convert.ToByte(1))
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .UpdateAsync(token);
        }

        public async Task<IEnumerable<EntityAnalysisModelActivationRule>>
            GetApprovedByEntityAnalysisModelIdInPriorityOrderAsync(
                int entityAnalysisModelId, int approvalsRequired = 1,
                CancellationToken token = default)
        {
            var current = await dbContext.EntityAnalysisModelActivationRule
                .Where(w =>
                    (w.EntityAnalysisModel.TenantRegistryId == tenantRegistryId || !tenantRegistryId.HasValue)
                    && w.EntityAnalysisModelId == entityAnalysisModelId)
                .Where(s => s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                            s.CaseWorkflowStatus.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                .OrderBy(o => o.Priority)
                .ThenByDescending(o => o.EnableResponseElevation)
                .ThenByDescending(o => o.ResponseElevation)
                .ThenByDescending(o => o.EnableCaseWorkflow)
                .ThenBy(o => o.CaseWorkflowStatus.Priority)
                .ToListAsync(token).ConfigureAwait(false);

            return await new GetApprovedEntityQuery<EntityAnalysisModelActivationRule,
                    EntityAnalysisModelActivationRuleVersion>(dbContext,
                    EntityApprovalKind.EntityAnalysisModelActivationRule,
                    nameof(EntityAnalysisModelActivationRuleVersion.EntityAnalysisModelActivationRuleId),
                    tenantRegistryId)
                .ExecuteAsync(current, approvalsRequired, token).ConfigureAwait(false);
        }
    }
}