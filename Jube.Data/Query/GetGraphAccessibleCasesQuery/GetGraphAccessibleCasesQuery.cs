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
using LinqToDB.Async;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphAccessibleCasesQuery
{
    public class GetGraphAccessibleCasesQuery(DbContext dbContext)
    {
        public const string Sql = """
            SELECT c."Id"
            FROM "Case" c
            JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND COALESCE(w."Deleted", 0) = 0
            JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId" AND m."TenantRegistryId" = @tenant
            WHERE c."Id" = ANY(@ids::integer[])
              AND COALESCE(w."Deleted", 0) = 0 AND COALESCE(m."Deleted", 0) = 0
              AND EXISTS (SELECT 1 FROM "CaseWorkflowRole" r
                          JOIN "RoleRegistry" rr ON rr."Guid" = r."RoleRegistryGuid" AND COALESCE(rr."Deleted", 0) = 0
                          JOIN "UserRegistry" u ON u."RoleRegistryGuid" = rr."Guid"
                          WHERE r."CaseWorkflowGuid" = w."Guid" AND COALESCE(r."Deleted", 0) = 0 AND u."Name" = @user)
              AND EXISTS (SELECT 1 FROM "CaseWorkflowStatusRole" r
                          JOIN "RoleRegistry" rr ON rr."Guid" = r."RoleRegistryGuid" AND COALESCE(rr."Deleted", 0) = 0
                          JOIN "UserRegistry" u ON u."RoleRegistryGuid" = rr."Guid"
                          WHERE r."CaseWorkflowStatusGuid" = c."CaseWorkflowStatusGuid" AND COALESCE(r."Deleted", 0) = 0
                            AND u."Name" = @user)
            """;

        public Task<List<int>> ExecuteAsync(int tenantRegistryId, string userName, int[] caseIds,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<int>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("user", userName, DataType.NVarChar),
                new DataParameter("ids", caseIds));
        }
    }
}
