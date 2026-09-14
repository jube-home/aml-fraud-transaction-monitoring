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
    public sealed class PostgresStatementStatisticsRepository(string connectionString) : IAsyncDisposable
    {
        private const string StatementStatisticsQuery = """
                                                        SELECT
                                                            s.queryid,
                                                            d.datname,
                                                            r.rolname,
                                                            s.query,
                                                            s.calls,
                                                            s.rows,
                                                            s.total_exec_time,
                                                            s.mean_exec_time,
                                                            s.min_exec_time,
                                                            s.max_exec_time,
                                                            s.stddev_exec_time,
                                                            s.shared_blks_hit * current_setting('block_size')::bigint,
                                                            s.shared_blks_read * current_setting('block_size')::bigint,
                                                            s.temp_blks_read * current_setting('block_size')::bigint,
                                                            s.temp_blks_written * current_setting('block_size')::bigint,
                                                            s.wal_bytes::bigint
                                                        FROM pg_stat_statements s
                                                        LEFT JOIN pg_database d ON d.oid = s.dbid
                                                        LEFT JOIN pg_roles r ON r.oid = s.userid
                                                        ORDER BY s.total_exec_time DESC
                                                        """;

        private NpgsqlConnection connection;

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<List<PostgresStatementStatisticsRow>> GetStatementStatisticsAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => GetStatementStatisticsCoreAsync(token), token);
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

        private async Task<List<PostgresStatementStatisticsRow>> GetStatementStatisticsCoreAsync(
            CancellationToken token)
        {
            var results = new List<PostgresStatementStatisticsRow>();

            await using var command = new NpgsqlCommand(StatementStatisticsQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(new PostgresStatementStatisticsRow(
                    reader.GetInt64(0),
                    await ReadStringAsync(reader, 1, token),
                    await ReadStringAsync(reader, 2, token),
                    await ReadStringAsync(reader, 3, token),
                    reader.GetInt64(4),
                    reader.GetInt64(5),
                    reader.GetDouble(6),
                    reader.GetDouble(7),
                    reader.GetDouble(8),
                    reader.GetDouble(9),
                    reader.GetDouble(10),
                    reader.GetInt64(11),
                    reader.GetInt64(12),
                    reader.GetInt64(13),
                    reader.GetInt64(14),
                    reader.GetInt64(15)));
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