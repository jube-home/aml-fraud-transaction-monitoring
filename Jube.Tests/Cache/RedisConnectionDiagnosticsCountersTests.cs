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

using FluentAssertions;
using Jube.Cache;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RedisConnectionDiagnosticsCountersTests
    {
        [Fact]
        public void TakeSnapshotReflectsEachCounterIndependently()
        {
            var counters = new RedisConnectionDiagnosticsCounters();

            counters.IncrementConnectionFailed();
            counters.IncrementConnectionFailed();
            counters.IncrementConnectionRestored();
            counters.IncrementErrorMessage();
            counters.IncrementInternalError();
            counters.IncrementConfigurationChanged();
            counters.IncrementConfigurationChangedBroadcast();

            var snapshot = counters.TakeSnapshot();

            snapshot.ConnectionFailedCount.Should().Be(2);
            snapshot.ConnectionRestoredCount.Should().Be(1);
            snapshot.ErrorMessageCount.Should().Be(1);
            snapshot.InternalErrorCount.Should().Be(1);
            snapshot.ConfigurationChangedCount.Should().Be(1);
            snapshot.ConfigurationChangedBroadcastCount.Should().Be(1);
        }

        [Fact]
        public void TakeSnapshotResetsAllCountersToZero()
        {
            var counters = new RedisConnectionDiagnosticsCounters();
            counters.IncrementConnectionFailed();
            counters.TakeSnapshot();

            var secondSnapshot = counters.TakeSnapshot();

            secondSnapshot.ConnectionFailedCount.Should().Be(0);
            secondSnapshot.ConnectionRestoredCount.Should().Be(0);
            secondSnapshot.ErrorMessageCount.Should().Be(0);
            secondSnapshot.InternalErrorCount.Should().Be(0);
            secondSnapshot.ConfigurationChangedCount.Should().Be(0);
            secondSnapshot.ConfigurationChangedBroadcastCount.Should().Be(0);
        }

        [Fact]
        public void FreshInstanceStartsAtZero()
        {
            var snapshot = new RedisConnectionDiagnosticsCounters().TakeSnapshot();

            snapshot.Should().Be(new RedisConnectionDiagnosticsSnapshot(0, 0, 0, 0, 0, 0));
        }
    }
}