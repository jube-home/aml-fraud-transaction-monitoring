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

namespace Jube.Data.Query.GetCaseTriageEnrichmentQuery
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Models;
    using Reporting;

    public class GetCaseTriageEnrichmentQuery(string connectionString, ILog log)
    {
        public const string ViewEventTypes = "2, 4, 16";
        public const string OwnershipEventTypes = "2, 3, 6, 14";

        private static string Event(string select, string types, string order = "e.\"CreatedDate\", e.\"Id\"") =>
            "(select " + select + " from \"CaseEvent\" e where e.\"CaseId\" = c.\"Id\" and e.\"CaseEventTypeId\" in (" +
            types + ") order by " + order + " limit 1)";

        public async Task<Dictionary<int, GetCaseTriageEnrichmentQueryDto>> ExecuteAsync(
            IReadOnlyCollection<int> caseIds, CancellationToken token = default)
        {
            if (caseIds.Count == 0)
            {
                return new Dictionary<int, GetCaseTriageEnrichmentQueryDto>();
            }

            var sql = "select c.\"Id\" as \"CaseId\", " +
                      Event("e.\"CreatedDate\"", ViewEventTypes) + " as \"FirstViewedDate\", " +
                      Event("e.\"CreatedUser\"", ViewEventTypes) + " as \"FirstViewedBy\", " +
                      Event("e.\"CreatedDate\"", OwnershipEventTypes) + " as \"FirstOwnedDate\", " +
                      Event("e.\"CreatedUser\"", OwnershipEventTypes) + " as \"FirstOwnedBy\", " +
                      "(select t.\"EnteredDate\" from \"CaseStatusTransition\" t where t.\"CaseId\" = c.\"Id\" " +
                      "and t.\"ExitedDate\" is null) as \"StayEnteredDate\", " +
                      "(select e.\"CaseEventTypeId\" from \"CaseEvent\" e where e.\"CaseId\" = c.\"Id\" " +
                      "order by e.\"Id\" desc limit 1) as \"LastEventTypeId\", " +
                      "(select e.\"CreatedUser\" from \"CaseEvent\" e where e.\"CaseId\" = c.\"Id\" " +
                      "order by e.\"Id\" desc limit 1) as \"LastEventUser\", " +
                      "(select e.\"CreatedDate\" from \"CaseEvent\" e where e.\"CaseId\" = c.\"Id\" " +
                      "order by e.\"Id\" desc limit 1) as \"LastEventDate\" " +
                      "from \"Case\" c where c.\"Id\" = any(@1)";

            using var postgres = new Postgres(connectionString, log);

            var rows = await postgres.ExecuteByOrderedParametersAsync(sql, [caseIds.ToArray()], token)
                .ConfigureAwait(false);

            return rows.Select(r => new GetCaseTriageEnrichmentQueryDto
            {
                CaseId = RowReader.Int(r, "CaseId"),
                FirstViewedDate = RowReader.DtN(r, "FirstViewedDate"),
                FirstViewedBy = RowReader.Str(r, "FirstViewedBy"),
                FirstOwnedDate = RowReader.DtN(r, "FirstOwnedDate"),
                FirstOwnedBy = RowReader.Str(r, "FirstOwnedBy"),
                StayEnteredDate = RowReader.DtN(r, "StayEnteredDate"),
                LastEventTypeId = RowReader.IntN(r, "LastEventTypeId"),
                LastEventUser = RowReader.Str(r, "LastEventUser"),
                LastEventDate = RowReader.DtN(r, "LastEventDate")
            }).ToDictionary(k => k.CaseId);
        }
    }
}
