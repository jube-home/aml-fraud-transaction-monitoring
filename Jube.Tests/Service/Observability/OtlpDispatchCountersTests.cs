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
using FluentAssertions;
using Jube.Service.Observability.OtlpDispatchCounters;
using Xunit;

namespace Jube.Test.Service.Observability
{
    [Trait("Category", "Unit")]
    [Collection("OtlpDispatchCounters")]
    public sealed class OtlpDispatchCountersTests
    {
        [Fact]
        public void RecordDispatchAccumulatesCountSuccessFailureAndTimingForOneSignal()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";

            OtlpDispatchCounters.RecordDispatch(signal, true, 10, 5.0);
            OtlpDispatchCounters.RecordDispatch(signal, false, 20, 15.0);

            var snapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);

            snapshot.Count.Should().Be(2);
            snapshot.SuccessCount.Should().Be(1);
            snapshot.FailureCount.Should().Be(1);
            snapshot.ItemCount.Should().Be(30);
            snapshot.TotalMicroseconds.Should().Be(20000);
            snapshot.MinMicroseconds.Should().Be(5000);
            snapshot.MaxMicroseconds.Should().Be(15000);
            snapshot.DroppedCount.Should().Be(0);
        }

        [Fact]
        public void RecordDroppedIsIndependentOfDispatchCount()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";

            OtlpDispatchCounters.RecordDropped(signal, 3);
            OtlpDispatchCounters.RecordDropped(signal, 2);

            var snapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);

            snapshot.DroppedCount.Should().Be(5);
            snapshot.Count.Should().Be(0);
            snapshot.SuccessCount.Should().Be(0);
            snapshot.FailureCount.Should().Be(0);
        }

        [Fact]
        public void TakeSnapshotResetsCountersButKeepsTheAccumulatorForTheNextCycle()
        {
            var signal = $"unit-test-signal-{Guid.NewGuid():N}";

            OtlpDispatchCounters.RecordDispatch(signal, true, 1, 1.0);
            OtlpDispatchCounters.TakeSnapshot();

            var secondSnapshot = OtlpDispatchCounters.TakeSnapshot();
            secondSnapshot.Should().NotContain(s => s.Signal == signal);

            OtlpDispatchCounters.RecordDispatch(signal, true, 7, 2.0);
            var thirdSnapshot = OtlpDispatchCounters.TakeSnapshot().Single(s => s.Signal == signal);
            thirdSnapshot.Count.Should().Be(1);
            thirdSnapshot.ItemCount.Should().Be(7);
        }
    }
}