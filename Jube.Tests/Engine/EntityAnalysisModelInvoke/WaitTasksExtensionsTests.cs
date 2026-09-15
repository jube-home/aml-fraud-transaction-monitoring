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

using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.TaskCancellation.TaskHelper;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class WaitTasksExtensionsTests
    {
        private static Context NewContext(bool logSampled = true)
        {
            return new Context
            {
                EntityAnalysisModel = new EntityAnalysisModel(),
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = logSampled
            };
        }

        private static Task<TimedTaskResult> CompletedTaskAsync(TaskType taskType, long computeTime = 100,
            long threadMemory = 200, bool faulted = false)
        {
            return Task.FromResult(new TimedTaskResult(taskType, computeTime, threadMemory, faulted));
        }

        [Fact]
        public async Task WaitReadTasksAsyncWithNoPendingTasksLeavesAnEmptyReadStatsBlockAsync()
        {
            var context = NewContext();

            await context.WaitReadTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Read.Should()
                .NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Read
                .SanctionsAsync.Should().BeNull();
        }

        [Fact]
        public async Task WaitReadTasksAsyncRecordsSanctionsAsyncComputeTimeAndMemoryAsync()
        {
            var context = NewContext();
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.SanctionsAsync, 123, 456));

            await context.WaitReadTasksAsync();

            var recorded = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats
                .Read.SanctionsAsync;
            recorded.Should().NotBeNull();
            recorded!.ComputeTimeMicroseconds.Should().Be(123);
            recorded.Memory.Should().Be(456);
        }

        [Fact]
        public async Task
            WaitReadTasksAsyncRecordsTtlCountersAsyncAndAbstractionRulesWithSearchKeysAsyncIndependentlyAsync()
        {
            var context = NewContext();
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.TtlCountersAsync, 10, 20));
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.AbstractionRulesWithSearchKeysAsync, 30, 40));

            await context.WaitReadTasksAsync();

            var read = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Read;
            read.TtlCountersAsync.Should().NotBeNull();
            read.TtlCountersAsync!.ComputeTimeMicroseconds.Should().Be(10);
            read.AbstractionRulesWithSearchKeysAsync.Should().NotBeNull();
            read.AbstractionRulesWithSearchKeysAsync!.ComputeTimeMicroseconds.Should().Be(30);
        }

        [Fact]
        public async Task WaitReadTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync()
        {
            var context = NewContext();
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.SanctionsAsync, faulted: true));

            var act = async () => await context.WaitReadTasksAsync();

            await act.Should().NotThrowAsync();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Read
                .SanctionsAsync.Should().BeNull();
        }

        [Fact]
        public async Task WaitReadTasksAsyncWhenSampledRecordsTheJoinReadTasksStageDurationAsync()
        {
            var context = NewContext();
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.SanctionsAsync));

            await context.WaitReadTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages!.JoinReadTasks.Should()
                .NotBeNull();
        }

        [Fact]
        public async Task WaitReadTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync()
        {
            var context = NewContext(false);
            context.PendingReadTasks.Add(CompletedTaskAsync(TaskType.SanctionsAsync));

            await context.WaitReadTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
        }

        [Fact]
        public async Task WaitWriteTasksAsyncWithNoPendingTasksLeavesAnEmptyWriteStatsBlockAsync()
        {
            var context = NewContext();

            await context.WaitWriteTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Write.Should()
                .NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Write
                .CachePayloadUpsertAsync.Should().BeNull();
        }

        [Theory]
        [InlineData(TaskType.CachePayloadLatestUpsertAsync)]
        [InlineData(TaskType.CachePayloadUpsertAsync)]
        [InlineData(TaskType.CachePayloadInsertAsync)]
        [InlineData(TaskType.CacheTtlCounterEntryUpsertAsync)]
        [InlineData(TaskType.CacheTtlCounterEntryIncrementAsync)]
        [InlineData(TaskType.CacheSanctionInsertAsync)]
        [InlineData(TaskType.UpsertReferenceDateAsync)]
        public async Task WaitWriteTasksAsyncRecordsEachRecognisedWriteTaskTypeAsync(TaskType taskType)
        {
            var context = NewContext();
            context.PendingWriteTasks.Add(CompletedTaskAsync(taskType, 55, 66));

            await context.WaitWriteTasksAsync();

            var taskWrapperStats = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance
                .TaskWrapperStats;
            var recorded = taskWrapperStats.EnumerateTasks().Should().ContainSingle().Subject;
            recorded.Name.Should().Be(taskType.ToString());
            recorded.Task.ComputeTimeMicroseconds.Should().Be(55);
            recorded.Task.Memory.Should().Be(66);
        }

        [Fact]
        public async Task WaitWriteTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync()
        {
            var context = NewContext();
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.CachePayloadUpsertAsync, faulted: true));

            var act = async () => await context.WaitWriteTasksAsync();

            await act.Should().NotThrowAsync();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Write
                .CachePayloadUpsertAsync.Should().BeNull();
        }

        [Fact]
        public async Task WaitWriteTasksAsyncIgnoresATaskTypeItDoesNotRecogniseAsync()
        {
            var context = NewContext();
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.SanctionsAsync));

            var act = async () => await context.WaitWriteTasksAsync();

            await act.Should().NotThrowAsync();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats
                .EnumerateTasks().Should().BeEmpty();
        }

        [Fact]
        public async Task WaitWriteTasksAsyncWhenSampledRecordsTheJoinWriteTasksStageDurationAsync()
        {
            var context = NewContext();
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.CachePayloadUpsertAsync));

            await context.WaitWriteTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages!.JoinWriteTasks.Should()
                .NotBeNull();
        }

        [Fact]
        public async Task WaitWriteTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync()
        {
            var context = NewContext(false);
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.CachePayloadUpsertAsync));

            await context.WaitWriteTasksAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
        }

        [Fact]
        public async Task WaitWriteTasksAsyncRecordsMultipleDistinctWriteTaskTypesIndependentlyAsync()
        {
            var context = NewContext();
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.CachePayloadInsertAsync, 1, 2));
            context.PendingWriteTasks.Add(CompletedTaskAsync(TaskType.CacheSanctionInsertAsync, 3, 4));

            await context.WaitWriteTasksAsync();

            var write = context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.TaskWrapperStats.Write;
            write.CachePayloadInsertAsync.Should().NotBeNull();
            write.CacheSanctionInsertAsync.Should().NotBeNull();
        }
    }
}