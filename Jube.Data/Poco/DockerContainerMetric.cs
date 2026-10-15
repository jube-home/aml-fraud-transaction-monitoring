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
    public class DockerContainerMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? OccurredDate { get; set; }
        [Column] [Nullable] [Key(2)] public string ContainerId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string Image { get; set; }
        [Column] [Nullable] [Key(5)] public string State { get; set; }
        [Column] [Nullable] [Key(6)] public string Status { get; set; }
        [Column] [Nullable] [Key(7)] public int? RestartCount { get; set; }
        [Column] [Nullable] [Key(8)] public bool? OomKilled { get; set; }
        [Column] [Nullable] [Key(9)] public int? ExitCode { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? StartedAt { get; set; }
        [Column] [Nullable] [Key(11)] public string HealthStatus { get; set; }
        [Column] [Nullable] [Key(12)] public double? CpuUsagePercent { get; set; }
        [Column] [Nullable] [Key(13)] public int? OnlineCpus { get; set; }
        [Column] [Nullable] [Key(14)] public long? MemoryUsageBytes { get; set; }
        [Column] [Nullable] [Key(15)] public long? MemoryLimitBytes { get; set; }
        [Column] [Nullable] [Key(16)] public double? MemoryPercent { get; set; }
        [Column] [Nullable] [Key(17)] public long? NetworkRxBytes { get; set; }
        [Column] [Nullable] [Key(18)] public long? NetworkTxBytes { get; set; }
        [Column] [Nullable] [Key(19)] public long? BlockReadBytes { get; set; }
        [Column] [Nullable] [Key(20)] public long? BlockWriteBytes { get; set; }
        [Column] [Nullable] [Key(21)] public int? PidsCurrent { get; set; }
        [Column] [Nullable] [Key(22)] public int? PidsLimit { get; set; }
        [Column] [Nullable] [Key(23)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(24)] public string Instance { get; set; }
    }
}