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

namespace Jube.Data.Query.GetGraphEdgeEventTimesQuery
{
    public class GetGraphEdgeEventTimesQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT e."OccurredDate"
            FROM "GraphEdgeEvent" e
            WHERE e."GraphEdgeId" = ANY(@edges::bigint[]) AND e."Retracted" = 0
              AND e."OccurredDate" >= @from AND e."OccurredDate" <= @to
            ORDER BY e."OccurredDate"
            LIMIT @limit
            """;

        public Task<List<GetGraphEdgeEventTimesQueryDto>> ExecuteAsync(long[] edgeIds, DateTime from, DateTime to,
            int limit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphEdgeEventTimesQueryDto>(Sql, token,
                new DataParameter("edges", edgeIds),
                new DataParameter("from", from, DataType.DateTime2),
                new DataParameter("to", to, DataType.DateTime2),
                new DataParameter("limit", limit, DataType.Int32));
        }
    }
}
