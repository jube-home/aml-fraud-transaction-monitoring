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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphExplodeQuery
{
    public class GetGraphExplodeQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT CASE WHEN e."FromNodeId" = @node THEN e."ToNodeId" ELSE e."FromNodeId" END AS "NodeId",
                   SUM(e."Count")::integer AS "Count",
                   SUM(e."AmountSum") AS "Total",
                   MIN(e."FirstSeen") AS "First",
                   MAX(e."LastSeen") AS "Last",
                   ARRAY_TO_STRING(ARRAY_AGG(DISTINCT e."GraphRegistryEdgeGuid"), ',') AS "Definitions",
                   ARRAY_TO_STRING(ARRAY_AGG(e."Id"), ',') AS "EdgeIds"
            FROM "GraphEdge" e
            WHERE e."TenantRegistryId" = @tenant AND (e."FromNodeId" = @node OR e."ToNodeId" = @node)
              AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
              AND (e."Restricted" = 0 OR @restricted)
              AND e."LastSeen" >= @from AND e."FirstSeen" <= @to
            GROUP BY 1
            ORDER BY "Total" DESC, "Count" DESC
            LIMIT @limit
            """;

        public Task<List<GetGraphExplodeQueryDto>> ExecuteAsync(int tenantRegistryId, long nodeId, Guid[] definitions,
            bool includeRestricted, DateTime from, DateTime to, int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphExplodeQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("node", nodeId, DataType.Int64),
                new DataParameter("defs", definitions),
                new DataParameter("restricted", includeRestricted, DataType.Boolean),
                new DataParameter("from", from, DataType.DateTime2),
                new DataParameter("to", to, DataType.DateTime2),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
