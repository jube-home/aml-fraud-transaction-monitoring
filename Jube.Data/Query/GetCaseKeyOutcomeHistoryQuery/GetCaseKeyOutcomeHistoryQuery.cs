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

namespace Jube.Data.Query.GetCaseKeyOutcomeHistoryQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetCaseKeyOutcomeHistoryQuery(DbContext dbContext)
    {
        public async Task<GetCaseKeyOutcomeHistoryQueryDto> ExecuteAsync(Guid caseWorkflowGuid, string caseKey,
            string caseKeyValue, int excludeCaseId, CancellationToken token = default)
        {
            var history = await (from c in dbContext.Case
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where c.CaseWorkflowGuid == caseWorkflowGuid
                      && c.CaseKey == caseKey
                      && c.CaseKeyValue == caseKeyValue
                      && c.Id != excludeCaseId
                      && c.ClosedStatusId == 3
                      && (s.OutcomeId == 1 || s.OutcomeId == 2)
                group new { s.OutcomeId, c.ClosedDate } by 1
                into g
                select new GetCaseKeyOutcomeHistoryQueryDto
                {
                    Closed = g.Count(),
                    Confirmed = g.Sum(x => x.OutcomeId == 1 ? 1 : 0),
                    NewestClosedDate = g.Max(x => x.ClosedDate)
                }).FirstOrDefaultAsync(token);

            return history ?? new GetCaseKeyOutcomeHistoryQueryDto();
        }
    }
}
