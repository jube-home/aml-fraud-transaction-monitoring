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

namespace Jube.Data.Query.GetCaseTriagePageQuery
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Dto.Options;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseTriagePageQuery(string connectionString, ILog log)
    {
        public async Task<GetCaseTriagePageQueryDto> ExecuteAsync(string whereSql, List<object> tokens,
            string extraCondition, string userName, CaseSearchSort sort, int take, int skip,
            CancellationToken token = default)
        {
            var parameters = new List<object>(tokens) { userName };

            var sql = CaseTriageSql.Page(whereSql, CaseTriageSql.Resolve(extraCondition, parameters.Count),
                parameters.Count, sort, take, skip);

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(sql, parameters, token).ConfigureAwait(false);

            var dto = new GetCaseTriagePageQueryDto();

            foreach (var r in rows)
            {
                dto.Total = RowReader.Long(r, "Total");

                dto.Rows.Add(new GetCaseTriagePageQueryDto.Row
                {
                    CaseId = RowReader.Int(r, "CaseId"),
                    CaseKey = RowReader.Str(r, "CaseKey"),
                    CaseKeyValue = RowReader.Str(r, "CaseKeyValue"),
                    CreatedDate = RowReader.Dt(r, "CreatedDate"),
                    Locked = RowReader.Int(r, "Locked") == 1,
                    LockedUser = RowReader.Str(r, "LockedUser"),
                    LockedDate = RowReader.DtN(r, "LockedDate"),
                    ClosedStatusId = RowReader.Int(r, "ClosedStatusId"),
                    ClosedDate = RowReader.DtN(r, "ClosedDate"),
                    Diary = RowReader.Int(r, "Diary") == 1,
                    DiaryDate = RowReader.DtN(r, "DiaryDate"),
                    DiaryTypeId = RowReader.Int(r, "DiaryTypeId"),
                    QaSample = RowReader.Bool(r, "QaSample"),
                    CaseWorkflowGuid = RowReader.Guid(r, "CaseWorkflowGuid"),
                    CaseWorkflowStatusGuid = RowReader.Guid(r, "CaseWorkflowStatusGuid"),
                    Rating = RowReader.Int(r, "Rating"),
                    StatusName = RowReader.Str(r, "StatusName"),
                    ForeColor = RowReader.Str(r, "ForeColor"),
                    BackColor = RowReader.Str(r, "BackColor"),
                    MaxDwellMinutes = RowReader.IntN(r, "MaxDwellMinutes"),
                    WorkflowName = RowReader.Str(r, "WorkflowName"),
                    FirstViewTargetMinutes = RowReader.IntN(r, "FirstViewTargetMinutes"),
                    OwnershipTargetMinutes = RowReader.IntN(r, "OwnershipTargetMinutes"),
                    ResolutionTargetMinutes = RowReader.IntN(r, "ResolutionTargetMinutes"),
                    RiskSequence = RowReader.IntN(r, "RiskSequence"),
                    LaneId = RowReader.IntN(r, "LaneId"),
                    Priority = RowReader.IntN(r, "Priority"),
                    LikelihoodProbability = RowReader.DblN(r, "LikelihoodProbability"),
                    LikelihoodBandId = RowReader.IntN(r, "LikelihoodBandId"),
                    ImpactId = RowReader.IntN(r, "ImpactId"),
                    ExpectedHarm = RowReader.DblN(r, "ExpectedHarm"),
                    NextDeadlineDate = RowReader.DtN(r, "NextDeadlineDate"),
                    DeadlineBreached = RowReader.Int(r, "DeadlineBreached") == 1,
                    LaneReason = RowReader.Str(r, "LaneReason"),
                    MethodId = RowReader.IntN(r, "MethodId"),
                    EvidenceCount = RowReader.Int(r, "EvidenceCount"),
                    RiskDate = RowReader.DtN(r, "RiskDate"),
                    TopDriver = RowReader.Str(r, "TopDriver"),
                    TopDriverFamily = RowReader.Str(r, "TopDriverFamily"),
                    Value = RowReader.DblN(r, "Value"),
                    Viewed = RowReader.Bool(r, "Viewed"),
                    LastViewedDate = RowReader.DtN(r, "LastViewedDate"),
                    Mine = RowReader.Bool(r, "Mine"),
                    AttReopened = RowReader.Bool(r, "AttReopened"),
                    AttEscalated = RowReader.Bool(r, "AttEscalated"),
                    AttMoved = RowReader.Bool(r, "AttMoved"),
                    AttDiaryDue = RowReader.Bool(r, "AttDiaryDue"),
                    PriorityRise = RowReader.Int(r, "PriorityRise"),
                    EventsSince = RowReader.Int(r, "EventsSince"),
                    Attention = RowReader.Bool(r, "Attention")
                });
            }

            return dto;
        }
    }
}
