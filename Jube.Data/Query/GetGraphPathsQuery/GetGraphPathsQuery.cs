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

namespace Jube.Data.Query.GetGraphPathsQuery
{
    public class GetGraphPathsQuery(DbContext dbContext)
    {
        private const string Sql = """
            WITH RECURSIVE fwd("NodeId", "Depth", "Path", "Edges", "Weight") AS (
                SELECT @seed::bigint, 0, ARRAY[@seed::bigint], ARRAY[]::bigint[], 1.0::double precision
                UNION ALL
                SELECT n."NodeId", f."Depth" + 1, f."Path" || n."NodeId", f."Edges" || n."EdgeId", f."Weight" * n."Weight"
                FROM fwd f
                JOIN LATERAL (
                    (SELECT e."ToNodeId" AS "NodeId", e."Id" AS "EdgeId", e."Weight"::double precision AS "Weight"
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."FromNodeId" = f."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[]) AND (e."ToHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                    UNION ALL
                    (SELECT e."FromNodeId", e."Id", e."Weight"::double precision
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."ToNodeId" = f."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[]) AND (e."FromHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                ) n ON NOT n."NodeId" = ANY(f."Path")
                WHERE f."Depth" < @forward
            ), bwd("NodeId", "Depth", "Path", "Edges", "Weight") AS (
                SELECT @target::bigint, 0, ARRAY[@target::bigint], ARRAY[]::bigint[], 1.0::double precision
                UNION ALL
                SELECT n."NodeId", b."Depth" + 1, n."NodeId" || b."Path", n."EdgeId" || b."Edges", b."Weight" * n."Weight"
                FROM bwd b
                JOIN LATERAL (
                    (SELECT e."ToNodeId" AS "NodeId", e."Id" AS "EdgeId", e."Weight"::double precision AS "Weight"
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."FromNodeId" = b."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[]) AND (e."ToHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                    UNION ALL
                    (SELECT e."FromNodeId", e."Id", e."Weight"::double precision
                       FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."ToNodeId" = b."NodeId"
                        AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[]) AND (e."FromHub" = 0 OR @hubs)
                        AND (e."Restricted" = 0 OR @restricted)
                      ORDER BY e."Weight" DESC LIMIT @fanout)
                ) n ON NOT n."NodeId" = ANY(b."Path")
                WHERE b."Depth" < @backward
            )
            SELECT DISTINCT "Hops", "Weight", "NodePath", "EdgePath"
            FROM (
                SELECT f."Depth" + b."Depth" AS "Hops", f."Weight" * b."Weight" AS "Weight",
                       ARRAY_TO_STRING(f."Path" || b."Path"[2:], ',') AS "NodePath",
                       ARRAY_TO_STRING(f."Edges" || b."Edges", ',') AS "EdgePath"
                FROM fwd f JOIN bwd b ON b."NodeId" = f."NodeId"
                WHERE f."Depth" + b."Depth" > 0 AND NOT (f."Path"[1:array_length(f."Path", 1) - 1] && b."Path"[2:])
            ) joined
            ORDER BY "Hops", "Weight" DESC, "NodePath"
            LIMIT @limit
            """;

        public Task<List<GetGraphPathsQueryDto>> ExecuteAsync(int tenantRegistryId, long seed, long target,
            Guid[] definitions, bool includeHubs, bool includeRestricted, int forwardDepth, int backwardDepth, int fanOut,
            int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphPathsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("seed", seed, DataType.Int64),
                new DataParameter("target", target, DataType.Int64),
                new DataParameter("defs", definitions),
                new DataParameter("hubs", includeHubs, DataType.Boolean),
                new DataParameter("restricted", includeRestricted, DataType.Boolean),
                new DataParameter("forward", forwardDepth, DataType.Int32),
                new DataParameter("backward", backwardDepth, DataType.Int32),
                new DataParameter("fanout", fanOut, DataType.Int32),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
