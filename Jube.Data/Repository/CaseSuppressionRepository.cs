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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using Dto.Options;
    using Models;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Poco;

    public class CaseSuppressionRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseSuppressionRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        private IQueryable<CaseSuppression> Visible()
        {
            return dbContext.CaseSuppression.Where(w
                => tenantRegistryId.HasValue
                   && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId
                   && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                       w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                   && (w.Case.CaseWorkflow.Deleted == 0 || w.Case.CaseWorkflow.Deleted == null)
                   && (w.Case.CaseWorkflowStatus.Deleted == 0 || w.Case.CaseWorkflowStatus.Deleted == null)
                   && dbContext.CaseWorkflowRole
                       .Where(r => r.CaseWorkflowGuid == w.Case.CaseWorkflow.Guid
                                   && (r.Deleted == 0 || r.Deleted == null))
                       .Any(r => dbContext.RoleRegistry
                           .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                           .Any(rr => dbContext.UserRegistry
                               .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                   && dbContext.CaseWorkflowStatusRole
                       .Where(r => r.CaseWorkflowStatusGuid == w.Case.CaseWorkflowStatus.Guid
                                   && (r.Deleted == 0 || r.Deleted == null))
                       .Any(r => dbContext.RoleRegistry
                           .Where(rr => rr.Guid == r.RoleRegistryGuid && (rr.Deleted == 0 || rr.Deleted == null))
                           .Any(rr => dbContext.UserRegistry
                               .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName))));
        }

        public Task<CaseSuppression> GetByIdAsync(int id, CancellationToken token = default)
        {
            return Visible().FirstOrDefaultAsync(w => w.Id == id, token);
        }

        public Task<List<CaseSuppression>> GetByCaseIdAsync(int caseId, CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseId == caseId).OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public Task<List<CaseSuppression>> GetByCaseKeyValueAsync(string key, string value,
            CancellationToken token = default)
        {
            return Visible().Where(w => w.CaseKey == key && w.CaseKeyValue == value).OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<CaseSuppression>> GetAwaitingAsync(int limit, DateTime pendingCutoff,
            CancellationToken token = default)
        {
            var pending = (int)CaseSuppressionState.Pending;
            var now = DateTime.UtcNow;

            return Visible().Where(w => w.StateId == pending && w.ExpiryDate > now
                                                              && (w.UpdatedDate ?? w.CreatedDate) > pendingCutoff)
                .OrderBy(o => o.CreatedDate).ThenBy(o => o.Id)
                .Take(limit).ToListAsync(token);
        }

        public Task<List<CaseSuppressionTarget>> GetTargetsAsync(IReadOnlyCollection<int> ids,
            CancellationToken token = default)
        {
            return dbContext.CaseSuppressionTarget.Where(w => ids.Contains(w.CaseSuppressionId)).OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public Task<List<CaseSuppressionTransaction>> GetTransactionsAsync(IReadOnlyCollection<int> ids,
            CancellationToken token = default)
        {
            return dbContext.CaseSuppressionTransaction.Where(w => ids.Contains(w.CaseSuppressionId))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public async Task<List<CaseSuppressionOrigin>> GetOriginsAsync(IReadOnlyCollection<int> overrideIds,
            CancellationToken token = default)
        {
            if (overrideIds.Count == 0)
            {
                return [];
            }

            var approved = (int)CaseSuppressionState.Approved;

            var rows = await (from t in dbContext.CaseSuppressionTarget
                join s in dbContext.CaseSuppression on t.CaseSuppressionId equals s.Id
                where t.AppliedOverrideId != null && overrideIds.Contains(t.AppliedOverrideId.Value)
                                                  && s.StateId == approved
                select new { OverrideId = t.AppliedOverrideId.Value, t.RuleName, s.CaseId })
                .ToListAsync(token).ConfigureAwait(false);

            return rows.Select(r => new CaseSuppressionOrigin(r.OverrideId, !string.IsNullOrEmpty(r.RuleName), r.CaseId))
                .ToList();
        }

        public Task<List<CaseSuppressionApproval>> GetApprovalsAsync(IReadOnlyCollection<int> ids,
            CancellationToken token = default)
        {
            return dbContext.CaseSuppressionApproval.Where(w => ids.Contains(w.CaseSuppressionId))
                .OrderBy(o => o.Id).ToListAsync(token);
        }

        public Task<List<CaseSuppressionVersion>> GetVersionsAsync(int id, CancellationToken token = default)
        {
            return dbContext.CaseSuppressionVersion.Where(w => w.CaseSuppressionId == id).OrderBy(o => o.Id)
                .ToListAsync(token);
        }

        public async Task<CaseSuppression> InsertAsync(CaseSuppression model,
            IReadOnlyCollection<CaseSuppressionTarget> targets,
            IReadOnlyCollection<CaseSuppressionTransaction> transactions, CancellationToken token = default)
        {
            await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            model.CreatedUser = userName;
            model.CreatedDate = now;
            model.StateId = (int)CaseSuppressionState.Pending;
            model.Version = 1;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            foreach (var target in targets)
            {
                target.CaseSuppressionId = model.Id;
                target.Id = await dbContext.InsertWithInt32IdentityAsync(target, token: token).ConfigureAwait(false);
            }

            foreach (var evidence in transactions)
            {
                evidence.CaseSuppressionId = model.Id;
                evidence.Id = await dbContext.InsertWithInt32IdentityAsync(evidence, token: token)
                    .ConfigureAwait(false);
            }

            await transaction.CommitAsync(token).ConfigureAwait(false);

            return model;
        }

        public Task LockAsync(int id, CancellationToken token = default)
        {
            return DataContextExtensions.ExecuteAsync(dbContext, "SELECT pg_advisory_xact_lock(hashtext(@key))",
                token, new DataParameter("key", "CaseSuppression:" + id));
        }

        public Task SnapshotAsync(CaseSuppression current, CancellationToken token = default)
        {
            return dbContext.InsertAsync(new CaseSuppressionVersion
            {
                CaseSuppressionId = current.Id,
                CaseId = current.CaseId,
                CaseKey = current.CaseKey,
                CaseKeyValue = current.CaseKeyValue,
                OverrideKey = current.OverrideKey,
                OverrideKeyValue = current.OverrideKeyValue,
                PathId = current.PathId,
                Reason = current.Reason,
                ExpiryDate = current.ExpiryDate,
                StateId = current.StateId,
                SourceId = current.SourceId,
                Version = current.Version,
                CountInCase = current.CountInCase,
                FirstSeenDate = current.FirstSeenDate,
                LastSeenDate = current.LastSeenDate,
                WiderCountStateId = current.WiderCountStateId,
                WiderTransactions = current.WiderTransactions,
                WiderKeys = current.WiderKeys,
                WiderCapped = current.WiderCapped,
                CreatedDate = current.CreatedDate,
                CreatedUser = current.CreatedUser,
                UpdatedDate = current.UpdatedDate,
                UpdatedUser = current.UpdatedUser,
                DecidedDate = current.DecidedDate,
                DecidedUser = current.DecidedUser
            }, token: token);
        }

        public async Task<CaseSuppression> UpdateStateAsync(CaseSuppression current, CaseSuppressionState state,
            int version, bool decided, DateTime? expiry = null, CancellationToken token = default)
        {
            await SnapshotAsync(current, token).ConfigureAwait(false);

            var now = DateTime.UtcNow;

            current.StateId = (int)state;
            current.Version = version;
            current.UpdatedDate = now;
            current.UpdatedUser = userName;

            if (expiry.HasValue)
            {
                current.ExpiryDate = expiry.Value;
            }

            if (decided)
            {
                current.DecidedDate = now;
                current.DecidedUser = userName;
            }

            await dbContext.UpdateAsync(current, token: token).ConfigureAwait(false);

            return current;
        }

        public async Task<CaseSuppressionApproval> AddApprovalAsync(int id, int version,
            CaseSuppressionDecision decision, string note, CancellationToken token = default)
        {
            var approval = new CaseSuppressionApproval
            {
                CaseSuppressionId = id,
                SuppressionVersion = version,
                DecisionId = (int)decision,
                Note = note,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = userName
            };

            approval.Id = await dbContext.InsertWithInt32IdentityAsync(approval, token: token).ConfigureAwait(false);

            return approval;
        }

        public Task MarkTargetAppliedAsync(int targetId, int overrideId, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.CaseSuppressionTarget.Where(w => w.Id == targetId)
                .Set(s => s.AppliedOverrideId, overrideId)
                .Set(s => s.AppliedDate, now)
                .UpdateAsync(token);
        }
    }
}
