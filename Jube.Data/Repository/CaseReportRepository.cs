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
    using Npgsql;
    using Poco;

    public class CaseReportRepository(DbContext dbContext)
    {
        public Task<CaseReport> GetAsync(int tenantRegistryId, Guid guid, CancellationToken token = default)
        {
            return dbContext.CaseReport.FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                  && w.Guid == guid, token);
        }

        public Task<List<CaseReport>> ListForCaseAsync(int tenantRegistryId, int caseId,
            CancellationToken token = default)
        {
            return dbContext.CaseReport.Where(w => w.TenantRegistryId == tenantRegistryId && w.CaseId == caseId)
                .OrderByDescending(o => o.Id).ToListAsync(token);
        }

        public Task<List<CaseReport>> ListAwaitingApprovalAsync(int tenantRegistryId, int take,
            CancellationToken token = default)
        {
            var pending = (short)CaseReportState.PendingApproval;

            return dbContext.CaseReport.Where(w => w.TenantRegistryId == tenantRegistryId && w.StateId == pending)
                .OrderBy(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<int> CountOpenForCaseAsync(int tenantRegistryId, int caseId, CancellationToken token = default)
        {
            var pending = (short)CaseReportState.PendingApproval;
            var approved = (short)CaseReportState.Approved;

            return dbContext.CaseReport.CountAsync(w => w.TenantRegistryId == tenantRegistryId && w.CaseId == caseId
                                                        && (w.StateId == pending || w.StateId == approved), token);
        }

        public async Task InsertAsync(CaseReport model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;
            model.Id = await dbContext.InsertWithInt64IdentityAsync(model, token: token).ConfigureAwait(false);
        }

        public Task<int> MoveAsync(long id, short from, short to, string user, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.CaseReport.Where(w => w.Id == id && w.StateId == from)
                .Set(s => s.StateId, to).Set(s => s.UpdatedUser, user).Set(s => s.UpdatedDate, now)
                .UpdateAsync(token);
        }

        public Task<int> SetVersionAsync(long id, short expectedState, short state, int version, string user,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.CaseReport.Where(w => w.Id == id && w.StateId == expectedState)
                .Set(s => s.StateId, state).Set(s => s.CurrentVersion, version).Set(s => s.UpdatedUser, user)
                .Set(s => s.UpdatedDate, now).UpdateAsync(token);
        }

        public Task<int> SubmitAsync(long id, string reference, string user, CancellationToken token = default)
        {
            var approved = (short)CaseReportState.Approved;
            var submitted = (short)CaseReportState.Submitted;
            var now = DateTime.UtcNow;

            return dbContext.CaseReport.Where(w => w.Id == id && w.StateId == approved)
                .Set(s => s.StateId, submitted).Set(s => s.FiuReference, reference).Set(s => s.SubmittedUser, user)
                .Set(s => s.SubmittedDate, now).Set(s => s.UpdatedUser, user).Set(s => s.UpdatedDate, now)
                .UpdateAsync(token);
        }
    }
}
