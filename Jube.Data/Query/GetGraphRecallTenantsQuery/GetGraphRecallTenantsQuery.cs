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
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphRecallTenantsQuery
{
    public class GetGraphRecallTenantsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT DISTINCT r."TenantRegistryId"
            FROM "GraphRegistry" r
            WHERE r."Active" = 1 AND COALESCE(r."Deleted", 0) = 0 AND r."TenantRegistryId" IS NOT NULL
            ORDER BY r."TenantRegistryId"
            """;

        public Task<List<GetGraphRecallTenantsQueryDto>> ExecuteAsync(CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphRecallTenantsQueryDto>(Sql, token);
        }
    }
}
