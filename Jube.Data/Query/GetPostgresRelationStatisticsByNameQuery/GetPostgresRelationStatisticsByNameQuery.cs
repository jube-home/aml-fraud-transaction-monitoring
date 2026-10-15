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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Query.GetPostgresRelationStatisticsByNameQuery.Models;
using Npgsql;

namespace Jube.Data.Query.GetPostgresRelationStatisticsByNameQuery
{
    public sealed class GetPostgresRelationStatisticsByNameQuery(string connectionString)
    {
        private const int CommandTimeoutSeconds = 15;

        public async Task<IReadOnlyList<PostgresRelationStatisticRow>> ExecuteAsync(
            IReadOnlyList<string> relations, CancellationToken token = default)
        {
            if (relations.Count == 0)
            {
                return [];
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);

            await using var command = new NpgsqlCommand(
                """
                SELECT relname, coalesce(n_live_tup, 0), coalesce(idx_scan, 0), coalesce(seq_scan, 0)
                FROM pg_stat_user_tables
                WHERE relname = ANY(@relations)
                ORDER BY coalesce(n_live_tup, 0) DESC
                """, connection);

            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.AddWithValue("relations", relations.ToArray());

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var rows = new List<PostgresRelationStatisticRow>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                rows.Add(new PostgresRelationStatisticRow(reader.GetString(0), reader.GetInt64(1),
                    reader.GetInt64(2), reader.GetInt64(3)));
            }

            return rows;
        }
    }
}