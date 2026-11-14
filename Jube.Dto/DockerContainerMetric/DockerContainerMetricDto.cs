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
using Jube.Dto.Forms;
using Jube.Dto.Options;
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.DockerContainerMetric
{
    [GridEndpoint("DockerContainerMetric", Title = "Docker container metric", DefaultSort = "occurredDate")]
    [GridSearch(Label = "Name, image, state or status contains")]
    [MessagePackObject]
    public sealed record DockerContainerMetricDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp this sample was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime OccurredDate,
        [property: Key(2)] [property: Description("The full Docker container id.")]
        [property: ListColumn(Order = 220)]
        string ContainerId,
        [property: Key(3)] [property: Description("The container's name (leading slash stripped).")]
        [property: ListColumn(Order = 20)]
        string Name,
        [property: Key(4)] [property: Description("The image the container was created from.")]
        [property: ListColumn(Order = 30)]
        string Image,
        [property: Key(5)] [property: Description("Docker's own state string (e.g. running, exited, paused).")]
        [property: ListColumn(Order = 40)]
        string State,
        [property: Key(6)] [property:
            Description("Docker's own human-readable status string (e.g. 'Up 3 hours', 'Exited (0) 2 minutes ago').")]
        [property: ListColumn(Order = 50)]
        string Status,
        [property: Key(7)] [property: Description("Number of times Docker has restarted this container.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        int RestartCount,
        [property: Key(8)] [property: Description("Whether the container's last exit was due to an out-of-memory kill.")]
        [property: ListColumn(Order = 80)]
        bool OomKilled,
        [property: Key(9)] [property: Description("The container's last exit code.")]
        [property: ListColumn(Order = 90, Format = GridFormat.Integer)]
        int ExitCode,
        [property: Key(10)] [property: Description("UTC timestamp the container was last started, if it has ever been started.")]
        [property: ListColumn(Order = 210)]
        DateTime? StartedAt,
        [property: Key(11)] [property:
            Description(
                "Docker HEALTHCHECK status (e.g. healthy, unhealthy, starting), or 'none' when the image defines no healthcheck.")]
        [property: ListColumn(Order = 60, Title = "Health")]
        string HealthStatus,
        [property: Key(12)] [property:
            Description(
                "CPU usage as a percentage of one CPU multiplied by the container's online CPU count, computed the same way 'docker stats' computes it.")]
        [property: ListColumn(Order = 100, Title = "CPU %", Unit = "%", Format = GridFormat.Decimal)]
        double? CpuUsagePercent,
        [property: Key(13)] [property:
            Description("Number of CPUs Docker considered online for this container when computing CpuUsagePercent.")]
        [property: ListColumn(Order = 110, Format = GridFormat.Integer)]
        int? OnlineCpus,
        [property: Key(14)] [property: Description("Current memory usage in bytes, including reclaimable page cache.")]
        [property: ListColumn(Order = 120, Title = "Memory Usage", Unit = "B", Format = GridFormat.Integer)]
        long? MemoryUsageBytes,
        [property: Key(15)] [property: Description("The container's memory limit in bytes.")]
        [property: ListColumn(Order = 130, Title = "Memory Limit", Unit = "B", Format = GridFormat.Integer)]
        long? MemoryLimitBytes,
        [property: Key(16)] [property: Description("MemoryUsageBytes as a percentage of MemoryLimitBytes.")]
        [property: ListColumn(Order = 140, Title = "Memory %", Unit = "%", Format = GridFormat.Decimal)]
        double? MemoryPercent,
        [property: Key(17)] [property: Description("Total bytes received, summed across all of the container's network interfaces.")]
        [property: ListColumn(Order = 150, Title = "Network Rx", Unit = "B", Format = GridFormat.Integer)]
        long? NetworkRxBytes,
        [property: Key(18)] [property: Description("Total bytes transmitted, summed across all of the container's network interfaces.")]
        [property: ListColumn(Order = 160, Title = "Network Tx", Unit = "B", Format = GridFormat.Integer)]
        long? NetworkTxBytes,
        [property: Key(19)] [property: Description("Total bytes read from block devices.")]
        [property: ListColumn(Order = 170, Title = "Block Read", Unit = "B", Format = GridFormat.Integer)]
        long? BlockReadBytes,
        [property: Key(20)] [property: Description("Total bytes written to block devices.")]
        [property: ListColumn(Order = 180, Title = "Block Write", Unit = "B", Format = GridFormat.Integer)]
        long? BlockWriteBytes,
        [property: Key(21)] [property: Description("Current number of processes/threads (pids) running in the container.")]
        [property: ListColumn(Order = 190, Format = GridFormat.Integer)]
        int? PidsCurrent,
        [property: Key(22)] [property: Description("The container's pids limit, if one is set.")]
        [property: ListColumn(Order = 200, Format = GridFormat.Integer)]
        int? PidsLimit,
        [property: Key(23)] [property: Description("UTC timestamp this row was flushed to the database.")]
        [property: ListColumn(Order = 240, Title = "Flushed Date")]
        DateTime CreatedDate,
        [property: Key(24)] [property: Description("Hostname of the Jube.Monitoring instance that captured this sample.")]
        [property: ListColumn(Order = 230)]
        string Instance);
}