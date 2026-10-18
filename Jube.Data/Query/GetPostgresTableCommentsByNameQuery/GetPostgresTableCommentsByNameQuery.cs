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
using Jube.Data.Query.GetPostgresTableCommentsByNameQuery.Models;
using Npgsql;

namespace Jube.Data.Query.GetPostgresTableCommentsByNameQuery
{
    public sealed class GetPostgresTableCommentsByNameQuery(string connectionString)
    {
        private const int CommandTimeoutSeconds = 15;

        public async Task<IReadOnlyList<PostgresTableCommentRow>> ExecuteAsync(
            IReadOnlyList<string> tables, CancellationToken token = default)
        {
            if (tables.Count == 0)
            {
                return [];
            }

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);

            await using var command = new NpgsqlCommand(
                """
                SELECT c.relname, pg_catalog.obj_description(c.oid, 'pg_class')
                FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                WHERE c.relname = ANY(@tables)
                  AND n.nspname = 'public'
                  AND pg_catalog.obj_description(c.oid, 'pg_class') IS NOT NULL
                """, connection);

            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.AddWithValue("tables", tables.ToArray());

            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            var rows = new List<PostgresTableCommentRow>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                rows.Add(new PostgresTableCommentRow(reader.GetString(0), reader.GetString(1)));
            }

            return rows;
        }
    }
}