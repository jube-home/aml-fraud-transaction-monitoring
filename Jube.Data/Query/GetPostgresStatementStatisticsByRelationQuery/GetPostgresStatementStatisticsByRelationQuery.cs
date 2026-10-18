/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Query.GetPostgresStatementStatisticsByRelationQuery.Models;
using Npgsql;

namespace Jube.Data.Query.GetPostgresStatementStatisticsByRelationQuery
{
    public sealed class GetPostgresStatementStatisticsByRelationQuery(string connectionString)
    {
        private const int CommandTimeoutSeconds = 15;

        public async Task<PostgresStatementStatisticsByRelationRow> ExecuteAsync(
            IReadOnlyList<string> relations, CancellationToken token = default)
        {
            if (relations.Count == 0)
            {
                return null;
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);

            await using var command = new NpgsqlCommand(
                """
                SELECT coalesce(sum(calls), 0),
                       coalesce(sum(total_exec_time), 0),
                       coalesce(max(max_exec_time), 0),
                       coalesce(sum(rows), 0),
                       coalesce(sum(shared_blks_hit), 0),
                       coalesce(sum(shared_blks_read), 0)
                FROM pg_stat_statements
                WHERE EXISTS (SELECT 1 FROM unnest(@relations) AS relation
                              WHERE query ILIKE '%"' || relation || '"%')
                """, connection);

            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.AddWithValue("relations", relations.ToArray());

            try
            {
                await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

                if (!await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    return null;
                }

                return new PostgresStatementStatisticsByRelationRow(
                    reader.GetInt64(0),
                    reader.GetDouble(1),
                    reader.GetDouble(2),
                    Convert.ToDouble(reader.GetValue(3)),
                    Convert.ToDouble(reader.GetValue(4)),
                    Convert.ToDouble(reader.GetValue(5)));
            }
            catch (PostgresException ex) when (ex.SqlState == "42P01")
            {
                return null;
            }
        }
    }
}