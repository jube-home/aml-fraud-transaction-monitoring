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

using System;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetCaseNetworkBucketOutcomesQuery
{
    public class GetCaseNetworkBucketOutcomesQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT COUNT(*) FILTER (WHERE s."OutcomeId" = 1)::integer AS "Confirmed", COUNT(*)::integer AS "Closed"
            FROM "Case" c
            JOIN "CaseWorkflowStatus" s ON s."Guid" = c."CaseWorkflowStatusGuid" AND COALESCE(s."Deleted", 0) = 0
            JOIN "GraphNode" n ON n."TenantRegistryId" = @tenant AND n."Key" = c."CaseKey" AND n."Value" = c."CaseKeyValue"
            WHERE c."CaseWorkflowGuid" = @workflow AND c."ClosedStatusId" = 3 AND c."Id" <> @case
              AND (s."OutcomeId" = 1 OR s."OutcomeId" = 2)
              AND n."DistanceToConfirmed" = @distance
            """;

        public async Task<GetCaseNetworkBucketOutcomesQueryDto> ExecuteAsync(int tenantRegistryId, Guid caseWorkflowGuid,
            int caseId, int distance, CancellationToken token = default)
        {
            var rows = await dbContext.QueryToListAsync<GetCaseNetworkBucketOutcomesQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("workflow", caseWorkflowGuid, DataType.Guid),
                new DataParameter("case", caseId, DataType.Int32),
                new DataParameter("distance", distance, DataType.Int32)).ConfigureAwait(false);

            return rows.Count == 0 ? new GetCaseNetworkBucketOutcomesQueryDto() : rows[0];
        }
    }
}
