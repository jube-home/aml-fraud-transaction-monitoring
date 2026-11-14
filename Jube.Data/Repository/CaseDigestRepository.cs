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
    using LinqToDB.Async;
    using LinqToDB.Data;

    public class CaseDigestRepository(DbContext dbContext)
    {
        public Task<List<CaseDigestFigures>> ListWorkflowFiguresAsync(DateTime closedSince,
            CancellationToken token = default)
        {
            const string sql = """
                SELECT w."Guid" AS "WorkflowGuid", w."Name" AS "WorkflowName", m."TenantRegistryId",
                    count(*) FILTER (WHERE c."ClosedStatusId" = 0) AS "Open",
                    count(*) FILTER (WHERE c."ClosedStatusId" = 0 AND (c."Locked" IS NULL OR c."Locked" = 0)) AS "Unassigned",
                    count(*) FILTER (WHERE c."ClosedStatusId" = 0
                        AND EXISTS (SELECT 1 FROM "CaseServiceLevelBreach" b WHERE b."CaseId" = c."Id")) AS "Overdue",
                    count(*) FILTER (WHERE c."ClosedStatusId" = 3 AND c."ClosedDate" >= @since) AS "Closed"
                FROM "CaseWorkflow" w
                JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId"
                    AND m."TenantRegistryId" IS NOT NULL AND (m."Deleted" IS NULL OR m."Deleted" = 0)
                LEFT JOIN "Case" c ON c."CaseWorkflowGuid" = w."Guid"
                WHERE (w."Deleted" IS NULL OR w."Deleted" = 0)
                GROUP BY w."Guid", w."Name", m."TenantRegistryId"
                HAVING count(*) FILTER (WHERE c."ClosedStatusId" = 0) > 0
                    OR count(*) FILTER (WHERE c."ClosedStatusId" = 3 AND c."ClosedDate" >= @since) > 0
                """;

            return dbContext.QueryToListAsync<CaseDigestFigures>(sql, token, new DataParameter("since", closedSince));
        }

        public async Task<Dictionary<string, string>> ListEmailsAsync(IReadOnlyCollection<string> userNames,
            CancellationToken token = default)
        {
            var rows = await dbContext.UserRegistry
                .Where(w => userNames.Contains(w.Name) && (w.Deleted == 0 || w.Deleted == null) && w.Email != null
                            && w.Email != "")
                .Select(s => new { s.Name, s.Email })
                .ToListAsync(token).ConfigureAwait(false);

            return rows.GroupBy(r => r.Name).ToDictionary(g => g.Key, g => g.First().Email);
        }
    }

    public class CaseDigestFigures
    {
        public Guid WorkflowGuid { get; set; }
        public string WorkflowName { get; set; }
        public int TenantRegistryId { get; set; }
        public int Open { get; set; }
        public int Unassigned { get; set; }
        public int Overdue { get; set; }
        public int Closed { get; set; }
    }
}
