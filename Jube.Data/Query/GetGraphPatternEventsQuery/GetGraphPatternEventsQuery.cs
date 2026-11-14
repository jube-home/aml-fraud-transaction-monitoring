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

namespace Jube.Data.Query.GetGraphPatternEventsQuery
{
    public class GetGraphPatternEventsQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT e."FromNodeId", e."ToNodeId", ev."OccurredDate", COALESCE(ev."Amount", 0) AS "Amount", ev."ArchiveId"
            FROM "GraphEdgeEvent" ev
            JOIN "GraphEdge" e ON e."Id" = ev."GraphEdgeId"
            WHERE ev."TenantRegistryId" = @tenant AND ev."Retracted" = 0 AND e."KindId" = 2
              AND e."FromHub" = 0 AND e."ToHub" = 0 AND e."Restricted" = 0
              AND ev."OccurredDate" >= @from AND ev."OccurredDate" <= @to
            ORDER BY ev."OccurredDate" DESC, ev."ArchiveId" DESC
            LIMIT @limit
            """;

        public Task<List<GetGraphPatternEventsQueryDto>> ExecuteAsync(int tenantRegistryId, DateTime from, DateTime to,
            int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphPatternEventsQueryDto>(Sql, token,
                new DataParameter("tenant", tenantRegistryId, DataType.Int32),
                new DataParameter("from", from, DataType.DateTime2),
                new DataParameter("to", to, DataType.DateTime2),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
