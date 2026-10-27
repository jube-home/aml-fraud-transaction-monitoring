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

namespace Jube.Data.Query.GetCaseAnalyticsFactsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseAnalyticsFactsQuery(string connectionString, ILog log)
    {
        public const int MaximumRows = 50000;

        public async Task<GetCaseAnalyticsFactsQueryDto> ExecuteAsync(string whereSql, List<object> tokens,
            DateTime from, DateTime to, CancellationToken token = default)
        {
            var parameters = new List<object>(tokens) { from, to };
            var fromParameter = "@" + (parameters.Count - 1);
            var toParameter = "@" + parameters.Count;

            const string asOf = "coalesce(fv.\"FirstViewedDate\", c.\"ClosedDate\", (now() at time zone 'utc'))";

            var sql = "with \"Matched\" as (select distinct \"Case\".\"Id\" as \"CaseId\" " + whereSql + ") " +
                      "select c.\"Id\" as \"CaseId\", c.\"CaseWorkflowStatusGuid\", c.\"CreatedDate\", c.\"ClosedDate\", " +
                      "(c.\"ClosedStatusId\" = 3) as \"Closed\", coalesce(s.\"OutcomeId\", 0) as \"OutcomeId\", " +
                      "c.\"EntityAnalysisModelActivationRuleGuid\" as \"RuleGuid\", r.\"Name\" as \"RuleName\", " +
                      "fv.\"FirstViewedDate\", ow.\"FirstOwnedDate\", " +
                      "(select count(*) from \"CaseStatusTransition\" x where x.\"CaseId\" = c.\"Id\" " +
                      "and x.\"EnterReasonId\" = 3) as \"Reopens\", " +
                      "(select count(*) from \"CaseEvent\" e where e.\"CaseId\" = c.\"Id\" " +
                      "and e.\"CaseEventTypeId\" in (6, 14)) as \"Handoffs\", " +
                      "a.\"LaneId\", a.\"Priority\", a.\"LikelihoodProbability\" as \"Likelihood\", a.\"ImpactId\", " +
                      "w.\"FirstViewTargetMinutes\", w.\"OwnershipTargetMinutes\", w.\"ResolutionTargetMinutes\", " +
                      "t.\"EnteredDate\" as \"StayEnteredDate\", s.\"MaxDwellMinutes\", c.\"ClosedUser\", " +
                      "c.\"LockedUser\", (c.\"QaSampleStatusId\" = 2) as \"QaSample\" from \"Matched\" m join \"Case\" c on c.\"Id\" = m.\"CaseId\" " +
                      "join \"CaseWorkflowStatus\" s on s.\"Guid\" = c.\"CaseWorkflowStatusGuid\" " +
                      "join \"CaseWorkflow\" w on w.\"Guid\" = c.\"CaseWorkflowGuid\" " +
                      "left join \"EntityAnalysisModelActivationRule\" r " +
                      "on r.\"Guid\" = c.\"EntityAnalysisModelActivationRuleGuid\" " +
                      "left join lateral (select min(e.\"CreatedDate\") as \"FirstViewedDate\" from \"CaseEvent\" e " +
                      "where e.\"CaseId\" = c.\"Id\" and e.\"CaseEventTypeId\" in (2, 4, 16)) fv on true " +
                      "left join lateral (select min(e.\"CreatedDate\") as \"FirstOwnedDate\" from \"CaseEvent\" e " +
                      "where e.\"CaseId\" = c.\"Id\" and e.\"CaseEventTypeId\" in (2, 3, 6, 14)) ow on true " +
                      "left join \"CaseStatusTransition\" t on t.\"CaseId\" = c.\"Id\" and t.\"ExitedDate\" is null " +
                      "left join lateral (select y.* from \"CaseRiskAssessment\" y where y.\"CaseId\" = c.\"Id\" " +
                      "order by case when y.\"CreatedDate\" <= " + asOf + " then 0 else 1 end, " +
                      "case when y.\"CreatedDate\" <= " + asOf + " then -y.\"Sequence\" else y.\"Sequence\" end " +
                      "limit 1) a on true " +
                      "where (c.\"CreatedDate\" >= " + fromParameter + " and c.\"CreatedDate\" < " + toParameter + ") " +
                      "or (c.\"ClosedDate\" >= " + fromParameter + " and c.\"ClosedDate\" < " + toParameter + ") " +
                      "or c.\"ClosedStatusId\" in (0, 1) " +
                      "order by c.\"CreatedDate\" desc limit " + (MaximumRows + 1);

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(sql, parameters, token).ConfigureAwait(false);

            var dto = new GetCaseAnalyticsFactsQueryDto { Truncated = rows.Count > MaximumRows };

            foreach (var r in rows)
            {
                if (dto.Rows.Count >= MaximumRows)
                {
                    break;
                }

                dto.Rows.Add(new GetCaseAnalyticsFactsQueryDto.Row
                {
                    CaseId = RowReader.Int(r, "CaseId"),
                    CaseWorkflowStatusGuid = RowReader.Guid(r, "CaseWorkflowStatusGuid"),
                    CreatedDate = RowReader.Dt(r, "CreatedDate"),
                    ClosedDate = RowReader.DtN(r, "ClosedDate"),
                    Closed = RowReader.Bool(r, "Closed"),
                    OutcomeId = RowReader.Int(r, "OutcomeId"),
                    RuleGuid = r.TryGetValue("RuleGuid", out var rule) && rule is Guid g ? g : null,
                    RuleName = RowReader.Str(r, "RuleName"),
                    FirstViewedDate = RowReader.DtN(r, "FirstViewedDate"),
                    FirstOwnedDate = RowReader.DtN(r, "FirstOwnedDate"),
                    Reopens = (int)RowReader.Long(r, "Reopens"),
                    Handoffs = (int)RowReader.Long(r, "Handoffs"),
                    LaneId = RowReader.IntN(r, "LaneId"),
                    Priority = RowReader.IntN(r, "Priority"),
                    Likelihood = RowReader.DblN(r, "Likelihood"),
                    ImpactId = RowReader.IntN(r, "ImpactId"),
                    FirstViewTargetMinutes = RowReader.IntN(r, "FirstViewTargetMinutes"),
                    OwnershipTargetMinutes = RowReader.IntN(r, "OwnershipTargetMinutes"),
                    ResolutionTargetMinutes = RowReader.IntN(r, "ResolutionTargetMinutes"),
                    StayEnteredDate = RowReader.DtN(r, "StayEnteredDate"),
                    MaxDwellMinutes = RowReader.IntN(r, "MaxDwellMinutes"),
                    ClosedUser = RowReader.Str(r, "ClosedUser"),
                    LockedUser = RowReader.Str(r, "LockedUser"),
                    QaSample = RowReader.Bool(r, "QaSample")
                });
            }

            return dto;
        }
    }
}
