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
using System.Diagnostics;
using System.Threading;
using Jube.Data.Poco;
using Jube.DynamicEnvironment.Container;

namespace Jube.Engine.BackgroundTasks.TaskStarters.Metrics
{
    public sealed class DotNetRuntimeMetricSampler
    {
        public DotNetRuntimeMetric Sample()
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            using var process = Process.GetCurrentProcess();
            var containerLimits = ContainerResourceLimitReader.Read();

            ThreadPool.GetAvailableThreads(out var workerAvailable, out var completionPortAvailable);
            ThreadPool.GetMaxThreads(out var workerMax, out var completionPortMax);

            return new DotNetRuntimeMetric
            {
                ProcessorCount = Environment.ProcessorCount,
                Gen0CollectionCount = GC.CollectionCount(0),
                Gen1CollectionCount = GC.CollectionCount(1),
                Gen2CollectionCount = GC.CollectionCount(2),
                TotalAllocatedBytes = GC.GetTotalAllocatedBytes(),
                HeapSizeBytes = gcMemoryInfo.HeapSizeBytes,
                FragmentedBytes = gcMemoryInfo.FragmentedBytes,
                MemoryLoadBytes = gcMemoryInfo.MemoryLoadBytes,
                HighMemoryLoadThresholdBytes = gcMemoryInfo.HighMemoryLoadThresholdBytes,
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                ThreadCount = process.Threads.Count,
                ThreadPoolWorkerThreadsAvailable = workerAvailable,
                ThreadPoolWorkerThreadsMax = workerMax,
                ThreadPoolCompletionPortThreadsAvailable = completionPortAvailable,
                ThreadPoolCompletionPortThreadsMax = completionPortMax,
                ThreadPoolQueueLength = ThreadPool.PendingWorkItemCount,
                CpuTimeMicroseconds =
                    (long)(process.TotalProcessorTime.Ticks / (double)TimeSpan.TicksPerMillisecond * 1000.0),
                RuntimeAvailableMemoryBytes = gcMemoryInfo.TotalAvailableMemoryBytes,
                RuntimeCommittedMemoryBytes = gcMemoryInfo.TotalCommittedBytes,
                ContainerCpuLimitCores = containerLimits.CpuLimitCores,
                ContainerCpuUsageMicroseconds = containerLimits.CpuUsageMicroseconds,
                ContainerCpuThrottledPeriods = containerLimits.CpuThrottledPeriods,
                ContainerCpuThrottledMicroseconds = containerLimits.CpuThrottledMicroseconds,
                ContainerMemoryLimitBytes = containerLimits.MemoryLimitBytes,
                ContainerMemoryUsageBytes = containerLimits.MemoryUsageBytes,
                GcPauseTimeMicroseconds = GC.GetTotalPauseDuration().Ticks / (TimeSpan.TicksPerMillisecond / 1000),
                LockContentionCount = Monitor.LockContentionCount
            };
        }
    }
}