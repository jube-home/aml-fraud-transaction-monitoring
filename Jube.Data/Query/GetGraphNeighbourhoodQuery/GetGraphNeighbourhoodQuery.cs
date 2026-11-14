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

namespace Jube.Data.Query.GetGraphNeighbourhoodQuery
{
    public class GetGraphNeighbourhoodQuery(DbContext dbContext)
    {
        public const int MaximumRows = 20000;

        private const string Sql = """
            WITH RECURSIVE frontier("NodeId", "Depth", "Path", "EdgeId", "Via") AS (
                SELECT s."Id", 0, ARRAY[s."Id"], NULL::bigint, NULL::bigint
                FROM unnest(@seeds::bigint[]) AS s("Id")
                UNION ALL
                SELECT n."NodeId", f."Depth" + 1, f."Path" || n."NodeId", n."EdgeId", f."NodeId"
                FROM frontier f
                JOIN LATERAL (
                    (SELECT e."ToNodeId" AS "NodeId", e."Id" AS "EdgeId", e."Weight"
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."FromNodeId" = f."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
                        AND (e."ToHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                        AND e."LastSeen" >= @from AND e."FirstSeen" <= @to
                        AND e."AmountSum" >= @minimum
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                    UNION ALL
                    (SELECT e."FromNodeId", e."Id", e."Weight"
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."ToNodeId" = f."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
                        AND (e."FromHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                        AND e."LastSeen" >= @from AND e."FirstSeen" <= @to
                        AND e."AmountSum" >= @minimum
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                ) n ON NOT n."NodeId" = ANY(f."Path")
                WHERE f."Depth" < @depth
            )
            SELECT "NodeId", "Depth", "Via" AS "ViaNodeId", "EdgeId" FROM frontier LIMIT @maxrows
            """;

        public Task<List<GetGraphNeighbourhoodQueryDto>> ExecuteAsync(int tenantRegistryId, long[] seeds,
            Guid[] definitions, bool includeHubs, bool includeRestricted, DateTime from, DateTime to,
            double minimumAmount, int fanOut, int depth, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphNeighbourhoodQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("seeds", seeds),
                new DataParameter("defs", definitions),
                new DataParameter("hubs", includeHubs, DataType.Boolean),
                new DataParameter("restricted", includeRestricted, DataType.Boolean),
                new DataParameter("from", from, DataType.DateTime2),
                new DataParameter("to", to, DataType.DateTime2),
                new DataParameter("minimum", minimumAmount, DataType.Double),
                new DataParameter("fanout", fanOut, DataType.Int32),
                new DataParameter("depth", depth, DataType.Int32),
                new DataParameter("maxrows", MaximumRows, DataType.Int32));
        }
    }
}
