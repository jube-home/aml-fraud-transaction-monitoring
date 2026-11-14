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
using Jube.Data.Query.GetGraphAccessibleCasesQuery;
using Jube.Data.Repository;
using Jube.Service.Query.Graph.Jobs;

namespace Jube.Service.Query.Graph
{
    internal sealed class GraphViewerNodes(DbContext dbContext, int tenantRegistryId, string userName)
    {
        private readonly GraphNodeRepository nodes = new(dbContext);

        public async Task<GraphNode?> GetAsync(int tenant, string key, string value, CancellationToken token = default)
        {
            var node = await nodes.GetAsync(tenant, key, value, token).ConfigureAwait(false);

            return await VisibleOrNullAsync(node, token).ConfigureAwait(false);
        }

        public async Task<GraphNode?> GetByIdAsync(int tenant, long id, CancellationToken token = default)
        {
            var node = await nodes.GetByIdAsync(tenant, id, token).ConfigureAwait(false);

            return await VisibleOrNullAsync(node, token).ConfigureAwait(false);
        }

        public async Task<List<GraphNode>> GetByIdsAsync(int tenant, IReadOnlyCollection<long> ids,
            CancellationToken token = default)
        {
            var rows = await nodes.GetByIdsAsync(tenant, ids, token).ConfigureAwait(false);

            return await VisibleAsync(dbContext, tenant, userName, rows, token).ConfigureAwait(false);
        }

        internal static async Task<List<GraphNode>> VisibleAsync(DbContext context, int tenant, string user,
            IReadOnlyList<GraphNode> rows, CancellationToken token)
        {
            var caseIds = new HashSet<int>();

            foreach (var row in rows.Where(r => r.Key == GraphCaseEdge.NodeKey))
            {
                if (int.TryParse(row.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                {
                    caseIds.Add(id);
                }
            }

            if (!rows.Any(r => r.Key == GraphCaseEdge.NodeKey))
            {
                return rows.ToList();
            }

            var allowed = caseIds.Count == 0
                ? []
                : (await new GetGraphAccessibleCasesQuery(context)
                    .ExecuteAsync(tenant, user, caseIds.ToArray(), token).ConfigureAwait(false)).ToHashSet();

            return rows.Where(r => r.Key != GraphCaseEdge.NodeKey
                                   || (int.TryParse(r.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
                                       && allowed.Contains(id))).ToList();
        }

        private async Task<GraphNode?> VisibleOrNullAsync(GraphNode? node, CancellationToken token)
        {
            if (node == null || node.Key != GraphCaseEdge.NodeKey)
            {
                return node;
            }

            return (await VisibleAsync(dbContext, tenantRegistryId, userName, [node], token).ConfigureAwait(false))
                .FirstOrDefault();
        }
    }
}
