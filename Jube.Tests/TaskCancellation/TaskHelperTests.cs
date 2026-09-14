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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.TaskCancellation.TaskHelper;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.TaskCancellation
{
    [Trait("Category", "Unit")]
    public sealed class TaskHelperTests
    {
        [Fact]
        public void TimedTaskResultConstructorArgumentOrderIsComputeTimeThenMemory()
        {
            var result = new TimedTaskResult(TaskType.SanctionsAsync, 123, 456);

            result.ComputeTime.Should().Be(123);
            result.ThreadMemory.Should().Be(456);
        }

        [Theory]
        [InlineData(TaskType.SanctionsAsync, 1)]
        [InlineData(TaskType.TtlCountersAsync, 3)]
        [InlineData(TaskType.AbstractionRulesWithSearchKeysAsync, 4)]
        [InlineData(TaskType.CachePayloadLatestUpsertAsync, 5)]
        [InlineData(TaskType.CachePayloadUpsertAsync, 6)]
        [InlineData(TaskType.CachePayloadInsertAsync, 7)]
        [InlineData(TaskType.CacheTtlCounterEntryUpsertAsync, 8)]
        [InlineData(TaskType.CacheTtlCounterEntryIncrementAsync, 9)]
        [InlineData(TaskType.ExecuteTimeToLiveCounterIterationAsync, 13)]
        [InlineData(TaskType.CacheSanctionInsertAsync, 15)]
        [InlineData(TaskType.ExecuteAbstractionRulesWithSearchKeyAsync, 16)]
        [InlineData(TaskType.BulkInsertCachePayloadRemovalBatchEntry, 17)]
        [InlineData(TaskType.SortedSetRemoveReferenceDate, 18)]
        [InlineData(TaskType.SetRemoveAsync, 19)]
        [InlineData(TaskType.PublishAsync, 20)]
        [InlineData(TaskType.HashDecrementBytes, 21)]
        [InlineData(TaskType.HashDecrementCount, 22)]
        [InlineData(TaskType.HashDeletePayload, 23)]
        [InlineData(TaskType.HashDeletePayloadBulk, 24)]
        [InlineData(TaskType.AppendBulkCleanupOfPayloadGuids, 25)]
        [InlineData(TaskType.SortedSetRemoveReferenceDateLatest, 26)]
        [InlineData(TaskType.HashDeletePayloadLatest, 28)]
        [InlineData(TaskType.ProcessTtlCounterDeprecation, 30)]
        [InlineData(TaskType.BulkInsertTtlCounterEntryRemovalBatchResponseTime, 31)]
        [InlineData(TaskType.BulkInsertCachePayloadLatestRemovalBatchEntry, 32)]
        [InlineData(TaskType.SortedSetLruJournalRemove, 33)]
        [InlineData(TaskType.BulkTtlCounterIdempotencyRemovalBatchEntry, 34)]
        [InlineData(TaskType.UpsertReferenceDateAsync, 35)]
        [InlineData(TaskType.InlineFunction, 36)]
        [InlineData(TaskType.InlineScript, 37)]
        [InlineData(TaskType.Gateway, 38)]
        [InlineData(TaskType.HttpAdaptation, 39)]
        public void TaskTypeNumericValueIsStable(TaskType taskType, int expectedValue)
        {
            ((int)taskType).Should().Be(expectedValue);
        }

        [Fact]
        public Task MeasureTaskTimeAndMemoryAllocatedAsyncWithoutLogPropagatesFaultUnchangedAsync()
        {
            var act = () => TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SanctionsAsync,
                () => throw new InvalidOperationException("boom"));

            return act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public async Task MeasureTaskTimeAndMemoryAllocatedAsyncWithLogReturnsFaultedResultInsteadOfThrowingAsync()
        {
            var log = new TestLog();

            var result = await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SanctionsAsync,
                () => throw new InvalidOperationException("boom"), log);

            result.Faulted.Should().BeTrue();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("SanctionsAsync"));
        }

        [Fact]
        public async Task MeasureTaskTimeAndMemoryAllocatedAsyncWithLogAndRethrowOnFaultPropagatesFaultAsync()
        {
            var log = new TestLog();

            var act = () => TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SanctionsAsync,
                () => throw new InvalidOperationException("boom"), log, true);

            await act.Should().ThrowAsync<InvalidOperationException>();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("SanctionsAsync"));
        }

        [Fact]
        public async Task MeasureTaskTimeAndMemoryAllocatedAsyncOnSuccessIsNotFaultedAsync()
        {
            var result = await TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SanctionsAsync,
                () => Task.CompletedTask);

            result.Faulted.Should().BeFalse();
            result.TaskType.Should().Be(TaskType.SanctionsAsync);
        }

        [Fact]
        public void MeasureTimeAndMemoryAllocatedSynchronousOverloadDoesNotThrowWhenLogSuppliedOnFault()
        {
            var log = new TestLog();

            var result = TaskHelper.MeasureTimeAndMemoryAllocated(TaskType.Gateway,
                () => throw new InvalidOperationException("boom"), log);

            result.Faulted.Should().BeTrue();
            log.Entries.Should().Contain(e => e.Level == "ERROR" && e.Message.Contains("Gateway"));
        }

        [Fact]
        public void MeasureTimeAndMemoryAllocatedSynchronousOverloadWithoutLogPropagatesFault()
        {
            Action act = () => TaskHelper.MeasureTimeAndMemoryAllocated(TaskType.Gateway,
                () => throw new InvalidOperationException("boom"));

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MeasureTimeAndMemoryAllocatedSynchronousOverloadOnSuccessRecordsElapsedTime()
        {
            var result = TaskHelper.MeasureTimeAndMemoryAllocated(TaskType.Gateway, () =>
            {
                var sum = Enumerable.Range(0, 1000).Sum();
                _ = sum;
            });

            result.Faulted.Should().BeFalse();
            result.ComputeTime.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}