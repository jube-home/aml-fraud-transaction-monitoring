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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphEdgeListForAlgorithmsQuery
{
    public class GetGraphEdgeListForAlgorithmsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT e."FromNodeId", e."ToNodeId", e."Weight", e."KindId"
            FROM "GraphEdge" e
            WHERE e."TenantRegistryId" = @tenant AND e."FromHub" = 0 AND e."ToHub" = 0 AND e."Restricted" = 0
            ORDER BY e."Id"
            LIMIT @limit
            """;

        public Task<List<GetGraphEdgeListForAlgorithmsQueryDto>> ExecuteAsync(int tenantRegistryId, int limit,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphEdgeListForAlgorithmsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
