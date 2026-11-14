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

using System.Threading;
using FluentAssertions;
using Jube.App.Code.Watcher;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.App.Code.Watcher
{
    [Trait("Category", "Unit")]
    public sealed class WatcherStreamRegistryTests
    {
        private static WatcherStreamRegistry NewRegistry()
        {
            return new WatcherStreamRegistry(TestLog.NoOp);
        }

        [Fact]
        public void DispatchReachesOnlyTheConnectionsOfThatTenant()
        {
            var registry = NewRegistry();
            var tenantOne = registry.Track("a", "alice", "1", 1, new CancellationTokenSource());
            var tenantOneToo = registry.Track("b", "bob", "1", 1, new CancellationTokenSource());
            var tenantTwo = registry.Track("c", "carol", "1", 2, new CancellationTokenSource());

            registry.Dispatch(1, "one");

            tenantOne.Reader.TryRead(out var first).Should().BeTrue();
            tenantOneToo.Reader.TryRead(out var second).Should().BeTrue();
            first.Should().Be("one");
            second.Should().Be("one");
            tenantTwo.Reader.TryRead(out _).Should().BeFalse();
        }

        [Fact]
        public void DispatchToATenantWithNoConnectionsIsANoOpAndARemovedConnectionReceivesNothing()
        {
            var registry = NewRegistry();
            var channel = registry.Track("a", "alice", "1", 1, new CancellationTokenSource());

            registry.Dispatch(99, "nobody");
            registry.Remove("a");
            registry.Dispatch(1, "late");

            channel.Reader.TryRead(out _).Should().BeFalse();
            registry.Tracked().Should().BeEmpty();
        }

        [Fact]
        public void ASlowClientLosesTheOldestMessagesAndNeverBlocksTheDispatcher()
        {
            var registry = NewRegistry();
            var channel = registry.Track("a", "alice", "1", 1, new CancellationTokenSource());

            for (var index = 0; index < 300; index++)
            {
                registry.Dispatch(1, index.ToString());
            }

            var received = 0;
            string last = null;

            while (channel.Reader.TryRead(out var item))
            {
                received++;
                last = item;
            }

            received.Should().Be(256);
            last.Should().Be("299");
        }

        [Fact]
        public void AbortingAUserCancelsOnlyThatUsersStreamsAndMarksThemRevoked()
        {
            var registry = NewRegistry();
            var aliceSource = new CancellationTokenSource();
            var bobSource = new CancellationTokenSource();

            registry.Track("a", "alice", "1", 1, aliceSource);
            registry.Track("b", "bob", "1", 1, bobSource);

            registry.AbortUser("alice");

            aliceSource.IsCancellationRequested.Should().BeTrue();
            registry.IsRevoked("a").Should().BeTrue();
            bobSource.IsCancellationRequested.Should().BeFalse();
            registry.IsRevoked("b").Should().BeFalse();
        }
    }
}
