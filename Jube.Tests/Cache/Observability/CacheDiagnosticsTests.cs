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
using Jube.Cache.Observability;
using Jube.Cache.Observability.CacheCallCounters;
using Xunit;

namespace Jube.Test.Cache.Observability
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheDiagnosticsTests
    {
        [Fact]
        public async Task RecordAsyncOfTReturnsFuncResultAndRecordsOneCallAsync()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            var result = await CacheDiagnostics.RecordAsync(call, () => Task.FromResult(42));

            result.Should().Be(42);
            CacheCallCounters.TakeSnapshot().Single(s => s.Call == call).Count.Should().Be(1);
        }

        [Fact]
        public async Task RecordAsyncVoidRecordsOneCallAsync()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";
            var invoked = false;

            await CacheDiagnostics.RecordAsync(call, () =>
            {
                invoked = true;
                return Task.CompletedTask;
            });

            invoked.Should().BeTrue();
            CacheCallCounters.TakeSnapshot().Single(s => s.Call == call).Count.Should().Be(1);
        }

        [Fact]
        public void RecordOfTReturnsFuncResultAndRecordsOneCall()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            var result = CacheDiagnostics.Record(call, () => 99);

            result.Should().Be(99);
            CacheCallCounters.TakeSnapshot().Single(s => s.Call == call).Count.Should().Be(1);
        }

        [Fact]
        public void RecordWithTimeSpanRecordsOneCallWithTheGivenDuration()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            CacheDiagnostics.Record(call, TimeSpan.FromMilliseconds(2));

            var snapshot = CacheCallCounters.TakeSnapshot().Single(s => s.Call == call);
            snapshot.Count.Should().Be(1);
            snapshot.TotalMicroseconds.Should().Be(2000);
        }

        [Fact]
        public async Task InFlightCallCountIncrementsWhileAsyncCallIsPendingAndDecrementsAfterAsync()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";
            var gate = new TaskCompletionSource<bool>();
            var baseline = CacheDiagnostics.InFlightCallCount;

#pragma warning disable VSTHRD003
            var task = CacheDiagnostics.RecordAsync(call, async () => await gate.Task);
#pragma warning restore VSTHRD003

            await Task.Delay(50);
            CacheDiagnostics.InFlightCallCount.Should().Be(baseline + 1);

            gate.SetResult(true);
            await task;

            CacheDiagnostics.InFlightCallCount.Should().Be(baseline);
        }

        [Fact]
        public async Task InFlightCallCountDecrementsEvenWhenTheDelegateThrowsAsync()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";
            var baseline = CacheDiagnostics.InFlightCallCount;

            Func<Task> act = () => CacheDiagnostics.RecordAsync(call,
                new Func<Task<int>>(() => throw new InvalidOperationException()));

            await act.Should().ThrowAsync<InvalidOperationException>();
            CacheDiagnostics.InFlightCallCount.Should().Be(baseline);
        }
    }
}