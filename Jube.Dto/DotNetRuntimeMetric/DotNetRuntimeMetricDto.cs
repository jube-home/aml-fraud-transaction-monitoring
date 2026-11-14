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

namespace Jube.Dto.DotNetRuntimeMetric
{
    [GridEndpoint("DotNetRuntimeMetric", Title = "Dot net runtime metric", DefaultSort = "createdDate")]
    [GridSearch(Label = "Instance (hostname) contains")]
    [MessagePackObject]
    public sealed record DotNetRuntimeMetricDto(
        [property: Key(0)] [property: Description("Server-assigned identifier of the row, most recent first when listed.")]
        [property: ListColumn(Hidden = true)]
        int Id,
        [property: Key(1)] [property: Description("UTC timestamp this row's sample was taken.")]
        [property: ListColumn(Order = 10)]
        [property: GridTimeRange]
        DateTime CreatedDate,
        [property: Key(2)] [property: Description("Hostname of the node this sample describes.")]
        [property: ListColumn(Order = 20)]
        string Instance,
        [property: Key(3)] [property: Description("Logical processor count available to the process.")]
        [property: ListColumn(Order = 30, Title = "Processors", Format = GridFormat.Integer)]
        int ProcessorCount,
        [property: Key(4)] [property: Description("Cumulative Gen0 garbage collections since process start.")]
        [property: ListColumn(Order = 40, Title = "Gen0 GC", Format = GridFormat.Integer)]
        int Gen0CollectionCount,
        [property: Key(5)] [property: Description("Cumulative Gen1 garbage collections since process start.")]
        [property: ListColumn(Order = 50, Title = "Gen1 GC", Format = GridFormat.Integer)]
        int Gen1CollectionCount,
        [property: Key(6)] [property: Description("Cumulative Gen2 garbage collections since process start.")]
        [property: ListColumn(Order = 60, Title = "Gen2 GC", Format = GridFormat.Integer)]
        int Gen2CollectionCount,
        [property: Key(7)] [property: Description("Cumulative bytes allocated by the managed heap since process start.")]
        [property: ListColumn(Order = 70, Title = "Total Allocated", Unit = "B", Format = GridFormat.Integer)]
        long TotalAllocatedBytes,
        [property: Key(8)] [property: Description("Current total size of the managed heap, in bytes.")]
        [property: ListColumn(Order = 80, Title = "Heap Size", Unit = "B", Format = GridFormat.Integer)]
        long HeapSizeBytes,
        [property: Key(9)] [property: Description("Bytes of the managed heap currently fragmented.")]
        [property: ListColumn(Order = 90, Title = "Fragmented", Unit = "B", Format = GridFormat.Integer)]
        long FragmentedBytes,
        [property: Key(10)] [property: Description("Current physical memory load on the machine, in bytes.")]
        [property: ListColumn(Order = 100, Title = "Memory Load", Unit = "B", Format = GridFormat.Integer)]
        long MemoryLoadBytes,
        [property: Key(11)] [property:
            Description("The memory load threshold at which the GC considers the machine under high memory pressure.")]
        [property: ListColumn(Hidden = true)]
        long HighMemoryLoadThresholdBytes,
        [property: Key(12)] [property: Description("Process working set, in bytes.")]
        [property: ListColumn(Order = 110, Title = "Working Set", Unit = "B", Format = GridFormat.Integer)]
        long WorkingSetBytes,
        [property: Key(13)] [property: Description("Process private memory, in bytes.")]
        [property: ListColumn(Order = 120, Title = "Private Memory", Unit = "B", Format = GridFormat.Integer)]
        long PrivateMemoryBytes,
        [property: Key(14)] [property: Description("Number of OS threads currently owned by the process.")]
        [property: ListColumn(Order = 130, Format = GridFormat.Integer)]
        int ThreadCount,
        [property: Key(15)] [property: Description("ThreadPool worker threads currently available (not busy).")]
        [property: ListColumn(Order = 140, Title = "Worker Threads Available", Format = GridFormat.Integer)]
        int ThreadPoolWorkerThreadsAvailable,
        [property: Key(16)] [property: Description("ThreadPool worker thread maximum configured for the process.")]
        [property: ListColumn(Order = 150, Title = "Worker Threads Max", Format = GridFormat.Integer)]
        int ThreadPoolWorkerThreadsMax,
        [property: Key(17)] [property: Description("ThreadPool I/O completion port threads currently available (not busy).")]
        [property: ListColumn(Order = 160, Title = "IOCP Threads Available", Format = GridFormat.Integer)]
        int ThreadPoolCompletionPortThreadsAvailable,
        [property: Key(18)] [property: Description("ThreadPool I/O completion port thread maximum configured for the process.")]
        [property: ListColumn(Order = 170, Title = "IOCP Threads Max", Format = GridFormat.Integer)]
        int ThreadPoolCompletionPortThreadsMax,
        [property: Key(19)] [property: Description("Number of work items currently queued on the ThreadPool, awaiting a free thread.")]
        [property: ListColumn(Order = 180, Title = "ThreadPool Queue Length", Format = GridFormat.Integer)]
        long ThreadPoolQueueLength,
        [property: Key(20)] [property:
            Description("Cumulative processor time consumed by the process, in microseconds, since process start.")]
        [property: ListColumn(Order = 190, Title = "CPU Time", Unit = "µs", Format = GridFormat.Integer)]
        long CpuTimeMicroseconds,
        [property: Key(21)] [property:
            Description(
                "The memory budget the .NET GC believes it has available -- container-limit-aware since .NET Core 3.0, so this reflects the container's memory limit when running containerized.")]
        [property: ListColumn(Order = 200, Title = "Runtime Available Memory", Unit = "B", Format = GridFormat.Integer)]
        long RuntimeAvailableMemoryBytes,
        [property: Key(22)] [property: Description("Total bytes currently committed by the GC.")]
        [property: ListColumn(Order = 210, Title = "Runtime Committed Memory", Unit = "B", Format = GridFormat.Integer)]
        long RuntimeCommittedMemoryBytes,
        [property: Key(23)] [property:
            Description(
                "The container's own CPU limit in cores, read directly from its cgroup (cpu.max/cpu.cfs_quota_us); null if not running in a container with a CPU limit, or if cgroup pseudo-files aren't reachable.")]
        [property: ListColumn(Order = 220, Format = GridFormat.Decimal)]
        double? ContainerCpuLimitCores,
        [property: Key(24)] [property:
            Description(
                "Cumulative CPU time consumed by the whole container (every process in its cgroup), in microseconds, since the container started; null if cgroup pseudo-files aren't reachable.")]
        [property: ListColumn(Order = 230, Title = "Container CPU Usage", Unit = "µs", Format = GridFormat.Integer)]
        long? ContainerCpuUsageMicroseconds,
        [property: Key(25)] [property:
            Description(
                "Cumulative number of scheduling periods in which the container was throttled by its CPU limit; null if cgroup pseudo-files aren't reachable. Non-zero and climbing means the container is CPU-starved by its own limit, not by the host.")]
        [property: ListColumn(Order = 240, Format = GridFormat.Integer)]
        long? ContainerCpuThrottledPeriods,
        [property: Key(26)] [property:
            Description(
                "Cumulative time the container spent throttled by its CPU limit, in microseconds; null if cgroup pseudo-files aren't reachable.")]
        [property: ListColumn(Order = 250, Title = "Container CPU Throttled", Unit = "µs", Format = GridFormat.Integer)]
        long? ContainerCpuThrottledMicroseconds,
        [property: Key(27)] [property:
            Description(
                "The container's own memory limit in bytes, read directly from its cgroup; null if unlimited or not running in a container.")]
        [property: ListColumn(Order = 260, Title = "Container Memory Limit", Unit = "B", Format = GridFormat.Integer)]
        long? ContainerMemoryLimitBytes,
        [property: Key(28)] [property:
            Description(
                "The container's own current memory usage in bytes as seen by its cgroup (the same figure the container runtime's OOM killer acts on); null if cgroup pseudo-files aren't reachable.")]
        [property: ListColumn(Order = 270, Title = "Container Memory Usage", Unit = "B", Format = GridFormat.Integer)]
        long? ContainerMemoryUsageBytes,
        [property: Key(29)] [property: Description("Cumulative time spent in GC pauses since process start, in microseconds.")]
        [property: ListColumn(Order = 280, Title = "GC Pause Time", Unit = "µs", Format = GridFormat.Integer)]
        long GcPauseTimeMicroseconds,
        [property: Key(30)] [property:
            Description(
                "Cumulative number of times a thread had to wait to enter a lock (Monitor) since process start -- a rising rate here points at lock contention as a cause of slowdown.")]
        [property: ListColumn(Order = 290, Format = GridFormat.Integer)]
        long LockContentionCount);
}