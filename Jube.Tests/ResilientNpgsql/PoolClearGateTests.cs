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
using Jube.ResilientNpgsqlConnection;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.ResilientNpgsql
{
    [Trait("Category", "Unit")]
    public sealed class PoolClearGateTests
    {
        private const string Key = "Host=haproxy;Port=5432;Username=jube_app";

        [Fact]
        public void TheFirstCallerForAKeyEnters()
        {
            var gate = new PoolClearGate(new FakeTimeProvider(), TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
        }

        [Fact]
        public void ASecondCallerInsideTheWindowIsBlocked()
        {
            var timeProvider = new FakeTimeProvider();
            var gate = new PoolClearGate(timeProvider, TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
            timeProvider.Advance(TimeSpan.FromSeconds(4));

            gate.TryEnter(Key).Should().BeFalse();
        }

        [Fact]
        public void ACallerEntersAgainOnceTheWindowHasElapsed()
        {
            var timeProvider = new FakeTimeProvider();
            var gate = new PoolClearGate(timeProvider, TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
            timeProvider.Advance(TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
        }

        [Fact]
        public void TheWindowIsMeasuredFromTheLastEntryRatherThanTheFirst()
        {
            var timeProvider = new FakeTimeProvider();
            var gate = new PoolClearGate(timeProvider, TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
            timeProvider.Advance(TimeSpan.FromSeconds(5));
            gate.TryEnter(Key).Should().BeTrue();
            timeProvider.Advance(TimeSpan.FromSeconds(4));

            gate.TryEnter(Key).Should().BeFalse();
        }

        [Fact]
        public void ABlockedAttemptDoesNotExtendTheWindow()
        {
            var timeProvider = new FakeTimeProvider();
            var gate = new PoolClearGate(timeProvider, TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();
            timeProvider.Advance(TimeSpan.FromSeconds(4));
            gate.TryEnter(Key).Should().BeFalse();
            timeProvider.Advance(TimeSpan.FromSeconds(1));

            gate.TryEnter(Key).Should().BeTrue();
        }

        [Fact]
        public void KeysAreIndependentOfOneAnother()
        {
            var gate = new PoolClearGate(new FakeTimeProvider(), TimeSpan.FromSeconds(5));

            gate.TryEnter(Key).Should().BeTrue();

            gate.TryEnter("Host=haproxy;Port=5433;Username=jube_reporting").Should().BeTrue();
        }

        [Fact]
        public async Task ExactlyOneOfManyConcurrentCallersEntersAsync()
        {
            const int callers = 256;
            var gate = new PoolClearGate(new FakeTimeProvider(), TimeSpan.FromSeconds(5));

            var attempts = Enumerable.Range(0, callers)
                .Select(_ => Task.Run(() => gate.TryEnter(Key)))
                .ToArray();

            var entered = await Task.WhenAll(attempts).ConfigureAwait(false);

            entered.Count(w => w).Should().Be(1);
        }

        [Fact]
        public void AZeroWindowNeverBlocks()
        {
            var gate = new PoolClearGate(new FakeTimeProvider(), TimeSpan.Zero);

            gate.TryEnter(Key).Should().BeTrue();
            gate.TryEnter(Key).Should().BeTrue();
        }

        [Fact]
        public void TheSharedGateIsReplacedByConfiguration()
        {
            var before = PoolClearGate.Shared;

            try
            {
                PoolClearGate.ConfigureShared(TimeSpan.FromMilliseconds(1234));

                PoolClearGate.Shared.Should().NotBeSameAs(before);
                PoolClearGate.Shared.Window.Should().Be(TimeSpan.FromMilliseconds(1234));
            }
            finally
            {
                PoolClearGate.ConfigureShared(before.Window);
            }
        }
    }
}