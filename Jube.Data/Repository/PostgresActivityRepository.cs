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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Repository.Models;
using Npgsql;

namespace Jube.Data.Repository
{
    public sealed class PostgresActivityRepository(string connectionString) : IAsyncDisposable
    {
        private const string ActivityQuery = """
                                             SELECT
                                                 a.pid,
                                                 a.datname,
                                                 a.usename,
                                                 a.application_name,
                                                 a.client_addr::text,
                                                 a.backend_type,
                                                 a.state,
                                                 a.wait_event_type,
                                                 a.wait_event,
                                                 pg_blocking_pids(a.pid),
                                                 a.backend_start,
                                                 a.xact_start,
                                                 EXTRACT(EPOCH FROM (now() - a.xact_start)),
                                                 a.query_start,
                                                 EXTRACT(EPOCH FROM (now() - a.query_start)),
                                                 a.query
                                             FROM pg_stat_activity a
                                             WHERE a.pid <> pg_backend_pid()
                                             ORDER BY a.query_start NULLS LAST
                                             """;

        private NpgsqlConnection connection;

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<List<PostgresActivityRow>> GetActivityAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => GetActivityCoreAsync(token), token);
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

        private async Task<List<PostgresActivityRow>> GetActivityCoreAsync(CancellationToken token)
        {
            var results = new List<PostgresActivityRow>();

            await using var command = new NpgsqlCommand(ActivityQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(new PostgresActivityRow(
                    reader.GetInt32(0),
                    await ReadStringAsync(reader, 1, token),
                    await ReadStringAsync(reader, 2, token),
                    await ReadStringAsync(reader, 3, token),
                    await ReadStringAsync(reader, 4, token),
                    await ReadStringAsync(reader, 5, token),
                    await ReadStringAsync(reader, 6, token),
                    await ReadStringAsync(reader, 7, token),
                    await ReadStringAsync(reader, 8, token),
                    await reader.IsDBNullAsync(9, token).ConfigureAwait(false)
                        ? []
                        : (int[])reader.GetValue(9),
                    await ReadDateTimeAsync(reader, 10, token),
                    await ReadDateTimeAsync(reader, 11, token),
                    await ReadDoubleAsync(reader, 12, token),
                    await ReadDateTimeAsync(reader, 13, token),
                    await ReadDoubleAsync(reader, 14, token),
                    await ReadStringAsync(reader, 15, token)));
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

        private static async Task<double?> ReadDoubleAsync(NpgsqlDataReader reader, int ordinal,
            CancellationToken token)
        {
            return await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false)
                ? null
                : reader.GetDouble(ordinal);
        }

        private static async Task<DateTime?> ReadDateTimeAsync(NpgsqlDataReader reader, int ordinal,
            CancellationToken token)
        {
            return await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false)
                ? null
                : reader.GetDateTime(ordinal);
        }
    }
}