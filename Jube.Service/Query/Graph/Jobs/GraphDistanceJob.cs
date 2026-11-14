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
using Jube.Data.Poco;
using Jube.Data.Query.GetGraphConfirmedSeedsQuery;
using Jube.Data.Query.GetGraphTagSeedsQuery;
using Jube.Data.Query.MarkCaseRiskRecheckQuery;
using Jube.Dto.Options;
using Jube.Data.Repository;
using Jube.Graph.Algorithms;
using Jube.Graph.Model;
using Jube.Service.Query.Graph.Jobs.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphDistanceJob(string connectionString, ILog log, int cap, int maximumEdges)
    {
        private const string Cycle = "Distance";

        private const string WriteSql = """
            UPDATE "GraphNode" n SET
                "DistanceToConfirmed" = u."Confirmed", "ConfirmedWithin1" = u."Within1", "ConfirmedWithin2" = u."Within2",
                "ConfirmedWithin3" = u."Within3", "DistanceToNotConfirmed" = u."NotConfirmed", "PropagationScore" = u."Propagation",
                "ComputedDate" = @now
            FROM unnest(@ids::bigint[], @confirmed::smallint[], @within1::integer[], @within2::integer[], @within3::integer[],
                        @notConfirmed::smallint[], @propagation::double precision[])
                 AS u("Id", "Confirmed", "Within1", "Within2", "Within3", "NotConfirmed", "Propagation")
            WHERE n."Id" = u."Id" AND n."TenantRegistryId" = @tenant
            """;

        private const string ClearSql = """
            UPDATE "GraphNode" SET "DistanceToConfirmed" = NULL, "ConfirmedWithin1" = NULL, "ConfirmedWithin2" = NULL,
                "ConfirmedWithin3" = NULL, "DistanceToNotConfirmed" = NULL, "PropagationScore" = NULL
            WHERE "TenantRegistryId" = @tenant AND ("DistanceToConfirmed" IS NOT NULL OR "DistanceToNotConfirmed" IS NOT NULL
                OR "PropagationScore" IS NOT NULL)
            """;

        public async Task<GraphDistanceResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var tenants = await GraphAlgorithmSupport.TenantsAsync(dbContext, token).ConfigureAwait(false);
            var seeds = 0;
            var nodes = 0;

            foreach (var tenant in tenants)
            {
                token.ThrowIfCancellationRequested();

                if (!await GraphAlgorithmSupport.TryLockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    var outcome = await RunTenantAsync(dbContext, tenant, token).ConfigureAwait(false);
                    seeds += outcome.Seeds;
                    nodes += outcome.Nodes;
                }
                finally
                {
                    await GraphAlgorithmSupport.UnlockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false);
                }
            }

            return new GraphDistanceResult(tenants.Count, seeds, nodes);
        }

        public async Task<(int Seeds, int Nodes)> RunTenantAsync(DbContext dbContext, int tenant,
            CancellationToken token = default)
        {
            var before = await DistancesAsync(dbContext, tenant, token).ConfigureAwait(false);
            var outcome = await ComputeTenantAsync(dbContext, tenant, token).ConfigureAwait(false);
            var after = await DistancesAsync(dbContext, tenant, token).ConfigureAwait(false);

            var changed = before.Keys.Union(after.Keys)
                .Where(id => before.GetValueOrDefault(id) != after.GetValueOrDefault(id)).ToList();

            try
            {
                await new MarkCaseRiskRecheckQuery(dbContext)
                    .ForGraphNodesAsync(tenant, changed, CaseRiskTrigger.NeighbourTagged, token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Graph Distance: the open cases of {changed.Count} changed nodes of tenant {tenant} " +
                          $"could not be marked for risk recheck: {ex}");
            }

            return outcome;
        }

        private static async Task<Dictionary<long, short?>> DistancesAsync(DbContext dbContext, int tenant,
            CancellationToken token)
        {
            var rows = await dbContext.GraphNode
                .Where(w => w.TenantRegistryId == tenant && w.DistanceToConfirmed != null)
                .Select(s => new { s.Id, s.DistanceToConfirmed }).ToListAsync(token).ConfigureAwait(false);

            return rows.ToDictionary(r => r.Id, r => r.DistanceToConfirmed);
        }

        private async Task<(int Seeds, int Nodes)> ComputeTenantAsync(DbContext dbContext, int tenant,
            CancellationToken token)
        {
            var (graph, _, _) = await GraphAlgorithmSupport.LoadAsync(dbContext, tenant, maximumEdges, token)
                .ConfigureAwait(false);
            var tenantParameter = new DataParameter("tenant", tenant, DataType.Int32);
            await dbContext.ExecuteAsync(ClearSql, token, tenantParameter).ConfigureAwait(false);
            var outcomes = await new GetGraphConfirmedSeedsQuery(dbContext).ExecuteAsync(tenant, token).ConfigureAwait(false);
            var confirmed = new List<int>();
            var notConfirmed = new List<int>();

            foreach (var outcome in outcomes)
            {
                if (!graph.TryGetIndex(outcome.NodeId, out var index))
                {
                    continue;
                }

                if (outcome.Confirmed > 0)
                {
                    confirmed.Add(index);
                }
                else if (outcome.Closed > 0)
                {
                    notConfirmed.Add(index);
                }
            }

            var tagged = await TagsAsync(dbContext, tenant, graph, token).ConfigureAwait(false);

            if (graph.NodeCount == 0 || confirmed.Count + notConfirmed.Count == 0)
            {
                return (tagged, 0);
            }

            var bad = BoundedBfs.DistancesFrom(graph, confirmed, cap);
            var good = BoundedBfs.DistancesFrom(graph, notConfirmed, cap);
            var propagation = confirmed.Count == 0
                ? new double[graph.NodeCount]
                : PageRank.Personalised(graph, Seeds.Bad(confirmed.ToArray()));
            var touched = Enumerable.Range(0, graph.NodeCount)
                .Where(i => bad.Distance[i] >= 0 || good.Distance[i] >= 0 || propagation[i] > 0).ToArray();
            const int chunk = 50000;

            for (var start = 0; start < touched.Length; start += chunk)
            {
                var slice = touched.Skip(start).Take(chunk).ToArray();
                await dbContext.ExecuteAsync(WriteSql, token, tenantParameter,
                    new DataParameter("now", DateTime.UtcNow, DataType.DateTime2),
                    new DataParameter("ids", slice.Select(i => graph.NodeIds[i]).ToArray()),
                    new DataParameter("confirmed", slice.Select(i => bad.Distance[i] < 0 ? (short?)null : bad.Distance[i]).ToArray()),
                    new DataParameter("within1", slice.Select(i => bad.Within1[i]).ToArray()),
                    new DataParameter("within2", slice.Select(i => bad.Within2[i]).ToArray()),
                    new DataParameter("within3", slice.Select(i => bad.Within3[i]).ToArray()),
                    new DataParameter("notConfirmed", slice.Select(i => good.Distance[i] < 0 ? (short?)null : good.Distance[i]).ToArray()),
                    new DataParameter("propagation", slice.Select(i => propagation[i]).ToArray())).ConfigureAwait(false);
            }

            return (confirmed.Count + notConfirmed.Count + tagged, touched.Length);
        }

        private async Task<int> TagsAsync(DbContext dbContext, int tenant, CompressedGraph graph, CancellationToken token)
        {
            var seeds = await new GetGraphTagSeedsQuery(dbContext).ExecuteAsync(tenant, token).ConfigureAwait(false);
            var repository = new GraphNodeTagRepository(dbContext);
            var tags = seeds.Select(s => s.Tag).Distinct(StringComparer.Ordinal).ToList();
            await repository.DeleteTagsNotInAsync(tenant, tags, token).ConfigureAwait(false);
            var seedCount = 0;

            foreach (var group in seeds.GroupBy(s => s.Tag, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                var indices = new List<int>();

                foreach (var seed in group)
                {
                    if (graph.TryGetIndex(seed.NodeId, out var index))
                    {
                        indices.Add(index);
                    }
                }

                var rows = new List<GraphNodeTag>();

                if (indices.Count > 0)
                {
                    var result = BoundedBfs.DistancesFrom(graph, indices, cap);

                    for (var i = 0; i < graph.NodeCount; i++)
                    {
                        if (result.Distance[i] >= 0)
                        {
                            rows.Add(new GraphNodeTag
                            {
                                TenantRegistryId = tenant, GraphNodeId = graph.NodeIds[i], Tag = group.Key,
                                Distance = result.Distance[i], Within1 = result.Within1[i], Within2 = result.Within2[i],
                                Within3 = result.Within3[i]
                            });
                        }
                    }
                }

                await repository.ReplaceTagAsync(tenant, group.Key, rows, token).ConfigureAwait(false);
                seedCount += indices.Count;
            }

            return seedCount;
        }
    }
}
