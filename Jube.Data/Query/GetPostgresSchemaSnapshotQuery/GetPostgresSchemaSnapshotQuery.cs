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
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Query.GetPostgresSchemaSnapshotQuery.Models;
using Npgsql;

namespace Jube.Data.Query.GetPostgresSchemaSnapshotQuery
{
    public sealed class GetPostgresSchemaSnapshotQuery(string connectionString)
    {
        private const int CommandTimeoutSeconds = 30;

        private const string ColumnsSql =
            """
            SELECT n.nspname, c.relname, a.attname, t.typname, t.typcategory::text
            FROM pg_catalog.pg_class c
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid AND a.attnum > 0 AND NOT a.attisdropped
            JOIN pg_catalog.pg_type t ON t.oid = a.atttypid
            WHERE c.relkind IN ('r', 'v', 'm', 'p', 'f')
              AND (n.nspname = 'public' OR (n.nspname = 'pg_catalog' AND c.relname LIKE 'pg\_%'))
            ORDER BY n.nspname, c.relname, a.attnum
            """;

        private const string PrimaryKeysSql =
            """
            SELECT c.relname, a.attname
            FROM pg_catalog.pg_index i
            JOIN pg_catalog.pg_class c ON c.oid = i.indrelid
            JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
            JOIN pg_catalog.pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY (i.indkey)
            WHERE i.indisprimary AND n.nspname = 'public'
            """;

        private const string FunctionsSql =
            """
            SELECT p.proname, p.pronargs::int, p.pronargdefaults::int, p.provariadic <> 0, p.prokind::text
            FROM pg_catalog.pg_proc p
            JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
            WHERE n.nspname IN ('pg_catalog', 'public')
            """;

        private const string TypesSql =
            """
            SELECT t.typname
            FROM pg_catalog.pg_type t
            JOIN pg_catalog.pg_namespace n ON n.oid = t.typnamespace
            WHERE n.nspname IN ('pg_catalog', 'public')
            """;

        public async Task<PostgresSchemaSnapshotRows> ExecuteAsync(CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);

            var columns = new List<PostgresSchemaColumnRow>();
            var primaryKeys = new List<PostgresSchemaPrimaryKeyRow>();
            var functions = new List<PostgresSchemaFunctionRow>();
            var types = new List<string>();

            await using (var reader = await Command(connection, ColumnsSql).ExecuteReaderAsync(token)
                             .ConfigureAwait(false))
            {
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    columns.Add(new PostgresSchemaColumnRow(reader.GetString(0), reader.GetString(1),
                        reader.GetString(2), reader.GetString(3), reader.GetString(4)));
                }
            }

            await using (var reader = await Command(connection, PrimaryKeysSql).ExecuteReaderAsync(token)
                             .ConfigureAwait(false))
            {
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    primaryKeys.Add(new PostgresSchemaPrimaryKeyRow(reader.GetString(0), reader.GetString(1)));
                }
            }

            await using (var reader = await Command(connection, FunctionsSql).ExecuteReaderAsync(token)
                             .ConfigureAwait(false))
            {
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    functions.Add(new PostgresSchemaFunctionRow(reader.GetString(0), reader.GetInt32(1),
                        reader.GetInt32(2), reader.GetBoolean(3), reader.GetString(4)));
                }
            }

            await using (var reader = await Command(connection, TypesSql).ExecuteReaderAsync(token)
                             .ConfigureAwait(false))
            {
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    types.Add(reader.GetString(0));
                }
            }

            return new PostgresSchemaSnapshotRows(columns, primaryKeys, functions, types);
        }

        private static NpgsqlCommand Command(NpgsqlConnection connection, string sql)
        {
            return new NpgsqlCommand(sql, connection) { CommandTimeout = CommandTimeoutSeconds };
        }
    }
}
