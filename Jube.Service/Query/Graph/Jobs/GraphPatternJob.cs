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
using Jube.Data.Query.GetGraphPatternEventsQuery;
using Jube.Data.Repository;
using Jube.Graph.Motifs;
using Jube.Service.Query.Graph.Jobs.Models;
using log4net;
using Newtonsoft.Json;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphPatternJob(string connectionString, ILog log, int windowDays, int maximumEvents,
        double structuringThreshold)
    {
        public const int MaximumStoredArchiveIds = 1000;
        public const int MaximumStoredMembers = 1000;
        public const int MaximumPatternsPerCycle = 5000;
        private const string Cycle = "Patterns";

        public async Task<GraphPatternResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(connectionString, log);
            var tenants = await GraphAlgorithmSupport.TenantsAsync(dbContext, token).ConfigureAwait(false);
            var events = 0;
            var patterns = 0;

            foreach (var tenant in tenants)
            {
                token.ThrowIfCancellationRequested();

                if (!await GraphAlgorithmSupport.TryLockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    var outcome = await RunTenantAsync(dbContext, tenant, DateTime.UtcNow, token).ConfigureAwait(false);
                    events += outcome.Events;
                    patterns += outcome.Patterns;
                }
                finally
                {
                    await GraphAlgorithmSupport.UnlockAsync(dbContext, Cycle, tenant, token).ConfigureAwait(false);
                }
            }

            return new GraphPatternResult(tenants.Count, events, patterns);
        }

        public async Task<(int Events, int Patterns)> RunTenantAsync(DbContext dbContext, int tenant, DateTime now,
            CancellationToken token = default)
        {
            var rows = await new GetGraphPatternEventsQuery(dbContext).ExecuteAsync(tenant, now.AddDays(-windowDays), now,
                maximumEvents, token).ConfigureAwait(false);

            if (rows.Count == 0)
            {
                return (0, 0);
            }

            var events = rows.Select(r => new GraphEvent(r.FromNodeId, r.ToNodeId,
                new DateTimeOffset(DateTime.SpecifyKind(r.OccurredDate, DateTimeKind.Utc)), r.Amount, r.ArchiveId)).ToList();
            var found = Detect(events);
            var repository = new GraphPatternRepository(dbContext);
            var written = 0;

            foreach (var instance in found.OrderByDescending(p => p.Score).ThenBy(p => p.Kind).ThenBy(p => p.AnchorNode)
                         .ThenBy(p => p.Window.From).Take(MaximumPatternsPerCycle))
            {
                token.ThrowIfCancellationRequested();
                await repository.UpsertAsync(new GraphPattern
                {
                    TenantRegistryId = tenant,
                    KindId = (byte)instance.Kind,
                    AnchorNodeId = instance.AnchorNode,
                    Score = instance.Score,
                    WindowFrom = instance.Window.From.UtcDateTime,
                    WindowTo = instance.Window.To.UtcDateTime,
                    Parameters = JsonConvert.SerializeObject(instance.Parameters),
                    ArchiveIds = instance.ArchiveIds.TakeLast(MaximumStoredArchiveIds).ToArray(),
                    MemberCount = instance.Members.Count,
                    StateId = 1,
                    FirstDetectedDate = now,
                    LastDetectedDate = now
                }, instance.Members.Take(MaximumStoredMembers).Select(m => (m.NodeId, m.Role)).ToList(), token).ConfigureAwait(false);
                written++;
            }

            return (rows.Count, written);
        }

        public List<PatternInstance> Detect(IReadOnlyList<GraphEvent> events)
        {
            var found = new List<PatternInstance>();
            found.AddRange(FanDetector.Detect(events));
            found.AddRange(GatherScatterDetector.Detect(events));
            found.AddRange(ScatterGatherDetector.Detect(events));
            found.AddRange(TemporalCycleDetector.Detect(events));
            found.AddRange(PassThroughDetector.Detect(events));
            found.AddRange(SmurfDetector.Detect(events));
            found.AddRange(StructuringWindowDetector.Detect(events,
                new StructuringWindowParameters(structuringThreshold, 0.1, TimeSpan.FromDays(7), 3)));
            found.AddRange(DenseBlockDetector.Detect(events));

            return found;
        }
    }
}
