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
using LinqToDB.Mapping;
using MessagePack;

namespace Jube.Data.Poco
{
    [Table]
    [MessagePackObject]
    public class DockerHostMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ContainersTotal { get; set; }
        [Column] [Nullable] [Key(2)] public int? ContainersRunning { get; set; }
        [Column] [Nullable] [Key(3)] public int? ContainersPaused { get; set; }
        [Column] [Nullable] [Key(4)] public int? ContainersStopped { get; set; }
        [Column] [Nullable] [Key(5)] public int? ImagesCount { get; set; }
        [Column] [Nullable] [Key(6)] public int? NCpu { get; set; }
        [Column] [Nullable] [Key(7)] public long? MemTotalBytes { get; set; }
        [Column] [Nullable] [Key(8)] public string DockerVersion { get; set; }
        [Column] [Nullable] [Key(9)] public string ApiVersion { get; set; }
        [Column] [Nullable] [Key(10)] public string KernelVersion { get; set; }
        [Column] [Nullable] [Key(11)] public string OperatingSystem { get; set; }
        [Column] [Nullable] [Key(12)] public string OsType { get; set; }
        [Column] [Nullable] [Key(13)] public string Architecture { get; set; }
        [Column] [Nullable] [Key(14)] public long? LayersSizeBytes { get; set; }
        [Column] [Nullable] [Key(15)] public long? ImagesSizeBytes { get; set; }
        [Column] [Nullable] [Key(16)] public long? ReclaimableImagesBytes { get; set; }
        [Column] [Nullable] [Key(17)] public long? ContainersDiskBytes { get; set; }
        [Column] [Nullable] [Key(18)] public long? VolumesSizeBytes { get; set; }
        [Column] [Nullable] [Key(19)] public long? BuildCacheSizeBytes { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string Instance { get; set; }
    }
}