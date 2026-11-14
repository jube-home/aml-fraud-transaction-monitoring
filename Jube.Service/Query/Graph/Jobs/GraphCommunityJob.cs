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

using System.Globalization;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.GetGraphCommunityLabelsQuery;
using Jube.Data.Query.GetGraphConfirmedSeedsQuery;
using Jube.Data.Repository;
using Jube.Graph.Algorithms;
using Jube.Service.Query.Graph.Jobs.Models;
using LinqToDB;
using LinqToDB.Data;
using log4net;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphCommunityJob(string connectionString, ILog log, double resolution, int maximumEdges)
    {
        public const int MinimumCommunitySize = 3;
        private const string Cycle = "Communities";

        private const string WriteSql = """
            UPDATE "GraphNode" n SET
                "ComponentId" = u."Component", "ComponentSize" = u."ComponentSize", "CommunityId" = u."Community",
                "CoreNumber" = u."Core", "PageRank" = u."PageRank", "HubScore" = u."Hub", "AuthorityScore" = u."Authority",
                "ClusteringCoefficient" = u."Clustering", "ComputedDate" = @now
            FROM unnest(@ids::bigint[], @components::bigint[], @sizes::integer[], @communities::bigint[], @cores::integer[],
                        @pageranks::double precision[], @hubs::double precision[], @authorities::double precision[],
                        @clusterings::double precision[])
                 AS u("Id", "Component", "ComponentSize", "Community", "Core", "PageRank", "Hub", "Authority", "Clustering")
            WHERE n."Id" = u."Id" AND n."TenantRegistryId" = @tenant
            """;

        private const string ClearStaleSql = """
            UPDATE "GraphNode" SET "ComponentId" = NULL, "ComponentSize" = NULL, "CommunityId" = NULL, "CoreNumber" = NULL,
                "PageRank" = NULL, "HubScore" = NULL, "AuthorityScore" = NULL, "ClusteringCoefficient" = NULL
            WHERE "TenantRegistryId" = @tenant AND ("ComputedDate" IS NULL OR "ComputedDate" < @now)
              AND ("CommunityId" IS NOT NULL OR "ComponentId" IS NOT NULL OR "PageRank" IS NOT NULL OR "CoreNumber" IS NOT NULL)
            """;

        public async Task<GraphCommunityResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var tenants = await GraphAlgorithmSupport.TenantsAsync(dbContext, token).ConfigureAwait(false);
            var nodes = 0;
            var communities = 0;
            string? skipped = null;

            foreach (var tenant in tenants)
            {
                token.ThrowIfCancellationRequested();

                if (!await GraphAlgorithmSupport.TryLockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false))
                {
                    skipped = $"tenant {tenant} locked";

                    continue;
                }

                try
                {
                    var outcome = await RunTenantAsync(dbContext, tenant, token).ConfigureAwait(false);
                    nodes += outcome.Nodes;
                    communities += outcome.Communities;
                }
                finally
                {
                    await GraphAlgorithmSupport.UnlockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false);
                }
            }

            return new GraphCommunityResult(tenants.Count, nodes, communities, skipped);
        }

        public async Task<(int Nodes, int Communities)> RunTenantAsync(DbContext dbContext, int tenant,
            CancellationToken token = default)
        {
            var (graph, directed, _) = await GraphAlgorithmSupport.LoadAsync(dbContext, tenant, maximumEdges, token)
                .ConfigureAwait(false);
            var repository = new GraphCommunityRepository(dbContext);
            await repository.MarkAllDeletedAsync(tenant, token).ConfigureAwait(false);

            if (graph.NodeCount == 0)
            {
                await dbContext.ExecuteAsync(ClearStaleSql, token,
                    new DataParameter("tenant", tenant, DataType.Int32),
                    new DataParameter("now", DateTime.UtcNow, DataType.DateTime2)).ConfigureAwait(false);

                return (0, 0);
            }

            var (component, componentSizes) = UnionFind.Components(graph);
            var (community, _) = Louvain.Communities(graph, resolution);
            var cores = KCore.CoreNumbers(graph);
            var pageRank = PageRank.Compute(directed);
            var (hub, authority) = Hits.Compute(directed);
            var (_, clustering) = Triangles.Count(graph);
            var seeds = (await new GetGraphConfirmedSeedsQuery(dbContext).ExecuteAsync(tenant, token).ConfigureAwait(false))
                .ToDictionary(s => s.NodeId);
            var tenantClosed = seeds.Values.Sum(s => s.Closed);
            var tenantRate = tenantClosed == 0 ? (double?)null : seeds.Values.Sum(s => s.Confirmed) / (double)tenantClosed;
            var componentRoot = new Dictionary<int, long>();
            var communityRows = new Dictionary<int, long>();
            var now = DateTime.UtcNow;
            var written = 0;

            foreach (var group in Enumerable.Range(0, graph.NodeCount).GroupBy(i => community[i])
                         .Where(g => g.Count() >= MinimumCommunitySize).OrderBy(g => g.Min(i => graph.NodeIds[i])))
            {
                var members = group.ToArray();
                var memberSet = members.ToHashSet();
                var inside = 0;

                foreach (var u in members)
                {
                    for (var p = graph.Offsets[u]; p < graph.Offsets[u + 1]; p++)
                    {
                        if (memberSet.Contains(graph.Neighbours[p]))
                        {
                            inside++;
                        }
                    }
                }

                var size = members.Length;
                var density = size < 2 ? 0 : inside / (double)(size * (size - 1));
                var confirmed = members.Sum(i => seeds.TryGetValue(graph.NodeIds[i], out var s) ? s.Confirmed : 0);
                var closed = members.Sum(i => seeds.TryGetValue(graph.NodeIds[i], out var s) ? s.Closed : 0);
                var rate = closed == 0 ? (double?)null : confirmed / (double)closed;
                var ids = members.Select(i => graph.NodeIds[i]).ToArray();
                var labels = await new GetGraphCommunityLabelsQuery(dbContext).ExecuteAsync(ids, 3, token)
                    .ConfigureAwait(false);
                var id = await repository.InsertAsync(new GraphCommunity
                {
                    TenantRegistryId = tenant, Size = size, Density = density, ConfirmedCount = confirmed,
                    ClosedCount = closed, ConfirmedRate = rate,
                    TenantRateRatio = rate.HasValue && tenantRate is > 0 ? rate / tenantRate : null,
                    Label = string.Join(", ", labels.Select(l => string.Format(CultureInfo.InvariantCulture, "{0} x{1}", l.Key, l.Count))),
                    ComputedDate = now, Deleted = 0
                }, token).ConfigureAwait(false);
                communityRows[group.Key] = id;
                written++;
            }

            for (var i = 0; i < graph.NodeCount; i++)
            {
                if (!componentRoot.TryGetValue(component[i], out var root) || graph.NodeIds[i] < root)
                {
                    componentRoot[component[i]] = graph.NodeIds[i];
                }
            }

            const int chunk = 50000;

            for (var start = 0; start < graph.NodeCount; start += chunk)
            {
                var slice = Enumerable.Range(start, Math.Min(chunk, graph.NodeCount - start)).ToArray();
                await dbContext.ExecuteAsync(WriteSql, token,
                    new DataParameter("tenant", tenant, DataType.Int32),
                    new DataParameter("now", now, DataType.DateTime2),
                    new DataParameter("ids", slice.Select(i => graph.NodeIds[i]).ToArray()),
                    new DataParameter("components", slice.Select(i => componentRoot[component[i]]).ToArray()),
                    new DataParameter("sizes", slice.Select(i => componentSizes[component[i]]).ToArray()),
                    new DataParameter("communities", slice.Select(i => communityRows.TryGetValue(community[i], out var c) ? c : (long?)null).ToArray()),
                    new DataParameter("cores", slice.Select(i => cores[i]).ToArray()),
                    new DataParameter("pageranks", slice.Select(i => pageRank[i]).ToArray()),
                    new DataParameter("hubs", slice.Select(i => hub[i]).ToArray()),
                    new DataParameter("authorities", slice.Select(i => authority[i]).ToArray()),
                    new DataParameter("clusterings", slice.Select(i => clustering[i]).ToArray())).ConfigureAwait(false);
            }

            await dbContext.ExecuteAsync(ClearStaleSql, token,
                new DataParameter("tenant", tenant, DataType.Int32),
                new DataParameter("now", now, DataType.DateTime2)).ConfigureAwait(false);

            return (graph.NodeCount, written);
        }
    }
}
