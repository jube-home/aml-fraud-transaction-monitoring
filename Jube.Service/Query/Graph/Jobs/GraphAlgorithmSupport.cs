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
using Jube.Data.Query.GetGraphEdgeListForAlgorithmsQuery;
using Jube.Graph.Model;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

namespace Jube.Service.Query.Graph.Jobs
{
    internal static class GraphAlgorithmSupport
    {
        private const string LockSql = """SELECT pg_try_advisory_lock(hashtext(@name))""";
        private const string UnlockSql = """SELECT pg_advisory_unlock(hashtext(@name))""";

        public static Task<List<int>> TenantsAsync(DbContext dbContext, CancellationToken token)
        {
            return dbContext.GraphRegistry
                .Where(r => r.Active == 1 && (r.Deleted == null || r.Deleted == 0) && r.TenantRegistryId != null)
                .Select(r => r.TenantRegistryId!.Value).Distinct().OrderBy(t => t).ToListAsync(token);
        }

        public static async Task<bool> TryLockAsync(DbContext dbContext, string cycle, int tenant, CancellationToken token)
        {
            return await dbContext.ExecuteAsync<bool>(LockSql, token,
                new DataParameter("name", $"Graph:{cycle}:{tenant}", DataType.Text)).ConfigureAwait(false);
        }

        public static Task UnlockAsync(DbContext dbContext, string cycle, int tenant, CancellationToken token)
        {
            return dbContext.ExecuteAsync<bool>(UnlockSql, token,
                new DataParameter("name", $"Graph:{cycle}:{tenant}", DataType.Text));
        }

        public static async Task<(CompressedGraph Undirected, CompressedGraph Directed, List<GetGraphEdgeListForAlgorithmsQueryDto> Edges)>
            LoadAsync(DbContext dbContext, int tenant, int maximumEdges, CancellationToken token)
        {
            var edges = await new GetGraphEdgeListForAlgorithmsQuery(dbContext).ExecuteAsync(tenant, maximumEdges, token)
                .ConfigureAwait(false);
            var tuples = edges.Select(e => (e.FromNodeId, e.ToNodeId, Math.Max(e.Weight, 1e-6f), (byte)e.KindId)).ToList();

            return (CompressedGraphBuilder.FromEdges(tuples, true), CompressedGraphBuilder.FromEdges(tuples, false), edges);
        }
    }
}
