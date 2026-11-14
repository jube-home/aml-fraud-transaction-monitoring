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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Query.GetGraphEntryTenantsQuery
{
    public class GetGraphEntryTenantsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT a."EntityAnalysisModelInstanceEntryGuid", m."TenantRegistryId"
            FROM unnest(@guids::uuid[]) AS g("Guid")
            JOIN "Archive" a ON a."EntityAnalysisModelInstanceEntryGuid" = g."Guid"
            JOIN "EntityAnalysisModel" m ON m."Id" = a."EntityAnalysisModelId"
            WHERE m."TenantRegistryId" IS NOT NULL
            """;

        public Task<List<GetGraphEntryTenantsQueryDto>> ExecuteAsync(Guid[] entryGuids, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphEntryTenantsQueryDto>(Sql, token, new DataParameter("guids", entryGuids));
        }
    }
}
