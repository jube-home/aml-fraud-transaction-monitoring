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

using System.Globalization;
using Jube.Service.Query.Graph.Jobs.Models;
using log4net;
using Npgsql;

namespace Jube.Service.Query.Graph.Jobs
{
    public sealed class GraphRetentionJob(string connectionString, string? migrationConnectionString, ILog log,
        int retentionDays)
    {
        public const int MonthsAhead = 3;

        public async Task<GraphRetentionResult> RunOnceAsync(CancellationToken token = default)
        {
            await using var connection = new NpgsqlConnection(string.IsNullOrWhiteSpace(migrationConnectionString)
                ? connectionString
                : migrationConnectionString);
            await connection.OpenAsync(token).ConfigureAwait(false);
            var created = 0;
            var dropped = 0;
            var month = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            for (var ahead = 0; ahead <= MonthsAhead; ahead++)
            {
                var from = month.AddMonths(ahead);
                var to = from.AddMonths(1);
                var name = PartitionName(from);

                if (await ExistsAsync(connection, name, token).ConfigureAwait(false))
                {
                    continue;
                }

                await ExecuteAsync(connection,
                    $"create table if not exists public.\"{name}\" partition of public.\"GraphEdgeEvent\" " +
                    $"for values from ('{from:yyyy-MM-dd}') to ('{to:yyyy-MM-dd}')", token).ConfigureAwait(false);
                created++;
            }

            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
            var partitions = await PartitionsAsync(connection, token).ConfigureAwait(false);

            foreach (var name in partitions)
            {
                if (!TryParseMonth(name, out var start) || start.AddMonths(1) >= cutoff)
                {
                    continue;
                }

                await ExecuteAsync(connection, $"drop table if exists public.\"{name}\"", token).ConfigureAwait(false);
                dropped++;
            }

            if (log.IsInfoEnabled && (created > 0 || dropped > 0))
            {
                log.Info($"GraphRetentionJob: partitions created {created}, dropped {dropped}.");
            }

            return new GraphRetentionResult(created, dropped);
        }

        public static string PartitionName(DateTime monthStart)
        {
            return "GraphEdgeEvent_" + monthStart.ToString("yyyyMM", CultureInfo.InvariantCulture);
        }

        private static bool TryParseMonth(string name, out DateTime start)
        {
            start = default;
            const string prefix = "GraphEdgeEvent_";

            return name.StartsWith(prefix, StringComparison.Ordinal) && DateTime.TryParseExact(name[prefix.Length..],
                "yyyyMM", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out start);
        }

        private static async Task<bool> ExistsAsync(NpgsqlConnection connection, string name, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(
                "select 1 from pg_class where relname = @name and relkind = 'r'", connection);
            command.Parameters.AddWithValue("name", name);

            return await command.ExecuteScalarAsync(token).ConfigureAwait(false) != null;
        }

        private static async Task<List<string>> PartitionsAsync(NpgsqlConnection connection, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(
                "select c.relname from pg_inherits i join pg_class c on c.oid = i.inhrelid " +
                "join pg_class p on p.oid = i.inhparent where p.relname = 'GraphEdgeEvent'", connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
            var names = new List<string>();

            while (await reader.ReadAsync(token).ConfigureAwait(false))
            {
                names.Add(reader.GetString(0));
            }

            return names;
        }

        private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken token)
        {
            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 0;
            await command.ExecuteNonQueryAsync(token).ConfigureAwait(false);
        }
    }
}
