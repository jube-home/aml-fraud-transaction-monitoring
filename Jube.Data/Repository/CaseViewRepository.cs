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
    using Concurrency;
    using Context;
    using LinqToDB;
    using Poco;

    public class CaseViewRepository(DbContext dbContext, string userName)
    {
        public Task<CaseView> GetAsync(int caseId, CancellationToken token = default)
        {
            return dbContext.CaseView.FirstOrDefaultAsync(w => w.CaseId == caseId && w.User == userName, token);
        }

        public async Task<Dictionary<int, CaseView>> GetForCasesAsync(IReadOnlyCollection<int> caseIds,
            CancellationToken token = default)
        {
            var ids = caseIds.ToList();

            var rows = await dbContext.CaseView.Where(w => ids.Contains(w.CaseId) && w.User == userName)
                .ToListAsync(token);

            return rows.ToDictionary(k => k.CaseId);
        }

        public async Task UpsertAsync(int caseId, int? lastViewedCaseEventId, int? lastViewedRiskSequence,
            CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            async Task<int> UpdateAsync()
            {
                return await dbContext.CaseView.Where(w => w.CaseId == caseId && w.User == userName)
                    .Set(s => s.LastViewedDate, now)
                    .Set(s => s.LastViewedCaseEventId, lastViewedCaseEventId)
                    .Set(s => s.LastViewedRiskSequence, lastViewedRiskSequence)
                    .UpdateAsync(token);
            }

            if (await UpdateAsync() > 0)
            {
                return;
            }

            try
            {
                await dbContext.InsertAsync(new CaseView
                {
                    CaseId = caseId,
                    User = userName,
                    LastViewedDate = now,
                    LastViewedCaseEventId = lastViewedCaseEventId,
                    LastViewedRiskSequence = lastViewedRiskSequence
                }, token: token);
            }
            catch (Exception ex) when (UniqueViolationCheck.Is(ex))
            {
                await UpdateAsync();
            }
        }
    }
}
