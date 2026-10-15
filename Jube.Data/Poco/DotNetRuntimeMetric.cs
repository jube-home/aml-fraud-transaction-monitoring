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
    public class DotNetRuntimeMetric
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public int? ProcessorCount { get; set; }
        [Column] [Nullable] [Key(4)] public int? Gen0CollectionCount { get; set; }
        [Column] [Nullable] [Key(5)] public int? Gen1CollectionCount { get; set; }
        [Column] [Nullable] [Key(6)] public int? Gen2CollectionCount { get; set; }
        [Column] [Nullable] [Key(7)] public long? TotalAllocatedBytes { get; set; }
        [Column] [Nullable] [Key(8)] public long? HeapSizeBytes { get; set; }
        [Column] [Nullable] [Key(9)] public long? FragmentedBytes { get; set; }
        [Column] [Nullable] [Key(10)] public long? MemoryLoadBytes { get; set; }
        [Column] [Nullable] [Key(11)] public long? HighMemoryLoadThresholdBytes { get; set; }
        [Column] [Nullable] [Key(12)] public long? WorkingSetBytes { get; set; }
        [Column] [Nullable] [Key(13)] public long? PrivateMemoryBytes { get; set; }
        [Column] [Nullable] [Key(14)] public int? ThreadCount { get; set; }
        [Column] [Nullable] [Key(15)] public int? ThreadPoolWorkerThreadsAvailable { get; set; }
        [Column] [Nullable] [Key(16)] public int? ThreadPoolWorkerThreadsMax { get; set; }
        [Column] [Nullable] [Key(17)] public int? ThreadPoolCompletionPortThreadsAvailable { get; set; }
        [Column] [Nullable] [Key(18)] public int? ThreadPoolCompletionPortThreadsMax { get; set; }
        [Column] [Nullable] [Key(19)] public long? ThreadPoolQueueLength { get; set; }
        [Column] [Nullable] [Key(20)] public long? CpuTimeMicroseconds { get; set; }
        [Column] [Nullable] [Key(21)] public long? RuntimeAvailableMemoryBytes { get; set; }
        [Column] [Nullable] [Key(22)] public long? RuntimeCommittedMemoryBytes { get; set; }
        [Column] [Nullable] [Key(23)] public double? ContainerCpuLimitCores { get; set; }
        [Column] [Nullable] [Key(24)] public long? ContainerCpuUsageMicroseconds { get; set; }
        [Column] [Nullable] [Key(25)] public long? ContainerCpuThrottledPeriods { get; set; }
        [Column] [Nullable] [Key(26)] public long? ContainerCpuThrottledMicroseconds { get; set; }
        [Column] [Nullable] [Key(27)] public long? ContainerMemoryLimitBytes { get; set; }
        [Column] [Nullable] [Key(28)] public long? ContainerMemoryUsageBytes { get; set; }
        [Column] [Nullable] [Key(29)] public long? GcPauseTimeMicroseconds { get; set; }
        [Column] [Nullable] [Key(30)] public long? LockContentionCount { get; set; }
    }
}