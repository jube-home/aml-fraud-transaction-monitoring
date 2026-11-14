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

namespace Jube.Data.Query.GetGraphFlowQuery
{
    public class GetGraphFlowQuery(DbContext dbContext)
    {
        private const string Sql = """
            WITH RECURSIVE flow("NodeId", "Depth", "At", "Tranche", "Path", "Edges", "Archives") AS (
                SELECT @seed::bigint, 0, @at::timestamp, @amount::double precision, ARRAY[@seed::bigint],
                       ARRAY[]::bigint[], ARRAY[]::bigint[]
                UNION ALL
                SELECT s."NextNodeId", f."Depth" + 1, s."OccurredDate", s."Amount", f."Path" || s."NextNodeId",
                       f."Edges" || s."EdgeId", f."Archives" || s."ArchiveId"
                FROM flow f
                JOIN LATERAL (
                    SELECT CASE WHEN @forward THEN e."ToNodeId" ELSE e."FromNodeId" END AS "NextNodeId",
                           ev."OccurredDate", COALESCE(ev."Amount", 0) AS "Amount", e."Id" AS "EdgeId", ev."ArchiveId"
                    FROM "GraphEdge" e
                    JOIN "GraphEdgeEvent" ev ON ev."GraphEdgeId" = e."Id" AND ev."Retracted" = 0
                    WHERE e."TenantRegistryId" = @tenant AND e."KindId" = 2
                      AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
                      AND (e."Restricted" = 0 OR @restricted)
                      AND CASE WHEN @forward THEN e."FromNodeId" = f."NodeId" AND e."ToHub" = 0
                                             ELSE e."ToNodeId" = f."NodeId" AND e."FromHub" = 0 END
                      AND CASE WHEN @forward THEN ev."OccurredDate" > f."At" AND ev."OccurredDate" <= f."At" + (CASE WHEN f."Depth" = 0 THEN @firstWindow ELSE @window END)::interval
                                             ELSE ev."OccurredDate" < f."At" AND ev."OccurredDate" >= f."At" - (CASE WHEN f."Depth" = 0 THEN @firstWindow ELSE @window END)::interval END
                      AND (f."Depth" = 0 OR f."Tranche" <= 0
                           OR COALESCE(ev."Amount", 0) BETWEEN f."Tranche" * (1 - @tolerance) AND f."Tranche" * (1 + @tolerance * 0.25))
                    ORDER BY CASE WHEN @forward THEN ev."OccurredDate" END ASC,
                             CASE WHEN NOT @forward THEN ev."OccurredDate" END DESC
                    LIMIT @fanout
                ) s ON NOT s."NextNodeId" = ANY(f."Path")
                WHERE f."Depth" < @depth
            )
            SELECT "NodeId", "Depth", "At", "Tranche",
                   ARRAY_TO_STRING("Path", ',') AS "NodePath", ARRAY_TO_STRING("Edges", ',') AS "EdgePath",
                   ARRAY_TO_STRING("Archives", ',') AS "ArchivePath"
            FROM flow WHERE "Depth" > 0
            ORDER BY "Depth", "At", "NodeId"
            LIMIT @limit
            """;

        public Task<List<GetGraphFlowQueryDto>> ExecuteAsync(int tenantRegistryId, long seed, DateTime at, double amount,
            bool forward, Guid[] definitions, bool includeRestricted, TimeSpan firstWindow, TimeSpan hopWindow,
            double tolerance, int fanOut, int depth, int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphFlowQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("seed", seed, DataType.Int64),
                new DataParameter("at", at, DataType.DateTime2),
                new DataParameter("amount", amount, DataType.Double),
                new DataParameter("forward", forward, DataType.Boolean),
                new DataParameter("defs", definitions),
                new DataParameter("restricted", includeRestricted, DataType.Boolean),
                new DataParameter("firstWindow", firstWindow, DataType.Interval),
                new DataParameter("window", hopWindow, DataType.Interval),
                new DataParameter("tolerance", tolerance, DataType.Double),
                new DataParameter("fanout", fanOut, DataType.Int32),
                new DataParameter("depth", depth, DataType.Int32),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
