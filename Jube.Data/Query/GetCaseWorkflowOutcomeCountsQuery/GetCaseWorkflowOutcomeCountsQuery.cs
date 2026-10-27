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

namespace Jube.Data.Query.GetCaseWorkflowOutcomeCountsQuery
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;

    public class GetCaseWorkflowOutcomeCountsQuery(DbContext dbContext)
    {
        public async Task<GetCaseWorkflowOutcomeCountsQueryDto> ExecuteAsync(Guid caseWorkflowGuid,
            CancellationToken token = default)
        {
            var outcomes = from c in dbContext.Case
                join s in dbContext.CaseWorkflowStatus on c.CaseWorkflowStatusGuid equals s.Guid
                where c.CaseWorkflowGuid == caseWorkflowGuid
                      && c.ClosedStatusId == 3
                      && (s.OutcomeId == 1 || s.OutcomeId == 2)
                select new { c.EntityAnalysisModelActivationRuleGuid, Confirmed = s.OutcomeId == 1 ? 1 : 0 };

            var workflow = await outcomes.GroupBy(g => 1).Select(g => new
            {
                Closed = g.Count(),
                Confirmed = g.Sum(x => x.Confirmed)
            }).FirstOrDefaultAsync(token);

            var rules = await outcomes.Where(w => w.EntityAnalysisModelActivationRuleGuid != null)
                .GroupBy(g => g.EntityAnalysisModelActivationRuleGuid)
                .Select(g => new GetCaseWorkflowOutcomeCountsQueryDto.RuleOutcome
                {
                    RuleGuid = g.Key.Value,
                    Closed = g.Count(),
                    Confirmed = g.Sum(x => x.Confirmed)
                }).ToListAsync(token);

            return new GetCaseWorkflowOutcomeCountsQueryDto
            {
                Closed = workflow?.Closed ?? 0,
                Confirmed = workflow?.Confirmed ?? 0,
                Rules = rules
            };
        }
    }
}
