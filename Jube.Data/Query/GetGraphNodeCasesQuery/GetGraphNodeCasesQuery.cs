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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphNodeCasesQuery
{
    public class GetGraphNodeCasesQuery(DbContext dbContext)
    {
        public const string Sql = """
            SELECT c."CaseKey", c."CaseKeyValue",
                   COUNT(*) FILTER (WHERE COALESCE(c."ClosedStatusId", 0) = 0)::integer AS "Open",
                   COUNT(*) FILTER (WHERE c."ClosedStatusId" = 3)::integer AS "Closed",
                   COUNT(*) FILTER (WHERE c."ClosedStatusId" = 3 AND s."OutcomeId" = 1)::integer AS "Confirmed",
                   MAX(c."Id") AS "LatestCaseId",
                   MAX(c."Id") FILTER (WHERE COALESCE(c."ClosedStatusId", 0) = 0) AS "LatestOpenCaseId"
            FROM "Case" c
            JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND COALESCE(w."Deleted", 0) = 0
            JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId" AND m."TenantRegistryId" = @tenant
            LEFT JOIN "CaseWorkflowStatus" s ON s."Guid" = c."CaseWorkflowStatusGuid" AND COALESCE(s."Deleted", 0) = 0
            JOIN unnest(@keys::text[], @values::text[]) AS k("Key", "Value")
                ON k."Key" = c."CaseKey" AND k."Value" = c."CaseKeyValue"
            WHERE COALESCE(w."Deleted", 0) = 0 AND COALESCE(m."Deleted", 0) = 0
              AND EXISTS (SELECT 1 FROM "CaseWorkflowRole" r
                          JOIN "RoleRegistry" rr ON rr."Guid" = r."RoleRegistryGuid" AND COALESCE(rr."Deleted", 0) = 0
                          JOIN "UserRegistry" u ON u."RoleRegistryGuid" = rr."Guid"
                          WHERE r."CaseWorkflowGuid" = w."Guid" AND COALESCE(r."Deleted", 0) = 0 AND u."Name" = @user)
              AND EXISTS (SELECT 1 FROM "CaseWorkflowStatusRole" r
                          JOIN "RoleRegistry" rr ON rr."Guid" = r."RoleRegistryGuid" AND COALESCE(rr."Deleted", 0) = 0
                          JOIN "UserRegistry" u ON u."RoleRegistryGuid" = rr."Guid"
                          WHERE r."CaseWorkflowStatusGuid" = c."CaseWorkflowStatusGuid" AND COALESCE(r."Deleted", 0) = 0
                            AND u."Name" = @user)
            GROUP BY c."CaseKey", c."CaseKeyValue"
            """;

        public Task<List<GetGraphNodeCasesQueryDto>> ExecuteAsync(int tenantRegistryId, string userName, string[] keys,
            string[] values, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphNodeCasesQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("user", userName, DataType.NVarChar),
                new DataParameter("keys", keys),
                new DataParameter("values", values));
        }
    }
}
