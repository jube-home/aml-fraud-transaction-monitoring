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
using Jube.Cache.Observability.CacheCallCounters;
using Xunit;

namespace Jube.Test.Cache.Observability
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheCallCountersTests
    {
        [Fact]
        public void RecordAccumulatesCountTotalMinAndMaxForOneCall()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            CacheCallCounters.Record(call, 10);
            CacheCallCounters.Record(call, 5);
            CacheCallCounters.Record(call, 15);

            var snapshot = CacheCallCounters.TakeSnapshot().Single(s => s.Call == call);

            snapshot.Count.Should().Be(3);
            snapshot.TotalMicroseconds.Should().Be(30000);
            snapshot.MinMicroseconds.Should().Be(5000);
            snapshot.MaxMicroseconds.Should().Be(15000);
        }

        [Fact]
        public void TakeSnapshotResetsCountersButKeepsTheAccumulatorForTheNextCycle()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            CacheCallCounters.Record(call, 1);
            CacheCallCounters.TakeSnapshot();

            var secondSnapshot = CacheCallCounters.TakeSnapshot();
            secondSnapshot.Should().NotContain(s => s.Call == call);

            CacheCallCounters.Record(call, 7);
            var thirdSnapshot = CacheCallCounters.TakeSnapshot().Single(s => s.Call == call);
            thirdSnapshot.Count.Should().Be(1);
            thirdSnapshot.TotalMicroseconds.Should().Be(7000);
        }

        [Fact]
        public void TakeSnapshotOmitsCallsWithZeroCount()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";

            CacheCallCounters.TakeSnapshot().Should().NotContain(s => s.Call == call);
        }

        [Fact]
        public async Task RecordUnderConcurrentLoadProducesExactAggregatesAsync()
        {
            var call = $"unit-test-call-{Guid.NewGuid():N}";
            const int iterations = 1000;

            await Task.WhenAll(Enumerable.Range(1, iterations)
                .Select(i => Task.Run(() => CacheCallCounters.Record(call, i))));

            var snapshot = CacheCallCounters.TakeSnapshot().Single(s => s.Call == call);

            snapshot.Count.Should().Be(iterations);
            snapshot.MinMicroseconds.Should().Be(1000);
            snapshot.MaxMicroseconds.Should().Be(iterations * 1000);
            snapshot.TotalMicroseconds.Should().Be(Enumerable.Range(1, iterations).Sum() * 1000L);
        }
    }
}