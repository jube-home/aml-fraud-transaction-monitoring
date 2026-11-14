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
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Context;
    using LinqToDB;
    using LinqToDB.Async;
    using LinqToDB.Data;
    using Models;
    using Poco;

    public class GraphNodeRepository(DbContext dbContext)
    {
        private const string UpsertSql = """
            WITH incoming AS (
                SELECT k."Key", k."Value", k."FirstSeen", k."LastSeen"
                FROM unnest(@keys::text[], @values::text[], @firsts::timestamp[], @lasts::timestamp[])
                    AS k("Key", "Value", "FirstSeen", "LastSeen")
            ), upserted AS (
                INSERT INTO "GraphNode" ("TenantRegistryId", "Key", "Value", "FirstSeen", "LastSeen")
                SELECT @tenant, i."Key", i."Value", i."FirstSeen", i."LastSeen" FROM incoming i
                ON CONFLICT ("TenantRegistryId", "Key", "Value") DO UPDATE SET
                    "FirstSeen" = LEAST("GraphNode"."FirstSeen", EXCLUDED."FirstSeen"),
                    "LastSeen" = GREATEST("GraphNode"."LastSeen", EXCLUDED."LastSeen")
                RETURNING "Id", "Key", "Value"
            )
            SELECT "Id", "Key", "Value" FROM upserted
            """;

        public async Task<Dictionary<(string Key, string Value), long>> UpsertManyAsync(int tenantRegistryId,
            IReadOnlyList<GraphNodeTouch> touches, CancellationToken token = default)
        {
            var result = new Dictionary<(string Key, string Value), long>();

            if (touches.Count == 0)
            {
                return result;
            }

            var rows = await dbContext.QueryToListAsync<GraphNodeKeyRow>(UpsertSql, token,
                    new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                    new DataParameter("keys", touches.Select(t => t.Key).ToArray()),
                    new DataParameter("values", touches.Select(t => t.Value).ToArray()),
                    new DataParameter("firsts", touches.Select(t => t.FirstSeen).ToArray()),
                    new DataParameter("lasts", touches.Select(t => t.LastSeen).ToArray()))
                .ConfigureAwait(false);

            foreach (var row in rows)
            {
                result[(row.Key, row.Value)] = row.Id;
            }

            return result;
        }

        public Task<GraphNode> GetAsync(int tenantRegistryId, string key, string value, CancellationToken token = default)
        {
            return dbContext.GraphNode.FirstOrDefaultAsync(n => n.TenantRegistryId == tenantRegistryId
                                                                && n.Key == key && n.Value == value, token);
        }

        public Task<GraphNode> GetByIdAsync(int tenantRegistryId, long id, CancellationToken token = default)
        {
            return dbContext.GraphNode.FirstOrDefaultAsync(n => n.TenantRegistryId == tenantRegistryId && n.Id == id,
                token);
        }

        public Task<List<GraphNode>> GetByIdsAsync(int tenantRegistryId, IReadOnlyCollection<long> ids,
            CancellationToken token = default)
        {
            return dbContext.GraphNode.Where(n => n.TenantRegistryId == tenantRegistryId && ids.Contains(n.Id))
                .ToListAsync(token);
        }

        public Task<int> SetHubOverrideAsync(int tenantRegistryId, long id, byte overrideId,
            CancellationToken token = default)
        {
            return dbContext.GraphNode.Where(n => n.TenantRegistryId == tenantRegistryId && n.Id == id)
                .Set(n => n.HubOverrideId, overrideId)
                .Set(n => n.Hub, n => overrideId == 2 ? (byte)1 : overrideId == 1 ? (byte)0 : n.Hub)
                .UpdateAsync(token);
        }

        public Task<List<GraphNode>> GetHubsAsync(int tenantRegistryId, int limit, CancellationToken token = default)
        {
            return dbContext.GraphNode.Where(n => n.TenantRegistryId == tenantRegistryId && n.Hub == 1)
                .OrderByDescending(n => n.Degree).ThenBy(n => n.Id).Take(limit).ToListAsync(token);
        }
    }
}
