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
    using Npgsql;
    using Poco;

    public class CaseLinkRepository(DbContext dbContext)
    {
        public Task<List<CaseLink>> ListForCaseAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseLink
                .Where(w => w.CaseIdLow == caseId || w.CaseIdHigh == caseId)
                .OrderByDescending(o => o.Id)
                .ToListAsync(token);
        }

        public async Task<bool> InsertAsync(int caseId, int otherCaseId, string reason, string user,
            CancellationToken token = default)
        {
            try
            {
                await dbContext.InsertAsync(new CaseLink
                {
                    CaseIdLow = Math.Min(caseId, otherCaseId),
                    CaseIdHigh = Math.Max(caseId, otherCaseId),
                    Reason = reason,
                    CreatedUser = user,
                    CreatedDate = DateTime.UtcNow
                }, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        public Task<int> DeleteAsync(int caseId, int otherCaseId, CancellationToken token = default)
        {
            var low = Math.Min(caseId, otherCaseId);
            var high = Math.Max(caseId, otherCaseId);

            return dbContext.CaseLink.Where(w => w.CaseIdLow == low && w.CaseIdHigh == high).DeleteAsync(token);
        }
    }
}
