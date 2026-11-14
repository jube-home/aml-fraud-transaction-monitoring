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
    using LinqToDB;
    using LinqToDB.Async;
    using Poco;

    public class CaseAnalystAssessmentRepository(DbContext dbContext)
    {
        public Task<List<CaseAnalystAssessment>> ListForCaseAsync(int caseId, int tenantRegistryId, int take,
            CancellationToken token = default)
        {
            return dbContext.CaseAnalystAssessment
                .Where(w => w.CaseId == caseId && w.TenantRegistryId == tenantRegistryId)
                .OrderByDescending(o => o.Id).Take(take).ToListAsync(token);
        }

        public async Task<short?> GetStandingLikelihoodAsync(int caseId, int tenantRegistryId,
            CancellationToken token = default)
        {
            return await dbContext.CaseAnalystAssessment
                .Where(w => w.CaseId == caseId && w.TenantRegistryId == tenantRegistryId && w.WithdrawnDate == null)
                .OrderByDescending(o => o.Id).Select(s => (short?)s.LikelihoodId).FirstOrDefaultAsync(token)
                .ConfigureAwait(false);
        }

        public Task InsertAsync(CaseAnalystAssessment model, CancellationToken token = default)
        {
            return dbContext.InsertAsync(model, token: token);
        }

        public Task<int> WithdrawAsync(Guid guid, int tenantRegistryId, string userName, DateTime when,
            CancellationToken token = default)
        {
            return dbContext.CaseAnalystAssessment
                .Where(w => w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.CreatedUser == userName
                            && w.WithdrawnDate == null)
                .Set(s => s.WithdrawnUser, userName)
                .Set(s => s.WithdrawnDate, when)
                .UpdateAsync(token);
        }
    }
}
