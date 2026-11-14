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

namespace Jube.Data.Query.GetGraphTagSeedsQuery
{
    public class GetGraphTagSeedsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT DISTINCT x."Tag", x."NodeId" FROM (
                SELECT t."Name" AS "Tag", e."FromNodeId" AS "NodeId"
                FROM "ArchiveTag" t
                JOIN "GraphEdgeEvent" ev ON ev."EntityAnalysisModelInstanceEntryGuid" = t."EntityAnalysisModelInstanceEntryGuid"
                    AND ev."TenantRegistryId" = @tenant AND ev."Retracted" = 0
                JOIN "GraphEdge" e ON e."Id" = ev."GraphEdgeId"
                WHERE COALESCE(t."Deleted", 0) = 0
                UNION ALL
                SELECT t."Name", e."ToNodeId"
                FROM "ArchiveTag" t
                JOIN "GraphEdgeEvent" ev ON ev."EntityAnalysisModelInstanceEntryGuid" = t."EntityAnalysisModelInstanceEntryGuid"
                    AND ev."TenantRegistryId" = @tenant AND ev."Retracted" = 0
                JOIN "GraphEdge" e ON e."Id" = ev."GraphEdgeId"
                WHERE COALESCE(t."Deleted", 0) = 0
                UNION ALL
                SELECT t."Name", n."Id"
                FROM "CaseTag" t
                JOIN "GraphNode" n ON n."TenantRegistryId" = @tenant AND n."Key" = 'Case' AND n."Value" = t."CaseId"::text
                WHERE COALESCE(t."Deleted", 0) = 0
            ) x
            ORDER BY x."Tag", x."NodeId"
            """;

        public Task<List<GetGraphTagSeedsQueryDto>> ExecuteAsync(int tenantRegistryId, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphTagSeedsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32));
        }
    }
}
