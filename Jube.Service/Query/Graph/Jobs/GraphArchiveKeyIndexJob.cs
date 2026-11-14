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

using Jube.Service.Query.Graph.Jobs.Models;
using log4net;
using Npgsql;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphArchiveKeyIndexJob(string connectionString, string? migrationConnectionString, ILog log)
    {
        public const long AdvisoryLock = 7304021;

        private const string CreateStatement =
            "create index concurrently if not exists \"" + GraphBuildJob.ArchiveKeyIndexName + "\" on \"ArchiveKey\" " +
            "(\"EntityAnalysisModelInstanceEntryGuid\") include (\"Key\", \"KeyValueString\", \"KeyValueInteger\", " +
            "\"KeyValueFloat\", \"KeyValueLong\", \"Version\")";

        public async Task<GraphIndexResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(string.IsNullOrWhiteSpace(migrationConnectionString)
                ? connectionString
                : migrationConnectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);

            if (await IsValidAsync(connection, token).ConfigureAwait(false))
            {
                return new GraphIndexResult(true, false, null);
            }

            await using (var attempt = new NpgsqlCommand("select pg_try_advisory_lock(@key)", connection))
            {
                attempt.Parameters.AddWithValue("key", AdvisoryLock);

                if (await attempt.ExecuteScalarAsync(token).ConfigureAwait(false) is not true)
                {
                    return new GraphIndexResult(false, false, "AnotherNodeIsIndexing");
                }
            }

            try
            {
                await using (var drop = new NpgsqlCommand(
                                 "drop index concurrently if exists \"" + GraphBuildJob.ArchiveKeyIndexName + "\"",
                                 connection))
                {
                    drop.CommandTimeout = 0;
                    await drop.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }

                await using (var create = new NpgsqlCommand(CreateStatement, connection))
                {
                    create.CommandTimeout = 0;
                    await create.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                }

                var valid = await IsValidAsync(connection, token).ConfigureAwait(false);

                return new GraphIndexResult(valid, valid, valid ? null : "IndexInvalidAfterBuild");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error("GraphArchiveKeyIndexJob: building the ArchiveKey entry guid index failed.", ex);

                return new GraphIndexResult(false, false, ex.Message);
            }
        }

        private static async Task<bool> IsValidAsync(NpgsqlConnection connection, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(
                "select i.indisvalid from pg_index i join pg_class c on c.oid = i.indexrelid where c.relname = @name",
                connection);
            command.Parameters.AddWithValue("name", GraphBuildJob.ArchiveKeyIndexName);

            return await command.ExecuteScalarAsync(token).ConfigureAwait(false) is true;
        }
    }
}
