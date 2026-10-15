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

namespace Jube.Data.Query.GetExistingCasePriorityQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetExistingCasePriorityQuery(DbContext dbContext)
    {
        public Task<GetExistingCasePriorityQueryDto> ExecuteAsync(Guid casesWorkflowGuid, string caseKey,
            string caseKeyValue, CancellationToken token = default)
        {
            return (from c in dbContext.Case
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid
                    equals s.Guid
                where c.CaseKey == caseKey
                      && (c.CaseWorkflow.EntityAnalysisModel.Deleted == 0 ||
                          c.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                      && c.CaseKeyValue == caseKeyValue
                      && c.CaseWorkflowGuid == casesWorkflowGuid
                      && (c.ClosedStatusId == 0 || c.ClosedStatusId == 1 || c.ClosedStatusId == 2 ||
                          c.ClosedStatusId == 4)
                      && (s.Deleted == 0 || s.Deleted == null)
                select new GetExistingCasePriorityQueryDto
                {
                    Priority = s.Priority,
                    CaseId = c.Id
                }).FirstOrDefaultAsync(token);
        }
    }
}