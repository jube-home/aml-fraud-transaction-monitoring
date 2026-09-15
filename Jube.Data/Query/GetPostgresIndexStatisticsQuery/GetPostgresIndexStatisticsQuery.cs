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

namespace Jube.Data.Query.GetPostgresIndexStatisticsQuery
{
    public class GetPostgresIndexStatisticsQuery(DbContext dbContext)
    {
        private const string Sql = """
                                   SELECT
                                       s.schemaname AS "SchemaName",
                                       s.relname AS "TableName",
                                       s.indexrelname AS "IndexName",
                                       s.idx_scan AS "IndexScans",
                                       s.idx_tup_read AS "IndexTuplesRead",
                                       s.idx_tup_fetch AS "IndexTuplesFetched",
                                       pg_relation_size(s.indexrelid) AS "IndexSizeBytes",
                                       ix.indisunique AS "IsUnique",
                                       ix.indisprimary AS "IsPrimary",
                                       (coalesce(s.idx_scan, 0) = 0) AS "IsUnused"
                                   FROM pg_stat_user_indexes s
                                   JOIN pg_index ix ON ix.indexrelid = s.indexrelid
                                   ORDER BY pg_relation_size(s.indexrelid) DESC
                                   """;

        public Task<List<GetPostgresIndexStatisticsQueryDto>> ExecuteAsync(CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetPostgresIndexStatisticsQueryDto>(Sql, token);
        }
    }
}