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

namespace Jube.Data.Query.GetCaseChecklistSlaFactsQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseChecklistSlaFactsQuery(string connectionString, ILog log)
    {
        public const int MaximumRows = 90000;

        public async Task<GetCaseChecklistSlaFactsQueryDto> ExecuteAsync(string whereSql, List<object> tokens,
            DateTime from, DateTime to, CancellationToken token = default)
        {
            var parameters = new List<object>(tokens) { from, to };
            var fromParameter = "@" + (parameters.Count - 1);
            var toParameter = "@" + parameters.Count;

            var sql = "with \"Matched\" as (select distinct \"Case\".\"Id\" as \"CaseId\" " + whereSql + ") " +
                      "select k.\"CaseWorkflowChecklistItemGuid\", k.\"CaseWorkflowChecklistGuid\", " +
                      "k.\"ChecklistName\", k.\"ItemName\", k.\"StateId\", k.\"FirstShownDate\", k.\"DueDate\", " +
                      "k.\"CompletedDate\", k.\"SlaBreachedDate\" " +
                      "from \"Matched\" m join \"CaseChecklistItem\" k on k.\"CaseId\" = m.\"CaseId\" " +
                      "where k.\"FirstShownDate\" >= " + fromParameter + " and k.\"FirstShownDate\" < " +
                      toParameter + " order by k.\"FirstShownDate\" desc limit " + (MaximumRows + 1);

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(sql, parameters, token).ConfigureAwait(false);

            var dto = new GetCaseChecklistSlaFactsQueryDto { Truncated = rows.Count > MaximumRows };

            foreach (var r in rows)
            {
                if (dto.Rows.Count >= MaximumRows)
                {
                    break;
                }

                dto.Rows.Add(new GetCaseChecklistSlaFactsQueryDto.Row
                {
                    ItemGuid = RowReader.Guid(r, "CaseWorkflowChecklistItemGuid"),
                    ChecklistGuid = RowReader.Guid(r, "CaseWorkflowChecklistGuid"),
                    ChecklistName = RowReader.Str(r, "ChecklistName"),
                    ItemName = RowReader.Str(r, "ItemName"),
                    StateId = RowReader.Int(r, "StateId"),
                    FirstShownDate = RowReader.Dt(r, "FirstShownDate"),
                    DueDate = RowReader.DtN(r, "DueDate"),
                    CompletedDate = RowReader.DtN(r, "CompletedDate"),
                    SlaBreachedDate = RowReader.DtN(r, "SlaBreachedDate")
                });
            }

            return dto;
        }
    }
}
