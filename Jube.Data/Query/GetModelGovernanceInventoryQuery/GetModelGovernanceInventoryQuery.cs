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

namespace Jube.Data.Query.GetModelGovernanceInventoryQuery
{
    public class GetModelGovernanceInventoryQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT 1::smallint AS "KindId", m."Guid" AS "ModelGuid", m."Name" AS "Name"
            FROM "EntityAnalysisModel" m
            WHERE m."TenantRegistryId" = @tenant AND (m."Deleted" IS NULL OR m."Deleted" = 0)
            UNION ALL
            SELECT 2::smallint, e."Guid", e."Name"
            FROM "ExhaustiveSearchInstance" e
            JOIN "EntityAnalysisModel" m ON m."Id" = e."EntityAnalysisModelId"
            WHERE m."TenantRegistryId" = @tenant AND (m."Deleted" IS NULL OR m."Deleted" = 0)
              AND (e."Deleted" IS NULL OR e."Deleted" = 0) AND e."Guid" IS NOT NULL
            ORDER BY 1, 3
            """;

        public Task<List<GetModelGovernanceInventoryQueryDto>> ExecuteAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetModelGovernanceInventoryQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId));
        }
    }
}
