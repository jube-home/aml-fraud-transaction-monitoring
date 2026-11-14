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

namespace Jube.Data.Query.GetGraphHubThresholdsQuery
{
    public class GetGraphHubThresholdsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT c."Def" AS "Definition", percentile_cont(@percentile) WITHIN GROUP (ORDER BY c."Degree") AS "Percentile"
            FROM (
                SELECT e."GraphRegistryEdgeGuid" AS "Def", e."ToNodeId", COUNT(DISTINCT e."FromNodeId") AS "Degree"
                FROM "GraphEdge" e
                WHERE e."TenantRegistryId" = @tenant AND e."Restricted" = 0
                GROUP BY e."GraphRegistryEdgeGuid", e."ToNodeId") c
            GROUP BY c."Def"
            """;

        public Task<List<GetGraphHubThresholdsQueryDto>> ExecuteAsync(int tenantRegistryId, double percentile,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphHubThresholdsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("percentile", percentile, DataType.Double));
        }
    }
}
