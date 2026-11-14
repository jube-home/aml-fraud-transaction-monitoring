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

namespace Jube.Data.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Models;
    using Poco;

    public class GraphEdgeRepository(DbContext dbContext)
    {
        private const string UpsertSql = """
            WITH incoming AS (
                SELECT * FROM unnest(@defs::uuid[], @kinds::smallint[], @restricted::smallint[], @froms::bigint[],
                    @tos::bigint[], @counts::integer[], @amounts::float8[], @firsts::timestamp[], @lasts::timestamp[],
                    @dcounts::float8[], @damounts::float8[], @ats::timestamp[], @halflives::float8[], @priors::float8[])
                    AS i("Def", "Kind", "Restricted", "From", "To", "Count", "Amount", "First", "Last", "DCount",
                         "DAmount", "At", "HalfLife", "Prior")
            ), upserted AS (
                INSERT INTO "GraphEdge" ("TenantRegistryId", "GraphRegistryEdgeGuid", "KindId", "FromNodeId", "ToNodeId",
                    "Count", "AmountSum", "FirstSeen", "LastSeen", "DecayedCount", "DecayedAmount", "DecayedAt",
                    "Specificity", "Confidence", "Weight", "Restricted", "FromHub", "ToHub")
                SELECT @tenant, i."Def", i."Kind", i."From", i."To", i."Count", i."Amount", i."First", i."Last",
                    i."DCount", i."DAmount", i."At", 1, 1,
                    i."Prior" * (1 + LN(1 + i."DCount")) * (1 + i."DAmount"), i."Restricted",
                    (SELECT "Hub" FROM "GraphNode" WHERE "Id" = i."From"), (SELECT "Hub" FROM "GraphNode" WHERE "Id" = i."To")
                FROM incoming i
                ON CONFLICT ("TenantRegistryId", "GraphRegistryEdgeGuid", "FromNodeId", "ToNodeId") DO UPDATE SET
                    "Count" = "GraphEdge"."Count" + EXCLUDED."Count",
                    "AmountSum" = "GraphEdge"."AmountSum" + EXCLUDED."AmountSum",
                    "FirstSeen" = LEAST("GraphEdge"."FirstSeen", EXCLUDED."FirstSeen"),
                    "LastSeen" = GREATEST("GraphEdge"."LastSeen", EXCLUDED."LastSeen"),
                    "DecayedCount" = "GraphEdge"."DecayedCount" * POWER(2, -EXTRACT(EPOCH FROM (GREATEST("GraphEdge"."DecayedAt", EXCLUDED."DecayedAt") - "GraphEdge"."DecayedAt")) / 86400 / (SELECT i."HalfLife" FROM incoming i WHERE i."Def" = EXCLUDED."GraphRegistryEdgeGuid" AND i."From" = EXCLUDED."FromNodeId" AND i."To" = EXCLUDED."ToNodeId"))
                                   + EXCLUDED."DecayedCount" * POWER(2, -EXTRACT(EPOCH FROM (GREATEST("GraphEdge"."DecayedAt", EXCLUDED."DecayedAt") - EXCLUDED."DecayedAt")) / 86400 / (SELECT i."HalfLife" FROM incoming i WHERE i."Def" = EXCLUDED."GraphRegistryEdgeGuid" AND i."From" = EXCLUDED."FromNodeId" AND i."To" = EXCLUDED."ToNodeId")),
                    "DecayedAmount" = "GraphEdge"."DecayedAmount" * POWER(2, -EXTRACT(EPOCH FROM (GREATEST("GraphEdge"."DecayedAt", EXCLUDED."DecayedAt") - "GraphEdge"."DecayedAt")) / 86400 / (SELECT i."HalfLife" FROM incoming i WHERE i."Def" = EXCLUDED."GraphRegistryEdgeGuid" AND i."From" = EXCLUDED."FromNodeId" AND i."To" = EXCLUDED."ToNodeId"))
                                    + EXCLUDED."DecayedAmount" * POWER(2, -EXTRACT(EPOCH FROM (GREATEST("GraphEdge"."DecayedAt", EXCLUDED."DecayedAt") - EXCLUDED."DecayedAt")) / 86400 / (SELECT i."HalfLife" FROM incoming i WHERE i."Def" = EXCLUDED."GraphRegistryEdgeGuid" AND i."From" = EXCLUDED."FromNodeId" AND i."To" = EXCLUDED."ToNodeId")),
                    "DecayedAt" = GREATEST("GraphEdge"."DecayedAt", EXCLUDED."DecayedAt"),
                    "Restricted" = EXCLUDED."Restricted"
                RETURNING "Id", "GraphRegistryEdgeGuid", "FromNodeId", "ToNodeId"
            )
            SELECT "Id", "GraphRegistryEdgeGuid", "FromNodeId", "ToNodeId" FROM upserted
            """;

        private const string WeightSql = """
            UPDATE "GraphEdge" e SET "Weight" = p."Prior" * e."Confidence" * e."Specificity"
                * (1 + LN(1 + e."DecayedCount")) * (1 + e."DecayedAmount")
            FROM unnest(@ids::bigint[], @priors::float8[]) AS p("Id", "Prior")
            WHERE e."Id" = p."Id"
            """;

        public async Task<Dictionary<(Guid Def, long From, long To), long>> UpsertManyAsync(int tenantRegistryId,
            IReadOnlyList<GraphEdgeDelta> deltas, CancellationToken token = default)
        {
            var result = new Dictionary<(Guid Def, long From, long To), long>();

            if (deltas.Count == 0)
            {
                return result;
            }

            var rows = await dbContext.QueryToListAsync<GraphEdgeKeyRow>(UpsertSql, token,
                    new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                    new DataParameter("defs", deltas.Select(d => d.GraphRegistryEdgeGuid).ToArray()),
                    new DataParameter("kinds", deltas.Select(d => (short)d.KindId).ToArray()),
                    new DataParameter("restricted", deltas.Select(d => (short)d.Restricted).ToArray()),
                    new DataParameter("froms", deltas.Select(d => d.FromNodeId).ToArray()),
                    new DataParameter("tos", deltas.Select(d => d.ToNodeId).ToArray()),
                    new DataParameter("counts", deltas.Select(d => d.Count).ToArray()),
                    new DataParameter("amounts", deltas.Select(d => d.AmountSum).ToArray()),
                    new DataParameter("firsts", deltas.Select(d => d.FirstSeen).ToArray()),
                    new DataParameter("lasts", deltas.Select(d => d.LastSeen).ToArray()),
                    new DataParameter("dcounts", deltas.Select(d => d.DecayedCount).ToArray()),
                    new DataParameter("damounts", deltas.Select(d => d.DecayedAmount).ToArray()),
                    new DataParameter("ats", deltas.Select(d => d.DecayedAt).ToArray()),
                    new DataParameter("halflives", deltas.Select(d => d.HalfLifeDays).ToArray()),
                    new DataParameter("priors", deltas.Select(d => d.KindPrior).ToArray()))
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                result[(row.GraphRegistryEdgeGuid, row.FromNodeId, row.ToNodeId)] = row.Id;
            }

            var priors = deltas.ToDictionary(d => (d.GraphRegistryEdgeGuid, d.FromNodeId, d.ToNodeId), d => d.KindPrior);
            var ids = rows.Select(r => r.Id).ToArray();
            var rowPriors = rows.Select(r => priors[(r.GraphRegistryEdgeGuid, r.FromNodeId, r.ToNodeId)]).ToArray();

            await dbContext.ExecuteAsync(WeightSql, token,
                new DataParameter("ids", ids), new DataParameter("priors", rowPriors)).ConfigureAwait(false);

            return result;
        }

        public Task<List<GraphEdge>> GetByIdsAsync(int tenantRegistryId, IReadOnlyCollection<long> ids,
            CancellationToken token = default)
        {
            return dbContext.GraphEdge.Where(e => e.TenantRegistryId == tenantRegistryId && ids.Contains(e.Id))
                .ToListAsync(token);
        }

        public Task<GraphEdge> GetByIdAsync(int tenantRegistryId, long id, CancellationToken token = default)
        {
            return dbContext.GraphEdge.FirstOrDefaultAsync(e => e.TenantRegistryId == tenantRegistryId && e.Id == id,
                token);
        }

        public async Task<List<GraphEdge>> GetTouchingAsync(int tenantRegistryId, long nodeId, int limit,
            CancellationToken token = default)
        {
            var outbound = await dbContext.GraphEdge
                .Where(e => e.TenantRegistryId == tenantRegistryId && e.FromNodeId == nodeId)
                .OrderByDescending(e => e.Weight).ThenBy(e => e.Id).Take(limit).ToListAsync(token);
            var inbound = await dbContext.GraphEdge
                .Where(e => e.TenantRegistryId == tenantRegistryId && e.ToNodeId == nodeId && e.FromNodeId != nodeId)
                .OrderByDescending(e => e.Weight).ThenBy(e => e.Id).Take(limit).ToListAsync(token);

            return outbound.Concat(inbound).ToList();
        }

        public Task<List<GraphEdge>> GetTouchingAsync(int tenantRegistryId, long nodeId,
            CancellationToken token = default)
        {
            return dbContext.GraphEdge.Where(e => e.TenantRegistryId == tenantRegistryId && e.FromNodeId == nodeId)
                .Concat(dbContext.GraphEdge.Where(e => e.TenantRegistryId == tenantRegistryId && e.ToNodeId == nodeId
                                                        && e.FromNodeId != nodeId))
                .ToListAsync(token);
        }

        public Task<int> ApplyRetractionAsync(long edgeId, int count, double amountSum, double decayedCount,
            double decayedAmount, DateTime decayedAt, double kindPrior, CancellationToken token = default)
        {
            return dbContext.GraphEdge.Where(e => e.Id == edgeId)
                .Set(e => e.Count, count)
                .Set(e => e.AmountSum, amountSum)
                .Set(e => e.DecayedCount, decayedCount)
                .Set(e => e.DecayedAmount, decayedAmount)
                .Set(e => e.DecayedAt, decayedAt)
                .Set(e => e.Weight, e => (float)(kindPrior * e.Confidence * e.Specificity
                                                 * (1 + Math.Log(1 + decayedCount)) * (1 + decayedAmount)))
                .UpdateAsync(token);
        }
    }
}
