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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.DockerHostMetric
{
    [GridEndpoint("DockerHostMetric", Title = "Docker host metric", DefaultSort = "createdDate")]
    [GridSearch(Label = "Instance or operating system contains")]
    public sealed record DockerHostMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Description("Total number of containers on the host, in any state.")]
        [property: ListColumn(Order = 30, Format = GridFormat.Integer)]
        int? ContainersTotal,
        [property: Description("Number of containers currently running.")]
        [property: ListColumn(Order = 40, Format = GridFormat.Integer)]
        int? ContainersRunning,
        [property: Description("Number of containers currently paused.")]
        [property: ListColumn(Order = 50, Format = GridFormat.Integer)]
        int? ContainersPaused,
        [property: Description("Number of containers currently stopped.")]
        [property: ListColumn(Order = 60, Format = GridFormat.Integer)]
        int? ContainersStopped,
        [property: Description("Total number of images present on the host.")]
        [property: ListColumn(Order = 70, Format = GridFormat.Integer)]
        int? ImagesCount,
        [property: Description("Number of CPUs available to the Docker daemon.")]
        [property: ListColumn(Order = 80, Title = "NCPU", Format = GridFormat.Integer)]
        int? NCpu,
        [property: Description("Total host memory in bytes, as reported by the Docker daemon.")]
        [property: ListColumn(Order = 90, Title = "Mem Total", Unit = "B", Format = GridFormat.Integer)]
        long? MemTotalBytes,
        [property: Description("The Docker Engine version string.")]
        [property: ListColumn(Order = 160)]
        string DockerVersion,
        [property: Description("The Docker Engine API version string.")]
        [property: ListColumn(Order = 170)]
        string ApiVersion,
        [property: Description("The host kernel version.")]
        [property: ListColumn(Order = 180)]
        string KernelVersion,
        [property: Description("The host operating system's descriptive name.")]
        [property: ListColumn(Order = 190)]
        string OperatingSystem,
        [property: Description("The host OS type (e.g. linux, windows).")]
        [property: ListColumn(Order = 200)]
        string OsType,
        [property: Description("The host CPU architecture (e.g. x86_64).")]
        [property: ListColumn(Order = 210)]
        string Architecture,
        [property: Description("Total size in bytes of image layers on disk.")]
        [property: ListColumn(Order = 100, Title = "Layers Size", Unit = "B", Format = GridFormat.Integer)]
        long? LayersSizeBytes,
        [property: Description("Total size in bytes of all images on the host.")]
        [property: ListColumn(Order = 110, Title = "Images Size", Unit = "B", Format = GridFormat.Integer)]
        long? ImagesSizeBytes,
        [property:
            Description("Total size in bytes of images that are not referenced by any container and could be pruned.")]
        [property: ListColumn(Order = 120, Title = "Reclaimable Images", Unit = "B", Format = GridFormat.Integer)]
        long? ReclaimableImagesBytes,
        [property: Description("Total size in bytes of the writable layer of every container on the host.")]
        [property: ListColumn(Order = 130, Title = "Containers Disk", Unit = "B", Format = GridFormat.Integer)]
        long? ContainersDiskBytes,
        [property: Description("Total size in bytes of every volume on the host that has computed usage data.")]
        [property: ListColumn(Order = 140, Title = "Volumes Size", Unit = "B", Format = GridFormat.Integer)]
        long? VolumesSizeBytes,
        [property: Description("Total size in bytes of the Docker build cache.")]
        [property: ListColumn(Order = 150, Title = "Build Cache Size", Unit = "B", Format = GridFormat.Integer)]
        long? BuildCacheSizeBytes,
        [property: Description("UTC timestamp this sample was taken and flushed to the database.")]
        [property: ListColumn(Order = 10, Title = "Sampled Date")]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property:
            Description(
                "Hostname of the Jube.Monitoring instance that captured this sample -- one row per Docker host per minute.")]
        [property: ListColumn(Order = 20)]
        string Instance);
}