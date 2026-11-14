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

namespace Jube.Data.Query.GetGraphRecallDefinitionsQuery
{
    public class GetGraphRecallDefinitionsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT r."Guid" AS "RegistryGuid",
                   COALESCE(d."FromNodeKey", d."FromKey") AS "FromNodeKey",
                   COALESCE(d."ToNodeKey", d."ToKey") AS "ToNodeKey"
            FROM "GraphRegistryEdge" d
            JOIN "GraphRegistry" r ON r."Id" = d."GraphRegistryId"
            WHERE r."TenantRegistryId" = @tenant AND r."Active" = 1 AND COALESCE(r."Deleted", 0) = 0
              AND d."Active" = 1 AND COALESCE(d."Deleted", 0) = 0 AND d."KindId" <> 5
            ORDER BY d."Id"
            """;

        public Task<List<GetGraphRecallDefinitionsQueryDto>> ExecuteAsync(int tenantRegistryId,
            CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphRecallDefinitionsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32));
        }
    }
}
