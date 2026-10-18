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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Query.GetPostgresLocksQuery.Models;
using Npgsql;

namespace Jube.Data.Query.GetPostgresLocksQuery
{
    public sealed class GetPostgresLocksQuery(string connectionString) : IAsyncDisposable
    {
        private const string LockQuery = """
                                         SELECT
                                             l.pid,
                                             a.usename,
                                             a.query,
                                             l.locktype,
                                             l.mode,
                                             l.granted,
                                             l.relation::regclass::text,
                                             l.virtualtransaction,
                                             l.fastpath
                                         FROM pg_locks l
                                         LEFT JOIN pg_stat_activity a ON a.pid = l.pid
                                         WHERE l.pid <> pg_backend_pid()
                                         ORDER BY l.granted, l.pid
                                         """;

        private NpgsqlConnection connection;

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<List<PostgresLockRow>> ExecuteAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => GetLocksCoreAsync(token), token);
        }

        private async Task<T> WithConnectionAsync<T>(Func<Task<T>> action, CancellationToken token)
        {
            try
            {
                if (connection is not { State: ConnectionState.Open })
                {
                    if (connection != null)
                    {
                        await connection.DisposeAsync().ConfigureAwait(false);
                    }

                    connection = new NpgsqlConnection(connectionString);
                    await connection.OpenAsync(token).ConfigureAwait(false);
                }

                return await action().ConfigureAwait(false);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                if (connection != null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    connection = null;
                }

                throw;
            }
        }

        private async Task<List<PostgresLockRow>> GetLocksCoreAsync(CancellationToken token)
        {
            var results = new List<PostgresLockRow>();

            await using var command = new NpgsqlCommand(LockQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(new PostgresLockRow(
                    reader.GetInt32(0),
                    await ReadStringAsync(reader, 1, token),
                    await ReadStringAsync(reader, 2, token),
                    await ReadStringAsync(reader, 3, token),
                    await ReadStringAsync(reader, 4, token),
                    !await reader.IsDBNullAsync(5, token).ConfigureAwait(false) && reader.GetBoolean(5),
                    await ReadStringAsync(reader, 6, token),
                    await ReadStringAsync(reader, 7, token),
                    !await reader.IsDBNullAsync(8, token).ConfigureAwait(false) && reader.GetBoolean(8)));
            }

            return results;
        }

        private static async Task<string> ReadStringAsync(NpgsqlDataReader reader, int ordinal,
            CancellationToken token)
        {
            return await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false)
                ? null
                : reader.GetString(ordinal);
        }
    }
}