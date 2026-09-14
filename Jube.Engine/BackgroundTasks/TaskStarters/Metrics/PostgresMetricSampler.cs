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
using Jube.Data.Poco;
using Npgsql;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class PostgresMetricSampler(string connectionString) : IAsyncDisposable
    {
        private const string StatsQuery = """
                                          SELECT
                                              d.numbackends,
                                              d.xact_commit,
                                              d.xact_rollback,
                                              d.blks_read,
                                              d.blks_hit,
                                              d.tup_returned,
                                              d.tup_fetched,
                                              d.tup_inserted,
                                              d.tup_updated,
                                              d.tup_deleted,
                                              d.deadlocks,
                                              d.temp_files,
                                              d.temp_bytes,
                                              d.conflicts,
                                              pg_is_in_recovery() AS is_in_recovery,
                                              CASE WHEN pg_is_in_recovery()
                                                  THEN EXTRACT(EPOCH FROM (now() - pg_last_xact_replay_timestamp()))
                                              END AS standby_lag_seconds,
                                              (SELECT MAX(EXTRACT(EPOCH FROM replay_lag)) FROM pg_stat_replication) AS max_replica_lag_seconds,
                                              (SELECT COUNT(*) FROM pg_stat_replication) AS replica_count,
                                              pg_database_size(current_database()) AS database_size_bytes,
                                              (SELECT MAX(EXTRACT(EPOCH FROM (now() - query_start)))
                                               FROM pg_stat_activity
                                               WHERE state = 'active' AND pid <> pg_backend_pid()) AS longest_running_query_seconds,
                                              (SELECT COUNT(*) FROM pg_stat_activity WHERE wait_event_type = 'Lock') AS waiting_backends,
                                              CASE WHEN pg_is_in_recovery()
                                                  THEN pg_wal_lsn_diff(pg_last_wal_receive_lsn(), '0/0')
                                                  ELSE pg_wal_lsn_diff(pg_current_wal_lsn(), '0/0')
                                              END AS wal_bytes_generated
                                          FROM pg_stat_database d
                                          WHERE d.datname = current_database()
                                          """;

        private const string ReplicationQuery = """
                                                SELECT
                                                    pid,
                                                    usename,
                                                    application_name,
                                                    client_addr::text,
                                                    state,
                                                    sent_lsn::text,
                                                    write_lsn::text,
                                                    flush_lsn::text,
                                                    replay_lsn::text,
                                                    EXTRACT(EPOCH FROM write_lag),
                                                    EXTRACT(EPOCH FROM flush_lag),
                                                    EXTRACT(EPOCH FROM replay_lag),
                                                    sync_state,
                                                    sync_priority
                                                FROM pg_stat_replication
                                                """;

        private NpgsqlConnection connection;
        private (long BlksHit, long BlksRead)? lastSample;

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<PostgresMetric> SampleAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => SampleCoreAsync(token), token);
        }

        public Task<List<PostgresReplicationStatus>> SampleReplicationAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => SampleReplicationCoreAsync(token), token);
        }

        private async Task<T> WithConnectionAsync<T>(Func<Task<T>> action, CancellationToken token)
        {
            try
            {
                if (connection is { State: ConnectionState.Open })
                {
                    return await action().ConfigureAwait(false);
                }

                if (connection != null)
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }

                connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(token).ConfigureAwait(false);

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

        private async Task<PostgresMetric> SampleCoreAsync(CancellationToken token)
        {
            await using var command = new NpgsqlCommand(StatsQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return null;
            }

            var blocksRead = reader.GetInt64(3);
            var blocksHit = reader.GetInt64(4);
            var isInRecovery = reader.GetBoolean(14);

            var standbyLagSeconds = await reader.IsDBNullAsync(15, token).ConfigureAwait(false)
                ? (double?)null
                : reader.GetDouble(15);

            var maxReplicaLagSeconds = await reader.IsDBNullAsync(16, token).ConfigureAwait(false)
                ? (double?)null
                : reader.GetDouble(16);

            double? cacheHitRatioPercent = null;

            if (lastSample is { } last)
            {
                var hitDelta = blocksHit - last.BlksHit;
                var readDelta = blocksRead - last.BlksRead;
                var totalDelta = hitDelta + readDelta;
                cacheHitRatioPercent = totalDelta > 0 ? 100.0 * hitDelta / totalDelta : 100.0;
            }

            lastSample = (blocksHit, blocksRead);

            return new PostgresMetric
            {
                ActiveConnections = reader.GetInt32(0),
                TransactionsCommitted = reader.GetInt64(1),
                TransactionsRolledBack = reader.GetInt64(2),
                BlocksRead = blocksRead,
                BlocksHit = blocksHit,
                CacheHitRatioPercent = cacheHitRatioPercent,
                RowsReturned = reader.GetInt64(5),
                RowsFetched = reader.GetInt64(6),
                RowsInserted = reader.GetInt64(7),
                RowsUpdated = reader.GetInt64(8),
                RowsDeleted = reader.GetInt64(9),
                Deadlocks = reader.GetInt64(10),
                TempFilesCreated = reader.GetInt64(11),
                TempBytesWritten = reader.GetInt64(12),
                Conflicts = reader.GetInt64(13),
                IsInRecovery = isInRecovery,
                ReplicationLagSeconds = isInRecovery ? standbyLagSeconds : maxReplicaLagSeconds,
                ReplicaCount = (int)reader.GetInt64(17),
                DatabaseSizeBytes = reader.GetInt64(18),
                LongestRunningQuerySeconds = await reader.IsDBNullAsync(19, token).ConfigureAwait(false)
                    ? null
                    : reader.GetDouble(19),
                WaitingBackends = (int)reader.GetInt64(20),
                WalBytesGenerated = reader.GetInt64(21)
            };
        }

        private async Task<List<PostgresReplicationStatus>> SampleReplicationCoreAsync(CancellationToken token)
        {
            var results = new List<PostgresReplicationStatus>();

            await using var command = new NpgsqlCommand(ReplicationQuery, connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                results.Add(new PostgresReplicationStatus
                {
                    Pid = reader.GetInt32(0),
                    UserName = await NullableStringAsync(reader, 1, token).ConfigureAwait(false),
                    ApplicationName = await NullableStringAsync(reader, 2, token).ConfigureAwait(false),
                    ClientAddress = await NullableStringAsync(reader, 3, token).ConfigureAwait(false),
                    State = await NullableStringAsync(reader, 4, token).ConfigureAwait(false),
                    SentLsn = await NullableStringAsync(reader, 5, token).ConfigureAwait(false),
                    WriteLsn = await NullableStringAsync(reader, 6, token).ConfigureAwait(false),
                    FlushLsn = await NullableStringAsync(reader, 7, token).ConfigureAwait(false),
                    ReplayLsn = await NullableStringAsync(reader, 8, token).ConfigureAwait(false),
                    WriteLagSeconds = await NullableDoubleAsync(reader, 9, token).ConfigureAwait(false),
                    FlushLagSeconds = await NullableDoubleAsync(reader, 10, token).ConfigureAwait(false),
                    ReplayLagSeconds = await NullableDoubleAsync(reader, 11, token).ConfigureAwait(false),
                    SyncState = await NullableStringAsync(reader, 12, token).ConfigureAwait(false),
                    SyncPriority = await reader.IsDBNullAsync(13, token).ConfigureAwait(false)
                        ? null
                        : reader.GetInt32(13)
                });
            }

            return results;
        }

        private static async Task<double?> NullableDoubleAsync(NpgsqlDataReader reader, int ordinal,
            CancellationToken token)
        {
            return await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false)
                ? null
                : reader.GetDouble(ordinal);
        }

        private static async Task<string> NullableStringAsync(NpgsqlDataReader reader, int ordinal,
            CancellationToken token)
        {
            return await reader.IsDBNullAsync(ordinal, token).ConfigureAwait(false)
                ? null
                : reader.GetString(ordinal);
        }
    }
}