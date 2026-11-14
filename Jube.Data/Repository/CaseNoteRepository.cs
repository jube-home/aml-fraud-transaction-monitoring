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
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Npgsql;
    using Poco;

    public class CaseNoteRepository
    {
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public CaseNoteRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public CaseNoteRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public CaseNoteRepository(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<IEnumerable<CaseNote>> GetByCaseKeyValueAsync(string key, string value,
            CancellationToken token = default)
        {
            return await dbContext.CaseNote.Where(w
                    => (tenantRegistryId.HasValue
                        && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                       && (w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                           w.Case.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                       && w.CaseKey == key && w.CaseKeyValue == value
                       && (userName == null
                           || ((w.ActionId == null
                                || dbContext.CaseWorkflowActionRole
                                    .Where(r => dbContext.CaseWorkflowAction
                                                    .Any(a => a.Id == w.ActionId && a.Guid == r.CaseWorkflowActionGuid)
                                                && (r.Deleted == 0 || r.Deleted == null))
                                    .Any(r => dbContext.RoleRegistry
                                        .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                     && (rr.Deleted == 0 || rr.Deleted == null))
                                        .Any(rr => dbContext.UserRegistry
                                            .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName))))
                               && dbContext.CaseWorkflowRole
                                   .Where(r => r.CaseWorkflowGuid == w.Case.CaseWorkflow.Guid
                                               && (r.Deleted == 0 || r.Deleted == null))
                                   .Any(r => dbContext.RoleRegistry
                                       .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                    && (rr.Deleted == 0 || rr.Deleted == null))
                                       .Any(rr => dbContext.UserRegistry
                                           .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))
                               && dbContext.CaseWorkflowStatusRole
                                   .Where(r => r.CaseWorkflowStatusGuid == w.Case.CaseWorkflowStatus.Guid
                                               && (r.Deleted == 0 || r.Deleted == null))
                                   .Any(r => dbContext.RoleRegistry
                                       .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                    && (rr.Deleted == 0 || rr.Deleted == null))
                                       .Any(rr => dbContext.UserRegistry
                                           .Any(u => u.RoleRegistryGuid == rr.Guid && u.Name == userName)))))
                )
                .OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public Task<bool> ActionGrantedToUserAsync(int noteId, CancellationToken token = default)
        {
            return dbContext.CaseNote.AnyAsync(w => w.Id == noteId
                                                    && (w.ActionId == null
                                                        || dbContext.CaseWorkflowActionRole
                                                            .Where(r => dbContext.CaseWorkflowAction
                                                                            .Any(a => a.Id == w.ActionId
                                                                                      && a.Guid == r.CaseWorkflowActionGuid)
                                                                        && (r.Deleted == 0 || r.Deleted == null))
                                                            .Any(r => dbContext.RoleRegistry
                                                                .Where(rr => rr.Guid == r.RoleRegistryGuid
                                                                             && (rr.Deleted == 0 || rr.Deleted == null))
                                                                .Any(rr => dbContext.UserRegistry
                                                                    .Any(u => u.RoleRegistryGuid == rr.Guid
                                                                              && u.Name == userName)))), token);
        }

        public Task<CaseNote> GetByIdAsync(int id, CancellationToken token = default)
        {
            return dbContext.CaseNote.FirstOrDefaultAsync(w => w.Id == id
                                                               && tenantRegistryId.HasValue
                                                               && w.Case.CaseWorkflow.EntityAnalysisModel
                                                                   .TenantRegistryId == tenantRegistryId, token);
        }

        public Task<int> SetFindingAsync(int id, bool isFinding, CancellationToken token = default)
        {
            var user = (int)CaseNoteSource.User;

            return dbContext.CaseNote
                .Where(w => w.Id == id && w.CreatedUser == userName && w.SourceId == user
                            && tenantRegistryId.HasValue
                            && w.Case.CaseWorkflow.EntityAnalysisModel.TenantRegistryId == tenantRegistryId)
                .Set(s => s.IsFinding, isFinding)
                .UpdateAsync(token);
        }

        public Task<CaseNote> GetLatestNarrativeAsync(int caseId, CancellationToken token = default)
        {
            var narrative = (int)CaseNoteKind.Narrative;

            return dbContext.CaseNote.Where(w => w.CaseId == caseId && w.KindId == narrative)
                .OrderByDescending(o => o.Version).FirstOrDefaultAsync(token);
        }

        public async Task<CaseNote> InsertNarrativeAsync(CaseNote model, CancellationToken token = default)
        {
            var narrative = (int)CaseNoteKind.Narrative;
            var caseId = model.CaseId.GetValueOrDefault();

            await using var transaction = await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            await dbContext.ExecuteAsync("select pg_advisory_xact_lock(hashtext(@key))", token,
                new DataParameter("key", $"CaseNoteNarrative|{caseId}")).ConfigureAwait(false);

            var latest = await dbContext.CaseNote.Where(w => w.CaseId == caseId && w.KindId == narrative)
                .OrderByDescending(o => o.Version).FirstOrDefaultAsync(token).ConfigureAwait(false);

            if (latest is not null && latest.FactsHash == model.FactsHash)
            {
                await transaction.RollbackAsync(token).ConfigureAwait(false);

                return null;
            }

            model.Version = (latest?.Version ?? 0) + 1;
            model.SupersedesNoteId = latest?.Id;
            model.KindId = narrative;
            model.SourceId = (int)CaseNoteSource.AskJooby;
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;

            try
            {
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);
                await transaction.CommitAsync(token).ConfigureAwait(false);
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(token).ConfigureAwait(false);

                return null;
            }

            return model;
        }

        public async Task<CaseNote> InsertAsync(CaseNote model, CancellationToken token = default)
        {
            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }
    }
}