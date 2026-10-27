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

namespace Jube.Data.Query.GetCaseRiskCandidatesQuery
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetCaseRiskCandidatesQuery(DbContext dbContext)
    {
        public Task<List<GetCaseRiskCandidatesQueryDto>> ExecuteAsync(DateTime checkedBefore, int take,
            CancellationToken token = default)
        {
            return (from c in dbContext.Case
                where (c.ClosedStatusId == 0 || c.ClosedStatusId == 1)
                      && (c.RiskCheckedDate == null || c.RiskCheckedDate < checkedBefore)
                      && (c.CaseWorkflow.Deleted == 0 || c.CaseWorkflow.Deleted == null)
                      && (c.CaseWorkflow.EntityAnalysisModel.Deleted == 0
                          || c.CaseWorkflow.EntityAnalysisModel.Deleted == null)
                orderby c.RiskCheckedDate == null ? 0 : 1, c.RiskCheckedDate
                select new GetCaseRiskCandidatesQueryDto
                {
                    CaseId = c.Id,
                    CaseWorkflowGuid = c.CaseWorkflowGuid,
                    CaseKey = c.CaseKey,
                    CaseKeyValue = c.CaseKeyValue,
                    EntityAnalysisModelActivationRuleGuid = c.EntityAnalysisModelActivationRuleGuid,
                    Diary = c.Diary,
                    DiaryDate = c.DiaryDate,
                    Json = c.Json,
                    RiskCheckedDate = c.RiskCheckedDate,
                    DiaryTypeId = c.DiaryTypeId,
                    RiskRecheckTriggerId = c.RiskRecheckTriggerId,
                    QaSampleStatusId = c.QaSampleStatusId,
                    HasAssessment = dbContext.CaseRiskAssessment.Any(a => a.CaseId == c.Id)
                }).Take(take).ToListAsync(token);
        }
    }
}
