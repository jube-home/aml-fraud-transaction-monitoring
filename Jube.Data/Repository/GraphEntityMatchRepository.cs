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
    using Dto.Options;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Npgsql;
    using Poco;

    public class GraphEntityMatchRepository(DbContext dbContext)
    {
        public Task<List<GraphEntityMatch>> ListAsync(int tenantRegistryId, short? decisionId, int take,
            CancellationToken token = default)
        {
            return dbContext.GraphEntityMatch
                .Where(w => w.TenantRegistryId == tenantRegistryId && (decisionId == null || w.DecisionId == decisionId))
                .OrderByDescending(o => o.Posterior).ThenBy(o => o.Id).Take(take).ToListAsync(token);
        }

        public Task<List<GraphEntityMatch>> ListForNodeAsync(int tenantRegistryId, long nodeId,
            CancellationToken token = default)
        {
            return dbContext.GraphEntityMatch
                .Where(w => w.TenantRegistryId == tenantRegistryId && (w.NodeIdA == nodeId || w.NodeIdB == nodeId))
                .OrderByDescending(o => o.Posterior).ToListAsync(token);
        }

        public Task<GraphEntityMatch> GetAsync(int tenantRegistryId, long id, CancellationToken token = default)
        {
            return dbContext.GraphEntityMatch.FirstOrDefaultAsync(w => w.TenantRegistryId == tenantRegistryId
                                                                       && w.Id == id, token);
        }

        public async Task<bool> InsertIfNewAsync(GraphEntityMatch model, CancellationToken token = default)
        {
            model.CreatedDate = DateTime.UtcNow;

            try
            {
                await dbContext.InsertAsync(model, token: token).ConfigureAwait(false);

                return true;
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                return false;
            }
        }

        public Task<int> DecideAsync(int tenantRegistryId, long id, short decision, string user, string note,
            CancellationToken token = default)
        {
            return dbContext.GraphEntityMatch.Where(w => w.TenantRegistryId == tenantRegistryId && w.Id == id)
                .Set(s => s.DecisionId, decision).Set(s => s.DecidedUser, user)
                .Set(s => s.DecidedDate, DateTime.UtcNow).Set(s => s.DecisionNote, note)
                .UpdateAsync(token);
        }

        public async Task<int> RecomputeResolutionAsync(int tenantRegistryId, CancellationToken token = default)
        {
            var confirmed = (short)GraphEntityMatchDecision.Confirmed;

            var pairs = await dbContext.GraphEntityMatch
                .Where(w => w.TenantRegistryId == tenantRegistryId && w.DecisionId == confirmed)
                .Select(s => new { s.Id, s.NodeIdA, s.NodeIdB }).ToListAsync(token).ConfigureAwait(false);

            var parent = new Dictionary<long, long>();

            long Find(long x)
            {
                if (!parent.TryGetValue(x, out var p))
                {
                    parent[x] = x;

                    return x;
                }

                return p == x ? x : parent[x] = Find(p);
            }

            foreach (var pair in pairs)
            {
                var a = Find(pair.NodeIdA);
                var b = Find(pair.NodeIdB);

                if (a != b)
                {
                    parent[Math.Max(a, b)] = Math.Min(a, b);
                }
            }

            var representative = parent.Keys.ToDictionary(k => k, Find);
            var nodes = representative.Keys.ToArray();
            var representatives = nodes.Select(n => representative[n]).ToArray();

            await dbContext.ExecuteAsync("""
                UPDATE "GraphNode" SET "ResolvedEntityId" = NULL
                WHERE "TenantRegistryId" = @tenant AND "ResolvedEntityId" IS NOT NULL AND NOT ("Id" = ANY(@nodes::bigint[]))
                """, token, new DataParameter("tenant", tenantRegistryId), new DataParameter("nodes", nodes))
                .ConfigureAwait(false);

            if (nodes.Length > 0)
            {
                await dbContext.ExecuteAsync("""
                    UPDATE "GraphNode" n SET "ResolvedEntityId" = r."Rep"
                    FROM unnest(@nodes::bigint[], @reps::bigint[]) AS r("Node", "Rep")
                    WHERE n."TenantRegistryId" = @tenant AND n."Id" = r."Node"
                      AND n."ResolvedEntityId" IS DISTINCT FROM r."Rep"
                    """, token, new DataParameter("tenant", tenantRegistryId), new DataParameter("nodes", nodes),
                    new DataParameter("reps", representatives)).ConfigureAwait(false);
            }

            if (pairs.Count > 0)
            {
                await dbContext.ExecuteAsync("""
                    UPDATE "GraphEntityMatch" m SET "ResolvedEntityId" = r."Rep"
                    FROM unnest(@ids::bigint[], @reps::bigint[]) AS r("Id", "Rep")
                    WHERE m."TenantRegistryId" = @tenant AND m."Id" = r."Id"
                      AND m."ResolvedEntityId" IS DISTINCT FROM r."Rep"
                    """, token, new DataParameter("tenant", tenantRegistryId),
                    new DataParameter("ids", pairs.Select(p => p.Id).ToArray()),
                    new DataParameter("reps", pairs.Select(p => representative[p.NodeIdA]).ToArray()))
                    .ConfigureAwait(false);
            }

            await dbContext.GraphEntityMatch.Where(w => w.TenantRegistryId == tenantRegistryId
                                                        && w.DecisionId != confirmed && w.ResolvedEntityId != null)
                .Set(s => s.ResolvedEntityId, (long?)null).UpdateAsync(token).ConfigureAwait(false);

            return nodes.Length;
        }
    }
}
