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

using Jube.Engine.BackgroundTasks.TaskStarters.Metrics;
using StackExchange.Redis;

namespace Jube.Service.RedisSentinel
{
    public sealed class RedisSentinelClient(string redisConnectionString) : IDisposable
    {
        private ConnectionMultiplexer? sentinelMultiplexer;
        private string? serviceName;
        public string? LastError { get; private set; }

        public void Dispose()
        {
            sentinelMultiplexer?.Dispose();
        }

        public async Task<IReadOnlyList<global::Jube.Data.Poco.RedisSentinelStatus>> GetClusterStatusNowAsync(
            CancellationToken token = default)
        {
            var (multiplexer, connectedServiceName) = Connect();

            if (multiplexer is null || connectedServiceName is null)
            {
                return [];
            }

            var sampler = new RedisSentinelStatusSampler();

            return await sampler.SampleAsync(multiplexer, connectedServiceName).ConfigureAwait(false);
        }

        public async Task<string?> CheckQuorumAsync(string masterName, CancellationToken token = default)
        {
            var server = ConnectedServer();

            if (server is null)
            {
                return null;
            }

            try
            {
                var reply = await server.ExecuteAsync("SENTINEL", "CKQUORUM", masterName).ConfigureAwait(false);

                return reply.IsNull ? null : reply.ToString();
            }
            catch (RedisException ex)
            {
                LastError = ex.Message;

                return ex.Message;
            }
        }

        public async Task<string?> GetMasterAddressAsync(string masterName, CancellationToken token = default)
        {
            var server = ConnectedServer();

            if (server is null)
            {
                return null;
            }

            var reply = await server.ExecuteAsync("SENTINEL", "GET-MASTER-ADDR-BY-NAME", masterName)
                .ConfigureAwait(false);

            if (reply.IsNull || ((RedisResult[])reply)! is not { Length: 2 } parts)
            {
                return null;
            }

            return (string?)parts[0] is { } host && (string?)parts[1] is { } port ? $"{host}:{port}" : null;
        }

        public async Task<bool> FailoverAsync(string masterName, CancellationToken token = default)
        {
            var server = ConnectedServer();

            if (server is null)
            {
                return false;
            }

            try
            {
                var reply = await server.ExecuteAsync("SENTINEL", "FAILOVER", masterName).ConfigureAwait(false);

                return !reply.IsNull && string.Equals(reply.ToString(), "OK", StringComparison.OrdinalIgnoreCase);
            }
            catch (RedisException ex)
            {
                LastError = ex.Message;

                return false;
            }
        }

        public async Task<long> ResetAsync(string pattern, CancellationToken token = default)
        {
            var server = ConnectedServer();

            if (server is null)
            {
                return 0;
            }

            var reply = await server.ExecuteAsync("SENTINEL", "RESET", pattern).ConfigureAwait(false);

            return reply.IsNull ? 0 : (long)reply;
        }

        private (ConnectionMultiplexer? Multiplexer, string? ServiceName) Connect()
        {
            LastError = null;

            if (sentinelMultiplexer is not null)
            {
                return (sentinelMultiplexer, serviceName);
            }

            try
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);

                if (string.IsNullOrEmpty(options.ServiceName))
                {
                    LastError = "RedisConnectionString has no ServiceName -- Sentinel is not configured.";

                    return (null, null);
                }

                options.AbortOnConnectFail = false;
                serviceName = options.ServiceName;
                sentinelMultiplexer = ConnectionMultiplexer.SentinelConnect(options);

                return (sentinelMultiplexer, serviceName);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;

                return (null, null);
            }
        }

        private IServer? ConnectedServer()
        {
            var (multiplexer, _) = Connect();

            return multiplexer?.GetServers().FirstOrDefault(s => s.IsConnected);
        }
    }
}