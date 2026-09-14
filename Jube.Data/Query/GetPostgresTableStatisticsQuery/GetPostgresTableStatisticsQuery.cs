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

namespace Jube.Data.Query.GetPostgresTableStatisticsQuery
{
    public class GetPostgresTableStatisticsQuery(DbContext dbContext)
    {
        private const string Sql = """
                                   SELECT
                                       t.schemaname AS "SchemaName",
                                       t.relname AS "TableName",
                                       t.seq_scan AS "SequentialScans",
                                       t.seq_tup_read AS "SequentialTuplesRead",
                                       t.idx_scan AS "IndexScans",
                                       t.idx_tup_fetch AS "IndexTuplesFetched",
                                       t.n_live_tup AS "LiveTupleCount",
                                       t.n_dead_tup AS "DeadTupleCount",
                                       pg_relation_size(t.relid) AS "TableSizeBytes",
                                       pg_indexes_size(t.relid) AS "IndexesSizeBytes",
                                       pg_total_relation_size(t.relid) AS "TotalSizeBytes",
                                       t.last_vacuum AS "LastVacuum",
                                       t.last_autovacuum AS "LastAutoVacuum",
                                       t.last_analyze AS "LastAnalyze",
                                       t.last_autoanalyze AS "LastAutoAnalyze",
                                       (t.seq_scan > coalesce(t.idx_scan, 0) AND t.n_live_tup > 1000) AS "LikelyMissingIndex"
                                   FROM pg_stat_user_tables t
                                   ORDER BY t.seq_tup_read DESC NULLS LAST
                                   """;

        public Task<List<GetPostgresTableStatisticsQueryDto>> ExecuteAsync(CancellationToken token = default)
        {
            return dbContext.QueryToListAsync<GetPostgresTableStatisticsQueryDto>(Sql, token);
        }
    }
}