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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Npgsql;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class PostgresLogSampler(string connectionString) : IAsyncDisposable
    {
        private static readonly Regex linePrefixPattern = new(
            @"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}) \w+ \[(\d+)\] (\w+):\s*(.*)$",
            RegexOptions.Compiled);

        private NpgsqlConnection connection;
        private string currentFileName;
        private long currentOffset;
        private bool initialized;

        public bool LoggingCollectorOff { get; private set; }

        public async ValueTask DisposeAsync()
        {
            if (connection != null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
        }

        public Task<List<PostgresLogEntry>> SampleAsync(CancellationToken token = default)
        {
            return WithConnectionAsync(() => SampleCoreAsync(token), token);
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

        private async Task<List<PostgresLogEntry>> SampleCoreAsync(CancellationToken token)
        {
            LoggingCollectorOff = !await IsLoggingCollectorOnAsync(token).ConfigureAwait(false);
            if (LoggingCollectorOff)
            {
                return null;
            }

            var (fileName, size) = await GetLatestLogFileAsync(token).ConfigureAwait(false);
            if (fileName == null)
            {
                return null;
            }

            if (!initialized)
            {
                currentFileName = fileName;
                currentOffset = size;
                initialized = true;
                return null;
            }

            if (fileName != currentFileName)
            {
                currentFileName = fileName;
                currentOffset = 0;
            }
            else if (size < currentOffset)
            {
                currentOffset = 0;
            }

            if (size <= currentOffset)
            {
                return null;
            }

            var delta = await ReadLogDeltaAsync(fileName, currentOffset, size - currentOffset, token)
                .ConfigureAwait(false);
            currentOffset = size;

            return string.IsNullOrEmpty(delta) ? null : ParseEntries(delta);
        }

        private async Task<bool> IsLoggingCollectorOnAsync(CancellationToken token)
        {
            await using var command =
                new NpgsqlCommand("SELECT current_setting('logging_collector')", connection);
            var result = (string)await command.ExecuteScalarAsync(token).ConfigureAwait(false);
            return string.Equals(result, "on", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<(string FileName, long Size)> GetLatestLogFileAsync(CancellationToken token)
        {
            await using var command =
                new NpgsqlCommand("SELECT name, size FROM pg_ls_logdir() ORDER BY modification DESC LIMIT 1",
                    connection);
            await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);

            if (!await reader.ReadAsync(token).ConfigureAwait(false))
            {
                return (null, 0);
            }

            return (reader.GetString(0), reader.GetInt64(1));
        }

        private async Task<string> ReadLogDeltaAsync(string fileName, long offset, long length,
            CancellationToken token)
        {
            await using var command =
                new NpgsqlCommand("SELECT pg_read_file('log/' || @fileName, @offset, @length)", connection);
            command.Parameters.AddWithValue("fileName", fileName);
            command.Parameters.AddWithValue("offset", offset);
            command.Parameters.AddWithValue("length", length);

            return (string)await command.ExecuteScalarAsync(token).ConfigureAwait(false);
        }

        private static List<PostgresLogEntry> ParseEntries(string text)
        {
            var entries = new List<PostgresLogEntry>();
            PostgresLogEntry current = null;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    continue;
                }

                var match = linePrefixPattern.Match(line);
                if (match.Success)
                {
                    DateTime? occurredDate = DateTime.TryParseExact(match.Groups[1].Value,
                        "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
                        ? parsed
                        : null;

                    current = new PostgresLogEntry
                    {
                        OccurredDate = occurredDate,
                        Pid = int.TryParse(match.Groups[2].Value, out var pid) ? pid : null,
                        Level = match.Groups[3].Value,
                        Message = match.Groups[4].Value
                    };
                    entries.Add(current);
                }
                else if (current != null)
                {
                    current.Message = current.Message + "\n" + line;
                }
                else
                {
                    current = new PostgresLogEntry { Level = null, Message = line };
                    entries.Add(current);
                }
            }

            return entries;
        }
    }
}