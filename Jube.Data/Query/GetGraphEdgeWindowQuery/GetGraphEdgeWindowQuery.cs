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

namespace Jube.Data.Query.GetGraphEdgeWindowQuery
{
    public class GetGraphEdgeWindowQuery(DbContext dbContext)
    {
        private const string Sql = """
            SELECT e."GraphEdgeId",
                   COUNT(*)::integer AS "WindowCount",
                   COALESCE(SUM(e."Amount"), 0) AS "WindowAmount",
                   MIN(e."OccurredDate") AS "WindowFirst",
                   MAX(e."OccurredDate") AS "WindowLast",
                   ARRAY_TO_STRING((ARRAY_AGG(e."ArchiveId" ORDER BY e."OccurredDate" DESC))[1:@limit], ',') AS "ArchiveIds"
            FROM "GraphEdgeEvent" e
            WHERE e."GraphEdgeId" = ANY(@edges::bigint[]) AND e."Retracted" = 0
              AND e."OccurredDate" >= @from AND e."OccurredDate" <= @to
            GROUP BY e."GraphEdgeId"
            """;

        public Task<List<GetGraphEdgeWindowQueryDto>> ExecuteAsync(long[] edgeIds, DateTime from, DateTime to,
            int archiveIdLimit, CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetGraphEdgeWindowQueryDto>(Sql, token,
                new DataParameter("edges", edgeIds),
                new DataParameter("from", from, DataType.DateTime2),
                new DataParameter("to", to, DataType.DateTime2),
                new DataParameter("limit", archiveIdLimit, DataType.Int32));
        }
    }
}
