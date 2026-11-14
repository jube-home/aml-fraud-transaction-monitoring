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

using Jube.Data.Context;
using Jube.Data.Query.GetGraphHubCandidatesQuery;
using Jube.Data.Query.GetGraphHubThresholdsQuery;
using Jube.Data.Repository;
using Jube.Service.Query.Graph.Jobs.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphHubJob(string connectionString, ILog log, double percentile, int minimumDegree)
    {
        private const string DegreeSql = """
            WITH touching AS (
                SELECT e."FromNodeId" AS "Node", e."ToNodeId" AS "Other", e."KindId" FROM "GraphEdge" e WHERE e."TenantRegistryId" = @tenant AND e."Restricted" = 0
                UNION ALL
                SELECT e."ToNodeId", e."FromNodeId", e."KindId" FROM "GraphEdge" e WHERE e."TenantRegistryId" = @tenant AND e."Restricted" = 0
            ), degrees AS (
                SELECT t."Node",
                       COUNT(DISTINCT t."Other")::integer AS "Degree",
                       COUNT(DISTINCT t."Other") FILTER (WHERE t."KindId" = 2)::integer AS "DegreeMoney",
                       COUNT(DISTINCT t."Other") FILTER (WHERE t."KindId" <> 2)::integer AS "DegreeIdentity"
                FROM touching t GROUP BY t."Node")
            UPDATE "GraphNode" n SET "Degree" = COALESCE(d."Degree", 0), "DegreeMoney" = COALESCE(d."DegreeMoney", 0),
                "DegreeIdentity" = COALESCE(d."DegreeIdentity", 0)
            FROM "GraphNode" g LEFT JOIN degrees d ON d."Node" = g."Id"
            WHERE n."Id" = g."Id" AND n."TenantRegistryId" = @tenant
              AND (n."Degree" IS DISTINCT FROM COALESCE(d."Degree", 0)
                   OR n."DegreeMoney" IS DISTINCT FROM COALESCE(d."DegreeMoney", 0)
                   OR n."DegreeIdentity" IS DISTINCT FROM COALESCE(d."DegreeIdentity", 0))
            """;

        private const string SyncRestrictedSql = """
            UPDATE "GraphEdge" e SET "Restricted" = COALESCE(d."Restricted", 0)
            FROM "GraphRegistryEdge" d
            WHERE e."TenantRegistryId" = @tenant AND d."Guid" = e."GraphRegistryEdgeGuid" AND COALESCE(d."Deleted", 0) = 0
              AND e."Restricted" <> COALESCE(d."Restricted", 0)
            """;

        private const string ClearHubsSql = """
            UPDATE "GraphNode" SET "Hub" = CASE WHEN "HubOverrideId" = 2 THEN 1 ELSE 0 END, "HubByEdgeGuids" = NULL
            WHERE "TenantRegistryId" = @tenant AND ("Hub" = 1 OR "HubOverrideId" <> 0)
            """;

        private const string SetHubsSql = """
            UPDATE "GraphNode" n SET "Hub" = 1,
                "HubByEdgeGuids" = (SELECT ARRAY_AGG(DISTINCT h."Def") FROM unnest(@nodes::bigint[], @defs::uuid[]) AS h("Node", "Def") WHERE h."Node" = n."Id")
            WHERE n."TenantRegistryId" = @tenant AND n."HubOverrideId" <> 1 AND n."Id" = ANY(@nodes::bigint[])
            """;

        private const string SyncEdgeHubsSql = """
            UPDATE "GraphEdge" e SET "FromHub" = f."Hub", "ToHub" = t."Hub"
            FROM "GraphNode" f, "GraphNode" t
            WHERE e."TenantRegistryId" = @tenant AND f."Id" = e."FromNodeId" AND t."Id" = e."ToNodeId"
              AND (e."FromHub" <> f."Hub" OR e."ToHub" <> t."Hub")
            """;

        private const string SpecificitySql = """
            WITH counts AS (
                SELECT e."GraphRegistryEdgeGuid" AS "Def", e."ToNodeId", COUNT(DISTINCT e."FromNodeId") AS "K"
                FROM "GraphEdge" e WHERE e."TenantRegistryId" = @tenant AND e."KindId" <> 2
                GROUP BY e."GraphRegistryEdgeGuid", e."ToNodeId")
            UPDATE "GraphEdge" e SET "Specificity" = 1.0 / LN(2 + c."K")
            FROM counts c
            WHERE e."TenantRegistryId" = @tenant AND e."GraphRegistryEdgeGuid" = c."Def" AND e."ToNodeId" = c."ToNodeId"
              AND ABS(e."Specificity" - 1.0 / LN(2 + c."K")) > 1e-6
            """;

        private const string WeightSql = """
            UPDATE "GraphEdge" e SET "Weight" = COALESCE(d."KindPrior", 1) * e."Confidence" * e."Specificity"
                * (1 + LN(1 + e."DecayedCount")) * (1 + e."DecayedAmount")
            FROM "GraphRegistryEdge" d
            WHERE e."TenantRegistryId" = @tenant AND d."Guid" = e."GraphRegistryEdgeGuid" AND COALESCE(d."Deleted", 0) = 0
            """;

        private const string HubNeighboursSql = """
            WITH touching AS (
                SELECT e."FromNodeId" AS "Node", e."ToHub" AS "OtherHub" FROM "GraphEdge" e WHERE e."TenantRegistryId" = @tenant AND e."Restricted" = 0
                UNION ALL
                SELECT e."ToNodeId", e."FromHub" FROM "GraphEdge" e WHERE e."TenantRegistryId" = @tenant AND e."Restricted" = 0
            ), counts AS (SELECT t."Node", COUNT(*) FILTER (WHERE t."OtherHub" = 1)::integer AS "Hubs" FROM touching t GROUP BY t."Node")
            UPDATE "GraphNode" n SET "HubNeighbourCount" = COALESCE(c."Hubs", 0)
            FROM "GraphNode" g LEFT JOIN counts c ON c."Node" = g."Id"
            WHERE n."Id" = g."Id" AND n."TenantRegistryId" = @tenant AND n."HubNeighbourCount" <> COALESCE(c."Hubs", 0)
            """;

        public async Task<GraphHubResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);
            var tenants = await dbContext.GraphRegistry
                .Where(r => r.Active == 1 && (r.Deleted == null || r.Deleted == 0) && r.TenantRegistryId != null)
                .Select(r => r.TenantRegistryId!.Value).Distinct().ToListAsync(token).ConfigureAwait(false);
            var hubs = 0;
            var reweighted = 0;

            foreach (var tenant in tenants.OrderBy(t => t))
            {
                token.ThrowIfCancellationRequested();
                var outcome = await RunTenantAsync(dbContext, tenant, token).ConfigureAwait(false);
                hubs += outcome.Hubs;
                reweighted += outcome.Edges;
            }

            return new GraphHubResult(tenants.Count, hubs, reweighted);
        }

        public async Task<(int Hubs, int Edges)> RunTenantAsync(DbContext dbContext, int tenant,
            CancellationToken token = default)
        {
            var tenantParameter = new DataParameter("tenant", tenant, DataType.Int32);
            await dbContext.ExecuteAsync(SyncRestrictedSql, token, tenantParameter).ConfigureAwait(false);
            await dbContext.ExecuteAsync(DegreeSql, token, tenantParameter).ConfigureAwait(false);
            var definitions = (await new GraphRegistryEdgeRepository(dbContext, tenant).GetActiveForTenantAsync(token)
                .ConfigureAwait(false)).ToDictionary(d => d.Guid);
            var thresholds = (await new GetGraphHubThresholdsQuery(dbContext).ExecuteAsync(tenant, percentile, token)
                    .ConfigureAwait(false))
                .ToDictionary(t => t.Definition, t => t.Percentile);
            var candidates = await new GetGraphHubCandidatesQuery(dbContext).ExecuteAsync(tenant, 2, token)
                .ConfigureAwait(false);
            var hubs = new List<(long Node, Guid Def)>();

            foreach (var candidate in candidates)
            {
                if (!definitions.TryGetValue(candidate.Definition, out var definition))
                {
                    continue;
                }

                var threshold = definition.HubThreshold
                                ?? Math.Max(minimumDegree,
                                    (int)Math.Ceiling(thresholds.GetValueOrDefault(candidate.Definition, double.MaxValue)));

                if (candidate.DistinctFrom > threshold)
                {
                    hubs.Add((candidate.ToNodeId, candidate.Definition));
                }
            }

            await dbContext.ExecuteAsync(ClearHubsSql, token, tenantParameter).ConfigureAwait(false);

            if (hubs.Count > 0)
            {
                await dbContext.ExecuteAsync(SetHubsSql, token, tenantParameter,
                    new DataParameter("nodes", hubs.Select(h => h.Node).ToArray()),
                    new DataParameter("defs", hubs.Select(h => h.Def).ToArray())).ConfigureAwait(false);
            }

            await dbContext.ExecuteAsync(SyncEdgeHubsSql, token, tenantParameter).ConfigureAwait(false);
            await dbContext.ExecuteAsync(SpecificitySql, token, tenantParameter).ConfigureAwait(false);
            var edges = await dbContext.ExecuteAsync(WeightSql, token, tenantParameter).ConfigureAwait(false);
            await dbContext.ExecuteAsync(HubNeighboursSql, token, tenantParameter).ConfigureAwait(false);

            return (hubs.Select(h => h.Node).Distinct().Count(), edges);
        }
    }
}
