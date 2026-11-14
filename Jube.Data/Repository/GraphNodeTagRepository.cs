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
    using Poco;

    public class GraphNodeTagRepository(DbContext dbContext)
    {
        private const string ReplaceSql = """
            DELETE FROM "GraphNodeTag" WHERE "TenantRegistryId" = @tenant AND "Tag" = @tag
            """;

        private const string InsertSql = """
            INSERT INTO "GraphNodeTag" ("TenantRegistryId", "GraphNodeId", "Tag", "Distance", "Within1", "Within2", "Within3", "ComputedDate")
            SELECT @tenant, u."Node", @tag, u."Distance", u."Within1", u."Within2", u."Within3", @now
            FROM unnest(@nodes::bigint[], @distances::smallint[], @within1::integer[], @within2::integer[], @within3::integer[])
                 AS u("Node", "Distance", "Within1", "Within2", "Within3")
            """;

        public async Task ReplaceTagAsync(int tenantRegistryId, string tag, IReadOnlyList<GraphNodeTag> rows,
            CancellationToken token = default)
        {
            var tenant = new DataParameter("tenant", tenantRegistryId, DataType.Int32);
            var name = new DataParameter("tag", tag, DataType.Text);
            await dbContext.ExecuteAsync(ReplaceSql, token, tenant, name).ConfigureAwait(false);
            const int chunk = 50000;

            for (var start = 0; start < rows.Count; start += chunk)
            {
                var slice = rows.Skip(start).Take(chunk).ToList();
                await dbContext.ExecuteAsync(InsertSql, token, tenant, name,
                    new DataParameter("now", DateTime.UtcNow, DataType.DateTime2),
                    new DataParameter("nodes", slice.Select(r => r.GraphNodeId).ToArray()),
                    new DataParameter("distances", slice.Select(r => r.Distance).ToArray()),
                    new DataParameter("within1", slice.Select(r => r.Within1).ToArray()),
                    new DataParameter("within2", slice.Select(r => r.Within2).ToArray()),
                    new DataParameter("within3", slice.Select(r => r.Within3).ToArray())).ConfigureAwait(false);
            }
        }

        public Task<int> DeleteTagsNotInAsync(int tenantRegistryId, IReadOnlyCollection<string> tags,
            CancellationToken token = default)
        {
            return dbContext.GraphNodeTag.Where(t => t.TenantRegistryId == tenantRegistryId && !tags.Contains(t.Tag))
                .DeleteAsync(token);
        }

        public Task<List<GraphNodeTag>> GetByNodeIdsAsync(int tenantRegistryId, IReadOnlyCollection<long> nodeIds,
            CancellationToken token = default)
        {
            return dbContext.GraphNodeTag.Where(t => t.TenantRegistryId == tenantRegistryId && nodeIds.Contains(t.GraphNodeId))
                .OrderBy(t => t.Distance).ThenBy(t => t.Tag).ToListAsync(token);
        }
    }
}
