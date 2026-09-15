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

namespace Jube.Dto.DotNetRuntimeMetric
{
    public sealed record DotNetRuntimeMetricDto(
        [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        int Id,
        [property: Description("UTC timestamp this row's sample was taken.")]
        DateTime CreatedDate,
        [property: Description("Hostname of the node this sample describes.")]
        string Instance,
        [property: Description("Logical processor count available to the process.")]
        int ProcessorCount,
        [property: Description("Cumulative Gen0 garbage collections since process start.")]
        int Gen0CollectionCount,
        [property: Description("Cumulative Gen1 garbage collections since process start.")]
        int Gen1CollectionCount,
        [property: Description("Cumulative Gen2 garbage collections since process start.")]
        int Gen2CollectionCount,
        [property: Description("Cumulative bytes allocated by the managed heap since process start.")]
        long TotalAllocatedBytes,
        [property: Description("Current total size of the managed heap, in bytes.")]
        long HeapSizeBytes,
        [property: Description("Bytes of the managed heap currently fragmented.")]
        long FragmentedBytes,
        [property: Description("Current physical memory load on the machine, in bytes.")]
        long MemoryLoadBytes,
        [property:
            Description("The memory load threshold at which the GC considers the machine under high memory pressure.")]
        long HighMemoryLoadThresholdBytes,
        [property: Description("Process working set, in bytes.")]
        long WorkingSetBytes,
        [property: Description("Process private memory, in bytes.")]
        long PrivateMemoryBytes,
        [property: Description("Number of OS threads currently owned by the process.")]
        int ThreadCount,
        [property: Description("ThreadPool worker threads currently available (not busy).")]
        int ThreadPoolWorkerThreadsAvailable,
        [property: Description("ThreadPool worker thread maximum configured for the process.")]
        int ThreadPoolWorkerThreadsMax,
        [property: Description("ThreadPool I/O completion port threads currently available (not busy).")]
        int ThreadPoolCompletionPortThreadsAvailable,
        [property: Description("ThreadPool I/O completion port thread maximum configured for the process.")]
        int ThreadPoolCompletionPortThreadsMax,
        [property: Description("Number of work items currently queued on the ThreadPool, awaiting a free thread.")]
        long ThreadPoolQueueLength,
        [property:
            Description("Cumulative processor time consumed by the process, in microseconds, since process start.")]
        long CpuTimeMicroseconds,
        [property:
            Description(
                "The memory budget the .NET GC believes it has available -- container-limit-aware since .NET Core 3.0, so this reflects the container's memory limit when running containerized.")]
        long RuntimeAvailableMemoryBytes,
        [property: Description("Total bytes currently committed by the GC.")]
        long RuntimeCommittedMemoryBytes,
        [property:
            Description(
                "The container's own CPU limit in cores, read directly from its cgroup (cpu.max/cpu.cfs_quota_us); null if not running in a container with a CPU limit, or if cgroup pseudo-files aren't reachable.")]
        double? ContainerCpuLimitCores,
        [property:
            Description(
                "Cumulative CPU time consumed by the whole container (every process in its cgroup), in microseconds, since the container started; null if cgroup pseudo-files aren't reachable.")]
        long? ContainerCpuUsageMicroseconds,
        [property:
            Description(
                "Cumulative number of scheduling periods in which the container was throttled by its CPU limit; null if cgroup pseudo-files aren't reachable. Non-zero and climbing means the container is CPU-starved by its own limit, not by the host.")]
        long? ContainerCpuThrottledPeriods,
        [property:
            Description(
                "Cumulative time the container spent throttled by its CPU limit, in microseconds; null if cgroup pseudo-files aren't reachable.")]
        long? ContainerCpuThrottledMicroseconds,
        [property:
            Description(
                "The container's own memory limit in bytes, read directly from its cgroup; null if unlimited or not running in a container.")]
        long? ContainerMemoryLimitBytes,
        [property:
            Description(
                "The container's own current memory usage in bytes as seen by its cgroup (the same figure the container runtime's OOM killer acts on); null if cgroup pseudo-files aren't reachable.")]
        long? ContainerMemoryUsageBytes,
        [property: Description("Cumulative time spent in GC pauses since process start, in microseconds.")]
        long GcPauseTimeMicroseconds,
        [property:
            Description(
                "Cumulative number of times a thread had to wait to enter a lock (Monitor) since process start -- a rising rate here points at lock contention as a cause of slowdown.")]
        long LockContentionCount);
}