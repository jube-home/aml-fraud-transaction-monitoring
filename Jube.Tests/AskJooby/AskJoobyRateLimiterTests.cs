/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using FluentAssertions;
using Jube.Service.AskJoobyConversation;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class AskJoobyRateLimiterTests
    {
        private static readonly DateTime now = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        private static AskJoobyRateLimits Limits(int perUser = 3, int perTenant = 100, int concurrentUser = 100,
            int concurrentTenant = 100)
        {
            return new AskJoobyRateLimits(60, perUser, perTenant, concurrentUser, concurrentTenant);
        }

        [Fact]
        public void AUserIsRefusedOnceTheyPassTheirWindowLimitAndAllowedAgainWhenItRollsOver()
        {
            const int tenant = 880001;

            for (var i = 0; i < 3; i++)
            {
                AskJoobyRateLimiter.TryBegin(tenant, "alice", Limits(), now, out var slot).Should().BeTrue();
                slot!.Dispose();
            }

            AskJoobyRateLimiter.TryBegin(tenant, "alice", Limits(), now.AddSeconds(30), out _).Should().BeFalse();
            AskJoobyRateLimiter.TryBegin(tenant, "bob", Limits(), now.AddSeconds(30), out var bob).Should().BeTrue();
            bob!.Dispose();
            AskJoobyRateLimiter.TryBegin(tenant, "ALICE", Limits(), now.AddSeconds(61), out var later)
                .Should().BeTrue();
            later!.Dispose();
        }

        [Fact]
        public void TheTenantWindowLimitCapsAllItsUsersTogether()
        {
            const int tenant = 880002;

            AskJoobyRateLimiter.TryBegin(tenant, "a", Limits(perTenant: 2), now, out var one).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "b", Limits(perTenant: 2), now, out var two).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "c", Limits(perTenant: 2), now, out _).Should().BeFalse();
            one!.Dispose();
            two!.Dispose();
        }

        [Fact]
        public void ConcurrentTurnsPerUserAreCappedAndTheSlotIsFreedOnDispose()
        {
            const int tenant = 880003;
            var limits = Limits(perUser: 100, concurrentUser: 2);

            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out var first).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out var second).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out _).Should().BeFalse();

            first!.Dispose();
            first.Dispose();

            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out var third).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out _).Should().BeFalse();

            second!.Dispose();
            third!.Dispose();
        }

        [Fact]
        public void ConcurrentTurnsPerTenantAreCapped()
        {
            const int tenant = 880004;
            var limits = Limits(perUser: 100, concurrentUser: 100, concurrentTenant: 1);

            AskJoobyRateLimiter.TryBegin(tenant, "a", limits, now, out var first).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "b", limits, now, out _).Should().BeFalse();
            first!.Dispose();
        }

        [Fact]
        public void ARefusedAttemptDoesNotHoldASlot()
        {
            const int tenant = 880005;
            var limits = Limits(perUser: 1, concurrentUser: 1);

            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out var first).Should().BeTrue();
            AskJoobyRateLimiter.TryBegin(tenant, "alice", limits, now, out var refused).Should().BeFalse();
            refused.Should().BeNull();
            first!.Dispose();
        }

        [Fact]
        public void LimitsFallBackToSaneDefaultsWhenNothingIsConfigured()
        {
            var limits = AskJoobyRateLimits.From(null);

            limits.WindowSeconds.Should().Be(60);
            limits.MaxMessagesPerUserPerWindow.Should().Be(20);
            limits.MaxMessagesPerTenantPerWindow.Should().Be(120);
            limits.MaxConcurrentTurnsPerUser.Should().Be(3);
            limits.MaxConcurrentTurnsPerTenant.Should().Be(20);
        }
    }
}
