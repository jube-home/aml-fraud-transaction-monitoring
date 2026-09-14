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

using System.ComponentModel;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Jube.Dto.DockerContainerMetric
{
    public sealed record DockerContainerMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this sample was taken.")]
        DateTime OccurredDate,
        [property: Description("The full Docker container id.")]
        string ContainerId,
        [property: Description("The container's name (leading slash stripped).")]
        string Name,
        [property: Description("The image the container was created from.")]
        string Image,
        [property: Description("Docker's own state string (e.g. running, exited, paused).")]
        string State,
        [property:
            Description("Docker's own human-readable status string (e.g. 'Up 3 hours', 'Exited (0) 2 minutes ago').")]
        string Status,
        [property: Description("Number of times Docker has restarted this container.")]
        int RestartCount,
        [property: Description("Whether the container's last exit was due to an out-of-memory kill.")]
        bool OomKilled,
        [property: Description("The container's last exit code.")]
        int ExitCode,
        [property: Description("UTC timestamp the container was last started, if it has ever been started.")]
        DateTime? StartedAt,
        [property:
            Description(
                "Docker HEALTHCHECK status (e.g. healthy, unhealthy, starting), or 'none' when the image defines no healthcheck.")]
        string HealthStatus,
        [property:
            Description(
                "CPU usage as a percentage of one CPU multiplied by the container's online CPU count, computed the same way 'docker stats' computes it.")]
        double? CpuUsagePercent,
        [property:
            Description("Number of CPUs Docker considered online for this container when computing CpuUsagePercent.")]
        int? OnlineCpus,
        [property: Description("Current memory usage in bytes, including reclaimable page cache.")]
        long? MemoryUsageBytes,
        [property: Description("The container's memory limit in bytes.")]
        long? MemoryLimitBytes,
        [property: Description("MemoryUsageBytes as a percentage of MemoryLimitBytes.")]
        double? MemoryPercent,
        [property: Description("Total bytes received, summed across all of the container's network interfaces.")]
        long? NetworkRxBytes,
        [property: Description("Total bytes transmitted, summed across all of the container's network interfaces.")]
        long? NetworkTxBytes,
        [property: Description("Total bytes read from block devices.")]
        long? BlockReadBytes,
        [property: Description("Total bytes written to block devices.")]
        long? BlockWriteBytes,
        [property: Description("Current number of processes/threads (pids) running in the container.")]
        int? PidsCurrent,
        [property: Description("The container's pids limit, if one is set.")]
        int? PidsLimit,
        [property: Description("UTC timestamp this row was flushed to the database.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the Jube.Monitoring instance that captured this sample.")]
        string Instance);
}