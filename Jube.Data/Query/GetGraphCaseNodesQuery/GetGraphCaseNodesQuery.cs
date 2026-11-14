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

namespace Jube.Data.Query.GetGraphCaseNodesQuery
{
    public class GetGraphCaseNodesQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT c."Id" AS "CaseId", c."CaseKey", c."CaseKeyValue", COALESCE(c."CreatedDate", now()) AS "CreatedDate"
            FROM "Case" c
            JOIN "CaseWorkflow" w ON w."Guid" = c."CaseWorkflowGuid" AND COALESCE(w."Deleted", 0) = 0
            JOIN "EntityAnalysisModel" m ON m."Id" = w."EntityAnalysisModelId"
            WHERE m."TenantRegistryId" = @tenant AND w."EnableGraph" = 1 AND c."Id" > @after
              AND c."CaseKey" IS NOT NULL AND c."CaseKeyValue" IS NOT NULL
            ORDER BY c."Id"
            LIMIT @limit
            """;

        public Task<List<GetGraphCaseNodesQueryDto>> ExecuteAsync(int tenantRegistryId, long afterCaseId, int limit,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphCaseNodesQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("after", afterCaseId, DataType.Int64),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
