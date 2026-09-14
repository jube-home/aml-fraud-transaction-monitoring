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

namespace Jube.Dto.DockerHostMetric
{
    public sealed record DockerHostMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("Total number of containers on the host, in any state.")]
        int? ContainersTotal,
        [property: Description("Number of containers currently running.")]
        int? ContainersRunning,
        [property: Description("Number of containers currently paused.")]
        int? ContainersPaused,
        [property: Description("Number of containers currently stopped.")]
        int? ContainersStopped,
        [property: Description("Total number of images present on the host.")]
        int? ImagesCount,
        [property: Description("Number of CPUs available to the Docker daemon.")]
        int? NCpu,
        [property: Description("Total host memory in bytes, as reported by the Docker daemon.")]
        long? MemTotalBytes,
        [property: Description("The Docker Engine version string.")]
        string DockerVersion,
        [property: Description("The Docker Engine API version string.")]
        string ApiVersion,
        [property: Description("The host kernel version.")]
        string KernelVersion,
        [property: Description("The host operating system's descriptive name.")]
        string OperatingSystem,
        [property: Description("The host OS type (e.g. linux, windows).")]
        string OsType,
        [property: Description("The host CPU architecture (e.g. x86_64).")]
        string Architecture,
        [property: Description("Total size in bytes of image layers on disk.")]
        long? LayersSizeBytes,
        [property: Description("Total size in bytes of all images on the host.")]
        long? ImagesSizeBytes,
        [property:
            Description("Total size in bytes of images that are not referenced by any container and could be pruned.")]
        long? ReclaimableImagesBytes,
        [property: Description("Total size in bytes of the writable layer of every container on the host.")]
        long? ContainersDiskBytes,
        [property: Description("Total size in bytes of every volume on the host that has computed usage data.")]
        long? VolumesSizeBytes,
        [property: Description("Total size in bytes of the Docker build cache.")]
        long? BuildCacheSizeBytes,
        [property: Description("UTC timestamp this sample was taken and flushed to the database.")]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that captured this sample -- one row per Docker host per minute.")]
        string Instance);
}