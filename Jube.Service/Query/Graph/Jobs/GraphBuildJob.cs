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
using Jube.Data.Query.GetGraphBuildPairsQuery;
using Jube.Data.Query.GetGraphCaseNodesQuery;
using Jube.Data.Query.GetGraphEntryTenantsQuery;
using Jube.Engine.Graph;
using Jube.Data.Query.GetGraphBuildStartQuery;
using Jube.Data.Query.GetGraphBuildWindowQuery;
using Jube.Data.Query.GetGraphReprocessedEntriesQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Graph.Weights;
using Jube.Service.Query.Graph.Jobs.Models;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using log4net;
using Npgsql;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphBuildJob(string connectionString, ILog log, int batchSize, int maximumBatchesPerCycle,
        int retentionDays, double referenceAmount, bool requireArchiveKeyIndex)
    {
        public const string ArchiveKeyIndexName = "IX_ArchiveKey_EntryGuid";
        private const int ReprocessLimit = 500;
        private const int RecomputeEventLimit = 5000;

        public async Task<GraphBuildResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);

            if (requireArchiveKeyIndex && !await IndexIsValidAsync(dbContext, token).ConfigureAwait(false))
            {
                return new GraphBuildResult(0, 0, 0, 0, 0, "WaitingForArchiveKeyIndex");
            }

            var tenants = await dbContext.GraphRegistry
                .Where(r => r.Active == 1 && (r.Deleted == null || r.Deleted == 0) && r.TenantRegistryId != null)
                .Select(r => r.TenantRegistryId!.Value).Distinct().ToListAsync(token).ConfigureAwait(false);
            var definitions = 0;
            var batches = 0;
            long events = 0;
            var retracted = 0;

            foreach (var tenant in tenants.OrderBy(t => t))
            {
                token.ThrowIfCancellationRequested();
                var active = (await new GraphRegistryEdgeRepository(dbContext, tenant).GetActiveForTenantAsync(token)
                        .ConfigureAwait(false))
                    .Where(d => d.KindId is 1 or 2 or 3 && d.FromKey != null && d.ToKey != null)
                    .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90,
                        d.KindPrior ?? 1, d.Restricted ?? 0))
                    .ToList();

                if (active.Count == 0)
                {
                    continue;
                }

                definitions += active.Count;
                var outcome = await BuildTenantAsync(dbContext, tenant, active, token).ConfigureAwait(false);
                batches += outcome.Batches;
                events += outcome.Events;
                retracted += await ReprocessTenantAsync(dbContext, tenant, active, token).ConfigureAwait(false);
                batches += await BuildCaseNodesAsync(dbContext, tenant, token).ConfigureAwait(false);
            }

            return new GraphBuildResult(tenants.Count, definitions, batches, events, retracted, null);
        }

        public async Task<GraphQueueResult> ApplyQueuedAsync(int maximum, CancellationToken token = default)
        {
            var guids = GraphBuildQueue.Drain(maximum);

            if (guids.Count == 0)
            {
                return new GraphQueueResult(0, 0, 0, GraphBuildQueue.Dropped);
            }

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString,
                log);
            var tenants = await new GetGraphEntryTenantsQuery(dbContext).ExecuteAsync(guids.Distinct().ToArray(), token)
                .ConfigureAwait(false);
            long events = 0;
            var touched = 0;

            foreach (var group in tenants.GroupBy(t => t.TenantRegistryId).OrderBy(g => g.Key))
            {
                token.ThrowIfCancellationRequested();
                var active = await ActiveDefinitionsAsync(dbContext, group.Key, token).ConfigureAwait(false);

                if (active.Count == 0)
                {
                    continue;
                }

                touched++;
                var pairs = await new GetGraphBuildPairsQuery(dbContext).ExecuteAsync(group.Key,
                        active.Select(d => d.Guid).ToArray(),
                        group.Select(g => g.EntityAnalysisModelInstanceEntryGuid).Distinct().ToArray(), token)
                    .ConfigureAwait(false);
                events += (await ApplyAsync(dbContext, group.Key, active, pairs, token).ConfigureAwait(false)).Events;
            }

            return new GraphQueueResult(guids.Count, touched, events, GraphBuildQueue.Dropped);
        }

        public async Task<int> BuildCaseNodesAsync(DbContext dbContext, int tenant, CancellationToken token = default)
        {
            var batches = 0;

            foreach (var definition in await GraphCaseEdge.EnsureAsync(dbContext, tenant, token).ConfigureAwait(false))
            {
                batches += await BuildCaseNodesAsync(dbContext, tenant, definition.Guid, token).ConfigureAwait(false);
            }

            return batches;
        }

        private async Task<int> BuildCaseNodesAsync(DbContext dbContext, int tenant, Guid definitionGuid,
            CancellationToken token)
        {
            var states = new GraphBuildStateRepository(dbContext);
            var state = await states.GetOrCreateAsync(tenant, definitionGuid, token).ConfigureAwait(false);
            var batches = 0;

            while (batches < maximumBatchesPerCycle)
            {
                token.ThrowIfCancellationRequested();
                var started = DateTime.UtcNow;
                var cases = await new GetGraphCaseNodesQuery(dbContext)
                    .ExecuteAsync(tenant, state.LastArchiveKeyId, batchSize, token).ConfigureAwait(false);

                if (cases.Count == 0)
                {
                    break;
                }

                var touches = cases.SelectMany(c => new[]
                    {
                        (GraphCaseEdge.NodeKey, c.CaseId.ToString(CultureInfo.InvariantCulture), c.CreatedDate),
                        (c.CaseKey, c.CaseKeyValue, c.CreatedDate)
                    })
                    .GroupBy(t => (t.Item1, t.Item2))
                    .Select(g => new GraphNodeTouch(g.Key.Item1, g.Key.Item2, Milliseconds(g.Min(t => t.Item3)),
                        Milliseconds(g.Max(t => t.Item3))))
                    .ToList();
                var nodeIds = await new GraphNodeRepository(dbContext).UpsertManyAsync(tenant, touches, token)
                    .ConfigureAwait(false);
                var deltas = cases.Select(c =>
                {
                    var at = Milliseconds(c.CreatedDate);

                    return new GraphEdgeDelta(definitionGuid, GraphCaseEdge.KindId, 0,
                        nodeIds[(c.CaseKey, c.CaseKeyValue)],
                        nodeIds[(GraphCaseEdge.NodeKey, c.CaseId.ToString(CultureInfo.InvariantCulture))], 1, 0, at, at, 1,
                        0, at, GraphCaseEdge.HalfLifeDays, 1);
                }).ToList();
                await new GraphEdgeRepository(dbContext).UpsertManyAsync(tenant, deltas, token).ConfigureAwait(false);
                state.LastArchiveKeyId = cases.Max(c => c.CaseId);
                state.Edges += deltas.Count;
                state.Nodes += touches.Count;
                state.LastRunDate = DateTime.UtcNow;
                state.LastBatchMilliseconds = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                state.StateId = 1;
                state.Error = null;
                await states.UpdateAsync(state, token).ConfigureAwait(false);
                batches++;

                if (cases.Count < batchSize)
                {
                    break;
                }
            }

            return batches;
        }

        private static async Task<List<GraphBuildDefinition>> ActiveDefinitionsAsync(DbContext dbContext, int tenant,
            CancellationToken token)
        {
            return (await new GraphRegistryEdgeRepository(dbContext, tenant).GetActiveForTenantAsync(token)
                    .ConfigureAwait(false))
                .Where(d => d.KindId is 1 or 2 or 3 && d.FromKey != null && d.ToKey != null)
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0))
                .ToList();
        }

        public async Task<(int Batches, long Events)> BuildTenantAsync(DbContext dbContext, int tenant,
            IReadOnlyList<GraphBuildDefinition> definitions, CancellationToken token = default)
        {
            var states = new GraphBuildStateRepository(dbContext);
            var byGuid = new Dictionary<Guid, GraphBuildState>();

            foreach (var definition in definitions)
            {
                byGuid[definition.Guid] = await states.GetOrCreateAsync(tenant, definition.Guid, token)
                    .ConfigureAwait(false);
            }

            var start = await new GetGraphBuildStartQuery(dbContext)
                .ExecuteAsync(DateTime.UtcNow.AddDays(-retentionDays), token).ConfigureAwait(false);

            foreach (var state in byGuid.Values.Where(s => s.LastArchiveKeyId == 0 && start > 0))
            {
                state.LastArchiveKeyId = start;
            }

            var groups = byGuid.Values.GroupBy(s => s.LastArchiveKeyId).OrderBy(g => g.Key).ToList();
            var batches = 0;
            long events = 0;

            foreach (var group in groups)
            {
                var groupDefinitions = definitions.Where(d => group.Any(s => s.GraphRegistryEdgeGuid == d.Guid))
                    .ToList();
                var watermark = group.Key;

                while (batches < maximumBatchesPerCycle)
                {
                    token.ThrowIfCancellationRequested();
                    var started = DateTime.UtcNow;
                    var window = await new GetGraphBuildWindowQuery(dbContext).ExecuteAsync(watermark, batchSize, token)
                        .ConfigureAwait(false);

                    if (window.Count == 0)
                    {
                        break;
                    }

                    var full = window.Count == batchSize;
                    var lastGuid = window[^1].EntityAnalysisModelInstanceEntryGuid;
                    var included = full ? window.Where(r => r.EntityAnalysisModelInstanceEntryGuid != lastGuid).ToList() : window;

                    if (included.Count == 0)
                    {
                        included = window;
                    }

                    var guids = included.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Distinct().ToArray();
                    var pairs = await new GetGraphBuildPairsQuery(dbContext).ExecuteAsync(tenant,
                        groupDefinitions.Select(d => d.Guid).ToArray(), guids, token).ConfigureAwait(false);
                    var applied = await ApplyAsync(dbContext, tenant, groupDefinitions, pairs, token)
                        .ConfigureAwait(false);
                    watermark = included.Max(r => r.Id);
                    events += applied.Events;
                    batches++;

                    foreach (var state in group)
                    {
                        state.LastArchiveKeyId = watermark;
                        state.Events += applied.Events;
                        state.Edges += applied.Edges;
                        state.Nodes += applied.Nodes;
                        state.LastRunDate = DateTime.UtcNow;
                        state.LastBatchMilliseconds = (int)(DateTime.UtcNow - started).TotalMilliseconds;
                        state.StateId = 1;
                        state.Error = null;
                        await states.UpdateAsync(state, token).ConfigureAwait(false);
                    }

                    if (!full)
                    {
                        break;
                    }
                }
            }

            return (batches, events);
        }

        public async Task<(long Events, int Edges, int Nodes)> ApplyAsync(DbContext dbContext, int tenant,
            IReadOnlyList<GraphBuildDefinition> definitions, IReadOnlyList<GetGraphBuildPairsQueryDto> pairs,
            CancellationToken token = default)
        {
            var rows = pairs.Where(p => !string.IsNullOrEmpty(p.FromValue) && !string.IsNullOrEmpty(p.ToValue)
                                        && !(p.FromKey == p.ToKey && p.FromValue == p.ToValue))
                .GroupBy(p => (p.Definition, p.ArchiveId, p.FromValue, p.ToValue))
                .Select(g => g.First())
                .ToList();

            foreach (var row in rows)
            {
                row.OccurredDate = Milliseconds(row.OccurredDate);
            }

            if (rows.Count > 0)
            {
                var applied = (await new GraphEdgeEventRepository(dbContext)
                        .GetByEntryGuidsAsync(rows.Select(r => r.EntityAnalysisModelInstanceEntryGuid).Distinct().ToList(),
                            token).ConfigureAwait(false))
                    .Where(e => e.Retracted == 0)
                    .Select(e => (e.EntityAnalysisModelInstanceEntryGuid, e.ArchiveKeyVersion ?? 1))
                    .ToHashSet();
                rows = rows.Where(r => !applied.Contains((r.EntityAnalysisModelInstanceEntryGuid, r.KeyVersion ?? 1))).ToList();
            }

            if (rows.Count == 0)
            {
                return (0, 0, 0);
            }

            var definitionsByGuid = definitions.ToDictionary(d => d.Guid);
            var touches = rows.SelectMany(r => new[]
                {
                    (r.FromKey, r.FromValue, r.OccurredDate), (r.ToKey, r.ToValue, r.OccurredDate)
                })
                .GroupBy(t => (t.Item1, t.Item2))
                .Select(g => new GraphNodeTouch(g.Key.Item1, g.Key.Item2, g.Min(t => t.OccurredDate),
                    g.Max(t => t.OccurredDate)))
                .ToList();
            var nodeIds = await new GraphNodeRepository(dbContext).UpsertManyAsync(tenant, touches, token)
                .ConfigureAwait(false);
            var deltas = new List<GraphEdgeDelta>();
            var eventsByEdge = new Dictionary<(Guid, long, long), List<GetGraphBuildPairsQueryDto>>();

            foreach (var group in rows.GroupBy(r => (r.Definition, From: nodeIds[(r.FromKey, r.FromValue)],
                         To: nodeIds[(r.ToKey, r.ToValue)])))
            {
                var definition = definitionsByGuid[group.Key.Definition];
                var ordered = group.OrderBy(r => r.OccurredDate).ToList();
                var at = ordered[^1].OccurredDate;
                var folded = EdgeWeight.Fold(
                    ordered.Select(r => (new DateTimeOffset(DateTime.SpecifyKind(r.OccurredDate, DateTimeKind.Utc)),
                        definition.KindId == 2 ? r.Amount ?? 0 : 0)),
                    definition.HalfLifeDays, referenceAmount,
                    new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Utc)));
                deltas.Add(new GraphEdgeDelta(definition.Guid, definition.KindId, definition.Restricted, group.Key.From,
                    group.Key.To, ordered.Count, definition.KindId == 2 ? ordered.Sum(r => r.Amount ?? 0) : 0,
                    ordered[0].OccurredDate, at, folded.DecayedCount, folded.DecayedAmount, at,
                    definition.HalfLifeDays, definition.KindPrior));
                eventsByEdge[group.Key] = ordered;
            }

            var edgeIds = await new GraphEdgeRepository(dbContext).UpsertManyAsync(tenant, deltas, token)
                .ConfigureAwait(false);
            var events = new List<GraphEdgeEvent>();

            foreach (var (key, list) in eventsByEdge)
            {
                var edgeId = edgeIds[key];
                var kind = definitionsByGuid[key.Item1].KindId;

                events.AddRange(list.Select(r => new GraphEdgeEvent
                {
                    TenantRegistryId = tenant,
                    GraphEdgeId = edgeId,
                    ArchiveId = r.ArchiveId,
                    EntityAnalysisModelInstanceEntryGuid = r.EntityAnalysisModelInstanceEntryGuid,
                    ArchiveKeyVersion = r.KeyVersion ?? 1,
                    OccurredDate = r.OccurredDate,
                    Amount = kind == 2 ? r.Amount : null,
                    Retracted = 0
                }));
            }

            await new GraphEdgeEventRepository(dbContext).BulkCopyAsync(events, token).ConfigureAwait(false);

            return (events.Count, deltas.Count, touches.Count);
        }

        public async Task<int> ReprocessTenantAsync(DbContext dbContext, int tenant,
            IReadOnlyList<GraphBuildDefinition> definitions, CancellationToken token = default)
        {
            var entries = await new GetGraphReprocessedEntriesQuery(dbContext).ExecuteAsync(tenant, ReprocessLimit, token)
                .ConfigureAwait(false);

            if (entries.Count == 0)
            {
                return 0;
            }

            var guids = entries.Select(e => e.EntityAnalysisModelInstanceEntryGuid).ToArray();
            var eventRepository = new GraphEdgeEventRepository(dbContext);
            var edgeRepository = new GraphEdgeRepository(dbContext);
            var stale = await eventRepository.GetByEntryGuidsAsync(guids, token).ConfigureAwait(false);
            await eventRepository.RetractAsync(stale.Select(e => e.Id).ToList(), token).ConfigureAwait(false);
            var definitionsByGuid = definitions.ToDictionary(d => d.Guid);

            foreach (var edgeId in stale.Select(e => e.GraphEdgeId).Distinct())
            {
                var edge = await edgeRepository.GetByIdAsync(tenant, edgeId, token).ConfigureAwait(false);

                if (edge == null || !definitionsByGuid.TryGetValue(edge.GraphRegistryEdgeGuid, out var definition))
                {
                    continue;
                }

                var remaining = await eventRepository.GetByEdgeAsync(edgeId, RecomputeEventLimit, token)
                    .ConfigureAwait(false);
                var at = remaining.Count == 0 ? edge.DecayedAt : remaining.Max(e => e.OccurredDate);
                var folded = EdgeWeight.Fold(
                    remaining.Select(e => (new DateTimeOffset(DateTime.SpecifyKind(e.OccurredDate, DateTimeKind.Utc)),
                        e.Amount ?? 0)),
                    definition.HalfLifeDays, referenceAmount,
                    new DateTimeOffset(DateTime.SpecifyKind(at, DateTimeKind.Utc)));
                await edgeRepository.ApplyRetractionAsync(edgeId, remaining.Count, remaining.Sum(e => e.Amount ?? 0),
                    folded.DecayedCount, folded.DecayedAmount, at, definition.KindPrior, token).ConfigureAwait(false);
            }

            var pairs = await new GetGraphBuildPairsQuery(dbContext).ExecuteAsync(tenant,
                definitions.Select(d => d.Guid).ToArray(), guids, token).ConfigureAwait(false);
            await ApplyAsync(dbContext, tenant, definitions, pairs, token).ConfigureAwait(false);

            return entries.Count;
        }

        public static DateTime Milliseconds(DateTime value)
        {
            return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);
        }

        public static async Task<bool> IndexIsValidAsync(DataConnection dbContext, CancellationToken token)
        {
            var rows = await dbContext.QueryToListAsync<int>(
                    "SELECT 1 FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid WHERE c.relname = @name AND i.indisvalid",
                    token, new DataParameter("name", ArchiveKeyIndexName, DataType.Text))
                .ConfigureAwait(false);

            return rows.Count > 0;
        }
    }
}
