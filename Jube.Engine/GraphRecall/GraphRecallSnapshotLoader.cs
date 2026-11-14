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
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Query.GetGraphRecallDefinitionsQuery;
using Jube.Data.Query.GetGraphRecallEdgesQuery;
using Jube.Data.Query.GetGraphRecallSnapshotQuery;
using log4net;

namespace Jube.Engine.GraphRecall
{
    public static class GraphRecallSnapshotLoader
    {
        public const int PageSize = 50000;

        public static async Task<GraphRecallSnapshot> LoadAsync(DbContext dbContext, int tenantRegistryId,
            GraphRecallLoaderOptions options, ILog log, CancellationToken token = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var startBytes = GC.GetTotalAllocatedBytes(true);

            var builder = new GraphRecallSnapshotBuilder(tenantRegistryId, options.HashKeys, options.MaximumNodes,
                options.MaximumEdges, options.DistanceCap);

            await LoadNodesAsync(dbContext, tenantRegistryId, true, builder, token).ConfigureAwait(false);

            await LoadNodesAsync(dbContext, tenantRegistryId, false, builder, token).ConfigureAwait(false);

            await LoadEdgesAsync(dbContext, tenantRegistryId, builder, token).ConfigureAwait(false);
            await LoadDefinitionsAsync(dbContext, tenantRegistryId, builder, token).ConfigureAwait(false);

            stopwatch.Stop();

            var snapshot = builder.Build(stopwatch.Elapsed, Math.Max(0, GC.GetTotalAllocatedBytes(true) - startBytes));

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Graph Recall Loader: tenant {tenantRegistryId} loaded {snapshot.NodeCount} nodes, {snapshot.EdgeCount} edges, {snapshot.Definitions.Count} edge definitions and {snapshot.Tags.Count} tags in {snapshot.LoadTime.TotalMilliseconds:F0} ms allocating {snapshot.Bytes} bytes ({BytesPerNode(snapshot):F0} per node){(snapshot.Truncated ? ", truncated at the node cap" : string.Empty)}{(snapshot.EdgesTruncated ? ", edges truncated at the edge cap" : string.Empty)}.");
            }

            return snapshot;
        }

        public static double BytesPerNode(GraphRecallSnapshot snapshot)
        {
            return snapshot.NodeCount == 0 ? 0 : (double)snapshot.Bytes / snapshot.NodeCount;
        }

        private static async Task LoadNodesAsync(DbContext dbContext, int tenantRegistryId, bool priority,
            GraphRecallSnapshotBuilder builder, CancellationToken token)
        {
            var query = new GetGraphRecallSnapshotQuery(dbContext);
            long after = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var page = await query.ExecuteAsync(tenantRegistryId, after, priority, PageSize, token)
                    .ConfigureAwait(false);

                foreach (var row in page)
                {
                    if (!builder.TryAddNode(row.Key, row.Value, ToNode(row)))
                    {
                        return;
                    }

                    after = row.Id;
                }

                if (page.Count < PageSize)
                {
                    return;
                }
            }
        }

        private static async Task LoadEdgesAsync(DbContext dbContext, int tenantRegistryId,
            GraphRecallSnapshotBuilder builder, CancellationToken token)
        {
            var query = new GetGraphRecallEdgesQuery(dbContext);
            long after = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                var page = await query.ExecuteAsync(tenantRegistryId, after, PageSize, token).ConfigureAwait(false);

                foreach (var row in page)
                {
                    builder.TryAddEdge(row.FromNodeId, row.ToNodeId, row.RegistryGuid,
                        new GraphRecallEdge(row.Count, row.AmountSum, row.FirstSeen, row.LastSeen,
                            (byte)Math.Clamp(row.KindId, byte.MinValue, byte.MaxValue)));

                    after = row.Id;
                }

                if (page.Count < PageSize)
                {
                    return;
                }
            }
        }

        private static async Task LoadDefinitionsAsync(DbContext dbContext, int tenantRegistryId,
            GraphRecallSnapshotBuilder builder, CancellationToken token)
        {
            foreach (var row in await new GetGraphRecallDefinitionsQuery(dbContext)
                         .ExecuteAsync(tenantRegistryId, token).ConfigureAwait(false))
            {
                builder.AddDefinition(new GraphRecallDefinition(row.RegistryGuid, row.FromNodeKey, row.ToNodeKey));
            }
        }

        public static GraphRecallNode ToNode(GetGraphRecallSnapshotQueryDto row)
        {
            return new GraphRecallNode
            {
                Id = row.Id,
                Hub = row.Hub == 1,
                Degree = row.Degree,
                DegreeMoney = row.DegreeMoney,
                DegreeIdentity = row.DegreeIdentity,
                HubNeighbourCount = row.HubNeighbourCount,
                CommunitySize = row.CommunitySize ?? 0,
                CommunityId = row.CommunityId ?? GraphRecallNode.Unknown,
                CoreNumber = (short)Math.Clamp(row.CoreNumber ?? 0, short.MinValue, short.MaxValue),
                PageRank = (float)(row.PageRank ?? 0),
                HubScore = (float)(row.HubScore ?? 0),
                AuthorityScore = (float)(row.AuthorityScore ?? 0),
                ClusteringCoefficient = (float)(row.ClusteringCoefficient ?? 0),
                PropagationScore = (float)(row.PropagationScore ?? 0),
                CommunityConfirmedRate = (float)(row.CommunityConfirmedRate ?? 0),
                CommunityTenantRateRatio = (float)(row.CommunityTenantRateRatio ?? 0),
                DistanceToConfirmed = row.DistanceToConfirmed ?? GraphRecallNode.Unknown,
                DistanceToNotConfirmed = row.DistanceToNotConfirmed ?? GraphRecallNode.Unknown,
                ConfirmedWithin1 = row.ConfirmedWithin1 ?? 0,
                ConfirmedWithin2 = row.ConfirmedWithin2 ?? 0,
                ConfirmedWithin3 = row.ConfirmedWithin3 ?? 0,
                PatternKinds = row.PatternKinds,
                Tags = ParseTags(row.Tags)
            };
        }

        public static GraphRecallTag[] ParseTags(string tags)
        {
            if (string.IsNullOrEmpty(tags))
            {
                return null;
            }

            var entries = tags.Split(GetGraphRecallSnapshotQuery.TagSeparator);
            var parsed = new GraphRecallTag[entries.Length];

            for (var i = 0; i < entries.Length; i++)
            {
                var fields = entries[i].Split(GetGraphRecallSnapshotQuery.TagFieldSeparator);

                parsed[i] = new GraphRecallTag(string.Intern(fields[0]),
                    short.Parse(fields[1], CultureInfo.InvariantCulture),
                    int.Parse(fields[2], CultureInfo.InvariantCulture),
                    int.Parse(fields[3], CultureInfo.InvariantCulture),
                    int.Parse(fields[4], CultureInfo.InvariantCulture));
            }

            return parsed;
        }
    }
}
