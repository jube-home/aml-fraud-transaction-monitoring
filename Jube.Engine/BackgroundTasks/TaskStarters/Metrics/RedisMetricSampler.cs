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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Poco;
using StackExchange.Redis;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class RedisMetricSampler(string connectionString) : IAsyncDisposable
    {
        private long? lastAofRewrites;
        private DateTime? lastKnownAofRewriteDate;
        private (long Hits, long Misses)? lastSample;
        private long lastSeenSlowLogId = -1;
        private IConnectionMultiplexer multiplexer;

        public async ValueTask DisposeAsync()
        {
            if (multiplexer != null)
            {
                await multiplexer.CloseAsync().ConfigureAwait(false);
                multiplexer.Dispose();
            }
        }

        public async Task<RedisMetric> SampleAsync()
        {
            var server = await GetServerAsync().ConfigureAwait(false);
            if (server == null)
            {
                return null;
            }

            var sections = await server.InfoAsync().ConfigureAwait(false);
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pair in sections.SelectMany(section => section))
            {
                values[pair.Key] = pair.Value;
            }

            long GetLong(string key)
            {
                return values.TryGetValue(key, out var raw) &&
                       long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : 0;
            }

            double? GetDouble(string key)
            {
                return values.TryGetValue(key, out var raw) &&
                       double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : null;
            }

            long totalKeys = 0;
            foreach (var (key, value) in values)
            {
                if (!key.StartsWith("db", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var keysField = value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(f => f.StartsWith("keys=", StringComparison.OrdinalIgnoreCase));
                if (keysField != null &&
                    long.TryParse(keysField.AsSpan("keys=".Length), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var keyCount))
                {
                    totalKeys += keyCount;
                }
            }

            long? rdbLastSaveAgeSeconds = null;
            DateTime? lastBgSaveDate = null;
            if (values.TryGetValue("rdb_last_save_time", out var rdbLastSaveTimeRaw) &&
                long.TryParse(rdbLastSaveTimeRaw, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var rdbLastSaveTimeUnixSeconds))
            {
                rdbLastSaveAgeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - rdbLastSaveTimeUnixSeconds;
                lastBgSaveDate = DateTimeOffset.FromUnixTimeSeconds(rdbLastSaveTimeUnixSeconds).UtcDateTime;
            }

            var aofRewrites = GetLong("aof_rewrites");
            if (lastAofRewrites.HasValue && aofRewrites > lastAofRewrites.Value)
            {
                lastKnownAofRewriteDate = DateTime.UtcNow;
            }

            lastAofRewrites = aofRewrites;

            var hits = GetLong("keyspace_hits");
            var misses = GetLong("keyspace_misses");

            double? hitRatePercent = null;
            if (lastSample is { } last)
            {
                var hitDelta = hits - last.Hits;
                var missDelta = misses - last.Misses;
                var totalDelta = hitDelta + missDelta;
                hitRatePercent = totalDelta > 0 ? 100.0 * hitDelta / totalDelta : 100.0;
            }

            lastSample = (hits, misses);

            return new RedisMetric
            {
                ConnectedClients = (int)GetLong("connected_clients"),
                BlockedClients = (int)GetLong("blocked_clients"),
                UsedMemoryBytes = GetLong("used_memory"),
                UsedMemoryRssBytes = GetLong("used_memory_rss"),
                MaxMemoryBytes = GetLong("maxmemory"),
                InstantaneousOpsPerSecond = (int)GetLong("instantaneous_ops_per_sec"),
                TotalCommandsProcessed = GetLong("total_commands_processed"),
                TotalConnectionsReceived = GetLong("total_connections_received"),
                KeyspaceHits = hits,
                KeyspaceMisses = misses,
                HitRatePercent = hitRatePercent,
                EvictedKeys = GetLong("evicted_keys"),
                ExpiredKeys = GetLong("expired_keys"),
                ConnectedReplicas = (int)GetLong("connected_slaves"),
                MasterReplicationOffset = GetLong("master_repl_offset"),
                UptimeSeconds = GetLong("uptime_in_seconds"),
                TotalKeys = totalKeys,
                MemoryFragmentationRatio = GetDouble("mem_fragmentation_ratio"),
                RdbLastSaveAgeSeconds = rdbLastSaveAgeSeconds,
                AofEnabled = GetLong("aof_enabled") == 1,
                LastAofRewriteDate = lastKnownAofRewriteDate,
                LastBgSaveDate = lastBgSaveDate
            };
        }

        public async Task<List<RedisSlowOperation>> SampleSlowOperationsAsync()
        {
            var server = await GetServerAsync().ConfigureAwait(false);
            if (server == null)
            {
                return [];
            }

            var raw = await server.ExecuteAsync("SLOWLOG", "GET", "128").ConfigureAwait(false);
            if (raw.IsNull || (RedisResult[])raw is not { } entries)
            {
                return [];
            }

            var results = new List<RedisSlowOperation>();

            foreach (var entryResult in entries)
            {
                if (entryResult == null || (RedisResult[])entryResult is not { Length: >= 4 } fields)
                {
                    continue;
                }

                var id = (long)fields[0];

                if (id <= lastSeenSlowLogId)
                {
                    continue;
                }

                var timestampUnixSeconds = (long)fields[1];
                var durationMicroseconds = (long)fields[2];
                var arguments = (RedisValue[])fields[3] ?? [];

                results.Add(new RedisSlowOperation
                {
                    RedisSlowLogId = id,
                    OccurredDate = DateTimeOffset.FromUnixTimeSeconds(timestampUnixSeconds).UtcDateTime,
                    DurationMicroseconds = durationMicroseconds,
                    Command = string.Join(' ', arguments.Select(a => a.ToString())),
                    CommandName = arguments.Length > 0 ? arguments[0].ToString() : null,
                    KeyName = arguments.Length > 1 ? arguments[1].ToString() : null,
                    ClientAddress = fields.Length > 4 ? (string)fields[4] : null,
                    ClientName = fields.Length > 5 ? (string)fields[5] : null
                });
            }

            if (results.Count > 0)
            {
                lastSeenSlowLogId = results.Max(r => r.RedisSlowLogId.GetValueOrDefault());
            }

            return results;
        }

        public async Task<Dictionary<string, double>> SampleReplicationLagAsync()
        {
            var lagByEndpoint = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            var server = await GetServerAsync().ConfigureAwait(false);
            if (server == null)
            {
                return lagByEndpoint;
            }

            var sections = await server.InfoAsync("replication").ConfigureAwait(false);

            foreach (var pair in sections.SelectMany(section => section))
            {
                if (!pair.Key.StartsWith("slave", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string ip = null;
                string port = null;
                double? lagSeconds = null;

                foreach (var field in pair.Value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = field.Split('=', 2);
                    if (kv.Length != 2)
                    {
                        continue;
                    }

                    switch (kv[0])
                    {
                        case "ip":
                            ip = kv[1];
                            break;
                        case "port":
                            port = kv[1];
                            break;
                        case "lag":
                            lagSeconds = double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture,
                                out var parsedLag)
                                ? parsedLag
                                : null;
                            break;
                    }
                }

                if (ip != null && port != null && lagSeconds.HasValue)
                {
                    lagByEndpoint[$"{ip}:{port}"] = lagSeconds.Value;
                }
            }

            return lagByEndpoint;
        }

        private async Task<IServer> GetServerAsync()
        {
            multiplexer ??= await ConnectionMultiplexer.ConnectAsync(BuildOptions()).ConfigureAwait(false);
            return multiplexer.GetServers().FirstOrDefault(s => s.IsConnected);
        }

        private ConfigurationOptions BuildOptions()
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            return options;
        }
    }
}