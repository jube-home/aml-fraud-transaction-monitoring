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
using Jube.Data.Poco;
using Newtonsoft.Json.Linq;

namespace Jube.Monitoring
{
    public sealed class DockerContainerMetricSampler(DockerApiClient client)
    {
        public async Task<List<DockerContainerMetric>> SampleAsync()
        {
            var results = new List<DockerContainerMetric>();

            var containers = await client.GetContainersAsync().ConfigureAwait(false);
            if (containers == null)
            {
                return results;
            }

            foreach (var container in containers)
            {
                var containerId = (string?)container["Id"];
                if (containerId == null)
                {
                    continue;
                }

                var stats = await client.GetContainerStatsAsync(containerId).ConfigureAwait(false);
                var inspect = await client.InspectContainerAsync(containerId).ConfigureAwait(false);

                var name = ((string?)container["Names"]?[0])?.TrimStart('/');

                results.Add(new DockerContainerMetric
                {
                    ContainerId = containerId,
                    Name = name,
                    Image = (string?)container["Image"],
                    State = (string?)container["State"],
                    Status = (string?)container["Status"],
                    RestartCount = (int?)inspect?["RestartCount"],
                    OomKilled = (bool?)inspect?["State"]?["OOMKilled"],
                    ExitCode = (int?)inspect?["State"]?["ExitCode"],
                    StartedAt = ParseDate((string?)inspect?["State"]?["StartedAt"]),
                    HealthStatus = (string?)container["Health"]?["Status"],
                    CpuUsagePercent = ComputeCpuPercent(stats, out var onlineCpus),
                    OnlineCpus = onlineCpus,
                    MemoryUsageBytes = (long?)stats?["memory_stats"]?["usage"],
                    MemoryLimitBytes = (long?)stats?["memory_stats"]?["limit"],
                    MemoryPercent = ComputeMemoryPercent(stats),
                    NetworkRxBytes = SumNetwork(stats, "rx_bytes"),
                    NetworkTxBytes = SumNetwork(stats, "tx_bytes"),
                    BlockReadBytes = SumBlkio(stats, "read"),
                    BlockWriteBytes = SumBlkio(stats, "write"),
                    PidsCurrent = (int?)stats?["pids_stats"]?["current"],
                    PidsLimit = (int?)stats?["pids_stats"]?["limit"]
                });
            }

            return results;
        }

        private static double? ComputeCpuPercent(JObject? stats, out int? onlineCpus)
        {
            onlineCpus = (int?)stats?["cpu_stats"]?["online_cpus"];

            var currentTotal = (long?)stats?["cpu_stats"]?["cpu_usage"]?["total_usage"];
            var previousTotal = (long?)stats?["precpu_stats"]?["cpu_usage"]?["total_usage"];
            var currentSystem = (long?)stats?["cpu_stats"]?["system_cpu_usage"];
            var previousSystem = (long?)stats?["precpu_stats"]?["system_cpu_usage"];

            if (currentTotal == null || previousTotal == null || currentSystem == null || previousSystem == null ||
                onlineCpus is null or 0)
            {
                return null;
            }

            var cpuDelta = currentTotal.Value - previousTotal.Value;
            var systemDelta = currentSystem.Value - previousSystem.Value;

            return systemDelta > 0 && cpuDelta >= 0
                ? cpuDelta / (double)systemDelta * onlineCpus.Value * 100.0
                : null;
        }

        private static double? ComputeMemoryPercent(JObject? stats)
        {
            var usage = (long?)stats?["memory_stats"]?["usage"];
            var limit = (long?)stats?["memory_stats"]?["limit"];

            return usage.HasValue && limit is > 0 ? usage.Value / (double)limit.Value * 100.0 : null;
        }

        private static long? SumNetwork(JObject? stats, string field)
        {
            if (stats?["networks"] is not JObject networks)
            {
                return null;
            }

            long total = 0;
            var any = false;
            foreach (var network in networks.Properties())
            {
                var value = (long?)network.Value[field];
                if (value.HasValue)
                {
                    total += value.Value;
                    any = true;
                }
            }

            return any ? total : null;
        }

        private static long? SumBlkio(JObject? stats, string op)
        {
            if (stats?["blkio_stats"]?["io_service_bytes_recursive"] is not JArray entries)
            {
                return null;
            }

            long total = 0;
            var any = false;
            foreach (var entry in entries)
            {
                if (!string.Equals((string?)entry["op"], op, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = (long?)entry["value"];
                if (value.HasValue)
                {
                    total += value.Value;
                    any = true;
                }
            }

            return any ? total : null;
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.StartsWith("0001-01-01"))
            {
                return null;
            }

            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.UtcDateTime
                : null;
        }
    }
}