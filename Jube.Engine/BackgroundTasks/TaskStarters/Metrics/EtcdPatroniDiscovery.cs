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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class EtcdPatroniDiscovery
    {
        private const int DefaultMaxNodes = 5;
        private const int DefaultEtcdClientPort = 2379;
        private const int DefaultPatroniApiPort = 8008;

        public Task<List<string>> DiscoverEtcdEndpointsAsync(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            return DiscoverAsync(dynamicEnvironment, "EtcdEndpoints", "EtcdDiscoveryPrefix", "etcd",
                "EtcdClientPort", DefaultEtcdClientPort, token);
        }

        public Task<List<string>> DiscoverPatroniEndpointsAsync(
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            CancellationToken token = default)
        {
            return DiscoverAsync(dynamicEnvironment, "PatroniEndpoints", "PatroniDiscoveryPrefix", "patroni",
                "PatroniApiPort", DefaultPatroniApiPort, token);
        }

        private static async Task<List<string>> DiscoverAsync(DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            string explicitEndpointsKey, string prefixKey, string defaultPrefix, string portKey, int defaultPort,
            CancellationToken token)
        {
            var explicitEndpoints = dynamicEnvironment.AppSettings(explicitEndpointsKey);
            if (!string.IsNullOrWhiteSpace(explicitEndpoints))
            {
                return explicitEndpoints
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Distinct()
                    .ToList();
            }

            var prefix = dynamicEnvironment.AppSettings(prefixKey);
            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = defaultPrefix;
            }

            var maxNodesSetting = dynamicEnvironment.AppSettings("EtcdPatroniDiscoveryMaxNodes");
            var maxNodes = int.TryParse(maxNodesSetting, out var parsedMaxNodes) ? parsedMaxNodes : DefaultMaxNodes;

            var portSetting = dynamicEnvironment.AppSettings(portKey);
            var port = int.TryParse(portSetting, out var parsedPort) ? parsedPort : defaultPort;
            var candidates = Enumerable.Range(1, maxNodes).Select(i => $"{prefix}{i}").ToList();
            var resolutions = await Task.WhenAll(candidates.Select(async host =>
                    (Host: host, Resolves: await ResolvesAsync(host, token).ConfigureAwait(false))))
                .ConfigureAwait(false);

            return resolutions.Where(r => r.Resolves).Select(r => $"{r.Host}:{port}").ToList();
        }

        private static async Task<bool> ResolvesAsync(string host, CancellationToken token)
        {
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host, token).ConfigureAwait(false);
                return addresses.Length > 0;
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}