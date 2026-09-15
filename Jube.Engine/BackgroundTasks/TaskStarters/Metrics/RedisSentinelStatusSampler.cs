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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Poco;
using Jube.Data.Repository;
using StackExchange.Redis;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class RedisSentinelStatusSampler
    {
        public async Task<List<RedisSentinelStatus>> SampleAsync(IConnectionMultiplexer sentinelMultiplexer,
            string serviceName, IReadOnlyDictionary<string, double> replicationLagByEndpoint = null)
        {
            var results = new List<RedisSentinelStatus>();
            replicationLagByEndpoint ??= new Dictionary<string, double>();

            if (sentinelMultiplexer == null || string.IsNullOrEmpty(serviceName))
            {
                return results;
            }

            var server = sentinelMultiplexer.GetServers().FirstOrDefault(s => s.IsConnected);
            if (server == null)
            {
                return results;
            }

            var mastersReply = await server.ExecuteAsync("SENTINEL", "MASTERS").ConfigureAwait(false);
            AddStatusesForEachEntry(results, mastersReply, RedisSentinelEntityType.Master, replicationLagByEndpoint);

            var slavesReply = await server.ExecuteAsync("SENTINEL", "SLAVES", serviceName).ConfigureAwait(false);
            AddStatusesForEachEntry(results, slavesReply, RedisSentinelEntityType.Slave, replicationLagByEndpoint);

            var sentinelsReply = await server.ExecuteAsync("SENTINEL", "SENTINELS", serviceName)
                .ConfigureAwait(false);

            AddStatusesForEachEntry(results, sentinelsReply, RedisSentinelEntityType.Sentinel,
                replicationLagByEndpoint);

            return results;
        }

        private static void AddStatusesForEachEntry(List<RedisSentinelStatus> results, RedisResult reply,
            RedisSentinelEntityType entityType, IReadOnlyDictionary<string, double> replicationLagByEndpoint)
        {
            if (reply.IsNull || (RedisResult[])reply is not { } entries)
            {
                return;
            }

            foreach (var entry in entries)
            {
                if (entry == null || entry.IsNull || (RedisResult[])entry is not { } flat)
                {
                    continue;
                }

                var fields = ParseFlatKeyValues(flat);
                results.Add(BuildStatus(entityType, fields, replicationLagByEndpoint));
            }
        }

        private static Dictionary<string, string> ParseFlatKeyValues(RedisResult[] flat)
        {
            var fields = new Dictionary<string, string>();

            for (var i = 0; i + 1 < flat.Length; i += 2)
            {
                var key = (string)flat[i];
                if (key != null)
                {
                    fields[key] = (string)flat[i + 1];
                }
            }

            return fields;
        }

        private static RedisSentinelStatus BuildStatus(RedisSentinelEntityType entityType,
            Dictionary<string, string> fields,
            IReadOnlyDictionary<string, double> replicationLagByEndpoint)
        {
            int? ParseInt(string key)
            {
                return fields.TryGetValue(key, out var raw) &&
                       int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : null;
            }

            long? ParseLong(string key)
            {
                return fields.TryGetValue(key, out var raw) &&
                       long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                    ? value
                    : null;
            }

            string GetString(string key)
            {
                return fields.GetValueOrDefault(key);
            }

            var ip = GetString("ip");
            var port = ParseInt("port");

            double? replicationLagSeconds = null;
            if (entityType == RedisSentinelEntityType.Slave && ip != null && port.HasValue &&
                replicationLagByEndpoint.TryGetValue($"{ip}:{port}", out var lag))
            {
                replicationLagSeconds = lag;
            }

            return new RedisSentinelStatus
            {
                EntityTypeId = (int)entityType,
                Name = GetString("name"),
                Ip = ip,
                Port = port,
                Flags = GetString("flags"),
                MasterLinkStatus = GetString("master-link-status"),
                MasterHost = GetString("master-host"),
                MasterPort = ParseInt("master-port"),
                SlaveReplOffset = ParseLong("slave-repl-offset"),
                ReplicationLagSeconds = replicationLagSeconds,
                NumSlaves = ParseInt("num-slaves"),
                NumOtherSentinels = ParseInt("num-other-sentinels"),
                Quorum = ParseInt("quorum"),
                DownAfterMilliseconds = ParseLong("down-after-milliseconds"),
                RunId = GetString("runid")
            };
        }
    }
}