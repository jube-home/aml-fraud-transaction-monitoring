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

using System.Net;
using Jube.Data.Poco;
using Newtonsoft.Json.Linq;

namespace Jube.Monitoring
{
    public sealed class OverlayNetworkTaskDriftSampler(DockerApiClient client)
    {
        private static readonly string[] serviceNames =
        [
            "jube-ui", "jube-api", "haproxy",
            "patroni1", "patroni2", "patroni3", "patroni4",
            "redis-master", "redis-replica1", "redis-replica2", "redis-replica3",
            "sentinel1", "sentinel2", "sentinel3", "sentinel4", "sentinel5",
            "etcd1", "etcd2", "etcd3", "etcd4", "etcd5"
        ];

        public async Task<List<OverlayNetworkTaskDrift>> SampleAsync()
        {
            var results = new List<OverlayNetworkTaskDrift>();

            if (!await client.IsSwarmManagerAsync().ConfigureAwait(false))
            {
                return results;
            }

            foreach (var serviceName in serviceNames)
            {
                var drift = await SampleServiceAsync(serviceName).ConfigureAwait(false);
                if (drift != null)
                {
                    results.Add(drift);
                }
            }

            return results;
        }

        private async Task<OverlayNetworkTaskDrift?> SampleServiceAsync(string serviceName)
        {
            var tasks = await client.GetRunningTasksForServiceAsync(serviceName).ConfigureAwait(false);
            if (tasks == null)
            {
                return null;
            }

            HashSet<string> dnsAddresses;
            try
            {
                dnsAddresses = (await Dns.GetHostAddressesAsync($"tasks.{serviceName}").ConfigureAwait(false))
                    .Select(address => address.ToString())
                    .ToHashSet();
            }
            catch (Exception)
            {
                dnsAddresses = [];
            }

            var swarmAddresses = ExtractRunningTaskAddresses(tasks);

            var addressesOnlyInDns = dnsAddresses.Except(swarmAddresses).ToList();
            var addressesOnlyInSwarm = swarmAddresses.Except(dnsAddresses).ToList();

            return new OverlayNetworkTaskDrift
            {
                ServiceName = serviceName,
                DnsResolvedAddresses = string.Join(",", dnsAddresses),
                SwarmTaskAddresses = string.Join(",", swarmAddresses),
                AddressesOnlyInDns = string.Join(",", addressesOnlyInDns),
                AddressesOnlyInSwarm = string.Join(",", addressesOnlyInSwarm),
                IsConsistent = addressesOnlyInDns.Count == 0 && addressesOnlyInSwarm.Count == 0
            };
        }

        private static HashSet<string> ExtractRunningTaskAddresses(JArray tasks)
        {
            var addresses = new HashSet<string>();

            foreach (var task in tasks)
            {
                if (!string.Equals((string?)task["Status"]?["State"], "running",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (task["NetworksAttachments"] is not JArray attachments)
                {
                    continue;
                }

                foreach (var attachment in attachments)
                {
                    if (attachment["Addresses"] is not JArray taskAddresses)
                    {
                        continue;
                    }

                    foreach (var address in taskAddresses)
                    {
                        var cidr = (string?)address;
                        if (string.IsNullOrEmpty(cidr))
                        {
                            continue;
                        }

                        addresses.Add(cidr.Split('/')[0]);
                    }
                }
            }

            return addresses;
        }
    }
}