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

namespace Jube.Data.Query.GetGraphRecallSnapshotQuery
{
    public class GetGraphRecallSnapshotQuery(DbContext dbContext)
    {
        public const char TagSeparator = '\u001e';
        public const char TagFieldSeparator = '\u001f';

        private const string Sql = """
            SELECT n."Id", n."Key", n."Value", n."Hub", n."Degree", n."DegreeMoney", n."DegreeIdentity",
                   n."HubNeighbourCount", n."CommunityId", n."CoreNumber", n."PageRank", n."HubScore",
                   n."AuthorityScore", n."ClusteringCoefficient", n."DistanceToConfirmed", n."ConfirmedWithin1",
                   n."ConfirmedWithin2", n."ConfirmedWithin3", n."DistanceToNotConfirmed", n."PropagationScore",
                   c."Size" AS "CommunitySize", c."ConfirmedRate" AS "CommunityConfirmedRate",
                   c."TenantRateRatio" AS "CommunityTenantRateRatio",
                   (SELECT COALESCE(BIT_OR(1 << (k."KindId" - 1)), 0)
                    FROM (SELECT p."KindId"::int AS "KindId" FROM "GraphPattern" p WHERE p."AnchorNodeId" = n."Id"
                          UNION ALL
                          SELECT p."KindId"::int FROM "GraphPatternNode" pn
                          JOIN "GraphPattern" p ON p."Id" = pn."GraphPatternId"
                          WHERE pn."GraphNodeId" = n."Id") k) AS "PatternKinds",
                   (SELECT STRING_AGG(t."Tag" || E'\x1f' || t."Distance" || E'\x1f' || t."Within1" || E'\x1f' || t."Within2"
                                      || E'\x1f' || t."Within3", E'\x1e' ORDER BY t."Tag")
                    FROM "GraphNodeTag" t WHERE t."GraphNodeId" = n."Id") AS "Tags"
            FROM "GraphNode" n
            LEFT JOIN "GraphCommunity" c ON c."Id" = n."CommunityId" AND c."Deleted" = 0
            WHERE n."TenantRegistryId" = @tenant AND n."Id" > @after AND n."Key" <> 'Case'
              AND (@priority = 0 OR (COALESCE(n."Hub", 0) = 1 OR n."DistanceToConfirmed" IS NOT NULL
                   OR n."DistanceToNotConfirmed" IS NOT NULL OR COALESCE(n."PropagationScore", 0) > 0 OR c."ConfirmedRate" IS NOT NULL
                   OR EXISTS (SELECT 1 FROM "GraphNodeTag" t WHERE t."GraphNodeId" = n."Id")))
              AND (@priority = 1 OR NOT (COALESCE(n."Hub", 0) = 1 OR n."DistanceToConfirmed" IS NOT NULL
                   OR n."DistanceToNotConfirmed" IS NOT NULL OR COALESCE(n."PropagationScore", 0) > 0 OR c."ConfirmedRate" IS NOT NULL
                   OR EXISTS (SELECT 1 FROM "GraphNodeTag" t WHERE t."GraphNodeId" = n."Id")))
            ORDER BY n."Id"
            LIMIT @limit
            """;

        public Task<List<GetGraphRecallSnapshotQueryDto>> ExecuteAsync(int tenantRegistryId, long afterNodeId,
            bool priority, int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphRecallSnapshotQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("after", afterNodeId, DataType.Int64),
                new DataParameter("priority", priority ? 1 : 0, DataType.Int32),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
